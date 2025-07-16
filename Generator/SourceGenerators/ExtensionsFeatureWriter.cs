namespace Generator.SourceGenerators;

internal sealed class ExtensionsFeatureWriter
{
    public void WriteFields(IndentedStringBuilder.IndentedStringBuilder sb)
    {
        sb.AppendLine("private readonly List<IStateMachineExtension> _extensionsList;");
        sb.AppendLine("private readonly IReadOnlyList<IStateMachineExtension> _extensions;");
        sb.AppendLine("private readonly ExtensionRunner _extensionRunner;");
        sb.AppendLine();
        sb.AppendLine("public IReadOnlyList<IStateMachineExtension> Extensions => _extensions;");
        sb.AppendLine();
    }

    public void WriteConstructorBody(IndentedStringBuilder.IndentedStringBuilder sb, bool generateLogging)
    {
        sb.AppendLine("_extensionsList = extensions?.ToList() ?? new List<IStateMachineExtension>();");
        sb.AppendLine("_extensions = _extensionsList;");
        sb.AppendLine(generateLogging
            ? "_extensionRunner = new ExtensionRunner(_logger);"
            : "_extensionRunner = new ExtensionRunner();");
    }

    public void WriteManagementMethods(IndentedStringBuilder.IndentedStringBuilder sb)
    {
        using (sb.Block("public void AddExtension(IStateMachineExtension extension)"))
        {
            sb.AppendLine("if (extension == null) throw new ArgumentNullException(nameof(extension));");
            sb.AppendLine("_extensionsList.Add(extension);");
        }
        sb.AppendLine();

        using (sb.Block("public bool RemoveExtension(IStateMachineExtension extension)"))
        {
            sb.AppendLine("if (extension == null) return false;");
            sb.AppendLine("return _extensionsList.Remove(extension);");
        }
    }
}