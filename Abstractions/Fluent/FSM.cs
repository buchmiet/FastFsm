namespace Abstractions.Fluent
{
    /// <summary>
    /// Entry point for Fluent API state machine configuration.
    /// This is a compile-time only API - all methods are no-op at runtime.
    /// </summary>
    public static class FSM
    {
        /// <summary>
        /// Define a state in the state machine.
        /// </summary>
        public static StateBuilder<TState> State<TState>(TState state) where TState : System.Enum
        {
            // Runtime no-op - only used at compile time by source generator
            return new StateBuilder<TState>();
        }
    }

    /// <summary>
    /// Builder for configuring a state.
    /// </summary>
    public class StateBuilder<TState> where TState : System.Enum
    {
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
        /// Set the entry action for this state.
        /// </summary>
        public StateBuilder<TState> OnEntry(string methodName) => this;

        /// <summary>
        /// Set the exit action for this state.
        /// </summary>
        public StateBuilder<TState> OnExit(string methodName) => this;

        /// <summary>
        /// Define a transition from this state.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> On<TTrigger>(TTrigger trigger) where TTrigger : System.Enum
        {
            return new TransitionBuilder<TState, TTrigger>();
        }

        /// <summary>
        /// Define an internal transition (no state change).
        /// </summary>
        public InternalTransitionBuilder<TTrigger> OnInternal<TTrigger>(TTrigger trigger) where TTrigger : System.Enum
        {
            return new InternalTransitionBuilder<TTrigger>();
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
    public class TransitionBuilder<TState, TTrigger> 
        where TState : System.Enum
        where TTrigger : System.Enum
    {
        /// <summary>
        /// Set the target state for this transition.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> GoTo(TState targetState) => this;

        /// <summary>
        /// Set the guard for this transition.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Guard(string methodName) => this;

        /// <summary>
        /// Set the action for this transition.
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Action(string methodName) => this;

        /// <summary>
        /// Set the priority for this transition (HSM).
        /// </summary>
        public TransitionBuilder<TState, TTrigger> Priority(int priority) => this;

        /// <summary>
        /// Continue to define another state.
        /// </summary>
        public StateBuilder<TState> State(TState state)
        {
            return new StateBuilder<TState>();
        }
    }

    /// <summary>
    /// Builder for configuring an internal transition.
    /// </summary>
    public class InternalTransitionBuilder<TTrigger> where TTrigger : System.Enum
    {
        /// <summary>
        /// Set the action for this internal transition.
        /// </summary>
        public InternalTransitionBuilder<TTrigger> Action(string methodName) => this;

        /// <summary>
        /// Set the guard for this internal transition.
        /// </summary>
        public InternalTransitionBuilder<TTrigger> Guard(string methodName) => this;
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
}