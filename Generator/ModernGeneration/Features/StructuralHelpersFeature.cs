using System;
using System.Linq;
using Generator.Infrastructure;
using Generator.ModernGeneration.Context;
using static Generator.Strings;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł generujący pomocnicze metody analizy struktury maszyny stanów.
    /// Odpowiada za: HasTransition, GetDefinedTriggers
    /// </summary>
    public class StructuralHelpersFeature : IEmitMethods
    {
        private readonly TypeSystemHelper _typeHelper = new();

        public void EmitMethods(GenerationContext ctx)
        {
            if (ctx.Model.EmitStructuralHelpers)
            {
                EmitHasTransitionMethod(ctx);
                EmitGetDefinedTriggersMethod(ctx);
            }
        }

        private void EmitHasTransitionMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            sb.WriteSummary("Checks if a transition is defined in the state machine structure (ignores guards)");
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
            sb.AppendLine($"public bool HasTransition({triggerType} trigger)");
            using (sb.Block(""))
            {
                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                        if (triggers.Any())
                        {
                            sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                            using (sb.Indent())
                            {
                                using (sb.Block("switch (trigger)"))
                                {
                                    foreach (var trigger in triggers)
                                    {
                                        sb.AppendLine($"case {triggerType}.{_typeHelper.EscapeIdentifier(trigger)}: return true;");
                                    }
                                    sb.AppendLine("default: return false;");
                                }
                            }
                        }
                    }
                    sb.AppendLine("default: return false;");
                }
            }
            sb.AppendLine();
        }

        private void EmitGetDefinedTriggersMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            sb.WriteSummary("Gets all triggers defined for the current state in the state machine structure (ignores guards)");
            sb.AppendLine($"public {ReadOnlyListType}<{triggerType}> GetDefinedTriggers()");
            using (sb.Block(""))
            {
                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                        sb.Append($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}: return ");
                        if (triggers.Any())
                        {
                            var triggerList = string.Join(", ", triggers.Select(t => $"{triggerType}.{_typeHelper.EscapeIdentifier(t)}"));
                            sb.AppendLine($"new {triggerType}[] {{ {triggerList} }};");
                        }
                        else
                        {
                            sb.AppendLine($"{ArrayEmptyMethod}<{triggerType}>();");
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

        private string GetStateTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.StateType, useGlobalPrefix: false);

        private string GetTriggerTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);
    }
}