namespace Generator.Rules.Contexts;

public class InvalidHistoryConfigurationContext
{
    public string StateName { get; }
    public int History { get; }
    public bool IsComposite { get; }
    
    public InvalidHistoryConfigurationContext(string stateName, int history, bool isComposite)
    {
        StateName = stateName;
        History = history;
        IsComposite = isComposite;
    }
}