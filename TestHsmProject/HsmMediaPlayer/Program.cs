using System;
using Abstractions.Attributes;

namespace HsmMediaPlayerTest;

// ==========================================
// HIERARCHICAL STATE MACHINE - Media Player
// Based on README.md example
// ==========================================

public enum PlayerState 
{ 
    PowerOff,
    PowerOn,              // Parent
    PowerOn_Stopped,      // Child
    PowerOn_Playing,      // Child  
    PowerOn_Paused        // Child
}

public enum PlayerTrigger 
{ 
    PowerButton, 
    Play, 
    Pause, 
    Stop 
}

[StateMachine(typeof(PlayerState), typeof(PlayerTrigger), EnableHierarchy = true)]
public partial class MediaPlayer
{
    private string _currentTrack = "TestSong.mp3";
    
    // Configure parent state with shallow history
    [State(PlayerState.PowerOn, History = HistoryMode.Shallow)]
    private void ConfigurePowerOn() { }
    
    // Configure child states
    [State(PlayerState.PowerOn_Stopped, Parent = PlayerState.PowerOn, IsInitial = true)]
    private void ConfigureStopped() { }
    
    [State(PlayerState.PowerOn_Playing, Parent = PlayerState.PowerOn, 
        OnEntry = nameof(StartPlayback), OnExit = nameof(StopPlayback))]
    private void ConfigurePlaying() { }
    
    [State(PlayerState.PowerOn_Paused, Parent = PlayerState.PowerOn)]
    private void ConfigurePaused() { }
    
    // Configure transitions
    [Transition(PlayerState.PowerOff, PlayerTrigger.PowerButton, PlayerState.PowerOn)]
    [Transition(PlayerState.PowerOn, PlayerTrigger.PowerButton, PlayerState.PowerOff)]
    [Transition(PlayerState.PowerOn_Stopped, PlayerTrigger.Play, PlayerState.PowerOn_Playing)]
    [Transition(PlayerState.PowerOn_Playing, PlayerTrigger.Pause, PlayerState.PowerOn_Paused)]
    [Transition(PlayerState.PowerOn_Playing, PlayerTrigger.Stop, PlayerState.PowerOn_Stopped)]
    [Transition(PlayerState.PowerOn_Paused, PlayerTrigger.Play, PlayerState.PowerOn_Playing)]
    [Transition(PlayerState.PowerOn_Paused, PlayerTrigger.Stop, PlayerState.PowerOn_Stopped)]
    private void ConfigureTransitions() { }
    
    // Internal transition example - update progress without changing state
    [InternalTransition(PlayerState.PowerOn, PlayerTrigger.Play, Action = nameof(LogProgress))]
    private void ConfigureInternalTransitions() { }
    
    // Action methods
    private void StartPlayback() => Console.WriteLine($"▶️ Playing: {_currentTrack}");
    private void StopPlayback() => Console.WriteLine("⏸️ Playback stopped");
    private void LogProgress() => Console.WriteLine("Progress updated (internal transition)");
}

// ==========================================
// TEST PROGRAM
// ==========================================

class Program
{
    static void Main()
    {
        Console.WriteLine("=== HSM Media Player Test ===");
        Console.WriteLine("Testing FastFsm.Net version 0.6.9.5 from local NuGet source");
        Console.WriteLine();
        
        // Create and start the media player
        var player = new MediaPlayer(PlayerState.PowerOff);
        player.Start();
        
        Console.WriteLine($"Initial state: {player.CurrentState}");
        Console.WriteLine();
        
        // Test basic transitions
        Console.WriteLine("--- Power On ---");
        player.Fire(PlayerTrigger.PowerButton);
        Console.WriteLine($"Current state: {player.CurrentState}");
        Console.WriteLine();
        
        Console.WriteLine("--- Play ---");
        player.Fire(PlayerTrigger.Play);
        Console.WriteLine($"Current state: {player.CurrentState}");
        Console.WriteLine();
        
        Console.WriteLine("--- Pause ---");
        player.Fire(PlayerTrigger.Pause);
        Console.WriteLine($"Current state: {player.CurrentState}");
        Console.WriteLine();
        
        Console.WriteLine("--- Power Off ---");
        player.Fire(PlayerTrigger.PowerButton);
        Console.WriteLine($"Current state: {player.CurrentState}");
        Console.WriteLine();
        
        Console.WriteLine("--- Power On Again (Should restore to Paused due to shallow history) ---");
        player.Fire(PlayerTrigger.PowerButton);
        Console.WriteLine($"Restored state: {player.CurrentState}");
        Console.WriteLine();
        
        // Test more transitions
        Console.WriteLine("--- Stop ---");
        player.Fire(PlayerTrigger.Stop);
        Console.WriteLine($"Current state: {player.CurrentState}");
        Console.WriteLine();
        
        // Test permitted triggers
        Console.WriteLine("--- Permitted Triggers ---");
        var permittedTriggers = player.GetPermittedTriggers();
        Console.WriteLine($"From {player.CurrentState}, can fire: {string.Join(", ", permittedTriggers)}");
        Console.WriteLine();
        
        // Test CanFire
        Console.WriteLine("--- CanFire Checks ---");
        Console.WriteLine($"Can fire Play? {player.CanFire(PlayerTrigger.Play)}");
        Console.WriteLine($"Can fire Pause? {player.CanFire(PlayerTrigger.Pause)}");
        Console.WriteLine($"Can fire Stop? {player.CanFire(PlayerTrigger.Stop)}");
        Console.WriteLine($"Can fire PowerButton? {player.CanFire(PlayerTrigger.PowerButton)}");
        
        Console.WriteLine();
        Console.WriteLine("=== Test Complete ===");
    }
}