using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Model;

namespace Generator.FeatureDetection
{
    /// <summary>
    /// Analizuje model maszyny stanów i wykrywa użyte funkcjonalności.
    /// Jest to pojedyncze źródło prawdy o tym, jakie cechy ma maszyna.
    /// </summary>
    public class FeatureDetector
    {
        public FeatureSet Detect(StateMachineModel model)
        {
            var hasOnEntryExit = DetectOnEntryExit(model);
            var hasGuards = DetectGuards(model);
            var hasActions = DetectActions(model);
            var hasInternalTransitions = DetectInternalTransitions(model);

            // Szczegóły async
            var asyncGuards = new HashSet<string>(
                model.Transitions
                     .Where(t => !string.IsNullOrEmpty(t.GuardMethod) && t.GuardIsAsync)
                     .Select(t => t.GuardMethod)
            );

            var asyncActions = new HashSet<string>(
                model.Transitions
                     .Where(t => !string.IsNullOrEmpty(t.ActionMethod) && t.ActionIsAsync)
                     .Select(t => t.ActionMethod)
            );

            var asyncOnEntry = new HashSet<string>(
                model.States.Values
                     .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryIsAsync)
                     .Select(s => s.OnEntryMethod)
            );

            var asyncOnExit = new HashSet<string>(
                model.States.Values
                     .Where(s => !string.IsNullOrEmpty(s.OnExitMethod) && s.OnExitIsAsync)
                     .Select(s => s.OnExitMethod)
            );

            var isAsync = model.GenerationConfig.IsAsync
                          || asyncGuards.Count > 0
                          || asyncActions.Count > 0
                          || asyncOnEntry.Count > 0
                          || asyncOnExit.Count > 0;

            return new FeatureSet
            {
                // Podstawowe cechy
                HasOnEntryExit = hasOnEntryExit,
                HasPayload = model.GenerationConfig.HasPayload,
                HasMultiPayload = model.TriggerPayloadTypes.Any(),
                HasExtensions = model.GenerationConfig.HasExtensions,
                HasLogging = model.GenerateLogging,
                IsAsync = isAsync,

                // Szczegółowe cechy
                HasGuards = hasGuards,
                HasActions = hasActions,
                HasInternalTransitions = hasInternalTransitions,

                // Szczegóły async
                AsyncGuards = asyncGuards,
                AsyncActions = asyncActions,
                AsyncOnEntry = asyncOnEntry,
                AsyncOnExit = asyncOnExit,

                // Szczegóły payload
                TriggerPayloadTypes = model.TriggerPayloadTypes,
                DefaultPayloadType = model.DefaultPayloadType,

                // Statystyki
                StateCount = model.States.Count,
                TransitionCount = model.Transitions.Count,
                GuardCount = model.Transitions.Count(t => !string.IsNullOrEmpty(t.GuardMethod)),
                ActionCount = model.Transitions.Count(t => !string.IsNullOrEmpty(t.ActionMethod))
            };
        }

        private bool DetectOnEntryExit(StateMachineModel model)
        {
            return model.States.Values.Any(s =>
                !string.IsNullOrEmpty(s.OnEntryMethod) ||
                !string.IsNullOrEmpty(s.OnExitMethod));
        }

        private bool DetectGuards(StateMachineModel model)
        {
            return model.Transitions.Any(t => !string.IsNullOrEmpty(t.GuardMethod));
        }

        private bool DetectActions(StateMachineModel model)
        {
            return model.Transitions.Any(t => !string.IsNullOrEmpty(t.ActionMethod));
        }

        private bool DetectInternalTransitions(StateMachineModel model)
        {
            return model.Transitions.Any(t => t.IsInternal);
        }
    }
}
