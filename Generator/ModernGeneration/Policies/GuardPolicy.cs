// File: ModernGeneration/Policies/GuardPolicy.cs
using System;
using Generator.Infrastructure;
using Generator.Model;
using Generator.ModernGeneration.Context;

namespace Generator.ModernGeneration.Policies
{
    /// <summary>
    /// Implementacja polityki guardów - obsługuje wszystkie kombinacje:
    /// - sync/async
    /// - z payloadem/bez
    /// - z overloadem bezparametrowym/bez
    /// - z obsługą wyjątków/bez
    /// </summary>
    public sealed class GuardPolicy : IGuardPolicy
    {
        private readonly TypeSystemHelper _typeHelper = new();

        public void EmitGuardCheck(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr,
            bool throwOnException)
        {
            var sb = sctx.Sb;
            var asyncPolicy = sctx.Root.AsyncPolicy;

            // Jeśli brak guarda - zawsze true
            if (string.IsNullOrEmpty(transition.GuardMethod))
            {
                sb.AppendLine($"bool {resultVar} = true;");
                return;
            }

            // Deklaracja zmiennej wyniku
            sb.AppendLine($"bool {resultVar};");

            // Obsługa try-catch jeśli nie propagujemy wyjątków
            if (!throwOnException)
            {
                sb.AppendLine("try");
                using (sb.Block(""))
                {
                    EmitGuardCallBody(sctx, transition, resultVar, payloadExpr);
                }
                sb.AppendLine("catch");
                using (sb.Block(""))
                {
                    sb.AppendLine($"{resultVar} = false;");
                }
            }
            else
            {
                EmitGuardCallBody(sctx, transition, resultVar, payloadExpr);
            }
        }

        public void EmitGuardCheckForPermitted(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr)
        {
            // Dla GetPermittedTriggers - zawsze łapiemy wyjątki
            EmitGuardCheck(sctx, transition, resultVar, payloadExpr, throwOnException: false);
        }

        public void EmitGuardCheckForCanFire(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr)
        {
            var sb = sctx.Sb;

            // Dla CanFire - uproszczona wersja, zawsze łapiemy wyjątki
            if (string.IsNullOrEmpty(transition.GuardMethod))
            {
                sb.AppendLine("return true;");
                return;
            }

            // W CanFire często zwracamy bezpośrednio
            sb.AppendLine("try");
            using (sb.Block(""))
            {
                // Sprawdź czy guard oczekuje payloadu ale nie mamy go (payloadExpr == "null")
                if (transition.GuardExpectsPayload && payloadExpr == "null")
                {
                    if (transition.GuardHasParameterlessOverload)
                    {
                        // Wywołaj wersję bezparametrową
                        var methodCall = BuildMethodCall(sctx, transition, null);
                        sb.AppendLine($"return {methodCall};");
                    }
                    else
                    {
                        // Guard wymaga payloadu którego nie mamy
                        sb.AppendLine("return false;");
                    }
                }
                else
                {
                    // Normalna logika
                    var guardExpr = BuildGuardExpression(sctx, transition, payloadExpr);
                    sb.AppendLine($"return {guardExpr};");
                }
            }
            sb.AppendLine("catch");
            using (sb.Block(""))
            {
                sb.AppendLine("return false;");
            }
        }

        private void EmitGuardCallBody(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr)
        {
            var sb = sctx.Sb;

            // Sprawdź czy guard wymaga payloadu ale go nie ma
            if (transition.GuardExpectsPayload && payloadExpr == "null" && !transition.GuardHasParameterlessOverload)
            {
                sb.AppendLine($"{resultVar} = false; // guard expects payload but none provided");
                return;
            }

            // Buduj wywołanie w zależności od wariantu
            if (transition.GuardExpectsPayload && transition.GuardHasParameterlessOverload)
            {
                // Guard z payloadem + overload bezparametrowy
                EmitGuardWithOptionalPayload(sctx, transition, resultVar, payloadExpr);
            }
            else if (transition.GuardExpectsPayload)
            {
                // Guard tylko z payloadem (bez overloadu)
                EmitGuardWithRequiredPayload(sctx, transition, resultVar, payloadExpr);
            }
            else
            {
                // Guard bez parametrów
                EmitParameterlessGuard(sctx, transition, resultVar);
            }
        }

        private void EmitParameterlessGuard(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar)
        {
            var sb = sctx.Sb;
            var asyncPolicy = sctx.Root.AsyncPolicy;
            var methodCall = $"{transition.GuardMethod}()";

            if (sctx.IsAsync && transition.GuardIsAsync)
            {
                sb.AppendLine($"{resultVar} = {asyncPolicy.AwaitKeyword(true)}{methodCall}{asyncPolicy.ConfigureAwait()};");
            }
            else
            {
                sb.AppendLine($"{resultVar} = {methodCall};");
            }
        }

