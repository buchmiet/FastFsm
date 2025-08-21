using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastFsm.Contracts;

/// <summary>
/// Asynchronous state machine interface for enum-based states and triggers
/// </summary>
public interface IStateMachineAsync<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// Current state of the machine
    /// </summary>
    TState CurrentState { get; }
    
    /// <summary>
    /// Indicates whether the state machine has been started
    /// </summary>
    bool IsStarted { get; }
    
    /// <summary>
    /// Asynchronously starts the state machine and executes initial OnEntry if present
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Asynchronously tries to fire a trigger. Returns true if a transition occurred.
    /// This operation is thread-safe.
    /// </summary>
    /// <param name="trigger">The trigger to fire.</param>
    /// <param name="payload">An optional payload for the transition.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, with a result of true if the transition was successful.</returns>
    ValueTask<bool> TryFireAsync(
        TTrigger trigger,
        object? payload = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously fires a trigger. Throws an <see cref="InvalidOperationException"/> if the transition is not valid.
    /// This operation is thread-safe.
    /// </summary>
    /// <param name="trigger">The trigger to fire.</param>
    /// <param name="payload">An optional payload for the transition.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask FireAsync(
        TTrigger trigger,
        object? payload = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if a trigger can be fired from the current state,
    /// including evaluation of asynchronous guards.
    /// </summary>
    /// <param name="trigger">The trigger to check.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, with a result of true if the trigger can be fired.</returns>
    ValueTask<bool> CanFireAsync(TTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets all triggers that can be fired from the current state,
    /// including evaluation of asynchronous guards.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, with a result of a read-only list of permitted triggers.</returns>
    ValueTask<IReadOnlyList<TTrigger>> GetPermittedTriggersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the given state is in the active path (HSM support)
    /// For non-hierarchical machines, returns true only if state equals CurrentState
    /// </summary>
    bool IsIn(TState state);
    
    /// <summary>
    /// Gets the active state path from root to the current leaf state (HSM support)
    /// For non-hierarchical machines, returns single-element list with CurrentState
    /// </summary>
    IReadOnlyList<TState> GetActivePath();
    
    /// <summary>
    /// Asynchronously gets the active state path from root to the current leaf state (HSM support)
    /// For non-hierarchical machines, returns single-element list with CurrentState
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, with a result of the active state path.</returns>
    ValueTask<IReadOnlyList<TState>> GetActivePathAsync(CancellationToken cancellationToken = default);
}