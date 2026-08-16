using Abstractions.Attributes;

namespace ParserComparison.Tests;

// Attribute-based equivalent for comparison
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(OrderTrigger.Submit, typeof(OrderData))]
[PayloadType(OrderTrigger.Cancel, typeof(CancellationData))]
[PayloadType(OrderTrigger.Ship, typeof(ShippingData))]
[PayloadType(OrderTrigger.Update, typeof(OrderData))]
public partial class PayloadAttributeExampleMachine
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

    [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Processing,
        Guard = nameof(ValidateOrder), Action = nameof(ProcessOrder))]
    [Transition(OrderState.New, OrderTrigger.Cancel, OrderState.Cancelled,
        Action = nameof(CancelOrder))]
    [Transition(OrderState.Processing, OrderTrigger.Ship, OrderState.Shipped,
        Guard = nameof(CanShip), Action = nameof(RecordShipment))]
    [InternalTransition(OrderState.Processing, OrderTrigger.Update, nameof(UpdateOrder))]
    [Transition(OrderState.Shipped, OrderTrigger.Deliver, OrderState.Delivered,
        Action = nameof(CompleteDelivery))]
    private void ConfigureTransitions() { }

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