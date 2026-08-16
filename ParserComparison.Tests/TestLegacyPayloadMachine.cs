using Abstractions.Attributes;

namespace ParserComparison.Tests
{
    [StateMachine(typeof(PayloadState), typeof(PayloadTrigger))]
    public partial class TestLegacyPayloadMachine
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

        [Transition(PayloadState.Ready, PayloadTrigger.Submit, PayloadState.Processing,
            Guard = nameof(ValidateSubmit), Action = nameof(HandleSubmit))]
        [PayloadType(PayloadTrigger.Submit, typeof(SubmitData))]
        [Transition(PayloadState.Processing, PayloadTrigger.Process, PayloadState.Processing,  // Self-transition
            Action = nameof(ProcessItems))]
        [PayloadType(PayloadTrigger.Process, typeof(ProcessData))]
        [Transition(PayloadState.Processing, PayloadTrigger.Finish, PayloadState.Complete)]
        private void Configure() { }

        public bool ValidateSubmit(SubmitData data) => !string.IsNullOrEmpty(data.Id);
        public void HandleSubmit(SubmitData data) => LastSubmitId = data.Id;
        public void ProcessItems(ProcessData data) => ProcessedItems += data.ItemCount;
    }
}