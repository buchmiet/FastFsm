using Abstractions.Attributes;

// FSM with payload support
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(typeof(OrderData))]  // Default payload
public partial class OrderMachine 
{
    // States with callbacks expecting payload
    [State(OrderState.New, OnEntry = "OnNewOrder")]
    [State(OrderState.Processing, OnEntry = "OnProcessing", OnExit = "OnLeavingProcessing")]
    [State(OrderState.Shipped)]
    [State(OrderState.Delivered)]
    [State(OrderState.Cancelled)]
    private void ConfigureStates() { }
    
    // Transitions with guards that use payload
    [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing, Guard = "CanProcess")]
    [Transition(OrderState.Processing, OrderTrigger.Ship, OrderState.Shipped)]
    [Transition(OrderState.Shipped, OrderTrigger.Deliver, OrderState.Delivered)]
    [Transition(OrderState.New, OrderTrigger.Cancel, OrderState.Cancelled)]
    [Transition(OrderState.Processing, OrderTrigger.Cancel, OrderState.Cancelled, Guard = "CanCancel")]
    private void ConfigureTransitions() { }
    
    // Per-trigger payload types
    [PayloadType(OrderTrigger.Process, typeof(ProcessingInfo))]
    [PayloadType(OrderTrigger.Ship, typeof(ShippingInfo))]
    private void ConfigurePayloads() { }
    
    // Callbacks with payload
    private void OnNewOrder(OrderData data) 
    {
        // Handle new order with data
    }
    
    private void OnProcessing(OrderData data) 
    {
        // Start processing
    }
    
    private void OnLeavingProcessing() 
    {
        // Cleanup processing
    }
    
    // Guards with payload
    private bool CanProcess(OrderData data) 
    {
        return data.Amount > 0;
    }
    
    private bool CanCancel(OrderData data) 
    {
        return !data.IsPriority;
    }
}

public enum OrderState 
{ 
    New,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

public enum OrderTrigger 
{ 
    Process,
    Ship,
    Deliver,
    Cancel
}

// Payload types
public class OrderData 
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public bool IsPriority { get; set; }
}

public class ProcessingInfo 
{
    public string Warehouse { get; set; }
}

public class ShippingInfo 
{
    public string TrackingNumber { get; set; }
    public string Carrier { get; set; }
}