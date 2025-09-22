using System;
using Abstractions.Fluent;

namespace Dsl
{
    // Stubbed DSL for Fluent API testing
    public static class FSM
    {
        public static FsmDsl State(object _) => new FsmDsl();
        public static FsmDsl At(object _) => new FsmDsl();
        public static FsmDsl OnException(string _) => new FsmDsl();
        public static FsmDsl OnException(Delegate _) => new FsmDsl();
    }

    public sealed class FsmDsl
    {
        // HSM methods
        public FsmDsl ChildOf(object _) => this;
        public FsmDsl Initial(object _) => this;
        public FsmDsl HistoryShallow() => this;
        public FsmDsl HistoryDeep() => this;
        
        // States and transitions
        public FsmDsl State(object _) => this;
        public FsmDsl At(object _) => this;  // Alias for State
        public FsmDsl On(object _) => this;
        public FsmDsl OnInternal(object _) => this;
        public FsmDsl GoTo(object _) => this;
        public FsmDsl Internal() => this;
        
        // Entry/Exit callbacks
        public FsmDsl OnEntry(string _) => this;
        public FsmDsl OnEntry(Entry _) => this;
        public FsmDsl OnEntry(EntryAsync _) => this;
        public FsmDsl OnEntry<T>(Entry<T> _) => this;
        public FsmDsl OnEntry<T>(EntryAsync<T> _) => this;
        public FsmDsl OnEntryAsync(string _) => this;
        public FsmDsl OnEntryAsync(EntryAsync _) => this;
        public FsmDsl OnEntryAsync<T>(EntryAsync<T> _) => this;
        public FsmDsl OnExit(string _) => this;
        public FsmDsl OnExit(Exit _) => this;
        public FsmDsl OnExit(ExitAsync _) => this;
        public FsmDsl OnExit<T>(Exit<T> _) => this;
        public FsmDsl OnExit<T>(ExitAsync<T> _) => this;
        public FsmDsl OnExitAsync(string _) => this;
        public FsmDsl OnExitAsync(ExitAsync _) => this;
        public FsmDsl OnExitAsync<T>(ExitAsync<T> _) => this;
        
        // Guards and Actions
        public FsmDsl Guard(string _) => this;
        public FsmDsl Guard(Guard _) => this;
        public FsmDsl Guard(GuardAsync _) => this;
        public FsmDsl Guard<T>(Guard<T> _) => this;
        public FsmDsl Guard<T>(GuardAsync<T> _) => this;
        public FsmDsl GuardAsync(string _) => this;
        public FsmDsl Action(string _) => this;
        public FsmDsl Action(Act _) => this;
        public FsmDsl Action(ActAsync _) => this;
        public FsmDsl Action<T>(Act<T> _) => this;
        public FsmDsl Action<T>(ActAsync<T> _) => this;
        public FsmDsl ActionAsync(string _) => this;
        public FsmDsl ActionAsync(ActAsync _) => this;
        public FsmDsl ActionAsync<T>(ActAsync<T> _) => this;
        public FsmDsl Payload(System.Type _) => this;
        public FsmDsl Payload<T>() => this; // Generic version
        public FsmDsl Priority(int _) => this;
        
        // Overloads for optional parameters
        public FsmDsl Guard() => this;
        public FsmDsl Action() => this;
        public FsmDsl Payload() => this;
    }
}
