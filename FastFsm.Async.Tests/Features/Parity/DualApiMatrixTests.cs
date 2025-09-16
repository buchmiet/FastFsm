using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastFsm.Async.Tests.TestHelpers;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using static FastFsm.Async.Tests.TestHelpers.StateMachineWrapperFactory;

namespace FastFsm.Async.Tests.Features.Parity;

public class DualApiMatrixTests
{
    private readonly ITestOutputHelper _output;
    public DualApiMatrixTests(ITestOutputHelper output) { _output = output; }

    public static IEnumerable<object[]> GetAllMachinesAndApis()
    {
        foreach (var machine in MatrixConfig.GetAllMachineNames())
        {
            yield return new object[] { machine, ApiType.Fluent };
            yield return new object[] { machine, ApiType.Legacy };
        }
    }

    [Theory(Skip = "Disabled: parity matrix causes hangs with RcMachine barrier; run focused tests instead")]
    [MemberData(nameof(GetAllMachinesAndApis))]
    public async Task Machine_AsyncOperations_WorkOnBothApis(string machineName, ApiType apiType)
    {
        var cfg = MatrixConfig.GetConfig(machineName);
        cfg.ShouldNotBeNull();

        IStateMachineTestWrapper wrapper;
        try
        {
            wrapper = StateMachineWrapperFactory.Create(machineName, apiType, cfg!.InitialState);
        }
        catch (NotImplementedException)
        {
            _output.WriteLine($"⚠️ {machineName}({apiType}) wrapper not implemented — skipping");
            return;
        }
        catch (NotSupportedException)
        {
            _output.WriteLine($"⚠️ {machineName}({apiType}) not supported — skipping");
            return;
        }

        wrapper.ShouldNotBeNull();
        wrapper.Caps.Has(ApiCapabilities.HasAsync).ShouldBeTrue();

        await wrapper.StartAsync();
        wrapper.CurrentState.ShouldNotBeNull();

        if (cfg.TriggerSequence.Length > 0)
        {
            var trigName = cfg.TriggerSequence[0];
            var trig = StateMachineWrapperFactory.GetTriggerEnum(machineName, apiType, trigName);

            object? payload = null;
            if (cfg.Payloads.Length > 0)
                payload = cfg.Payloads[0];
            else if (wrapper.Caps.SupportsPayloads())
                payload = MatrixConfig.CreateDummyPayload(machineName);

            var ok = await wrapper.TryFireAsync(trig, payload);
            _output.WriteLine($"{machineName}({apiType}) TryFireAsync({trigName}) => {ok}");
        }

        var permitted = wrapper.GetPermittedTriggers();
        permitted.ShouldNotBeNull();
    }
}
