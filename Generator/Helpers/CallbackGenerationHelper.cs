using Generator.Infrastructure;
using Generator.Model;

namespace Generator.Helpers
{
    /// <summary>
    /// Helper for generating callback code.
    /// </summary>
    internal static class CallbackGenerationHelper
    {
        private static TypeSystemHelper TypeHelper = new();

        /// <summary>
        /// Type of callback being generated.
        /// </summary>
        internal enum CallbackType
        {
            /// <summary>
            /// State entry callback.
            /// </summary>
            OnEntry,
            /// <summary>
            /// State exit callback.
            /// </summary>
            OnExit,
            /// <summary>
            /// Transition action callback.
            /// </summary>
            Action
        }

        /// <summary>
        /// Generuje wywołanie callback z pełną obsługą wariantów.
        /// </summary>
        private static void EmitCallbackInvocation(
            IndentedStringBuilder.IndentedStringBuilder sb,
            string callbackMethod,
            CallbackType type,
            bool expectsPayload,
            bool hasParameterlessOverload,
            bool isCallbackAsync,
            bool isCallerAsync,
            string? expectedPayloadType,
            string payloadVar,
            bool wrapInTryCatch,
            bool continueOnCapturedContext = false,
            bool isMultiPayload = false,
            bool passRawObjectForMultiPayload = false,
            CallbackSignatureInfo? signatureInfo = null,
            string? cancellationTokenVar = null,
            bool treatCancellationAsFailure = false)
        {
            if (wrapInTryCatch)
            {
                using (sb.Block("try"))
                {
                    GenerateCallbackBody(sb, callbackMethod, type, expectsPayload, hasParameterlessOverload,
                        isCallbackAsync, isCallerAsync, expectedPayloadType, payloadVar,
                        continueOnCapturedContext, isMultiPayload, passRawObjectForMultiPayload,
                        signatureInfo, cancellationTokenVar);
                }

                // Handle cancellation separately if configured
                if (!treatCancellationAsFailure && cancellationTokenVar != null)
                {
                    using (sb.Block("catch (System.OperationCanceledException)"))
                    {
                        sb.AppendLine("success = false;");
                        // TODO: Add optional CallbackCanceled log
                        sb.AppendLine("goto END_TRY_FIRE;");
                    }
                }

                using (sb.Block("catch (System.Exception)"))
                {
                    sb.AppendLine("success = false;");
                    sb.AppendLine("goto END_TRY_FIRE;");
                }
            }
            else
            {
                GenerateCallbackBody(sb, callbackMethod, type, expectsPayload, hasParameterlessOverload,
                    isCallbackAsync, isCallerAsync, expectedPayloadType, payloadVar,
                    continueOnCapturedContext, isMultiPayload, passRawObjectForMultiPayload,
                    signatureInfo, cancellationTokenVar);
            }
        }

        private static void GenerateCallbackBody(
            IndentedStringBuilder.IndentedStringBuilder sb,
            string callbackMethod,
            CallbackType type,
            bool expectsPayload,
            bool hasParameterlessOverload,
            bool isCallbackAsync,
            bool isCallerAsync,
            string? expectedPayloadType,
            string payloadVar,
            bool continueOnCapturedContext,
            bool isMultiPayload,
            bool passRawObjectForMultiPayload,
            CallbackSignatureInfo? signatureInfo,
            string? cancellationTokenVar)
        {
            // Use signature info if available, otherwise fall back to legacy flags
            if (signatureInfo.HasValue)
            {
                GenerateCallbackWithSignatureInfo(sb, callbackMethod, type, signatureInfo.Value,
                    payloadVar, cancellationTokenVar, isCallbackAsync, isCallerAsync,
                    continueOnCapturedContext, isMultiPayload, passRawObjectForMultiPayload);
            }
            else
            {
                // Legacy path - will be removed once all callers provide signature info
                GenerateLegacyCallback(sb, callbackMethod, type, expectsPayload, hasParameterlessOverload,
                    isCallbackAsync, isCallerAsync, expectedPayloadType, payloadVar,
                    continueOnCapturedContext, isMultiPayload, passRawObjectForMultiPayload);
            }
        }

