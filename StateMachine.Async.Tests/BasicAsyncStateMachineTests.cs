using Abstractions.Attributes;
using Shouldly;
using StateMachine.Contracts;
using StateMachine.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StateMachine.Async.Tests;

// Definicje dla testów
public enum AsyncStates
{
    Initial,
    Processing,
    Completed,
    Failed
}

public enum AsyncTriggers
{
    Start,
    Process,
    Complete,
    Fail,
    Reset
}

// Prosta maszyna asynchroniczna
[StateMachine(typeof(AsyncStates), typeof(AsyncTriggers))]
public partial class SimpleAsyncMachine
{
    private readonly List<string> _executionLog = new();
    public IReadOnlyList<string> ExecutionLog => _executionLog;

    // Async guard
    [Transition(AsyncStates.Initial, AsyncTriggers.Start, AsyncStates.Processing, Guard = nameof(CanStartAsync))]
    private async ValueTask<bool> CanStartAsync()
    {
        _executionLog.Add("CanStartAsync:Begin");
        await Task.Delay(10); // Symulacja async operacji
        _executionLog.Add("CanStartAsync:End");
        return true;
    }

    // Async action
    [Transition(AsyncStates.Processing, AsyncTriggers.Process, AsyncStates.Processing, Action = nameof(ProcessAsync))]
    private async Task ProcessAsync()
    {
        _executionLog.Add("ProcessAsync:Begin");
        await Task.Delay(10);
        _executionLog.Add("ProcessAsync:End");
    }

    // Sync action (dozwolone w async maszynie)
    [Transition(AsyncStates.Processing, AsyncTriggers.Complete, AsyncStates.Completed, Action = nameof(Complete))]
    private void Complete()
    {
        _executionLog.Add("Complete:Sync");
    }

    // Async OnEntry
    [State(AsyncStates.Processing, OnEntry = nameof(OnProcessingEntryAsync))]
    private async Task OnProcessingEntryAsync()
    {
        _executionLog.Add("OnProcessingEntry:Begin");
        await Task.Delay(5);
        _executionLog.Add("OnProcessingEntry:End");
    }

    // Async OnExit
    [State(AsyncStates.Processing, OnExit = nameof(OnProcessingExitAsync))]
    private async ValueTask OnProcessingExitAsync()
    {
        _executionLog.Add("OnProcessingExit:Begin");
        await Task.Delay(5);
        _executionLog.Add("OnProcessingExit:End");
    }
}

public class BasicAsyncStateMachineTests
{
    [Fact]
    public async Task Should_Execute_Async_Guard_And_Action_In_Order()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Initial);

        // Act
        var result = await machine.TryFireAsync(AsyncTriggers.Start);

        // Assert
        result.ShouldBeTrue();
        machine.CurrentState.ShouldBe(AsyncStates.Processing);

        // Sprawdzamy kolejność wykonania
        machine.ExecutionLog.ShouldBe(new[]
        {
            "CanStartAsync:Begin",
            "CanStartAsync:End",
            "OnProcessingEntry:Begin",
            "OnProcessingEntry:End"
        });
    }

    [Fact]
    public async Task Should_Execute_Async_Action_With_Internal_Transition()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Processing);

        // Act
        await machine.FireAsync(AsyncTriggers.Process);

        // Assert
        machine.CurrentState.ShouldBe(AsyncStates.Processing); // Stan się nie zmienił
        machine.ExecutionLog.ShouldContain("ProcessAsync:Begin");
        machine.ExecutionLog.ShouldContain("ProcessAsync:End");
        machine.ExecutionLog.IndexOf("ProcessAsync:Begin").ShouldBeLessThan(
            machine.ExecutionLog.IndexOf("ProcessAsync:End"));
    }

    [Fact]
    public async Task Should_Execute_OnExit_Before_OnEntry()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Processing);

        // Act
        await machine.FireAsync(AsyncTriggers.Complete);

        // Assert
        machine.CurrentState.ShouldBe(AsyncStates.Completed);
        var log = machine.ExecutionLog;

        // OnExit powinno być przed akcją
        log.IndexOf("OnProcessingExit:Begin").ShouldBeLessThan(log.IndexOf("Complete:Sync"));
    }

    [Fact]
    public void Should_Throw_When_Calling_Sync_Methods_On_Async_Machine()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Initial);

        // Act & Assert - wszystkie sync metody powinny rzucać wyjątek
        Should.Throw<SyncCallOnAsyncMachineException>(() => machine.TryFire(AsyncTriggers.Start));
        Should.Throw<SyncCallOnAsyncMachineException>(() => machine.Fire(AsyncTriggers.Start));
        Should.Throw<SyncCallOnAsyncMachineException>(() => machine.CanFire(AsyncTriggers.Start));
        Should.Throw<SyncCallOnAsyncMachineException>(() => machine.GetPermittedTriggers());
    }

    [Fact]
    public async Task Should_Support_CancellationToken()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Initial);
        using var cts = new CancellationTokenSource();

        // Act - anuluj przed operacją
        cts.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await machine.TryFireAsync(AsyncTriggers.Start, cancellationToken: cts.Token));
    }
}