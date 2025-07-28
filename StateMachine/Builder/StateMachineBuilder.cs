

using System;
using System.Reflection;
using Abstractions.Attributes;
using StateMachine.Contracts;

namespace StateMachine.Builder;


public class StateMachineBuilder<TState, TTrigger> : IStateMachineBuilder<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    private readonly Type _machineType;
    
    public StateMachineBuilder(Type machineType)
    {
        _machineType = machineType ?? throw new ArgumentNullException(nameof(machineType));
        
        // Verify the type has the StateMachine attribute
        var attr = _machineType.GetCustomAttribute<StateMachineAttribute>();
        if (attr == null)
            throw new ArgumentException($"Type {machineType} is not marked with StateMachineAttribute");
    }
    
    public IStateMachine<TState, TTrigger> Build(TState initialState)
    {
        // Create instance using reflection (for runtime scenarios)
        var instance = Activator.CreateInstance(_machineType, initialState);
        
        if (instance is not IStateMachine<TState, TTrigger> machine)
            throw new InvalidOperationException($"Type {_machineType} does not implement IStateMachine");
            
        return machine;
    }
}
