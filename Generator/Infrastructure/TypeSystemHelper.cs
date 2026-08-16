
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;


namespace Generator.Infrastructure;

/// <summary>
/// Provides centralized, testable operations for type system manipulation in code generation.
/// </summary>
internal class TypeSystemHelper
{
    private  INamedTypeSymbol? _taskSymbol;
    private  INamedTypeSymbol? _taskOfTSymbol;
    private  INamedTypeSymbol? _valueTaskSymbol;
    private  INamedTypeSymbol? _valueTaskOfTSymbol;


    internal TypeSystemHelper()
    {


        
    }
    
    /// <summary>
    /// Returns the fully qualified name for HistoryMode type.
    /// </summary>
    public string GetHistoryModeTypeName()
    {
        return "Abstractions.Attributes.HistoryMode";
    }

    /// <summary>
    /// Returns <c>true</c> if symbol represents <see cref="System.Threading.CancellationToken"/>.
    /// Does this in a way that is resistant to:
    /// • brak referencji do System.Private.CoreLib (GetTypeByMetadataName zwraca <c>null</c>)  
    /// • "retargeting assemblies" (same type from two different compilations)  
    /// </summary>
    public bool IsCancellationToken(ITypeSymbol typeSymbol, Compilation compilation)
    {
        if (typeSymbol is null)
            return false;

        // 1) Try to compare with canonical-symbol from this compilation
        var ctCanonical = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        if (ctCanonical is not null &&
            SymbolEqualityComparer.Default.Equals(typeSymbol, ctCanonical))
            return true;

        // 2) Fallback: comparison by name + namespace
        //    (potrzebne gdy ctCanonical == null lub gdy mamy retargetowany symbol)
        string ns = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "";
        return typeSymbol.Name == "CancellationToken" && ns == "System.Threading";
    }


