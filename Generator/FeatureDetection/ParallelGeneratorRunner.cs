using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Generator.Model;
using Generator.SourceGenerators;
using Generator.ModernGeneration;

namespace Generator.FeatureDetection
{
    /// <summary>
    /// Umożliwia równoległe generowanie kodu przez stary i nowy system
    /// oraz porównywanie wyników.
    /// </summary>
    public class ParallelGeneratorRunner
    {
        private readonly bool _enableModernGenerator;

        public ParallelGeneratorRunner(bool enableModernGenerator = false)
        {
            _enableModernGenerator = enableModernGenerator;
        }

        public GenerationResult GenerateBoth(StateMachineModel model)
        {
            // Zawsze generuj legacy kod
            var legacyCode = GenerateLegacy(model);

            // Modern generator na razie zwraca null (będziemy implementować w Milestone 1+)
            string modernCode = null;
            if (_enableModernGenerator)
            {
                try
                {
                    modernCode = GenerateModern(model);
                }
                catch (Exception ex)
                {
                    // Na razie łapiemy wyjątki, żeby nie zepsuć buildu
                    modernCode = $"// Modern generator failed: {ex.Message}";
                }
            }

            return new GenerationResult
            {
                LegacyCode = legacyCode,
                ModernCode = modernCode,
                Model = model
            };
        }

        private string GenerateLegacy(StateMachineModel model)
        {
            StateMachineCodeGenerator generator;

            generator = model.Variant switch
            {
                GenerationVariant.Full => new FullVariantGenerator(model),
                GenerationVariant.WithPayload => new PayloadVariantGenerator(model),
                GenerationVariant.WithExtensions => new ExtensionsVariantGenerator(model),
                _ => new CoreVariantGenerator(model)
            };

            return generator.Generate();
        }

        private string GenerateModern(StateMachineModel model)
        {
            var modernGenerator = new ModernGenerator(model);
            return modernGenerator.Generate();
        }

        public ComparisonResult Compare(string legacy, string modern)
        {
            if (string.IsNullOrEmpty(modern))
            {
                return new ComparisonResult { Status = ComparisonStatus.ModernNotGenerated };
            }

            var normalizedLegacy = NormalizeCode(legacy);
            var normalizedModern = NormalizeCode(modern);

            if (normalizedLegacy == normalizedModern)
            {
                return new ComparisonResult { Status = ComparisonStatus.Identical };
            }

            // Szczegółowa analiza różnic
            var result = new ComparisonResult { Status = ComparisonStatus.Different };

            // Porównaj using statements
            var legacyUsings = ExtractUsings(legacy);
            var modernUsings = ExtractUsings(modern);
            result.UsingsDiff = CompareCollections(legacyUsings, modernUsings);

            // Porównaj metody
            var legacyMethods = ExtractMethods(legacy);
            var modernMethods = ExtractMethods(modern);
            result.MethodsDiff = CompareCollections(legacyMethods.Keys, modernMethods.Keys);

            // Więcej analiz można dodać później

            return result;
        }

        private string NormalizeCode(string code)
        {
            // Usuń komentarze
            code = Regex.Replace(code, @"//.*$", "", RegexOptions.Multiline);
            code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Normalizuj whitespace
            code = Regex.Replace(code, @"\s+", " ");
            code = Regex.Replace(code, @"\s*{\s*", " { ");
            code = Regex.Replace(code, @"\s*}\s*", " } ");
            code = Regex.Replace(code, @"\s*;\s*", "; ");

            return code.Trim();
        }

        private HashSet<string> ExtractUsings(string code)
        {
            var usings = new HashSet<string>();
            var matches = Regex.Matches(code, @"using\s+([^;]+);");
            foreach (Match match in matches)
            {
                usings.Add(match.Groups[1].Value.Trim());
            }
            return usings;
        }

        private Dictionary<string, string> ExtractMethods(string code)
        {
            var methods = new Dictionary<string, string>();
            // Uproszczona ekstrakcja - w prawdziwej implementacji użyj Roslyn
            var matches = Regex.Matches(code, @"(public|private|protected|internal)\s+.*?\s+(\w+)\s*\([^)]*\)\s*{");
            foreach (Match match in matches)
            {
                var methodName = match.Groups[2].Value;
                methods[methodName] = match.Value;
            }
            return methods;
        }

        private CollectionDiff<T> CompareCollections<T>(IEnumerable<T> legacy, IEnumerable<T> modern)
        {
            var legacySet = new HashSet<T>(legacy);
            var modernSet = new HashSet<T>(modern);

            return new CollectionDiff<T>
            {
                OnlyInLegacy = legacySet.Except(modernSet).ToList(),
                OnlyInModern = modernSet.Except(legacySet).ToList(),
                InBoth = legacySet.Intersect(modernSet).ToList()
            };
        }
    }

    public class GenerationResult
    {
        public string LegacyCode { get; set; }
        public string ModernCode { get; set; }
        public StateMachineModel Model { get; set; }
    }

    public class ComparisonResult
    {
        public ComparisonStatus Status { get; set; }
        public CollectionDiff<string> UsingsDiff { get; set; }
        public CollectionDiff<string> MethodsDiff { get; set; }
        public List<string> Differences { get; set; } = [];

        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Comparison Status: {Status}");

            if (UsingsDiff != null && UsingsDiff.HasDifferences())
            {
                sb.AppendLine("Using differences:");
                if (UsingsDiff.OnlyInLegacy.Any())
                    sb.AppendLine($"  Only in legacy: {string.Join(", ", UsingsDiff.OnlyInLegacy)}");
                if (UsingsDiff.OnlyInModern.Any())
                    sb.AppendLine($"  Only in modern: {string.Join(", ", UsingsDiff.OnlyInModern)}");
            }

            if (MethodsDiff != null && MethodsDiff.HasDifferences())
            {
                sb.AppendLine("Method differences:");
                if (MethodsDiff.OnlyInLegacy.Any())
                    sb.AppendLine($"  Only in legacy: {string.Join(", ", MethodsDiff.OnlyInLegacy)}");
                if (MethodsDiff.OnlyInModern.Any())
                    sb.AppendLine($"  Only in modern: {string.Join(", ", MethodsDiff.OnlyInModern)}");
            }

            return sb.ToString();
        }
    }

    public class CollectionDiff<T>
    {
        public List<T> OnlyInLegacy { get; set; } = [];
        public List<T> OnlyInModern { get; set; } = [];
        public List<T> InBoth { get; set; } = [];

        public bool HasDifferences()
        {
            return OnlyInLegacy.Any() || OnlyInModern.Any();
        }
    }

    public enum ComparisonStatus
    {
        Identical,
        Different,
        ModernNotGenerated
    }
}
