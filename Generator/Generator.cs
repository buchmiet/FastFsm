using Generator.DependencyInjection;
using Generator.Helpers;
using Generator.Model;
using Generator.Parsers;
using Generator.Rules.Definitions;
using Generator.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using static Generator.Strings;

namespace Generator;

[Generator]
public class StateMachineGenerator : IIncrementalGenerator
{

    private static readonly HashSet<string> FsmAttrFullNames =
    [
        TransitionAttributeFullName,
        InternalTransitionAttributeFullName,
        StateAttributeFullName,
        PayloadTypeAttributeFullName
    ];


    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        // ──────────────────────────────────────────────────────────────
        // 1. Wyszukujemy klasy oznaczone atrybutem [StateMachine] ───────
        // ──────────────────────────────────────────────────────────────
        var stateMachineClasses = ctx.SyntaxProvider
            .CreateSyntaxProvider(
                (node, _) => IsPotentialStateMachine(node),          // filtr wstępny
                (ctx, _) => GetStateMachineClass(ctx))              // transformacja na ClassDeclarationSyntax
            .Where(c => c is not null)
            .Select((c, _) => c!);                                   // ! – już odfiltrowane null-e

        // ──────────────────────────────────────────────────────────────
        // 2. Łączymy Compilation z AnalyzerConfigOptionsProvider ──────
        // ──────────────────────────────────────────────────────────────
        var compAndOpts = ctx.CompilationProvider
                             .Combine(ctx.AnalyzerConfigOptionsProvider);
        // typ wynikowy: (Compilation, AnalyzerConfigOptionsProvider)

        // ──────────────────────────────────────────────────────────────
        // 3. Dodajemy kolekcję klas z pkt 1 do krotki z pkt 2 ──────────
        // ──────────────────────────────────────────────────────────────
        var input = compAndOpts.Combine(stateMachineClasses.Collect());
        // typ wynikowy: ((Compilation, AnalyzerConfigOptionsProvider), ImmutableArray<ClassDeclarationSyntax>)

        // ──────────────────────────────────────────────────────────────
        // 4. Rejestrujemy główną produkcję źródła ──────────────────────
        // ──────────────────────────────────────────────────────────────
        ctx.RegisterSourceOutput(input, Execute);

        // ──────────────────────────────────────────────────────────────
        // 5. Diagnostyka FSM004: klasy z atrybutami przejść bez
        //    [StateMachine] lub bez partial ───────────────────────────
        // ──────────────────────────────────────────────────────────────
        var classesMissingStateMachine = ctx.SyntaxProvider
            .CreateSyntaxProvider(
                IsPotentialFsmClassWithoutAttribute,                 // filtr
                GetClassIfMissingStateMachine)                       // transformacja
            .Where(sym => sym is not null);

