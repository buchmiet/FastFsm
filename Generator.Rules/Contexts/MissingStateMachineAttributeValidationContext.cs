namespace Generator.Rules.Contexts;

public class MissingStateMachineAttributeValidationContext(
    bool hasAttribute,
    int argCount,
    string className,
    bool isPartial)
{
    public bool HasStateMachineAttribute { get; } = hasAttribute;
    public int FsmAttributeConstructorArgCount { get; } = argCount; 
    public string ClassName { get; } = className;
    public bool IsClassPartial { get; } = isPartial; 
}