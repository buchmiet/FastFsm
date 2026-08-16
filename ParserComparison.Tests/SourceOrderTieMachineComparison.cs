using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

public partial class SourceOrderTieMachineComparison
{
    public enum S { Parent, Child, ParentDone, A, B, C }
    public enum T { Go }

    // Legacy version - using attributes
    [StateMachine(typeof(S), typeof(T))]
    public partial class SourceOrderTieMachine_Legacy
    {
        public List<string> Log { get; } = new();

        [State(S.A, OnEntry = nameof(OnAEntryAsync))]
        [State(S.B)]
        [State(S.C)]
        private void ConfigureStates() { }

        [Transition(S.A, T.Go, S.B, Action = nameof(First), Priority = 0)]
        [Transition(S.A, T.Go, S.C, Action = nameof(Second), Priority = 0)]
        private void ConfigureTransitions() { }

        private async Task First() { await Task.Yield(); Log.Add("First"); }
        private async Task Second() { await Task.Yield(); Log.Add("Second"); }
        private async Task OnAEntryAsync() => await Task.CompletedTask;
    }

    // Fluent version
    [StateMachine(typeof(S), typeof(T))]
    public partial class SourceOrderTieMachine_Fluent
    {
        public List<string> Log { get; } = new();
        
        private static void Configure()
        {
            FSM.State(S.A)
                .OnEntryAsync(nameof(OnAEntryAsync));
            FSM.State(S.B);
            FSM.State(S.C);
            
            // Two transitions with same priority - first wins
            FSM.At(S.A)
                .On(T.Go)
                .ActionAsync(nameof(First))
                .Priority(0)
                .GoTo(S.B);
                
            FSM.At(S.A)
                .On(T.Go)
                .ActionAsync(nameof(Second))
                .Priority(0)
                .GoTo(S.C);
        }

        private async Task OnAEntryAsync() => await Task.CompletedTask;
        private async Task First() { Log.Add("First"); await Task.Yield(); }
        private async Task Second() { Log.Add("Second"); await Task.Yield(); }
    }
}