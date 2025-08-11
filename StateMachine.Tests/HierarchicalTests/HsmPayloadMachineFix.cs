using System;
using StateMachine.Tests.HierarchicalTests;

namespace StateMachine.Tests.HierarchicalTests
{
    public partial class HsmAdditionalCompilationTests
    {
        // Temporary fix for missing HSM metadata initialization in generated code
        public partial class HsmPayloadMachine
        {
            private const int NO_PARENT = -1;
            private const int NO_CHILD = -1;

            private static readonly int[] s_parent;
            private static readonly int[] s_depth;
            private static readonly int[] s_initialChild;

            static HsmPayloadMachine()
            {
                // Enum order for HP_State: Root=0, ChildA=1, ChildB=2
                s_parent = new[] { NO_PARENT, 0, 0 };
                s_initialChild = new[] { 1, NO_CHILD, NO_CHILD }; // Root → ChildA
                s_depth = new[] { 0, 1, 1 };
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            private static HP_State DescendToInitial(HP_State s)
            {
                int idx = (int)s;
                int child = s_initialChild[idx];
                while (child != NO_CHILD)
                {
                    idx = child;
                    child = s_initialChild[idx];
                }
                return (HP_State)idx;
            }

            public override void Start()
            {
                base.Start(); // preserve base semantics
                _currentState = DescendToInitial(_currentState);
            }
        }
    }
}