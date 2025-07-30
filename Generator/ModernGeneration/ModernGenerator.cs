using System;
using System.Linq;
using Generator.Model;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Director;
using Generator.ModernGeneration.Features;
using Generator.SourceGenerators;

namespace Generator.ModernGeneration
{
    /// <summary>
    /// Nowy generator używający modularnej architektury.
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
            var context = new GenerationContext(_model);
            var director = new Director.Director(context);

            // Rejestruj moduły w zależności od wariantu
            RegisterModules(director);

            return director.Generate();
        }

        private void RegisterModules(Director.Director director)
        {
            // Zawsze rejestruj CoreFeature
            director.RegisterModule(new CoreFeature());

            // Dodaj moduły w zależności od wariantu
            switch (_model.Variant)
            {
                case GenerationVariant.Pure:
                    // Tylko Core
                    break;

                case GenerationVariant.Basic:
                    // Core + OnEntry/OnExit (CoreFeature już to obsługuje)
                    break;

                case GenerationVariant.WithPayload:
                    RegisterPayloadModules(director);
                    break;

                case GenerationVariant.WithExtensions:
                    // Core + Extensions (TODO w przyszłości)
                    // director.RegisterModule(new ExtensionsFeature());
                    break;

                case GenerationVariant.Full:
                    RegisterPayloadModules(director);
                    // TODO: Extensions
                    // director.RegisterModule(new ExtensionsFeature());
                    break;
            }

            // TODO: Dodaj LoggingFeature jeśli włączone
            // if (_model.GenerateLogging)
            // {
            //     director.RegisterModule(new LoggingFeature());
            // }
        }

        private void RegisterPayloadModules(Director.Director director)
        {
            if (_model.TriggerPayloadTypes.Any())
            {
                // Multi-payload
                director.RegisterModule(new MultiPayloadFeature());
            }
            else if (_model.DefaultPayloadType != null)
            {
                // Single-payload
                director.RegisterModule(new SinglePayloadFeature(_model.DefaultPayloadType));
            }
        }
    }
}