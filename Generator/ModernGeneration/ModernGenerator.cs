using System;
using Generator.Model;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Director;
using Generator.SourceGenerators;

namespace Generator.ModernGeneration
{
    /// <summary>
    /// Nowy generator używający modularnej architektury.
    /// W Milestone 3 obsługuje Pure, Basic i WithPayload.
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
            // Sprawdź czy możemy obsłużyć ten wariant
            if (CanHandleVariant(_model.Variant))
            {
                return GenerateViaModern();
            }

            // Fallback do legacy dla nieobsługiwanych wariantów
            return GenerateViaLegacy();
        }

        private bool CanHandleVariant(GenerationVariant variant)
        {
            // W Milestone 3 obsługujemy Pure, Basic i WithPayload
            return variant == GenerationVariant.Pure ||
                   variant == GenerationVariant.Basic ||
                   variant == GenerationVariant.WithPayload;
        }

        private string GenerateViaModern()
        {
            // Tworzenie kontekstu
            var context = new GenerationContext(_model);

            // Tworzenie polityk
            var (asyncPolicy, guardPolicy, hookPolicy) = FeatureCatalog.CreatePolicies(_model);
            context.SetPolicies(asyncPolicy, guardPolicy, hookPolicy);

            // Wybór modułów
            var modules = FeatureCatalog.SelectModules(_model);
            context.RegisterModules(modules);

            // Tworzenie directora
            var director = new Director.Director(context);

            // Rejestracja modułów w directorze
            foreach (var module in modules)
            {
                director.RegisterModule(module);
            }

            // Generowanie kodu
            return director.Generate();
        }

        private string GenerateViaLegacy()
        {
            StateMachineCodeGenerator generator;

            generator = _model.Variant switch
            {
                GenerationVariant.Full => new FullVariantGenerator(_model),
                GenerationVariant.WithExtensions => new ExtensionsVariantGenerator(_model),
                _ => new CoreVariantGenerator(_model)
            };

            return generator.Generate();
        }
    }
}