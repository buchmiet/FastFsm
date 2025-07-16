namespace Generator.Model;

public class StateMachineModel
{
    public string Namespace { get; set; } = "";
    public string ClassName { get; set; } = "";
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


}
