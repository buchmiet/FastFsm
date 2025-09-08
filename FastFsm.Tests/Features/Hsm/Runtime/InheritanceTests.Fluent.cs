using Xunit;
using Dsl;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    public partial class InheritanceTestsFluent
    {
        [Fact]
        public void Child_Inherits_Parent_Transitions_And_PermittedTriggers_UnionsFluent()
        {
            var m = new InheritanceMachineFluent(S.Outside);
            m.Start();

            // Enter the composite parent
            m.Fire(T.Enter);
            Assert.Equal(S.Parent_A, m.CurrentState);

            var permitted = m.GetPermittedTriggers();
            Assert.Contains(T.Leave, permitted); // from parent
            Assert.Contains(T.Next, permitted);  // from child
            Assert.True(m.CanFire(T.Leave));

            m.Fire(T.Leave);
            Assert.Equal(S.Outside, m.CurrentState);
        }

        [Fact]
        public void IsInHierarchy_Reports_CorrectlyFluent()
        {
            var m = new InheritanceMachineFluent(S.Outside);
            m.Start();

            m.Fire(T.Enter); // now in Parent_A
            Assert.True(m.IsInHierarchy(S.Parent));

            m.Fire(T.Leave);
            Assert.False(m.IsInHierarchy(S.Parent));
        }

        public enum S { Outside, Parent, Parent_A, Parent_B }
        public enum T { Enter, Next, Leave }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InheritanceMachineFluent
        {
            public static void Configure()
            {
                // Parent with initial child and inherited transition
                FSM.State(S.Parent)
                   .Initial(S.Parent_A)
                   .On(T.Leave).GoTo(S.Outside);

                // Child states
                FSM.State(S.Parent_A)
                   .ChildOf(S.Parent)
                   .On(T.Next).GoTo(S.Parent_B);

                FSM.State(S.Parent_B)
                   .ChildOf(S.Parent);

                // Outside state
                FSM.State(S.Outside)
                   .On(T.Enter).GoTo(S.Parent);
            }
        }
    }
}