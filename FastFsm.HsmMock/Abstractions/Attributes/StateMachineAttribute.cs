namespace Abstractions.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StateMachineAttribute : System.Attribute
    {
        public StateMachineAttribute(System.Type stateType, System.Type triggerType) { }
        public System.Type? DefaultPayloadType { get; set; }
        public bool GenerateStructuralApi { get; set; }
        public bool ContinueOnCapturedContext { get; set; }
        public bool EnableHierarchy { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class PayloadTypeAttribute : System.Attribute
    {
        public PayloadTypeAttribute(System.Type defaultPayload) { }
        public PayloadTypeAttribute(object trigger, System.Type payload) { }
    }
}