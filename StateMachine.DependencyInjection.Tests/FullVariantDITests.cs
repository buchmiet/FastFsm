using Microsoft.Extensions.DependencyInjection;
using StateMachine.Contracts;
using StateMachine.Tests.DI.TestMachines;
using System.Collections.Generic;
using Xunit;


namespace StateMachine.Tests.DI;

/// <summary>
/// Tests for Full variant (Payload + Extensions)
/// </summary>
public class FullVariantDiTests : DITestBase
{
    [Fact]
    public void FullMachine_RegistersWithCorrectInterface()
    {
        // Arrange
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        // Act
        var machine = GetService<IFullTestMachine>();

        // Assert - Should implement extensible interface
        Assert.IsAssignableFrom<IExtensibleStateMachineSync<TestState, TestTrigger>>(machine);
    }

    [Fact]
    public void FullMachine_ExtensionsReceivePayload()
    {
        // Arrange
        var extension = new PayloadCapturingExtension();
        Services.AddSingleton<IStateMachineExtension>(extension);
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var testData = new TestData { Id = 123, Name = "Test" };

        // Act
        var machine = GetService<IFullTestMachine>();
        machine.TryFire(TestTrigger.Next, testData);

        // Assert
        Assert.NotNull(extension.LastPayload);
        Assert.Same(testData, extension.LastPayload);
        Assert.Equal(123, ((TestData)extension.LastPayload).Id);
    }

    [Fact]
    public void FullMachine_GuardAndActionReceivePayload_WhileExtensionsObserve()
    {
        // Arrange
        var extension = new DetailedExtension();
        Services.AddSingleton<IStateMachineExtension>(extension);
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var testData = new TestData { Id = 456, Name = "Important" };

        // Act

        var machine = GetService<IFullTestMachine>() as FullTestMachine; // Użyj var lub FullTestMachine
        Assert.NotNull(machine);
        var result = machine.TryFire(TestTrigger.Next, testData);

        Assert.True(result);
        Assert.Equal(TestState.B, machine.CurrentState);
        Assert.NotNull(machine.LastData);
        Assert.Equal(456, machine.LastData.Id);
        Assert.Equal(1, machine.ActionCount);

        // Assert - Extension observed everything
        Assert.Contains("BeforeTransition", extension.Events);
        Assert.Contains("GuardEvaluation:ValidateData", extension.Events);
        Assert.Contains("GuardEvaluated:ValidateData:True", extension.Events);
        Assert.Contains("AfterTransition:Success", extension.Events);
    }

    [Fact]
    public void FullMachine_MultipleExtensions_AllReceivePayload()
    {
        // Arrange
        var ext1 = new PayloadCapturingExtension();
        var ext2 = new PayloadCapturingExtension();
        var ext3 = new PayloadCapturingExtension();

        Services.AddSingleton<IStateMachineExtension>(ext1);
        Services.AddSingleton<IStateMachineExtension>(ext2);
        Services.AddSingleton<IStateMachineExtension>(ext3);
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var testData = new TestData { Id = 789 };

        // Act
        var machine = GetService<IFullTestMachine>();
        machine.Fire(TestTrigger.Next, testData);

        // Assert - All extensions received the payload
        Assert.Same(testData, ext1.LastPayload);
        Assert.Same(testData, ext2.LastPayload);
        Assert.Same(testData, ext3.LastPayload);
    }

    [Fact]
    public void FullMachine_NoExtensions_PayloadStillWorks()
    {
        // Arrange - No extensions
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var testData = new TestData { Id = 999, Name = "NoExt" };

        // Act
        var machine = GetService<IFullTestMachine>() as FullTestMachine; // Użyj var lub FullTestMachine
        Assert.NotNull(machine);
        var result = machine.TryFire(TestTrigger.Next, testData);

        // Assert
        Assert.True(result);
        Assert.Equal(TestState.B, machine.CurrentState);
        Assert.NotNull(machine.LastData);
        Assert.Equal(999, machine.LastData.Id);
    }

    [Fact]
    public void FullMachine_FailedGuard_ExtensionsStillNotified()
    {
        // Arrange
        var extension = new DetailedExtension();
        Services.AddSingleton<IStateMachineExtension>(extension);
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var invalidData = new TestData { Id = -1 }; // Will fail guard

        // Act
        var machine = GetService<IFullTestMachine>();
        var result = machine.TryFire(TestTrigger.Next, invalidData);

        // Assert - Transition failed
        Assert.False(result);
        Assert.Equal(TestState.A, machine.CurrentState);

        // Assert - Extension was notified
        Assert.Contains("BeforeTransition", extension.Events);
        Assert.Contains("GuardEvaluation:ValidateData", extension.Events);
        Assert.Contains("GuardEvaluated:ValidateData:False", extension.Events);
        Assert.Contains("AfterTransition:Failed", extension.Events);
    }

    // Test Extensions
    private class PayloadCapturingExtension : IStateMachineExtension
    {
        public object? LastPayload { get; private set; }

        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            if (context is IStateMachineContext<TestState, TestTrigger> typedContext)
            {
                LastPayload = typedContext.Payload;
            }
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
    }

    private class DetailedExtension : IStateMachineExtension
    {
        public List<string> Events { get; } = [];

        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            Events.Add("BeforeTransition");
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        {
            Events.Add(success ? "AfterTransition:Success" : "AfterTransition:Failed");
        }

        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
        {
            Events.Add($"GuardEvaluation:{guardName}");
        }

        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
        {
            Events.Add($"GuardEvaluated:{guardName}:{result}");
        }
    }
}