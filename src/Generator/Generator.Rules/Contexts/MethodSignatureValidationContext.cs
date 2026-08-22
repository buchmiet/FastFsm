namespace Generator.Rules.Contexts
{
    // Context for callback method-signature validation
    public class MethodSignatureValidationContext(
        string methodName,
        string callbackType,
        string expectedReturnType,
        bool parametersAllowed)
    {
        public string MethodName { get; } = methodName;
        public string CallbackType { get; } = callbackType; // "Guard", "Action", "OnEntry", "OnExit"
        public string ExpectedReturnType { get; } = expectedReturnType; // e.g. "bool" or "void"
        public bool ParametersAllowed { get; } = parametersAllowed; // Typically false for Pure/Basic

        // Actual method discovered by the parser/analyzer
        public bool MethodFound { get; set; }
        public string ActualReturnType { get; set; } = string.Empty;
        public int ActualParameterCount { get; set; }
        public string? ExpectedParameterType { get; set; } // Expected parameter type (when payload)
        public string? ActualParameterType { get; set; }   // Actual parameter type found
    }
}
