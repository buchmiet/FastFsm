using Abstractions.Attributes;
using Abstractions.Fluent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParserComparison.Tests;

public enum PayloadStates { Off, On }
public enum PayloadTriggers { ToggleOn, ToggleOff }
public class TogglePayload { public int Id { get; init; } }

[StateMachine(typeof(PayloadStates), typeof(PayloadTriggers))]
[PayloadType(typeof(TogglePayload))]
public partial class TestPayloadMachineFluentFsm
{
    private readonly List<string> _log = new();
    public IReadOnlyList<string> Log => _log;

    private static void Configure() => FSM
        .State(PayloadStates.Off)
            .On(PayloadTriggers.ToggleOn)
                .Payload<TogglePayload>()
                .Guard(nameof(CanToggleOnAsync))
                .Action(nameof(ToggleOnAsync))
                .GoTo(PayloadStates.On);

    private async ValueTask<bool> CanToggleOnAsync(TogglePayload payload)
    {
        _log.Add($"Guard:Begin:{payload.Id}");
        await Task.Delay(10);
        _log.Add($"Guard:End:{payload.Id}");
        return payload.Id >= 0;
    }

    private async Task ToggleOnAsync(TogglePayload payload)
    {
        _log.Add($"ActionOn:Begin:{payload.Id}");
        await Task.Delay(10);
        _log.Add($"ActionOn:End:{payload.Id}");
    }
}