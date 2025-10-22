using Machines.Tests.Payloads;

namespace Machines.Tests.Machines.Legacy;



[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(typeof(OrderData))]
public partial class OrderStateMachine
{
    public int LastProcessedOrderId { get; private set; }
    public decimal LastProcessedAmount { get; private set; }

    [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted,
        Action = (ProcessSubmission))]
    private void Configure() { }

    private void ProcessSubmission(OrderData order)
    {
        LastProcessedOrderId = order.OrderId;
        LastProcessedAmount = order.Amount;
    }
}