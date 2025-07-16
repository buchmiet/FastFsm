

namespace Generator.Rules.Contexts;

// Kontekst dla walidacji sygnatury metody callback
public class MethodSignatureValidationContext(
    string methodName,
    string callbackType,
    string expectedReturnType,
    bool parametersAllowed)
{
    public string MethodName { get; } = methodName;
    public string CallbackType { get; } = callbackType; // "Guard", "Action", "OnEntry", "OnExit"
    public string ExpectedReturnType { get; } = expectedReturnType; // Np. "bool" lub "void"
    public bool ParametersAllowed { get; } = parametersAllowed; // Dla Pure/Basic zazwyczaj false

    // Informacje o rzeczywistej metodzie znalezionej przez parser/analyzer
    public bool MethodFound { get; set; }
    public string ActualReturnType { get; set; }
    public int ActualParameterCount { get; set; }
    public string? ExpectedParameterType { get; set; } // Oczekiwany typ parametru (jeśli payload)
    public string? ActualParameterType { get; set; }   // Rzeczywisty typ parametru znaleziony
}