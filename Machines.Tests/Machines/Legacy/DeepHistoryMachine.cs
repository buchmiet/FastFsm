using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(DeepHistoryMachine_S), typeof(DeepHistoryMachine_T), EnableHierarchy = true)]
public partial class DeepHistoryMachine
{
    [State(DeepHistoryMachine_S.Work, History = HistoryMode.Deep)] private void Work() { }
    [State(DeepHistoryMachine_S.Work_S1, Parent = DeepHistoryMachine_S.Work, IsInitial = true)] private void S1() { }
    [State(DeepHistoryMachine_S.Work_S1_Loading, Parent = DeepHistoryMachine_S.Work_S1, IsInitial = true)] private void Loading() { }
    [State(DeepHistoryMachine_S.Work_S1_Calc, Parent = DeepHistoryMachine_S.Work_S1)] private void Calc() { }

    [Transition(DeepHistoryMachine_S.Out, DeepHistoryMachine_T.EnterWork, DeepHistoryMachine_S.Work)]
    [Transition(DeepHistoryMachine_S.Work_S1_Loading, DeepHistoryMachine_T.Next, DeepHistoryMachine_S.Work_S1_Calc)]
    [Transition(DeepHistoryMachine_S.Work, DeepHistoryMachine_T.Abort, DeepHistoryMachine_S.Out)]
    private void Configure() { }
}
