using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Syntax.Test;

[Collection(GeneratorOutputCollection.Name)]
public class GeneratedCodeParityTests
{
    private readonly GeneratorOutputFixture _fixture;
    private readonly IReadOnlyList<MachinePair> _machinePairs;

    public GeneratedCodeParityTests(GeneratorOutputFixture fixture)
    {
        _fixture = fixture;
        _machinePairs = LoadPairs();
    }

    [Fact]
    public void Generated_code_should_match_for_all_pairs()
    {
        foreach (var pair in _machinePairs)
        {
            var fluentHint = pair.GetHintName(pair.FluentType);
            var legacyHint = pair.GetHintName(pair.LegacyType);

            Assert.True(_fixture.GeneratedSources.TryGetValue(fluentHint, out var fluentCode),
                $"Missing generated output for {pair.FluentType} (hint: {fluentHint}).");
            Assert.True(_fixture.GeneratedSources.TryGetValue(legacyHint, out var legacyCode),
                $"Missing generated output for {pair.LegacyType} (hint: {legacyHint}).");

            var normalizedFluent = pair.Normalize(fluentCode, pair.FluentType);
            var normalizedLegacy = pair.Normalize(legacyCode, pair.LegacyType);

            Assert.Equal(normalizedLegacy, normalizedFluent);
        }
    }

    private IReadOnlyList<MachinePair> LoadPairs()
    {
        var machineDir = Path.Combine(_fixture.SolutionRoot, "Machines.Tests", "Machines");
        var fluentFiles = Directory.GetFiles(machineDir, "*.Fluent.cs", SearchOption.TopDirectoryOnly);
        var pairs = new List<MachinePair>();

        foreach (var fluentFile in fluentFiles)
        {
            var baseName = Path.GetFileName(fluentFile).Replace(".Fluent.cs", string.Empty, StringComparison.Ordinal);
            var legacyPath = Path.Combine(machineDir, baseName + ".Legacy.cs");
            if (!File.Exists(legacyPath))
            {
                throw new InvalidOperationException($"Cannot find legacy counterpart for {fluentFile}.");
            }

            var fluentType = ExtractPrimaryClassName(fluentFile);
            var legacyType = ExtractPrimaryClassName(legacyPath);
            pairs.Add(new MachinePair(baseName, fluentType, legacyType));
        }

        pairs.Sort((a, b) => string.CompareOrdinal(a.BaseName, b.BaseName));
        return pairs;
    }

    private static string ExtractPrimaryClassName(string path)
    {
        var content = File.ReadAllText(path);
        var match = Regex.Match(content, "class\\s+(\\w+)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Cannot determine class name for {path}.");
        }

        return match.Groups[1].Value;
    }

    public record MachinePair(string BaseName, string FluentType, string LegacyType)
    {
        public string GetHintName(string typeName)
            => $"global__FastFsm.Tests.Machines.{typeName}.Generated.cs";

        public string Normalize(string source, string typeName)
        {
            var normalized = source;
            var replacements = BuildReplacementMap(typeName);
            foreach (var replacement in replacements)
            {
                normalized = normalized.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
            }

            normalized = normalized.Replace("@", string.Empty, StringComparison.Ordinal);

            var markers = new[]
            {
                "\nOriginal StateMachineParser Model:",
                "\nFluentParser Model:"
            };

            foreach (var marker in markers)
            {
                var markerIndex = normalized.IndexOf(marker, StringComparison.Ordinal);
                if (markerIndex >= 0)
                {
                    normalized = normalized.Substring(0, markerIndex);
                    break;
                }
            }

            return normalized.Trim();
        }

        private Dictionary<string, string> BuildReplacementMap(string typeName)
        {
            var targetName = BaseName;
            var map = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [$"I{typeName}"] = $"I{targetName}",
                [typeName] = targetName
            };

            if (typeName.EndsWith("Fluent", StringComparison.Ordinal))
            {
                var withoutSuffix = typeName[..^"Fluent".Length];
                map[$"I{withoutSuffix}"] = $"I{targetName}";
                map[withoutSuffix] = targetName;
            }

            if (typeName.EndsWith("Legacy", StringComparison.Ordinal))
            {
                var withoutSuffix = typeName[..^"Legacy".Length];
                map[$"I{withoutSuffix}"] = $"I{targetName}";
                map[withoutSuffix] = targetName;
            }

            return map;
        }
    }
}
