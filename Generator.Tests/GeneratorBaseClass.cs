using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit.Abstractions;

namespace Generator.Tests;
/*
 *
 *### Propozycja zestawu test-case’ów (tylko nazwy + krótki opis)
   
   | #        | Scenariusz                                                                               | Cel testu                                                                                                                |
   | -------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
   | **F-01** | **Forced WithPayload bez `[PayloadType]`**                                               | Sprawdzić, że generator zgłasza błąd **FSM007** (brak typu payload) zamiast wyjątku wewnętrznego.                        |
   | **F-02** | **Forced WithPayload + wiele `[PayloadType(trigger,…)]`**                                | Oczekiwana eskalacja do wariantu *multi* lub błąd diagnostyczny – upewnić się, że Force nie tworzy niepoprawnej maszyny. |
   | **F-03** | **Forced WithPayload + guard z parametrem innego typu**                                  | Walidacja **FSM003** powinna wychwycić złą sygnaturę metody.                                                             |
   | **F-04** | **Forced WithPayload + brak overloadu bezparametrowego**                                 | Zweryfikować, że payload-metody działają, a brak parametru powoduje poprawne wywołanie/odrzucenie.                       |
   | **F-05** | **Forced WithPayload + wywołanie `TryFire(trigger, object?)` z błędnym typem w runtime** | Potwierdzić, że wariant *single* nie rzuca wyjątku, tylko przechodzi ścieżkę „fallback” (is + wersja bezparametrowa).    |
   | **F-06** | **Forced Full bez `[PayloadType]`**                                                      | Błąd diagnostyczny jak w F-01.                                                                                           |
   | **F-07** | **Forced Full + `GenerateExtensibleVersion=false`**                                      | Generator powinien zignorować flagę Force lub zgłosić spójny błąd („Full wymaga extensible”).                            |
   | **F-08** | **Forced Pure przy obecnych `[PayloadType]`**                                            | Oczekiwana seria błędów **FSM003** (nadmiarowe parametry) – kod nie powinien się wygenerować.                            |
   | **F-09** | **Forced Pure, ale klasa zawiera OnEntry/OnExit**                                        | Should downgrade zawartość (ignorować metody) lub wyemitować ostrzeżenie, brak crashu.                                   |
   | **F-10** | **Brak Force → autodetekcja Single vs Multi**                                            | Referencyjny przypadek poprawnej detekcji (kontrola regresji).                                                           |
   | **F-11** | **Multi-payload: złe dane w runtime (`_payloadMap` check)**                              | `TryFire` zwraca `false`, `Fire` rzuca `InvalidOperationException`.                                                      |
   | **F-12** | **Single-payload: poprawny typ, ale `null` payload**                                     | Metody z parametrem pomijane, wywołania bezparametrowe działają – brak wyjątków.                                         |
   
   > *Lista obejmuje zarówno walidację kompilacji (diagnostyki FSM00x) jak i zachowanie w trakcie wykonywania dla najczęstszych „mismatchy” pomiędzy deklaracjami a realnym użyciem.*
   
 *
 */
