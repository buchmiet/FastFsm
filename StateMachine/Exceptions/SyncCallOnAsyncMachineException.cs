using System;

namespace StateMachine.Exceptions;

/// <summary>
/// The exception that is thrown when a synchronous method is called on a state machine
/// that was generated for asynchronous operations.
/// </summary>
public class SyncCallOnAsyncMachineException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncCallOnAsyncMachineException"/> class.
    /// </summary>
    public SyncCallOnAsyncMachineException()
        : base("Cannot call synchronous methods on an asynchronous state machine. Use the async variants instead.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncCallOnAsyncMachineException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SyncCallOnAsyncMachineException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncCallOnAsyncMachineException"/> class
    /// with a specified error message and a reference to the inner exception that is
    /// the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SyncCallOnAsyncMachineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}