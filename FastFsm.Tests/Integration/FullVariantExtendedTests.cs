using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastFsm.Contracts;
using Machines.Tests.Extensions;
using Machines.Tests.Machines;
using Machines.Tests.Machines.Legacy;
using Machines.Tests.Payloads;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace FastFsm.Tests.Integration
{


    public class AllFeaturesExtendedTests(ITestOutputHelper output)
    {
     





    

        [Fact]
        public void AllFeatures_ComplexPayloadHierarchy_WorksCorrectly()
        {
            // Arrange
            var audit = new AuditExtension();
            var machine = new FullOrderMachine(PhysicalOrderState.New, new[] { audit });
            machine.Start();

            var orderPayload = new OrderPayload { OrderId = 123, Amount = 99.99m };
            var paymentPayload = new PaymentPayload
            {
                OrderId = 123,
                Amount = 99.99m,
                PaymentMethod = "CreditCard",
                PaymentDate = DateTime.Now
            };

            // Act
            machine.TryFire(PhysicalOrderTrigger.Process, orderPayload);
            machine.TryFire(PhysicalOrderTrigger.Pay, paymentPayload);

            // Assert
            machine.CurrentState.ShouldBe(PhysicalOrderState.Paid);

            audit.Entries.Count.ShouldBe(2);
            audit.Entries[0].PayloadType.ShouldBe(typeof(OrderPayload));
            audit.Entries[1].PayloadType.ShouldBe(typeof(PaymentPayload));

            var capturedPayment = audit.Entries[1].PayloadData as PaymentPayload;
            capturedPayment.ShouldNotBeNull();
            capturedPayment.PaymentMethod.ShouldBe("CreditCard");
        }

        [Fact]
        public void AllFeatures_ConditionalPayloadProcessing_WithExtensions()
        {
            // Arrange
            var processingExtension = new ConditionalProcessingExtension();
            var machine = new FullOrderMachine(PhysicalOrderState.New, new[] { processingExtension });
            machine.Start();

            // Act - Process high value order
            var highValueOrder = new OrderPayload { OrderId = 1, Amount = 10000m };
            machine.TryFire(PhysicalOrderTrigger.Process, highValueOrder);

            // Reset to process another order
            machine = new FullOrderMachine(PhysicalOrderState.New, new[] { processingExtension });
            machine.Start();

            // Process low value order
            var lowValueOrder = new OrderPayload { OrderId = 2, Amount = 10m };
            machine.TryFire(PhysicalOrderTrigger.Process, lowValueOrder);

            // Assert
            processingExtension.HighValueOrders.ShouldContain(1);
            processingExtension.HighValueOrders.ShouldNotContain(2);
        }

        [Fact]
        public void AllFeatures_ExtensionModifyingBehavior_ThroughContext()
        {
            // Arrange
            var behaviorExtension = new BehaviorModifyingExtension();
            var machine = new FullOrderMachine(PhysicalOrderState.Paid, new[] { behaviorExtension });
            machine.Start();

            // Configure extension to block shipping for certain orders
            behaviorExtension.BlockedOrderIds.Add(999);

            // Act & Assert - Normal order ships successfully
            var normalOrder = new OrderPayload { OrderId = 123, TrackingNumber = "TRACK123" };
            var shipped = machine.TryFire(PhysicalOrderTrigger.Ship, normalOrder);
            shipped.ShouldBeTrue();

            // Reset state
            machine = new FullOrderMachine(PhysicalOrderState.Paid, new[] { behaviorExtension });
            machine.Start();

            // Blocked order should fail (but extension still records the attempt)
            var blockedOrder = new OrderPayload { OrderId = 999, TrackingNumber = "TRACK999" };
            machine.TryFire(PhysicalOrderTrigger.Ship, blockedOrder);

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
            Assert.Single(pubs, m => m.Name == "TryFire" && m.IsGenericMethod);
        }


        [Fact]
        public void PayloadMap_ShouldPointTo_CompileTimeTypes()
        {
            // 1. Utwórz instancję (konstruktor NIE wywołuje TryFire)
            var machine = new FullMultiPayloadMachine(PhysicalOrderState.New, extensions: null);
            machine.Start();

            // 2. Wyciągnij prywatne, statyczne pole _payloadMap
            var field = typeof(FullMultiPayloadMachine)
                .GetField("_payloadMap",
                    BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(field);                                  // pole istnieje
            var map = (Dictionary<PhysicalOrderTrigger, Type>)field!.GetValue(null)!;

            // 3. Wypisz co siedzi w mapie
            foreach (var (trigger, type) in map)
            {
                output.WriteLine(
                    $"{trigger,-7} → {type.FullName}  (asm: {type.Assembly.GetName().Name})");
            }

            // 4. Assercje: czy to *te same* obiekty Type?
            Assert.Same(typeof(OrderPayload), map[PhysicalOrderTrigger.Process]);
            Assert.Same(typeof(PaymentPayload), map[PhysicalOrderTrigger.Pay]);
            Assert.Same(typeof(ShippingPayload), map[PhysicalOrderTrigger.Ship]);

            // 5. (opcjonalnie) szybki runtime-check bez TryFire:
            var ok = map[PhysicalOrderTrigger.Process].IsInstanceOfType(
                new OrderPayload { OrderId = 42 });
            Assert.True(ok);  // powinno być true dla „pasującego” payloadu
        }

        [Fact]
        public void AllFeatures_MultiplePayloadTypes_SingleTransition()
        {
            // Arrange
            var typeTracker = new PayloadTypeTracker();
            // Upewnij się, że maszyna ma konstruktor przyjmujący rozszerzenia
            var machine = new FullMultiPayloadMachine(PhysicalOrderState.New, new[] { typeTracker });
            machine.Start();

            //// === Krok 1: Tylko pierwsze przejście ===
            output.WriteLine("---" + " Krok 1: Przejście Process -> Processing ---");

            var processResult = machine.TryFire(PhysicalOrderTrigger.Process, new OrderPayload { OrderId = 1 });
            Assert.True(processResult, "Przejście Process -> Processing nie powiodło się.");
            Assert.Contains(typeof(OrderPayload), typeTracker.ObservedTypes);
            Assert.Single(typeTracker.ObservedTypes);
            output.WriteLine("Sukces kroku 1. Obserwowane typy: " + string.Join(", ", typeTracker.ObservedTypes.Select(t => t.Name)));


            //  === Krok 2: Drugie przejście ===
            output.WriteLine("\n--- Krok 2: Przejście Processing -> Paid ---");
            var payResult = machine.TryFire(PhysicalOrderTrigger.Pay, new PaymentPayload { OrderId = 1, PaymentMethod = "PayPal" });
            Assert.True(payResult, "Przejście Processing -> Paid nie powiodło się.");
            Assert.Contains(typeof(PaymentPayload), typeTracker.ObservedTypes);
            Assert.Equal(2, typeTracker.ObservedTypes.Count);
            output.WriteLine("Sukces kroku 2. Obserwowane typy: " + string.Join(", ", typeTracker.ObservedTypes.Select(t => t.Name)));


            // === Krok 3: Trzecie przejście ===
            output.WriteLine("\n--- Krok 3: Przejście Paid -> Shipped ---");
            var shipResult = machine.TryFire(PhysicalOrderTrigger.Ship, new ShippingPayload { OrderId = 1, Carrier = "FedEx" });
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
            var machine = new FullOrderMachine(PhysicalOrderState.New, new[] { observerExtension });
            machine.Start();

            var order = new OrderPayload
            {
                OrderId = 456,
                Amount = 250.50m,
                TrackingNumber = "SHIP123"
            };

            // Act
            var processed = machine.TryFire(PhysicalOrderTrigger.Process, order);

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
                if (success && context is IStateMachineContext<PhysicalOrderState, PhysicalOrderTrigger> orderContext)
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
            public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new NotImplementedException();
            }

            public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new NotImplementedException();
            }

            public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext { }
        }

        private class BehaviorModifyingExtension : IStateMachineExtension
        {
            public HashSet<int> BlockedOrderIds { get; } = new();
            public Dictionary<int, DateTime> BlockedAttempts { get; } = new();

            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                if (context is IStateMachineContext<PhysicalOrderState, PhysicalOrderTrigger> orderContext &&
                    orderContext.Trigger == PhysicalOrderTrigger.Ship &&
                    orderContext.Payload is OrderPayload order &&
                    BlockedOrderIds.Contains(order.OrderId))
                {
                    BlockedAttempts[order.OrderId] = DateTime.Now;
                }
            }

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
            public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new NotImplementedException();
            }

            public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new NotImplementedException();
            }

            public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext { }
        }

        private class PayloadTypeTracker : IStateMachineExtension
        {
            public HashSet<Type> ObservedTypes { get; } = new();

            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                if (context is IStateMachineContext<PhysicalOrderState, PhysicalOrderTrigger> orderContext &&
                    orderContext.Payload != null)
                {
                    ObservedTypes.Add(orderContext.Payload.GetType());
                }
            }

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
            public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new NotImplementedException();
            }

            public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new NotImplementedException();
            }

            public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext { }
        }

        private class PayloadObserverExtension : IStateMachineExtension
        {
            public List<object> ObservedPayloads { get; } = new();

            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                if (context is IStateMachineContext<PhysicalOrderState, PhysicalOrderTrigger> orderContext &&
                    orderContext.Payload != null)
                {
                    ObservedPayloads.Add(orderContext.Payload);
                }
            }

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
            public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new NotImplementedException();
            }

            public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new NotImplementedException();
            }

            public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext { }
        }
    }

    // Enum definitions

}
