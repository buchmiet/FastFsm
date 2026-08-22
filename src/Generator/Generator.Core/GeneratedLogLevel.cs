namespace Generator.SourceGenerators;

/// <summary>
/// Log level of a generated <c>*Log.</c> call. Trace is included because HSM path logging uses it;
/// the four primary levels are Debug, Information, Warning, and Error.
/// </summary>
internal enum GeneratedLogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error
}
