using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Contracts;
using Microsoft.Extensions.Logging;

namespace Tests.Logging.TestHelpers;

// ============== InitialOnEntryStateMachineActions Wrappers ==============

public class InitialOnEntryStateMachineActionsFluentWrapper : IStateMachineTestWrapper
{
    private readonly InitialOnEntryStateMachineActionsFluent _machine;

    public InitialOnEntryStateMachineActionsFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestInitialState>("InitialOnEntryStateMachineActions", initialStateName);
        var state = (TestInitialState)Enum.Parse(typeof(TestInitialState), resolvedName);
        _machine = new InitialOnEntryStateMachineActionsFluent(state, LoggerAdapter.For<InitialOnEntryStateMachineActionsFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((TestInitialTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((TestInitialTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((TestInitialTrigger)trigger);
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

public class InitialOnEntryStateMachineActionsLegacyWrapper : IStateMachineTestWrapper
{
    private readonly InitialOnEntryStateMachineActions _machine;

    public InitialOnEntryStateMachineActionsLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<TestInitialState>("InitialOnEntryStateMachineActions", initialStateName);
        var state = (TestInitialState)Enum.Parse(typeof(TestInitialState), resolvedName);
        _machine = new InitialOnEntryStateMachineActions(state, LoggerAdapter.For<InitialOnEntryStateMachineActions>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((TestInitialTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((TestInitialTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((TestInitialTrigger)trigger);
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

// ============== FullMultiPayloadMachine Wrappers ==============

public class FullMultiPayloadMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly FullMultiPayloadMachineFluent _machine;

    public FullMultiPayloadMachineFluentWrapper(string? initialStateName, ILogger? logger = null, IStateMachineExtension[]? extensions = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<OrderStatePayload>("FullMultiPayloadMachine", initialStateName);
        var state = (OrderStatePayload)Enum.Parse(typeof(OrderStatePayload), resolvedName);
        _machine = new FullMultiPayloadMachineFluent(state, extensions, LoggerAdapter.For<FullMultiPayloadMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasMultiPayloads;

    public void Start() => _machine.Start();
    
    public bool TryFire(object trigger, object? payload = null)
    {
        var triggerEnum = (OrderTriggerPayload)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            return _machine.TryFire(triggerEnum, dynamicPayload);
        }
        return _machine.TryFire(triggerEnum);
    }

    public void Fire(object trigger, object? payload = null)
    {
        var triggerEnum = (OrderTriggerPayload)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            _machine.Fire(triggerEnum, dynamicPayload);
        }
        else
        {
            _machine.Fire(triggerEnum);
        }
    }

    public bool CanFire(object trigger) => _machine.CanFire((OrderTriggerPayload)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        => ValueTask.FromResult(TryFire(trigger, payload));

    public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        Fire(trigger, payload);
        return ValueTask.CompletedTask;
    }
}

public class FullMultiPayloadMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly FullMultiPayloadMachine _machine;

    public FullMultiPayloadMachineLegacyWrapper(string? initialStateName, ILogger? logger = null, IStateMachineExtension[]? extensions = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<OrderStatePayload>("FullMultiPayloadMachine", initialStateName);
        var state = (OrderStatePayload)Enum.Parse(typeof(OrderStatePayload), resolvedName);
        _machine = new FullMultiPayloadMachine(state, extensions, LoggerAdapter.For<FullMultiPayloadMachine>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.HasMultiPayloads;

    public void Start() => _machine.Start();
    
    public bool TryFire(object trigger, object? payload = null)
    {
        var triggerEnum = (OrderTriggerPayload)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            return _machine.TryFire(triggerEnum, dynamicPayload);
        }
        return _machine.TryFire(triggerEnum);
    }

    public void Fire(object trigger, object? payload = null)
    {
        var triggerEnum = (OrderTriggerPayload)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            _machine.Fire(triggerEnum, dynamicPayload);
        }
        else
        {
            _machine.Fire(triggerEnum);
        }
    }

    public bool CanFire(object trigger) => _machine.CanFire((OrderTriggerPayload)trigger);
    public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();

    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _machine.Start();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        // Legacy machine exposes only sync API; wrap accordingly
        var triggerEnum = (OrderTriggerPayload)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            var res = _machine.TryFire(triggerEnum, dynamicPayload);
            return ValueTask.FromResult(res);
        }
        return ValueTask.FromResult(_machine.TryFire(triggerEnum));
    }

    public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
    {
        var triggerEnum = (OrderTriggerPayload)trigger;
        if (payload != null)
        {
            dynamic dynamicPayload = payload;
            _machine.Fire(triggerEnum, dynamicPayload);
        }
        else
        {
            _machine.Fire(triggerEnum);
        }
        return ValueTask.CompletedTask;
    }
}

// ============== ExampleStateMachine Wrappers ==============

public class ExampleStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly ExampleStateMachineFluent _machine;

    public ExampleStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<OrderState>("ExampleStateMachine", initialStateName);
        var state = (OrderState)Enum.Parse(typeof(OrderState), resolvedName);
        _machine = new ExampleStateMachineFluent(state, LoggerAdapter.For<ExampleStateMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((OrderTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((OrderTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((OrderTrigger)trigger);
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

public class ExampleStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly ExampleStateMachine _machine;

    public ExampleStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<OrderState>("ExampleStateMachine", initialStateName);
        var state = (OrderState)Enum.Parse(typeof(OrderState), resolvedName);
        _machine = new ExampleStateMachine(state, LoggerAdapter.For<ExampleStateMachine>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((OrderTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((OrderTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((OrderTrigger)trigger);
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

// ============== GuardedStateMachine Wrappers ==============

public class GuardedStateMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly GuardedStateMachineFluent _machine;

    public GuardedStateMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<ProcessState>("GuardedStateMachine", initialStateName);
        var state = (ProcessState)Enum.Parse(typeof(ProcessState), resolvedName);
        _machine = new GuardedStateMachineFluent(state, LoggerAdapter.For<GuardedStateMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((ProcessTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((ProcessTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((ProcessTrigger)trigger);
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

public class GuardedStateMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly GuardedStateMachine _machine;

    public GuardedStateMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<ProcessState>("GuardedStateMachine", initialStateName);
        var state = (ProcessState)Enum.Parse(typeof(ProcessState), resolvedName);
        _machine = new GuardedStateMachine(state, LoggerAdapter.For<GuardedStateMachine>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((ProcessTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((ProcessTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((ProcessTrigger)trigger);
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

// ============== ExtensibleMachine Wrappers ==============

public class ExtensibleMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly ExtensibleMachineFluent _machine;

    public ExtensibleMachineFluentWrapper(string? initialStateName, ILogger? logger = null, IStateMachineExtension[]? extensions = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<WorkflowState>("ExtensibleMachine", initialStateName);
        var state = (WorkflowState)Enum.Parse(typeof(WorkflowState), resolvedName);
        _machine = new ExtensibleMachineFluent(state, extensions ?? Array.Empty<IStateMachineExtension>(), LoggerAdapter.For<ExtensibleMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((WorkflowTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((WorkflowTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((WorkflowTrigger)trigger);
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

public class ExtensibleMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly ExtensibleMachine _machine;

    public ExtensibleMachineLegacyWrapper(string? initialStateName, ILogger? logger = null, IStateMachineExtension[]? extensions = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<WorkflowState>("ExtensibleMachine", initialStateName);
        var state = (WorkflowState)Enum.Parse(typeof(WorkflowState), resolvedName);
        _machine = new ExtensibleMachine(state, extensions ?? Array.Empty<IStateMachineExtension>(), LoggerAdapter.For<ExtensibleMachine>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.None;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((WorkflowTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((WorkflowTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((WorkflowTrigger)trigger);
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

// ============== HsmMachine Wrappers ==============

public class HsmMachineFluentWrapper : IStateMachineTestWrapper
{
    private readonly HsmMachineFluent _machine;

    public HsmMachineFluentWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<HState>("HsmMachine", initialStateName);
        var state = (HState)Enum.Parse(typeof(HState), resolvedName);
        _machine = new HsmMachineFluent(state, LoggerAdapter.For<HsmMachineFluent>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((HTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((HTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((HTrigger)trigger);
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

public class HsmMachineLegacyWrapper : IStateMachineTestWrapper
{
    private readonly HsmMachine _machine;

    public HsmMachineLegacyWrapper(string? initialStateName, ILogger? logger = null)
    {
        var resolvedName = InitialStateResolver.ResolveOrDefault<HState>("HsmMachine", initialStateName);
        var state = (HState)Enum.Parse(typeof(HState), resolvedName);
        _machine = new HsmMachine(state, LoggerAdapter.For<HsmMachine>(logger));
    }

    public object CurrentState => _machine.CurrentState;
    public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;

    public void Start() => _machine.Start();
    public bool TryFire(object trigger, object? payload = null) => _machine.TryFire((HTrigger)trigger);
    public void Fire(object trigger, object? payload = null) => _machine.Fire((HTrigger)trigger);
    public bool CanFire(object trigger) => _machine.CanFire((HTrigger)trigger);
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
