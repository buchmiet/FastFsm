namespace Generator.SourceGenerators;

internal sealed class ExtensionsFeatureWriter
{
    public void WriteFields(IndentedStringBuilder.IndentedStringBuilder sb, string stateType, string triggerType)
    {
        sb.AppendLine("private readonly object _extensionsLock = new();");
        sb.AppendLine($"private ExtensionSet<{stateType}, {triggerType}> _extensionSet;");
        sb.AppendLine("private readonly ExtensionRunner _extensionRunner;");
        sb.AppendLine();
        sb.AppendLine($"public IReadOnlyList<IStateMachineExtension<{stateType}, {triggerType}>> Extensions => System.Threading.Volatile.Read(ref _extensionSet).PublicItems;");
        sb.AppendLine();
    }

    public void WriteConstructorBody(IndentedStringBuilder.IndentedStringBuilder sb, bool generateLogging, string stateType, string triggerType)
    {
        sb.AppendLine($"_extensionSet = ExtensionSet<{stateType}, {triggerType}>.Create(extensions);");
        sb.AppendLine(generateLogging
            ? "_extensionRunner = new ExtensionRunner(_logger);"
            : "_extensionRunner = new ExtensionRunner();");
    }

    public void WriteManagementMethods(IndentedStringBuilder.IndentedStringBuilder sb, string stateType, string triggerType)
    {
        using (sb.Block($"public void AddExtension(IStateMachineExtension<{stateType}, {triggerType}> extension)"))
        {
            sb.AppendLine("if (extension == null) throw new ArgumentNullException(nameof(extension));");
            using (sb.Block("lock (_extensionsLock)"))
            {
                sb.AppendLine("var current = _extensionSet.Items;");
                sb.AppendLine($"var updated = new IStateMachineExtension<{stateType}, {triggerType}>[current.Length + 1];");
                sb.AppendLine("Array.Copy(current, updated, current.Length);");
                sb.AppendLine("updated[current.Length] = extension;");
                sb.AppendLine($"System.Threading.Volatile.Write(ref _extensionSet, ExtensionSet<{stateType}, {triggerType}>.Create(updated));");
            }
        }
        sb.AppendLine();

        using (sb.Block($"public bool RemoveExtension(IStateMachineExtension<{stateType}, {triggerType}> extension)"))
        {
            sb.AppendLine("if (extension == null) return false;");
            using (sb.Block("lock (_extensionsLock)"))
            {
                sb.AppendLine("var current = _extensionSet.Items;");
                sb.AppendLine("var index = Array.IndexOf(current, extension);");
                sb.AppendLine("if (index < 0) return false;");
                sb.AppendLine($"var updated = new IStateMachineExtension<{stateType}, {triggerType}>[current.Length - 1];");
                sb.AppendLine("if (index > 0) Array.Copy(current, 0, updated, 0, index);");
                sb.AppendLine("if (index < current.Length - 1) Array.Copy(current, index + 1, updated, index, current.Length - index - 1);");
                sb.AppendLine($"System.Threading.Volatile.Write(ref _extensionSet, ExtensionSet<{stateType}, {triggerType}>.Create(updated));");
                sb.AppendLine("return true;");
            }
        }
    }
}