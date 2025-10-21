using Abstractions.Fluent;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(KeywordState), typeof(KeywordTrigger))]
public partial class KeywordStateMachineFluent
{
    private void Configure() => FSM
        .State(KeywordState.@class)
        .On(KeywordTrigger.@goto).GoTo(KeywordState.@return)
        .State(KeywordState.@return)
        .On(KeywordTrigger.@continue).GoTo(KeywordState.@void)
        .State(KeywordState.@void)
        .On(KeywordTrigger.@break).GoTo(KeywordState.@int)
        .State(KeywordState.@int)
        .On(KeywordTrigger.@new).GoTo(KeywordState.@interface)
        .State(KeywordState.@interface)
        .On(KeywordTrigger.@throw).GoTo(KeywordState.@namespace);
}