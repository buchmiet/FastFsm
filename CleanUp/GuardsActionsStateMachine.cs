using Abstractions.Attributes;

namespace CleanUp;

// State machine with guards and actions
public enum ProcessState
{
    Idle,
    Validating,
    Processing,
    Success,
    Error
}

public enum ProcessTrigger
{
    Start,
    Process,
    Complete,
    Fail,
    Retry,
    Reset
}

[StateMachine(typeof(ProcessState), typeof(ProcessTrigger))]
[PayloadType(typeof(ProcessData))]
public partial class GuardsActionsStateMachine
{
    private int _retryCount = 0;
    private const int MaxRetries = 3;
    private string? _lastError;
    
    // State callbacks
    [State(ProcessState.Idle, OnEntry = nameof(OnEnterIdle), OnExit = nameof(OnExitIdle))]
    [State(ProcessState.Processing, OnEntry = nameof(OnEnterProcessing))]
    [State(ProcessState.Error, OnEntry = nameof(OnEnterError))]
    private void ConfigureStates() { }
    
    // Transitions with guards and actions
    [Transition(ProcessState.Idle, ProcessTrigger.Start, ProcessState.Validating,
        Guard = nameof(CanStart),
        Action = nameof(PrepareForValidation))]
    
    [Transition(ProcessState.Validating, ProcessTrigger.Process, ProcessState.Processing,
        Guard = nameof(IsValidData),
        Action = nameof(StartProcessing))]
    
    [Transition(ProcessState.Validating, ProcessTrigger.Fail, ProcessState.Error,
        Action = nameof(LogValidationError))]
    
    [Transition(ProcessState.Processing, ProcessTrigger.Complete, ProcessState.Success,
        Action = nameof(CompleteProcessing))]
    
    [Transition(ProcessState.Processing, ProcessTrigger.Fail, ProcessState.Error,
        Action = nameof(HandleProcessingError))]
    
    [Transition(ProcessState.Error, ProcessTrigger.Retry, ProcessState.Validating,
        Guard = nameof(CanRetry),
        Action = nameof(IncrementRetryCount))]
    
    [Transition(ProcessState.Error, ProcessTrigger.Reset, ProcessState.Idle,
        Action = nameof(ResetState))]
    
    [Transition(ProcessState.Success, ProcessTrigger.Reset, ProcessState.Idle,
        Action = nameof(ResetState))]
    private void ConfigureTransitions() { }
    
    // Guards
    private bool CanStart(ProcessData data) => data != null && !string.IsNullOrEmpty(data.Id);
    private bool IsValidData(ProcessData data) => data.IsValid && data.Value > 0;
    private bool CanRetry() => _retryCount < MaxRetries;
    
    // Actions
    private void PrepareForValidation(ProcessData data)
    {
        Console.WriteLine($"Preparing to validate data with ID: {data.Id}");
        _lastError = null;
    }
    
    private void StartProcessing(ProcessData data)
    {
        Console.WriteLine($"Starting processing for: {data.Id}");
    }
    
    private void LogValidationError(ProcessData data)
    {
        _lastError = $"Validation failed for data: {data.Id}";
        Console.WriteLine(_lastError);
    }
    
    private void CompleteProcessing(ProcessData data)
    {
        Console.WriteLine($"Processing completed successfully for: {data.Id}");
    }
    
    private void HandleProcessingError(ProcessData data)
    {
        _lastError = $"Processing failed for data: {data.Id}";
        Console.WriteLine(_lastError);
    }
    
    private void IncrementRetryCount()
    {
        _retryCount++;
        Console.WriteLine($"Retry attempt {_retryCount} of {MaxRetries}");
    }
    
    private void ResetState()
    {
        _retryCount = 0;
        _lastError = null;
        Console.WriteLine("State machine reset");
    }
    
    // State callbacks
    private void OnEnterIdle()
    {
        Console.WriteLine("Entering Idle state");
    }
    
    private void OnExitIdle()
    {
        Console.WriteLine("Exiting Idle state");
    }
    
    private void OnEnterProcessing()
    {
        Console.WriteLine("Entering Processing state");
    }
    
    private void OnEnterError()
    {
        Console.WriteLine($"Entering Error state. Last error: {_lastError}");
    }
}

// Payload data class
public class ProcessData
{
    public string Id { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool IsValid { get; set; }
}