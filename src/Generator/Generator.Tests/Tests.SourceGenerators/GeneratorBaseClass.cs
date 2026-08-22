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

namespace Tests.SourceGenerators;

public abstract class GeneratorBaseClass(ITestOutputHelper output)
{
    private sealed class DictionaryAnalyzerConfigOptionsProvider(IDictionary<string, string> global)
        : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _globalOptions = new DictionaryAnalyzerConfigOptions(global);

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

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
        // Find the projects directory
        string testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string configuration = testAssemblyPath.Contains("Debug") ? "Debug" : "Release";

        // Walk up to the solution directory
        string currentDir = testAssemblyPath;
        string? solutionDir = null;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.GetFiles(currentDir, "*.slnx").Any())
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

        // Add FastFsm.dll
        string fsmFastDllPath = Path.Combine(
            solutionDir, "src", "Fsm", "Fsm.Core", "bin", configuration, "net10.0", "FastFsm.dll");

        if (File.Exists(fsmFastDllPath))
        {
            refs.Add(MetadataReference.CreateFromFile(fsmFastDllPath));
            output.WriteLine($"Added reference to: {fsmFastDllPath}");
        }
        else
        {
            throw new FileNotFoundException($"FastFsm.dll not found at: {fsmFastDllPath}. " +
                                            "Make sure Fsm.Core is built before running tests.");
        }

        // Add Abstractions.dll (if it is not already in the attributes)
        string abstractionsDllPath = Path.Combine(
            solutionDir, "src", "Abstractions", "bin", configuration, "netstandard2.0", "Abstractions.dll");

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
            if (Directory.GetFiles(current, "*.sln").Any()
                || Directory.GetFiles(current, "*.slnx").Any())
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;

            current = parent.FullName;
        }

        return null;
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
            // ExtensionRunner.cs (shared-source)
            var extRunner = Path.Combine(solutionDir,
                                          "src",
                                          "Fsm",
                                          "Fsm.Core",
                                          "Runtime",
                                          "Extensions",
                                          "ExtensionRunner.cs");
            if (File.Exists(extRunner))
                allSourceTexts.Add(File.ReadAllText(extRunner));

            // ─── extra DI files (shared-source) ───
            if (enableDependencyInjection)
            {
                var diDir = Path.Combine(solutionDir, "src", "Fsm", "Fsm.Core", "DependencyInjection");
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

        // 3.  User source
        allSourceTexts.AddRange(userSources);

        // ─── preprocessor symbols (#if FSM_…) ───
        var symbols = new List<string>();
        if (enableLogging) symbols.Add("FSM_LOGGING_ENABLED");
        if (enableDependencyInjection) symbols.Add("FSM_DI_ENABLED");

        var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(symbols);

        var trees = allSourceTexts
            .Select(src => CSharpSyntaxTree.ParseText(src, parseOptions))
            .ToArray();

        // ─── references ───
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

            // ── Fsm.DependencyInjection.dll (from repo product project) ──
            if (solutionDir is not null)
            {
                string testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                string configuration = testAssemblyPath.Contains("Debug") ? "Debug" : "Release";

                var diDllPath = Path.Combine(
                    solutionDir,
                    "src",
                    "Fsm",
                    "Fsm.DependencyInjection",
                    "bin",
                    configuration,
                    "net10.0",
                    "Fsm.DependencyInjection.dll");

                if (File.Exists(diDllPath))
                    refs.Add(MetadataReference.CreateFromFile(diDllPath));
                // if missing – the test will show diagnostics, which helps debugging.
            }
        }

        // netstandard (needed on some runtimes)
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

        // ─── collected generated sources ───
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