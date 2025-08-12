using System.Collections.Generic;

namespace Generator.Rules.Definitions
{
    /// <summary>
    /// Stable facade for accessing rule definitions. Wraps <see cref="RuleCatalog"/>.
    /// Prefer using this type from external code to avoid direct dependency on the catalog internals.
    /// </summary>
    public static class RuleLookup
    {
        /// <summary>Gets a rule definition by ID or throws if unknown.</summary>
        public static RuleDefinition Get(string id) => RuleCatalog.Get(id);

        /// <summary>Tries to get a rule definition by ID.</summary>
        public static bool TryGet(string id, out RuleDefinition def) => RuleCatalog.TryGet(id, out def!);

        /// <summary>Returns the immutable list of all rule definitions.</summary>
        public static IReadOnlyList<RuleDefinition> All => RuleCatalog.All;
    }
}
