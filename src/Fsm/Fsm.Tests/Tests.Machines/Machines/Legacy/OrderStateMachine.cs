using Tests.Machines.Payloads;
using Abstractions.Attributes;

namespace Tests.Machines.Machines.Legacy;



[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(typeof(OrderData))]
public partial class OrderStateMachine
{
    public int LastProcessedOrderId { get; private set; }
    public decimal LastProcessedAmount { get; private set; }

    [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted,
        Action = nameof(ProcessSubmission))]
    private void Configure() { }

    private void ProcessSubmission(OrderData order)
    {
        LastProcessedOrderId = order.OrderId;
        LastProcessedAmount = order.Amount;
    }
}
