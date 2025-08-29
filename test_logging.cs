using System;
using FastFsm.Logging.Tests;
using Microsoft.Extensions.Logging;

public class TestProgram
{
    public static void Main()
    {
        try
        {
            Console.WriteLine("Creating machine...");
            var machine = new HsmMachine(HState.A, null);
            
            Console.WriteLine("Starting machine...");
            machine.Start();
            
            Console.WriteLine("Firing MoveToA2...");
            machine.TryFire(HTrigger.MoveToA2);
            
            Console.WriteLine("Firing Switch...");
            machine.TryFire(HTrigger.Switch);
            
            Console.WriteLine("Firing Back...");
            machine.TryFire(HTrigger.Back);
            
            Console.WriteLine("Test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}