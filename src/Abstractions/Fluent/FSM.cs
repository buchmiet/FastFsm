using System;
using System.Threading;
using System.Threading.Tasks;

namespace Abstractions.Fluent
{
    #region Guard Delegates

    /// <summary>
    /// Synchronous guard without payload.
    /// </summary>
    public delegate bool Guard();

    /// <summary>
    /// Asynchronous guard without payload.
    /// </summary>
    public delegate ValueTask<bool> GuardAsync(CancellationToken ct);

    /// <summary>
    /// Synchronous guard with payload.
    /// </summary>
    public delegate bool Guard<TPayload>(in TPayload payload);

    /// <summary>
    /// Asynchronous guard with payload.
    /// </summary>
    public delegate ValueTask<bool> GuardAsync<TPayload>(in TPayload payload, CancellationToken ct);

    #endregion

    #region Action / Entry / Exit Delegates

    /// <summary>
    /// Synchronous action without payload.
    /// </summary>
    public delegate void Act();

    /// <summary>
    /// Asynchronous action without payload.
    /// </summary>
    public delegate ValueTask ActAsync(CancellationToken ct);

    /// <summary>
    /// Synchronous action with payload.
    /// </summary>
    public delegate void Act<TPayload>(in TPayload payload);

    /// <summary>
    /// Asynchronous action with payload.
    /// </summary>
    public delegate ValueTask ActAsync<TPayload>(in TPayload payload, CancellationToken ct);

    /// <summary>
    /// Entry callback without payload.
    /// </summary>
    public delegate void Entry();

    /// <summary>
    /// Asynchronous entry callback without payload.
    /// </summary>
    public delegate ValueTask EntryAsync(CancellationToken ct);

    /// <summary>
    /// Entry callback with payload.
    /// </summary>
    public delegate void Entry<TPayload>(in TPayload payload);

    /// <summary>
    /// Asynchronous entry callback with payload.
    /// </summary>
    public delegate ValueTask EntryAsync<TPayload>(in TPayload payload, CancellationToken ct);

    /// <summary>
    /// Exit callback without payload.
    /// </summary>
    public delegate void Exit();

    /// <summary>
    /// Asynchronous exit callback without payload.
    /// </summary>
    public delegate ValueTask ExitAsync(CancellationToken ct);

    /// <summary>
    /// Exit callback with payload.
    /// </summary>
    public delegate void Exit<TPayload>(in TPayload payload);

    /// <summary>
    /// Asynchronous exit callback with payload.
    /// </summary>
    public delegate ValueTask ExitAsync<TPayload>(in TPayload payload, CancellationToken ct);

    #endregion

    /// <summary>
    /// Entry point for Fluent API state machine configuration.
    /// This is a compile-time only API - all methods are no-op at runtime.
    /// </summary>
    public static class FSM
    {
        /// <summary>
        /// Enable extensions support for the state machine.
        /// Equivalent to GenerateExtensibleVersion = true in attribute API.
        /// </summary>
        public static StateBuilder<TState> Extensible<TState>() where TState : Enum
        {
            // Runtime no-op - only used at compile time by source generator
            return new StateBuilder<TState>();
        }

        /// <summary>
        /// Set the exception handler for the state machine.
        /// The handler method must accept ExceptionContext and return ExceptionDirective.
        /// Equivalent to [OnException] attribute in attribute API.
        /// </summary>
        public static StateBuilder<TState> OnException<TState>(string methodName) where TState : Enum
        {
            // Runtime no-op - only used at compile time by source generator
            return new StateBuilder<TState>();
        }

        /// <summary>
        /// Set the exception handler using a method group.
        /// </summary>
        public static StateBuilder<TState> OnException<TState>(Delegate handler) where TState : Enum
        {
            return new StateBuilder<TState>();
        }

        /// <summary>
        /// Define a state in the state machine.
        /// </summary>
        public static StateBuilder<TState> State<TState>(TState state) where TState : Enum
        {
            // Runtime no-op - only used at compile time by source generator
            return new StateBuilder<TState>();
        }

