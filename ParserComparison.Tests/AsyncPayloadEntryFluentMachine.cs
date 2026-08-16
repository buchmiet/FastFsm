using System.Threading.Tasks;
using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

[StateMachine(typeof(ApefState), typeof(ApefTrigger), DefaultPayloadType = typeof(Payload))]
public partial class AsyncPayloadEntryFluentMachine
{
    public enum ApefState { Init }
    public enum ApefTrigger { X }

    private static void Configure() => FSM
        .State(ApefState.Init).OnEntry(nameof(OnInitAsync));

    private async ValueTask OnInitAsync(Payload p)
    {
        await Task.Yield();
    }

    public sealed class Payload { public int V { get; init; } }
}

