using System;
using Generator.Model;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Features;

namespace Generator.ModernGeneration.Features.Shared
{
    /// <summary>
    /// Wspólne metody pomocnicze dla modułów payload.
    /// </summary>
    public static class PayloadHelper
    {
        /// <summary>
        /// Określa czy model wymaga modułu payload.
        /// </summary>
        public static bool RequiresPayloadModule(StateMachineModel model)
        {
            return model.GenerationConfig.HasPayload ||
                   model.Variant == GenerationVariant.WithPayload ||
                   model.Variant == GenerationVariant.Full;
        }

        /// <summary>
        /// Tworzy odpowiedni moduł payload na podstawie modelu.
        /// </summary>
        public static IPayloadFeature? CreatePayloadFeature(StateMachineModel model)
        {
            if (!RequiresPayloadModule(model))
                return null;

            // Multi-payload jeśli mamy mapę trigger->type
            // POPRAWKA: Dodano sprawdzenie null
            if (model.TriggerPayloadTypes?.Count > 0)
            {
                return new MultiPayloadFeature();
            }

            // Single-payload jeśli mamy domyślny typ
            if (!string.IsNullOrEmpty(model.DefaultPayloadType))
            {
                return new SinglePayloadFeature(model.DefaultPayloadType);
            }

            // Fallback - single payload z object
            return new SinglePayloadFeature("object");
        }

        /// <summary>
        /// Sprawdza czy metoda (guard/action/onEntry/onExit) wymaga payloadu.
        /// </summary>
        public static bool MethodRequiresPayload(
            TransitionModel? transition,
            StateModel? state,
            string methodName,
            MethodType methodType)
        {
            return methodType switch
            {
                MethodType.Guard => transition?.GuardExpectsPayload ?? false,
                MethodType.Action => transition?.ActionExpectsPayload ?? false,
                MethodType.OnEntry => state?.OnEntryExpectsPayload ?? false,
                MethodType.OnExit => state?.OnExitExpectsPayload ?? false,
                _ => false
            };
        }

        /// <summary>
        /// Sprawdza czy metoda ma overload bezparametrowy.
        /// </summary>
        public static bool MethodHasParameterlessOverload(
            TransitionModel? transition,
            StateModel? state,
            MethodType methodType)
        {
            return methodType switch
            {
                MethodType.Guard => transition?.GuardHasParameterlessOverload ?? false,
                MethodType.Action => transition?.ActionHasParameterlessOverload ?? false,
                MethodType.OnEntry => state?.OnEntryHasParameterlessOverload ?? false,
                MethodType.OnExit => state?.OnExitHasParameterlessOverload ?? false,
                _ => false
            };
        }

        /// <summary>
        /// Pobiera nazwę metody.
        /// </summary>
        public static string? GetMethodName(
            TransitionModel? transition,
            StateModel? state,
            MethodType methodType)
        {
            return methodType switch
            {
                MethodType.Guard => transition?.GuardMethod,
                MethodType.Action => transition?.ActionMethod,
                MethodType.OnEntry => state?.OnEntryMethod,
                MethodType.OnExit => state?.OnExitMethod,
                _ => null
            };
        }

        /// <summary>
        /// Sprawdza czy metoda jest asynchroniczna.
        /// </summary>
        public static bool IsMethodAsync(
            TransitionModel? transition,
            StateModel? state,
            MethodType methodType)
        {
            return methodType switch
            {
                MethodType.Guard => transition?.GuardIsAsync ?? false,
                MethodType.Action => transition?.ActionIsAsync ?? false,
                MethodType.OnEntry => state?.OnEntryIsAsync ?? false,
                MethodType.OnExit => state?.OnExitIsAsync ?? false,
                _ => false
            };
        }
    }

    /// <summary>
    /// Typ metody callback w maszynie stanów.
    /// </summary>
    public enum MethodType
    {
        Guard,
        Action,
        OnEntry,
        OnExit
    }
}