using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Generator;
using Xunit;

namespace Syntax.Test;

public sealed class GeneratorOutputFixture
{
    public IReadOnlyDictionary<string, string> GeneratedSources { get; }

    public string SolutionRoot { get; }

    public GeneratorOutputFixture()
    {
        SolutionRoot = LocateSolutionRoot();

        var sourceFiles = CollectSourceFiles();
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTrees = sourceFiles.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path));

        var references = BuildMetadataReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName: "Syntax.Tests.SourceGeneration",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new StateMachineGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(ToSourceGenerator(generator));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

        Assert.True(diagnostics.All(d => d.Severity < DiagnosticSeverity.Error),
            string.Join(Environment.NewLine, diagnostics));
        Assert.True(!updatedCompilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error),
            string.Join(Environment.NewLine, updatedCompilation.GetDiagnostics()));

        var runResult = driver.GetRunResult();
        GeneratedSources = runResult.Results
            .SelectMany(result => result.GeneratedSources)
            .ToDictionary(source => source.HintName, source => source.SourceText.ToString());
    }

    private IEnumerable<string> CollectSourceFiles()
    {
        var machineRoot = Path.Combine(SolutionRoot, "Machines.Tests");
        var files = Directory.GetFiles(machineRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                        && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .ToList();

        // Include shared runtime file that is linked into Machines.Tests but lives in FastFsm project.
        files.Add(Path.Combine(SolutionRoot, "FastFsm", "Runtime", "Extensions", "ExtensionRunner.cs"));
        return files;
    }

    private static ISourceGenerator ToSourceGenerator(IIncrementalGenerator generator)
    {
        var generatorExtensions = typeof(GeneratorExtensions);
        var method = generatorExtensions.GetMethod(
            "AsSourceGenerator",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(IIncrementalGenerator) },
            modifiers: null);

        if (method == null)
        {
            throw new InvalidOperationException("Unable to locate GeneratorExtensions.AsSourceGenerator.");
        }

        return (ISourceGenerator)method.Invoke(null, new object[] { generator })!;
    }

    private IReadOnlyList<MetadataReference> BuildMetadataReferences()
    {
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            typeof(object).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(Enumerable).Assembly.Location,
            typeof(List<>).Assembly.Location,
            typeof(Abstractions.Attributes.StateMachineAttribute).Assembly.Location,
            typeof(Abstractions.Fluent.FSM).Assembly.Location,
            typeof(FastFsm.Runtime.StateMachineBase<,>).Assembly.Location,
            typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location
        };

        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimeDir is not null)
        {
            var netstandard = Path.Combine(runtimeDir, "netstandard.dll");
            if (File.Exists(netstandard)) references.Add(netstandard);
            var systemRuntime = Path.Combine(runtimeDir, "System.Runtime.dll");
            if (File.Exists(systemRuntime)) references.Add(systemRuntime);
            var systemCollections = Path.Combine(runtimeDir, "System.Collections.dll");
            if (File.Exists(systemCollections)) references.Add(systemCollections);
        }


        return references.Select(path => MetadataReference.CreateFromFile(path)).ToList();
    }

    private static string LocateSolutionRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current, "FastFsm.Net.slnx")))
            {
                return current;
            }

            current = Path.GetDirectoryName(current);
        }

        throw new InvalidOperationException("Cannot locate solution root (FastFsm.Net.slnx).");
    }
}

[CollectionDefinition(Name)]
public sealed class GeneratorOutputCollection : ICollectionFixture<GeneratorOutputFixture>
{
    public const string Name = "GeneratorOutput";
}
