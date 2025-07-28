namespace Generator.Model;

public class TransitionModel
{
    public string FromState { get; set; } = ""; // EnumMemberName
    public string Trigger { get; set; } = "";   // EnumMemberName
    public string ToState { get; set; } = "";     // EnumMemberName
    public string? GuardMethod { get; set; }    // Nazwa metody, zakładamy bezparametrową
    public string? ActionMethod { get; set; }   // Nazwa metody, zakładamy bezparametrową
    public bool IsInternal { get; set; }
    public string? ExpectedPayloadType { get; set; } // Fully qualified name of expected payload type for this transition
    public bool GuardExpectsPayload { get; set; }
    public bool ActionExpectsPayload { get; set; }
    public bool GuardHasParameterlessOverload { get; set; }
    public bool ActionHasParameterlessOverload { get; set; }
    public bool GuardIsAsync { get; set; }
    public bool ActionIsAsync { get; set; }
}