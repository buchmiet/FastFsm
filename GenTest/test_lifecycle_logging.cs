using Abstractions.Attributes;
using Microsoft.Extensions.Logging;

namespace TestMachines
{
    public enum State { Initial, Processing, Completed, Failed }
    public enum Trigger { Start, Process, Complete, Fail, Retry }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class LifecycleTestMachine
    {
        // State configurations with entry/exit actions
        [State(State.Initial, OnEntry = nameof(OnInitialEntry))]
        [State(State.Processing, OnEntry = nameof(OnProcessingEntry), OnExit = nameof(OnProcessingExit))]
        [State(State.Completed, OnEntry = nameof(OnCompletedEntry))]
        [State(State.Failed)]
        private void ConfigureStates() { }

        // Transitions with guards and actions
        [Transition(State.Initial, Trigger.Start, State.Processing, 
            Guard = nameof(CanStart), 
            Action = nameof(StartProcessing))]
        [Transition(State.Processing, Trigger.Complete, State.Completed,
            Action = nameof(CompleteProcessing))]
        [Transition(State.Processing, Trigger.Fail, State.Failed)]
        [Transition(State.Failed, Trigger.Retry, State.Initial)]
        private void ConfigureTransitions() { }

        // Guard method
        private bool CanStart() => true;

        // Action methods
        private void StartProcessing() 
        { 
            // Simulate some work
        }

        private void CompleteProcessing() 
        { 
            // Finalize processing
        }

        // Entry/Exit methods
        private void OnInitialEntry() 
        { 
            // Initialize state
        }

        private void OnProcessingEntry() 
        { 
            // Start processing
        }

        private void OnProcessingExit() 
        { 
            // Cleanup processing
        }

        private void OnCompletedEntry() 
        { 
            // Mark as completed
        }
    }
}