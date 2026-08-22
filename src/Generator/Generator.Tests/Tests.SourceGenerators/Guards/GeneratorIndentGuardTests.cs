using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Tests.SourceGenerators.Guards;

public sealed class GeneratorIndentGuardTests
{
    private static readonly Regex ManualIndentVariable =
        new(@"var\s+indent\s*=\s*""[ ]+""", RegexOptions.CultureInvariant);

    private static readonly Regex IndentInterpolation =
        new(@"\$\{indent\}", RegexOptions.CultureInvariant);

    [Fact]
    public void Generator_source_has_no_manual_indent_prefix_variables()
    {
        var solutionDir = GetSolutionDir();
        Assert.NotNull(solutionDir);

        var generatorRoot = Path.Combine(solutionDir!, "src", "Generator");
        Assert.True(Directory.Exists(generatorRoot), $"Generator root not found: {generatorRoot}");

        var violations = new List<string>();
        foreach (var path in Directory.EnumerateFiles(generatorRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}Generator.Tests{Path.DirectorySeparatorChar}"))
                continue;

            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal))
                    continue;

                if (ManualIndentVariable.IsMatch(line) || IndentInterpolation.IsMatch(line))
                    violations.Add($"{path}:{i + 1}: {line.Trim()}");
            }
        }

        Assert.Empty(violations);
    }

    private static string? GetSolutionDir()
    {
        var current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        for (var i = 0; i < 10; i++)
        {
            if (Directory.GetFiles(current, "*.slnx").Length > 0
                || Directory.GetFiles(current, "*.sln").Length > 0)
                return current;

            var parent = Directory.GetParent(current);
            if (parent is null)
                break;

            current = parent.FullName;
        }

        return null;
    }
}
