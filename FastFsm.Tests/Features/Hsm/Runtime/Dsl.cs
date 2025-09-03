// Simple DSL stub for testing Fluent HSM features
using System;

namespace Dsl
{
    public static class FSM
    {
        public static StateBuilder<TState> State<TState>(TState state) where TState : Enum
        {
            return new StateBuilder<TState>(state);
        }
    }

    public class StateBuilder<TState> where TState : Enum
    {
        private readonly TState _state;

        public StateBuilder(TState state)
        {
            _state = state;
        }

        // HSM-specific methods
        public StateBuilder<TState> ChildOf(TState parent) => this;
        public StateBuilder<TState> Initial(TState initialChild) => this;
        public StateBuilder<TState> HistoryShallow() => this;
        public StateBuilder<TState> HistoryDeep() => this;

        // Standard FSM methods
        public StateBuilder<TState> OnEntry(string callback) => this;
        public StateBuilder<TState> OnExit(string callback) => this;
        public TransitionBuilder<TState> On<TTrigger>(TTrigger trigger) where TTrigger : Enum
        {
            return new TransitionBuilder<TState>(this);
        }
        public StateBuilder<TState> OnInternal<TTrigger>(TTrigger trigger) where TTrigger : Enum
        {
            return this;
        }
        public StateBuilder<TState> Action(string callback) => this;
    }

    public class TransitionBuilder<TState> where TState : Enum
    {
        private readonly StateBuilder<TState> _stateBuilder;

        public TransitionBuilder(StateBuilder<TState> stateBuilder)
        {
            _stateBuilder = stateBuilder;
        }

        public StateBuilder<TState> GoTo(TState targetState) => _stateBuilder;
    }
}