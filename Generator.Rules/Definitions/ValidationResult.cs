using System;

namespace Generator.Rules.Definitions
{
    /// <summary>
    /// Validation outcome returned by rules; either success or failure with message/severity.
    /// </summary>
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

        /// <summary>
        /// Represents a 'no issues' result; IsValid = true. Severity value is not meaningful.
        /// </summary>
        public static ValidationResult Success() => new(true, null, null, RuleSeverity.Info);

        /// <summary>
        /// Creates a failure; IsValid = false; ruleId and message must be non-empty.
        /// </summary>
        public static ValidationResult Fail(string ruleId, string message, RuleSeverity severity)
        {
            if (string.IsNullOrEmpty(ruleId)) throw new ArgumentNullException(nameof(ruleId));
            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));
            return new ValidationResult(false, ruleId, message, severity);
        }
    }
}