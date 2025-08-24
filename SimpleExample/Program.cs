// No explicit using needed - global using from NuGet package handles it!

namespace SimpleExample;

// Define states and triggers
public enum TrafficLightState { Red, Yellow, Green }
public enum TrafficLightTrigger { Next }

// Create state machine WITHOUT explicit using Abstractions.Attributes
// The attributes should be available globally
[StateMachine(typeof(TrafficLightState), typeof(TrafficLightTrigger))]
public partial class TrafficLight
{
    // Define transitions
    [Transition(TrafficLightState.Red, TrafficLightTrigger.Next, TrafficLightState.Green)]
    [Transition(TrafficLightState.Green, TrafficLightTrigger.Next, TrafficLightState.Yellow)]
    [Transition(TrafficLightState.Yellow, TrafficLightTrigger.Next, TrafficLightState.Red)]
    private void ConfigureTransitions() { }

    // Define state behaviors
    [State(TrafficLightState.Red, OnEntry = nameof(OnRedLight))]
    private void ConfigureRed() { }
    
    [State(TrafficLightState.Yellow, OnEntry = nameof(OnYellowLight))]
    private void ConfigureYellow() { }
    
    [State(TrafficLightState.Green, OnEntry = nameof(OnGreenLight))]
    private void ConfigureGreen() { }

    private void OnRedLight() => Console.WriteLine("🔴 Red light - STOP!");
    private void OnYellowLight() => Console.WriteLine("🟡 Yellow light - Prepare to stop");
    private void OnGreenLight() => Console.WriteLine("🟢 Green light - GO!");
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Traffic Light State Machine Example ===");
        Console.WriteLine("This example demonstrates that FastFSM attributes work");
        Console.WriteLine("WITHOUT explicit 'using Abstractions.Attributes' statement");
        Console.WriteLine("thanks to global usings in the NuGet package.\n");

        // Create and start the traffic light
        var trafficLight = new TrafficLight(TrafficLightState.Red);
        trafficLight.Start();

        // Cycle through the lights
        for (int i = 0; i < 6; i++)
        {
            Console.WriteLine($"\nCycle {i + 1}:");
            Console.WriteLine($"Current state: {trafficLight.CurrentState}");
            trafficLight.Fire(TrafficLightTrigger.Next);
        }

        Console.WriteLine("\n✅ Example completed successfully!");
        Console.WriteLine("The state machine worked without explicit using statements.");
    }
}