//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using FastFsm.Abstractions;
//using Xunit;
//using Xunit.Abstractions;

//namespace StateMachine.Tests.HierarchicalStateMachine;

//[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
//public partial class AsyncHierarchicalMachine
//{
//    public List<string> ExecutionLog { get; } = new();

//    [State(HsmState.Root)]
//    void Root() { }

//    [State(HsmState.A, Parent = HsmState.Root, OnEntry = nameof(OnEntryAAsync), OnExit = nameof(OnExitAAsync))]
//    void A() { }

//    [State(HsmState.A1, Parent = HsmState.A, OnEntry = nameof(OnEntryA1Async), OnExit = nameof(OnExitA1Async))]
//    [InitialSubstate(HsmState.A, HsmState.A1)]
//    void A1() { }

//    [State(HsmState.A2, Parent = HsmState.A)]
//    void A2() { }

//    [State(HsmState.B, Parent = HsmState.Root, OnEntry = nameof(OnEntryBAsync))]
//    void B() { }

//    [Transition(HsmState.A, HsmTrigger.ToB, HsmState.B, Guard = nameof(CanTransitionAsync))]
//    async Task AToBAsync()
//    {
//        ExecutionLog.Add("Action-A-to-B-Start");
//        await Task.Delay(10);
//        ExecutionLog.Add("Action-A-to-B-End");
//    }

//    [Transition(HsmState.A1, HsmTrigger.Next, HsmState.A2)]
//    async Task A1ToA2Async()
//    {
//        ExecutionLog.Add("Action-A1-to-A2-Start");
//        await Task.Delay(10);
//        ExecutionLog.Add("Action-A1-to-A2-End");
//    }

//    async Task<bool> CanTransitionAsync()
//    {
//        ExecutionLog.Add("Guard-Check-Start");
//        await Task.Delay(10);
//        ExecutionLog.Add("Guard-Check-End");
//        return true;
//    }

//    async Task OnEntryAAsync()
//    {
//        ExecutionLog.Add("Entry-A-Start");
//        await Task.Delay(10);
//        ExecutionLog.Add("Entry-A-End");
//    }

//    async Task OnExitAAsync()
//    {
//        ExecutionLog.Add("Exit-A-Start");
//        await Task.Delay(10);
//        ExecutionLog.Add("Exit-A-End");
//    }

//    async Task OnEntryA1Async()
//    {
//        ExecutionLog.Add("Entry-A1-Start");
//        await Task.Delay(10);
//        ExecutionLog.Add("Entry-A1-End");
//    }

//    async Task OnExitA1Async()
//    {
//        ExecutionLog.Add("Exit-A1-Start");
//        await Task.Delay(10);
//        ExecutionLog.Add("Exit-A1-End");
//    }

//    async Task OnEntryBAsync()
//    {
//        ExecutionLog.Add("Entry-B-Start");
//        await Task.Delay(10);
//        ExecutionLog.Add("Entry-B-End");
//    }
//}

//[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
//public partial class CancellableHsmMachine
//{
//    public List<string> ExecutionLog { get; } = new();
//    public int DelayMs { get; set; } = 1000;

//    [State(HsmState.Root)]
//    void Root() { }

//    [State(HsmState.A, Parent = HsmState.Root, OnEntry = nameof(OnEntryAAsync), OnExit = nameof(OnExitAAsync))]
//    void A() { }

//    [State(HsmState.A1, Parent = HsmState.A, OnEntry = nameof(OnEntryA1Async))]
//    [InitialSubstate(HsmState.A, HsmState.A1)]
//    void A1() { }

//    [State(HsmState.B, Parent = HsmState.Root)]
//    void B() { }

//    [Transition(HsmState.A1, HsmTrigger.ToB, HsmState.B)]
//    async Task A1ToBAsync(CancellationToken ct)
//    {
//        ExecutionLog.Add("Action-Start");
//        try
//        {
//            await Task.Delay(DelayMs, ct);
//            ExecutionLog.Add("Action-Complete");
//        }
//        catch (OperationCanceledException)
//        {
//            ExecutionLog.Add("Action-Cancelled");
//            throw;
//        }
//    }

//    async Task OnEntryAAsync(CancellationToken ct)
//    {
//        ExecutionLog.Add("Entry-A-Start");
//        try
//        {
//            await Task.Delay(DelayMs, ct);
//            ExecutionLog.Add("Entry-A-Complete");
//        }
//        catch (OperationCanceledException)
//        {
//            ExecutionLog.Add("Entry-A-Cancelled");
//            throw;
//        }
//    }

//    async Task OnExitAAsync(CancellationToken ct)
//    {
//        ExecutionLog.Add("Exit-A-Start");
//        try
//        {
//            await Task.Delay(DelayMs, ct);
//            ExecutionLog.Add("Exit-A-Complete");
//        }
//        catch (OperationCanceledException)
//        {
//            ExecutionLog.Add("Exit-A-Cancelled");
//            throw;
//        }
//    }

//    async Task OnEntryA1Async(CancellationToken ct)
//    {
//        ExecutionLog.Add("Entry-A1-Start");
//        try
//        {
//            await Task.Delay(DelayMs, ct);
//            ExecutionLog.Add("Entry-A1-Complete");
//        }
//        catch (OperationCanceledException)
//        {
//            ExecutionLog.Add("Entry-A1-Cancelled");
//            throw;
//        }
//    }
//}

//public class HsmAsyncTests
//{
//    private readonly ITestOutputHelper _output;

//    public HsmAsyncTests(ITestOutputHelper output)
//    {
//        _output = output;
//    }

