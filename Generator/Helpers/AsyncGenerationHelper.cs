using System;
using System.Text;
using IndentedStringBuilder;

namespace Generator.Helpers
{
    /// <summary>
    /// Centralizes sync→async transformations for state machine code generation.
    /// Eliminates if(IsAsync) scattered throughout the code.
    /// </summary>
    public static class AsyncGenerationHelper
    {
        /// <summary>
        /// Returns the appropriate return type for a method depending on sync/async mode.
        /// </summary>
        public static string GetReturnType(string syncType, bool isAsync)
        {
            if (!isAsync) return syncType;

            return syncType switch
            {
                "void" => "Task",
                "bool" => "ValueTask<bool>",
                var t when t.StartsWith("IReadOnlyList<") => $"ValueTask<{t}>",
                var t when t.StartsWith("List<") => $"Task<{t}>",
                _ => $"ValueTask<{syncType}>"
            };
        }

        /// <summary>
        /// Returns method modifiers (async keyword).
        /// </summary>
        public static string GetMethodModifiers(bool isAsync)
        {
            return isAsync ? "async " : "";
        }

        /// <summary>
        /// Returns await keyword if needed.
        /// </summary>
        public static string GetAwaitKeyword(bool targetMethodIsAsync, bool callerIsAsync)
        {
            return callerIsAsync && targetMethodIsAsync ? "await " : "";
        }

        /// <summary>
        /// Returns ConfigureAwait call if needed.
        /// </summary>
        public static string GetConfigureAwait(bool isAsync, bool continueOnCapturedContext)
        {
            return isAsync
                ? $".ConfigureAwait({continueOnCapturedContext.ToString().ToLowerInvariant()})"
                : "";
        }

        /// <summary>
        /// Generates method invocation with await and ConfigureAwait handling.
        /// </summary>
        public static void EmitMethodInvocation(
            IndentedStringBuilder.IndentedStringBuilder sb,
            string methodName,
            bool methodIsAsync,
            bool callerIsAsync,
            bool continueOnCapturedContext,
            params string[] args)
        {
            var argsStr = args.Length > 0 ? string.Join(", ", args) : "";

            if (callerIsAsync && methodIsAsync)
            {
                var configureAwait = GetConfigureAwait(true, continueOnCapturedContext);
                sb.AppendLine($"await {methodName}({argsStr}){configureAwait};");
            }
            else
            {
                sb.AppendLine($"{methodName}({argsStr});");
            }
        }

        /// <summary>
        /// Returns the appropriate method name with Async suffix if needed.
        /// </summary>
        public static string GetMethodName(string baseName, bool isAsync, bool addAsyncSuffix = true)
        {
            if (!isAsync || !addAsyncSuffix) return baseName;

            // Check if name already ends with "Async"
            if (baseName.EndsWith("Async", StringComparison.Ordinal))
                return baseName;

            return baseName + "Async";
        }

        /// <summary>
        /// Returns the base class name for the state machine.
        /// </summary>
        public static string GetBaseClassName(string stateType, string triggerType, bool isAsync)
        {
            return isAsync
                ? $"AsyncStateMachineBase<{stateType}, {triggerType}>"
                : $"StateMachineBase<{stateType}, {triggerType}>";
        }

        /// <summary>
        /// Returns the interface name for the state machine.
        /// </summary>
        public static string GetInterfaceName(string stateType, string triggerType, bool isAsync)
        {
            return isAsync
                ? $"IStateMachineAsync<{stateType}, {triggerType}>"
                : $"IStateMachineSync<{stateType}, {triggerType}>";
        }

        /// <summary>
        /// Generates fire-and-forget async call for constructor (initial OnEntry).
        /// </summary>
        public static void EmitFireAndForgetAsyncCall(
            IndentedStringBuilder.IndentedStringBuilder sb,
            Action<IndentedStringBuilder.IndentedStringBuilder> generateAsyncCode)
        {
            sb.AppendLine("_ = Task.Run(async () =>");
            using (sb.Block(""))
            {
                generateAsyncCode(sb);
            }
            sb.Append(");");
        }
    }
}