public abstract class GeneratorBaseClass(ITestOutputHelper output)
{
    private sealed class DictionaryAnalyzerConfigOptionsProvider(IDictionary<string, string> global)
        : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions = new DictionaryAnalyzerConfigOptions(global);

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        // Generator i tak patrzy wyłącznie w GlobalOptions,
        // więc dla SyntaxTree / AdditionalText zwracamy te same.
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText text) => _globalOptions;

        private sealed class DictionaryAnalyzerConfigOptions(IDictionary<string, string> values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string? value) => values.TryGetValue(key, out value);

            public override IEnumerable<string> Keys => values.Keys;
        }
    }
    protected void AddProjectReferences(List<MetadataReference> refs)
    {
        // Znajdź katalog z projektami
        string testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string configuration = testAssemblyPath.Contains("Debug") ? "Debug" : "Release";

        // Idź w górę do katalogu rozwiązania
        string currentDir = testAssemblyPath;
        string? solutionDir = null;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.GetFiles(currentDir, "*.sln").Any())
            {
                solutionDir = currentDir;
                break;
            }
            var parent = Directory.GetParent(currentDir);
            if (parent == null) break;
            currentDir = parent.FullName;
        }

        if (solutionDir == null)
            throw new InvalidOperationException("Cannot find solution directory");

        // Dodaj StateMachine.dll
        string fsmFastDllPath = Path.Combine(
            solutionDir, "StateMachine.", "bin", configuration, "net9.0", "StateMachine.dll");

        if (File.Exists(fsmFastDllPath))
        {
            refs.Add(MetadataReference.CreateFromFile(fsmFastDllPath));
            output.WriteLine($"Added reference to: {fsmFastDllPath}");
        }
        else
        {
            throw new FileNotFoundException($"StateMachine.dll not found at: {fsmFastDllPath}. " +
                                            "Make sure StateMachine. project is built before running tests.");
        }

        // Dodaj Abstractions.dll (jeśli nie jest już w atrybutach)
        string abstractionsDllPath = Path.Combine(
            solutionDir, "Abstractions", "bin", configuration, "netstandard2.0", "Abstractions.dll");

        if (File.Exists(abstractionsDllPath))
        {
            refs.Add(MetadataReference.CreateFromFile(abstractionsDllPath));
            output.WriteLine($"Added reference to: {abstractionsDllPath}");
        }
    }


    private string? GetSolutionDir()
    {
        string testAssemblyPath = Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location)!;

        string current = testAssemblyPath;
        for (int i = 0; i < 10; i++)
        {
            // Jeśli w bieżącym katalogu jest plik .sln → to nasz root
            if (Directory.GetFiles(current, "*.sln").Any())
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;                     // dotarliśmy do korzenia dysku

            current = parent.FullName;     // przejdź katalog wyżej
        }

        return null;                       // nie znaleziono pliku .sln
    }




    protected (Assembly? asm,
            ImmutableArray<Diagnostic> diags,
            Dictionary<string, string> generatedSources)
 CompileAndRunGenerator(
     IEnumerable<string> userSources,
     IIncrementalGenerator generator,
     bool enableLogging = false,
     bool enableDependencyInjection = false)
    {
        // ───────── build_property.* → AnalyzerConfigOptionsProvider ─────────
        var buildProps = new Dictionary<string, string>();
        if (enableLogging)
            buildProps["build_property.FsmGenerateLogging"] = "true";
        if (enableDependencyInjection)
            buildProps["build_property.FsmGenerateDI"] = "true";

        var optionsProvider = new DictionaryAnalyzerConfigOptionsProvider(buildProps);
        // -------------------------------------------------------------------

        var allSourceTexts = new List<string>();

        var solutionDir = GetSolutionDir();
        if (solutionDir is not null)
        {
            // ExtensionRunner.cs (shared‑source)
            var extRunner = Path.Combine(solutionDir,
                                         "StateMachine",
                                         "Runtime",
                                         "Extensions",
                                         "ExtensionRunner.cs");
            if (File.Exists(extRunner))
                allSourceTexts.Add(File.ReadAllText(extRunner));

            // ─── dodatkowe pliki DI (shared‑source) ───
            if (enableDependencyInjection)
            {
                var diDir = Path.Combine(solutionDir, "StateMachine.DependencyInjection");
                foreach (var file in new[]
                {
                "FsmServiceCollectionExtensions.cs",
                "StateMachineFactory.cs"
            })
                {
                    var path = Path.Combine(diDir, file);
                    if (File.Exists(path))
                        allSourceTexts.Add(File.ReadAllText(path));
                }
            }
        }

        // 3.  Kody użytkownika
        allSourceTexts.AddRange(userSources);

        // ─── symbole preprocesora (#if FSM_…) ───
        var symbols = new List<string>();
        if (enableLogging) symbols.Add("FSM_LOGGING_ENABLED");
        if (enableDependencyInjection) symbols.Add("FSM_DI_ENABLED");

        var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(symbols);

        var trees = allSourceTexts
            .Select(src => CSharpSyntaxTree.ParseText(src, parseOptions))
            .ToArray();

        // ─── referencje ───
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        AddProjectReferences(refs);

        if (enableLogging)
        {
            // ILogger<T>
            refs.Add(MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location));
        }

        if (enableDependencyInjection)
        {
            // IServiceCollection
            refs.Add(MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location));

            // ── StateMachine.DependencyInjection.dll (z projektu w repo) ──
            if (solutionDir is not null)
            {
                string testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                string configuration = testAssemblyPath.Contains("Debug") ? "Debug" : "Release";

                var diDllPath = Path.Combine(
                    solutionDir,
                    "StateMachine.DependencyInjection",
                    "bin",
                    configuration,
                    "net9.0",
                    "StateMachine.DependencyInjection.dll");

                if (File.Exists(diDllPath))
                    refs.Add(MetadataReference.CreateFromFile(diDllPath));
                // jeśli brak – test pokaże diagnostykę, co ułatwi debug.
            }
        }

        // netstandard (potrzebny przy niektórych runtime’ach)
        var netstandard = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "netstandard.dll");
        if (File.Exists(netstandard))
            refs.Add(MetadataReference.CreateFromFile(netstandard));

        var compilation = CSharpCompilation.Create(
            "FsmTestAssembly",
            trees,
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            additionalTexts: null,
            parseOptions,
            optionsProvider);

        var driverAfterRun = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outCompilation,
            out var genDiags);

        // ─── zebrane kody wygenerowane ───
        var generated = new Dictionary<string, string>();
        foreach (var result in driverAfterRun.GetRunResult().Results)
            foreach (var src in result.GeneratedSources)
                generated[src.HintName] = src.SourceText.ToString();

        using var ms = new MemoryStream();
        var emitResult = outCompilation.Emit(ms);
        var allDiagnostics = genDiags.AddRange(emitResult.Diagnostics);

        Assembly? asm = null;
        if (emitResult.Success)
        {
            ms.Position = 0;
            asm = Assembly.Load(ms.ToArray());
        }

        return (asm, allDiagnostics, generated);
    }




}