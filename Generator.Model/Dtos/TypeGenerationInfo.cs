using System;
using System.Collections.Generic;
using System.Text;

namespace Generator.Model.Dtos
{
    public sealed record TypeGenerationInfo
    {
        /// <summary>
        /// Nazwa typu sformatowana do użycia w kodzie (np. "string", "List<int>").
        /// Wynik: TypeSystemHelper.FormatTypeForUsage()
        /// </summary>
        public string UsageName { get; set; }

        /// <summary>
        /// Nazwa typu sformatowana do użycia w typeof() (np. "global::System.String", "List<>").
        /// Wynik: TypeSystemHelper.FormatForTypeof()
        /// </summary>
        public string TypeOfName { get; set; }

        /// <summary>
        /// Prosta nazwa typu bez przestrzeni nazw (np. "String", "List").
        /// Wynik: TypeSystemHelper.GetSimpleTypeName()
        /// </summary>
        public string SimpleName { get; set; }

        /// <summary>
        /// Przestrzenie nazw wymagane przez ten typ i jego argumenty generyczne.
        /// Wynik: TypeSystemHelper.GetRequiredNamespaces()
        /// </summary>
        public IEnumerable<string> RequiredNamespaces { get; set; }
    }
}
