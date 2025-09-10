using FastFsm.Tests.TestHelpers;
using Xunit;
using static FastFsm.Tests.TestHelpers.StateMachineWrapperFactory;

namespace FastFsm.Tests.Features.Core
{
    /// <summary>
    /// Refactored version of GuardPermittedTriggersTests using the new wrapper infrastructure
    /// </summary>
    public class GuardPermittedTriggersTestsRefactored
    {
        private const string MachineType = "GuardPermitted";
        
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void PermittedTriggers_ReflectCurrentGuardState(ApiType apiType)
        {
            // Arrange
            var wrapper = StateMachineWrapperFactory.Create(MachineType, apiType, "Idle");
            
            // Set the Allow property based on wrapper type
            if (wrapper is GuardPermittedFluentWrapper fluentWrapper)
            {
                fluentWrapper.Allow = false;
            }
            else if (wrapper is GuardPermittedLegacyWrapper legacyWrapper)
            {
                legacyWrapper.Allow = false;
            }
            
            wrapper.Start();
            
            var triggerRun = GetTriggerEnum(MachineType, apiType, "Run");
            
            // Act & Assert - guard initially false
            var permittedTriggers = wrapper.GetPermittedTriggers();
            Assert.DoesNotContain(triggerRun, permittedTriggers);
            
            // Set guard to true
            if (wrapper is GuardPermittedFluentWrapper fluentWrapper2)
            {
                fluentWrapper2.Allow = true;
            }
            else if (wrapper is GuardPermittedLegacyWrapper legacyWrapper2)
            {
                legacyWrapper2.Allow = true;
            }
            
            // Act & Assert - guard now true
            permittedTriggers = wrapper.GetPermittedTriggers();
            Assert.Contains(triggerRun, permittedTriggers);
        }
        
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void CanFire_RespectsGuardCondition(ApiType apiType)
        {
            // Arrange
            var wrapper = StateMachineWrapperFactory.Create(MachineType, apiType, "Idle");
            
            // Set the Allow property to false
            if (wrapper is GuardPermittedFluentWrapper fluentWrapper)
            {
                fluentWrapper.Allow = false;
            }
            else if (wrapper is GuardPermittedLegacyWrapper legacyWrapper)
            {
                legacyWrapper.Allow = false;
            }
            
            wrapper.Start();
            
            var triggerRun = GetTriggerEnum(MachineType, apiType, "Run");
            
            // Act & Assert - guard false
            Assert.False(wrapper.CanFire(triggerRun));
            
            // Set guard to true
            if (wrapper is GuardPermittedFluentWrapper fluentWrapper2)
            {
                fluentWrapper2.Allow = true;
            }
            else if (wrapper is GuardPermittedLegacyWrapper legacyWrapper2)
            {
                legacyWrapper2.Allow = true;
            }
            
            // Act & Assert - guard true
            Assert.True(wrapper.CanFire(triggerRun));
        }
        
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void TryFire_RespectsGuardCondition(ApiType apiType)
        {
            // Arrange
            var wrapper = StateMachineWrapperFactory.Create(MachineType, apiType, "Idle");
            
            // Set the Allow property to false
            if (wrapper is GuardPermittedFluentWrapper fluentWrapper)
            {
                fluentWrapper.Allow = false;
            }
            else if (wrapper is GuardPermittedLegacyWrapper legacyWrapper)
            {
                legacyWrapper.Allow = false;
            }
            
            wrapper.Start();
            
            var stateIdle = GetStateEnum(MachineType, apiType, "Idle");
            var stateDone = GetStateEnum(MachineType, apiType, "Done");
            var triggerRun = GetTriggerEnum(MachineType, apiType, "Run");
            
            // Act & Assert - guard false, transition should fail
            var result = wrapper.TryFire(triggerRun);
            Assert.False(result);
            Assert.Equal(stateIdle, wrapper.CurrentState);
            
            // Set guard to true
            if (wrapper is GuardPermittedFluentWrapper fluentWrapper2)
            {
                fluentWrapper2.Allow = true;
            }
            else if (wrapper is GuardPermittedLegacyWrapper legacyWrapper2)
            {
                legacyWrapper2.Allow = true;
            }
            
            // Act & Assert - guard true, transition should succeed
            result = wrapper.TryFire(triggerRun);
            Assert.True(result);
            Assert.Equal(stateDone, wrapper.CurrentState);
        }
    }
}