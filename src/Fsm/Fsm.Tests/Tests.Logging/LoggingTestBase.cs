using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Tests.Logging;

/// <summary>
/// Base class for all logging tests
/// </summary>
public abstract class LoggingTestBase
{
    // LoggingTestBase.cs
    protected Mock<ILogger<T>> GetLoggerMock<T>() where T : class
    {
        // Extract the generic interface from a single shared mock
        var typed = LoggerMock.As<ILogger<T>>();

        // Forward IsEnabled -> always true (as in the base mock)
        typed.Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);

        // Forward Log -> the same LoggedMessages list
        typed.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception?, Delegate>((lvl, id, state, ex, fmt) =>
            {
                var msg = fmt.DynamicInvoke(state, ex)?.ToString() ?? string.Empty;
                LoggedMessages.Add((lvl, id, msg));
            });

        return typed;
    }



    protected Mock<ILogger> LoggerMock { get; }
    protected List<(LogLevel Level, EventId EventId, string Message)> LoggedMessages { get; }

    protected LoggingTestBase()
    {
        LoggerMock = new Mock<ILogger>();
        LoggedMessages = new();

        // --- capture Log ----------------------------------------
        LoggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception?, Delegate>((lvl, id, state, ex, fmt) =>
            {
                var msg = fmt.DynamicInvoke(state, ex)?.ToString() ?? string.Empty;
                LoggedMessages.Add((lvl, id, msg));
            });

        // --- all levels enabled by default ------------------------
        LoggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    /*  -------  NEW methods  ----------------------------------------- */

    /// <summary>
    /// Returns a generic logger that delegates to the shared <see cref="LoggerMock"/>.
    /// This way the IsEnabled configuration on LoggerMock
    /// also applies to ILogger&lt;T&gt;.
    /// </summary>
    protected ILogger<T> GetLogger<T>() where T : class
        => new DelegatingLogger<T>(LoggerMock.Object);

    private sealed class DelegatingLogger<T> : ILogger<T>
    {
        private readonly ILogger _inner;
        public DelegatingLogger(ILogger inner) => _inner = inner;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId,
                                TState state, Exception? exception,
                                Func<TState, Exception?, string> formatter)
            => _inner.Log(logLevel, eventId, state, exception, formatter);
    }

    protected void VerifyLogMessage(LogLevel expectedLevel, string expectedEventName, params string[] expectedMessageParts)
    {
        var matchingLog = LoggedMessages.FirstOrDefault(log =>
            log.Level == expectedLevel &&
            log.EventId.Name == expectedEventName);

        matchingLog.ShouldNotBe(default);
        matchingLog.Message.ShouldNotBeNull();

        foreach (var part in expectedMessageParts)
        {
            matchingLog.Message.ShouldContain(part);
        }
    }

    protected void VerifyLogCount(int expectedCount) => LoggedMessages.Count.ShouldBe(expectedCount);

    protected void VerifyNoLogs() => LoggedMessages.ShouldBeEmpty();
}

/// <summary>
/// Test states for all variants
/// </summary>
public enum TestState
{
    Initial,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Test triggers for all variants
/// </summary>
public enum TestTrigger
{
    Start,
    Process,
    Complete,
    Fail,
    Reset
}