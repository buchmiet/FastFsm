using System.Threading.Tasks;
using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(AgState), typeof(AgTrigger))]
public partial class AsyncGuardStateMachine
{
    public enum AgState { Idle, Busy }
    public enum AgTrigger { Start }

    [Transition(AgState.Idle, AgTrigger.Start, AgState.Busy, Guard = nameof(CanStartAsync))]
    private void Configure() { }

    private async ValueTask<bool> CanStartAsync()
    {
        await Task.Yield();
        return true;
    }
}

