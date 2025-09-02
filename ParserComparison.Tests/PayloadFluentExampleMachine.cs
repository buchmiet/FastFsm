using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

// Example showing proper FluentAPI payload usage with .Payload() method
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
public partial class PayloadFluentExampleMachine
{
    public enum OrderState { New, Processing, Shipped, Delivered, Cancelled }
    public enum OrderTrigger { Submit, Ship, Deliver, Cancel, Update }

    // Payload types
    public sealed class OrderData
    {
        public required string OrderId { get; init; }
        public decimal Amount { get; init; }
    }

    public sealed class ShippingData
    {
        public required string TrackingNumber { get; init; }
        public required string Carrier { get; init; }
    }

    public sealed class CancellationData
    {
        public required string Reason { get; init; }
        public bool RefundRequested { get; init; }
    }

    private static void Configure() => FSM
        .State(OrderState.New)
            .On(OrderTrigger.Submit)
                .Payload<OrderData>()  // Specify payload type for this transition
                .Guard(nameof(ValidateOrder))
                .Action(nameof(ProcessOrder))
                .GoTo(OrderState.Processing)
            .On(OrderTrigger.Cancel)
                .Payload<CancellationData>()  // Different payload for cancel
                .Action(nameof(CancelOrder))
                .GoTo(OrderState.Cancelled)
        
        .State(OrderState.Processing)
            .On(OrderTrigger.Ship)
                .Payload<ShippingData>()  // Shipping payload
                .Guard(nameof(CanShip))
                .Action(nameof(RecordShipment))
                .GoTo(OrderState.Shipped)
            .On(OrderTrigger.Update)
                .Payload<OrderData>()  // Internal update with order data
                .Action(nameof(UpdateOrder))
                .Internal()  // Stay in same state
        
        .State(OrderState.Shipped)
            .On(OrderTrigger.Deliver)
                .Action(nameof(CompleteDelivery))  // No payload needed
                .GoTo(OrderState.Delivered)
        
        .State(OrderState.Delivered)
        .State(OrderState.Cancelled);

    // Guard methods with payload
    private bool ValidateOrder(OrderData order) => 
        !string.IsNullOrEmpty(order.OrderId) && order.Amount > 0;

    private bool CanShip(ShippingData shipping) =>
        !string.IsNullOrEmpty(shipping.TrackingNumber);

    // Action methods with payload
    private void ProcessOrder(OrderData order)
    {
        // Process the order
    }

    private void RecordShipment(ShippingData shipping)
    {
        // Record shipment details
    }

    private void CancelOrder(CancellationData cancellation)
    {
        // Handle cancellation
    }

    private void UpdateOrder(OrderData order)
    {
        // Update order details
    }

    private void CompleteDelivery()
    {
        // Mark as delivered
    }
}