        ctx.RegisterSourceOutput(classesMissingStateMachine, (spc, cls) =>
        {
            var diag = Diagnostic.Create(
                DiagnosticFactory.Get(RuleIdentifiers.MissingStateMachineAttribute),
                cls!.Locations.FirstOrDefault() ?? Location.None,
                cls!.Name);

            spc.ReportDiagnostic(diag);
        });
    }


    private static bool IsPotentialStateMachine(SyntaxNode node) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    private static ClassDeclarationSyntax? GetStateMachineClass(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol) continue;
                var containingType = methodSymbol.ContainingType;
                if (containingType.ToDisplayString() == StateMachineAttributeFullName)
                {
                    return classDeclaration;
                }
            }
        }

        return null;
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
         ImmutableArray<ClassDeclarationSyntax> Classes
     ) data)
    {
        try
        {
            // ──────────────────────────────────────────────────────────────
            // Rozbij krotkę wejściową na składniki
            // ──────────────────────────────────────────────────────────────
            var (compAndOpts, classes) = data;
            var (compilation, optionsProvider) = compAndOpts;
            
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSMDEBUG",
                    "Debugging Generator",
                    "🧪 Generator executed on {0}",
                    "FastFSM",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None,
                DateTime.Now.ToString("T")));
                
            // Debug: Report number of classes found
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSMDEBUG2",
                    "Classes Found",
                    "Found {0} classes to process",
                    "FastFSM",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None,
                classes.Length));

            // ──────────────────────────────────────────────────────────────
            // Nic do roboty, jeśli nie ma klas
            // ──────────────────────────────────────────────────────────────
            if (classes.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FSMDEBUG",
                        "Debugging Generator",
                        "🧪 No classes to process",
                        "FastFSM",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSMDEBUG",
                    "Debugging Generator",
                    $"🧪 Processing {classes.Length} classes",
                    "FastFSM",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None));

            // ──────────────────────────────────────────────────────────────
            // Przygotuj parser i selector wariantów
            // ──────────────────────────────────────────────────────────────
            StateMachineParser parser = null;
            VariantSelector variantSelector = null;

            try
            {
                parser = new StateMachineParser(compilation, context);
                variantSelector = new VariantSelector();

                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FSMDEBUG",
                        "Debugging Generator",
                        "🧪 Parser and variant selector created",
                        "FastFSM",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FSMERROR",
                        "Generator Error",
                        $"🔥 Failed to create parser: {ex.GetType().Name}: {ex.Message}",
                        "FastFSM",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
                return;
            }

            // ──────────────────────────────────────────────────────────────
            // Iteracja po wszystkich klasach z [StateMachine]
            // ──────────────────────────────────────────────────────────────
            int classIndex = 0;
            foreach (var classDeclaration in classes)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                classIndex++;
                string className = classDeclaration.Identifier.Text;

                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FSMDEBUG",
                        "Debugging Generator",
                        $"🧪 Processing class {classIndex}/{classes.Length}: {className}",
                        "FastFSM",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));

                // ──────────────────────────────────────────────────────────
                // Spróbuj sparsować definicję state machine
                // ──────────────────────────────────────────────────────────
                void reportParsingError(string message)
                {
                    var diagnostic = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "FSM002",
                            title: "StateMachine parsing error",
                            messageFormat: $"[{className}] {message}",
                            category: "FastFSM.Generator",
                            DiagnosticSeverity.Warning,
                            isEnabledByDefault: true),
                        classDeclaration.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }

                StateMachineModel model = null;
                try
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FSMDEBUG",
                            "Debugging Generator",
                            $"🧪 Starting TryParse for {className}",
                            "FastFSM",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));

                    bool parseResult = parser.TryParse(classDeclaration, out model, reportParsingError);

                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FSMDEBUG",
                            "Debugging Generator",
                            $"🧪 TryParse completed for {className}, result: {parseResult}",
                            "FastFSM",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));

                    if (!parseResult)
                    {
                        var diagnostic = Diagnostic.Create(
                            new DiagnosticDescriptor(
                                id: "FSM001",
                                title: "StateMachine parsing failed",
                                messageFormat: "Could not parse class '{0}' as a valid state machine.",
                                category: "FastFSM.Generator",
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault: true),
                            classDeclaration.GetLocation(),
                            className);

                        context.ReportDiagnostic(diagnostic);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FSMERROR",
                            "Generator Error",
                            $"🔥 Exception in TryParse for {className}: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}",
                            "FastFSM",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FSMDEBUG",
                        "Debugging Generator",
                        $"🧪 Parsing done for {className}",
                        "FastFSM",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));

                try
                {
                    // ──────────────────────────────────────────────────────────
                    // Pobierz symbol klasy i skonfiguruj model
                    // ──────────────────────────────────────────────────────────
                    var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "FSMERROR",
                                "Generator Error",
                                $"🔥 Could not get class symbol for {className}",
                                "FastFSM",
                                DiagnosticSeverity.Warning,
                                true),
                            Location.None));
                        continue;
                    }

                    // Wybór wariantu generatora (dla maszyn sync)
                    variantSelector.DetermineVariant(model!, classSymbol);

                    // Ustaw flagi dla DI i logowania
                    model!.GenerateLogging = BuildProperties.GetGenerateLogging(
                        optionsProvider.GlobalOptions);

                    model!.GenerateDependencyInjection = BuildProperties.GetGenerateDI(
                        optionsProvider.GlobalOptions);

                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FSMDEBUG",
                            "Debugging Generator",
                            $"🧪 Model configured for {className}: Variant={model.Variant}, Logging={model.GenerateLogging}, DI={model.GenerateDependencyInjection}",
                            "FastFSM",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));

                    // ──────────────────────────────────────────────────────────
                    // 1. Wybierz i uruchom odpowiedni generator kodu
                    // ──────────────────────────────────────────────────────────
                    StateMachineCodeGenerator generator;

                    // Istniejąca logika dla maszyn synchronicznych
                    generator = model.Variant switch
                    {
                        GenerationVariant.Full => new FullVariantGenerator(model),
                        GenerationVariant.WithPayload => new PayloadVariantGenerator(model),
                        GenerationVariant.WithExtensions => new ExtensionsVariantGenerator(model),
                        _ => new CoreVariantGenerator(model) // Pure / Basic
                    };

                    var source = generator.Generate();
                    context.AddSource(
                        $"{model.ClassName}.Generated.cs",
                        SourceText.From(source, Encoding.UTF8));

                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FSMDEBUG",
                            "Debugging Generator",
                            $"🧪 Main source generated for {className}",
                            "FastFSM",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));

                    // ──────────────────────────────────────────────────────────
                    // 2. Generuj kod dla Dependency Injection (jeśli włączone)
                    // ──────────────────────────────────────────────────────────
                    if (model.GenerateDependencyInjection)
                    {
                        var factoryModel = FactoryGenerationModelBuilder.Create(model);
                        var factoryGenerator = new FactoryCodeGenerator(factoryModel);
                        var factorySource = factoryGenerator.Generate();
                        context.AddSource(
                            $"{model.ClassName}.Factory.g.cs",
                            SourceText.From(factorySource, Encoding.UTF8));

                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "FSMDEBUG",
                                "Debugging Generator",
                                $"🧪 Factory generated for {className}",
                                "FastFSM",
                                DiagnosticSeverity.Warning,
                                true),
                            Location.None));
                    }

                    // ──────────────────────────────────────────────────────────
                    // 3. Generuj klasę helperów logowania (jeśli włączone)
                    // ──────────────────────────────────────────────────────────
                    if (model.GenerateLogging)
                    {
                        var loggingGenerator = new Generator.Log.LoggingClassGenerator(model.ClassName, model.Namespace);
                        var loggingSource = loggingGenerator.Generate();
                        context.AddSource(
                            $"{model.ClassName}Log.g.cs",
                            SourceText.From(loggingSource, Encoding.UTF8));

                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "FSMDEBUG",
                                "Debugging Generator",
                                $"🧪 Logging helper generated for {className}",
                                "FastFSM",
                                DiagnosticSeverity.Warning,
                                true),
                            Location.None));
                    }
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FSMERROR",
                            "Generator Error",
                            $"🔥 Exception in generation for {className}: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}",
                            "FastFSM",
                            DiagnosticSeverity.Warning,
                            true),
                        Location.None));
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSMDEBUG",
                    "Debugging Generator",
                    "🧪 Generator execution completed successfully",
                    "FastFSM",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None));
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSMERROR",
                    "Generator Error",
                    $"🔥 Unhandled exception in Execute: {ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    "FastFSM",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None));
        }
    }
}