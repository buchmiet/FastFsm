namespace Generator.Model;

public class StateMachineModel
{
    public string Namespace { get; set; } = "";
    public string ClassName { get; set; } = "";
    /// <summary>
    /// Names of containing types if the state machine class is nested.
    /// Represents the outer types in declaration order.
    /// </summary>
    public List<string> ContainerClasses { get; set; } = new();
    public string StateType { get; set; } = ""; // Fully qualified name
    public string TriggerType { get; set; } = ""; // Fully qualified name
    public List<TransitionModel> Transitions { get; set; } = [];
    public Dictionary<string, StateModel> States { get; set; } = new(); // Key is EnumMemberName
    public GenerationConfig GenerationConfig { get; set; } = new();
    public GenerationVariant Variant => GenerationConfig.Variant;
    public string? DefaultPayloadType { get; set; } // Fully qualified name of default payload type
    public Dictionary<string, string> TriggerPayloadTypes { get; set; } = new(); 

    public bool GenerateLogging { get; set; }
    public bool GenerateDependencyInjection { get; set; }
    public bool EmitStructuralHelpers { get; set; }
    public bool ContinueOnCapturedContext { get; set; } = false;
    
    /// <summary>
    /// Optional exception handler configuration.
    /// </summary>
    public ExceptionHandlerModel? ExceptionHandler { get; set; }

    #region HSM Properties

    /// <summary>
    /// Maps each state to its parent state (null for root states)
    /// </summary>
    public Dictionary<string, string?> ParentOf { get; set; } = new();

    /// <summary>
    /// Maps each composite state to its child states
    /// </summary>
    public Dictionary<string, List<string>> ChildrenOf { get; set; } = new();

    /// <summary>
    /// Maps each state to its depth in the hierarchy (0 for root states)
    /// </summary>
    public Dictionary<string, int> Depth { get; set; } = new();

    /// <summary>
    /// Maps each composite state to its initial child state
    /// </summary>
    public Dictionary<string, string?> InitialChildOf { get; set; } = new();

    /// <summary>
    /// Maps each composite state to its history mode
    /// </summary>
    public Dictionary<string, HistoryMode> HistoryOf { get; set; } = new();

    /// <summary>
    /// Whether hierarchy is enabled (from attribute or auto-detected)
    /// </summary>
    public bool HierarchyEnabled { get; set; } = false;

    /// <summary>
    /// Whether any HSM features are actually used
    /// </summary>
    public bool HasHierarchy => ParentOf.Any(p => p.Value != null) || ChildrenOf.Any(c => c.Value.Count > 0);
    
    /// <summary>
    /// Whether enum-only fallback was used (no [State] attributes found)
    /// </summary>
    public bool UsedEnumOnlyFallback { get; set; } = false;

    #endregion
}
