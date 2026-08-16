using Microsoft.Extensions.Logging;

namespace FastFsm.Logging.Tests.TestHelpers;

internal static class LoggerAdapter
{
    public static ILogger<T>? For<T>(ILogger? logger) where T : class
        => logger == null ? null : logger as ILogger<T> ?? new DelegatingLogger<T>(logger);

    private sealed class DelegatingLogger<T> : ILogger<T>
    {
        private readonly ILogger _inner;
        public DelegatingLogger(ILogger inner) => _inner = inner;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _inner.Log(logLevel, eventId, state!, exception, formatter!);
    }
}

