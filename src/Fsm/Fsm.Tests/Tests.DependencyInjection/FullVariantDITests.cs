using Microsoft.Extensions.DependencyInjection;
using FastFsm.Contracts;
using Tests.DependencyInjection.TestMachines;
using Tests.DependencyInjection;
using System.Collections.Generic;
using Xunit;
using FastFsm.DependencyInjection;


namespace Tests.DependencyInjection;

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
        machine.Start();

        // Assert - Should implement extensible interface
        Assert.IsAssignableFrom<IExtensibleStateMachineSync<TestState, TestTrigger>>(machine);
    }

    [Fact]
    public void FullMachine_ExtensionsReceivePayload()
    {
        // Arrange
        var extension = new PayloadCapturingExtension();
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension);
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var testData = new TestData { Id = 123, Name = "Test" };

        // Act
        var machine = GetService<IFullTestMachine>();
        machine.Start();
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
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension);
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var testData = new TestData { Id = 456, Name = "Important" };

        // Act

        var machine = GetService<IFullTestMachine>() as FullTestMachine; // Użyj var lub FullTestMachine
        Assert.NotNull(machine);
        machine.Start();
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

        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(ext1);
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(ext2);
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(ext3);
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var testData = new TestData { Id = 789 };

        // Act
        var machine = GetService<IFullTestMachine>();
        machine.Start();
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
        machine.Start();
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
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension);
        Services.AddFullTestMachine(TestState.A);
        BuildProvider();

        var invalidData = new TestData { Id = -1 }; // Will fail guard

        // Act
        var machine = GetService<IFullTestMachine>();
        machine.Start();
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
    private class PayloadCapturingExtension : IStateMachineExtension<TestState, TestTrigger>
    {
        public object? LastPayload { get; private set; }

        public void OnAttemptStarting(in TransitionAttemptContext<TestState, TestTrigger> attempt)
            => LastPayload = attempt.Payload;
    }

    private class DetailedExtension : IStateMachineExtension<TestState, TestTrigger>
    {
        public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;
        public List<string> Events { get; } = [];

        public void OnAttemptStarting(in TransitionAttemptContext<TestState, TestTrigger> attempt)
        {
            Events.Add("BeforeTransition");
        }

        public void OnAttemptCompleted(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionResult<TestState> result)
        {
            Events.Add(result.Outcome == TransitionOutcome.Succeeded
                ? "AfterTransition:Success"
                : "AfterTransition:Failed");
        }

        public void OnGuardEvaluating(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionInfo<TestState> candidate,
            string guardName)
        {
            Events.Add($"GuardEvaluation:{guardName}");
        }

        public void OnGuardEvaluated(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionInfo<TestState> candidate,
            string guardName,
            bool result)
        {
            Events.Add($"GuardEvaluated:{guardName}:{result}");
        }
    }
}
