namespace Generator.Model;

public class StateModel
{
    public string Name { get; set; } = ""; // EnumMemberName
    public string? OnEntryMethod { get; set; }
    public string? OnExitMethod { get; set; }
    public bool OnEntryHasParameterlessOverload { get; set; }
    public bool OnExitHasParameterlessOverload { get; set; }
    public bool OnEntryExpectsPayload { get; set; }
    public bool OnExitExpectsPayload { get; set; }

}