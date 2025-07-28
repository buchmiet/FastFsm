namespace IndentedStringBuilder;

/// <summary>
///  Wywołuje podaną akcję przy Dispose().
/// </summary>
internal sealed class DisposableAction(Action action) : IDisposable
{
    private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _action();
    }
}