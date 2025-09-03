using Abstractions.Attributes;

namespace ParserComparison.Tests
{
    [StateMachine(typeof(PayloadState), typeof(PayloadTrigger))]
    public partial class TestFluentPayloadMachine
    {
        public enum PayloadState { Ready, Processing, Complete }
        public enum PayloadTrigger { Submit, Process, Finish }

        public sealed class SubmitData 
        { 
            public required string Id { get; init; }
            public int Priority { get; init; }
        }

        public sealed class ProcessData 
        { 
            public int ItemCount { get; init; } 
        }

        public string? LastSubmitId { get; private set; }
        public int ProcessedItems { get; private set; }

        private static void Configure() => FSM
            .State(PayloadState.Ready)
                .On(PayloadTrigger.Submit)
                    .Payload<SubmitData>()
                    .Guard(nameof(ValidateSubmit))
                    .Action(nameof(HandleSubmit))
                    .GoTo(PayloadState.Processing)
            .State(PayloadState.Processing)
                .On(PayloadTrigger.Process)
                    .Payload<ProcessData>()
                    .Action(nameof(ProcessItems))
                    .GoTo(PayloadState.Processing)  // Self-transition
                .On(PayloadTrigger.Finish)
                    .GoTo(PayloadState.Complete)
            .State(PayloadState.Complete);

        public bool ValidateSubmit(SubmitData data) => !string.IsNullOrEmpty(data.Id);
        public void HandleSubmit(SubmitData data) => LastSubmitId = data.Id;
        public void ProcessItems(ProcessData data) => ProcessedItems += data.ItemCount;
    }
}