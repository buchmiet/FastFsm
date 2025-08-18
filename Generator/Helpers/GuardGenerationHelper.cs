using Generator.Infrastructure;
using Generator.Model;

namespace Generator.Helpers
{
    public static class GuardGenerationHelper
    {
        private static TypeSystemHelper TypeHelper = new();

        /// <summary>
        /// Generuje kompletny blok sprawdzenia guarda z obsługą wszystkich wariantów.
        /// </summary>
        /// <param name="sb">String builder do generowania kodu</param>
        /// <param name="transition">Model przejścia zawierający informacje o guard</param>
        /// <param name="resultVar">Nazwa zmiennej wynikowej (np. "guardResult")</param>
        /// <param name="payloadVar">Nazwa zmiennej z payloadem lub "null"</param>
        /// <param name="isAsync">Czy wywołujący jest metodą async</param>
        /// <param name="wrapInTryCatch">Czy owinąć w try-catch (dla CanFire/GetPermittedTriggers)</param>
        /// <param name="continueOnCapturedContext">Wartość dla ConfigureAwait (tylko dla async)</param>
        /// <param name="handleResultAfterTry">Czy wynik guarda będzie używany po bloku try/catch</param>
        /// <param name="cancellationTokenVar">Nazwa zmiennej z CancellationToken (null = brak przekazywania)</param>
        /// <param name="treatCancellationAsFailure">Czy traktować anulowanie jako błąd</param>
        public static void EmitGuardCheck(
            IndentedStringBuilder.IndentedStringBuilder sb,
            TransitionModel transition,
            string resultVar,
            string payloadVar,
            bool isAsync,
            bool wrapInTryCatch,
            bool continueOnCapturedContext = false,
            bool handleResultAfterTry = false,
            string? cancellationTokenVar = null,
            bool treatCancellationAsFailure = false)
        {
            // If no guard method, always true
            if (string.IsNullOrEmpty(transition.GuardMethod))
            {
                sb.AppendLine($"bool {resultVar} = true;");
                return;
            }

            var sig = transition.GuardSignature;
            bool hasPayload = payloadVar != "null" && sig.PayloadTypeFullName != null;
            bool hasToken = cancellationTokenVar != null;

            // Determine best overload
            var bestOverload = sig.GetBestOverload(hasPayload, hasToken);

            // If no matching overload found
            if (bestOverload == OverloadType.None)
            {
                sb.AppendLine($"bool {resultVar} = false; // No matching guard overload");
                return;
            }

            // If payload required but not available
            if ((bestOverload == OverloadType.PayloadOnly || bestOverload == OverloadType.PayloadAndToken)
                && payloadVar == "null")
            {
                sb.AppendLine($"bool {resultVar} = false; // Guard expects payload but none provided");
                return;
            }

            // Prepare async components
            var guardAsync = sig.IsAsync;
            var awaitPrefix = (isAsync && guardAsync) ? "await " : "";
            var configureAwait = (isAsync && guardAsync)
                ? $".ConfigureAwait({continueOnCapturedContext.ToString().ToLowerInvariant()})"
                : "";

            // Declare result variable if needed
            if (wrapInTryCatch && handleResultAfterTry)
            {
                sb.AppendLine($"bool {resultVar};");
            }

            // Generate with or without try-catch
            if (wrapInTryCatch)
            {
                using (sb.Block("try"))
                {
                    EmitGuardCall(sb, transition, sig, bestOverload, resultVar, payloadVar,
                        cancellationTokenVar, awaitPrefix, configureAwait, !handleResultAfterTry);
                }

                // Handle cancellation separately if configured
                if (!treatCancellationAsFailure && hasToken)
                {
                    using (sb.Block("catch (System.OperationCanceledException)"))
                    {
                        sb.AppendLine($"{resultVar} = false;");
                        // TODO: Add optional GuardCanceled log
                    }
                }

                using (sb.Block("catch (System.Exception)"))
                {
                    sb.AppendLine($"{resultVar} = false;");
                    // TODO: Add optional GuardFailed log
                }
            }
            else
            {
                EmitGuardCall(sb, transition, sig, bestOverload, resultVar, payloadVar,
                    cancellationTokenVar, awaitPrefix, configureAwait, true);
            }
        }

