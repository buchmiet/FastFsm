using System.Collections.Generic;
using Abstractions.Attributes;
using S = FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.S;
using T = FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.T;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    [StateMachine(typeof(SimpleParentChildMachineFluent.S), typeof(SimpleParentChildMachineFluent.T), EnableHierarchy = true)]
    public partial class SimpleParentChildMachineLegacy
    {
        // Simple states
        [State(S.Idle)]
        private void ConfigureIdle() { }

        // Parent state with children
        [State(S.Working, OnEntry = nameof(OnWorkingEntry), OnExit = nameof(OnWorkingExit))]
        private void ConfigureWorking() { }

        // Child states with proper hierarchy
        [State(S.Working_Initializing, Parent = S.Working, IsInitial = true, OnEntry = nameof(OnInitializingEntry), OnExit = nameof(OnInitializingExit))]
        private void ConfigureWorkingInitializing() { }

        [State(S.Working_Processing, Parent = S.Working, OnEntry = nameof(OnProcessingEntry))]
        private void ConfigureWorkingProcessing() { }

        [State(S.Working_Validating, Parent = S.Working)]
        private void ConfigureWorkingValidating() { }

        // Other states
        [State(S.Completed)]
        private void ConfigureCompleted() { }

        [State(S.Error)]
        private void ConfigureError() { }

        // Transitions
        [Transition(S.Idle, T.Start, S.Working)]
        private void ConfigureIdleToWorking() { }

        [Transition(S.Working_Initializing, T.Process, S.Working_Processing)]
        private void ConfigureInitializingToProcessing() { }

        [Transition(S.Working_Processing, T.Validate, S.Working_Validating)]
        private void ConfigureProcessingToValidating() { }

        [Transition(S.Working, T.Complete, S.Completed)]
        private void ConfigureWorkingToCompleted() { }

        [Transition(S.Working, T.Abort, S.Error)]
        private void ConfigureWorkingToError() { }

        // Entry/Exit callbacks
        public void OnWorkingEntry() => EntryExitLog.Add("Working:Entry");
        public void OnWorkingExit() => EntryExitLog.Add("Working:Exit");
        public void OnInitializingEntry() => EntryExitLog.Add("Initializing:Entry");
        public void OnInitializingExit() => EntryExitLog.Add("Initializing:Exit");
        public void OnProcessingEntry() => EntryExitLog.Add("Processing:Entry");

        // Track entry/exit calls for testing
        public List<string> EntryExitLog { get; } = new List<string>();
    }
}