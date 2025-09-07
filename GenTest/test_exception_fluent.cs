using System;
using System.Collections.Generic;

namespace TestMachines;

// Test 1: Exception in OnEntry with Continue directive
public enum EDState_Fluent { A, B }
public enum EDTrigger_Fluent { Go }

[FastFsm.Attributes.FastFsm(typeof(EDState_Fluent), typeof(EDTrigger_Fluent))]
[FastFsm.Attributes.FastFsmOnException(nameof(Handle))]
public partial class ContinueOnEntryMachine_Fluent : FastFsm.LifecycleBase
{
    public List<string> Log { get; } = new();
    public bool ThrowOnEntryB { get; set; }

    protected override void Configure()
    {
        In(EDState_Fluent.A)
            .On(EDTrigger_Fluent.Go).GoTo(EDState_Fluent.B).Do(ActionAB);
        
        In(EDState_Fluent.B)
            .OnEntry(OnEntryB);
    }

    private void OnEntryB()
    {
        if (ThrowOnEntryB)
        {
            Log.Add("OnEntryB-THREW");
            throw new InvalidOperationException("transient");
        }
        Log.Add("OnEntryB-OK");
    }

    private void ActionAB() => Log.Add("Action-A->B");

    private FastFsm.ExceptionDirective Handle(FastFsm.ExceptionContext<EDState_Fluent, EDTrigger_Fluent> ctx)
        => ctx.Exception is InvalidOperationException
            ? FastFsm.ExceptionDirective.Continue
            : FastFsm.ExceptionDirective.Propagate;
}

// Test 2: Exception in Action
public enum TestState_Fluent { A, B }
public enum TestTrigger_Fluent { Go }

[FastFsm.Attributes.FastFsm(typeof(TestState_Fluent), typeof(TestTrigger_Fluent))]
public partial class ThrowingActionMachine_Fluent : FastFsm.LifecycleBase
{
    protected override void Configure()
    {
        In(TestState_Fluent.A)
            .On(TestTrigger_Fluent.Go).GoTo(TestState_Fluent.B).Do(ThrowingAction);
    }

    public void ThrowingAction() => throw new InvalidOperationException("boom");
}

// Test runner
public static class ExceptionFluentTests
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Fluent Exception Handling ===");
        
        // Test 1: OnEntry exception with Continue
        Console.WriteLine("\n--- Test 1: OnEntry Exception with Continue ---");
        var m1 = new ContinueOnEntryMachine_Fluent(EDState_Fluent.A) { ThrowOnEntryB = true };
        Console.WriteLine($"Initial state: {m1.CurrentState}");
        
        try
        {
            m1.Fire(EDTrigger_Fluent.Go);
            Console.WriteLine($"Final state: {m1.CurrentState}");
            Console.WriteLine($"Log: {string.Join(", ", m1.Log)}");
            Console.WriteLine($"✓ Test 1 passed: State changed to {m1.CurrentState}, Action ran despite OnEntry exception");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Test 1 failed: {ex.Message}");
        }
        
        // Test 2: Action exception
        Console.WriteLine("\n--- Test 2: Action Exception ---");
        var m2 = new ThrowingActionMachine_Fluent(TestState_Fluent.A);
        Console.WriteLine($"Initial state: {m2.CurrentState}");
        
        var ok = m2.TryFire(TestTrigger_Fluent.Go);
        Console.WriteLine($"TryFire result: {ok}");
        Console.WriteLine($"State after TryFire: {m2.CurrentState}");
        
        if (!ok && m2.CurrentState == TestState_Fluent.A)
        {
            Console.WriteLine("✓ Test 2a passed: TryFire returned false, state unchanged");
        }
        else
        {
            Console.WriteLine("✗ Test 2a failed: TryFire should return false and not change state");
        }
        
        try
        {
            m2.Fire(TestTrigger_Fluent.Go);
            Console.WriteLine("✗ Test 2b failed: Fire should have thrown");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("✓ Test 2b passed: Fire threw exception as expected");
        }
    }
}