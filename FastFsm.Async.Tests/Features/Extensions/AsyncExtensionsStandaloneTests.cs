using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Xunit;

namespace  FastFsm.Async.Tests.Features.Extensions;

public class AsyncExtensionsStandaloneTests
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
    }

    [Fact]
    public async Task Extensions_AddRemoveAtRuntime_WorksCorrectly()
    {
        var ext1 = new TestExtension();
        var ext2 = new TestExtension();
        var machine = new AsyncExtensionsMachine(ExtState.Idle, new IStateMachineExtension[] { ext1 });
        await machine.StartAsync();

        // Initial extension active
        var ok = await machine.TryFireAsync(ExtTrigger.Start);
        ok.ShouldBeTrue();
        ext1.Log.ShouldNotBeEmpty();
        ext2.Log.ShouldBeEmpty();

        // Add second extension
        machine.AddExtension(ext2);
        ok = await machine.TryFireAsync(ExtTrigger.Finish);
        ok.ShouldBeTrue();
        ext2.Log.ShouldNotBeEmpty();

        // Remove first extension
        var removed = machine.RemoveExtension(ext1);
        removed.ShouldBeTrue();

        ext1.Log.Clear();
        ok = await machine.TryFireAsync(ExtTrigger.Cancel);
        ok.ShouldBeTrue();
        ext1.Log.ShouldBeEmpty();
        ext2.Log.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task Extensions_GuardNotifications_ReceiveCorrectInfo()
    {
        var extension = new TestExtension();
        var machine = new AsyncExtensionsMachine(ExtState.Idle, new[] { extension });
        await machine.StartAsync();

        await machine.TryFireAsync(ExtTrigger.Start); // Has guard

        extension.Log.ShouldContain(log => log.StartsWith("GuardEval:"));
        extension.Log.ShouldContain(log => log.StartsWith("GuardResult:"));
    }

    [Fact]
    public async Task Extensions_FailedTransition_StillNotified()
    {
        var extension = new TestExtension();
        var machine = new AsyncExtensionsMachine(ExtState.Complete, new[] { extension });
        await machine.StartAsync();

        var result = await machine.TryFireAsync(ExtTrigger.Start); // Invalid from Complete

        result.ShouldBeFalse();
        extension.Log.ShouldContain(log => log.Contains("Success: False"));
    }

    [Fact]
    public async Task Extensions_WithoutExtensions_MachineStillWorks()
    {
        var machine = new AsyncExtensionsMachine(ExtState.Idle, null);
        await machine.StartAsync();

        var result = await machine.TryFireAsync(ExtTrigger.Start);

        result.ShouldBeTrue();
        machine.CurrentState.ShouldBe(ExtState.Working);
    }

    [Fact]
    public async Task Extensions_ExceptionInExtension_DoesNotBreakTransition()
    {
        var faultyExtension = new FaultyExtension();
        var goodExtension = new TestExtension();
        var machine = new AsyncExtensionsMachine(ExtState.Idle, new IStateMachineExtension[] { faultyExtension, goodExtension });
        await machine.StartAsync();

        var result = await machine.TryFireAsync(ExtTrigger.Start);

        result.ShouldBeTrue();
        machine.CurrentState.ShouldBe(ExtState.Working);
        goodExtension.Log.ShouldNotBeEmpty(); // Good extension still executed
    }

    private class FaultyExtension : IStateMachineExtension
    {
        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            throw new Exception("Extension error");
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        {
            throw new Exception("Extension error");
        }

        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
        {
            throw new Exception("Extension error");
        }

        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
        {
            throw new Exception("Extension error");
        }
    }
}

// Async machine with extensions support
[StateMachine(typeof(ExtState), typeof(ExtTrigger), GenerateExtensibleVersion = true)]
public partial class AsyncExtensionsMachine
{
    [State(ExtState.Idle, OnEntry = nameof(OnEnterIdleAsync))]
    [State(ExtState.Working, OnExit = nameof(OnExitWorkingAsync))]
    private void ConfigureStates() { }

    [Transition(ExtState.Idle, ExtTrigger.Start, ExtState.Working,
        Guard = nameof(CanStartAsync), Action = nameof(StartWorkAsync))]
    [Transition(ExtState.Working, ExtTrigger.Finish, ExtState.Complete)]
    [Transition(ExtState.Complete, ExtTrigger.Cancel, ExtState.Idle)]
    private void Configure() { }

    private async ValueTask<bool> CanStartAsync()
    {
        await Task.Yield();
        return true;
    }

    private async Task StartWorkAsync()
    {
        await Task.Yield();
    }

    private async Task OnEnterIdleAsync()
    {
        await Task.Yield();
    }

    private async Task OnExitWorkingAsync()
    {
        await Task.Yield();
    }
}

public enum ExtState { Idle, Working, Complete }
public enum ExtTrigger { Start, Finish, Cancel }
