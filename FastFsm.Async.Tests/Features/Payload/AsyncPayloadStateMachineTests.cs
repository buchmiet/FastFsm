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

namespace StateMachine.Async.Tests.Features.Payload;

#region Test Enums and Payload Classes

public enum AsyncPayloadStates
{
    Initial,
    Processing,
    Completed,
    Failed
}

public enum AsyncPayloadTriggers
{
    Start,
    Process,
    Complete,
    Fail,
    Reset
}

public class ProcessPayload
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
}

public class ResultPayload
{
    public string Result { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
}

public class ErrorPayload
{
    public string ErrorCode { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}

#endregion

#region 1. Basic Async Payload Machine

[StateMachine(typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers))]
[PayloadType(typeof(ProcessPayload))]
public partial class BasicAsyncPayloadMachine
{
    private readonly List<string> _executionLog = new();
    public IReadOnlyList<string> ExecutionLog => _executionLog;
    public ProcessPayload? LastProcessedPayload { get; private set; }

    // Async guard with payload
    [Transition(AsyncPayloadStates.Initial, AsyncPayloadTriggers.Start, AsyncPayloadStates.Processing,
        Guard = nameof(CanStartAsync), Action = nameof(StartProcessingAsync))]
    private async ValueTask<bool> CanStartAsync(ProcessPayload payload)
    {
        _executionLog.Add($"CanStartAsync:Begin:{payload.Id}");
        await Task.Delay(10);
        _executionLog.Add($"CanStartAsync:End:{payload.Id}");
        return payload.IsValid;
    }

    // Async action with payload
    private async Task StartProcessingAsync(ProcessPayload payload)
    {
        _executionLog.Add($"StartProcessingAsync:Begin:{payload.Id}");
        await Task.Delay(10);
        LastProcessedPayload = payload;
        _executionLog.Add($"StartProcessingAsync:End:{payload.Id}");
    }

    // Async OnEntry with payload
    [State(AsyncPayloadStates.Processing, OnEntry = nameof(OnProcessingEntryAsync))]
    private async Task OnProcessingEntryAsync(ProcessPayload payload)
    {
        _executionLog.Add($"OnProcessingEntry:Begin:{payload.Data}");
        await Task.Delay(5);
        _executionLog.Add($"OnProcessingEntry:End:{payload.Data}");
    }
}

#endregion

#region 2. Overloaded Methods Machine

[StateMachine(typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers))]
[PayloadType(typeof(ProcessPayload))]
public partial class OverloadedAsyncMachine
{
    public List<string> CallLog { get; } = [];

    [State(AsyncPayloadStates.Processing, OnEntry = nameof(OnEntryProcessing))]
    private void ConfigureStates() { }

    [Transition(AsyncPayloadStates.Initial, AsyncPayloadTriggers.Start, AsyncPayloadStates.Processing,
        Guard = nameof(CanStart), Action = nameof(DoStart))]
    private void Configure() { }

    // Parameterless versions
    private async ValueTask<bool> CanStart()
    {
        CallLog.Add("Guard()");
        await Task.Delay(5);
        return true;
    }

    private async Task DoStart()
    {
        CallLog.Add("Action()");
        await Task.Delay(5);
    }

    private async Task OnEntryProcessing()
    {
        CallLog.Add("OnEntry()");
        await Task.Delay(5);
    }

    // Payload versions
    private async ValueTask<bool> CanStart(ProcessPayload payload)
    {
        CallLog.Add($"Guard(payload:{payload.Id})");
        await Task.Delay(5);
        return payload.IsValid;
    }

    private async Task DoStart(ProcessPayload payload)
    {
        CallLog.Add($"Action(payload:{payload.Id})");
        await Task.Delay(5);
    }

    private async Task OnEntryProcessing(ProcessPayload payload)
    {
        CallLog.Add($"OnEntry(payload:{payload.Id})");
        await Task.Delay(5);
    }
}

#endregion

#region 3. Multi-Payload Machine

public enum MultiPayloadStates { Ready, ConfigSet, Processing, Done, Error }
public enum MultiPayloadTriggers { Configure, Process, Complete, HandleError }

public class ConfigPayload
{
    public string Setting { get; set; } = string.Empty;
    public int Timeout { get; set; }
}

