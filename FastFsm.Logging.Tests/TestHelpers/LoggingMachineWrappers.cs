using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Runtime;
using Microsoft.Extensions.Logging;

namespace FastFsm.Logging.Tests.TestHelpers;

// ============== PureStateMachine Wrappers ==============

public class PureStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly PureStateMachineFluent _machine;

    public PureStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("PureStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new PureStateMachineFluent(state, LoggerAdapter.For<PureStateMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((TestTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((TestTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((TestTrigger)trigger);
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

public class PureStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly PureStateMachine _machine;

    public PureStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("PureStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new PureStateMachine(state, LoggerAdapter.For<PureStateMachine>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((TestTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((TestTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((TestTrigger)trigger);
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

// ============== BasicStateMachine Wrappers ==============

public class BasicStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly BasicStateMachineFluent _machine;

    public BasicStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("BasicStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new BasicStateMachineFluent(state, LoggerAdapter.For<BasicStateMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((TestTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((TestTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((TestTrigger)trigger);
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

public class BasicStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly BasicStateMachine _machine;

    public BasicStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("BasicStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new BasicStateMachine(state, LoggerAdapter.For<BasicStateMachine>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((TestTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((TestTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((TestTrigger)trigger);
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

// ============== PayloadStateMachine Wrappers ==============

public class PayloadStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly PayloadStateMachineFluent _machine;

    public PayloadStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("PayloadStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new PayloadStateMachineFluent(state, LoggerAdapter.For<PayloadStateMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasDefaultPayload;

    public void Start() => _machine.Start();
    
    public bool TryFire(object trigger, object? payload = null)
    {
        if (payload != null)
            return _machine.TryFire((TestTrigger)trigger, (TestPayload)payload);
        return _machine.TryFire((TestTrigger)trigger);
    }

    public void Fire(object trigger, object? payload = null)
    {
        if (payload != null)
            _machine.Fire((TestTrigger)trigger, (TestPayload)payload);
        else
            _machine.Fire((TestTrigger)trigger);
    }

    public bool CanFire(object trigger) => _machine.CanFire((TestTrigger)trigger);
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

public class PayloadStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly PayloadStateMachine _machine;

    public PayloadStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("PayloadStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new PayloadStateMachine(state, LoggerAdapter.For<PayloadStateMachine>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasDefaultPayload;

    public void Start() => _machine.Start();
    
    public bool TryFire(object trigger, object? payload = null)
    {
        if (payload != null)
            return _machine.TryFire((TestTrigger)trigger, (TestPayload)payload);
        return _machine.TryFire((TestTrigger)trigger);
    }

    public void Fire(object trigger, object? payload = null)
    {
        if (payload != null)
            _machine.Fire((TestTrigger)trigger, (TestPayload)payload);
        else
            _machine.Fire((TestTrigger)trigger);
    }

    public bool CanFire(object trigger) => _machine.CanFire((TestTrigger)trigger);
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
