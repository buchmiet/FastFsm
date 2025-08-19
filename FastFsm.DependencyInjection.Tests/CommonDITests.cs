using Microsoft.Extensions.DependencyInjection;
using StateMachine.Contracts;
using StateMachine.Tests.DI;
using StateMachine.Tests.DI.TestMachines;

namespace StateMachine.DependencyInjection.Tests;

/// <summary>
/// Tests that should pass for all state machine variants
/// </summary>
public class CommonDITests : DITestBase
{
    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void Machine_CanBeRegisteredWithDifferentLifetimes(ServiceLifetime lifetime)
    {
        // Arrange & Act
        Services.AddPureTestMachine(TestState.A, lifetime);

        // Assert
        AssertServiceRegistered<IPureTestMachine>();
        AssertServiceLifetime<IPureTestMachine>(lifetime);
    }

    [Fact]
    public void Machine_SingletonByDefault()
    {
        // Arrange & Act
        Services.AddPureTestMachine(TestState.A);

        // Assert
        AssertServiceLifetime<IPureTestMachine>(ServiceLifetime.Singleton);
    }

    [Fact]
    public void Machine_SingletonReturnsSameInstance()
    {
        // Arrange
        Services.AddPureTestMachine(TestState.A, ServiceLifetime.Singleton);
        BuildProvider();

        // Act
        var instance1 = GetService<IPureTestMachine>();
        instance1.Start();
        var instance2 = GetService<IPureTestMachine>();
        instance2.Start();

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Machine_TransientReturnsNewInstance()
    {
        // Arrange
        Services.AddPureTestMachine(TestState.A, ServiceLifetime.Transient);
        BuildProvider();

        // Act
        var instance1 = GetService<IPureTestMachine>();
        instance1.Start();
        var instance2 = GetService<IPureTestMachine>();
        instance2.Start();

        // Assert
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void Machine_ScopedReturnsSameInstanceInScope()
    {
        // Arrange
        Services.AddPureTestMachine(TestState.A, ServiceLifetime.Scoped);
        BuildProvider();

        // Act & Assert - Same instance within scope
        using (var scope1 = CreateScope())
        {
            var instance1 = scope1.ServiceProvider.GetRequiredService<IPureTestMachine>();
            instance1.Start();
            var instance2 = scope1.ServiceProvider.GetRequiredService<IPureTestMachine>();
            instance2.Start();
            Assert.Same(instance1, instance2);
        }

        // Act & Assert - Different instance in different scope
        IPureTestMachine? scopedInstance1 = null;
        IPureTestMachine? scopedInstance2 = null;

        using (var scope1 = CreateScope())
        {
            scopedInstance1 = scope1.ServiceProvider.GetRequiredService<IPureTestMachine>();
            scopedInstance1.Start();
        }

        using (var scope2 = CreateScope())
        {
            scopedInstance2 = scope2.ServiceProvider.GetRequiredService<IPureTestMachine>();
            scopedInstance2.Start();
        }

        Assert.NotSame(scopedInstance1, scopedInstance2);
    }

    [Fact]
    public void Machine_UsesProvidedInitialState()
    {
        // Arrange
        const TestState expectedState = TestState.B;
        Services.AddPureTestMachine(expectedState);
        BuildProvider();

        // Act
        var machine = GetService<IPureTestMachine>();
        machine.Start();

        // Assert
        Assert.Equal(expectedState, machine.CurrentState);
    }

    [Fact]
    public void Machine_UsesInitialStateFactory()
    {
        // Arrange
        const TestState expectedState = TestState.C;
        Services.AddPureTestMachine(provider => expectedState);
        BuildProvider();

        // Act
        var machine = GetService<IPureTestMachine>();
        machine.Start();

        // Assert
        Assert.Equal(expectedState, machine.CurrentState);
    }

    [Fact]
    public void Machine_UsesInitialStateProvider()
    {
        // Arrange
        const TestState expectedState = TestState.D;
        Services.ConfigureStateMachineInitialState<TestState>(provider => expectedState);
        Services.AddPureTestMachine(); // No explicit initial state
        BuildProvider();

        // Act
        var machine = GetService<IPureTestMachine>();
        machine.Start();

        // Assert
        Assert.Equal(expectedState, machine.CurrentState);
    }

    [Fact]
    public void Machine_DefaultsToFirstEnumValue_WhenNoInitialStateProvided()
    {
        // Arrange
        Services.AddPureTestMachine(); // No initial state
        BuildProvider();

        // Act
        var machine = GetService<IPureTestMachine>();

        // Assert
        Assert.Equal(TestState.A, machine.CurrentState); // First enum value
    }

    [Fact]
    public void MultipleMachines_CanBeRegistered()
    {
        // Arrange
        Services.AddPureTestMachine(TestState.A);
        Services.AddBasicTestMachine(TestState.B);
        BuildProvider();

        // Act
        var pureMachine = GetService<IPureTestMachine>();
        pureMachine.Start();
        var basicMachine = GetService<IBasicTestMachine>();
        basicMachine.Start();

        // Assert
        Assert.NotNull(pureMachine);
        Assert.NotNull(basicMachine);
        Assert.Equal(TestState.A, pureMachine.CurrentState);
        Assert.Equal(TestState.B, basicMachine.CurrentState);
    }

    [Fact]
    public void Factory_IsRegisteredAutomatically()
    {
        // Arrange
        Services.AddPureTestMachine(TestState.A);
        BuildProvider();

        // Act & Assert
        var factory = GetService<IStateMachineFactory<IPureTestMachine, TestState, TestTrigger>>();
        Assert.NotNull(factory);

        // Factory should create instances
        var machine = factory.Create(TestState.C);
        machine.Start();
        Assert.Equal(TestState.C, machine.CurrentState);
    }

    [Fact]
    public void Machine_WorksAfterDI()
    {
        // Arrange
        Services.AddPureTestMachine(TestState.A);
        BuildProvider();

        // Act
        var machine = GetService<IPureTestMachine>();
        machine.Start();
        var result = machine.TryFire(TestTrigger.Next);

        // Assert
        Assert.True(result);
        Assert.Equal(TestState.B, machine.CurrentState);
    }
}