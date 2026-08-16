using System.Threading.Tasks;
using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(ApesState), typeof(ApesTrigger), DefaultPayloadType = typeof(Payload))]
public partial class AsyncPayloadEntryStateMachine
{
    public enum ApesState { Init }
    public enum ApesTrigger { X }

    [State(ApesState.Init, OnEntry = nameof(OnInitAsync))]
    private void ConfigureStates() { }

    private async ValueTask OnInitAsync(Payload p)
    {
        await Task.Yield();
    }

    public sealed class Payload { public int V { get; init; } }
}

