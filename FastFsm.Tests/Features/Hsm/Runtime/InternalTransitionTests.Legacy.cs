using Xunit;
using Abstractions.Attributes;
using System.Collections.Generic;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    public partial class InternalTransitionTestsLegacy
    {
        [Fact]
        public void Internal_OnParent_Executes_Action_Without_ExitOrEntryLegacy()
        {
            var m = new InternalMachineLegacy(InternalTransitionTestsFluent.S.Parent);
            m.Start(); // auto enters Child
            m.Log.Clear();

            m.Fire(InternalTransitionTestsFluent.T.Refresh);

            Assert.Equal(InternalTransitionTestsFluent.S.Child, m.CurrentState); // state unchanged
            Assert.Equal(new[] { "ParentInternal" }, m.Log);
        }

        [StateMachine(typeof(InternalTransitionTestsFluent.S), typeof(InternalTransitionTestsFluent.T), EnableHierarchy = true)]
        public partial class InternalMachineLegacy
        {
            public List<string> Log { get; } = new();

            // Parent state
            [State(InternalTransitionTestsFluent.S.Parent)]
            private void ConfigureParent() { }

            // Child state (initial)
            [State(InternalTransitionTestsFluent.S.Child, Parent = InternalTransitionTestsFluent.S.Parent, IsInitial = true)]
            private void ConfigureChild() { }

            // Internal transition on Parent
            [InternalTransition(InternalTransitionTestsFluent.S.Parent, InternalTransitionTestsFluent.T.Refresh, Action = nameof(ParentInternalAction))]
            private void ConfigureParentInternal() { }

            private void ParentInternalAction() => Log.Add("ParentInternal");
        }
    }
}