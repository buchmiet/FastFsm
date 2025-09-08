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
        public void Debug_SimpleParentChildMachineFluent_BasicInfo()
        {
            var m = new SimpleParentChildMachineFluent(HsmStateFluent.Idle);
            
            _output.WriteLine($"Initial state: {m.CurrentState}");
            _output.WriteLine($"IsStarted: {m.IsStarted}");
            
            m.Start();
            _output.WriteLine($"After Start(): {m.CurrentState}");
            
            // Check if can fire Start
            var canFire = m.CanFire(HsmTriggerFluent.Start);
            _output.WriteLine($"CanFire(Start): {canFire}");
            
            // Get permitted triggers
            var permitted = m.GetPermittedTriggers();
            _output.WriteLine($"Permitted triggers: {string.Join(", ", permitted)}");
            
            // Check hierarchy info
            _output.WriteLine($"IsInHierarchy(Idle): {m.IsInHierarchy(HsmStateFluent.Idle)}");
            _output.WriteLine($"IsInHierarchy(Working): {m.IsInHierarchy(HsmStateFluent.Working)}");
            
            if (canFire)
            {
                m.Fire(HsmTriggerFluent.Start);
                _output.WriteLine($"After Fire(Start): {m.CurrentState}");
                _output.WriteLine($"CurrentState int value: {(int)m.CurrentState}");
                _output.WriteLine($"Expected Working_Initializing int: {(int)HsmStateFluent.Working_Initializing}");
                _output.WriteLine($"IsInHierarchy(Working): {m.IsInHierarchy(HsmStateFluent.Working)}");
                _output.WriteLine($"IsInHierarchy(Working_Initializing): {m.IsInHierarchy(HsmStateFluent.Working_Initializing)}");
                _output.WriteLine($"IsInHierarchy(Completed): {m.IsInHierarchy(HsmStateFluent.Completed)}");
            }
            else
            {
                _output.WriteLine("ERROR: Cannot fire Start trigger!");
            }
        }
        
        [Fact]
        public void Debug_SimpleParentChildMachineFluent_v2_BasicInfo()
        {
            var m = new SimpleParentChildMachineFluent_v2(HsmStateFluent_v2.Idle);
            
            _output.WriteLine($"V2 Initial state: {m.CurrentState}");
            _output.WriteLine($"V2 IsStarted: {m.IsStarted}");
            
            m.Start();
            _output.WriteLine($"V2 After Start(): {m.CurrentState}");
            
            // Check if can fire Start
            var canFire = m.CanFire(HsmTriggerFluent_v2.Start);
            _output.WriteLine($"V2 CanFire(Start): {canFire}");
            
            // Get permitted triggers
            var permitted = m.GetPermittedTriggers();
            _output.WriteLine($"V2 Permitted triggers: {string.Join(", ", permitted)}");
            
            if (canFire)
            {
                m.Fire(HsmTriggerFluent_v2.Start);
                _output.WriteLine($"V2 After Fire(Start): {m.CurrentState}");
            }
        }
    }
}