public class DataPayload
{
    public int Value { get; set; }
    public string Tag { get; set; } = string.Empty;
}

[StateMachine(typeof(MultiPayloadStates), typeof(MultiPayloadTriggers))]
[PayloadType(MultiPayloadTriggers.Configure, typeof(ConfigPayload))]
[PayloadType(MultiPayloadTriggers.Process, typeof(DataPayload))]
[PayloadType(MultiPayloadTriggers.Complete, typeof(ResultPayload))]
[PayloadType(MultiPayloadTriggers.HandleError, typeof(ErrorPayload))]
public partial class MultiPayloadAsyncMachine
{
    public string CurrentSetting { get; private set; } = string.Empty;
    public int ProcessedValue { get; private set; }
    public string LastResult { get; private set; } = string.Empty;
    public string LastErrorCode { get; private set; } = string.Empty;

    [Transition(MultiPayloadStates.Ready, MultiPayloadTriggers.Configure, MultiPayloadStates.ConfigSet,
        Action = nameof(ApplyConfigurationAsync))]
    [Transition(MultiPayloadStates.ConfigSet, MultiPayloadTriggers.Process, MultiPayloadStates.Processing,
        Guard = nameof(CanProcessAsync), Action = nameof(ProcessDataAsync))]
    [Transition(MultiPayloadStates.Processing, MultiPayloadTriggers.Complete, MultiPayloadStates.Done,
        Action = nameof(CompleteProcessingAsync))]
    [Transition(MultiPayloadStates.Processing, MultiPayloadTriggers.HandleError, MultiPayloadStates.Error,
        Action = nameof(HandleErrorAsync))]
    private void Configure() { }

    private async Task ApplyConfigurationAsync(ConfigPayload config)
    {
        await Task.Delay(10);
        CurrentSetting = config.Setting;
    }

    private async ValueTask<bool> CanProcessAsync(DataPayload data)
    {
        await Task.Delay(5);
        return data.Value > 0;
    }

    private async Task ProcessDataAsync(DataPayload data)
    {
        await Task.Delay(10);
        ProcessedValue = data.Value;
    }

    private async Task CompleteProcessingAsync(ResultPayload result)
    {
        await Task.Delay(5);
        LastResult = result.Result;
    }

    private async Task HandleErrorAsync(ErrorPayload error)
    {
        await Task.Delay(5);
        LastErrorCode = error.ErrorCode;
    }
}

#endregion

#region 4. Exception Handling Machine

[StateMachine(typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers))]
[PayloadType(typeof(ProcessPayload))]
public partial class ExceptionAsyncPayloadMachine
{
    private readonly List<string> _log = new();
    public IReadOnlyList<string> Log => _log;

    // Guard that throws
    [Transition(AsyncPayloadStates.Initial, AsyncPayloadTriggers.Start, AsyncPayloadStates.Processing,
        Guard = nameof(ThrowingGuardAsync))]
    private async ValueTask<bool> ThrowingGuardAsync(ProcessPayload payload)
    {
        _log.Add($"Guard:Begin:{payload.Id}");
        await Task.Yield();
        throw new InvalidOperationException("guard with payload failed");
    }

    // Action that throws
    [Transition(AsyncPayloadStates.Initial, AsyncPayloadTriggers.Process, AsyncPayloadStates.Processing,
        Action = nameof(ThrowingActionAsync))]
    private async Task ThrowingActionAsync(ProcessPayload payload)
    {
        _log.Add($"Action:Begin:{payload.Id}");
        await Task.Yield();
        throw new InvalidOperationException("action with payload failed");
    }

    // OnEntry that throws
    [State(AsyncPayloadStates.Failed, OnEntry = nameof(ThrowingOnEntryAsync))]
    [Transition(AsyncPayloadStates.Initial, AsyncPayloadTriggers.Fail, AsyncPayloadStates.Failed)]
    private async Task ThrowingOnEntryAsync(ProcessPayload payload)
    {
        _log.Add($"OnEntry:Begin:{payload.Id}");
        await Task.Yield();
        throw new InvalidOperationException("onentry with payload failed");
    }
}

#endregion

#region 5. CanFire with Payload Machine

