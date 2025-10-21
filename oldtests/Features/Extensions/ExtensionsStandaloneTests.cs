using Shouldly;
using System.Collections.Generic;
using FastFsm.Contracts;
using FastFsm.Tests.Machines;
using Xunit;
using Xunit.Abstractions;
using FastFsm.Tests.Machines.Legacy;

namespace FastFsm.Tests.Features.Extensions
{
    public partial class ExtensionsStandaloneTests(ITestOutputHelper output)
    {

        private class TestExtension : IStateMachineExtension
        {
            public List<string> Log { get; } = new();

            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                Log.Add($"Before: {context.GetType().Name}");
            }

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
            {
                Log.Add($"After: {context.GetType().Name} - Success: {success}");
            }

            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
            {
                Log.Add($"GuardEval: {guardName}");
            }

            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
            {
                Log.Add($"GuardResult: {guardName} = {result}");
            }

            public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext { }
            public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext { }
        }

        [Fact]
        public void Extensions_AddRemoveAtRuntime_WorksCorrectly()
        {
            // Arrange
            var ext1 = new TestExtension();
            var ext2 = new TestExtension();
            var machine = new Machines.Legacy.ExtensionsMachine(ExtState.Idle, [ext1]);
            machine.Start();

            // Act & Assert - Initial extension works
            machine.TryFire(ExtTrigger.Start);
            ext1.Log.ShouldNotBeEmpty();
            ext2.Log.ShouldBeEmpty();

            // Add second extension
            machine.AddExtension(ext2);
            machine.TryFire(ExtTrigger.Finish);
            ext2.Log.ShouldNotBeEmpty();

            // Remove first extension
            var removed = machine.RemoveExtension(ext1);
            removed.ShouldBeTrue();

            ext1.Log.Clear();
            machine.TryFire(ExtTrigger.Cancel);
            ext1.Log.ShouldBeEmpty();
            ext2.Log.Count.ShouldBeGreaterThan(1);
        }

        [Fact]
        public void Extensions_GuardNotifications_ReceiveCorrectInfo()
        {
            // Arrange
            var extension = new TestExtension();
            var machine = new Machines.Legacy.ExtensionsMachine(ExtState.Idle, [extension]);
            machine.Start();

            // Act
            machine.TryFire(ExtTrigger.Start); // Has guard

            // Assert
            extension.Log.ShouldContain(log => log.StartsWith("GuardEval:"));
            extension.Log.ShouldContain(log => log.StartsWith("GuardResult:"));
        }

        [Fact]
        public void Extensions_FailedTransition_StillNotified()
        {
            // Arrange
            var extension = new TestExtension();
            var machine = new Machines.Legacy.ExtensionsMachine(ExtState.Complete, [extension]);
            machine.Start();

            // Act
            var result = machine.TryFire(ExtTrigger.Start); // Invalid from Complete

            // Assert
            result.ShouldBeFalse();
            output.WriteLine(string.Join("\n", extension.Log));
            extension.Log.ShouldContain(log => log.Contains("Success: False"));
        }

        [Fact]
        public void Extensions_WithoutExtensions_MachineStillWorks()
        {
            // Arrange
            var machine = new Machines.Legacy.ExtensionsMachine(ExtState.Idle, null);
            machine.Start();

            // Act
            var result = machine.TryFire(ExtTrigger.Start);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(ExtState.Working);
        }

        [Fact]
        public void Extensions_ExceptionInExtension_DoesNotBreakTransition()
        {
            // Arrange
            var faultyExtension = new FaultyExtension();
            var goodExtension = new TestExtension();
            var machine = new Machines.Legacy.ExtensionsMachine(ExtState.Idle, new IStateMachineExtension[] { faultyExtension, goodExtension });
            machine.Start();

            // Act
            var result = machine.TryFire(ExtTrigger.Start);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(ExtState.Working);
            goodExtension.Log.ShouldNotBeEmpty(); // Good extension still executed
        }
    }



}
