using System;
using System.Text;

namespace Generator.ModernGeneration.Policies
{
    /// <summary>
    /// Polityka dla maszyn synchronicznych - wszystkie transformacje zwracają wartości synchroniczne.
    /// </summary>
    public sealed class AsyncPolicySync : IAsyncPolicy
    {
        public bool IsAsync => false;
        public bool UseSyncAdapter => false;

        public string ReturnType(string syncType) => syncType;
        public string AsyncKeyword() => "";
        public string AwaitKeyword(bool targetIsAsync) => "";
        public string ConfigureAwait() => "";
        public string CancellationTokenParam() => "";
        public string CancellationTokenArg() => "";
        
        public string MethodName(string baseName, bool addAsyncSuffix = true) => baseName;
        
        public void EmitInvocation(StringBuilder sb, string methodName, bool methodIsAsync, params string[]? args)
        {
            var argsList = args is { Length: > 0 } ? string.Join(", ", args) : "";
            sb.AppendLine($"{methodName}({argsList});");
        }
    }
}
