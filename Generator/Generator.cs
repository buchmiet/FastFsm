using Generator.DependencyInjection;
using Generator.Helpers;
using Generator.Model;
using Generator.Parsers;
using Generator.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static Generator.Strings;

namespace Generator;

[Generator]
public class StateMachineGenerator : IIncrementalGenerator
{
    // Track all added sources for index generation (removed static to avoid cross-invocation issues)
    
    // Diagnostic descriptors for discovery logging
    private static readonly DiagnosticDescriptor FSM998_CandidateFound = new(
        "FSM998",
        "State machine candidate found",
        "Discovered [StateMachine]: {0} in {1}",
        "FSM.Generator.Discovery",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor FSM997_SkippedCandidate = new(
        "FSM997",
        "State machine candidate skipped",
        "Skipped state machine candidate {0}: {1}",
        "FSM.Generator.Discovery",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor FSM996_AddSourceOk = new(
        "FSM996",
        "AddSource succeeded",
        "AddSource ok: {0} (len={1})",
        "FSM.Generator.AddSource",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor FSM994_EnumOnlyFallback = new(
        "FSM994",
        "Enum-only states fallback",
        "Enum-only states fallback applied for '{0}' — 0 [State] attributes found; using all enum members as states",
        "FSM.Generator",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor FSM995_MSBuildProps = new(
        "FSM995",
        "MSBuild analyzer properties",
        "EmitCompilerGeneratedFiles={0}; CompilerGeneratedFilesOutputPath={1}",
        "FSM.Generator.Config",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor FSM993_EmptyCode = new(
        "FSM993",
        "Empty code generated",
        "EMPTY_CODE for {0}; variant={1}; states={2}; transitions int={3}, ext={4}; payloads={5}; enumFallback={6}",
        "FSM.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor FSM992_DeclarationPlan = new(
        "FSM992",
        "Declaration plan",
        "DECLARATION_PLAN for {0}: ns='{1}', nesting='{2}', class='{3}', accessibility='{4}', partial={5}",
        "FSM.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor FSM991_Variant = new(
        "FSM991",
        "Variant decision",
        "{0} -> variant={1}; internalOnly={2}; payloadPresent={3}",
        "FSM.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor FSM989_ConfigSections = new(
        "FSM989",
        "Configuration sections",
        "{0} - StatesFrom: {1} | TransitionsFrom: {2} (ext={3}) | InternalFrom: {4} (int={5}) | PayloadTypes: {6}",
        "FSM.Generator.Parser",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);
    private static readonly HashSet<string> FsmAttrFullNames =
    [
        TransitionAttributeFullName,
        InternalTransitionAttributeFullName,
        StateAttributeFullName,
        PayloadTypeAttributeFullName
    ];

    private static string SanitizeHintName(string raw)
    {
        // Replace invalid filename chars and whitespace with '_'
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
            sb.Append(char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' ? ch : '_');
        return sb.ToString();
    }
    
    private static string GetUniqueHintName(string baseName, HashSet<string> usedNames)
    {
        var sanitized = SanitizeHintName(baseName);
        var hint = $"{sanitized}.Generated.cs";
        
        if (usedNames.Add(hint))
            return hint;
        
        // Add suffix if collision
        int i = 2;
        while (true)
        {
            hint = $"{sanitized}_{i}.Generated.cs";
            if (usedNames.Add(hint))
                return hint;
            i++;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        // Following Roslyn incremental generator best practices:
        // Never use .Where() after discovery - carry all candidates through as Result union
        // See: https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
        
        // 1) Use broad syntax predicate to find all classes with attributes
        var candidates = ctx.SyntaxProvider.CreateSyntaxProvider(
            static (node, ct) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
            static (ctx, ct) => 
            {
                var classDecl = (ClassDeclarationSyntax)ctx.Node;
                var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;
                return (Class: classDecl, Symbol: symbol);
            });

        // 2) Resolve StateMachine attribute symbol with multiple fallbacks
        var attributeSymbol = ctx.CompilationProvider.Select((comp, ct) => 
        {
            var names = new[]
            {
                "Abstractions.Attributes.StateMachineAttribute",
                "Abstractions.StateMachineAttribute",
                "FastFsm.Abstractions.Attributes.StateMachineAttribute",
                "StateMachineAttribute" // last resort (no namespace)
            };
            
            INamedTypeSymbol? attrSym = null;
            foreach (var name in names)
            {
                var s = comp.GetTypeByMetadataName(name);
                if (s is not null)
                {
                    attrSym = s;
                    break;
                }
            }
            return attrSym;
        });

        // 3) Combine candidates with attribute symbol and determine if they're state machines
        // IMPORTANT: We don't filter here - we map to CandidateResult to preserve visibility
        var candidateResults = candidates
            .Combine(attributeSymbol)
            .Select((tuple, ct) => 
            {
                var ((classDecl, sym), attrSym) = tuple;
                
                if (sym is null)
                    return CandidateResult.Skipped(classDecl, sym!, "Symbol not resolved");
                
                // (a) Exact symbol match when we have the attribute symbol
                bool hasBySymbol = attrSym is not null && sym.GetAttributes().Any(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSym));
                
                // (b) Fallback: match by simple name
                bool hasByName = sym.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "StateMachineAttribute");
                
                bool isStateMachine = hasBySymbol || hasByName;
                
                if (!isStateMachine)
                    return CandidateResult.Skipped(classDecl, sym, "No [StateMachine] attribute");
                    
                // Check if partial
                if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    return CandidateResult.Skipped(classDecl, sym, "Class is not partial");
                
                // At this point we have a candidate to parse - use a special marker
                // We can't mark it as Valid yet because model is null
                // Use empty skip reason to indicate it needs parsing
                return new CandidateResult(classDecl, sym, null, null); // Will be parsed in ProcessCandidateAndGenerate
            });

        // 4) Collect for discovery dump (before parsing)
        var collectedCandidates = candidateResults.Collect();
        ctx.RegisterSourceOutput(collectedCandidates, GenerateDiscoveryDumpV2);
        
        // 5) Combine with compilation and options for final generation
        var forGeneration = candidateResults
            .Combine(ctx.CompilationProvider)
            .Combine(ctx.AnalyzerConfigOptionsProvider);
        
        // 6) Register final source output - parse and generate or report skip
        ctx.RegisterSourceOutput(forGeneration, ProcessCandidateAndGenerate);
    }


    private static void GenerateDiscoveryDumpV2(
        SourceProductionContext context,
        ImmutableArray<CandidateResult> candidates)
    {
        if (candidates.IsDefaultOrEmpty)
            return;
            
        var sb = new IndentedStringBuilder.IndentedStringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// FastFSM Discovery Dump");
        sb.AppendLine("// This file lists all discovered [StateMachine] candidates");
        sb.AppendLine();
        sb.AppendLine("internal static class __FastFsm_DiscoveryDump");
        using (sb.Block(""))
        {
            var validCount = candidates.Count(c => c.IsValid);
            var skippedCount = candidates.Length - validCount;
            
            sb.AppendLine($"// Total candidates discovered: {candidates.Length}");
            sb.AppendLine($"// Valid: {validCount}, Skipped: {skippedCount}");
            sb.AppendLine("// Format: Index | FullyQualifiedName | Status | SkipReason");
            sb.AppendLine();
            
            int index = 0;
            foreach (var candidate in candidates)
            {
                var fullName = GetFullTypeName(candidate.Symbol);
                var status = candidate.IsValid ? "VALID" : "SKIPPED";
                var reason = candidate.SkipReason ?? "";
                
                sb.AppendLine($"// {index}: {fullName}");
                sb.AppendLine($"//     status={status}");
                if (!string.IsNullOrEmpty(reason))
                    sb.AppendLine($"//     reason={reason}");
                sb.AppendLine();
                index++;
            }
            
            sb.AppendLine("/*");
            sb.AppendLine(" * End of discovery dump");
            sb.AppendLine(" */");
        }
        
        context.AddSource("__FastFsm.DiscoveredMachines.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
    
    private static void ProcessCandidateAndGenerate(
        SourceProductionContext context,
        ((CandidateResult Candidate, Compilation Compilation), AnalyzerConfigOptionsProvider OptionsProvider) tuple)
    {
        var ((candidate, compilation), optionsProvider) = tuple;
        
        // Debug: Report all candidates being processed
        var debugName = GetFullTypeName(candidate.Symbol);
        if (debugName.Contains("InternalOnlyMachine") || debugName.Contains("InternalTransitionMachine") || debugName.Contains("InternalPayloadMachine"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSM989D",
                    "Debug entry",
                    "ENTERING ProcessCandidateAndGenerate for: {0}",
                    "FSM.Generator",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None,
                debugName));
        }
        
        // Report MSBuild properties for valid candidates
        // Note: Can't use static in source generator methods - they run in parallel
        if (candidate.IsValid)
        {
            var globalOptions = optionsProvider.GlobalOptions;
            globalOptions.TryGetValue("build_property.EmitCompilerGeneratedFiles", out var emitFiles);
            globalOptions.TryGetValue("build_property.CompilerGeneratedFilesOutputPath", out var outputPath);
            
            context.ReportDiagnostic(Diagnostic.Create(
                FSM995_MSBuildProps,
                Location.None,
                emitFiles ?? "(not set)",
                outputPath ?? "(not set)"));
        }
        
        var fullName = GetFullTypeName(candidate.Symbol);
        
        // Report that we're processing this candidate
        if (fullName.Contains("InternalOnlyMachine") || fullName.Contains("InternalTransitionMachine") || fullName.Contains("InternalPayloadMachine"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSM990",
                    "Processing candidate",
                    "Processing candidate: {0}",
                    "FSM.Generator",
                    DiagnosticSeverity.Info,
                    true),
                candidate.ClassDeclaration.GetLocation(),
                fullName));
        }
        
        // Handle pre-parse skipped candidates (those with a skip reason)
        if (!string.IsNullOrEmpty(candidate.SkipReason))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FSM997_SkippedCandidate,
                candidate.ClassDeclaration.GetLocation(),
                fullName,
                candidate.SkipReason));
            return;
        }
        
        // Parse the model
        StateMachineModel model;
        try
        {
            var parser = new StateMachineParser(compilation, context);
            
            // Add diagnostic reporting for internal-only machines
            Action<string>? report = null;
            if (fullName.Contains("InternalOnlyMachine") || fullName.Contains("InternalTransitionMachine") || fullName.Contains("InternalPayloadMachine"))
            {
                // Report that we're about to parse this machine
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FSM998A",
                        "Starting parse",
                        "Starting parse for: {0}",
                        "FSM.Generator.Parser",
                        DiagnosticSeverity.Warning,
                        true),
                    candidate.ClassDeclaration.GetLocation(),
                    fullName));
                    
                report = msg => context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FSM998",
                        "Parser trace",
                        "{0}",
                        "FSM.Generator.Parser",
                        DiagnosticSeverity.Warning,
                        true),
                    candidate.ClassDeclaration.GetLocation(),
                    msg));
            }
            
            var parseResult = parser.TryParse(candidate.ClassDeclaration, out model, report);
            
            if (!parseResult || model == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    FSM997_SkippedCandidate,
                    candidate.ClassDeclaration.GetLocation(),
                    fullName,
                    "Parser validation failed"));
                return;
            }
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FSM997_SkippedCandidate,
                candidate.ClassDeclaration.GetLocation(),
                fullName,
                $"Parser exception: {ex.Message}"));
            return;
        }
        
        // Generate source for valid candidates
        try
        {
            // Assert model is not null
            if (model == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    FSM997_SkippedCandidate,
                    candidate.ClassDeclaration.GetLocation(),
                    fullName,
                    "model=null at output"));
                return;
            }
            
            // Determine feature configuration (no external variant forcing)
            model.GenerationConfig.HasOnEntryExit = model.States.Values.Any(s =>
                !string.IsNullOrEmpty(s.OnEntryMethod) || !string.IsNullOrEmpty(s.OnExitMethod));
            // Extensions flag from [StateMachine(GenerateExtensibleVersion = true)]
            var smAttr = candidate.Symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == StateMachineAttributeFullName);
            if (smAttr != null)
            {
                var extArg = smAttr.NamedArguments.FirstOrDefault(na => na.Key == nameof(Abstractions.Attributes.StateMachineAttribute.GenerateExtensibleVersion));
                model.GenerationConfig.HasExtensions = extArg.Key != null && (bool)extArg.Value.Value!;
            }
            // No internal variants — unified generator gates purely on feature flags
            
            // Extract metrics for diagnostics
            int totalStates = model.States?.Count ?? 0;
            int totalTransitions = model.Transitions?.Count ?? 0;
            int internalCount = model.Transitions?.Count(t => t.IsInternal) ?? 0;
            int externalCount = model.Transitions?.Count(t => !t.IsInternal) ?? 0;
            int payloadTypesCount = model.TriggerPayloadTypes?.Count ?? 0;
            bool hasPayload = payloadTypesCount > 0 || !string.IsNullOrEmpty(model.DefaultPayloadType);
            
            // Report features summary (without variants)
            context.ReportDiagnostic(Diagnostic.Create(
                FSM991_Variant,
                candidate.ClassDeclaration.GetLocation(),
                fullName,
                $"features: payload={(model.GenerationConfig.HasPayload ? 1:0)}, ext={(model.GenerationConfig.HasExtensions ? 1:0)}, callbacks={(model.GenerationConfig.HasOnEntryExit ? 1:0)}",
                internalCount > 0 && externalCount == 0,
                hasPayload));
            
            // Get configuration
            model.GenerateLogging = BuildProperties.GetGenerateLogging(optionsProvider.GlobalOptions);
            model.GenerateDependencyInjection = BuildProperties.GetGenerateDI(optionsProvider.GlobalOptions);
            
            // Report declaration plan
            var ns = model.Namespace ?? "";
            var nesting = model.ContainerClasses != null && model.ContainerClasses.Count > 0
                ? string.Join("->", model.ContainerClasses)
                : "(none)";
            var className = model.ClassName;
            var accessibility = "public"; // We generate public partial classes
            
            context.ReportDiagnostic(Diagnostic.Create(
                FSM992_DeclarationPlan,
                candidate.ClassDeclaration.GetLocation(),
                fullName,
                ns,
                nesting,
                className,
                accessibility,
                true));
            
            // Create appropriate generator
            // FSM990_HSM_FLAG: Log at generator entry
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSM990_HSM_FLAG",
                    "HSM Flag Tracking",
                    "[3-GenEntry] {0}: HierarchyEnabled={1}, Features payload={2} ext={3} callbacks={4}",
                    "FSM.Generator",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true),
                Location.None,
                model.ClassName,
                model.HierarchyEnabled,
                model.GenerationConfig.HasPayload,
                model.GenerationConfig.HasExtensions,
                model.GenerationConfig.HasOnEntryExit));
            
            // Use flattened unified generator
            var generator = new Generator.SourceGenerators.UnifiedStateMachineGenerator(model);
            
            var source = generator.Generate();
            
            // Check if generated source is valid
            if (string.IsNullOrWhiteSpace(source) || source.Length == 0)
            {
            context.ReportDiagnostic(Diagnostic.Create(
                FSM993_EmptyCode,
                candidate.ClassDeclaration.GetLocation(),
                fullName,
                "<no-variant>",
                totalStates,
                internalCount,
                externalCount,
                payloadTypesCount,
                model.UsedEnumOnlyFallback));
                
                // Do NOT call AddSource for empty code
                return;
            }
            
            // Generate hint name - each FQN should be unique
            var fqn = candidate.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var sanitized = SanitizeHintName(fqn);
            var hintName = $"{sanitized}.Generated.cs";
            
            // Add source
            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
            
            // Report success
            context.ReportDiagnostic(Diagnostic.Create(
                FSM996_AddSourceOk,
                candidate.ClassDeclaration.GetLocation(),
                hintName,
                source.Length));
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FSM997_SkippedCandidate,
                candidate.ClassDeclaration.GetLocation(),
                fullName,
                $"Generation exception: {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private static void GenerateDiscoveryDump(
        SourceProductionContext context,
        ImmutableArray<(ClassDeclarationSyntax Class, INamedTypeSymbol Symbol)> candidates)
    {
        var sb = new IndentedStringBuilder.IndentedStringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// FastFSM Discovery Dump");
        sb.AppendLine("// This file lists all discovered [StateMachine] candidates");
        sb.AppendLine();
        sb.AppendLine("internal static class __FastFsm_DiscoveryDump");
        using (sb.Block(""))
        {
            sb.AppendLine($"// Total candidates discovered: {candidates.Length}");
            sb.AppendLine("// Format: Index | FullyQualifiedName | Assembly | FilePath");
            sb.AppendLine();
            
            for (int i = 0; i < candidates.Length; i++)
            {
                var (classDecl, symbol) = candidates[i];
                var fqn = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var assembly = symbol.ContainingAssembly?.Name ?? "<unknown>";
                var location = symbol.Locations.FirstOrDefault() ?? classDecl.GetLocation();
                var filePath = location.SourceTree?.FilePath ?? "<unknown>";
                
                sb.AppendLine($"// {i}: {fqn}");
                sb.AppendLine($"//     asm={assembly}");
                sb.AppendLine($"//     file={filePath}");
                sb.AppendLine();
                
                // Report discovery as warning for visibility
                context.ReportDiagnostic(Diagnostic.Create(
                    FSM998_CandidateFound,
                    location,
                    fqn,
                    Path.GetFileName(filePath)));
            }
            
            sb.AppendLine("/*");
            sb.AppendLine(" * End of discovery dump");
            sb.AppendLine(" */");
        }
        
        var hintName = "__FastFsm.DiscoveredMachines.g.cs";
        var content = sb.ToString();
        
        try
        {
            context.AddSource(hintName, SourceText.From(content, Encoding.UTF8));
            // Note: discovery dump is not tracked in the main index
            context.ReportDiagnostic(Diagnostic.Create(
                FSM996_AddSourceOk,
                Location.None,
                hintName,
                content.Length));
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FSM997_SkippedCandidate,
                Location.None,
                hintName,
                $"AddSource failed: {ex.GetType().Name}: {ex.Message}"));
            throw;
        }
    }

    private static (ClassDeclarationSyntax? Class, INamedTypeSymbol? Symbol) GetStateMachineClass(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        
        if (symbol == null)
            return (null, null);

        // Build full name for logging
        var fullName = GetFullTypeName(symbol);
        var location = classDeclaration.GetLocation();
        
        // Check for StateMachine attribute
        bool hasStateMachineAttr = false;
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) continue;
                var containingType = methodSymbol.ContainingType;
                
                // Check by full name or by type + namespace
                if (containingType.ToDisplayString() == StateMachineAttributeFullName ||
                    (containingType.Name == "StateMachineAttribute" && 
                     containingType.ContainingNamespace?.ToDisplayString() == "Abstractions.Attributes"))
                {
                    hasStateMachineAttr = true;
                    break;
                }
            }
            if (hasStateMachineAttr) break;
        }
        
        if (hasStateMachineAttr)
        {
            // Log candidate found
            return (classDeclaration, symbol);
        }
        
        return (null, null);
    }
    
    private static string GetFullTypeName(INamedTypeSymbol symbol)
    {
        var parts = new List<string>();
        
        // Add containing types (for nested classes)
        var containingType = symbol.ContainingType;
        while (containingType != null)
        {
            parts.Insert(0, containingType.Name);
            containingType = containingType.ContainingType;
        }
        
        // Add the class name
        parts.Add(symbol.Name);
        
        // Add namespace if present
        var ns = symbol.ContainingNamespace?.ToDisplayString();
        if (!string.IsNullOrEmpty(ns) && ns != "<global namespace>")
        {
            return $"{ns}.{string.Join(".", parts)}";
        }
        
        return string.Join(".", parts);
    }

    private static bool IsPartial(INamedTypeSymbol cls) =>
        cls.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .Any(s => s.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

    private static bool IsPotentialFsmClassWithoutAttribute(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax cds &&
        (cds.AttributeLists.Count > 0 ||
         cds.Members.OfType<MethodDeclarationSyntax>().Any(m => m.AttributeLists.Count > 0));

    private static INamedTypeSymbol? GetClassIfMissingStateMachine(GeneratorSyntaxContext ctx, CancellationToken _)
    {
        if (ctx.Node is not ClassDeclarationSyntax cds) return null;

        var cls = ctx.SemanticModel.GetDeclaredSymbol(cds)!;

        // czy ma Transition / itp. w metodach?
        bool hasFsmAttr = cls.GetMembers().OfType<IMethodSymbol>()
            .Any(m => m.GetAttributes().Any(a =>
                FsmAttrFullNames.Contains(a.AttributeClass?.ToDisplayString())));

        if (!hasFsmAttr) return null;

        bool hasStateMachineAttr = cls.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == StateMachineAttributeFullName);

        bool isPartial = IsPartial(cls);

        return (!hasStateMachineAttr || !isPartial) ? cls : null;
    }

    private static void Execute(
        SourceProductionContext context,
        (
            (Compilation Compilation, AnalyzerConfigOptionsProvider OptionsProvider) compAndOpts,
            ImmutableArray<(ClassDeclarationSyntax Class, INamedTypeSymbol Symbol)> Classes
        ) data)
    {
        // Track added sources in this execution
        var addedSources = new List<string>();
        var usedHintNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedDumpNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // First, report MSBuild properties
        var globalOptions = data.compAndOpts.OptionsProvider.GlobalOptions;
        globalOptions.TryGetValue("build_property.EmitCompilerGeneratedFiles", out var emitFiles);
        globalOptions.TryGetValue("build_property.CompilerGeneratedFilesOutputPath", out var outputPath);
        
        context.ReportDiagnostic(Diagnostic.Create(
            FSM995_MSBuildProps,
            Location.None,
            emitFiles ?? "(not set)",
            outputPath ?? "(not set)"));
        
        try
        {
            // Rozbicie krotki wejściowej
            var (compAndOpts, classes) = data;
            var (compilation, optionsProvider) = compAndOpts;

            // Nic do zrobienia
            if (classes.IsDefaultOrEmpty)
                return;

            // Parser (variant selection handled inside parser/generator logic)
            StateMachineParser parser;
            try
            {
                parser = new StateMachineParser(compilation, context);
            }
            catch
            {
                // Bez diagnostyki: kończymy cicho jeśli parser nie powstał
                return;
            }

            // Process all discovered state machine classes
            foreach (var (classDeclaration, classSymbol) in classes)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                string className = classDeclaration.Identifier.Text;
                string fullName = GetFullTypeName(classSymbol);

                // Check if class is partial
                if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FSM997_SkippedCandidate,
                        classDeclaration.GetLocation(),
                        fullName,
                        "Class is not partial"));
                    continue;
                }

                // Parsowanie definicji state machine
                var parserMessages = new System.Text.StringBuilder();
                void reportParsingError(string message) 
                { 
                    parserMessages.AppendLine(message);
                }

                StateMachineModel model;
                bool parseResult;
                
                // Transition analysis variables (need them in wider scope)
                int externalCount = 0;
                int internalCount = 0;
                bool isInternalOnly = false;
                
                try
                {
                    parseResult = parser.TryParse(classDeclaration, out model, reportParsingError);
                    if (!parseResult || model == null)
                    {
                        // Include parser messages in the diagnostic
                        var reason = parserMessages.Length > 0 
                            ? $"Parser validation failed. Last messages: {parserMessages.ToString().Replace("\r\n", " ").Replace("\n", " ").Substring(0, System.Math.Min(200, parserMessages.Length))}"
                            : "Parser validation failed";
                        
                        context.ReportDiagnostic(Diagnostic.Create(
                            FSM997_SkippedCandidate,
                            classDeclaration.GetLocation(),
                            fullName,
                            reason));
                        continue;
                    }
                    
                    // Dump the model for debugging
                    ModelDebugDumper.Dump(context, model, fullName.Replace(".", "_").Replace("::", "_"), usedDumpNames);
                    
                    // Analyze transitions
                    externalCount = model.Transitions.Count(t => !t.IsInternal);
                    internalCount = model.Transitions.Count(t => t.IsInternal);
                    isInternalOnly = internalCount > 0 && externalCount == 0;
                    
                    // Check if we have any transitions at all (including internal)
                    if (model.Transitions.Count == 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            FSM997_SkippedCandidate,
                            classDeclaration.GetLocation(),
                            fullName,
                            $"SKIP: No transitions found. REASON=No transitions defined, Summary: internalOnly=false, hasExternal=false, hasInternal=false, states={model.States?.Count ?? 0}, transitions=0"));
                        continue;
                    }
                    
                    // Log internal-only status for debugging
                    if (isInternalOnly)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            FSM998_CandidateFound,
                            classDeclaration.GetLocation(),
                            fullName,
                            $"Internal-only machine with {internalCount} internal transitions"));
                    }
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        FSM997_SkippedCandidate,
                        classDeclaration.GetLocation(),
                        fullName,
                        $"Parser exception: {ex.Message}"));
                    continue;
                }

                try
                {
                    // Use the already resolved symbol
                    var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                    // We already have classSymbol from the tuple

                    // determine feature config for model
                    model!.GenerationConfig.HasOnEntryExit = model.States.Values.Any(s =>
                        !string.IsNullOrEmpty(s.OnEntryMethod) || !string.IsNullOrEmpty(s.OnExitMethod));
                    var smAttr2 = classSymbol.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == StateMachineAttributeFullName);
                    if (smAttr2 != null)
                    {
                        var extArg2 = smAttr2.NamedArguments.FirstOrDefault(na => na.Key == nameof(Abstractions.Attributes.StateMachineAttribute.GenerateExtensibleVersion));
                        model.GenerationConfig.HasExtensions = extArg2.Key != null && (bool)extArg2.Value.Value!;
                    }
                    // No internal variants — unified generator gates purely on feature flags

                    model!.GenerateLogging = BuildProperties.GetGenerateLogging(
                        optionsProvider.GlobalOptions);

                    model!.GenerateDependencyInjection = BuildProperties.GetGenerateDI(
                        optionsProvider.GlobalOptions);

                    // 1) Główny generator — flattened unified generator obsługuje wszystkie warianty
                    var generator = new Generator.SourceGenerators.UnifiedStateMachineGenerator(model);

                    string source;
                    try
                    {
                        source = generator.Generate();
                    }
                    catch (Exception genEx)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            FSM997_SkippedCandidate,
                            classDeclaration.GetLocation(),
                            fullName,
                            $"SKIP: Generator exception. REASON={genEx.GetType().Name}: {genEx.Message}"));
                        continue;
                    }
                    
                    // Log the generated source length for debugging
                    context.ReportDiagnostic(Diagnostic.Create(
                        FSM998_CandidateFound,
                        classDeclaration.GetLocation(),
                        fullName,
                        $"Generated source length: {source?.Length ?? 0} chars (features: payload={(model.GenerationConfig.HasPayload ? 1:0)}, ext={(model.GenerationConfig.HasExtensions ? 1:0)}, callbacks={(model.GenerationConfig.HasOnEntryExit ? 1:0)})"));
                    
                    // Check if generated source is empty or too small
                    if (string.IsNullOrWhiteSpace(source) || source.Length < 100)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            FSM997_SkippedCandidate,
                            classDeclaration.GetLocation(),
                            fullName,
                            $"SKIP: Generated source is empty or too small. REASON=Generator produced invalid output, Size={source?.Length ?? 0} chars, Summary: internalOnly={isInternalOnly}, hasExternal={externalCount > 0}, hasInternal={internalCount > 0}, states={model.States?.Count ?? 0}, transitions={model.Transitions?.Count ?? 0}"));
                        continue;
                    }

                    // Generate proper hint name using FQN
                    var fqn = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var hintName = GetUniqueHintName(fqn, usedHintNames);
                    
                    try
                    {
                        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
                        addedSources.Add(hintName);
                        context.ReportDiagnostic(Diagnostic.Create(
                            FSM996_AddSourceOk,
                            classDeclaration.GetLocation(),
                            hintName,
                            source.Length));
                    }
                    catch (Exception ex)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            FSM997_SkippedCandidate,
                            classDeclaration.GetLocation(),
                            hintName,
                            $"AddSource failed: {ex.GetType().Name}: {ex.Message}"));
                        throw;
                    }

                    // 2) Dependency Injection (opcjonalnie)
                    if (model.GenerateDependencyInjection)
                    {
                        var factoryModel = FactoryGenerationModelBuilder.Create(model);
                        var factoryGenerator = new FactoryCodeGenerator(factoryModel);
                        var factorySource = factoryGenerator.Generate();
                        var factoryHintName = GetUniqueHintName($"{fqn}.Factory", usedHintNames);
                        
                        try
                        {
                            context.AddSource(factoryHintName, SourceText.From(factorySource, Encoding.UTF8));
                            addedSources.Add(factoryHintName);
                            context.ReportDiagnostic(Diagnostic.Create(
                                FSM996_AddSourceOk,
                                classDeclaration.GetLocation(),
                                factoryHintName,
                                factorySource.Length));
                        }
                        catch (Exception ex)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                FSM997_SkippedCandidate,
                                classDeclaration.GetLocation(),
                                factoryHintName,
                                $"AddSource failed: {ex.GetType().Name}: {ex.Message}"));
                        }
                    }

                    // 3) Logging helpers (opcjonalnie)
                    if (model.GenerateLogging)
                    {
                        var loggingGenerator = new Generator.Log.LoggingClassGenerator(model.ClassName, model.Namespace);
                        var loggingSource = loggingGenerator.Generate();
                        var loggingHintName = GetUniqueHintName($"{fqn}Log", usedHintNames);
                        
                        try
                        {
                            context.AddSource(loggingHintName, SourceText.From(loggingSource, Encoding.UTF8));
                            addedSources.Add(loggingHintName);
                            context.ReportDiagnostic(Diagnostic.Create(
                                FSM996_AddSourceOk,
                                classDeclaration.GetLocation(),
                                loggingHintName,
                                loggingSource.Length));
                        }
                        catch (Exception ex)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                FSM997_SkippedCandidate,
                                classDeclaration.GetLocation(),
                                loggingHintName,
                                $"AddSource failed: {ex.GetType().Name}: {ex.Message}"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Report the exception as a diagnostic
                    context.ReportDiagnostic(Diagnostic.Create(
                        FSM997_SkippedCandidate,
                        classDeclaration.GetLocation(),
                        fullName,
                        $"Generation exception: {ex.Message}"));
                }
            }
        }
        catch
        {
            // Bez diagnostyki
        }
        finally
        {
            // Always emit the index file at the end
            EmitAddedSourcesIndexInExecute(context, addedSources, data.compAndOpts.OptionsProvider);
        }
    }
    
    private static void EmitAddedSourcesIndexInExecute(
        SourceProductionContext context,
        List<string> addedSources,
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        // Compute expected output directory
        var generatorAssemblyName = typeof(StateMachineGenerator).Assembly.GetName().Name;
        var generatorTypeName = typeof(StateMachineGenerator).FullName;
        var expectedDir = $"obj/GeneratedFiles/{generatorAssemblyName}/{generatorTypeName}";
        
        // Read MSBuild properties again for the index
        var globalOptions = optionsProvider.GlobalOptions;
        globalOptions.TryGetValue("build_property.EmitCompilerGeneratedFiles", out var emitFiles);
        globalOptions.TryGetValue("build_property.CompilerGeneratedFilesOutputPath", out var outputPath);
        
        // Build index content
        var sb = new IndentedStringBuilder.IndentedStringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// FastFSM added sources index");
        sb.AppendLine($"// Generator: {generatorTypeName}");
        sb.AppendLine($"// Assembly: {generatorAssemblyName}");
        sb.AppendLine($"// Expected output dir: {expectedDir}");
        sb.AppendLine($"// EmitCompilerGeneratedFiles: {emitFiles ?? "(not set)"}");
        sb.AppendLine($"// CompilerGeneratedFilesOutputPath: {outputPath ?? "(not set)"}");
        
        addedSources.Sort();
        
        sb.AppendLine($"// Count: {addedSources.Count}");
        sb.AppendLine();
        sb.AppendLine("/*");
        for (int i = 0; i < addedSources.Count; i++)
        {
            sb.AppendLine($" * {i}: {addedSources[i]}");
        }
        sb.AppendLine(" */");
        sb.AppendLine();
        sb.AppendLine("internal static class __FastFsm_AddedSourcesIndex");
        using (sb.Block(""))
        {
            sb.AppendLine($"public const int Count = {addedSources.Count};");
        }
        
        try
        {
            context.AddSource("__FastFsm.AddedSources.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            context.ReportDiagnostic(Diagnostic.Create(
                FSM996_AddSourceOk,
                Location.None,
                "__FastFsm.AddedSources.g.cs",
                sb.ToString().Length));
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FSM997_SkippedCandidate,
                Location.None,
                "__FastFsm.AddedSources.g.cs",
                $"AddSource failed: {ex.GetType().Name}: {ex.Message}"));
        }
    }
}