//    [Fact]
//    public async Task AsyncCallbacks_ExecuteInCorrectOrder()
//    {
//        // Arrange
//        var machine = new AsyncHierarchicalMachine(HsmState.A1);
//        machine.ExecutionLog.Clear();

//        // Act
//        await machine.FireAsync(HsmTrigger.ToB);

//        // Assert - Check execution order
//        var log = machine.ExecutionLog;
        
//        // Guard executes first
//        Assert.Contains("Guard-Check-Start", log);
//        Assert.Contains("Guard-Check-End", log);
        
//        // Then exit callbacks (child before parent)
//        Assert.Contains("Exit-A1-Start", log);
//        Assert.Contains("Exit-A1-End", log);
//        Assert.Contains("Exit-A-Start", log);
//        Assert.Contains("Exit-A-End", log);
        
//        // Then entry callbacks
//        Assert.Contains("Entry-B-Start", log);
//        Assert.Contains("Entry-B-End", log);
        
//        // Finally action
//        Assert.Contains("Action-A-to-B-Start", log);
//        Assert.Contains("Action-A-to-B-End", log);

//        // Verify order
//        var guardIndex = log.IndexOf("Guard-Check-End");
//        var exitA1Index = log.IndexOf("Exit-A1-Start");
//        var exitAIndex = log.IndexOf("Exit-A-Start");
//        var entryBIndex = log.IndexOf("Entry-B-Start");
//        var actionIndex = log.IndexOf("Action-A-to-B-Start");

//        Assert.True(guardIndex < exitA1Index, "Guard before exit");
//        Assert.True(exitA1Index < exitAIndex, "Child exit before parent exit");
//        Assert.True(exitAIndex < entryBIndex, "Exit before entry");
//        Assert.True(entryBIndex < actionIndex, "Entry before action");
//    }

//    [Fact]
//    public async Task CanFireAsync_ChecksInheritedTransitions()
//    {
//        // Arrange
//        var machine = new AsyncHierarchicalMachine(HsmState.A1);

//        // Act & Assert
//        // Can fire transition defined on parent
//        Assert.True(await machine.CanFireAsync(HsmTrigger.ToB));
        
//        // Can fire transition defined on self
//        Assert.True(await machine.CanFireAsync(HsmTrigger.Next));
        
//        // Cannot fire unrelated transition
//        Assert.False(await machine.CanFireAsync(HsmTrigger.ToC));
//    }

//    [Fact]
//    public async Task GetPermittedTriggersAsync_IncludesInherited()
//    {
//        // Arrange
//        var machine = new AsyncHierarchicalMachine(HsmState.A1);

//        // Act
//        var triggers = await machine.GetPermittedTriggersAsync();

//        // Assert
//        Assert.Contains(HsmTrigger.ToB, triggers);  // From parent A
//        Assert.Contains(HsmTrigger.Next, triggers); // From A1
//    }

//    [Fact]
//    public async Task CancellationToken_CancelsTransition()
//    {
//        // Arrange
//        var machine = new CancellableHsmMachine(HsmState.A1);
//        machine.DelayMs = 100;
//        machine.ExecutionLog.Clear();
        
//        using var cts = new CancellationTokenSource();

//        // Act
//        var fireTask = machine.FireAsync(HsmTrigger.ToB, cts.Token);
        
//        // Cancel after a short delay
//        await Task.Delay(25);
//        cts.Cancel();

//        // Assert
//        await Assert.ThrowsAsync<OperationCanceledException>(() => fireTask);
        
//        // Check that cancellation occurred during execution
//        var log = machine.ExecutionLog;
//        Assert.Contains("Exit-A1-Start", log);
        
//        // At least one operation should have been cancelled
//        Assert.True(
//            log.Contains("Exit-A1-Cancelled") ||
//            log.Contains("Exit-A-Cancelled") ||
//            log.Contains("Action-Cancelled"),
//            "At least one operation should have been cancelled"
//        );
        
//        // State should remain unchanged if cancelled during exit
//        if (log.Contains("Exit-A1-Cancelled") || log.Contains("Exit-A-Cancelled"))
//        {
//            Assert.Equal(HsmState.A1, machine.CurrentState);
//        }
//    }

//    [Fact]
//    public async Task GetActivePathAsync_WorksCorrectly()
//    {
//        // Arrange
//        var machine = new AsyncHierarchicalMachine(HsmState.A1);

//        // Act
//        var path = await machine.GetActivePathAsync();

//        // Assert
//        Assert.Equal(3, path.Count);
//        Assert.Equal(HsmState.Root, path[0]);
//        Assert.Equal(HsmState.A, path[1]);
//        Assert.Equal(HsmState.A1, path[2]);
//    }

//    [Fact]
//    public async Task AsyncTransitionWithinHierarchy_MaintainsPath()
//    {
//        // Arrange
//        var machine = new AsyncHierarchicalMachine(HsmState.A1);
//        machine.ExecutionLog.Clear();

//        // Act - Transition within same parent
//        await machine.FireAsync(HsmTrigger.Next);

//        // Assert
//        Assert.Equal(HsmState.A2, machine.CurrentState);
        
//        // Parent should not have exit/entry
//        Assert.DoesNotContain("Exit-A-Start", machine.ExecutionLog);
//        Assert.DoesNotContain("Entry-A-Start", machine.ExecutionLog);
        
//        // Only sibling transition
//        Assert.Contains("Exit-A1-Start", machine.ExecutionLog);
//        Assert.Contains("Action-A1-to-A2-Start", machine.ExecutionLog);
//    }
//}