using System;
using System.Collections.Generic;
using System.Text;

namespace Generator.Model.Dtos
{
    public sealed record TypeGenerationInfo
    {
        /// <summary>
        /// Type name formatted for use in code (e.g. "string", "List&lt;int&gt;").
        /// From TypeSystemHelper.FormatTypeForUsage().
        /// </summary>
        public string UsageName { get; set; } = "";

        /// <summary>
        /// Type name formatted for typeof() (e.g. "global::System.String", "List&lt;&gt;").
        /// From TypeSystemHelper.FormatForTypeof().
        /// </summary>
        public string TypeOfName { get; set; } = "";

        /// <summary>
        /// Simple type name without namespace (e.g. "String", "List").
        /// From TypeSystemHelper.GetSimpleTypeName().
        /// </summary>
        public string SimpleName { get; set; } = "";

        /// <summary>
        /// Namespaces required by this type and its generic arguments.
        /// From TypeSystemHelper.GetRequiredNamespaces().
        /// </summary>
        public IEnumerable<string> RequiredNamespaces { get; set; } = Array.Empty<string>();
    }
}
