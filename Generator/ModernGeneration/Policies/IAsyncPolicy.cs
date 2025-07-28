namespace FastStateMachine.Generator.ModernGeneration.Policies;

public interface IAsyncPolicy
{
    // Transformacje typów zwracanych
    string ReturnType(string syncType);
    
    // Słowa kluczowe
    string AsyncKeyword();
    string AwaitKeyword(bool targetIsAsync);
    string ConfigureAwait();
    
    // Parametry i argumenty
    string CancellationTokenParam();
    string CancellationTokenArg();
    
    // Nazwy metod
    string MethodName(string baseName, bool addAsyncSuffix = true);
    
    // Helpery do emisji kodu
    void EmitInvocation(System.Text.StringBuilder sb, string methodName, bool methodIsAsync, params string[]? args);
    
    // Właściwości
    bool IsAsync { get; }
    bool UseSyncAdapter { get; }
}
