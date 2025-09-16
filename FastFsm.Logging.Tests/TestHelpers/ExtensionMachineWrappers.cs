using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Contracts;
using Microsoft.Extensions.Logging;

namespace FastFsm.Logging.Tests.TestHelpers;

// ============== ExtensionsStateMachine Wrappers ==============

public class ExtensionsStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly ExtensionsStateMachineFluent _machine;

    public ExtensionsStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null, IStateMachineExtension[]? extensions = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("ExtensionsStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new ExtensionsStateMachineFluent(state, extensions ?? Array.Empty<IStateMachineExtension>(), LoggerAdapter.For<ExtensionsStateMachineFluent>(logger));
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

public class ExtensionsStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly ExtensionsStateMachine _machine;

    public ExtensionsStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null, IStateMachineExtension[]? extensions = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("ExtensionsStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new ExtensionsStateMachine(state, extensions ?? Array.Empty<IStateMachineExtension>(), LoggerAdapter.For<ExtensionsStateMachine>(logger));
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

// ============== FullStateMachine Wrappers ==============

public class FullStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly FullStateMachineFluent _machine;

    public FullStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null, IStateMachineExtension[]? extensions = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("FullStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new FullStateMachineFluent(state, extensions ?? Array.Empty<IStateMachineExtension>(), LoggerAdapter.For<FullStateMachineFluent>(logger));
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

public class FullStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly FullStateMachine _machine;

    public FullStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null, IStateMachineExtension[]? extensions = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("FullStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new FullStateMachine(state, extensions ?? Array.Empty<IStateMachineExtension>(), LoggerAdapter.For<FullStateMachine>(logger));
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

// ============== MultiPayloadStateMachine Wrappers ==============

public class MultiPayloadStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly MultiPayloadStateMachineFluent _machine;

    public MultiPayloadStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("MultiPayloadStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new MultiPayloadStateMachineFluent(state, LoggerAdapter.For<MultiPayloadStateMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasMultiPayloads;

    public void Start() => _machine.Start();
    
    public bool TryFire(object trigger, object? payload = null)
    {
        var triggerEnum = (TestTrigger)trigger;
        if (payload != null)
        {
            // Dynamic dispatch based on trigger and payload type
            if (triggerEnum == TestTrigger.Start && payload is TestPayload tp)
                return _machine.TryFire(triggerEnum, tp);
            else if (triggerEnum == TestTrigger.Process && payload is string s)
                return _machine.TryFire(triggerEnum, s);
        }
        return _machine.TryFire(triggerEnum);
    }

    public void Fire(object trigger, object? payload = null)
    {
        var triggerEnum = (TestTrigger)trigger;
        if (payload != null)
        {
            if (triggerEnum == TestTrigger.Start && payload is TestPayload tp)
                _machine.Fire(triggerEnum, tp);
            else if (triggerEnum == TestTrigger.Process && payload is string s)
                _machine.Fire(triggerEnum, s);
        }
        else
        {
            _machine.Fire(triggerEnum);
        }
    }

    public bool CanFire(object trigger) => _machine.CanFire((TestTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var triggerEnum = (TestTrigger)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            return await _machine.TryFireAsync(triggerEnum, dynamicPayload, ct);
        }
        return await _machine.TryFireAsync(triggerEnum, ct);
    }

    public async ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var triggerEnum = (TestTrigger)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            await _machine.FireAsync(triggerEnum, dynamicPayload, ct);
        }
        else
        {
            await _machine.FireAsync(triggerEnum, ct);
        }
    }
}

public class MultiPayloadStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly MultiPayloadStateMachine _machine;

    public MultiPayloadStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestState>("MultiPayloadStateMachine", initialStateName);
        var state = (TestState)Enum.Parse(typeof(TestState), resolvedName);
        _machine = new MultiPayloadStateMachine(state, logger);
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasMultiPayloads;

    public void Start() => _machine.Start();
    
    public bool TryFire(object trigger, object? payload = null)
    {
        var triggerEnum = (TestTrigger)trigger;
        if (payload != null)
        {
            // Dynamic dispatch based on trigger and payload type
            if (triggerEnum == TestTrigger.Start && payload is TestPayload tp)
                return _machine.TryFire(triggerEnum, tp);
            else if (triggerEnum == TestTrigger.Process && payload is string s)
                return _machine.TryFire(triggerEnum, s);
        }
        return _machine.TryFire(triggerEnum);
    }

    public void Fire(object trigger, object? payload = null)
    {
        var triggerEnum = (TestTrigger)trigger;
        if (payload != null)
        {
            if (triggerEnum == TestTrigger.Start && payload is TestPayload tp)
                _machine.Fire(triggerEnum, tp);
            else if (triggerEnum == TestTrigger.Process && payload is string s)
                _machine.Fire(triggerEnum, s);
        }
        else
        {
            _machine.Fire(triggerEnum);
        }
    }

    public bool CanFire(object trigger) => _machine.CanFire((TestTrigger)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var triggerEnum = (TestTrigger)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            return await _machine.TryFireAsync(triggerEnum, dynamicPayload, ct);
        }
        return await _machine.TryFireAsync(triggerEnum, ct);
    }

    public async ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var triggerEnum = (TestTrigger)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            await _machine.FireAsync(triggerEnum, dynamicPayload, ct);
        }
        else
        {
            await _machine.FireAsync(triggerEnum, ct);
        }
    }
}
