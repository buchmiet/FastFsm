using System.Collections.Generic;

namespace Generator.Rules.Contexts;

public class MultipleInitialSubstatesContext
{
    public string ParentStateName { get; }
    public List<string> InitialSubstates { get; }
    
    public MultipleInitialSubstatesContext(string parentStateName, List<string> initialSubstates)
    {
        ParentStateName = parentStateName;
        InitialSubstates = initialSubstates ?? new List<string>();
    }
}