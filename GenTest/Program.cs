using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GenTest;

internal static class Program
{
    private static int Main(string[] args)
    {
        var inOpt  = new Option<FileInfo>("--in", description: "Path to input .cs file") { IsRequired = true };
        var genOpt = new Option<FileInfo?>("--generator", description: "Path to Generator.dll (IIncrementalGenerator/ISourceGenerator)");
        var outOpt = new Option<DirectoryInfo?>("--out", description: "Output directory for generated sources and diagnostics");
        var watch  = new Option<bool>("--watch", () => false, "Watch Generator.dll for changes and hot-reload");
        var logOpt = new Option<bool>("--logging", () => false, "Enable logging support (sets FsmGenerateLogging)");
        var diOpt  = new Option<bool>("--di", () => false, "Enable dependency injection (sets FsmGenerateDI)");

        var root = new RootCommand("GenTest - Fast Roslyn Source Generator Runner with Hot-Reload");
        root.AddOption(inOpt);
        root.AddOption(genOpt);
        root.AddOption(outOpt);
        root.AddOption(watch);
        root.AddOption(logOpt);
        root.AddOption(diOpt);
        
        root.SetHandler(async (FileInfo input, FileInfo? generator, DirectoryInfo? outDir, bool watchMode, bool logging, bool di) =>
        {
            if (!input.Exists)
            {
                Console.Error.WriteLine($"[ERROR] Input file not found: {input.FullName}");
                Environment.ExitCode = 3; 
                return;
            }
            
            // If no generator specified, try to find it in standard location
            if (generator is null || !generator.Exists)
            {
                var defaultPath = Path.Combine(AppContext.BaseDirectory, "..", "Generator", "bin", "Release", "netstandard2.0", "Generator.dll");
                if (!File.Exists(defaultPath))
                {
                    defaultPath = Path.Combine(AppContext.BaseDirectory, "..", "Generator", "bin", "Debug", "netstandard2.0", "Generator.dll");
                }
                
                if (File.Exists(defaultPath))
                {
                    generator = new FileInfo(defaultPath);
                    Console.WriteLine($"[INFO] Using default generator: {generator.FullName}");
                }
                else
                {
                    Console.Error.WriteLine("[ERROR] Provide --generator <path to Generator.dll> or build the Generator project");
                    Environment.ExitCode = 2; 
                    return;
                }
            }
            
            outDir?.Create();

            using var runner = new HotRunner(generator.FullName, outDir?.FullName, logging, di);
            await runner.RunOnceAsync(input.FullName);

            if (!watchMode) return;
            
            using var fsw = new FileSystemWatcher(generator.DirectoryName!, generator.Name)
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
            };
            
            Console.WriteLine($"[WATCH] Watching {generator.FullName} ... Press Ctrl+C to exit.");
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
            
            fsw.Changed += async (_, __) =>
            {
                await Task.Delay(100); // Small delay to ensure file write is complete
                try 
                { 
                    await runner.ReloadAndRunAsync(input.FullName); 
                }
                catch (Exception ex) 
                { 
                    Console.Error.WriteLine("[ERROR] Reload failed: " + ex.Message); 
                }
            };
            
            try { await Task.Delay(Timeout.Infinite, cts.Token); } catch { /* exit */ }
        }, inOpt, genOpt, outOpt, watch, logOpt, diOpt);

        return root.Invoke(args);
    }
}

internal sealed class HotRunner : IDisposable
{
    private readonly string _generatorPath;
    private readonly string? _outDir;
    private readonly bool _enableLogging;
    private readonly bool _enableDI;
    private CollectibleAlc? _alc;
    private ImmutableArray<ISourceGenerator> _gens = ImmutableArray<ISourceGenerator>.Empty;
    private int _runCount = 0;

    public HotRunner(string generatorPath, string? outDir, bool enableLogging, bool enableDI)
    {
        _generatorPath = generatorPath;
        _outDir = outDir;
        _enableLogging = enableLogging;
        _enableDI = enableDI;
        LoadGenerators();
    }

