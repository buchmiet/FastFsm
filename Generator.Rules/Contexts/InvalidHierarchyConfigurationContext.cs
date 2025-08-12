namespace Generator.Rules.Contexts;

/// <summary>
/// Context describing an invalid HSM hierarchy configuration for a given state.
/// </summary>
public class InvalidHierarchyConfigurationContext
{
    /// <summary>State in which the issue was detected (non-null).</summary>
    public string StateName { get; }

    /// <summary>Short machine-readable issue key or label (non-null).</summary>
    public string Issue { get; }

    /// <summary>Optional human-readable details. May be <c>null</c>.</summary>
    public string? Details { get; }

    public InvalidHierarchyConfigurationContext(string stateName, string issue, string? details = null)
    {
        StateName = stateName;
        Issue = issue;
        Details = details;
    }
}