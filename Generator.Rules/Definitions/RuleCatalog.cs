using System;
using System.Collections.Generic;
using System.Linq;

namespace Generator.Rules.Definitions
{
    /// <summary>
    /// Central, validated catalog of all rule definitions. Wraps <see cref="DefinedRules"/>.
    /// </summary>
    public static class RuleCatalog
    {
        private static readonly Dictionary<string, RuleDefinition> s_byId;

        static RuleCatalog()
        {
            // Eager init + validation: unique IDs and basic fields present.
            var all = DefinedRules.All ?? Array.Empty<RuleDefinition>();

            // Duplicate ID check
            var dupGroups = all
                .GroupBy(r => r.Id)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
                .ToArray();

            if (dupGroups.Length > 0)
            {
                var msg = "Duplicate rule IDs detected: " +
                          string.Join("; ", dupGroups.Select(g => $"{g.Key} ×{g.Count()} [{string.Join(", ", g.Select(x => x.Title))}]"));
                throw new InvalidOperationException(msg);
            }

            // Basic field sanity
            foreach (var r in all)
            {
                if (string.IsNullOrWhiteSpace(r.Id))
                    throw new InvalidOperationException("RuleDefinition.Id must be non-empty.");
                if (string.IsNullOrWhiteSpace(r.Title))
                    throw new InvalidOperationException($"Rule '{r.Id}' has empty Title.");
                if (string.IsNullOrWhiteSpace(r.MessageFormat))
                    throw new InvalidOperationException($"Rule '{r.Id}' has empty MessageFormat.");
                if (string.IsNullOrWhiteSpace(r.Category))
                    throw new InvalidOperationException($"Rule '{r.Id}' has empty Category.");
            }

            s_byId = all.ToDictionary(r => r.Id, r => r, StringComparer.Ordinal);
        }

        /// <summary>
        /// Returns the <see cref="RuleDefinition"/> for the given rule ID, or throws if unknown.
        /// </summary>
        public static RuleDefinition Get(string id)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));
            if (!s_byId.TryGetValue(id, out var def))
            {
                var known = string.Join(", ", s_byId.Keys.OrderBy(k => k, StringComparer.Ordinal));
                throw new KeyNotFoundException($"Unknown rule id '{id}'. Known: {known}.");
            }
            return def;
        }

        /// <summary>
        /// Tries to get a rule by ID.
        /// </summary>
        public static bool TryGet(string id, out RuleDefinition def)
        {
            if (id is null) { def = null!; return false; }
            return s_byId.TryGetValue(id, out def!);
        }

        /// <summary>
        /// Returns the immutable list of all rule definitions.
        /// </summary>
        public static IReadOnlyList<RuleDefinition> All => DefinedRules.All;
    }
}