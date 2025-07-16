//using Abstractions.Attributes;
//using StateMachine.Contracts;

//namespace Experiments
//{
//    public class PayloadTypeTracker : IStateMachineExtension
//    {
//        public HashSet<Type> ObservedTypes { get; } = new();

//        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
//        {
//            FileLogger.EnterMethod();
//            FileLogger.Log($"Context type: {typeof(TContext).Name}");
//            if (context is IStateMachineContext<OrderState, OrderTrigger> orderContext &&
//                orderContext.Payload != null)
//            {
//                var payloadType = orderContext.Payload.GetType();
//                FileLogger.Log($"Observed payload type: {payloadType.Name}");
//                ObservedTypes.Add(payloadType);
//            }
//            FileLogger.ExitMethod();
//        }

//        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
//        {
//            FileLogger.EnterMethod();
//            FileLogger.Log($"Success: {success}");
//            FileLogger.ExitMethod();
//        }
//        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
//        {
//            FileLogger.EnterMethod();
//            FileLogger.Log($"Guard: {guardName}");
//            FileLogger.ExitMethod();
//        }
//        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
//        {
//            FileLogger.EnterMethod();
//            FileLogger.Log($"Guard: {guardName}, Result: {result}");
//            FileLogger.ExitMethod();
//        }
//    }
//    public partial class FullOrderMachine
//    {
//        public decimal TotalProcessed { get; private set; }
//        public List<int> ProcessedOrderIds { get; } = new();

//        [State(OrderState.New, OnEntry = nameof(OnEnterNew))]
//        [State(OrderState.Processing, OnEntry = nameof(OnEnterProcessing))]
//        [State(OrderState.Paid, OnEntry = nameof(OnEnterPaid))]
//        private void ConfigureStates() { }

//        [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing,
//            Guard = nameof(CanProcess), Action = nameof(ProcessOrder))]
//        [Transition(OrderState.Processing, OrderTrigger.Pay, OrderState.Paid,
//            Action = nameof(RecordPayment))]
//        [Transition(OrderState.Processing, OrderTrigger.Cancel, OrderState.Cancelled)]
//        [Transition(OrderState.Paid, OrderTrigger.Ship, OrderState.Shipped,
//            Guard = nameof(CanShip))]
//        private void Configure() { }

//        private bool CanProcess(FullVariantExtendedTests.OrderPayload order) => order.Amount > 0;

//        private void ProcessOrder(FullVariantExtendedTests.OrderPayload order)
//        {
//            ProcessedOrderIds.Add(order.OrderId);
//            TotalProcessed += order.Amount;
//        }

//        private void RecordPayment(FullVariantExtendedTests.OrderPayload order)
//        {
//            // Payment processing logic
//        }

//        private bool CanShip(FullVariantExtendedTests.OrderPayload order) => !string.IsNullOrEmpty(order.TrackingNumber);

//        private void OnEnterNew() { }
//        private void OnEnterProcessing(FullVariantExtendedTests.OrderPayload order) { }
//        private void OnEnterPaid(FullVariantExtendedTests.OrderPayload order) { }
//    }
//    public enum OrderState { New, Processing, Paid, Shipped, Delivered, Cancelled }
//    public enum OrderTrigger { Process, Pay, Ship, Deliver, Cancel, Refund }

//}