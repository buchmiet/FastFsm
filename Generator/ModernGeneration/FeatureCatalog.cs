using System;
using System.Collections.Generic;
using Generator.Model;
using Generator.ModernGeneration.Features;

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

            // Na razie tylko Core dla Pure/Basic
            if (model.Variant == GenerationVariant.Pure || model.Variant == GenerationVariant.Basic)
            {
                modules.Add(new CoreFeature());
            }

            // TODO: Dodać więcej modułów w kolejnych milestone'ach
            // if (model.Variant == GenerationVariant.WithPayload || model.Variant == GenerationVariant.Full)
            // {
            //     modules.Add(new PayloadFeature());
            // }

            return modules;
        }
    }
}
