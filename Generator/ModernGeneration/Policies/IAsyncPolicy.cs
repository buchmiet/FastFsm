using System.Text;

namespace Generator.ModernGeneration.Policies
{
    /// <summary>
    /// Polityka określająca sposób generowania kodu dla maszyn synchronicznych i asynchronicznych.
    /// </summary>
    public interface IAsyncPolicy
    {
        /// <summary>
        /// Transformuje typ zwracany z synchronicznego na asynchroniczny.
        /// </summary>
        string ReturnType(string syncType);
        
        /// <summary>
        /// Zwraca słowo kluczowe "async" lub pusty string.
        /// </summary>
        string AsyncKeyword();
        
        /// <summary>
        /// Zwraca słowo kluczowe "await" lub pusty string.
        /// </summary>
        string AwaitKeyword(bool targetIsAsync);
        
        /// <summary>
        /// Zwraca fragment ConfigureAwait lub pusty string.
        /// </summary>
        string ConfigureAwait();
        
        /// <summary>
        /// Zwraca parametr CancellationToken lub pusty string.
        /// </summary>
        string CancellationTokenParam();
        
        /// <summary>
        /// Zwraca argument CancellationToken lub pusty string.
        /// </summary>
        string CancellationTokenArg();
        
        /// <summary>
        /// Zwraca nazwę metody z odpowiednim sufiksem.
        /// </summary>
        string MethodName(string baseName, bool addAsyncSuffix = true);
        
        /// <summary>
        /// Helper do emisji wywołania metody z obsługą async/await.
        /// </summary>
        void EmitInvocation(StringBuilder sb, string methodName, bool methodIsAsync, params string[]? args);
        
        /// <summary>
        /// Czy to polityka asynchroniczna.
        /// </summary>
        bool IsAsync { get; }
        
        /// <summary>
        /// Czy używać adaptera synchronicznego (dla async-first).
        /// </summary>
        bool UseSyncAdapter { get; }
    }
}
