namespace Generator.Rules.Contexts;

public class ConflictingPayloadContext(
    bool isWithPayloadVariant,
    int triggerSpecificPayloadCount)
{
    public bool IsWithPayloadVariant { get; } = isWithPayloadVariant;
    public int TriggerSpecificPayloadCount { get; } = triggerSpecificPayloadCount;
}