using Abstractions.Attributes;
using Abstractions.Fluent;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ParserComparison.Tests;

/// <summary>
/// Example 4: Internal transitions with payload and method overloading
/// Shows internal transitions, method overloading (with/without payload), and mixed scenarios
/// </summary>
[StateMachine(typeof(DeviceState), typeof(DeviceTrigger))]
public partial class FluentPayloadExample4_InternalAndOverloads
{
    public enum DeviceState { Off, Standby, Active, Maintenance }
    public enum DeviceTrigger { PowerOn, Activate, Deactivate, PowerOff, Configure, Diagnose, Reset }

    // Payload types
    public sealed class ConfigurationData
    {
        public required string SettingName { get; init; }
        public required object Value { get; init; }
        public bool Persistent { get; init; }
    }

    public sealed class DiagnosticRequest
    {
        public required string TestType { get; init; }
        public int Depth { get; init; } = 1;
        public bool Verbose { get; init; }
    }

    public sealed class PowerSettings
    {
        public int PowerLevel { get; init; }
        public bool EcoMode { get; init; }
        public TimeSpan AutoOffDelay { get; init; }
    }

    private readonly Dictionary<string, object> _configuration = new();
    private int _diagnosticRunCount = 0;
    private int _powerLevel = 0;

    private static void Configure() => FSM
        .State(DeviceState.Off)
            .On(DeviceTrigger.PowerOn)
                .Payload<PowerSettings>()  // Optional payload for power settings
                .Guard(nameof(CanPowerOn))  // Has overloads
                .Action(nameof(PowerOn))    // Has overloads
                .GoTo(DeviceState.Standby)
        
        .State(DeviceState.Standby)
            .OnEntry(nameof(EnterStandby))  // Has overloads for initial vs transition entry
            .On(DeviceTrigger.Activate)
                .Guard(nameof(CanActivate))
                .Action(nameof(Activate))
                .GoTo(DeviceState.Active)
            .On(DeviceTrigger.PowerOff)
                .Action(nameof(PowerOff))
                .GoTo(DeviceState.Off)
            .OnInternal(DeviceTrigger.Configure)  // Internal transition with payload
                .Payload<ConfigurationData>()
                .Guard(nameof(ValidateConfiguration))
                .Action(nameof(ApplyConfiguration))
                .Internal()
        
        .State(DeviceState.Active)
            .OnEntry(nameof(OnActiveEntry))
            .On(DeviceTrigger.Deactivate)
                .Action(nameof(Deactivate))
                .GoTo(DeviceState.Standby)
            .OnInternal(DeviceTrigger.Configure)  // Internal with payload
                .Payload<ConfigurationData>()
                .Action(nameof(ApplyRuntimeConfiguration))
                .Internal()
            .OnInternal(DeviceTrigger.Diagnose)  // Another internal with different payload
                .Payload<DiagnosticRequest>()
                .Guard(nameof(CanRunDiagnostics))
                .Action(nameof(RunDiagnostics))
                .Internal()
            .On(DeviceTrigger.Reset)  // External transition without payload
                .Action(nameof(ResetDevice))
                .GoTo(DeviceState.Standby)
        
        .State(DeviceState.Maintenance)
            .OnEntry(nameof(StartMaintenance))
            .OnInternal(DeviceTrigger.Diagnose)
                .Payload<DiagnosticRequest>()
                .Action(nameof(RunMaintenanceDiagnostics))
                .Internal()
            .On(DeviceTrigger.Reset)
                .Guard(nameof(CanExitMaintenance))
                .Action(nameof(CompleteMaintenanceReset))
                .GoTo(DeviceState.Off);

    // Method overloading examples - Guards

    // Guard without payload
    private bool CanPowerOn()
    {
        Console.WriteLine("Checking basic power requirements");
        return true;
    }

    // Guard with payload - called when PowerSettings provided
    private bool CanPowerOn(PowerSettings settings)
    {
        Console.WriteLine($"Checking power on with settings: PowerLevel={settings.PowerLevel}, EcoMode={settings.EcoMode}");
        return settings.PowerLevel <= 100;
    }

    private bool CanActivate() => _powerLevel > 0;