        private static void GenerateCallbackWithSignatureInfo(
            IndentedStringBuilder.IndentedStringBuilder sb,
            string callbackMethod,
            CallbackType type,
            CallbackSignatureInfo sig,
            string payloadVar,
            string? cancellationTokenVar,
            bool isCallbackAsync,
            bool isCallerAsync,
            bool continueOnCapturedContext,
            bool isMultiPayload,
            bool passRawObjectForMultiPayload)
        {
            // Determine variable name based on callback type
            string variableName = type switch
            {
                CallbackType.Action => "typedActionPayload",
                CallbackType.OnEntry => "typedPayload",
                CallbackType.OnExit => "typedPayload",
                _ => "typedPayload"
            };

            bool hasPayload = payloadVar != "null" && sig.PayloadTypeFullName != null;
            bool hasToken = cancellationTokenVar != null;

            var bestOverload = sig.GetBestOverload(hasPayload, hasToken);

            // Special case for multi-payload with raw object
            if (isMultiPayload && passRawObjectForMultiPayload && hasPayload)
            {
                GenerateMultiPayloadRawObjectCall(sb, callbackMethod, sig, payloadVar, cancellationTokenVar,
                    isCallbackAsync, isCallerAsync, continueOnCapturedContext);
                return;
            }

            // Generate call based on best overload
            switch (bestOverload)
            {
                case OverloadType.PayloadAndToken:
                    {
                        var payloadType = TypeHelper.FormatTypeForUsage(sig.PayloadTypeFullName);
                        using (sb.Block($"if ({payloadVar} is {payloadType} {variableName})"))
                        {
                            EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                                continueOnCapturedContext, variableName, cancellationTokenVar);
                        }
                        // Fallback if cast fails
                        if (sig.HasTokenOnly || sig.HasParameterless)
                        {
                            sb.AppendLine("else");
                            using (sb.Indent())
                            {
                                if (sig.HasTokenOnly && hasToken)
                                {
                                    EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                                        continueOnCapturedContext, cancellationTokenVar);
                                }
                                else if (sig.HasParameterless)
                                {
                                    EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                                        continueOnCapturedContext);
                                }
                            }
                        }
                        break;
                    }

