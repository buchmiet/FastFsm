using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastFsm.Tests.Machines.Legacy
{
    [StateMachine(typeof(UnreachableState), typeof(UnreachableTrigger))]
    public partial class UnreachableMachine
    {
        [State(UnreachableState.Start, IsInitial = true)]
        [State(UnreachableState.Connected)]
        [State(UnreachableState.Isolated)]

        [Transition(UnreachableState.Start, UnreachableTrigger.Connect, UnreachableState.Connected)]
        private void Configure()
        {
        }
    }
}
