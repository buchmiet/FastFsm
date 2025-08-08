using System;
using System.Text;
using IndentedStringBuilder;

namespace Generator.Helpers
{
    /// <summary>
    /// Centralizuje transformacje sync→async dla generowania kodu maszyn stanów.
    /// Eliminuje if(IsAsync) rozproszone po kodzie.
    /// </summary>
    public static class AsyncGenerationHelper
    {
        /// <summary>
        /// Zwraca odpowiedni typ zwracany dla metody w zależności od trybu sync/async.
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
        /// Zwraca modyfikatory metody (async keyword).
        /// </summary>
        public static string GetMethodModifiers(bool isAsync)
        {
            return isAsync ? "async " : "";
        }

        /// <summary>
        /// Zwraca await keyword jeśli potrzebny.
        /// </summary>
        public static string GetAwaitKeyword(bool targetMethodIsAsync, bool callerIsAsync)
        {
            return callerIsAsync && targetMethodIsAsync ? "await " : "";
        }

        /// <summary>
        /// Zwraca ConfigureAwait call jeśli potrzebny.
        /// </summary>
        public static string GetConfigureAwait(bool isAsync, bool continueOnCapturedContext)
        {
            return isAsync
                ? $".ConfigureAwait({continueOnCapturedContext.ToString().ToLowerInvariant()})"
                : "";
        }

        /// <summary>
        /// Generuje pełną sygnaturę metody z obsługą async.
        /// </summary>
        public static string GetMethodSignature(
            string methodName,
            string returnType,
            string parameters,
            bool isAsync,
            string visibility = "public")
        {
            var asyncKeyword = GetMethodModifiers(isAsync);
            var asyncReturnType = GetReturnType(returnType, isAsync);
            return $"{visibility} {asyncKeyword}{asyncReturnType} {methodName}({parameters})";
        }

        /// <summary>
        /// Generuje wywołanie metody z obsługą await i ConfigureAwait.
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
        /// Generuje return statement z obsługą await.
        /// </summary>
        public static void EmitReturn(
            IndentedStringBuilder.IndentedStringBuilder sb,
            string expression,
            bool isAsync,
            bool methodReturnsTask,
            bool continueOnCapturedContext)
        {
            if (isAsync && methodReturnsTask)
            {
                var configureAwait = GetConfigureAwait(true, continueOnCapturedContext);
                sb.AppendLine($"return await {expression}{configureAwait};");
            }
            else
            {
                sb.AppendLine($"return {expression};");
            }
        }

        /// <summary>
        /// Zwraca odpowiednią nazwę metody z sufiksem Async jeśli potrzebny.
        /// </summary>
        public static string GetMethodName(string baseName, bool isAsync, bool addAsyncSuffix = true)
        {
            if (!isAsync || !addAsyncSuffix) return baseName;

            // Sprawdź czy nazwa już kończy się na "Async"
            if (baseName.EndsWith("Async", StringComparison.Ordinal))
                return baseName;

            return baseName + "Async";
        }

        /// <summary>
        /// Zwraca nazwę klasy bazowej dla maszyny stanów.
        /// </summary>
        public static string GetBaseClassName(string stateType, string triggerType, bool isAsync)
        {
            return isAsync
                ? $"AsyncStateMachineBase<{stateType}, {triggerType}>"
                : $"StateMachineBase<{stateType}, {triggerType}>";
        }

        /// <summary>
        /// Zwraca nazwę interfejsu dla maszyny stanów.
        /// </summary>
        public static string GetInterfaceName(string stateType, string triggerType, bool isAsync)
        {
            return isAsync
                ? $"IStateMachineAsync<{stateType}, {triggerType}>"
                : $"IStateMachineSync<{stateType}, {triggerType}>";
        }

        /// <summary>
        /// Generuje fire-and-forget async call dla konstruktora (initial OnEntry).
        /// </summary>
        public static void EmitFireAndForgetAsyncCall(
            IndentedStringBuilder.IndentedStringBuilder sb,
            Action<IndentedStringBuilder.IndentedStringBuilder> generateAsyncCode)
        {
            sb.AppendLine("_ = Task.Run(async () => {");
            using (sb.Indent())
            {
                generateAsyncCode(sb);
            }
            sb.AppendLine("});");
        }
    }
}