                case OverloadType.PayloadOnly:
                    {
                        var payloadType = TypeHelper.FormatTypeForUsage(sig.PayloadTypeFullName);
                        using (sb.Block($"if ({payloadVar} is {payloadType} {variableName})"))
                        {
                            EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                                continueOnCapturedContext, variableName);
                        }
                        // Fallback if cast fails
                        if (sig.HasTokenOnly && hasToken)
                        {
                            sb.AppendLine("else");
                            using (sb.Indent())
                            {
                                EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                                    continueOnCapturedContext, cancellationTokenVar);
                            }
                        }
                        else if (sig.HasParameterless)
                        {
                            sb.AppendLine("else");
                            using (sb.Indent())
                            {
                                EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                                    continueOnCapturedContext);
                            }
                        }
                        break;
                    }

                case OverloadType.TokenOnly:
                    EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                        continueOnCapturedContext, cancellationTokenVar);
                    break;

                case OverloadType.Parameterless:
                    EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                        continueOnCapturedContext);
                    break;

                default:
                    // No matching overload - this shouldn't happen if analysis was correct
                    sb.AppendLine("// Warning: No matching callback overload found");
                    break;
            }
        }

        private static void GenerateMultiPayloadRawObjectCall(
            IndentedStringBuilder.IndentedStringBuilder sb,
            string callbackMethod,
            CallbackSignatureInfo sig,
            string payloadVar,
            string? cancellationTokenVar,
            bool isCallbackAsync,
            bool isCallerAsync,
            bool continueOnCapturedContext)
        {
            // For multi-payload with raw object, prefer overloads in this order:
            // 1. (object, CancellationToken) if available
            // 2. (object) if available  
            // 3. (CancellationToken) if available
            // 4. () if available

            bool hasToken = cancellationTokenVar != null;

            if (hasToken && sig.HasPayloadAndToken)
            {
                using (sb.Block($"if ({payloadVar} != null)"))
                {
                    EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                        continueOnCapturedContext, payloadVar, cancellationTokenVar);
                }

                if (sig.HasTokenOnly)
                {
                    sb.AppendLine("else");
                    using (sb.Indent())
                    {
                        EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                            continueOnCapturedContext, cancellationTokenVar);
                    }
                }
                else if (sig.HasParameterless)
                {
                    sb.AppendLine("else");
                    using (sb.Indent())
                    {
                        EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                            continueOnCapturedContext);
                    }
                }
            }
            else if (sig.HasPayloadOnly)
            {
                using (sb.Block($"if ({payloadVar} != null)"))
                {
                    EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                        continueOnCapturedContext, payloadVar);
                }

                if (sig.HasParameterless)
                {
                    sb.AppendLine("else");
                    using (sb.Indent())
                    {
                        EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                            continueOnCapturedContext);
                    }
                }
            }
            else if (hasToken && sig.HasTokenOnly)
            {
                EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                    continueOnCapturedContext, cancellationTokenVar);
            }
            else if (sig.HasParameterless)
            {
                EmitMethodCall(sb, callbackMethod, isCallbackAsync, isCallerAsync,
                    continueOnCapturedContext);
            }
        }

        private static void EmitMethodCall(
            IndentedStringBuilder.IndentedStringBuilder sb,
            string methodName,
            bool isMethodAsync,
            bool isCallerAsync,
            bool continueOnCapturedContext,
            params string[] args)
        {
            AsyncGenerationHelper.EmitMethodInvocation(
                sb, methodName, isMethodAsync, isCallerAsync, continueOnCapturedContext, args);
        }

        private static void GenerateLegacyCallback(
            IndentedStringBuilder.IndentedStringBuilder sb,
            string callbackMethod,
            CallbackType type,
            bool expectsPayload,
            bool hasParameterlessOverload,
            bool isCallbackAsync,
            bool isCallerAsync,
            string expectedPayloadType,
            string payloadVar,
            bool continueOnCapturedContext,
            bool isMultiPayload,
            bool passRawObjectForMultiPayload)
        {
            // Keep existing legacy implementation for backward compatibility
            // This will be removed once all code is migrated to use CallbackSignatureInfo

            string variableName = type switch
            {
                CallbackType.Action => "typedActionPayload",
                CallbackType.OnEntry => "typedPayload",
                CallbackType.OnExit => "typedPayload",
                _ => "typedPayload"
            };

            if (isMultiPayload && passRawObjectForMultiPayload && expectsPayload)
            {
                if (hasParameterlessOverload)
                {
                    using (sb.Block($"if ({payloadVar} != null)"))
                    {
                        AsyncGenerationHelper.EmitMethodInvocation(
                            sb, callbackMethod, isCallbackAsync, isCallerAsync, continueOnCapturedContext, payloadVar);
                    }
                    sb.AppendLine("else");
                    using (sb.Indent())
                    {
                        AsyncGenerationHelper.EmitMethodInvocation(
                            sb, callbackMethod, isCallbackAsync, isCallerAsync, continueOnCapturedContext);
                    }
                }
                else
                {
                    using (sb.Block($"if ({payloadVar} != null)"))
                    {
                        AsyncGenerationHelper.EmitMethodInvocation(
                            sb, callbackMethod, isCallbackAsync, isCallerAsync, continueOnCapturedContext, payloadVar);
                    }
                }
                return;
            }

            if (!expectsPayload || string.IsNullOrEmpty(expectedPayloadType))
            {
                AsyncGenerationHelper.EmitMethodInvocation(
                    sb, callbackMethod, isCallbackAsync, isCallerAsync, continueOnCapturedContext);
            }
            else if (hasParameterlessOverload)
            {
                expectedPayloadType = TypeHelper.FormatTypeForUsage(expectedPayloadType);
                using (sb.Block($"if ({payloadVar} is {expectedPayloadType} {variableName})"))
                {
                    AsyncGenerationHelper.EmitMethodInvocation(
                        sb, callbackMethod, isCallbackAsync, isCallerAsync, continueOnCapturedContext, variableName);
                }
                sb.AppendLine("else");
                using (sb.Indent())
                {
                    AsyncGenerationHelper.EmitMethodInvocation(
                        sb, callbackMethod, isCallbackAsync, isCallerAsync, continueOnCapturedContext);
                }
            }
            else
            {
                expectedPayloadType = TypeHelper.FormatTypeForUsage(expectedPayloadType);
                using (sb.Block($"if ({payloadVar} is {expectedPayloadType} {variableName})"))
                {
                    AsyncGenerationHelper.EmitMethodInvocation(
                        sb, callbackMethod, isCallbackAsync, isCallerAsync, continueOnCapturedContext, variableName);
                }
            }
        }

        /// <summary>
        /// Generuje wywołanie OnEntry dla StateModel z obsługą multi-payload.
        /// </summary>
        public static void EmitOnEntryCall(
            IndentedStringBuilder.IndentedStringBuilder sb,
            StateModel state,
            string? expectedPayloadType,
            string? defaultPayloadType,
            string payloadVar,
            bool isCallerAsync,
            bool wrapInTryCatch,
            bool continueOnCapturedContext = false,
            bool isSinglePayload = false,
            bool isMultiPayload = false,
            string? cancellationTokenVar = null,
            bool treatCancellationAsFailure = false)
        {
            if (string.IsNullOrEmpty(state.OnEntryMethod)) return;

            var effectiveType = expectedPayloadType ?? defaultPayloadType;

            // For multi-payload without expected type and without parameterless overload - don't generate
            if (isMultiPayload && expectedPayloadType == null && !state.OnEntryHasParameterlessOverload)
            {
                return;
            }

            EmitCallbackInvocation(
                sb,
                state.OnEntryMethod,
                CallbackType.OnEntry,
                state.OnEntryExpectsPayload,
                state.OnEntryHasParameterlessOverload,
                state.OnEntryIsAsync,
                isCallerAsync,
                effectiveType,
                payloadVar,
                wrapInTryCatch,
                continueOnCapturedContext,
                isMultiPayload,
                passRawObjectForMultiPayload: false, // OnEntry uses typed payload
                state.OnEntrySignature,
                cancellationTokenVar,
                treatCancellationAsFailure
            );
        }

        /// <summary>
        /// Generuje wywołanie OnExit dla StateModel z obsługą multi-payload.
        /// </summary>
        public static void EmitOnExitCall(
            IndentedStringBuilder.IndentedStringBuilder sb,
            StateModel state,
            string? expectedPayloadType,
            string? defaultPayloadType,
            string payloadVar,
            bool isCallerAsync,
            bool wrapInTryCatch,
            bool continueOnCapturedContext = false,
            bool isSinglePayload = false,
            bool isMultiPayload = false,
            string? cancellationTokenVar = null,
            bool treatCancellationAsFailure = false)
        {
            if (string.IsNullOrEmpty(state.OnExitMethod)) return;

            var effectiveType = expectedPayloadType ?? defaultPayloadType;

            // Multi-payload uses raw object for OnExit
            bool passRawObject = isMultiPayload && !isSinglePayload;

            EmitCallbackInvocation(
                sb,
                state.OnExitMethod,
                CallbackType.OnExit,
                state.OnExitExpectsPayload,
                state.OnExitHasParameterlessOverload,
                state.OnExitIsAsync,
                isCallerAsync,
                effectiveType,
                payloadVar,
                wrapInTryCatch,
                continueOnCapturedContext,
                isMultiPayload,
                passRawObjectForMultiPayload: passRawObject,
                state.OnExitSignature,
                cancellationTokenVar,
                treatCancellationAsFailure
            );
        }

        /// <summary>
        /// Generuje wywołanie Action dla TransitionModel.
        /// </summary>
        public static void EmitActionCall(
            IndentedStringBuilder.IndentedStringBuilder sb,
            TransitionModel transition,
            string payloadVar,
            bool isCallerAsync,
            bool wrapInTryCatch,
            bool continueOnCapturedContext = false,
            string? cancellationTokenVar = null,
            bool treatCancellationAsFailure = false)
        {
            if (string.IsNullOrEmpty(transition.ActionMethod)) return;

            EmitCallbackInvocation(
                sb,
                transition.ActionMethod,
                CallbackType.Action,
                transition.ActionExpectsPayload,
                transition.ActionHasParameterlessOverload,
                transition.ActionIsAsync,
                isCallerAsync,
                transition.ExpectedPayloadType,
                payloadVar,
                wrapInTryCatch,
                continueOnCapturedContext,
                isMultiPayload: false,
                passRawObjectForMultiPayload: false,
                transition.ActionSignature,
                cancellationTokenVar,
                treatCancellationAsFailure
            );
        }
    }
}