        private void EmitGuardWithRequiredPayload(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr)
        {
            var sb = sctx.Sb;
            var asyncPolicy = sctx.Root.AsyncPolicy;
            var payloadType = GetPayloadType(transition);

            // Pattern: result = payload is T typed && Guard(typed)
            string guardCall;
            if (sctx.IsAsync && transition.GuardIsAsync)
            {
                guardCall = $"{asyncPolicy.AwaitKeyword(true)}{transition.GuardMethod}(typedGuardPayload){asyncPolicy.ConfigureAwait()}";
            }
            else
            {
                guardCall = $"{transition.GuardMethod}(typedGuardPayload)";
            }

            sb.AppendLine($"{resultVar} = {payloadExpr} is {payloadType} typedGuardPayload && {guardCall};");
        }

        private void EmitGuardWithOptionalPayload(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr)
        {
            var sb = sctx.Sb;
            var asyncPolicy = sctx.Root.AsyncPolicy;
            var payloadType = GetPayloadType(transition);

            // Pattern: if (payload is T typed) { result = Guard(typed); } else { result = Guard(); }
            sb.AppendLine($"if ({payloadExpr} is {payloadType} typedGuardPayload)");
            using (sb.Block(""))
            {
                if (sctx.IsAsync && transition.GuardIsAsync)
                {
                    sb.AppendLine($"{resultVar} = {asyncPolicy.AwaitKeyword(true)}{transition.GuardMethod}(typedGuardPayload){asyncPolicy.ConfigureAwait()};");
                }
                else
                {
                    sb.AppendLine($"{resultVar} = {transition.GuardMethod}(typedGuardPayload);");
                }
            }
            sb.AppendLine("else");
            using (sb.Indent())
            {
                if (sctx.IsAsync && transition.GuardIsAsync)
                {
                    sb.AppendLine($"{resultVar} = {asyncPolicy.AwaitKeyword(true)}{transition.GuardMethod}(){asyncPolicy.ConfigureAwait()};");
                }
                else
                {
                    sb.AppendLine($"{resultVar} = {transition.GuardMethod}();");
                }
            }
        }

        private string BuildGuardExpression(
            SliceContext sctx,
            TransitionModel transition,
            string payloadExpr)
        {
            var asyncPolicy = sctx.Root.AsyncPolicy;

            // TODO: W przyszłości ta logika powinna być w GuardFeature
            if (transition.GuardExpectsPayload && transition.GuardHasParameterlessOverload)
            {
                if (payloadExpr == "null")
                {
                    return BuildMethodCall(sctx, transition, null);
                }
                else
                {
                    // NAPRAWIONE: Teraz poprawnie obsługujemy payload z overloadem
                    var payloadType = GetPayloadType(transition);
                    var typedCall = BuildMethodCall(sctx, transition, "typedGuardPayload");
                    var parameterlessCall = BuildMethodCall(sctx, transition, null);

                    // Pattern dla inline: payload is T typed ? Guard(typed) : Guard()
                    return $"({payloadExpr} is {payloadType} typedGuardPayload ? {typedCall} : {parameterlessCall})";
                }
            }
            else if (transition.GuardExpectsPayload)
            {
                var payloadType = GetPayloadType(transition);
                var methodCall = BuildMethodCall(sctx, transition, "typedGuardPayload");
                return $"{payloadExpr} is {payloadType} typedGuardPayload && {methodCall}";
            }
            else
            {
                return BuildMethodCall(sctx, transition, null);
            }
        }

        private string BuildMethodCall(SliceContext sctx, TransitionModel transition, string? payloadArg)
        {
            var asyncPolicy = sctx.Root.AsyncPolicy;
            var args = payloadArg ?? "";
            var methodCall = $"{transition.GuardMethod}({args})";

            if (sctx.IsAsync && transition.GuardIsAsync)
            {
                return $"{asyncPolicy.AwaitKeyword(true)}{methodCall}{asyncPolicy.ConfigureAwait()}";
            }
            else
            {
                return methodCall;
            }
        }

        private string GetPayloadType(TransitionModel transition)
        {
            if (string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                throw new InvalidOperationException($"Guard {transition.GuardMethod} expects payload but no type specified");
            }

            return _typeHelper.FormatTypeForUsage(transition.ExpectedPayloadType, useGlobalPrefix: false);
        }
    }
}