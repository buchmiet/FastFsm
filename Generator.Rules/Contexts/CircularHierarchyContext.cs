using System.Collections.Generic;

namespace Generator.Rules.Contexts;

public class CircularHierarchyContext
{
    public string StateName { get; }
    public List<string> CyclePath { get; }
    
    public CircularHierarchyContext(string stateName, List<string> cyclePath)
    {
        StateName = stateName;
        CyclePath = cyclePath ?? new List<string>();
    }
}