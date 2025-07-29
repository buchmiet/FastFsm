using System;
using System.Collections.Generic;
using Generator.Model;
using Generator.ModernGeneration.Features;
using Generator.ModernGeneration.Features.Shared;
using Generator.ModernGeneration.Policies;

namespace Generator.ModernGeneration
{
    /// <summary>
    /// Wybiera odpowiednie moduły na podstawie konfiguracji modelu.
    /// </summary>
    public static class FeatureCatalog
    {
        public static List<IFeatureModule> SelectModules(StateMachineModel model)
        {
            var modules = new List<IFeatureModule>();

            // Core feature - zawsze wymagany
            modules.Add(new CoreFeature());

            // Payload feature - dla WithPayload i Full
            var payloadFeature = PayloadHelper.CreatePayloadFeature(model);
            if (payloadFeature != null)
            {
                modules.Add(payloadFeature);
            }

            // TODO: W Milestone 4
            // if (model.Variant == GenerationVariant.WithExtensions || model.Variant == GenerationVariant.Full)
            // {
            //     modules.Add(new ExtensionsFeature());
            // }

            // TODO: W Milestone 4
            // if (model.GenerateLogging)
            // {
            //     modules.Add(new LoggingFeature());
            // }

            return modules;
        }

        public static (IAsyncPolicy asyncPolicy, IGuardPolicy guardPolicy, IHookDispatchPolicy hookPolicy)
            CreatePolicies(StateMachineModel model)
        {
            // Async policy
            IAsyncPolicy asyncPolicy = model.GenerationConfig.IsAsync
                ? new AsyncPolicyAsync(model.ContinueOnCapturedContext)
                : new AsyncPolicySync();

            // Guard policy - uniwersalna
            IGuardPolicy guardPolicy = new GuardPolicy();

            // Hook policy - TODO w Milestone 4
            IHookDispatchPolicy hookPolicy = new NoOpHookDispatchPolicy(); // Tymczasowa implementacja

            return (asyncPolicy, guardPolicy, hookPolicy);
        }
    }

    /// <summary>
    /// Tymczasowa implementacja HookDispatchPolicy która nic nie robi.
    /// </summary>
    internal class NoOpHookDispatchPolicy : IHookDispatchPolicy
    {
        // Pusta implementacja - zostanie zastąpiona w Milestone 4
    }
}