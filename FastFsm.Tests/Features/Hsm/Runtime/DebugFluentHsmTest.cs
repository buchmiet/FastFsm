using Xunit;
using Xunit.Abstractions;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    public class DebugFluentHsmTest
    {
        private readonly ITestOutputHelper _output;

        public DebugFluentHsmTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Debug_SimpleParentChildMachine_Fluent_BasicInfo()
        {
            var m = new SimpleParentChildMachine_Fluent(HsmState_Fluent.Idle);
            
            _output.WriteLine($"Initial state: {m.CurrentState}");
            _output.WriteLine($"IsStarted: {m.IsStarted}");
            
            m.Start();
            _output.WriteLine($"After Start(): {m.CurrentState}");
            
            // Check if can fire Start
            var canFire = m.CanFire(HsmTrigger_Fluent.Start);
            _output.WriteLine($"CanFire(Start): {canFire}");
            
            // Get permitted triggers
            var permitted = m.GetPermittedTriggers();
            _output.WriteLine($"Permitted triggers: {string.Join(", ", permitted)}");
            
            // Check hierarchy info
            _output.WriteLine($"IsInHierarchy(Idle): {m.IsInHierarchy(HsmState_Fluent.Idle)}");
            _output.WriteLine($"IsInHierarchy(Working): {m.IsInHierarchy(HsmState_Fluent.Working)}");
            
            if (canFire)
            {
                m.Fire(HsmTrigger_Fluent.Start);
                _output.WriteLine($"After Fire(Start): {m.CurrentState}");
                _output.WriteLine($"IsInHierarchy(Working): {m.IsInHierarchy(HsmState_Fluent.Working)}");
                _output.WriteLine($"IsInHierarchy(Working_Initializing): {m.IsInHierarchy(HsmState_Fluent.Working_Initializing)}");
            }
            else
            {
                _output.WriteLine("ERROR: Cannot fire Start trigger!");
            }
        }
        
        [Fact]
        public void Debug_SimpleParentChildMachine_Fluent_v2_BasicInfo()
        {
            var m = new SimpleParentChildMachine_Fluent_v2(HsmState_Fluent_v2.Idle);
            
            _output.WriteLine($"V2 Initial state: {m.CurrentState}");
            _output.WriteLine($"V2 IsStarted: {m.IsStarted}");
            
            m.Start();
            _output.WriteLine($"V2 After Start(): {m.CurrentState}");
            
            // Check if can fire Start
            var canFire = m.CanFire(HsmTrigger_Fluent_v2.Start);
            _output.WriteLine($"V2 CanFire(Start): {canFire}");
            
            // Get permitted triggers
            var permitted = m.GetPermittedTriggers();
            _output.WriteLine($"V2 Permitted triggers: {string.Join(", ", permitted)}");
            
            if (canFire)
            {
                m.Fire(HsmTrigger_Fluent_v2.Start);
                _output.WriteLine($"V2 After Fire(Start): {m.CurrentState}");
            }
        }
    }
}