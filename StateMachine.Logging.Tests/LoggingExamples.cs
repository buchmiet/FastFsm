using Abstractions.Attributes;
using Microsoft.Extensions.Logging;
using StateMachine.Contracts;
using Xunit.Abstractions;

namespace StateMachine.Logging.Tests
{
    /// <summary>
    /// Example tests showing actual log output
    /// These tests use real console output for debugging purposes
    /// </summary>
    public class LoggingExamples
    {
        private readonly ITestOutputHelper _output;
        private readonly ILoggerFactory _loggerFactory;

        public LoggingExamples(ITestOutputHelper output)
        {
            _output = output;

            // Create a logger factory that writes to xUnit output
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(new XUnitLoggerProvider(output));
            });
        }

        [Fact]
        public void Example_BasicTransitionWithLogging()
        {
            // Arrange
            var logger = _loggerFactory.CreateLogger<ExampleStateMachine>();
            var machine = new ExampleStateMachine(OrderState.New, logger);

            // Act
            _output.WriteLine("=== Starting state machine test ===");
            _output.WriteLine($"Initial state: {machine.CurrentState}");

            var result = machine.TryFire(OrderTrigger.Submit);

            // Assert
            _output.WriteLine($"Transition result: {result}");
            _output.WriteLine($"Final state: {machine.CurrentState}");
            _output.WriteLine("=== Test completed ===");
        }

        [Fact]
        public void Example_FailedGuardWithLogging()
        {
            // Arrange
            var logger = _loggerFactory.CreateLogger<GuardedStateMachine>();
            var machine = new GuardedStateMachine(ProcessState.Idle, logger);
            machine.CanProcess = false; // Guard will fail

            // Act
            _output.WriteLine("=== Testing failed guard ===");
            var result = machine.TryFire(ProcessTrigger.Start);

            // Assert
            _output.WriteLine($"Guard prevented transition: {!result}");
            _output.WriteLine($"State remained: {machine.CurrentState}");
        }

        [Fact]
        public void Example_ExtensionWithLogging()
        {
            // Arrange
            var logger = _loggerFactory.CreateLogger<ExtensibleMachine>();
            var extension = new LoggingExtension(_output);
            var machine = new ExtensibleMachine(WorkflowState.Draft, new[] { extension }, logger);

            // Act
            _output.WriteLine("=== Testing with extension ===");
            machine.TryFire(WorkflowTrigger.Submit);

            // Assert
            _output.WriteLine("=== Extension callbacks completed ===");
        }
    }

    // Simple logger provider for xUnit
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public XUnitLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_output, categoryName);
        }

        public void Dispose() { }
    }

    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _categoryName;

        public XUnitLogger(ITestOutputHelper output, string categoryName)
        {
            _output = output;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}");
            if (exception != null)
            {
                _output.WriteLine($"  Exception: {exception}");
            }
        }
    }

    // Example state machines for demonstration
    public enum OrderState { New, Submitted, Shipped }
    public enum OrderTrigger { Submit, Ship }

    [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
    public partial class ExampleStateMachine
    {
        [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted)]
        private void Configure() { }
    }

    public enum ProcessState { Idle, Running, Completed }
    public enum ProcessTrigger { Start, Complete }

    [StateMachine(typeof(ProcessState), typeof(ProcessTrigger))]
    [GenerationMode(GenerationMode.Basic, Force = true)]
    public partial class GuardedStateMachine
    {
        public bool CanProcess { get; set; } = true;

        [Transition(ProcessState.Idle, ProcessTrigger.Start, ProcessState.Running,
            Guard = nameof(CheckCanProcess))]
        private void Configure() { }

        private bool CheckCanProcess() => CanProcess;
    }

    public enum WorkflowState { Draft, Submitted, Approved }
    public enum WorkflowTrigger { Submit, Approve }

    [StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), GenerateExtensibleVersion = true)]
    [GenerationMode(GenerationMode.WithExtensions, Force = true)]
    public partial class ExtensibleMachine
    {
        [Transition(WorkflowState.Draft, WorkflowTrigger.Submit, WorkflowState.Submitted)]
        private void Configure() { }
    }

    public class LoggingExtension : IStateMachineExtension
    {
        private readonly ITestOutputHelper _output;

        public LoggingExtension(ITestOutputHelper output)
        {
            _output = output;
        }

        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            _output.WriteLine($"Extension: Before transition at {context.Timestamp}");
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        {
            _output.WriteLine($"Extension: After transition, success={success}");
        }

        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
        {
            _output.WriteLine($"Extension: Evaluating guard '{guardName}'");
        }

        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
        {
            _output.WriteLine($"Extension: Guard '{guardName}' returned {result}");
        }
    }
}