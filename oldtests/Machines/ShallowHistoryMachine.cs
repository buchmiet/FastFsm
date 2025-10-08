using Abstractions.Attributes;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(ShallowHistoryMachine_S), typeof(ShallowHistoryMachine_T), EnableHierarchy = true)]
public partial class ShallowHistoryMachine
{
    [State(ShallowHistoryMachine_S.Menu, History = HistoryMode.Shallow)] private void Menu() { }
    [State(ShallowHistoryMachine_S.Menu_Main, Parent = ShallowHistoryMachine_S.Menu, IsInitial = true)] private void Main() { }
    [State(ShallowHistoryMachine_S.Menu_Settings, Parent = ShallowHistoryMachine_S.Menu)] private void Settings() { }

    [Transition(ShallowHistoryMachine_S.Outside, ShallowHistoryMachine_T.Enter, ShallowHistoryMachine_S.Menu)]
    [Transition(ShallowHistoryMachine_S.Menu_Main, ShallowHistoryMachine_T.Next, ShallowHistoryMachine_S.Menu_Settings)]
    [Transition(ShallowHistoryMachine_S.Menu_Settings, ShallowHistoryMachine_T.Back, ShallowHistoryMachine_S.Menu_Main)]
    [Transition(ShallowHistoryMachine_S.Menu, ShallowHistoryMachine_T.Exit, ShallowHistoryMachine_S.Outside)]
    private void Configure() { }
}