    /// <summary>
    /// Sprawdza czy zwracany typ jest:
    ///   * Task
    ///   * ValueTask
    ///   * Task&lt;bool&gt;
    ///   * ValueTask&lt;bool&gt;
    /// </summary>
    public (bool isAsync, bool isBoolEquivalent) AnalyzeAwaitable(ITypeSymbol returnType, Compilation compilation)
    {
        _taskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        _taskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        _valueTaskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        _valueTaskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        
        if (_taskSymbol != null && SymbolEqualityComparer.Default.Equals(returnType, _taskSymbol))
            return (true, false);

        if (_valueTaskSymbol != null && SymbolEqualityComparer.Default.Equals(returnType, _valueTaskSymbol))
            return (true, false);

        if (returnType is INamedTypeSymbol named &&
            named.TypeArguments.Length == 1)
        {
            // otwarta definicja generyka: Task<T> / ValueTask<T>
            var open = named.ConstructedFrom;

            if ((_taskOfTSymbol != null && SymbolEqualityComparer.Default.Equals(open, _taskOfTSymbol)) ||
                (_valueTaskOfTSymbol != null && SymbolEqualityComparer.Default.Equals(open, _valueTaskOfTSymbol)))
            {
                bool boolLike = named.TypeArguments[0].SpecialType == SpecialType.System_Boolean;
                return (true, boolLike);
            }
        }

        return (false, false);
    }
    private readonly HashSet<string> _csharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
        "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
        "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "value", "async", "await",
        "dynamic", "partial", "yield", "var", "when", "where", "add", "remove", "get", "set", "global",
        "alias", "ascending", "descending", "from", "group", "into", "join", "let", "orderby",
        "select", "where"
    ];

    private readonly Dictionary<string, string> _typeAliases = new()
    {
        { "System.String", "string" },
        { "System.Int32", "int" },
        { "System.Int64", "long" },
        { "System.Boolean", "bool" },
        { "System.Byte", "byte" },
        { "System.Char", "char" },
        { "System.Decimal", "decimal" },
        { "System.Double", "double" },
        { "System.Single", "float" },
        { "System.Object", "object" },
        { "System.Void", "void" },
        { "System.UInt32", "uint" },
        { "System.UInt64", "ulong" },
        { "System.Int16", "short" },
        { "System.UInt16", "ushort" },
        { "System.SByte", "sbyte" }
    };


    private static readonly Regex _arraySuffixRegex =
        new(@"(\[\s*(,\s*)*\])+$", RegexOptions.Compiled);

    /// <summary>
    /// Formats fully qualified type name for use in generated code:
    /// • stosuje aliasy (string, int, …),
    /// • simplifies CLR generics and C# friendly syntax,
    /// • zachowuje (opcjonalnie) prefiks global::,
    /// • correctly handles nested types, arrays and nullable.
    /// Metoda jest zgodna z netstandard2.0 (bez System.Range itp.).
    /// </summary>
    public string FormatTypeForUsage(string fullyQualifiedTypeName,
        bool useGlobalPrefix = false)
    {
        if (string.IsNullOrEmpty(fullyQualifiedTypeName))
            return "object";

        // ---------- TABLICE --------------------------------------------------
        Match arrayMatch = _arraySuffixRegex.Match(fullyQualifiedTypeName);
        if (arrayMatch.Success)
        {
            string elementType = fullyQualifiedTypeName.Substring(0, arrayMatch.Index);
            string suffix = arrayMatch.Value;                   // "[]", "[,]", "[][]" …

            // Rekurencyjnie formatuj element + doklej oryginalny sufiks.
            return FormatTypeForUsage(elementType, useGlobalPrefix) + suffix;
        }

        // ---------- NULLABLE -------------------------------------------------
        if (fullyQualifiedTypeName.Length > 1 &&
            fullyQualifiedTypeName[fullyQualifiedTypeName.Length - 1] == '?')
        {
            string elementType = fullyQualifiedTypeName.Substring(
                0, fullyQualifiedTypeName.Length - 1);

            return FormatTypeForUsage(elementType, useGlobalPrefix) + "?";
        }

        // ---------- GENERYKI (CLR lub przyjazne) ----------------------------
        if (IsGenericType(fullyQualifiedTypeName))
            return FormatGenericType(fullyQualifiedTypeName, useGlobalPrefix);

        // ---------- NAZWA NIE-GENERYCZNA ------------------------------------
        string typeName = fullyQualifiedTypeName.Replace("+", ".");

        if (typeName.StartsWith(Strings.GlobalNamespace, StringComparison.Ordinal))
            typeName = typeName.Substring(8);

        string alias;
        if (_typeAliases.TryGetValue(typeName, out alias))
            return alias;

        if (IsNestedType(fullyQualifiedTypeName))
            return useGlobalPrefix ? Strings.GlobalNamespace + typeName : typeName;

        string simpleName = GetSimpleTypeName(typeName);
        return useGlobalPrefix ? Strings.GlobalNamespace + typeName : simpleName;
    }


    /// <summary>
    /// Gets the namespace from a fully qualified type name
    /// </summary>
    public string? GetNamespace(string fullyQualifiedTypeName)
    {
        if (string.IsNullOrEmpty(fullyQualifiedTypeName))
            return null;

        var typeName = RemoveGlobalPrefix(fullyQualifiedTypeName);

        // Handle generic types
        if (IsGenericType(typeName))
        {
            var genericStart = typeName.IndexOfAny(['<', '`']);
            if (genericStart > 0)
                typeName = typeName.Substring(0, genericStart);
        }

        // Handle nested types - namespace ends before the first '+'
        if (typeName.Contains('+'))
        {
            var plusIndex = typeName.IndexOf('+');
            typeName = typeName.Substring(0, plusIndex);
        }

        var lastDot = typeName.LastIndexOf('.');
        return lastDot > 0 ? typeName.Substring(0, lastDot) : null;
    }

    internal string GetSimpleTypeName(string fullyQualifiedTypeName)
    {
        if (string.IsNullOrEmpty(fullyQualifiedTypeName))
            return "object";

        string typeName = RemoveGlobalPrefix(fullyQualifiedTypeName);

        // cut off generic part (<…> or `n)
        int genericMarker = typeName.IndexOfAny(new[] { '<', '`' });
        if (genericMarker > 0)
            typeName = typeName.Substring(0, genericMarker);

        // ---------- nested types handling -------------------------------
        int plusFirst = typeName.IndexOf('+');
        if (plusFirst >= 0)
        {
            // policz plusy
            int plusCount = 1;
            for (int i = plusFirst + 1; i < typeName.Length; i++)
                if (typeName[i] == '+') plusCount++;

            int lastDotBeforePlus = typeName.LastIndexOf('.', plusFirst);
            int start;

            if (plusCount == 1)
            {
                // preserve entire namespace (if exists)
                start = 0;
            }
            else
            {
                // ≥2 plus signs → preserve at most the last namespace segment
                if (lastDotBeforePlus >= 0)
                {
                    int prevDot = typeName.LastIndexOf('.', lastDotBeforePlus - 1);
                    start = prevDot >= 0 ? prevDot + 1 : lastDotBeforePlus + 1;
                }
                else
                {
                    // brak namespace’u
                    start = 0;
                }
            }

            string nestedPath = typeName.Substring(start).Replace('+', '.');
            return nestedPath;
        }

        // ---------- non-nested types ---------------------------------------
        int lastDot = typeName.LastIndexOf('.');
        return lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
    }


    /// <summary>
    /// Determines if a type is nested (contains another type)
    /// </summary>
    public bool IsNestedType(string fullyQualifiedTypeName)
    {
        return !string.IsNullOrEmpty(fullyQualifiedTypeName) &&
               // Check for '+' which indicates nested type
               fullyQualifiedTypeName.Contains('+');
    }

    /// <summary>
    /// Determines if a type is generic
    /// </summary>
    public bool IsGenericType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        return typeName.Contains('`') || typeName.Contains('<');
    }

    /// <summary>
    /// Escapes C# keywords with @ prefix
    /// </summary>
    public string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return identifier;

        return _csharpKeywords.Contains(identifier) ? $"@{identifier}" : identifier;
    }

    /// <summary>
    /// Builds a fully qualified type name from a type symbol, preserving nested type format
    /// </summary>
    public string BuildFullTypeName(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            throw new ArgumentNullException(nameof(typeSymbol));

        // Handle generic types
        if (typeSymbol.IsGenericType)
        {
            return BuildGenericTypeName(typeSymbol);
        }

        // Build type hierarchy for nested types
        var parts = new List<string> { typeSymbol.Name };

        var current = typeSymbol.ContainingType;
        while (current != null)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingType;
        }

        // Join with '+' for nested types
        var typePath = string.Join("+", parts);

        // Add namespace if exists
        var ns = typeSymbol.ContainingNamespace;
        if (ns is { IsGlobalNamespace: false })
        {
            return $"{ns.ToDisplayString()}.{typePath}";
        }

        return typePath;
    }

    /// <summary>
    /// Returns namespaces that should be added in using statements,
    /// to correctly use the given type in generated code.
    /// </summary>
    public IEnumerable<string> GetRequiredNamespaces(string fullyQualifiedTypeName)
    {
        if (string.IsNullOrEmpty(fullyQualifiedTypeName))
            yield break;

        // ----------------- 1) standalone nested type -----------------
        // If name contains '+' (nesting) and is NOT generic,
        // skip — can't meaningfully import such type.
        if (IsNestedType(fullyQualifiedTypeName) && !IsGenericType(fullyQualifiedTypeName))
            yield break;

        // ----------------- 2) main type namespace ----------------------
        var ns = GetNamespace(fullyQualifiedTypeName);
        if (!string.IsNullOrEmpty(ns))
            yield return ns;

        // ----------------- 3) generic argument namespaces ----------
        if (IsGenericType(fullyQualifiedTypeName))
        {
            foreach (var argNs in ExtractGenericArgumentNamespaces(fullyQualifiedTypeName))
            {
                if (!string.IsNullOrEmpty(argNs))
                    yield return argNs;
            }
        }
    }



    /// <summary>
    /// Formats type name for use in typeof(…) expression.
    /// • For CLR generic definitions (List`1, Dictionary`2, …) returns open
    ///   definition in form of List&lt;&gt;, Dictionary&lt;,&gt; etc.
    /// • For types having namespace (".") or nesting ("+") adds "global::".
    /// • For bare names (without dot or plus) leaves name without prefix,
    ///   so that types from current namespace can be used.
    /// </summary>
    public string FormatForTypeof(string fullyQualifiedTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullyQualifiedTypeName))
            return "object";

        string raw = RemoveGlobalPrefix(fullyQualifiedTypeName);

        // ---------- 1) CLR definitions with `n (e.g. List`1) -----------------
        if (IsGenericType(raw) && raw.IndexOf('`') >= 0)
        {
            var backtick = raw.IndexOf('`');
            var baseName = raw.Substring(0, backtick);
            int argCount = ExtractGenericArgumentCount(raw);
            var commas = new string(',', Math.Max(argCount - 1, 0));
            return $"{baseName}<{commas}>";
        }

        // ---------- 2) friendly generic notation with '<' '>' --------------
        int open = raw.IndexOf('<');
        if (open >= 0)
        {
            int close = raw.LastIndexOf('>');
            if (close > open + 1)
            {
                string baseName = raw.Substring(0, open);
                string argsSection = raw.Substring(open + 1, close - open - 1);

                IEnumerable<string> argList;
                try
                {
                    argList = SplitGenericArguments(argsSection);
                }
                catch { return raw; }

                string[] formattedArgs = argList
                    .Select(a => FormatTypeForUsage(a.Trim())) // aliases OK
                    .ToArray();

                string baseNorm = baseName.Replace('+', '.');
                bool needsGlobal = baseName.IndexOf('.') >= 0 ||
                                   baseName.IndexOf('+') >= 0;

                string result = baseNorm +
                                "<" +
                                string.Join(", ", formattedArgs) +
                                ">";

                return needsGlobal ? Strings.GlobalNamespace + result
                    : result;
            }
        }

        // ---------- 3) non-generic type ------------------------------
        bool needsGlobalSimple = raw.IndexOf('.') >= 0 || raw.IndexOf('+') >= 0;
        string simple = raw.Replace('+', '.');
        return needsGlobalSimple ? Strings.GlobalNamespace + simple : simple;
    }




    #region Private Helper Methods

    private string RemoveGlobalPrefix(string typeName)
    {
        if (typeName.StartsWith(Strings.GlobalNamespace, StringComparison.Ordinal))
            return typeName.Substring(8);
        return typeName;
    }

    private string FormatGenericType(string genericTypeName, bool useGlobalPrefix)
    {
        // Handle CLR format: System.Collections.Generic.List`1[[System.String, mscorlib]]
        if (genericTypeName.Contains('`'))
        {
            return ConvertClrGenericToFriendly(genericTypeName, useGlobalPrefix);
        }

        // Handle friendly format: List<string>
        return ProcessFriendlyGenericType(genericTypeName, useGlobalPrefix);
    }

    /// <summary>
    /// Konwertuje zapis CLR (np. Dictionary`2[[System.String],[System.Int32]])
    /// to friendly C# (Dictionary&lt;string, int&gt;), handling nested
    /// generyki i kwalifikatory assembly.
    /// </summary>
    private string ConvertClrGenericToFriendly(string clrTypeName, bool useGlobalPrefix)
    {
        if (string.IsNullOrEmpty(clrTypeName))
            return "object";

        // ---- base name (part before `n) ------------------------------------
        int backtick = clrTypeName.IndexOf('`');
        if (backtick < 0)
            return FormatTypeForUsage(clrTypeName, useGlobalPrefix);

        string baseRaw = clrTypeName.Substring(0, backtick); // full name with NS
        string baseSimple = GetSimpleTypeName(baseRaw);         // np. „Event”

        // ---- building name with global:: prefix (if needed) ------------------
        string baseNoGlobal = RemoveGlobalPrefix(baseRaw);
        string baseWithPrefix = (useGlobalPrefix && !_typeAliases.ContainsValue(baseSimple))
            ? Strings.GlobalNamespace + baseNoGlobal          // global::MyNs.Event
            : baseSimple;                        // Event

        // ---- segment z argumentami [[...],[...]] ------------------------------
        int firstBracket = clrTypeName.IndexOf('[', backtick);
        if (firstBracket < 0)          // otwarta definicja, np. List`1  -> List<>
        {
            int argCount = ExtractGenericArgumentCount(clrTypeName);
            string commas = new string(',', Math.Max(argCount - 1, 0));  // "" / "," / ",," …
            return $"{baseWithPrefix}<{commas}>";
        }

        string argsSegment = clrTypeName.Substring(firstBracket);

        // ---- split into individual type specifications ------------------------
        List<string> argSpecs = ExtractClrGenericArguments(argsSegment);

        string[] formattedArgs = argSpecs
            .Select(spec => FormatTypeForUsage(RemoveAssemblyQualifier(spec), useGlobalPrefix))
            .ToArray();

        // ---- rezultat ---------------------------------------------------------
        return $"{baseWithPrefix}<{string.Join(", ", formattedArgs)}>";
    }


    /// Removes part ", Assembly.Name, Version=..." from CLR type notation.
    private static string RemoveAssemblyQualifier(string typeSpec)
    {
        int depth = 0;
        for (int i = 0; i < typeSpec.Length; i++)
        {
            char c = typeSpec[i];
            switch (c)
            {
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    return typeSpec.Substring(0, i).Trim();
            }
        }
        return typeSpec.Trim();
    }

    /// Extracts list of raw arguments from sequence "[[T1],[T2], ...]".
    private static List<string> ExtractClrGenericArguments(string segment)
    {
        var result = new List<string>();
        int depth = 0;
        int argStart = -1;

        for (int i = 0; i < segment.Length; i++)
        {
            char c = segment[i];

            switch (c)
            {
                case '[':
                {
                    depth++;
                    if (depth == 2)
                        argStart = i + 1;          // beginning of single argument
                    break;
                }
                case ']':
                {
                    if (depth == 2 && argStart >= 0)
                    {
                        string spec = segment.Substring(argStart, i - argStart);
                        result.Add(spec);
                        argStart = -1;
                    }
                    depth--;
                    break;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Przetwarza przyjazny (C#) zapis generyka, np.
    ///     "Namespace.Event&lt;string, List&lt;int&gt;&gt;"
    /// and returns formatted name ready for insertion into code.
    /// </summary>
    private string ProcessFriendlyGenericType(string friendlyTypeName,
        bool useGlobalPrefix)
    {
        // 1. Initial validation
        if (string.IsNullOrEmpty(friendlyTypeName))
            return "object";

        int open = friendlyTypeName.IndexOf('<');
        if (open < 0)                               // nie-generyk
            return FormatTypeForUsage(friendlyTypeName, useGlobalPrefix);

        int close = friendlyTypeName.LastIndexOf('>');
        if (close < 0 || close <= open + 1)         // brak '>' lub puste <>
            return friendlyTypeName;

        // 2. Split into base + argument list
        string baseRaw = friendlyTypeName.Substring(0, open);
        string argsSection = friendlyTypeName.Substring(open + 1,
            close - open - 1);

        IEnumerable<string> argList;
        try
        {
            argList = SplitGenericArguments(argsSection);
        }
        catch   // incorrect syntax – return original
        {
            return friendlyTypeName;
        }

        // 3. Recursive argument formatting
        string[] formattedArgs = argList
            .Select(a => FormatTypeForUsage(a.Trim(), useGlobalPrefix))
            .ToArray();

        // 4. Budowa nazwy bazowej
        string baseNoGlobal = RemoveGlobalPrefix(baseRaw);
        string baseWithPrefix;

        if (useGlobalPrefix)
        {
            // with "global::" prefix, preserve full path
            baseWithPrefix = Strings.GlobalNamespace + baseNoGlobal.Replace('+', '.');
        }
        else
        {
            baseWithPrefix = baseNoGlobal.IndexOf('+') >= 0 ? baseNoGlobal.Replace('+', '.') : // nested type
                // regular type – simple name only
                GetSimpleTypeName(baseNoGlobal);
        }

        // 5. Sklejenie wyniku
        return baseWithPrefix +
               "<" +
               string.Join(", ", formattedArgs) +
               ">";
    }


    // pomaga: dzieli "Dictionary<string, List<int>>" -> ["Dictionary<string, List<int>>"]
    private static IEnumerable<string> SplitGenericArguments(string args)
    {
        var depth = 0;
        var sb = new StringBuilder();

        foreach (var ch in args)
        {
            if (ch == ',' && depth == 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            else
            {
                switch (ch)
                {
                    case '<':
                        depth++;
                        break;
                    case '>':
                        depth--;
                        break;
                }

                sb.Append(ch);
            }
        }

        if (sb.Length > 0) yield return sb.ToString();
    }


    private int ExtractGenericArgumentCount(string genericTypeName)
    {
        var backtickIndex = genericTypeName.IndexOf('`');
        if (backtickIndex < 0) return 0;

        var afterBacktick = genericTypeName.Substring(backtickIndex + 1);
        var digits = afterBacktick.TakeWhile(char.IsDigit).ToArray();

        return int.TryParse(new string(digits), out var count) ? count : 0;
    }

    private IEnumerable<string> ExtractGenericArgumentNamespaces(string genericTypeName)
    {
        // -------- 1) przyjazny zapis C# (List<string, int>) -----------------------
        if (genericTypeName.Contains('<') && genericTypeName.Contains('>'))
        {
            var start = genericTypeName.IndexOf('<');
            var end = genericTypeName.LastIndexOf('>');
            if (start < end)
            {
                var args = genericTypeName.Substring(start + 1, end - start - 1);

                // UWAGA: uproszczone splitowanie po przecinku 1-go poziomu
                foreach (var arg in args.Split(','))
                {
                    var ns = GetNamespace(arg.Trim());
                    if (!string.IsNullOrEmpty(ns))
                        yield return ns;
                }
            }
        }
        // -------- 2) zapis CLR (Dictionary`2[[System.String, mscorlib],[...]] ) ----
        else if (genericTypeName.IndexOf('`') >= 0 && genericTypeName.Contains("[["))
        {
            // find segment starting from first '[' after suffix `n
            var backtick = genericTypeName.IndexOf('`');
            var firstBracket = genericTypeName.IndexOf('[', backtick);
            if (firstBracket >= 0)
            {
                foreach (var spec in ExtractClrGenericArguments(
                             genericTypeName.Substring(firstBracket)))
                {
                    var cleaned = RemoveAssemblyQualifier(spec);
                    var ns = GetNamespace(cleaned);
                    if (!string.IsNullOrEmpty(ns))
                        yield return ns;
                }
            }
        }
    }

    private string BuildGenericTypeName(INamedTypeSymbol typeSymbol)
    {
        // ------------------- nazwa definicji generyka (List`1, Dictionary`2 …) -----
        var definition = typeSymbol.OriginalDefinition;   // otwarta definicja

        // Composing path for nested types: Outer+Inner`1
        var parts = new List<string> { definition.Name };   // nazwa z sufiksem `n
        var current = definition.ContainingType;

        while (current != null)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingType;
        }

        var basePath = string.Join("+", parts);

        // Namespace (if exists)
        var ns = definition.ContainingNamespace;
        if (ns is { IsGlobalNamespace: false })
            basePath = ns.ToDisplayString() + "." + basePath;

        // Usuwamy sufiks `n
        var backtick = basePath.IndexOf('`');
        if (backtick >= 0)
            basePath = basePath.Substring(0, backtick);

        // ------------------- argumenty typu ---------------------------------------
        var argNames = typeSymbol.TypeArguments
            .Select(arg =>
                arg is INamedTypeSymbol nts
                    ? BuildFullTypeName(nts)          // no recursion loop here
                    : arg.ToDisplayString())
            .ToArray();

        return basePath + "<" + string.Join(", ", argNames) + ">";
    }
    /// <summary>
    /// Builds a fully qualified type name from any type symbol.
    /// Handles INamedTypeSymbol, arrays, pointers, and other type kinds.
    /// </summary>
    public string BuildFullTypeName(ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            throw new ArgumentNullException(nameof(typeSymbol));

        // If it's a named type, use the existing method
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            return BuildFullTypeName(namedType);
        }

        // For arrays, pointers, and other special types, use display string
        // This gives us the fully qualified name in a format like:
        // System.String[] for arrays
        // System.Int32* for pointers
        // etc.
        return typeSymbol.ToDisplayString(new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.None,
            delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
            parameterOptions: SymbolDisplayParameterOptions.None,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            localOptions: SymbolDisplayLocalOptions.None,
            kindOptions: SymbolDisplayKindOptions.None,
            miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes | // Use int instead of Int32
            SymbolDisplayMiscellaneousOptions.ExpandNullable    // int? instead of Nullable<int>
        ));
    }

    #endregion
}
