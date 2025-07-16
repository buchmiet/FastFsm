using System;
using System.Collections.Generic;
using System.Text;

namespace Generator.Model.Dtos
{
    /// <summary>
    /// Główny model danych dla FactoryCodeGenerator, zawierający wszystkie wstępnie
    /// przetworzone informacje, eliminując potrzebę posiadania TypeSystemHelper w generatorze.
    /// </summary>
    public sealed record FactoryGenerationModel
    {
        // Informacje o typach
        public TypeGenerationInfo StateType { get; set; }
        public TypeGenerationInfo TriggerType { get; set; }
        public TypeGenerationInfo? PayloadType { get; set; } // Może nie istnieć

        // Informacje z oryginalnego StateMachineModel
        public string ClassName { get; set; }
        public string? UserNamespace { get; set; }
        public bool ShouldGenerateLogging { get; set; }
        public GenerationVariant Variant { get; set; } // Wymaga przeniesienia enum GenerationVariant

        // Pre-kalkulowane flagi
        public bool IsSinglePayloadVariant { get; set; }

        /// <summary>
        /// Zbiorcza, unikalna lista wszystkich przestrzeni nazw potrzebnych w generowanym pliku.
        /// </summary>
        public IReadOnlyCollection<string> AllRequiredNamespaces { get; set; }
    }
}
