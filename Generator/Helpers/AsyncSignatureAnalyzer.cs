using System.Collections.Concurrent;
using System.Linq;
using Generator.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Generator.Helpers;

/// <summary>
/// Analizuje symbol metody (IMethodSymbol) i określa jej charakterystykę asynchroniczną.
/// Działa w izolacji, co ułatwia testowanie.
/// </summary>
public sealed class AsyncSignatureAnalyzer
{
    private readonly TypeSystemHelper _typeHelper;
    private readonly ConcurrentDictionary<IMethodSymbol, AsyncSignatureInfo> _cache = new(SymbolEqualityComparer.Default);

    // Pełne nazwy typów Task/ValueTask, które będziemy sprawdzać
    private const string TaskFullName = "System.Threading.Tasks.Task";
    private const string ValueTaskFullName = "System.Threading.Tasks.ValueTask";
    private const string TaskOfTFullName = "System.Threading.Tasks.Task`1";
    private const string ValueTaskOfTFullName = "System.Threading.Tasks.ValueTask`1";

    // Nazwy typów bool i void dla porównań
    private const string BoolFullName = "System.Boolean";
    private const string VoidFullName = "System.Void";

    public AsyncSignatureAnalyzer(TypeSystemHelper typeHelper)
    {
        _typeHelper = typeHelper;
    }


    /// <summary>
    /// Analizuje sygnaturę metody z cache'owaniem wyników.
    /// </summary>
    public AsyncSignatureInfo Analyze(IMethodSymbol method)
    {
        var (isAsync, isBoolEquivalent) =
            _typeHelper.AnalyzeAwaitable(method.ReturnType);

        return new AsyncSignatureInfo
        {
            IsAsync = isAsync,
            IsBoolEquivalent = isBoolEquivalent,
        };
    }

    /// <summary>
    /// Analizuje sygnaturę metody z dodatkową walidacją dla konkretnego typu callbacku.
    /// </summary>
    public AsyncSignatureInfo AnalyzeCallback(IMethodSymbol methodSymbol, string callbackType)
    {
        var info = Analyze(methodSymbol);

        // Dodatkowa walidacja per typ callbacku
        if (callbackType == "Guard" && info.IsAsync)
        {
            // Guard musi zwracać ValueTask<bool>, nie Task<bool>
            if (info.IsBoolEquivalent && IsTaskBool(methodSymbol.ReturnType))
            {
                info.IsInvalidGuardTask = true;
            }
        }

        return info;
    }

    /// <summary>
    /// Sprawdza czy sygnatura metody jest poprawna dla danego typu callbacku.
    /// </summary>
    public bool IsValidForCallback(IMethodSymbol methodSymbol, string callbackType)
    {
        var info = AnalyzeCallback(methodSymbol, callbackType);

        // Sprawdzenie podstawowej poprawności
        bool isValid = callbackType switch
        {
            "Guard" => info.IsBoolEquivalent && !info.IsInvalidAsyncVoid && !info.IsInvalidGuardTask,
            "Action" or "OnEntry" or "OnExit" => info.IsVoidEquivalent && !info.IsInvalidAsyncVoid,
            _ => false
        };

        return isValid;
    }

    /// <summary>
    /// Zwraca oczekiwany typ zwracany dla danego typu callbacku i trybu.
    /// </summary>
    public string GetExpectedReturnType(string callbackType, bool isAsync)
    {
        return (callbackType, isAsync) switch
        {
            ("Guard", false) => "bool",
            ("Guard", true) => "ValueTask<bool>",
            ("Action", false) => "void",
            ("Action", true) => "Task or ValueTask",
            ("OnEntry", false) => "void",
            ("OnEntry", true) => "Task or ValueTask",
            ("OnExit", false) => "void",
            ("OnExit", true) => "Task or ValueTask",
            _ => "void" // domyślnie dla nieznanych typów callbacków
        };
    }

    private AsyncSignatureInfo AnalyzeInternal(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;

        // --- Analiza dla `async void` - to jedyny przypadek, gdzie `IsAsync` jest kluczowe ---
        if (methodSymbol.IsAsync && returnType.SpecialType == SpecialType.System_Void)
        {
            return new AsyncSignatureInfo { IsAsync = true, IsInvalidAsyncVoid = true };
        }

        if (returnType is not INamedTypeSymbol namedReturnType)
        {
            // Nie jest to nazwany typ, więc nie może być Task/ValueTask etc.
            return new AsyncSignatureInfo { IsAsync = false, IsVoidEquivalent = returnType.SpecialType == SpecialType.System_Void };
        }

        // Używamy helpera do uzyskania kanonicznej nazwy typu
        string fullTypeName = _typeHelper.BuildFullTypeName(namedReturnType.OriginalDefinition);

        // --- Analiza typów asynchronicznych ---
        if (fullTypeName == TaskFullName || fullTypeName == ValueTaskFullName)
        {
            return new AsyncSignatureInfo { IsAsync = true, IsVoidEquivalent = true };
        }

        if (fullTypeName == TaskOfTFullName || fullTypeName == ValueTaskOfTFullName)
        {
            var typeArgument = namedReturnType.TypeArguments.FirstOrDefault();
            if (typeArgument is INamedTypeSymbol argType && _typeHelper.BuildFullTypeName(argType) == BoolFullName)
            {
                // Guard musi być ValueTask<bool>, a nie Task<bool>
                bool isInvalidGuard = fullTypeName == TaskOfTFullName;
                return new AsyncSignatureInfo
                {
                    IsAsync = true,
                    IsBoolEquivalent = true,
                    IsInvalidGuardTask = isInvalidGuard
                };
            }
        }

        // --- Analiza typów synchronicznych ---
        string syncFullTypeName = _typeHelper.BuildFullTypeName(namedReturnType);
        if (syncFullTypeName == VoidFullName)
        {
            return new AsyncSignatureInfo { IsAsync = false, IsVoidEquivalent = true };
        }

        if (syncFullTypeName == BoolFullName)
        {
            return new AsyncSignatureInfo { IsAsync = false, IsBoolEquivalent = true };
        }

        // Domyślnie sygnatura jest nieobsługiwana
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
    /// Czyści cache analizy. Użyteczne w testach.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}