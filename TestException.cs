using System;
using FastFsm.Tests.Machines;
using FastFsm.Tests.Features.Core;

// Test ExceptionCallbackMachine behavior
var attrMachine = new ExceptionCallbackMachine(StateCallbackTests.ExceptionState.A);
attrMachine.Start();

Console.WriteLine($"Initial state: {attrMachine.CurrentState}");

// Set to throw in OnEntry of state B
attrMachine.ThrowInOnEntry = true;

try
{
    attrMachine.Fire(StateCallbackTests.ExceptionTrigger.Go);
}
catch (Exception ex)
{
    Console.WriteLine($"Exception caught: {ex.Message}");
}

Console.WriteLine($"State after exception: {attrMachine.CurrentState}");
Console.WriteLine($"Expected: A, Actual: {attrMachine.CurrentState}");