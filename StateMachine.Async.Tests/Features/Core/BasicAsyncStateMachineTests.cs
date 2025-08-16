using Abstractions.Attributes;
using Shouldly;
using StateMachine.Contracts;
using StateMachine.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StateMachine.Async.Tests.Features.Core;

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
        await machine.StartAsync();

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
        await machine.StartAsync();
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
        await machine.StartAsync();
        // Act
        await machine.FireAsync(AsyncTriggers.Complete);

        // Assert
        machine.CurrentState.ShouldBe(AsyncStates.Completed);
        var log = machine.ExecutionLog;

        // OnExit powinno być przed akcją
        log.IndexOf("OnProcessingExit:Begin").ShouldBeLessThan(log.IndexOf("Complete:Sync"));
    }

    [Fact]
    public async Task Should_Throw_When_Calling_Sync_Methods_On_Async_Machine()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Initial);
        await machine.StartAsync();

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
        await machine.StartAsync();
        using var cts = new CancellationTokenSource();

        // Act - anuluj przed operacją
        cts.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await machine.TryFireAsync(AsyncTriggers.Start, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task Initial_OnEntry_Is_FireAndForget_When_Constructed_In_Processing()
    {
        // Arrange – stan startowy ma OnEntry async
        var machine = new SimpleAsyncMachine(AsyncStates.Processing);
        await machine.StartAsync();

        // Assert
        machine.ExecutionLog.ShouldContain("OnProcessingEntry:Begin");
        machine.ExecutionLog.ShouldContain("OnProcessingEntry:End");
        machine.ExecutionLog.IndexOf("OnProcessingEntry:Begin")
               .ShouldBeLessThan(machine.ExecutionLog.IndexOf("OnProcessingEntry:End"));
    }

    [Fact]
    public async Task GetPermittedTriggersAsync_Evaluates_Async_Guards()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Initial);
        await machine.StartAsync();

        // Act
        var triggers = await machine.GetPermittedTriggersAsync();

        // Assert
        triggers.ShouldContain(AsyncTriggers.Start);
        triggers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetPermittedTriggersAsync_Returns_Empty_For_Terminal_State()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Completed);
        await machine.StartAsync();
        // Act
        var triggers = await machine.GetPermittedTriggersAsync();

        // Assert
        triggers.ShouldBeEmpty();
    }

    [Fact]
    public async Task CanFireAsync_Returns_False_For_Unknown_Trigger_In_Current_State()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Processing);
        await machine.StartAsync();
        // Act
        var canReset = await machine.CanFireAsync(AsyncTriggers.Reset);

        // Assert
        canReset.ShouldBeFalse();
    }

    [Fact]
    public async Task Parallel_Fires_Do_Not_Break_State_And_All_Internal_Actions_Run()
    {
        // Arrange
        var machine = new SimpleAsyncMachine(AsyncStates.Processing);
        await machine.StartAsync();
        // Act
        var tasks = new List<Task>();
        const int fires = 5;
        var vtasks = new List<ValueTask>();
        for (int i = 0; i < fires; i++)
            vtasks.Add(machine.FireAsync(AsyncTriggers.Process));

        await Task.WhenAll(vtasks.Select(vt => vt.AsTask()));


        // Assert – stan się nie zmienia (internal transition)
        machine.CurrentState.ShouldBe(AsyncStates.Processing);

        // Powinno być tyle samo Begin/End co wywołań
        machine.ExecutionLog.Count(s => s == "ProcessAsync:Begin").ShouldBe(fires);
        machine.ExecutionLog.Count(s => s == "ProcessAsync:End").ShouldBe(fires);
    }

}
internal static class ReadOnlyListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> list, T value)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(list[i], value))
                return i;
        }
        return -1;
    }
}
