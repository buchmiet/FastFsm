namespace Generator.Rules.Contexts;

public class EnumValueValidationContext(string enumTypeName, string providedValueString, bool isValueDefinedInEnum)
{
    public string EnumTypeName { get; } = enumTypeName;
    public string ProvidedValueString { get; } = providedValueString; // Value as given in the attribute
    public bool IsValueDefinedInEnum { get; } = isValueDefinedInEnum; // Whether parser/analyzer found this value in the enum
}