    public async Task RunOnceAsync(string inputPath)
    {
        _runCount++;
        Console.WriteLine($"\n[RUN #{_runCount}] Executing generators...");
        
        var compilation = BuildCompilation(inputPath);
        
        // Create analyzer config for generator options
        var options = CreateAnalyzerOptions();
        
        var driver = CSharpGeneratorDriver.Create(
            generators: _gens,
            additionalTexts: ImmutableArray<AdditionalText>.Empty,
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
            optionsProvider: options);
            
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics);
        await PersistAsync(updated, diagnostics, driver);
    }

    public async Task ReloadAndRunAsync(string inputPath)
    {
        Console.WriteLine("\n[HOT-RELOAD] Change detected - reloading generator...");
        UnloadGenerators();
        await Task.Delay(200); // Give time for file handles to release
        LoadGenerators();
        await RunOnceAsync(inputPath);
    }

    private AnalyzerConfigOptionsProvider? CreateAnalyzerOptions()
    {
        if (!_enableLogging && !_enableDI) return null;
        
        var globalOptions = new Dictionary<string, string>();
        if (_enableLogging)
            globalOptions["build_property.FsmGenerateLogging"] = "true";
        if (_enableDI)
            globalOptions["build_property.FsmGenerateDI"] = "true";
            
        return new SimpleAnalyzerConfigOptionsProvider(globalOptions);
    }

    private void LoadGenerators()
    {
        Console.WriteLine($"[LOAD] Loading generators from: {_generatorPath}");
        
        _alc = new CollectibleAlc(Path.GetDirectoryName(_generatorPath)!);
        var asm = _alc.LoadFromAssemblyPath(_generatorPath);

        var allTypes = asm.GetTypes();
        var incType = typeof(Microsoft.CodeAnalysis.IIncrementalGenerator);
        var srcType = typeof(Microsoft.CodeAnalysis.ISourceGenerator);

        var list = new List<ISourceGenerator>();
        
        // Search for generator types
        foreach (var t in allTypes)
        {
            if (!t.IsClass || t.IsAbstract) continue;
            
            // Debug: Show what we're checking
            var interfaces = t.GetInterfaces().Select(i => i.Name).ToList();
            if (interfaces.Any(i => i.Contains("Generator")))
            {
                Console.WriteLine($"  Checking type: {t.FullName} (interfaces: {string.Join(", ", interfaces)})");
            }
            
            // Check for ISourceGenerator
            if (srcType.IsAssignableFrom(t))
            {
                var instance = Activator.CreateInstance(t);
                if (instance != null)
                {
                    list.Add((ISourceGenerator)instance);
                    Console.WriteLine($"  ✓ Loaded ISourceGenerator: {t.FullName}");
                }
                continue;
            }
            
            // Check for IIncrementalGenerator - need to check by name due to version mismatch
            var hasIncrementalInterface = interfaces.Any(i => i == "IIncrementalGenerator");
            if (hasIncrementalInterface)
            {
                try
                {
                    // Try to create instance and adapt it
                    var instance = Activator.CreateInstance(t);
                    if (instance != null)
                    {
                        // Use GeneratorExtensions.AsSourceGenerator via reflection
                        var codeAnalysisAssembly = typeof(Compilation).Assembly;
                        var extensionsType = codeAnalysisAssembly.GetType("Microsoft.CodeAnalysis.GeneratorExtensions");
                        
                        if (extensionsType != null)
                        {
                            // Find the AsSourceGenerator method that takes IIncrementalGenerator
                            var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                            var asSourceGeneratorMethod = methods.FirstOrDefault(m => 
                                m.Name == "AsSourceGenerator" && 
                                m.GetParameters().Length == 1 &&
                                m.GetParameters()[0].ParameterType.Name == "IIncrementalGenerator");
                                
                            if (asSourceGeneratorMethod != null)
                            {
                                try
                                {
                                    // Use MakeGenericMethod if needed
                                    var adapted = asSourceGeneratorMethod.Invoke(null, new[] { instance });
                                    if (adapted != null)
                                    {
                                        list.Add((ISourceGenerator)adapted);
                                        Console.WriteLine($"  ✓ Loaded IIncrementalGenerator: {t.FullName}");
                                    }
                                }
                                catch (TargetInvocationException tie)
                                {
                                    Console.WriteLine($"  ✗ Failed to adapt {t.FullName}: {tie.InnerException?.Message ?? tie.Message}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"  ✗ Failed to adapt {t.FullName}: {ex.Message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"  ✗ AsSourceGenerator method not found with correct signature");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed to instantiate {t.FullName}: {ex.Message}");
                }
            }
        }
        
        if (list.Count == 0)
        {
            Console.WriteLine($"[WARNING] No generators found in {_generatorPath}");
            Console.WriteLine($"  Assembly contains {allTypes.Length} types");
        }

        _gens = list.ToImmutableArray();
        Console.WriteLine($"[LOAD] Loaded {_gens.Length} generator(s) successfully\n");
    }

    private void UnloadGenerators()
    {
        Console.WriteLine("[UNLOAD] Unloading current generators...");
        _gens = ImmutableArray<ISourceGenerator>.Empty;
        
        var alcToDispose = _alc;
        _alc = null;
        
        alcToDispose?.Dispose();
        
        // Force garbage collection to release file handles
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Console.WriteLine("[UNLOAD] Generators unloaded");
    }

    private static CSharpCompilation BuildCompilation(string inputPath)
    {
        var text = File.ReadAllText(inputPath);
        var syntax = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Latest));
        
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        };
        
        // Try to add project-specific references
        TryAddProjectReferences(refs);
        
        return CSharpCompilation.Create(
            assemblyName: "GenTest.Input",
            syntaxTrees: new[] { syntax },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
    
    private static void TryAddProjectReferences(List<MetadataReference> refs)
    {
        // Try to find and add Abstractions.dll
        var searchPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Abstractions", "bin", "Release", "netstandard2.0", "Abstractions.dll"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Abstractions", "bin", "Debug", "netstandard2.0", "Abstractions.dll"),
        };
        
        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                refs.Add(MetadataReference.CreateFromFile(path));
                Console.WriteLine($"  Added reference: {Path.GetFileName(path)}");
                break;
            }
        }
    }

    private async Task PersistAsync(Compilation updated, ImmutableArray<Diagnostic> diags, GeneratorDriver driver)
    {
        // Filter and display diagnostics
        var relevantDiags = diags.Where(d => 
            d.Id.StartsWith("FSM") || 
            d.Severity == DiagnosticSeverity.Error ||
            (d.Id.StartsWith("CS") && d.Severity == DiagnosticSeverity.Warning)
        ).ToList();
        
        if (_outDir is null)
        {
            // Console output
            Console.WriteLine("============= DIAGNOSTICS =============");
            Console.WriteLine($"Total: {relevantDiags.Count} (Errors: {relevantDiags.Count(d => d.Severity == DiagnosticSeverity.Error)}, " +
                            $"Warnings: {relevantDiags.Count(d => d.Severity == DiagnosticSeverity.Warning)})");
            
            foreach (var d in relevantDiags.Take(20)) // Limit console output
                Console.WriteLine($"  [{d.Severity}] {d.Id}: {d.GetMessage()}");
            
            if (relevantDiags.Count > 20)
                Console.WriteLine($"  ... and {relevantDiags.Count - 20} more");

            Console.WriteLine("\n============= GENERATED FILES =============");
            var sources = driver.GetRunResult().Results
                .SelectMany(r => r.GeneratedSources)
                .ToList();
                
            if (sources.Count == 0)
            {
                Console.WriteLine("  (No files generated)");
            }
            else
            {
                foreach (var src in sources)
                {
                    Console.WriteLine($"\n--- {src.HintName} ---");
                    var content = src.SourceText.ToString();
                    // Show first 50 lines
                    var lines = content.Split('\n').Take(50);
                    foreach (var line in lines)
                        Console.WriteLine(line);
                    
                    var totalLines = content.Split('\n').Length;
                    if (totalLines > 50)
                        Console.WriteLine($"... ({totalLines - 50} more lines)");
                }
            }
            return;
        }

        // File output
        Directory.CreateDirectory(_outDir);
        
        // Save diagnostics
        var diagPath = Path.Combine(_outDir, "diagnostics.txt");
        await File.WriteAllLinesAsync(diagPath, relevantDiags.Select(d => d.ToString()));

        // Save generated files
        var fileCount = 0;
        foreach (var res in driver.GetRunResult().Results)
        {
            foreach (var src in res.GeneratedSources)
            {
                var path = Path.Combine(_outDir, src.HintName);
                await File.WriteAllTextAsync(path, src.SourceText.ToString());
                fileCount++;
            }
        }
        
        Console.WriteLine($"[OUTPUT] Saved {fileCount} generated file(s) and diagnostics to: {_outDir}");
    }

    public void Dispose() => UnloadGenerators();
}

