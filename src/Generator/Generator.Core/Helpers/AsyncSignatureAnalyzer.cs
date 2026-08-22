using System.Collections.Concurrent;
using System.Linq;
using Generator.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Generator.Helpers;

/// <summary>
/// Analyzes an <see cref="IMethodSymbol"/> and classifies its async characteristics.
/// Isolated so it is easy to unit-test.
/// </summary>
internal sealed class AsyncSignatureAnalyzer
{
    private readonly TypeSystemHelper _typeHelper;
    private readonly ConcurrentDictionary<IMethodSymbol, AsyncSignatureInfo> _cache = new(SymbolEqualityComparer.Default);

    // Full Task/ValueTask type names used for comparisons
    private const string TaskFullName = "System.Threading.Tasks.Task";
    private const string ValueTaskFullName = "System.Threading.Tasks.ValueTask";
    private const string TaskOfTFullName = "System.Threading.Tasks.Task`1";
    private const string ValueTaskOfTFullName = "System.Threading.Tasks.ValueTask`1";

    // bool/void type names for comparisons
    private const string BoolFullName = "System.Boolean";
    private const string VoidFullName = "System.Void";

    /// <summary>
    /// Initializes a new instance of the AsyncSignatureAnalyzer class.
    /// </summary>
    public AsyncSignatureAnalyzer(TypeSystemHelper typeHelper)
    {
        _typeHelper = typeHelper;
    }


    /// <summary>
    /// Analyzes a method signature, caching results.
    /// </summary>
    public AsyncSignatureInfo Analyze(IMethodSymbol method, Compilation compilation) => _cache.GetOrAdd(method, _ =>
                                                                                             {
                                                                                                 var (isAsync, isBoolEquivalent) = _typeHelper.AnalyzeAwaitable(method.ReturnType, compilation);

                                                                                                 var taskSym = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                                                                                                 var valueTaskSym = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
                                                                                                 var taskOfTSym = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

                                                                                                 var info = new AsyncSignatureInfo
                                                                                                 {
                                                                                                     IsAsync = isAsync,
                                                                                                     IsBoolEquivalent = isBoolEquivalent,
                                                                                                     IsVoidEquivalent =
                                                                                                         method.ReturnType.SpecialType == SpecialType.System_Void ||
                                                                                                         (isAsync && (SymbolEqualityComparer.Default.Equals(method.ReturnType, taskSym) ||
                                                                                                                      SymbolEqualityComparer.Default.Equals(method.ReturnType, valueTaskSym)))
                                                                                                 };

                                                                                                 // async void
                                                                                                 if (method.IsAsync && method.ReturnsVoid)
                                                                                                     info.IsInvalidAsyncVoid = true;

                                                                                                 // Guard: Task<bool> (ValueTask<bool> jest OK)
                                                                                                 if (isBoolEquivalent &&
                                                                                                     method.ReturnType is INamedTypeSymbol nts &&
                                                                                                     SymbolEqualityComparer.Default.Equals(nts.ConstructedFrom, taskOfTSym))
                                                                                                 {
                                                                                                     info.IsInvalidGuardTask = true;
                                                                                                 }

                                                                                                 return info;
                                                                                             });


    /// <summary>
    /// Analyzes a method signature with extra validation for a specific callback kind.
    /// </summary>
    public AsyncSignatureInfo AnalyzeCallback(IMethodSymbol methodSymbol, string callbackType, Compilation compilation)
    {
        var info = Analyze(methodSymbol, compilation);

        // Extra validation per callback kind
        if (callbackType == "Guard" && info.IsAsync)
        {
            // Guards must return ValueTask<bool>, not Task<bool>
            if (info.IsBoolEquivalent && IsTaskBool(methodSymbol.ReturnType))
            {
                info.IsInvalidGuardTask = true;
            }
        }

        return info;
    }



    /// <summary>
    /// Returns the expected return type for a callback kind and async mode.
    /// </summary>
    public string GetExpectedReturnType(string callbackType, bool isAsync) => (callbackType, isAsync) switch
    {
        ("Guard", false) => "bool",
        ("Guard", true) => "ValueTask<bool>",
        ("Action", false) => "void",
        ("Action", true) => "Task or ValueTask",
        ("OnEntry", false) => "void",
        ("OnEntry", true) => "Task or ValueTask",
        ("OnExit", false) => "void",
        ("OnExit", true) => "Task or ValueTask",
        _ => "void" // default for unknown callback kinds
    };

    private AsyncSignatureInfo AnalyzeInternal(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;

        // --- async void is the only case where method.IsAsync is essential ---
        if (methodSymbol.IsAsync && returnType.SpecialType == SpecialType.System_Void)
        {
            return new AsyncSignatureInfo { IsAsync = true, IsInvalidAsyncVoid = true };
        }

        if (returnType is not INamedTypeSymbol namedReturnType)
        {
            // Not a named type, so it cannot be Task/ValueTask etc.
            return new AsyncSignatureInfo { IsAsync = false, IsVoidEquivalent = returnType.SpecialType == SpecialType.System_Void };
        }

        // Canonical type name via the helper
        string fullTypeName = _typeHelper.BuildFullTypeName(namedReturnType.OriginalDefinition);

        // --- Async types ---
        if (fullTypeName == TaskFullName || fullTypeName == ValueTaskFullName)
        {
            return new AsyncSignatureInfo { IsAsync = true, IsVoidEquivalent = true };
        }

        if (fullTypeName == TaskOfTFullName || fullTypeName == ValueTaskOfTFullName)
        {
            var typeArgument = namedReturnType.TypeArguments.FirstOrDefault();
            if (typeArgument is INamedTypeSymbol argType && _typeHelper.BuildFullTypeName(argType) == BoolFullName)
            {
                // Guards must be ValueTask<bool>, not Task<bool>
                bool isInvalidGuard = fullTypeName == TaskOfTFullName;
                return new AsyncSignatureInfo
                {
                    IsAsync = true,
                    IsBoolEquivalent = true,
                    IsInvalidGuardTask = isInvalidGuard
                };
            }
        }

        // --- Sync types ---
        string syncFullTypeName = _typeHelper.BuildFullTypeName(namedReturnType);
        if (syncFullTypeName == VoidFullName)
        {
            return new AsyncSignatureInfo { IsAsync = false, IsVoidEquivalent = true };
        }

        if (syncFullTypeName == BoolFullName)
        {
            return new AsyncSignatureInfo { IsAsync = false, IsBoolEquivalent = true };
        }

        // Unsupported signature by default
        return default;
    }

    private bool IsTaskBool(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol namedType) return false;

        string fullTypeName = _typeHelper.BuildFullTypeName(namedType.OriginalDefinition);
        if (fullTypeName != TaskOfTFullName) return false;

        var typeArgument = namedType.TypeArguments.FirstOrDefault();
        return typeArgument is INamedTypeSymbol argType &&
               _typeHelper.BuildFullTypeName(argType) == BoolFullName;
    }

    /// <summary>
    /// Clears the analysis cache. Useful in tests.
    /// </summary>
    public void ClearCache() => _cache.Clear();
}