using Generator.DependencyInjection;
using Generator.FeatureDetection;
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
        // ──────────────────────────────────────────────────────────────
        // Rozbij krotkę wejściową na składniki
        // ──────────────────────────────────────────────────────────────
        var (compAndOpts, classes) = data;
        var (compilation, optionsProvider) = compAndOpts;

        // ──────────────────────────────────────────────────────────────
        // Nic do roboty, jeśli nie ma klas
        // ──────────────────────────────────────────────────────────────
        if (classes.IsDefaultOrEmpty)
            return;

        // ──────────────────────────────────────────────────────────────
        // Przygotuj parser i selector wariantów
        // ──────────────────────────────────────────────────────────────
        var parser = new StateMachineParser(compilation, context);
        var variantSelector = new VariantSelector();

        // ──────────────────────────────────────────────────────────────
        // Iteracja po wszystkich klasach z [StateMachine]
        // ──────────────────────────────────────────────────────────────
        foreach (var classDeclaration in classes)
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            // ──────────────────────────────────────────────────────────
            // Spróbuj sparsować definicję state machine
            // ──────────────────────────────────────────────────────────
            if (!parser.TryParse(classDeclaration, out StateMachineModel? model))
            {
                // Parser już zgłosił diagnostykę; pomiń
                continue;
            }

            // ──────────────────────────────────────────────────────────
            // Pobierz symbol klasy i skonfiguruj model
            // ──────────────────────────────────────────────────────────
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
            {
                // Nie powinno się zdarzyć – bezpieczeństwo
                continue;
            }

            // Wybór wariantu generatora (dla maszyn sync)
            variantSelector.DetermineVariant(model!, classSymbol);


            // W metodzie Execute, zaktualizuj sekcję Feature Detection:

            // ──────────────────────────────────────────────────────────
            // Feature Detection i Walidacja (Milestone 0)
            // ──────────────────────────────────────────────────────────
            var detector = new FeatureDetector();
            var features = detector.Detect(model!);

            // Loguj wykryte cechy (pomocne przy debugowaniu)
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "FSM999",
                    "Feature Detection",
                    $"Detected features: {features.GetDescription()}",
                    "Debug",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true),
                Location.None));

            // Waliduj czy wybrany wariant jest zgodny z wykrytymi cechami
            var matcher = new VariantMatcher();
            var validation = matcher.ValidateVariant(features, model!.Variant, model!.GenerationConfig.IsForced);

            if (validation.HasIssues())
            {
                // Jeśli to tylko ostrzeżenia (Force = true), zgłoś je ale kontynuuj
                if (validation.IsWarningOnly)
                {
                    foreach (var warning in validation.Warnings)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "FSM997",
                                "Variant Mismatch Warning",
                                $"Forced variant '{model!.Variant}' may not work correctly: {warning}",
                                "Generation",
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault: true),
                            classDeclaration.GetLocation()));
                    }
                    // Kontynuuj generację mimo ostrzeżeń
                }
                else if (!validation.IsValid)
                {
                    // Zgłoś błąd jeśli wariant nie pasuje do cech i nie jest wymuszony
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FSM998",
                            "Variant Mismatch",
                            $"Selected variant '{model!.Variant}' is incompatible with detected features: {validation.GetErrorMessage()}",
                            "Generation",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        classDeclaration.GetLocation()));

                    continue; // Pomiń generację tylko dla błędów
                }
            }

            // Dodaj też informację o sugerowanym wariancie
            var suggestedVariant = features.GetSuggestedVariant();
            if (suggestedVariant != model!.Variant && !model!.GenerationConfig.IsForced)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "FSM996",
                        "Variant Suggestion",
                        $"Based on detected features, consider using variant '{suggestedVariant}' instead of '{model!.Variant}'",
                        "Generation",
                        DiagnosticSeverity.Info,
                        isEnabledByDefault: true),
                    classDeclaration.GetLocation()));
            }


















            // Ustaw flagi dla DI i logowania
            model!.GenerateLogging = BuildProperties.GetGenerateLogging(
                optionsProvider.GlobalOptions);

            model!.GenerateDependencyInjection = BuildProperties.GetGenerateDI(
                optionsProvider.GlobalOptions);

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

            // ──────────────────────────────────────────────────────────
            // 2. Generuj kod dla Dependency Injection (jeśli włączone)
            // ──────────────────────────────────────────────────────────
            if (model.GenerateDependencyInjection)
            {
                // TODO: Ta część będzie wymagała modyfikacji, aby poprawnie
                // generować fabryki dla maszyn asynchronicznych.
                // Na razie pozostaje bez zmian.
                var factoryModel = FactoryGenerationModelBuilder.Create(model);
                var factoryGenerator = new FactoryCodeGenerator(factoryModel);
                var factorySource = factoryGenerator.Generate();
                context.AddSource(
                    $"{model.ClassName}.Factory.g.cs",
                    SourceText.From(factorySource, Encoding.UTF8));
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
            }
        }
    }
}