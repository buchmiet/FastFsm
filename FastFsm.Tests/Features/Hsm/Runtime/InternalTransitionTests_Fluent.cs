using Xunit;
using Dsl;
using System.Collections.Generic;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    public partial class InternalTransitionTests_Fluent
    {
        [Fact]
        public void Internal_OnParent_Executes_Action_Without_ExitOrEntry_Fluent()
        {
            var m = new InternalMachine_Fluent(S.Parent);
            m.Start(); // auto enters Child
            m.Log.Clear();

            m.Fire(T.Refresh);

            Assert.Equal(S.Child, m.CurrentState); // state unchanged
            Assert.Equal(new[] { "ParentInternal" }, m.Log);
        }

        public enum S { Parent, Child }
        public enum T { Refresh }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InternalMachine_Fluent
        {
            public List<string> Log { get; } = new();

            public static void Configure()
            {
                // Parent with internal transition
                FSM.State(S.Parent)
                   .Initial(S.Child)
                   .OnInternal(T.Refresh).Action(nameof(ParentInternalAction));

                // Child state
                FSM.State(S.Child)
                   .ChildOf(S.Parent);
            }

            private void ParentInternalAction() => Log.Add("ParentInternal");
        }
    }
}