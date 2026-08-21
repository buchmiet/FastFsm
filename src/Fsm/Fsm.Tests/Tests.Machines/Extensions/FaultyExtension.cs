using FastFsm.Contracts;
using Tests.Machines.Machines;

namespace Tests.Machines.Extensions
{

        public class FaultyExtension : IStateMachineExtension<ExtState, ExtTrigger>
        {
            public void OnAttemptStarting(in TransitionAttemptContext<ExtState, ExtTrigger> attempt)
            {
                throw new Exception("Extension error");
            }
        }
    }




