using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Abstractions.Attributes;
using FastFsm.Contracts;

namespace FastFsm.Builder;


public class StateMachineBuilder<TState, TTrigger> : IStateMachineBuilder<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _machineType;
    
    public StateMachineBuilder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type machineType)
    {
        _machineType = machineType ?? throw new ArgumentNullException(nameof(machineType));
        
        // Verify the type has the StateMachine attribute
        var attr = _machineType.GetCustomAttribute<StateMachineAttribute>();
        if (attr == null)
            throw new ArgumentException($"Type {machineType} is not marked with StateMachineAttribute");
    }
    
    public object Build(TState initialState)
    {
        // Create instance using reflection (for runtime scenarios)
        var instance = Activator.CreateInstance(_machineType, initialState);
        
        // Instance could be either IStateMachineSync or IStateMachineAsync
        if (instance is not IStateMachineSync<TState, TTrigger> && 
            instance is not IStateMachineAsync<TState, TTrigger>)
            throw new InvalidOperationException($"Type {_machineType} does not implement IStateMachineSync or IStateMachineAsync");
            
        return instance;
    }
}
