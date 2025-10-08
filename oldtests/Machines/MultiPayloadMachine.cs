using Abstractions.Attributes;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Payloads;

namespace FastFsm.Tests.Machines;



[StateMachine(typeof(MultiState), typeof(MultiTrigger))]
[PayloadType(MultiTrigger.Configure, typeof(ConfigPayload))]
[PayloadType(MultiTrigger.Process, typeof(DataPayload))]
[PayloadType(MultiTrigger.Error, typeof(ErrorPayload))]
public partial class MultiPayloadMachine
{
    public string CurrentSetting { get; private set; }
    public int ProcessedValue { get; private set; }
    public string LastErrorCode { get; private set; }

    [Transition(MultiState.Initial, MultiTrigger.Configure, MultiState.Configured,
        Action = nameof(ApplyConfiguration))]
    [Transition(MultiState.Configured, MultiTrigger.Process, MultiState.Processing,
        Action = nameof(ProcessData))]
    [Transition(MultiState.Processing, MultiTrigger.Error, MultiState.Failed,
        Action = nameof(HandleError))]
    private void Configure() { }

    private void ApplyConfiguration(ConfigPayload config)
    {
        CurrentSetting = config.Setting;
    }

    private void ProcessData(DataPayload data)
    {
        ProcessedValue = data.Value;
    }

    private void HandleError(ErrorPayload error)
    {
        LastErrorCode = error.Code;
    }
}