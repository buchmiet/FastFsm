using System.Text;

namespace IndentedStringBuilder;

/// <summary>
/// Lightweight wrapper over <see cref="StringBuilder"/> that tracks indentation.
/// </summary>
public sealed class IndentedStringBuilder(string indentUnit = "    ")
{
    private readonly StringBuilder _sb = new();
    private int _level;


    /*---------- public API ----------*/

    public IDisposable Indent()
    {
        _level++;
        return new PopIndent(this);
    }

    public IndentedStringBuilder AppendLine(string? text = null)
    {
        if (!string.IsNullOrEmpty(text))
        {
            for (int i = 0; i < _level; i++)
                _sb.Append(indentUnit);

            _sb.Append(text);
        }

        _sb.AppendLine();
        return this;
    }

    public IndentedStringBuilder Append(string text)
    {
        _sb.Append(text);
        return this;
    }

    public IDisposable Block(string header)
    {
        if (!string.IsNullOrEmpty(header))
        {
            AppendLine($"{header} {{");
        }
        else
        {
            AppendLine("{");
        }
        var indent = Indent();
        return new DisposableAction(() =>
        {
            indent.Dispose();
            AppendLine("}");
        });
    }

    /// <summary>
    /// Creates a block with the header and opening brace on the same line
    /// Example: "try {" instead of "try" on one line and "{" on the next
    /// </summary>
    public IDisposable InlineBlock(string header)
    {
        AppendLine($"{header} {{");
        var indent = Indent();
        return new DisposableAction(() =>
        {
            indent.Dispose();
            AppendLine("}");
        });
    }

    /// <summary>
    /// Emits <c>#if condition</c> … <c>#endif</c> at the current indent.
    /// Use <see cref="ElseDirective"/> inside the scope for <c>#else</c>.
    /// </summary>
    public IDisposable IfDirective(string condition)
    {
        AppendLine($"#if {condition}");
        return new DisposableAction(() => AppendLine("#endif"));
    }

    /// <summary>
    /// Emits <c>#else</c>. Call inside <see cref="IfDirective"/>.
    /// </summary>
    public void ElseDirective() => AppendLine("#else");

    /// <summary>
    /// Emits <c>switch (expression) { … }</c>.
    /// </summary>
    public IDisposable Switch(string expression) => Block($"switch ({expression})");

    /// <summary>
    /// Emits <c>case matcher:</c> followed by a braced body. Write <c>break;</c> (or <c>return</c>) inside the scope.
    /// </summary>
    public IDisposable Case(string matcher, bool braces = true)
    {
        AppendLine($"case {matcher}:");
        return braces ? Block("") : Indent();
    }

    public void DefaultBreak() => AppendLine("default: break;");

    public void DefaultReturn(string expression) => AppendLine($"default: return {expression};");

    /// <summary>
    /// Writes an XML documentation summary comment
    /// </summary>
    public void WriteSummary(string summary)
    {
        AppendLine("/// <summary>");
        AppendLine($"/// {summary}");
        AppendLine("/// </summary>");
    }

    public void WriteParam(string paramName, string description) => AppendLine($"/// <param name=\"{paramName}\">{description}</param>");

    public void WriteReturns(string description) => AppendLine($"/// <returns>{description}</returns>");

    public void AddProperty(string typeAndName, string? value = null) => AppendLine($"{typeAndName}{(value is not null ? $"={value}" : string.Empty)};");


    public override string ToString() => _sb.ToString();

    /*---------- implementation ----------*/

    private void Pop() => _level--;

    private sealed class PopIndent(IndentedStringBuilder parent) : IDisposable
    {
        public void Dispose() => parent.Pop();
    }
}
