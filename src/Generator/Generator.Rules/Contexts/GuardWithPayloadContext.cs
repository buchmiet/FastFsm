namespace Generator.Rules.Contexts;

public class GuardWithPayloadContext(
    string guardMethodName,
    bool guardExpectsPayload,
    bool machineHasPayload)
{
    public string GuardMethodName { get; } = guardMethodName;
    public bool GuardExpectsPayload { get; } = guardExpectsPayload;
    public bool MachineHasPayload { get; } = machineHasPayload;
}