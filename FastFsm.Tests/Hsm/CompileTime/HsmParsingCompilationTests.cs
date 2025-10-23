using Machines.Tests.Machines;
using Machines.Tests.Machines.Legacy;
using Xunit;

namespace FastFsm.Tests.Hsm.CompileTime;

/// <summary>
/// HSM Parsing Compilation Tests
/// This file contains ONLY VALID HSM configurations that should compile successfully.
/// All machines here represent correct usage of HSM attributes.
/// Invalid/error cases should be tested in a separate diagnostics project.
/// </summary>
public partial class HsmParsingCompilationTests
{

    #region Compilation Tests

    [Fact]
    public void AllHsmMachinesShouldCompile()
    {
        // This test passes if all the state machines compile successfully
        // The actual compilation happens at build time
        Assert.True(true, "All HSM parsing tests compiled successfully");
    }

    [Fact]
    public void SimpleParentChildMachineCanBeInstantiated()
    {
        var machine = new SimpleParentChildMachine(HsmState.Idle);
        Assert.NotNull(machine);
        Assert.Equal(HsmState.Idle, machine.CurrentState);
    }

    [Fact]
    public void DeepHierarchyMachineCanBeInstantiated()
    {
        var machine = new DeepHierarchyMachine(HsmState.Working);
        Assert.NotNull(machine);
        Assert.Equal(HsmState.Working, machine.CurrentState);
    }

    [Fact]
    public void ShallowHistoryMachineCanBeInstantiated()
    {
        var machine = new ShallowHistoryMachine(ShallowHistoryMachine_S.Menu);
        Assert.NotNull(machine);
    }

    [Fact]
    public void DeepHistoryMachineCanBeInstantiated()
    {
        var machine = new DeepHistoryMachine(DeepHistoryMachine_S.Out);
        Assert.NotNull(machine);
    }

    [Fact]
    public void PriorityTransitionMachineCanBeInstantiated()
    {
        var machine = new PriorityTransitionMachine(HsmState.Priority_Low);
        Assert.NotNull(machine);
    }

    [Fact]
    public void InternalTransitionMachineCanBeInstantiated()
    {
        var machine = new InternalTransitionMachine(InternalState.Active);
        Assert.NotNull(machine);
    }

    [Fact]
    public void CrossHierarchyMachineCanBeInstantiated()
    {
        var machine = new CrossHierarchyMachine(HsmState.Branch1);
        Assert.NotNull(machine);
    }

    [Fact]
    public void ComplexMixedScenarioMachineCanBeInstantiated()
    {
        var machine = new ComplexMixedScenarioMachine(HsmState.ComplexParent);
        Assert.NotNull(machine);
    }

    [Fact]
    public void InitialStateMachineCanBeInstantiated()
    {
        var machine = new InitialStateMachine(InitialState.Start);
        Assert.NotNull(machine);
    }

    [Fact]
    public void EdgeCaseMachineCanBeInstantiated()
    {
        var machine = new EdgeCaseMachine(HsmState.EdgeParent);
        Assert.NotNull(machine);
    }

    #endregion
}