        /// <summary>
        /// Alias for State - set state context.
        /// </summary>
        public static StateBuilder<TState> At<TState>(TState state) where TState : Enum
        {
            return new StateBuilder<TState>();
        }
    }

    /// <summary>
    /// Builder for configuring a state.
    /// </summary>
    public sealed class StateBuilder<TState> where TState : Enum
    {
        /// <summary>
        /// Set the exception handler for the state machine.
        /// The handler method must accept ExceptionContext and return ExceptionDirective.
        /// Equivalent to [OnException] attribute in attribute API.
        /// </summary>
        public StateBuilder<TState> OnException(string methodName) => this;

        /// <summary>
        /// Set the exception handler for the state machine using a method group.
        /// </summary>
        public StateBuilder<TState> OnException(Delegate handler) => this;

        /// <summary>
        /// Set the parent state (for hierarchical state machines).
        /// </summary>
        public StateBuilder<TState> Parent(TState parentState) => this;

        /// <summary>
        /// Mark this state as the initial state of its parent.
        /// </summary>
        public StateBuilder<TState> IsInitial() => this;

        /// <summary>
        /// Configure history mode for this composite state.
        /// </summary>
        public StateBuilder<TState> WithHistory(HistoryMode mode) => this;

        /// <summary>
        /// Set the synchronous entry action for this state.
        /// </summary>
        public StateBuilder<TState> OnEntry(string methodName) => this;
        public StateBuilder<TState> OnEntry(Entry callback) => this;
        public StateBuilder<TState> OnEntry(EntryAsync callback) => this;
        public StateBuilder<TState> OnEntry<TPayload>(Entry<TPayload> callback) => this;
        public StateBuilder<TState> OnEntry<TPayload>(EntryAsync<TPayload> callback) => this;

        /// <summary>
        /// Set the asynchronous entry action for this state.
        /// </summary>
        public StateBuilder<TState> OnEntryAsync(string methodName) => this;
        public StateBuilder<TState> OnEntryAsync(EntryAsync callback) => this;
        public StateBuilder<TState> OnEntryAsync<TPayload>(EntryAsync<TPayload> callback) => this;

        /// <summary>
        /// Set the synchronous exit action for this state.
        /// </summary>
        public StateBuilder<TState> OnExit(string methodName) => this;
        public StateBuilder<TState> OnExit(Exit callback) => this;
        public StateBuilder<TState> OnExit(ExitAsync callback) => this;
        public StateBuilder<TState> OnExit<TPayload>(Exit<TPayload> callback) => this;
        public StateBuilder<TState> OnExit<TPayload>(ExitAsync<TPayload> callback) => this;

        /// <summary>
        /// Set the asynchronous exit action for this state.
        /// </summary>
        public StateBuilder<TState> OnExitAsync(string methodName) => this;
        public StateBuilder<TState> OnExitAsync(ExitAsync callback) => this;
        public StateBuilder<TState> OnExitAsync<TPayload>(ExitAsync<TPayload> callback) => this;

        /// <summary>
        /// Define a transition from this state.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> On<TTrigger>(TTrigger trigger) where TTrigger : Enum
        {
            return new TransitionBuilder<TState, TTrigger>();
        }

        /// <summary>
        /// Define an internal transition (no state change).
        /// </summary>
        public TransitionBuilder<TState, TTrigger> OnInternal<TTrigger>(TTrigger trigger) where TTrigger : Enum
        {
            return new TransitionBuilder<TState, TTrigger>(isInternal: true);
        }

        /// <summary>
        /// Continue to define another state.
        /// </summary>
        public StateBuilder<TState> State(TState state)
        {
            return new StateBuilder<TState>();
        }
    }