    private bool ValidateConfiguration(ConfigurationData config) =>
        !string.IsNullOrEmpty(config.SettingName) && config.Value != null;

    private bool CanRunDiagnostics(DiagnosticRequest request) =>
        _diagnosticRunCount < 5 && request.Depth <= 3;

    private bool CanExitMaintenance() => _diagnosticRunCount > 0;

    // Method overloading examples - Actions

    // Action without payload
    private void PowerOn()
    {
        Console.WriteLine("Powering on with default settings");
        _powerLevel = 50;
    }

    // Action with payload - called when PowerSettings provided
    private void PowerOn(PowerSettings settings)
    {
        Console.WriteLine($"Powering on with custom settings: {settings.PowerLevel}% power");
        _powerLevel = settings.PowerLevel;
        
        if (settings.EcoMode)
        {
            Console.WriteLine("Eco mode enabled");
        }
    }

    private void Activate()
    {
        Console.WriteLine($"Device activated at power level {_powerLevel}");
    }

    private void Deactivate()
    {
        Console.WriteLine("Device deactivated");
    }

    private void PowerOff()
    {
        Console.WriteLine("Powering off");
        _powerLevel = 0;
    }

    private void ResetDevice()
    {
        Console.WriteLine("Resetting device to defaults");
        _configuration.Clear();
        _diagnosticRunCount = 0;
    }

    private void CompleteMaintenanceReset()
    {
        Console.WriteLine("Maintenance complete, resetting");
        _diagnosticRunCount = 0;
    }

    // Internal transition actions with payload
    private void ApplyConfiguration(ConfigurationData config)
    {
        Console.WriteLine($"Applying configuration: {config.SettingName} = {config.Value}");
        _configuration[config.SettingName] = config.Value;
        
        if (config.Persistent)
        {
            Console.WriteLine("Configuration saved persistently");
        }
    }

    private void ApplyRuntimeConfiguration(ConfigurationData config)
    {
        Console.WriteLine($"Runtime configuration update: {config.SettingName} = {config.Value}");
        _configuration[config.SettingName] = config.Value;
    }

    private void RunDiagnostics(DiagnosticRequest request)
    {
        _diagnosticRunCount++;
        Console.WriteLine($"Running diagnostics: {request.TestType} (depth={request.Depth}, verbose={request.Verbose})");
        Console.WriteLine($"Diagnostic run #{_diagnosticRunCount}");
    }

    private void RunMaintenanceDiagnostics(DiagnosticRequest request)
    {
        _diagnosticRunCount++;
        Console.WriteLine($"Maintenance diagnostics: {request.TestType}");
    }

    // OnEntry overloading examples

    // Called when entering from another state with no payload
    private void EnterStandby()
    {
        Console.WriteLine("Entered standby mode");
    }

    // Called when entering from a transition with PowerSettings payload
    private void EnterStandby(PowerSettings settings)
    {
        Console.WriteLine($"Entered standby with power settings: AutoOff in {settings.AutoOffDelay}");
    }

    private void OnActiveEntry()
    {
        Console.WriteLine($"Device is now active at {_powerLevel}% power");
    }

    private void StartMaintenance()
    {
        Console.WriteLine("Starting maintenance mode");
        _diagnosticRunCount = 0;
    }

    // Example usage methods showing different Fire patterns
    public void ExampleUsage()
    {
        var machine = new FluentPayloadExample4_InternalAndOverloads(DeviceState.Off);
        
        // Fire without payload - uses parameterless overload
        machine.Fire(DeviceTrigger.PowerOn);
        
        // Fire with payload - uses payload overload
        var powerSettings = new PowerSettings { PowerLevel = 75, EcoMode = true, AutoOffDelay = TimeSpan.FromMinutes(30) };
        machine.Fire(DeviceTrigger.PowerOn, powerSettings);
        
        // Internal transition with payload
        var config = new ConfigurationData { SettingName = "brightness", Value = 80, Persistent = true };
        machine.Fire(DeviceTrigger.Configure, config);
        
        // CanFire with payload
        var diagnostic = new DiagnosticRequest { TestType = "full", Depth = 2, Verbose = true };
        bool canDiagnose = machine.CanFire(DeviceTrigger.Diagnose, diagnostic);
    }
}