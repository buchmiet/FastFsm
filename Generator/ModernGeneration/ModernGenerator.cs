using System;
using System.Text;
using Generator.Model;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Director;
using Generator.ModernGeneration.Policies;
using Generator.SourceGenerators;

namespace Generator.ModernGeneration
{
    /// <summary>
    /// Nowy generator używający modularnej architektury.
    /// </summary>
    public class ModernGenerator
    {
        private readonly StateMachineModel _model;
        private readonly GenerationContext _context;

        public ModernGenerator(StateMachineModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            
            // Inicjalizacja kontekstu
            _context = new GenerationContext(model);
            
            // Ustawienie polityk
            var asyncPolicy = model.GenerationConfig.IsAsync 
                ? (IAsyncPolicy)new AsyncPolicyAsync(model.ContinueOnCapturedContext) 
                : new AsyncPolicySync();
            
            // Na razie tylko AsyncPolicy, reszta w kolejnych milestone'ach
            var guardPolicy = new StubGuardPolicy();
            var hookPolicy = new StubHookDispatchPolicy();
            
            _context.SetPolicies(asyncPolicy, guardPolicy, hookPolicy);
        }

        public string Generate()
        {
            // Dla Pure i Basic używamy nowego systemu
            if (_model.Variant == GenerationVariant.Pure || _model.Variant == GenerationVariant.Basic)
            {
                return GenerateViaDirector();
            }

            // Reszta wariantów nadal przez legacy
            return LegacyAdapter.GenerateViaContext(_context);
        }

        private string GenerateViaDirector()
        {
            var director = new Director.Director(_context);

            // Wybierz moduły
            var modules = FeatureCatalog.SelectModules(_model);
            
            // Zarejestruj moduły
            foreach (var module in modules)
            {
                director.RegisterModule(module);
            }

            // Generuj kod
            return director.Generate();
        }
    }

    // Tymczasowy adapter do legacy kodu
    internal static class LegacyAdapter
    {
        public static string GenerateViaContext(GenerationContext ctx)
        {
            var model = ctx.Model;
            
            StateMachineCodeGenerator generator = model.Variant switch
            {
                GenerationVariant.Full => new FullVariantGenerator(model),
                GenerationVariant.WithPayload => new PayloadVariantGenerator(model),
                GenerationVariant.WithExtensions => new ExtensionsVariantGenerator(model),
                _ => new CoreVariantGenerator(model)
            };

            return generator.Generate();
        }
    }

    // Tymczasowe implementacje stub
    internal class StubGuardPolicy : IGuardPolicy { }
    internal class StubHookDispatchPolicy : IHookDispatchPolicy { }
}
