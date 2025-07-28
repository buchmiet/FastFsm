using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace StateMachine.Tests.Exceptions
{
    /// <summary>
    /// Prosta implementacja ILogger do przechwytywania logów w testach.
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public List<string> LoggedErrors { get; } = [];

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                var message = formatter(state, exception);
                LoggedErrors.Add(message);
            }
        }
    }
}