        private static void EmitGuardCall(
            IndentedStringBuilder.IndentedStringBuilder sb,
            TransitionModel transition,
            CallbackSignatureInfo sig,
            OverloadType overloadType,
            string resultVar,
            string payloadVar,
            string? cancellationTokenVar,
            string awaitPrefix,
            string configureAwait,
            bool declareVariable)
        {
            var guardMethod = transition.GuardMethod;
            string callExpression;

            switch (overloadType)
            {
                case OverloadType.PayloadAndToken:
                    {
                        var payloadType = TypeHelper.FormatTypeForUsage(sig.PayloadTypeFullName);
                        if (declareVariable)
                        {
                            sb.AppendLine($"bool {resultVar};");
                        }
                        using (sb.Block($"if ({payloadVar} is {payloadType} typedGuardPayload)"))
                        {
                            sb.AppendLine($"{resultVar} = {awaitPrefix}{guardMethod}(typedGuardPayload, {cancellationTokenVar}){configureAwait};");
                        }
                        sb.AppendLine("else");
                        using (sb.Indent())
                        {
                            // Fallback logic based on available overloads
                            if (sig.HasTokenOnly)
                            {
                                sb.AppendLine($"{resultVar} = {awaitPrefix}{guardMethod}({cancellationTokenVar}){configureAwait};");
                            }
                            else if (sig.HasParameterless)
                            {
                                sb.AppendLine($"{resultVar} = {awaitPrefix}{guardMethod}(){configureAwait};");
                            }
                            else
                            {
                                sb.AppendLine($"{resultVar} = false; // No fallback available");
                            }
                        }
                        break;
                    }

                case OverloadType.PayloadOnly:
                    {
                        var payloadType = TypeHelper.FormatTypeForUsage(sig.PayloadTypeFullName);
                        if (declareVariable)
                        {
                            sb.AppendLine($"bool {resultVar};");
                        }
                        using (sb.Block($"if ({payloadVar} is {payloadType} typedGuardPayload)"))
                        {
                            sb.AppendLine($"{resultVar} = {awaitPrefix}{guardMethod}(typedGuardPayload){configureAwait};");
                        }
                        sb.AppendLine("else");
                        using (sb.Indent())
                        {
                            // Fallback logic based on available overloads
                            if (sig.HasParameterless)
                            {
                                sb.AppendLine($"{resultVar} = {awaitPrefix}{guardMethod}(){configureAwait};");
                            }
                            else if (sig.HasTokenOnly && cancellationTokenVar != null)
                            {
                                sb.AppendLine($"{resultVar} = {awaitPrefix}{guardMethod}({cancellationTokenVar}){configureAwait};");
                            }
                            else
                            {
                                sb.AppendLine($"{resultVar} = false; // No fallback available");
                            }
                        }
                        break;
                    }

                case OverloadType.TokenOnly:
                    {
                        callExpression = $"{awaitPrefix}{guardMethod}({cancellationTokenVar}){configureAwait}";
                        if (declareVariable)
                        {
                            sb.AppendLine($"bool {resultVar} = {callExpression};");
                        }
                        else
                        {
                            sb.AppendLine($"{resultVar} = {callExpression};");
                        }
                        break;
                    }

                case OverloadType.Parameterless:
                    {
                        callExpression = $"{awaitPrefix}{guardMethod}(){configureAwait}";
                        if (declareVariable)
                        {
                            sb.AppendLine($"bool {resultVar} = {callExpression};");
                        }
                        else
                        {
                            sb.AppendLine($"{resultVar} = {callExpression};");
                        }
                        break;
                    }

                default:
                    sb.AppendLine($"bool {resultVar} = false; // Unexpected overload type");
                    break;
            }
        }


        //private static void EmitGuardWithPayloadAndOverload(
        //    IndentedStringBuilder.IndentedStringBuilder sb,
        //    TransitionModel transition,
        //    string resultVar,
        //    string payloadVar,
        //    string awaitPrefix,
        //    string configureAwait,
        //    bool declareVariable)
        //{
        //    var payloadType = TypeHelper.FormatTypeForUsage(transition.ExpectedPayloadType!);

        //    if (declareVariable)
        //    {
        //        sb.AppendLine($"bool {resultVar};");
        //    }

        //    using (sb.Block($"if ({payloadVar} is {payloadType} typedGuardPayload)"))
        //    {
        //        sb.AppendLine($"{resultVar} = {awaitPrefix}{transition.GuardMethod}(typedGuardPayload){configureAwait};");
        //    }
        //    sb.AppendLine("else");
        //    using (sb.Indent())
        //    {
        //        sb.AppendLine($"{resultVar} = {awaitPrefix}{transition.GuardMethod}(){configureAwait};");
        //    }
        //}

        //private static void EmitGuardWithPayloadOnly(
        //    IndentedStringBuilder.IndentedStringBuilder sb,
        //    TransitionModel transition,
        //    string resultVar,
        //    string payloadVar,
        //    string awaitPrefix,
        //    string configureAwait,
        //    bool declareVariable)
        //{
        //    var payloadType = TypeHelper.FormatTypeForUsage(transition.ExpectedPayloadType!);

        //    if (declareVariable)
        //    {
        //        // Pełna deklaracja z przypisaniem
        //        if ((awaitPrefix + transition.GuardMethod + configureAwait).Length > 80)
        //        {
        //            sb.AppendLine($"bool {resultVar} = {payloadVar} is {payloadType} typedGuardPayload &&");
        //            using (sb.Indent())
        //            {
        //                sb.AppendLine($"{awaitPrefix}{transition.GuardMethod}(typedGuardPayload){configureAwait};");
        //            }
        //        }
        //        else
        //        {
        //            sb.AppendLine($"bool {resultVar} = {payloadVar} is {payloadType} typedGuardPayload && {awaitPrefix}{transition.GuardMethod}(typedGuardPayload){configureAwait};");
        //        }
        //    }
        //    else
        //    {
        //        // ✅ Tylko przypisanie (bez bool)
        //        sb.AppendLine($"{resultVar} = {payloadVar} is {payloadType} typedGuardPayload && {awaitPrefix}{transition.GuardMethod}(typedGuardPayload){configureAwait};");
        //    }
        //}

        //private static void EmitGuardWithoutPayload(
        //    IndentedStringBuilder.IndentedStringBuilder sb,
        //    TransitionModel transition,
        //    string resultVar,
        //    string awaitPrefix,
        //    string configureAwait,
        //    bool declareVariable)
        //{
        //    if (declareVariable)
        //    {
        //        sb.AppendLine($"bool {resultVar} = {awaitPrefix}{transition.GuardMethod}(){configureAwait};");
        //    }
        //    else
        //    {
        //        sb.AppendLine($"{resultVar} = {awaitPrefix}{transition.GuardMethod}(){configureAwait};");
        //    }
        //}
    }
}
