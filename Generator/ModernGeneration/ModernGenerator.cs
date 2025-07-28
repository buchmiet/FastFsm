using System;
using Generator.Model;
using Generator.ModernGeneration.Context;
using Generator.SourceGenerators;

namespace Generator.ModernGeneration
{
    /// <summary>
    /// Nowy generator używający modularnej architektury.
    /// W Milestone 1 jest tylko wrapperem na legacy generator.
    /// </summary>
    public class ModernGenerator
    {
        private readonly StateMachineModel _model;

        public ModernGenerator(StateMachineModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public string Generate()
        {
            // Milestone 1: Używamy kontekstu, ale delegujemy do legacy
            var context = new GenerationContext(_model);

            // Na razie używamy legacy generatorów
            return GenerateViaLegacy();
        }

        private string GenerateViaLegacy()
        {
            StateMachineCodeGenerator generator;

            generator = _model.Variant switch
            {
                GenerationVariant.Full => new FullVariantGenerator(_model),
                GenerationVariant.WithPayload => new PayloadVariantGenerator(_model),
                GenerationVariant.WithExtensions => new ExtensionsVariantGenerator(_model),
                _ => new CoreVariantGenerator(_model)
            };

            return generator.Generate();
        }
    }
}