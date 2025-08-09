namespace Generator.Rules.Contexts;

public class OrphanSubstateContext
{
    public string StateName { get; }
    public string ParentStateName { get; }
    
    public OrphanSubstateContext(string stateName, string parentStateName)
    {
        StateName = stateName;
        ParentStateName = parentStateName;
    }
}