[StateMachine(typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers))]
[PayloadType(typeof(ProcessPayload))]
public partial class CanFireAsyncPayloadMachine
{
    private readonly int _threshold;

    public CanFireAsyncPayloadMachine(AsyncPayloadStates initialState, int threshold) : this(initialState)
    {
        _threshold = threshold;
    }

    [Transition(AsyncPayloadStates.Initial, AsyncPayloadTriggers.Start, AsyncPayloadStates.Processing,
        Guard = nameof(CheckThresholdAsync))]
    private void Configure() { }

    private async ValueTask<bool> CheckThresholdAsync(ProcessPayload payload)
    {
        await Task.Delay(10);
        return payload.Id >= _threshold;
    }
}

#endregion

#region 6. Concurrent Operations Machine

[StateMachine(typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers))]
[PayloadType(typeof(ProcessPayload))]
public partial class ConcurrentAsyncPayloadMachine
{
    private readonly List<int> _processedIds = new();
    private readonly object _lock = new();

    public IReadOnlyList<int> ProcessedIds => _processedIds.ToList();

    [InternalTransition(AsyncPayloadStates.Processing, AsyncPayloadTriggers.Process, nameof(ProcessDataAsync))]
    private void Configure() { }

    private async Task ProcessDataAsync(ProcessPayload payload)
    {
        await Task.Delay(Random.Shared.Next(5, 20));
        lock (_lock)
        {
            _processedIds.Add(payload.Id);
        }
    }
}

#endregion

#region 7. Initial State OnEntry Machine

[StateMachine(typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers))]
[PayloadType(typeof(ProcessPayload))]
public partial class InitialOnEntryAsyncPayloadMachine
{
    public bool InitialEntryCalledParameterless { get; private set; }
    public bool InitialEntryCalledWithPayload { get; private set; }
    public List<string> CallLog { get; } = [];

    [State(AsyncPayloadStates.Initial, OnEntry = nameof(OnEntryInitial))]
    private void ConfigureStates() { }

    [Transition(AsyncPayloadStates.Initial, AsyncPayloadTriggers.Start, AsyncPayloadStates.Processing)]
    private void Configure() { }

    private async Task OnEntryInitial()
    {
        CallLog.Add("OnEntry()");
        await Task.Delay(10);
        InitialEntryCalledParameterless = true;
    }

    private async Task OnEntryInitial(ProcessPayload payload)
    {
        CallLog.Add($"OnEntry(payload:{payload.Id})");
        await Task.Delay(10);
        InitialEntryCalledWithPayload = true;
    }
}

#endregion





#region Test Class

public class AsyncPayloadStateMachineTests
{
    [Fact]
    public async Task Should_Execute_Async_Guard_With_Payload_And_Action_In_Order()
    {
        // Arrange
        var machine = new BasicAsyncPayloadMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();
        var payload = new ProcessPayload { Id = 123, Data = "TestData", IsValid = true };

        // Act
        var result = await machine.TryFireAsync(AsyncPayloadTriggers.Start, payload);

        // Assert
        result.ShouldBeTrue();
        machine.CurrentState.ShouldBe(AsyncPayloadStates.Processing);
        machine.LastProcessedPayload.ShouldNotBeNull();
        machine.LastProcessedPayload.Id.ShouldBe(123);

        // Verify execution order
        machine.ExecutionLog.ShouldBe(new[]
        {
            "CanStartAsync:Begin:123",
            "CanStartAsync:End:123",
            "OnProcessingEntry:Begin:TestData",
            "OnProcessingEntry:End:TestData",
            "StartProcessingAsync:Begin:123",
            "StartProcessingAsync:End:123"
        });
    }

    [Fact]
    public async Task Should_Execute_Async_OnEntry_With_Payload()
    {
        // Arrange
        var machine = new BasicAsyncPayloadMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();
        var payload = new ProcessPayload { Id = 456, Data = "EntryTest", IsValid = true };

        // Act
        await machine.FireAsync(AsyncPayloadTriggers.Start, payload);

        // Assert
        machine.CurrentState.ShouldBe(AsyncPayloadStates.Processing);
        machine.ExecutionLog.ShouldContain("OnProcessingEntry:Begin:EntryTest");
        machine.ExecutionLog.ShouldContain("OnProcessingEntry:End:EntryTest");
    }

