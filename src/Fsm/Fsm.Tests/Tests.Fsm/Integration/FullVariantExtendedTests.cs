using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastFsm.Contracts;
using Tests.Machines.Extensions;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Tests.Machines.Payloads;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Fsm.Integration
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

            // --- Constructors ---
            var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            output.WriteLine("Constructors:");
            foreach (var c in ctors)
                output.WriteLine("  • " + c);

            // --- Private fields ---
            var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            output.WriteLine("\nFields (non-public):");
            foreach (var f in fields)
                output.WriteLine($"  • {f.FieldType.Name} {f.Name}");

            // --- Public methods ---
            var pubs = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            output.WriteLine("\nPublic methods:");
            foreach (var m in pubs)
                output.WriteLine("  • " + m);

            // --- Quick assertion that there is exactly ONE TryFire<TPayload> method ---
            Assert.Single(pubs, m => m.Name == "TryFire" && m.IsGenericMethod);
        }


        [Fact]
        public void PayloadMap_ShouldPointTo_CompileTimeTypes()
        {
            // 1. Create an instance (the constructor does NOT call TryFire)
            var machine = new FullMultiPayloadMachine(PhysicalOrderState.New, extensions: null);
            machine.Start();

            // 2. Extract the private static _payloadMap field
            var field = typeof(FullMultiPayloadMachine)
                .GetField("_payloadMap",
                    BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(field);                                  // field exists
            var map = (Dictionary<PhysicalOrderTrigger, Type>)field!.GetValue(null)!;

            // 3. Print what is in the map
            foreach (var (trigger, type) in map)
            {
                output.WriteLine(
                    $"{trigger,-7} → {type.FullName}  (asm: {type.Assembly.GetName().Name})");
            }

            // 4. Assertions: are these the *same* Type objects?
            Assert.Same(typeof(OrderPayload), map[PhysicalOrderTrigger.Process]);
            Assert.Same(typeof(PaymentPayload), map[PhysicalOrderTrigger.Pay]);
            Assert.Same(typeof(ShippingPayload), map[PhysicalOrderTrigger.Ship]);

            // 5. (optional) quick runtime check without TryFire:
            var ok = map[PhysicalOrderTrigger.Process].IsInstanceOfType(
                new OrderPayload { OrderId = 42 });
            Assert.True(ok);  // should be true for a matching payload
        }

        [Fact]
        public void AllFeatures_MultiplePayloadTypes_SingleTransition()
        {
            // Arrange
            var typeTracker = new PayloadTypeTracker();
            // Make sure the machine has a constructor that accepts extensions
            var machine = new FullMultiPayloadMachine(PhysicalOrderState.New, new[] { typeTracker });
            machine.Start();

            //// === Step 1: First transition only ===
            output.WriteLine("---" + " Step 1: Transition Process -> Processing ---");

            var processResult = machine.TryFire(PhysicalOrderTrigger.Process, new OrderPayload { OrderId = 1 });
            Assert.True(processResult, "Transition Process -> Processing failed.");
            Assert.Contains(typeof(OrderPayload), typeTracker.ObservedTypes);
            Assert.Single(typeTracker.ObservedTypes);
            output.WriteLine("Step 1 succeeded. Observed types: " + string.Join(", ", typeTracker.ObservedTypes.Select(t => t.Name)));


            //  === Step 2: Second transition ===
            output.WriteLine("\n--- Step 2: Transition Processing -> Paid ---");
            var payResult = machine.TryFire(PhysicalOrderTrigger.Pay, new PaymentPayload { OrderId = 1, PaymentMethod = "PayPal" });
            Assert.True(payResult, "Transition Processing -> Paid failed.");
            Assert.Contains(typeof(PaymentPayload), typeTracker.ObservedTypes);
            Assert.Equal(2, typeTracker.ObservedTypes.Count);
            output.WriteLine("Step 2 succeeded. Observed types: " + string.Join(", ", typeTracker.ObservedTypes.Select(t => t.Name)));


            // === Step 3: Third transition ===
            output.WriteLine("\n--- Step 3: Transition Paid -> Shipped ---");
            var shipResult = machine.TryFire(PhysicalOrderTrigger.Ship, new ShippingPayload { OrderId = 1, Carrier = "FedEx" });
            Assert.True(shipResult, "Transition Paid -> Shipped failed.");
            Assert.Contains(typeof(ShippingPayload), typeTracker.ObservedTypes);
            Assert.Equal(3, typeTracker.ObservedTypes.Count);
            output.WriteLine("Step 3 succeeded. Observed types: " + string.Join(", ", typeTracker.ObservedTypes.Select(t => t.Name)));
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

        private class ConditionalProcessingExtension : IStateMachineExtension<PhysicalOrderState, PhysicalOrderTrigger>
        {
            public HashSet<int> HighValueOrders { get; } = new();

            public void OnAttemptCompleted(
                in TransitionAttemptContext<PhysicalOrderState, PhysicalOrderTrigger> attempt,
                in TransitionResult<PhysicalOrderState> result)
            {
                if (result.Outcome == TransitionOutcome.Succeeded &&
                    attempt.Payload is OrderPayload { Amount: > 1000 } order)
                    HighValueOrders.Add(order.OrderId);
            }
        }

        private class BehaviorModifyingExtension : IStateMachineExtension<PhysicalOrderState, PhysicalOrderTrigger>
        {
            public HashSet<int> BlockedOrderIds { get; } = new();
            public Dictionary<int, DateTime> BlockedAttempts { get; } = new();

            public void OnAttemptStarting(in TransitionAttemptContext<PhysicalOrderState, PhysicalOrderTrigger> attempt)
            {
                if (attempt.Trigger == PhysicalOrderTrigger.Ship &&
                    attempt.Payload is OrderPayload order &&
                    BlockedOrderIds.Contains(order.OrderId))
                    BlockedAttempts[order.OrderId] = DateTime.Now;
            }
        }

        private class PayloadTypeTracker : IStateMachineExtension<PhysicalOrderState, PhysicalOrderTrigger>
        {
            public HashSet<Type> ObservedTypes { get; } = new();

            public void OnAttemptStarting(in TransitionAttemptContext<PhysicalOrderState, PhysicalOrderTrigger> attempt)
            {
                if (attempt.Payload is not null) ObservedTypes.Add(attempt.Payload.GetType());
            }
        }

        private class PayloadObserverExtension : IStateMachineExtension<PhysicalOrderState, PhysicalOrderTrigger>
        {
            public List<object> ObservedPayloads { get; } = new();

            public void OnAttemptStarting(in TransitionAttemptContext<PhysicalOrderState, PhysicalOrderTrigger> attempt)
            {
                if (attempt.Payload is not null) ObservedPayloads.Add(attempt.Payload);
            }
        }
    }

    // Enum definitions

}
