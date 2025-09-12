using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FastFsm.Logging.Tests.TestHelpers;

// ============== LifecycleMachine Wrappers ==============

public class LifecycleMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly LifecycleMachineFluent _machine;

    public LifecycleMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<LifecycleState>("LifecycleMachine", initialStateName);
        var state = (LifecycleState)Enum.Parse(typeof(LifecycleState), resolvedName);
        _machine = new LifecycleMachineFluent(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((LifecycleTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((LifecycleTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((LifecycleTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var result = TryFire(trigger, payload);
        return ValueTask.FromResult(result);
    }

    public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        Fire(trigger, payload);
        return ValueTask.CompletedTask;
    }
}

public class LifecycleMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly LifecycleMachine _machine;

    public LifecycleMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<LifecycleState>("LifecycleMachine", initialStateName);
        var state = (LifecycleState)Enum.Parse(typeof(LifecycleState), resolvedName);
        _machine = new LifecycleMachine(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((LifecycleTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((LifecycleTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((LifecycleTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var result = TryFire(trigger, payload);
        return ValueTask.FromResult(result);
    }

    public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        Fire(trigger, payload);
        return ValueTask.CompletedTask;
    }
}

// ============== AsyncLifecycleMachine Wrappers ==============

public class AsyncLifecycleMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly AsyncLifecycleMachineFluent _machine;

    public AsyncLifecycleMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<AsyncLifecycleState>("AsyncLifecycleMachine", initialStateName);
        var state = (AsyncLifecycleState)Enum.Parse(typeof(AsyncLifecycleState), resolvedName);
        _machine = new AsyncLifecycleMachineFluent(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasAsync;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((AsyncLifecycleTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((AsyncLifecycleTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((AsyncLifecycleTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        await _machine.StartAsync(ct);
    }

    public async ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        return await _machine.TryFireAsync((AsyncLifecycleTrigger)trigger, ct);
    }

    public async ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        await _machine.FireAsync((AsyncLifecycleTrigger)trigger, ct);
    }
}

public class AsyncLifecycleMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly AsyncLifecycleMachine _machine;

    public AsyncLifecycleMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<AsyncLifecycleState>("AsyncLifecycleMachine", initialStateName);
        var state = (AsyncLifecycleState)Enum.Parse(typeof(AsyncLifecycleState), resolvedName);
        _machine = new AsyncLifecycleMachine(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasAsync;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((AsyncLifecycleTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((AsyncLifecycleTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((AsyncLifecycleTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        await _machine.StartAsync(ct);
    }

    public async ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        return await _machine.TryFireAsync((AsyncLifecycleTrigger)trigger, ct);
    }

    public async ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        await _machine.FireAsync((AsyncLifecycleTrigger)trigger, ct);
    }
}

// ============== InternalTransitionMachine Wrappers ==============

public class InternalTransitionMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly InternalTransitionMachineFluent _machine;

    public InternalTransitionMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<InternalState>("InternalTransitionMachine", initialStateName);
        var state = (InternalState)Enum.Parse(typeof(InternalState), resolvedName);
        _machine = new InternalTransitionMachineFluent(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasInternalTransitions;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((InternalTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((InternalTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((InternalTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var result = TryFire(trigger, payload);
        return ValueTask.FromResult(result);
    }

    public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        Fire(trigger, payload);
        return ValueTask.CompletedTask;
    }
}

public class InternalTransitionMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly InternalTransitionMachine _machine;

    public InternalTransitionMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<InternalState>("InternalTransitionMachine", initialStateName);
        var state = (InternalState)Enum.Parse(typeof(InternalState), resolvedName);
        _machine = new InternalTransitionMachine(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasInternalTransitions;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((InternalTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((InternalTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((InternalTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var result = TryFire(trigger, payload);
        return ValueTask.FromResult(result);
    }

    public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        Fire(trigger, payload);
        return ValueTask.CompletedTask;
    }
}

// ============== StructStateMachine Wrappers ==============

public class StructStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly StructStateMachineFluent _machine;

    public StructStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<StructState>("StructStateMachine", initialStateName);
        var state = (StructState)Enum.Parse(typeof(StructState), resolvedName);
        _machine = new StructStateMachineFluent(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((StructTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((StructTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((StructTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var result = TryFire(trigger, payload);
        return ValueTask.FromResult(result);
    }

    public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        Fire(trigger, payload);
        return ValueTask.CompletedTask;
    }
}

public class StructStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly StructStateMachine _machine;

    public StructStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<StructState>("StructStateMachine", initialStateName);
        var state = (StructState)Enum.Parse(typeof(StructState), resolvedName);
        _machine = new StructStateMachine(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((StructTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((StructTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((StructTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var result = TryFire(trigger, payload);
        return ValueTask.FromResult(result);
    }

    public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        Fire(trigger, payload);
        return ValueTask.CompletedTask;
    }
}