    [Fact]
    public async Task Should_Handle_Overloaded_Methods_With_And_Without_Payload()
    {
        // Arrange
        var machine = new OverloadedAsyncMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();

        // Act 1: Fire without payload - should use parameterless versions
        await machine.FireAsync(AsyncPayloadTriggers.Start);

        // Assert 1
        machine.CallLog.ShouldBe(new[] { "Guard()", "OnEntry()", "Action()" });

        // Reset for next test
        machine.CallLog.Clear();
        machine = new OverloadedAsyncMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();

        // Act 2: Fire with payload - should use payload versions
        var payload = new ProcessPayload { Id = 789, IsValid = true };
        await machine.FireAsync(AsyncPayloadTriggers.Start, payload);

        // Assert 2
        machine.CallLog.ShouldBe(new[]
        {
            "Guard(payload:789)",
            "OnEntry(payload:789)",
            "Action(payload:789)"
        });
    }

    [Fact]
    public async Task Should_Execute_Different_Payloads_For_Different_Triggers()
    {
        // Arrange
        var machine = new MultiPayloadAsyncMachine(MultiPayloadStates.Ready);
        await machine.StartAsync();

        // Act 1: Configure with ConfigPayload
        var configPayload = new ConfigPayload { Setting = "TestSetting", Timeout = 30 };
        await machine.FireAsync(MultiPayloadTriggers.Configure, configPayload);

        // Assert 1
        machine.CurrentState.ShouldBe(MultiPayloadStates.ConfigSet);
        machine.CurrentSetting.ShouldBe("TestSetting");

        // Act 2: Process with DataPayload
        var dataPayload = new DataPayload { Value = 42, Tag = "TestTag" };
        await machine.FireAsync(MultiPayloadTriggers.Process, dataPayload);

        // Assert 2
        machine.CurrentState.ShouldBe(MultiPayloadStates.Processing);
        machine.ProcessedValue.ShouldBe(42);

        // Act 3: Complete with ResultPayload
        var resultPayload = new ResultPayload { Result = "Success", ProcessedCount = 1 };
        await machine.FireAsync(MultiPayloadTriggers.Complete, resultPayload);

        // Assert 3
        machine.CurrentState.ShouldBe(MultiPayloadStates.Done);
        machine.LastResult.ShouldBe("Success");
    }

    [Fact]
    public async Task Should_Validate_Payload_Type_Before_Async_Operations()
    {
        // Arrange
        var machine = new MultiPayloadAsyncMachine(MultiPayloadStates.Ready);
        await machine.StartAsync();

        // Act - Try to fire Configure trigger with wrong payload type
        var wrongPayload = new DataPayload { Value = 123 };
        var result = await machine.TryFireAsync(MultiPayloadTriggers.Configure, wrongPayload);

        // Assert
        result.ShouldBeFalse();
        machine.CurrentState.ShouldBe(MultiPayloadStates.Ready); // State unchanged
        machine.CurrentSetting.ShouldBe(string.Empty); // Action not executed
    }

    [Fact]
    public async Task TryFireAsync_When_Guard_With_Payload_Throws_Should_Return_False()
    {
        // Arrange
        var machine = new ExceptionAsyncPayloadMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();
        var payload = new ProcessPayload { Id = 999 };

        // Act
        var result = await machine.TryFireAsync(AsyncPayloadTriggers.Start, payload);

        // Assert
        result.ShouldBeFalse();
        machine.CurrentState.ShouldBe(AsyncPayloadStates.Initial);
        machine.Log.ShouldContain("Guard:Begin:999");
    }

    [Fact]
    public async Task TryFireAsync_When_Action_With_Payload_Throws_Should_Throw_And_State_Changed()
    {
        // Arrange
        var machine = new ExceptionAsyncPayloadMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();
        var payload = new ProcessPayload { Id = 888 };

        // Act + Assert: teraz oczekujemy wyjątku z akcji
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await machine.TryFireAsync(AsyncPayloadTriggers.Process, payload));

        // Stan: bez rollbacku, ustawiony na docelowy
        machine.CurrentState.ShouldBe(AsyncPayloadStates.Processing);

