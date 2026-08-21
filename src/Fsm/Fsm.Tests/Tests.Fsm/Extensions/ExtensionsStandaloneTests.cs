using System.Collections.Generic;
using FastFsm.Contracts;
using Tests.Machines.Extensions;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Fsm.Extensions
{
    public partial class ExtensionsStandaloneTests(ITestOutputHelper output)
    {
        private class TestExtension : IStateMachineExtension<ExtState, ExtTrigger>
        {
            public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;
            public List<string> Log { get; } = new();

            public void OnAttemptStarting(in TransitionAttemptContext<ExtState, ExtTrigger> attempt)
                => Log.Add($"Before: {attempt.SourceState}");

            public void OnAttemptCompleted(
                in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
                in TransitionResult<ExtState> result)
                => Log.Add($"After: {result.FinalState} - Success: {result.Outcome == TransitionOutcome.Succeeded}");

            public void OnGuardEvaluating(
                in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
                in TransitionInfo<ExtState> candidate,
                string guardName)
                => Log.Add($"GuardEval: {guardName}");

            public void OnGuardEvaluated(
                in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
                in TransitionInfo<ExtState> candidate,
                string guardName,
                bool result)
                => Log.Add($"GuardResult: {guardName} = {result}");
        }

        [Fact]
        public void Extensions_AddRemoveAtRuntime_WorksCorrectly()
        {
            // Arrange
            var ext1 = new TestExtension();
            var ext2 = new TestExtension();
            var machine = new ExtensionsMachine(ExtState.Idle, [ext1]);
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
            var machine = new ExtensionsMachine(ExtState.Idle, [extension]);
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
            var machine = new ExtensionsMachine(ExtState.Complete, [extension]);
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
            var machine = new ExtensionsMachine(ExtState.Idle, null);
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
            var machine = new ExtensionsMachine(ExtState.Idle, new IStateMachineExtension<ExtState, ExtTrigger>[] { faultyExtension, goodExtension });
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
