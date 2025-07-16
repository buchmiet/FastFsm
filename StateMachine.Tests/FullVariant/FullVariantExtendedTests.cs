using Shouldly;
using StateMachine.Contracts;
using StateMachine.Tests.Machines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StateMachine.Tests.FullVariant
{


    public class FullVariantExtendedTests(ITestOutputHelper output)
    {
        public class OrderPayload
        {
            public int OrderId { get; set; }
            public decimal Amount { get; set; }
            public string? TrackingNumber { get; set; }
        }

        public class PaymentPayload : OrderPayload
        {
            public string PaymentMethod { get; set; } = "";
            public DateTime PaymentDate { get; set; }
        }

        public class ShippingPayload : OrderPayload
        {
            public string Carrier { get; set; } = "";
            public DateTime EstimatedDelivery { get; set; }
        }

        private class AuditExtension : IStateMachineExtension
        {
            public List<AuditEntry> Entries { get; } = new();

            public class AuditEntry
            {
                public DateTime Timestamp { get; set; }
                public object FromState { get; set; } = null!;
                public object ToState { get; set; } = null!;
                public object? Trigger { get; set; }
                public Type? PayloadType { get; set; }
                public object? PayloadData { get; set; }
                public bool Success { get; set; }
            }

            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                // Capture state before
            }

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
            {
                if (context is IStateMachineContext<OrderState, OrderTrigger> orderContext &&
                    context is IStateSnapshot snapshot)
                {
                    Entries.Add(new AuditEntry
                    {
                        Timestamp = context.Timestamp,
                        FromState = snapshot.FromState,
                        ToState = snapshot.ToState,
                        Trigger = snapshot.Trigger,
                        PayloadType = orderContext.Payload?.GetType(),
                        PayloadData = orderContext.Payload,
                        Success = success
                    });
                }
            }

            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
        }

        [Fact]
        public void FullVariant_ComplexPayloadHierarchy_WorksCorrectly()
        {
            // Arrange
            var audit = new AuditExtension();
            var machine = new FullOrderMachine(OrderState.New, new[] { audit });

            var orderPayload = new OrderPayload { OrderId = 123, Amount = 99.99m };
            var paymentPayload = new PaymentPayload
            {
                OrderId = 123,
                Amount = 99.99m,
                PaymentMethod = "CreditCard",
                PaymentDate = DateTime.Now
            };

            // Act
            machine.TryFire(OrderTrigger.Process, orderPayload);
            machine.TryFire(OrderTrigger.Pay, paymentPayload);

            // Assert
            machine.CurrentState.ShouldBe(OrderState.Paid);

            audit.Entries.Count.ShouldBe(2);
            audit.Entries[0].PayloadType.ShouldBe(typeof(OrderPayload));
            audit.Entries[1].PayloadType.ShouldBe(typeof(PaymentPayload));

            var capturedPayment = audit.Entries[1].PayloadData as PaymentPayload;
            capturedPayment.ShouldNotBeNull();
            capturedPayment.PaymentMethod.ShouldBe("CreditCard");
        }

        [Fact]
        public void FullVariant_ConditionalPayloadProcessing_WithExtensions()
        {
            // Arrange
            var processingExtension = new ConditionalProcessingExtension();
            var machine = new FullOrderMachine(OrderState.New, new[] { processingExtension });

            // Act - Process high value order
            var highValueOrder = new OrderPayload { OrderId = 1, Amount = 10000m };
            machine.TryFire(OrderTrigger.Process, highValueOrder);

            // Reset to process another order
            machine = new FullOrderMachine(OrderState.New, new[] { processingExtension });

            // Process low value order
            var lowValueOrder = new OrderPayload { OrderId = 2, Amount = 10m };
            machine.TryFire(OrderTrigger.Process, lowValueOrder);

            // Assert
            processingExtension.HighValueOrders.ShouldContain(1);
            processingExtension.HighValueOrders.ShouldNotContain(2);
        }

        [Fact]
        public void FullVariant_ExtensionModifyingBehavior_ThroughContext()
        {
            // Arrange
            var behaviorExtension = new BehaviorModifyingExtension();
            var machine = new FullOrderMachine(OrderState.Paid, new[] { behaviorExtension });

            // Configure extension to block shipping for certain orders
            behaviorExtension.BlockedOrderIds.Add(999);

            // Act & Assert - Normal order ships successfully
            var normalOrder = new OrderPayload { OrderId = 123, TrackingNumber = "TRACK123" };
            var shipped = machine.TryFire(OrderTrigger.Ship, normalOrder);
            shipped.ShouldBeTrue();

            // Reset state
            machine = new FullOrderMachine(OrderState.Paid, new[] { behaviorExtension });

            // Blocked order should fail (but extension still records the attempt)
            var blockedOrder = new OrderPayload { OrderId = 999, TrackingNumber = "TRACK999" };
            machine.TryFire(OrderTrigger.Ship, blockedOrder);

            // Extension recorded the attempt
            behaviorExtension.BlockedAttempts.ContainsKey(999).ShouldBeTrue();
        }

        [Fact]
        public void SmokeCheck_GeneratedMachine_Surface()
        {
            var t = typeof(FullMultiPayloadMachine);

            // --- Konstruktory ---
            var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            output.WriteLine("Konstruktory:");
            foreach (var c in ctors)
                output.WriteLine("  • " + c);

            // --- Pola prywatne ---
            var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            output.WriteLine("\nPola (non-public):");
            foreach (var f in fields)
                output.WriteLine($"  • {f.FieldType.Name} {f.Name}");

            // --- Metody publiczne ---
            var pubs = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            output.WriteLine("\nMetody publiczne:");
            foreach (var m in pubs)
                output.WriteLine("  • " + m);

            // --- Szybka asercja, że jest dokładnie JEDNA metoda TryFire<TPayload> ---
            Assert.Single(pubs.Where(m => m.Name == "TryFire" && m.IsGenericMethod));
        }


        [Fact]
        public void PayloadMap_ShouldPointTo_CompileTimeTypes()
        {
            // 1. Utwórz instancję (konstruktor NIE wywołuje TryFire)
            var machine = new FullMultiPayloadMachine(OrderState.New, extensions: null);

            // 2. Wyciągnij prywatne, statyczne pole _payloadMap
            var field = typeof(FullMultiPayloadMachine)
                .GetField("_payloadMap",
                    BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(field);                                  // pole istnieje
            var map = (Dictionary<OrderTrigger, Type>)field!.GetValue(null)!;

            // 3. Wypisz co siedzi w mapie
            foreach (var (trigger, type) in map)
            {
                output.WriteLine(
                    $"{trigger,-7} → {type.FullName}  (asm: {type.Assembly.GetName().Name})");
            }

            // 4. Assercje: czy to *te same* obiekty Type?
            Assert.Same(typeof(OrderPayload), map[OrderTrigger.Process]);
            Assert.Same(typeof(PaymentPayload), map[OrderTrigger.Pay]);
            Assert.Same(typeof(ShippingPayload), map[OrderTrigger.Ship]);

            // 5. (opcjonalnie) szybki runtime-check bez TryFire:
            var ok = map[OrderTrigger.Process].IsInstanceOfType(
                new OrderPayload { OrderId = 42 });
            Assert.True(ok);  // powinno być true dla „pasującego” payloadu
        }

        [Fact]
        public void FullVariant_MultiplePayloadTypes_SingleTransition()
        {
            // Arrange
            var typeTracker = new PayloadTypeTracker();
            // Upewnij się, że maszyna ma konstruktor przyjmujący rozszerzenia
            var machine = new FullMultiPayloadMachine(OrderState.New, new[] { typeTracker });

            //// === Krok 1: Tylko pierwsze przejście ===
            output.WriteLine("--- Krok 1: Przejście Process -> Processing ---");

            var processResult = machine.TryFire(OrderTrigger.Process, new OrderPayload { OrderId = 1 });
            Assert.True(processResult, "Przejście Process -> Processing nie powiodło się.");
            Assert.Contains(typeof(OrderPayload), typeTracker.ObservedTypes);
            Assert.Single(typeTracker.ObservedTypes);
            output.WriteLine("Sukces kroku 1. Obserwowane typy: " + string.Join(", ", typeTracker.ObservedTypes.Select(t => t.Name)));


           //  === Krok 2: Drugie przejście ===
            output.WriteLine("\n--- Krok 2: Przejście Processing -> Paid ---");
            var payResult = machine.TryFire(OrderTrigger.Pay, new PaymentPayload { OrderId = 1, PaymentMethod = "PayPal" });
            Assert.True(payResult, "Przejście Processing -> Paid nie powiodło się.");
            Assert.Contains(typeof(PaymentPayload), typeTracker.ObservedTypes);
            Assert.Equal(2, typeTracker.ObservedTypes.Count);
            output.WriteLine("Sukces kroku 2. Obserwowane typy: " + string.Join(", ", typeTracker.ObservedTypes.Select(t => t.Name)));


            // === Krok 3: Trzecie przejście ===
            output.WriteLine("\n--- Krok 3: Przejście Paid -> Shipped ---");
            var shipResult = machine.TryFire(OrderTrigger.Ship, new ShippingPayload { OrderId = 1, Carrier = "FedEx" });
            Assert.True(shipResult, "Przejście Paid -> Shipped nie powiodło się.");
            Assert.Contains(typeof(ShippingPayload), typeTracker.ObservedTypes);
            Assert.Equal(3, typeTracker.ObservedTypes.Count);
            output.WriteLine("Sukces kroku 3. Obserwowane typy: " + string.Join(", ", typeTracker.ObservedTypes.Select(t => t.Name)));
        }

      
        [Fact]
        public void FullVariant_GuardAndActionReceivePayload_ExtensionsObserve()
        {
            // Arrange
            var observerExtension = new PayloadObserverExtension();
            var machine = new FullOrderMachine(OrderState.New, new[] { observerExtension });

            var order = new OrderPayload
            {
                OrderId = 456,
                Amount = 250.50m,
                TrackingNumber = "SHIP123"
            };

            // Act
            var processed = machine.TryFire(OrderTrigger.Process, order);

            // Assert
            processed.ShouldBeTrue();
            machine.ProcessedOrderIds.ShouldContain(456);
            machine.TotalProcessed.ShouldBe(250.50m);

            // Extension saw the payload
            observerExtension.ObservedPayloads.Count.ShouldBe(1);
            var observedOrder = observerExtension.ObservedPayloads[0] as OrderPayload;
            observedOrder.ShouldNotBeNull();
            observedOrder.OrderId.ShouldBe(456);
        }

    

        private class ConditionalProcessingExtension : IStateMachineExtension
        {
            public HashSet<int> HighValueOrders { get; } = new();

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
            {
                if (success && context is IStateMachineContext<OrderState, OrderTrigger> orderContext)
                {
                    if (orderContext.Payload is OrderPayload order && order.Amount > 1000)
                    {
                        HighValueOrders.Add(order.OrderId);
                    }
                }
            }

            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext { }
            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
        }

        private class BehaviorModifyingExtension : IStateMachineExtension
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

        private class PayloadTypeTracker : IStateMachineExtension
        {
            public HashSet<Type> ObservedTypes { get; } = new();

            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                if (context is IStateMachineContext<OrderState, OrderTrigger> orderContext &&
                    orderContext.Payload != null)
                {
                    ObservedTypes.Add(orderContext.Payload.GetType());
                }
            }

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
        }

        private class PayloadObserverExtension : IStateMachineExtension
        {
            public List<object> ObservedPayloads { get; } = new();

            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                if (context is IStateMachineContext<OrderState, OrderTrigger> orderContext &&
                    orderContext.Payload != null)
                {
                    ObservedPayloads.Add(orderContext.Payload);
                }
            }

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
        }
    }

    // Enum definitions
  
}