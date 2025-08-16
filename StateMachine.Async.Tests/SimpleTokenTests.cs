using Abstractions.Attributes;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StateMachine.Async.Tests;

// \n// ====== Minimal token‑aware state machine ======\n//

public enum TokenStates
{
    Off,
    On
}

public enum TokenTriggers
{
    SwitchOn,
    SwitchOff
}

/// <summary>
/// Smallest possible state machine that demonstrates guard and action overloads
/// receiving a <see cref="CancellationToken"/>.
/// </summary>
[StateMachine(typeof(TokenStates), typeof(TokenTriggers))]
public partial class TokenMachine
{
    private readonly List<string> _log = new();
    public IReadOnlyList<string> Log => _log;

    // --- Transition: Off → On --------------------------------------------------

    [Transition(TokenStates.Off, TokenTriggers.SwitchOn, TokenStates.On,
        Guard = nameof(CanSwitchOnAsync),
        Action = nameof(SwitchOnAsync))]
    private async ValueTask<bool> CanSwitchOnAsync(CancellationToken cancellationToken)
    {
        _log.Add("Guard:Begin");
        await Task.Delay(10, cancellationToken);
        _log.Add("Guard:End");
        return true;
    }

    private async Task SwitchOnAsync(CancellationToken cancellationToken)
    {
        _log.Add("ActionOn:Begin");
        await Task.Delay(10, cancellationToken);
        _log.Add("ActionOn:End");
    }

    // --- Transition: On → Off --------------------------------------------------

    [Transition(TokenStates.On, TokenTriggers.SwitchOff, TokenStates.Off,
        Guard = nameof(CanSwitchOffAsync),
        Action = nameof(SwitchOffAsync))]
    private async ValueTask<bool> CanSwitchOffAsync(CancellationToken cancellationToken)
    {
        _log.Add("GuardOff:Begin");
        await Task.Delay(10, cancellationToken);
        _log.Add("GuardOff:End");
        return true;
    }

    private async Task SwitchOffAsync(CancellationToken cancellationToken)
    {
        _log.Add("ActionOff:Begin");
        await Task.Delay(10, cancellationToken);
        _log.Add("ActionOff:End");
    }
}

// \n// ====== Single test exercising the token path ======\n//

public class SimpleTokenTests
{
    [Fact]
    public async Task Should_Transition_With_CancellationToken()
    {
        // Arrange
        var machine = new TokenMachine(TokenStates.Off);
        await machine.StartAsync();

        // Act – explicit token is propagated to guard & action overloads
        var result = await machine.TryFireAsync(TokenTriggers.SwitchOn, cancellationToken: CancellationToken.None);

        // Assert – transition succeeded and callbacks ran in order
        result.ShouldBeTrue();
        machine.CurrentState.ShouldBe(TokenStates.On);
        machine.Log.ShouldBe(new[]
        {
            "Guard:Begin",
            "Guard:End",
            "ActionOn:Begin",
            "ActionOn:End"
        });
    }
}

// ====== Minimal payload‑aware state machine ======

public class TogglePayload
{
    public int Id { get; set; }
}

public enum PayloadStates
{
    Off,
    On
}

public enum PayloadTriggers
{
    ToggleOn,
    ToggleOff
}

/// <summary>
/// Smallest possible state machine that demonstrates guard and action overloads
/// which accept a strongly‑typed payload.
/// </summary>
[StateMachine(typeof(PayloadStates), typeof(PayloadTriggers))]
[PayloadType(typeof(TogglePayload))]
public partial class PayloadMachine
{
    private readonly List<string> _log = new();
    public IReadOnlyList<string> Log => _log;

    // --- Transition: Off → On with payload -----------------------------------

    [Transition(PayloadStates.Off, PayloadTriggers.ToggleOn, PayloadStates.On,
        Guard = nameof(CanToggleOnAsync),
        Action = nameof(ToggleOnAsync))]
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

// ====== Single test exercising the payload path ======

public class SimplePayloadTests
{
    [Fact]
    public async Task Should_Transition_With_Payload()
    {
        // Arrange
        var machine = new PayloadMachine(PayloadStates.Off);
        var payload = new TogglePayload { Id = 42 };
        await machine.StartAsync();

        // Act – payload is propagated to guard & action overloads
        var result = await machine.TryFireAsync(PayloadTriggers.ToggleOn, payload);

        // Assert – transition succeeded and callbacks ran in order
        result.ShouldBeTrue();
        machine.CurrentState.ShouldBe(PayloadStates.On);
        machine.Log.ShouldBe(new[]
        {
            "Guard:Begin:42",
            "Guard:End:42",
            "ActionOn:Begin:42",
            "ActionOn:End:42"
        });
    }
}