    /// <summary>
    /// Builder for configuring a transition.
    /// </summary>
    public sealed class TransitionBuilder<TState, TTrigger>
        where TState : Enum
        where TTrigger : Enum
    {
        private readonly bool _isInternal;

        public TransitionBuilder(bool isInternal = false)
        {
            _isInternal = isInternal;
        }

        /// <summary>
        /// Set the payload type for this transition.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Payload(Type payloadType) => this;

        /// <summary>
        /// Set the payload type for this transition (generic).
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Payload<TPayload>() => Payload(typeof(TPayload));

        /// <summary>
        /// Set the synchronous guard for this transition.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Guard(string methodName) => this;

        /// <summary>
        /// Set the asynchronous guard for this transition.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> GuardAsync(string methodName) => this;

        #region Guard Method Group Overloads

        /// <summary>
        /// Set the synchronous guard for this transition (no payload).
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Guard(Guard guard) => this;

        /// <summary>
        /// Set the asynchronous guard for this transition (no payload).
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Guard(GuardAsync guard) => this;

        /// <summary>
        /// Set the synchronous guard for this transition (with payload).
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Guard<TPayload>(Guard<TPayload> guard) => this;

        /// <summary>
        /// Set the asynchronous guard for this transition (with payload).
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Guard<TPayload>(GuardAsync<TPayload> guard) => this;

        #endregion

        /// <summary>
        /// Set the synchronous action for this transition.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Action(string methodName) => this;
        public TransitionBuilder<TState, TTrigger> Action(Act callback) => this;
        public TransitionBuilder<TState, TTrigger> Action(ActAsync callback) => this;
        public TransitionBuilder<TState, TTrigger> Action<TPayload>(Act<TPayload> callback) => this;
        public TransitionBuilder<TState, TTrigger> Action<TPayload>(ActAsync<TPayload> callback) => this;

        /// <summary>
        /// Set the asynchronous action for this transition.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> ActionAsync(string methodName) => this;
        public TransitionBuilder<TState, TTrigger> ActionAsync(ActAsync callback) => this;
        public TransitionBuilder<TState, TTrigger> ActionAsync<TPayload>(ActAsync<TPayload> callback) => this;

        /// <summary>
        /// Set the priority for this transition (HSM).
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Priority(int priority) => this;

        /// <summary>
        /// Set the target state for this transition and return to state builder.
        /// </summary>
        public StateBuilder<TState> GoTo(TState targetState)
        {
            // Finalize transition with target state
            return new StateBuilder<TState>();
        }

        /// <summary>
        /// Finalize as internal transition and return to state builder.
        /// </summary>
        public StateBuilder<TState> Internal()
        {
            // Finalize as internal transition
            return new StateBuilder<TState>();
        }

        /// <summary>
        /// Continue to define another transition from the same state.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> On(TTrigger trigger)
        {
            // Auto-finalize previous transition as internal (parser will handle warning)
            return new TransitionBuilder<TState, TTrigger>();
        }

        /// <summary>
        /// Continue to define an internal transition from the same state.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> OnInternal(TTrigger trigger)
        {
            // Auto-finalize previous transition as internal (parser will handle warning)
            return new TransitionBuilder<TState, TTrigger>(isInternal: true);
        }

        /// <summary>
        /// Continue to define another state.
        /// </summary>
        public StateBuilder<TState> State(TState state)
        {
            // Auto-finalize previous transition as internal if not finalized
            return new StateBuilder<TState>();
        }
    }

    /// <summary>
    /// History modes for hierarchical state machines.
    /// </summary>
    public enum HistoryMode
    {
        None,
        Shallow,
        Deep
    }

    /// <summary>
    /// Async policy for state machine configuration.
    /// </summary>
    public enum AsyncPolicy
    {
        /// <summary>
        /// Parser infers async from signatures. FireAsync recommended for async paths.
        /// </summary>
        Implicit,

        /// <summary>
        /// Async methods must use *Async suffix. FireAsync required for async paths.
        /// </summary>
        Explicit,

        /// <summary>
        /// All handlers must be async. Only FireAsync available.
        /// </summary>
        Required
    }
}
