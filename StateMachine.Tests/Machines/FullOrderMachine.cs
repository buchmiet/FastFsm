using System.Collections.Generic;
using Abstractions.Attributes;
using StateMachine.Tests.FullVariant;

namespace StateMachine.Tests.Machines
{
    // Full variant machine with single payload type and extensions
    [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
    [PayloadType(typeof(FullVariantExtendedTests.OrderPayload))]
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

        private bool CanProcess(FullVariantExtendedTests.OrderPayload order) => order.Amount > 0;

        private void ProcessOrder(FullVariantExtendedTests.OrderPayload order)
        {
            ProcessedOrderIds.Add(order.OrderId);
            TotalProcessed += order.Amount;
        }

        private void RecordPayment(FullVariantExtendedTests.OrderPayload order)
        {
            // Payment processing logic
        }

        private bool CanShip(FullVariantExtendedTests.OrderPayload order) => !string.IsNullOrEmpty(order.TrackingNumber);

        private void OnEnterNew() { }
        private void OnEnterProcessing(FullVariantExtendedTests.OrderPayload order) { }
        private void OnEnterPaid(FullVariantExtendedTests.OrderPayload order) { }
    }
    public enum OrderState { New, Processing, Paid, Shipped, Delivered, Cancelled }
    public enum OrderTrigger { Process, Pay, Ship, Deliver, Cancel, Refund }
}
