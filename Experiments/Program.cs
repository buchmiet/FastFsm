using Abstractions.Attributes;
using System.Reflection.PortableExecutable;
using StateMachine.Contracts;

namespace Experiments;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Testing StateMachine Generator...");
        var withGuardMachine = new WithGuardBenchmarkMachine(BenchmarkState.A);
        var otherMach = new FullOrderMachine(OrderState.New);
        otherMach.TryFire(OrderTrigger.Process, new OrderPayload { OrderId = 1, Amount = 100 });
        withGuardMachine.TryFire(BenchmarkTrigger.Next);
    }
}
[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
[GenerationMode(GenerationMode.Pure, Force = true)]
public partial class WithGuardBenchmarkMachine
{
    private int _counter;

    [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B, Guard = nameof(CanTransition))]
    [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.A, Guard = nameof(CanTransition))]
    private void Configure() { }

    private bool CanTransition()
    {
        _counter++;
        return _counter % 2 == 0; // Simple condition
    }
}

public enum BenchmarkState { A, B, C, D }
public enum BenchmarkTrigger { Previous, Next }
public class OverloadPayload
{
    public string Data { get; set; }
}

[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(typeof(OrderPayload))]
[GenerationMode(GenerationMode.Full, Force = true)]



public partial class FullOrderMachine
{
    public decimal TotalProcessed { get; private set; }
    public List<int> ProcessedOrderIds { get; } = new();

    [State(OrderState.New, OnEntry = nameof(OnEnterNew))]
    [State(OrderState.Processing, OnEntry = nameof(OnEnterProcessing))]
    [State(OrderState.Paid, OnEntry = nameof(OnEnterPaid))]
    private void ConfigureStates() { }

    [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing,
        Guard = nameof(CanProcess), Action = nameof(ProcessOrder))]
    [Transition(OrderState.Processing, OrderTrigger.Pay, OrderState.Paid,
        Action = nameof(RecordPayment))]
    [Transition(OrderState.Processing, OrderTrigger.Cancel, OrderState.Cancelled)]
    [Transition(OrderState.Paid, OrderTrigger.Ship, OrderState.Shipped,
        Guard = nameof(CanShip))]
    private void Configure() { }

    private bool CanProcess(OrderPayload order) => order.Amount > 0;

    private void ProcessOrder(OrderPayload order)
    {
        ProcessedOrderIds.Add(order.OrderId);
        TotalProcessed += order.Amount;
    }

    private void RecordPayment(OrderPayload order)
    {
        // Payment processing logic
    }

    private bool CanShip(OrderPayload order) => !string.IsNullOrEmpty(order.TrackingNumber);

    private void OnEnterNew() { }
    private void OnEnterProcessing(OrderPayload order) { }
    private void OnEnterPaid(OrderPayload order) { }
}
public enum OrderState { New, Processing, Paid, Shipped, Delivered, Cancelled }
public enum OrderTrigger { Process, Pay, Ship, Deliver, Cancel, Refund }
public class OrderPayload
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? TrackingNumber { get; set; }
}

public class BehaviorModifyingExtension : IStateMachineExtension
{
    public HashSet<int> BlockedOrderIds { get; } = new();
    public Dictionary<int, DateTime> BlockedAttempts { get; } = new();

    public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
    {
        if (context is IStateMachineContext<OrderState, OrderTrigger> orderContext &&
            orderContext.Trigger == OrderTrigger.Ship &&
            orderContext.Payload is OrderPayload order &&
            BlockedOrderIds.Contains(order.OrderId))
        {
            BlockedAttempts[order.OrderId] = DateTime.Now;
        }
    }

    public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
    public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
    public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
}