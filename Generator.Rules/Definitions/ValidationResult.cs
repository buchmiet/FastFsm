using System;

namespace Generator.Rules.Definitions;

public class ValidationResult
{
    public bool IsValid { get; }
    public string? RuleId { get; } 
    public string? Message { get; } 
    public RuleSeverity Severity { get; }

    private ValidationResult(bool isValid, string? ruleId, string? message, RuleSeverity severity)
    {
        IsValid = isValid;
        RuleId = ruleId;
        Message = message;
        Severity = severity;
    }

    public static ValidationResult Success() => new(true, null, null, RuleSeverity.Info); // Info or a specific "SuccessSeverity"

    public static ValidationResult Fail(string ruleId, string message, RuleSeverity severity)
    {
        if (string.IsNullOrEmpty(ruleId)) throw new ArgumentNullException(nameof(ruleId));
        if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));
        return new ValidationResult(false, ruleId, message, severity);
    }
}