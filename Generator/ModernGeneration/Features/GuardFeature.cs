using Generator.Infrastructure;
using Generator.Model;
using Generator.ModernGeneration.Context;
using System;
using System.Linq;
using static Generator.Strings;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł generujący zaawansowane metody query oparte na guardach.
    /// Odpowiada za: GetPermittedTriggers (wszystkie warianty)
    /// </summary>
    public class GuardFeature : IEmitMethods
    {
        private readonly TypeSystemHelper _typeHelper = new();

        public void EmitMethods(GenerationContext ctx)
        {
            EmitGetPermittedTriggersMethod(ctx);

            // GetPermittedTriggers z payload resolver - tylko dla wariantów z payloadem
            if (ctx.Model.GenerationConfig.HasPayload)
            {
                EmitGetPermittedTriggersWithPayloadResolver(ctx);
            }
        }

        private void EmitGetPermittedTriggersMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.WriteSummary("Gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
            sb.WriteReturns("List of triggers that can be fired in the current state");
            sb.AppendLine(isAsync
                ? $"public override async ValueTask<{ReadOnlyListType}<{triggerType}>> GetPermittedTriggersAsync(CancellationToken cancellationToken = default)"
                : $"public override {ReadOnlyListType}<{triggerType}> GetPermittedTriggers()");

            using (sb.Block(""))
            {
                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                        using (sb.Block(""))
                        {
                            // Check if any transition has a guard
                            var hasAsyncGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod) && t.GuardIsAsync);
                            var hasGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod));

                            if (!hasAsyncGuards && !hasGuards)
                            {
                                // No guards - return static array
                                var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                                if (triggers.Any())
                                {
                                    var triggerList = string.Join(", ", triggers.Select(t => $"{triggerType}.{_typeHelper.EscapeIdentifier(t)}"));
                                    sb.AppendLine($"return new {triggerType}[] {{ {triggerList} }};");
                                }
                                else
                                {
                                    sb.AppendLine($"return {ArrayEmptyMethod}<{triggerType}>();");
                                }
                            }
                            else
                            {
                                // Has guards - build list dynamically
                                sb.AppendLine($"var permitted = new List<{triggerType}>();");

                                foreach (var transition in stateGroup)
                                {
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        // Guard z overloadem - wywołaj bezparametrową wersję
                                        if (transition.GuardExpectsPayload && transition.GuardHasParameterlessOverload)
                                        {
                                            sb.AppendLine("try");
                                            sb.AppendLine();
                                            using (sb.Block(""))
                                            {
                                                if (isAsync && transition.GuardIsAsync)
                                                {
                                                    sb.AppendLine($"if (await {transition.GuardMethod}().ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()}))");
                                                }
                                                else
                                                {
                                                    sb.AppendLine($"if ({transition.GuardMethod}())");
                                                }
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                                }
                                            }
                                            sb.AppendLine("catch { }");
                                        }
                                        // Guard bez payloadu
                                        else if (!transition.GuardExpectsPayload)
                                        {
                                            sb.AppendLine("try");
                                            sb.AppendLine();
                                            using (sb.Block(""))
                                            {
                                                if (isAsync && transition.GuardIsAsync)
                                                {
                                                    sb.AppendLine($"if (await {transition.GuardMethod}().ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()}))");
                                                }
                                                else
                                                {
                                                    sb.AppendLine($"if ({transition.GuardMethod}())");
                                                }
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                                }
                                            }
                                            sb.AppendLine("catch { }");
                                        }
                                        // Guard wymaga payloadu i nie ma overloadu - skip w GetPermittedTriggers bez payloadu
                                    }
                                    else
                                    {
                                        // Brak guarda - zawsze dozwolone
                                        sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                    }
                                }

                                sb.AppendLine("return permitted.Count == 0 ? ");
                                using (sb.Indent())
                                {
                                    sb.AppendLine($"{ArrayEmptyMethod}<{triggerType}>() :");
                                    sb.AppendLine("permitted.ToArray();");
                                }
                            }
                        }
                    }

                    var statesWithNoOutgoingTransitions = ctx.Model.States.Keys
                        .Except(transitionsByState.Select(g => g.Key))
                        .OrderBy(s => s);

                    foreach (var stateName in statesWithNoOutgoingTransitions)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateName)}: return {ArrayEmptyMethod}<{triggerType}>();");
                    }

                    sb.AppendLine($"default: return {ArrayEmptyMethod}<{triggerType}>();");
                }
            }
            sb.AppendLine();
        }

        private void EmitGetPermittedTriggersWithPayloadResolver(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            sb.WriteSummary("Gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
            sb.WriteParam("payloadResolver", "Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.");

            sb.AppendLine($"public System.Collections.Generic.IReadOnlyList<{triggerType}> GetPermittedTriggers(Func<{triggerType}, object?> payloadResolver)");
            using (sb.Block(""))
            {
                sb.AppendLine("if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
                sb.AppendLine();

                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                        sb.AppendLine();
                        using (sb.Block(""))
                        {
                            sb.AppendLine($"var permitted = new List<{triggerType}>();");
                            sb.AppendLine($"// DEBUG: State {stateGroup.Key} has {stateGroup.Count()} transitions");

                            foreach (var transition in stateGroup)
                            {
                                sb.AppendLine($"// DEBUG: - {transition.Trigger} (Guard: {transition.GuardMethod ?? "none"})");

                                if (!string.IsNullOrEmpty(transition.GuardMethod))
                                {
                                    // Guard wymaga payloadu - użyj resolver
                                    if (transition.GuardExpectsPayload)
                                    {
                                        sb.AppendLine($"var payload_{transition.Trigger} = payloadResolver({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");

                                        // Generuj sprawdzenie guarda z payloadem
                                        sb.AppendLine("bool canFire;");
                                        sb.AppendLine("try");
                                        sb.AppendLine();
                                        using (sb.Block(""))
                                        {
                                            var payloadType = GetPayloadType(ctx, transition);
                                            sb.AppendLine($"canFire = payload_{transition.Trigger} is {payloadType} typedPayload && {transition.GuardMethod}(typedPayload);");
                                        }
                                        sb.AppendLine("catch");
                                        sb.AppendLine();
                                        using (sb.Block(""))
                                        {
                                            sb.AppendLine("canFire = false;");
                                        }

                                        using (sb.Block("if (canFire)"))
                                        {
                                            sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                        }
                                    }
                                    else
                                    {
                                        // Guard nie wymaga payloadu
                                        sb.AppendLine("bool canFire;");
                                        sb.AppendLine("try");
                                        sb.AppendLine();
                                        using (sb.Block(""))
                                        {
                                            sb.AppendLine($"canFire = {transition.GuardMethod}();");
                                        }
                                        sb.AppendLine("catch");
                                        sb.AppendLine();
                                        using (sb.Block(""))
                                        {
                                            sb.AppendLine("canFire = false;");
                                        }

                                        using (sb.Block("if (canFire)"))
                                        {
                                            sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                        }
                                    }
                                }
                                else
                                {
                                    // Brak guarda - zawsze dozwolone
                                    sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                }
                            }

                            sb.AppendLine("return permitted.Count == 0 ? ");
                            using (sb.Indent())
                            {
                                sb.AppendLine($"System.Array.Empty<{triggerType}>() :");
                                sb.AppendLine("permitted.ToArray();");
                            }
                        }
                    }

                    // Stany bez przejść
                    var statesWithNoOutgoingTransitions = ctx.Model.States.Keys
                        .Except(transitionsByState.Select(g => g.Key))
                        .OrderBy(s => s);

                    foreach (var stateName in statesWithNoOutgoingTransitions)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateName)}: return System.Array.Empty<{triggerType}>();");
                    }

                    sb.AppendLine($"default: return System.Array.Empty<{triggerType}>();");
                }
            }
            sb.AppendLine();
        }

        // Metoda pomocnicza do określenia typu payloadu
        private string GetPayloadType(GenerationContext ctx, TransitionModel transition)
        {
            // Dla single payload
            if (!string.IsNullOrEmpty(ctx.Model.DefaultPayloadType))
            {
                return _typeHelper.FormatTypeForUsage(ctx.Model.DefaultPayloadType, useGlobalPrefix: false);
            }

            // Dla multi-payload
            if (!string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                return _typeHelper.FormatTypeForUsage(transition.ExpectedPayloadType, useGlobalPrefix: false);
            }

            return "object";
        }

        private string GetStateTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.StateType, useGlobalPrefix: false);

        private string GetTriggerTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);
    }
}