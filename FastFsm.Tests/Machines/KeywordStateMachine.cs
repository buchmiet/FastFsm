using Abstractions.Attributes;
using static FastFsm.Tests.Features.EdgeCases.NameCollisionTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(KeywordState), typeof(KeywordTrigger))]
    public partial class KeywordStateMachine
    {
        [Transition(KeywordState.@class, KeywordTrigger.@goto, KeywordState.@return)]
        [Transition(KeywordState.@return, KeywordTrigger.@continue, KeywordState.@void)]
        [Transition(KeywordState.@void, KeywordTrigger.@break, KeywordState.@int)]
        [Transition(KeywordState.@int, KeywordTrigger.@new, KeywordState.@interface)]
        [Transition(KeywordState.@interface, KeywordTrigger.@throw, KeywordState.@namespace)]
        private void Configure() { }
    }
}
