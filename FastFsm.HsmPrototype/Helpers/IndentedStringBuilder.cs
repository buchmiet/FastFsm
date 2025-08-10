using System;
using System.Text;

namespace FastFsm.HsmPrototype.Helpers;

public class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel = 0;
    private bool _newLine = true;
    
    public void Indent() => _indentLevel++;
    public void Outdent() => _indentLevel = Math.Max(0, _indentLevel - 1);
    
    public IndentedStringBuilder AppendLine(string text = "")
    {
        if (!string.IsNullOrEmpty(text))
        {
            if (_newLine)
            {
                _sb.Append(new string(' ', _indentLevel * 4));
            }
            _sb.Append(text);
        }
        _sb.AppendLine();
        _newLine = true;
        return this;
    }
    
    public IndentedStringBuilder OpenBrace()
    {
        AppendLine("{");
        Indent();
        return this;
    }
    
    public IndentedStringBuilder CloseBrace()
    {
        Outdent();
        AppendLine("}");
        return this;
    }
    
    public override string ToString() => _sb.ToString();
}