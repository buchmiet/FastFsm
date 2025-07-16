namespace Generator.Rules.Contexts;

public class AttributeTypeValidationContext(
    string stateTypeName,
    bool isStateTypeEnum,
    string triggerTypeName,
    bool isTriggerTypeEnum)
{
    public string StateTypeName { get; } = stateTypeName; // Nazwa typu podanego dla stanu
    public bool IsStateTypeEnum { get; } = isStateTypeEnum;
    public string TriggerTypeName { get; } = triggerTypeName; // Nazwa typu podanego dla triggera
    public bool IsTriggerTypeEnum { get; } = isTriggerTypeEnum;
}