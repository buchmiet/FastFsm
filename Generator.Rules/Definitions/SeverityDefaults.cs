namespace Generator.Rules.Definitions
{
    /// <summary>
    /// Roslyn-free severity helpers for consumers that don't reference Microsoft.CodeAnalysis.
    /// </summary>
    public static class SeverityDefaults
    {
        /// <summary>
        /// Lightweight local copy of a severity enum to avoid referencing Roslyn from this assembly.
        /// </summary>
        public enum DiagnosticSeverityCompat
        {
            Hidden = 0,
            Info   = 1,
            Warning= 2,
            Error  = 3
        }

        /// <summary>
        /// Converts a <see cref="RuleSeverity"/> to <see cref="DiagnosticSeverityCompat"/>.
        /// </summary>
        public static DiagnosticSeverityCompat ToDiagnosticSeverityCompat(RuleSeverity s)
            => s switch
            {
                RuleSeverity.Error   => DiagnosticSeverityCompat.Error,
                RuleSeverity.Warning => DiagnosticSeverityCompat.Warning,
                _                    => DiagnosticSeverityCompat.Info,
            };
    }
}