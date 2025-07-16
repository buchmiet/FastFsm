namespace Generator.Rules.Contexts;

public class InvalidVariantConfigContext(
    string variantName,
    string conflictType,
    bool hasConflict)
{
    public string VariantName { get; } = variantName;
    public string ConflictType { get; } = conflictType; // "PayloadTypes", "Extensions", "OnEntryExit"
    public bool HasConflict { get; } = hasConflict;
}