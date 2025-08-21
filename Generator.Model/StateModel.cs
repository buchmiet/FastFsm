namespace Generator.Model;

/// <summary>
/// Model reprezentujący stan w maszynie stanów wraz z jego callbackami.
/// </summary>
public sealed class StateModel
{
    #region Core Properties

    /// <summary>
    /// Nazwa stanu
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Ordinal value of the enum member (for correct array indexing)
    /// </summary>
    public int OrdinalValue { get; set; } = 0;

    /// <summary>
    /// Nazwa metody OnEntry (wykonywana przy wejściu do stanu)
    /// </summary>
    public string? OnEntryMethod { get; set; }

    /// <summary>
    /// Nazwa metody OnExit (wykonywana przy wyjściu ze stanu)
    /// </summary>
    public string? OnExitMethod { get; set; }

    #endregion

    #region Signature Information

    /// <summary>
    /// Complete signature information for the OnEntry method
    /// </summary>
    public CallbackSignatureInfo OnEntrySignature { get; set; } = CallbackSignatureInfo.Empty;

    /// <summary>
    /// Complete signature information for the OnExit method
    /// </summary>
    public CallbackSignatureInfo OnExitSignature { get; set; } = CallbackSignatureInfo.Empty;

    #endregion

    #region Convenience Properties (Derived from Signatures)

    /// <summary>
    /// Whether the OnEntry method is async
    /// </summary>
    public bool OnEntryIsAsync
    {
        get => OnEntrySignature.IsAsync;
        set
        {
            var sig = OnEntrySignature;
            sig.IsAsync = value;
            OnEntrySignature = sig;
        }
    }

    /// <summary>
    /// Whether the OnExit method is async
    /// </summary>
    public bool OnExitIsAsync
    {
        get => OnExitSignature.IsAsync;
        set
        {
            var sig = OnExitSignature;
            sig.IsAsync = value;
            OnExitSignature = sig;
        }
    }

    /// <summary>
    /// Whether the OnEntry expects a payload parameter
    /// </summary>
    public bool OnEntryExpectsPayload
    {
        get => OnEntrySignature.HasPayloadOnly || OnEntrySignature.HasPayloadAndToken;
        set
        {
            var sig = OnEntrySignature;
            sig.HasPayloadOnly = value;
            OnEntrySignature = sig;
        }
    }

    /// <summary>
    /// Whether the OnEntry has a parameterless overload
    /// </summary>
    public bool OnEntryHasParameterlessOverload
    {
        get => OnEntrySignature.HasParameterless;
        set
        {
            var sig = OnEntrySignature;
            sig.HasParameterless = value;
            OnEntrySignature = sig;
        }
    }

    /// <summary>
    /// Whether the OnExit expects a payload parameter
    /// </summary>
    public bool OnExitExpectsPayload
    {
        get => OnExitSignature.HasPayloadOnly || OnExitSignature.HasPayloadAndToken;
        set
        {
            var sig = OnExitSignature;
            sig.HasPayloadOnly = value;
            OnExitSignature = sig;
        }
    }

    /// <summary>
    /// Whether the OnExit has a parameterless overload
    /// </summary>
    public bool OnExitHasParameterlessOverload
    {
        get => OnExitSignature.HasParameterless;
        set
        {
            var sig = OnExitSignature;
            sig.HasParameterless = value;
            OnExitSignature = sig;
        }
    }

    #endregion

    #region HSM Properties

    /// <summary>
    /// Parent state name for hierarchical state machines
    /// </summary>
    public string? ParentState { get; set; }

    /// <summary>
    /// List of child state names for composite states
    /// </summary>
    public List<string> ChildStates { get; set; } = new List<string>();

    /// <summary>
    /// Whether this state is a composite state (has children)
    /// </summary>
    public bool IsComposite => ChildStates.Count > 0;

    /// <summary>
    /// History mode for this composite state
    /// </summary>
    public HistoryMode History { get; set; } = HistoryMode.None;

    /// <summary>
    /// Whether this state is marked as the initial substate of its parent
    /// </summary>
    public bool IsInitial { get; set; } = false;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a state model with the specified properties
    /// </summary>
    public static StateModel Create(
        string name,
        string? onEntryMethod = null,
        string? onExitMethod = null)
    {
        return new StateModel
        {
            Name = name,
            OnEntryMethod = onEntryMethod,
            OnExitMethod = onExitMethod
        };
    }

    #endregion

    /// <inheritdoc/>
    public override string ToString() => Name;
}