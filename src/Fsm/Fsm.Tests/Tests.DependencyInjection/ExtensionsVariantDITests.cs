using Microsoft.Extensions.DependencyInjection;
using FastFsm.Contracts;
using Tests.DependencyInjection.TestMachines;
using System;
using System.Collections.Generic;
using System.Linq;
using Tests.DependencyInjection;
using FastFsm.DependencyInjection;
using Xunit;


namespace Tests.DependencyInjection;

public class ExtensionsVariantDITests : DITestBase
{
    [Fact]
    public void Extensions_AreInjectedAutomatically()
    {
        // Arrange
        var extension1 = new TestExtension("Ext1");
        var extension2 = new TestExtension("Ext2");

        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension1);
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension2);
        Services.AddExtensionsTestMachine(TestState.A);
        BuildProvider();

        // Act
        var machine = GetService<IExtensionsTestMachine>();
        machine.Start();
        machine.TryFire(TestTrigger.Next);

        // Assert
        Assert.Contains("Ext1:Before", extension1.Events);
        Assert.Contains("Ext1:After", extension1.Events);
        Assert.Contains("Ext2:Before", extension2.Events);
        Assert.Contains("Ext2:After", extension2.Events);
    }

    [Fact]
    public void Extensions_ExecuteInRegistrationOrder()
    {
        // Arrange
        var sharedEvents = new List<string>();
        var extension1 = new OrderedTestExtension("Ext1", sharedEvents);
        var extension2 = new OrderedTestExtension("Ext2", sharedEvents);
        var extension3 = new OrderedTestExtension("Ext3", sharedEvents);

        // Register in specific order
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension1);
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension2);
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension3);
        Services.AddExtensionsTestMachine(TestState.A);
        BuildProvider();

        // Act
        var machine = GetService<IExtensionsTestMachine>();
        machine.Start();
        machine.TryFire(TestTrigger.Next);

        // Assert - Check order
        var beforeEvents = sharedEvents.Where(e => e.Contains("Before")).ToList();
        Assert.Equal(new[] { "Ext1:Before", "Ext2:Before", "Ext3:Before" }, beforeEvents);

        var afterEvents = sharedEvents.Where(e => e.Contains("After")).ToList();
        Assert.Equal(new[] { "Ext1:After", "Ext2:After", "Ext3:After" }, afterEvents);
    }

    [Fact]
    public void Extensions_CanBeRegisteredUsingHelper()
    {
        // Arrange
        Services.AddStateMachineExtension<TestState, TestTrigger, TestExtension>();
        Services.AddExtensionsTestMachine(TestState.A);
        BuildProvider();

        // Act
        var machine = GetService<IExtensionsTestMachine>();
        machine.Start();
        var extensions = GetService<IEnumerable<IStateMachineExtension<TestState, TestTrigger>>>();

        // Assert
        Assert.Single(extensions);
        Assert.IsType<TestExtension>(extensions.First());
    }
    [Fact]
    public void NoExtensions_MachineStillWorks()
    {
        // Arrange - No extensions registered
        Services.AddExtensionsTestMachine(TestState.A);
        BuildProvider();

        // Act
        var machine = GetService<IExtensionsTestMachine>() as ExtensionsTestMachine;
        Assert.NotNull(machine); // Change to NotNull!
        machine.Start();

        var result = machine.TryFire(TestTrigger.Next);

        // Assert
        Assert.True(result);
        Assert.Equal(TestState.B, machine.CurrentState);
        Assert.Empty(machine.Extensions);
    }

    [Fact]
    public void Extensions_WithDifferentLifetimes()
    {
        // Arrange
        Services.AddStateMachineExtension<TestState, TestTrigger, SingletonExtension>(ServiceLifetime.Singleton);
        Services.AddStateMachineExtension<TestState, TestTrigger, ScopedExtension>(ServiceLifetime.Scoped);
        Services.AddStateMachineExtension<TestState, TestTrigger, TransientExtension>(ServiceLifetime.Transient);
        Services.AddExtensionsTestMachine(TestState.A);
        BuildProvider();

        // Act & Assert - Singleton
        using var scope1 = CreateScope();
        var extensions1 = scope1.ServiceProvider.GetServices<IStateMachineExtension<TestState, TestTrigger>>().ToList();

        using var scope2 = CreateScope();
        var extensions2 = scope2.ServiceProvider.GetServices<IStateMachineExtension<TestState, TestTrigger>>().ToList();

        var singleton1 = extensions1.OfType<SingletonExtension>().First();
        var singleton2 = extensions2.OfType<SingletonExtension>().First();
        Assert.Same(singleton1, singleton2); // Singleton same instance

        // Act & Assert - Scoped
        var scoped1 = extensions1.OfType<ScopedExtension>().First();
        var scoped2 = extensions2.OfType<ScopedExtension>().First();
        Assert.NotSame(scoped1, scoped2); // Scoped different per scope

        // Act & Assert - Transient
        // For transient, we must call GetServices twice
        var transientExtensions1 = scope1.ServiceProvider.GetServices<IStateMachineExtension<TestState, TestTrigger>>().ToList();
        var transientExtensions2 = scope1.ServiceProvider.GetServices<IStateMachineExtension<TestState, TestTrigger>>().ToList();

        var transient1 = transientExtensions1.OfType<TransientExtension>().First();
        var transient2 = transientExtensions2.OfType<TransientExtension>().First();
        Assert.NotSame(transient1, transient2); // Transient always different
    }

    [Fact]
    public void Extensions_HaveAccessToContext()
    {
        // Arrange
        var extension = new ContextCapturingExtension();
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension);
        Services.AddExtensionsTestMachine(TestState.A);
        BuildProvider();

        // Act
        var machine = GetService<IExtensionsTestMachine>();
        machine.Start();
        machine.TryFire(TestTrigger.Next);

        // Assert
        Assert.Equal(TestState.A, extension.LastAttempt.SourceState);
        Assert.Equal(TestState.B, extension.LastTransition.DeclaredTarget);
        Assert.Equal(TestTrigger.Next, extension.LastAttempt.Trigger);
        Assert.NotEqual(Guid.Empty, extension.LastAttempt.InstanceId);
        Assert.True(extension.LastAttempt.AttemptId > 0);
        Assert.True(extension.LastAttempt.StartTimestamp > 0);
    }

    [Fact]
    public void Extensions_GuardHooksWork()
    {
        // Arrange
        var extension = new TestExtension("Guard");
        Services.AddSingleton<IStateMachineExtension<TestState, TestTrigger>>(extension);
        Services.AddGuardedTestMachine(TestState.A);
        BuildProvider();

        // Act
        var machine = GetService<IGuardedTestMachine>();
        machine.Start();
        machine.TryFire(TestTrigger.Next);

        // Assert
        Assert.Contains("Guard:GuardEval:CanTransition", extension.Events);
        Assert.Contains("Guard:GuardEvaluated:CanTransition:True", extension.Events);
    }

    // Test Extensions
    private class TestExtension : IStateMachineExtension<TestState, TestTrigger>
    {
        public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;
        public string Name { get; }
        public List<string> Events { get; } = [];

        public TestExtension(string name = "Test")
        {
            Name = name;
        }

        public virtual void OnAttemptStarting(in TransitionAttemptContext<TestState, TestTrigger> attempt) => Events.Add($"{Name}:Before");

        public virtual void OnAttemptCompleted(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionResult<TestState> result) => Events.Add($"{Name}:After");

        public virtual void OnGuardEvaluating(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionInfo<TestState> candidate,
            string guardName) => Events.Add($"{Name}:GuardEval:{guardName}");

        public virtual void OnGuardEvaluated(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionInfo<TestState> candidate,
            string guardName,
            bool result) => Events.Add($"{Name}:GuardEvaluated:{guardName}:{result}");
    }

    private class OrderedTestExtension : TestExtension
    {
        private readonly List<string> _sharedEvents;

        public OrderedTestExtension(string name, List<string> sharedEvents) : base(name)
        {
            _sharedEvents = sharedEvents;
        }

        public override void OnAttemptStarting(in TransitionAttemptContext<TestState, TestTrigger> attempt)
        {
            base.OnAttemptStarting(in attempt);
            _sharedEvents.Add($"{Name}:Before");
        }

        public override void OnAttemptCompleted(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionResult<TestState> result)
        {
            base.OnAttemptCompleted(in attempt, in result);
            _sharedEvents.Add($"{Name}:After");
        }
    }

    private class ContextCapturingExtension : IStateMachineExtension<TestState, TestTrigger>
    {
        public TransitionAttemptContext<TestState, TestTrigger> LastAttempt { get; private set; }
        public TransitionInfo<TestState> LastTransition { get; private set; }

        public void OnAttemptStarting(in TransitionAttemptContext<TestState, TestTrigger> attempt)
            => LastAttempt = attempt;

        public void OnTransitionMatched(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionInfo<TestState> matched)
            => LastTransition = matched;
    }

    private class SingletonExtension : TestExtension { }
    private class ScopedExtension : TestExtension { }
    private class TransientExtension : TestExtension { }
}
