using System;
using System.Collections.Generic;
using System.Text;

namespace Generator.Model.Dtos
{
    /// <summary>
    /// Primary data model for FactoryCodeGenerator: preprocessed type info so the generator
    /// does not need TypeSystemHelper at emission time.
    /// </summary>
    public sealed record FactoryGenerationModel
    {
        // Type info
        public TypeGenerationInfo StateType { get; set; } = new();
        public TypeGenerationInfo TriggerType { get; set; } = new();
        public TypeGenerationInfo? PayloadType { get; set; } // May be absent

        // From the original StateMachineModel
        public string ClassName { get; set; } = "";
        public string? UserNamespace { get; set; }
        public bool ShouldGenerateLogging { get; set; }
        public bool HasExtensions { get; set; }
        // Pre-calculated flags
        public bool IsSinglePayload { get; set; }

        /// <summary>
        /// Unique set of namespaces required in the generated file.
        /// </summary>
        public IReadOnlyCollection<string> AllRequiredNamespaces { get; set; } = Array.Empty<string>();
    }
}
