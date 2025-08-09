namespace Generator.Rules.Contexts;

public class InvalidHierarchyConfigurationContext
{
    public string StateName { get; }
    public string Issue { get; }
    public string Details { get; }
    
    public InvalidHierarchyConfigurationContext(string stateName, string issue, string details = null)
    {
        StateName = stateName;
        Issue = issue;
        Details = details;
    }
}