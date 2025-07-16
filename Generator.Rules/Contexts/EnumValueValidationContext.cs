namespace Generator.Rules.Contexts;

public class EnumValueValidationContext(string enumTypeName, string providedValueString, bool isValueDefinedInEnum)
{
    public string EnumTypeName { get; } = enumTypeName;
    public string ProvidedValueString { get; } = providedValueString; // Wartość podana w atrybucie jako string
    public bool IsValueDefinedInEnum { get; } = isValueDefinedInEnum; // Czy parser/analyzer znalazł tę wartość w enumie
}