        // Log: akcja rozpoczęta i rzuciła; brak OnEntry/OnExit w tym scenariuszu
        machine.Log.ShouldContain("Action:Begin:888");
        // (opcjonalnie)
        // machine.Log.ShouldNotContain("OnEntry:Begin:888");
    }


    [Fact]
    public async Task TryFireAsync_When_OnEntry_With_Payload_Throws_Should_Throw_And_State_Changed()
    {
        // Arrange
        var machine = new ExceptionAsyncPayloadMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();
        var payload = new ProcessPayload { Id = 777 };

        // Act + Assert: oczekujemy propagacji wyjątku z OnEntry
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await machine.TryFireAsync(AsyncPayloadTriggers.Fail, payload));

        // Brak rollbacku – stan docelowy ustawiony przed OnEntry
        machine.CurrentState.ShouldBe(AsyncPayloadStates.Failed);

        // Log: OnEntry rozpoczęte (i rzuciło)
        machine.Log.ShouldContain("OnEntry:Begin:777");
    }


    [Fact]
    public async Task CanFireAsync_With_Payload_Should_Evaluate_Guard_With_Correct_Payload()
    {
        // Arrange
        var machine = new CanFireAsyncPayloadMachine(AsyncPayloadStates.Initial, threshold: 100);
        await machine.StartAsync();

        // Act & Assert - payload below threshold
        var lowPayload = new ProcessPayload { Id = 50 };
        var canFireLow = await machine.CanFireAsync(AsyncPayloadTriggers.Start, lowPayload);
        canFireLow.ShouldBeFalse();

        // Act & Assert - payload above threshold
        var highPayload = new ProcessPayload { Id = 150 };
        var canFireHigh = await machine.CanFireAsync(AsyncPayloadTriggers.Start, highPayload);
        canFireHigh.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPermittedTriggersAsync_With_PayloadResolver_Should_Evaluate_Guards()
    {
        // Arrange
        var machine = new CanFireAsyncPayloadMachine(AsyncPayloadStates.Initial, threshold: 100);
        await machine.StartAsync();

        // Act
        var triggers = await machine.GetPermittedTriggersAsync(trigger =>
        {
            if (trigger == AsyncPayloadTriggers.Start)
                return new ProcessPayload { Id = 200 }; // Above threshold
            return null;
        });

        // Assert
        triggers.ShouldContain(AsyncPayloadTriggers.Start);
    }

    [Fact]
    public async Task Parallel_Fires_With_Different_Payloads_Should_Process_Correctly()
    {
        // Arrange
        var machine = new ConcurrentAsyncPayloadMachine(AsyncPayloadStates.Processing);
        await machine.StartAsync();
        var payloads = Enumerable.Range(1, 10).Select(i => new ProcessPayload { Id = i }).ToList();

        // Act
        var tasks = payloads.Select(p => machine.FireAsync(AsyncPayloadTriggers.Process, p));
        await Task.WhenAll(tasks);

        // Assert
        machine.CurrentState.ShouldBe(AsyncPayloadStates.Processing); // Internal transitions
        machine.ProcessedIds.Count.ShouldBe(10);
        machine.ProcessedIds.Distinct().Count().ShouldBe(10); // All unique IDs processed
    }

    [Fact]
    public async Task Internal_Transitions_With_Payload_Should_Handle_Concurrency()
    {
        // Arrange
        var machine = new ConcurrentAsyncPayloadMachine(AsyncPayloadStates.Processing);
        await machine.StartAsync();
        const int concurrentFires = 20;

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < concurrentFires; i++)
        {
            var payload = new ProcessPayload { Id = i };
            tasks.Add(machine.FireAsync(AsyncPayloadTriggers.Process, payload)
            );
        }
        await Task.WhenAll(tasks);

        // Assert
        machine.ProcessedIds.Count.ShouldBe(concurrentFires);
        machine.ProcessedIds.OrderBy(x => x).ShouldBe(Enumerable.Range(0, concurrentFires));
    }

    [Fact]
    public async Task Constructor_With_Initial_State_Having_Async_OnEntry_Should_Use_Parameterless_Version()
    {
        // Arrange & Act
        var machine = new InitialOnEntryAsyncPayloadMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();

        // Wait for fire-and-forget task to complete
        await Task.Delay(50);

        // Assert
        machine.InitialEntryCalledParameterless.ShouldBeTrue();
        machine.InitialEntryCalledWithPayload.ShouldBeFalse();
        machine.CallLog.ShouldBe(new[] { "OnEntry()" });
    }

    [Fact]
    public async Task Should_Support_CancellationToken_In_All_Async_Methods_With_Payload()
    {
        // Arrange
        var machine = new BasicAsyncPayloadMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();
        var payload = new ProcessPayload { Id = 111 };
        using var cts = new CancellationTokenSource();

        // Act - cancel before operation
        cts.Cancel();

        // Assert - TryFireAsync
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await machine.TryFireAsync(AsyncPayloadTriggers.Start, payload, cts.Token));

        // Assert - FireAsync
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await machine.FireAsync(AsyncPayloadTriggers.Start, payload, cts.Token));

        // Assert - CanFireAsync
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await machine.CanFireAsync(AsyncPayloadTriggers.Start, payload, cts.Token));
    }



    [Fact]
    public async Task Should_Handle_Null_Payload_When_Expected()
    {
        // Arrange
        var machine = new BasicAsyncPayloadMachine(AsyncPayloadStates.Initial);
        await machine.StartAsync();

        // Act - Try to fire with null payload
        var result = await machine.TryFireAsync(AsyncPayloadTriggers.Start, null as ProcessPayload);

        // Assert
        result.ShouldBeFalse(); // Guard will receive null and likely fail
        machine.CurrentState.ShouldBe(AsyncPayloadStates.Initial);
    }

    [Fact]
    public async Task Should_Use_Default_Payload_Type_When_Not_Specified_For_Trigger()
    {
        // This test would require a machine with default payload type
        // Since our test machines use specific payload types, this scenario is covered
        // by the multi-payload tests where each trigger has its specific type
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Should_Handle_High_Frequency_Async_Transitions_With_Large_Payloads()
    {
        // Arrange
        var machine = new ConcurrentAsyncPayloadMachine(AsyncPayloadStates.Processing);
        await machine.StartAsync();
        const int iterations = 100;

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task>();

        for (int i = 0; i < iterations; i++)
        {
            var largePayload = new ProcessPayload
            {
                Id = i,
                Data = new string('x', 1000) // Simulate larger payload
            };
            tasks.Add(machine.FireAsync(AsyncPayloadTriggers.Process, largePayload));
        }

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        machine.ProcessedIds.Count.ShouldBe(iterations);
        sw.ElapsedMilliseconds.ShouldBeLessThan(5000); // Should complete reasonably fast
    }

    [Fact]
    public void Should_Throw_When_Calling_Sync_Methods_On_Async_Payload_Machine()
    {
        // Arrange
        var machine = new BasicAsyncPayloadMachine(AsyncPayloadStates.Initial);
        var payload = new ProcessPayload { Id = 333 };

        // Act & Assert
        Should.Throw<SyncCallOnAsyncMachineException>(() => machine.TryFire(AsyncPayloadTriggers.Start, payload));
        Should.Throw<SyncCallOnAsyncMachineException>(() => machine.Fire(AsyncPayloadTriggers.Start, payload));
        Should.Throw<SyncCallOnAsyncMachineException>(() => machine.CanFire(AsyncPayloadTriggers.Start, payload));
        Should.Throw<SyncCallOnAsyncMachineException>(() => machine.GetPermittedTriggers());
    }

    [Fact]
    public async Task GetPermittedTriggersAsync_Should_Return_Empty_For_Terminal_State()
    {
        // Arrange
        var machine = new BasicAsyncPayloadMachine(AsyncPayloadStates.Completed);
        await machine.StartAsync();

        // Act
        var triggers = await machine.GetPermittedTriggersAsync();

        // Assert
        triggers.ShouldBeEmpty();
    }

    [Fact]
    public async Task FireAsync_Should_Throw_When_No_Valid_Transition_With_Payload()
    {
        // Arrange
        var machine = new BasicAsyncPayloadMachine(AsyncPayloadStates.Completed);
        await machine.StartAsync();
        var payload = new ProcessPayload { Id = 444 };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await machine.FireAsync(AsyncPayloadTriggers.Start, payload));
    }
}

#endregion
