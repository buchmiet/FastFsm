using System.Collections.Generic;
using Generator.Rules.Contexts;
using Generator.Rules.Definitions;

namespace Generator.Rules.Rules;

/// <summary>
/// Validates that types used in [StateMachineAttribute] are enums.
/// </summary>
public class InvalidTypesInAttributeRule : IValidationRule<AttributeTypeValidationContext>
{
    private const string UnknownType = "Unknown Type";
    private const string StateTypeNotEnumFormat = "Type provided for State ('{0}') in [StateMachineAttribute] must be an enum.";
    private const string TriggerTypeNotEnumFormat = "Type provided for Trigger ('{0}') in [StateMachineAttribute] must be an enum.";

    public IEnumerable<ValidationResult> Validate(AttributeTypeValidationContext context)
    {
        if (!context.IsStateTypeEnum || !context.IsTriggerTypeEnum)
        {
            string stateTypeName = context.StateTypeName ?? UnknownType;
            string triggerTypeName = context.TriggerTypeName ?? UnknownType;

            string finalMessage;
            if (!context.IsStateTypeEnum && !context.IsTriggerTypeEnum)
            {
                // Both types are invalid
                finalMessage = string.Format(
                    DefinedRules.InvalidTypesInAttribute.MessageFormat,
                    stateTypeName,
                    triggerTypeName);
            }
            else if (!context.IsStateTypeEnum)
            {
                finalMessage = string.Format(StateTypeNotEnumFormat, stateTypeName);
            }
            else // !context.IsTriggerTypeEnum
            {
                finalMessage = string.Format(TriggerTypeNotEnumFormat, triggerTypeName);
            }

            yield return ValidationResult.Fail(
                RuleIdentifiers.InvalidTypesInAttribute,
                finalMessage,
                DefinedRules.InvalidTypesInAttribute.DefaultSeverity
            );
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}
