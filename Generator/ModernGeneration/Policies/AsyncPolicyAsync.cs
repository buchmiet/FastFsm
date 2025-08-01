﻿using System;
using System.Text;
using System.Threading;

namespace Generator.ModernGeneration.Policies
{
    /// <summary>
    /// Polityka dla maszyn asynchronicznych - transformuje typy i dodaje async/await.
    /// </summary>
    public sealed class AsyncPolicyAsync : IAsyncPolicy
    {
        private readonly bool _continueOnCapturedContext;
        
        public AsyncPolicyAsync(bool continueOnCapturedContext = false)
        {
            _continueOnCapturedContext = continueOnCapturedContext;
        }
        
        public bool IsAsync => true;
        public bool UseSyncAdapter => false;

        public string ReturnType(string syncType) => syncType switch
        {
            "void" => "Task",
            "bool" => "ValueTask<bool>",
            _ => $"ValueTask<{syncType}>"
        };
        
        public string AsyncKeyword() => "async ";
        public string AwaitKeyword(bool targetIsAsync) => targetIsAsync ? "await " : "";
        public string ConfigureAwait() => $".ConfigureAwait({(_continueOnCapturedContext ? "true" : "false")})";
        
        public string CancellationTokenParam() => ", CancellationToken cancellationToken = default";
        public string CancellationTokenArg() => ", cancellationToken";

       
        public string MethodName(string baseName, bool addAsyncSuffix = true)
        {
            // Upewnij się że zawsze dodajesz Async suffix dla metod wewnętrznych
            if (baseName.Contains("Internal") && !baseName.EndsWith("Async"))
                return baseName + "Async";

            return addAsyncSuffix && !baseName.EndsWith("Async", StringComparison.Ordinal)
                ? baseName + "Async"
                : baseName;
        }

        public void EmitInvocation(StringBuilder sb, string methodName, bool methodIsAsync, params string[]? args)
        {
            var argsList = args is { Length: > 0 } ? string.Join(", ", args) : "";
            var awaitKw = AwaitKeyword(methodIsAsync);
            var cfg = methodIsAsync ? ConfigureAwait() : "";
            sb.AppendLine($"{awaitKw}{methodName}({argsList}){cfg};");
        }
    }
}