internal sealed class CollectibleAlc : AssemblyLoadContext, IDisposable
{
    private readonly string _probingDir;
    
    public CollectibleAlc(string probingDir) : base($"GenTest.ALC.{Guid.NewGuid()}", isCollectible: true)
    {
        _probingDir = probingDir;
    }
    
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name!;
        // CRITICAL: Never load Microsoft.CodeAnalysis or System assemblies in ALC
        // This ensures type identity matches with the host's Roslyn
        if (name.StartsWith("Microsoft.CodeAnalysis") || name.StartsWith("System."))
            return null; // Let default context resolve -> maintains type identity
        
        // Try to load dependencies from the same directory as the generator
        var candidate = Path.Combine(_probingDir, name + ".dll");
        if (File.Exists(candidate))
        {
            return LoadFromAssemblyPath(candidate);
        }
        
        // Fall back to default context for remaining assemblies
        return null;
    }
    
    public void Dispose()
    {
        // Request unload (note: actual unload is asynchronous)
        Unload();
    }
}

// Simple analyzer config provider for generator options
internal sealed class SimpleAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly AnalyzerConfigOptions _globalOptions;

    public SimpleAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
    {
        _globalOptions = new SimpleAnalyzerConfigOptions(globalOptions);
    }

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;
    
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;
    
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _globalOptions;

    private sealed class SimpleAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public SimpleAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string? value)
        {
            return _options.TryGetValue(key, out value);
        }
    }
}