//using Abstractions.Attributes;
//using StateMachine.Contracts;

//namespace Experiments
//{
//    [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
//    [PayloadType(OrderTrigger.Process, typeof(FullVariantExtendedTests.OrderPayload))]
//    [PayloadType(OrderTrigger.Pay, typeof(FullVariantExtendedTests.PaymentPayload))]
//    [PayloadType(OrderTrigger.Ship, typeof(FullVariantExtendedTests.ShippingPayload))]
//    [GenerationMode(GenerationMode.Full, Force = true)]
//    public partial class FullMultiPayloadMachine
//    {
//        // === POPRAWIONY KONSTRUKTOR ===
//        public FullMultiPayloadMachine(OrderState initialState, IEnumerable<IStateMachineExtension>? extensions)
//            : base(initialState) 
//        {
//            FileLogger.EnterMethod();
//            FileLogger.Log($"Custom constructor called with initialState: {initialState}, extensions count: {extensions?.Count() ?? 0}");
//            // Ciało może być puste. Generator wstrzyknie swoją logikę.
//            // Ważne jest, aby wywołanie : base(initialState) istniało.
//            FileLogger.ExitMethod();
//        }
//        // ============================

//        // ... reszta klasy bez zmian ...
//        [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing, Action = nameof(HandleOrder))]
//        [Transition(OrderState.Processing, OrderTrigger.Pay, OrderState.Paid, Action = nameof(HandlePayment))]
//        [Transition(OrderState.Paid, OrderTrigger.Ship, OrderState.Shipped, Action = nameof(HandleShipping))]
//        private void Configure() { }

//        private void HandleOrder(FullVariantExtendedTests.OrderPayload order)
//        {
//            FileLogger.EnterMethod();
//            FileLogger.Log($"Handling order: {order.OrderId}");
//            FileLogger.ExitMethod();
//        }

//        private void HandlePayment(FullVariantExtendedTests.PaymentPayload payment)
//        {
//            FileLogger.EnterMethod();
//            FileLogger.Log($"Handling payment for order: {payment.OrderId}");
//            FileLogger.ExitMethod();
//        }

//        private void HandleShipping(FullVariantExtendedTests.ShippingPayload shipping)
//        {
//            FileLogger.EnterMethod();
//            FileLogger.Log($"Handling shipping for order: {shipping.OrderId}");
//            FileLogger.ExitMethod();
//        }
//    }
//}