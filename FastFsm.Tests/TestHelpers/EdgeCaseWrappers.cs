using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.EdgeCases;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    // ====== CaseSensitive ======
    public class CaseSensitiveFluentWrapper : IStateMachineTestWrapper
    {
        private readonly CaseSensitiveMachineFluent _machine;
        
        public CaseSensitiveFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.CaseSensitiveState>(
                "CaseSensitive", initialStateName);
            var state = (NameCollisionTests.CaseSensitiveState)Enum.Parse(
                typeof(NameCollisionTests.CaseSensitiveState), resolvedName);
            _machine = new CaseSensitiveMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.CaseSensitiveTrigger)Enum.Parse(
                typeof(NameCollisionTests.CaseSensitiveTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.CaseSensitiveTrigger)Enum.Parse(
                typeof(NameCollisionTests.CaseSensitiveTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.CaseSensitiveTrigger)Enum.Parse(
                typeof(NameCollisionTests.CaseSensitiveTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    public class CaseSensitiveLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly CaseSensitiveMachineLegacy _machine;
        
        public CaseSensitiveLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.CaseSensitiveState>(
                "CaseSensitive", initialStateName);
            var state = (NameCollisionTests.CaseSensitiveState)Enum.Parse(
                typeof(NameCollisionTests.CaseSensitiveState), resolvedName);
            _machine = new CaseSensitiveMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.CaseSensitiveTrigger)Enum.Parse(
                typeof(NameCollisionTests.CaseSensitiveTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.CaseSensitiveTrigger)Enum.Parse(
                typeof(NameCollisionTests.CaseSensitiveTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.CaseSensitiveTrigger)Enum.Parse(
                typeof(NameCollisionTests.CaseSensitiveTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    // ====== ConflictingNames ======
    public class ConflictingNamesFluentWrapper : IStateMachineTestWrapper
    {
        private readonly ConflictingNamesMachineFluent _machine;
        
        public ConflictingNamesFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.ConflictState>(
                "ConflictingNames", initialStateName);
            var state = (NameCollisionTests.ConflictState)Enum.Parse(
                typeof(NameCollisionTests.ConflictState), resolvedName);
            _machine = new ConflictingNamesMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.ConflictTrigger)Enum.Parse(
                typeof(NameCollisionTests.ConflictTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.ConflictTrigger)Enum.Parse(
                typeof(NameCollisionTests.ConflictTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.ConflictTrigger)Enum.Parse(
                typeof(NameCollisionTests.ConflictTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    public class ConflictingNamesLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly ConflictingNamesMachineLegacy _machine;
        
        public ConflictingNamesLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.ConflictState>(
                "ConflictingNames", initialStateName);
            var state = (NameCollisionTests.ConflictState)Enum.Parse(
                typeof(NameCollisionTests.ConflictState), resolvedName);
            _machine = new ConflictingNamesMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.ConflictTrigger)Enum.Parse(
                typeof(NameCollisionTests.ConflictTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.ConflictTrigger)Enum.Parse(
                typeof(NameCollisionTests.ConflictTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.ConflictTrigger)Enum.Parse(
                typeof(NameCollisionTests.ConflictTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    // ====== LongName ======
    public class LongNameFluentWrapper : IStateMachineTestWrapper
    {
        private readonly LongNameMachineFluent _machine;
        
        public LongNameFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.LongNameState>(
                "LongName", initialStateName);
            var state = (NameCollisionTests.LongNameState)Enum.Parse(
                typeof(NameCollisionTests.LongNameState), resolvedName);
            _machine = new LongNameMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.LongNameTrigger)Enum.Parse(
                typeof(NameCollisionTests.LongNameTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.LongNameTrigger)Enum.Parse(
                typeof(NameCollisionTests.LongNameTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.LongNameTrigger)Enum.Parse(
                typeof(NameCollisionTests.LongNameTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    public class LongNameLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly LongNameMachineLegacy _machine;
        
        public LongNameLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.LongNameState>(
                "LongName", initialStateName);
            var state = (NameCollisionTests.LongNameState)Enum.Parse(
                typeof(NameCollisionTests.LongNameState), resolvedName);
            _machine = new LongNameMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.LongNameTrigger)Enum.Parse(
                typeof(NameCollisionTests.LongNameTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.LongNameTrigger)Enum.Parse(
                typeof(NameCollisionTests.LongNameTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.LongNameTrigger)Enum.Parse(
                typeof(NameCollisionTests.LongNameTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    // ====== Unicode ======
    public class UnicodeFluentWrapper : IStateMachineTestWrapper
    {
        private readonly UnicodeMachineFluent _machine;
        
        public UnicodeFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.UnicodeState>(
                "Unicode", initialStateName);
            var state = (NameCollisionTests.UnicodeState)Enum.Parse(
                typeof(NameCollisionTests.UnicodeState), resolvedName);
            _machine = new UnicodeMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.UnicodeTrigger)Enum.Parse(
                typeof(NameCollisionTests.UnicodeTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.UnicodeTrigger)Enum.Parse(
                typeof(NameCollisionTests.UnicodeTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.UnicodeTrigger)Enum.Parse(
                typeof(NameCollisionTests.UnicodeTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    public class UnicodeLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly UnicodeMachineLegacy _machine;
        
        public UnicodeLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.UnicodeState>(
                "Unicode", initialStateName);
            var state = (NameCollisionTests.UnicodeState)Enum.Parse(
                typeof(NameCollisionTests.UnicodeState), resolvedName);
            _machine = new UnicodeMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.UnicodeTrigger)Enum.Parse(
                typeof(NameCollisionTests.UnicodeTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.UnicodeTrigger)Enum.Parse(
                typeof(NameCollisionTests.UnicodeTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.UnicodeTrigger)Enum.Parse(
                typeof(NameCollisionTests.UnicodeTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    // ====== Numeric ======
    public class NumericFluentWrapper : IStateMachineTestWrapper
    {
        private readonly NumericMachineFluent _machine;
        
        public NumericFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.NumericState>(
                "Numeric", initialStateName);
            var state = (NameCollisionTests.NumericState)Enum.Parse(
                typeof(NameCollisionTests.NumericState), resolvedName);
            _machine = new NumericMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.NumericTrigger)Enum.Parse(
                typeof(NameCollisionTests.NumericTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.NumericTrigger)Enum.Parse(
                typeof(NameCollisionTests.NumericTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.NumericTrigger)Enum.Parse(
                typeof(NameCollisionTests.NumericTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    public class NumericLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly NumericMachineLegacy _machine;
        
        public NumericLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.NumericState>(
                "Numeric", initialStateName);
            var state = (NameCollisionTests.NumericState)Enum.Parse(
                typeof(NameCollisionTests.NumericState), resolvedName);
            _machine = new NumericMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.NumericTrigger)Enum.Parse(
                typeof(NameCollisionTests.NumericTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.NumericTrigger)Enum.Parse(
                typeof(NameCollisionTests.NumericTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.NumericTrigger)Enum.Parse(
                typeof(NameCollisionTests.NumericTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
}