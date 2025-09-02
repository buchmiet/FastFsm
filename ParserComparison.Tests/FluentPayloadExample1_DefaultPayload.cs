using Abstractions.Attributes;
using Abstractions.Fluent;
using System.Threading;
using System.Threading.Tasks;

namespace ParserComparison.Tests;

/// <summary>
/// Example 1: Default Payload Type for all transitions
/// Shows how to use a single payload type across the entire state machine
/// </summary>
[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), DefaultPayloadType = typeof(WorkflowContext))]
public partial class FluentPayloadExample1_DefaultPayload
{
    public enum WorkflowState { Draft, Review, Approved, Published, Rejected }
    public enum WorkflowTrigger { Submit, Approve, Publish, Reject, Revise }

    public sealed class WorkflowContext
    {
        public required string DocumentId { get; init; }
        public required string UserId { get; init; }
        public string? Comments { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    private static void Configure() => FSM
        .State(WorkflowState.Draft)
            .OnEntry(nameof(LogEntry))
            .On(WorkflowTrigger.Submit)
                // No need to specify .Payload<WorkflowContext>() - it's the default
                .Guard(nameof(CanSubmit))
                .Action(nameof(RecordSubmission))
                .GoTo(WorkflowState.Review)
        
        .State(WorkflowState.Review)
            .OnEntry(nameof(NotifyReviewers))
            .On(WorkflowTrigger.Approve)
                // Default payload is automatically used
                .Guard(nameof(HasApprovalAuthority))
                .Action(nameof(RecordApproval))
                .GoTo(WorkflowState.Approved)
            .On(WorkflowTrigger.Reject)
                .Action(nameof(RecordRejection))
                .GoTo(WorkflowState.Rejected)
        
        .State(WorkflowState.Approved)
            .On(WorkflowTrigger.Publish)
                .Guard(nameof(CanPublish))
                .Action(nameof(PublishDocument))
                .GoTo(WorkflowState.Published)
            .On(WorkflowTrigger.Revise)
                .Action(nameof(RequestRevision))
                .GoTo(WorkflowState.Draft)
        
        .State(WorkflowState.Published)
            .OnExit(nameof(LogExit))  // Note: OnExit never receives payload
        
        .State(WorkflowState.Rejected)
            .On(WorkflowTrigger.Revise)
                .Action(nameof(StartRevision))
                .GoTo(WorkflowState.Draft);

    // Guards with default payload
    private bool CanSubmit(WorkflowContext context) => 
        !string.IsNullOrEmpty(context.DocumentId);

    private bool HasApprovalAuthority(WorkflowContext context) =>
        context.UserId.StartsWith("ADMIN");

    private bool CanPublish(WorkflowContext context) =>
        context.Timestamp.AddHours(1) < DateTime.UtcNow; // 1 hour cooldown

    // Actions with default payload
    private void RecordSubmission(WorkflowContext context)
    {
        Console.WriteLine($"Document {context.DocumentId} submitted by {context.UserId}");
    }

    private void RecordApproval(WorkflowContext context)
    {
        Console.WriteLine($"Approved by {context.UserId}: {context.Comments}");
    }

    private void RecordRejection(WorkflowContext context)
    {
        Console.WriteLine($"Rejected by {context.UserId}: {context.Comments}");
    }

    private void PublishDocument(WorkflowContext context)
    {
        Console.WriteLine($"Publishing document {context.DocumentId}");
    }

    private void RequestRevision(WorkflowContext context)
    {
        Console.WriteLine($"Revision requested for {context.DocumentId}");
    }

    private void StartRevision(WorkflowContext context)
    {
        Console.WriteLine($"Starting revision of {context.DocumentId}");
    }

    // OnEntry with payload (receives payload from triggering transition)
    private void LogEntry(WorkflowContext context)
    {
        Console.WriteLine($"Entered state with document {context.DocumentId}");
    }

    private void NotifyReviewers(WorkflowContext context)
    {
        Console.WriteLine($"Notifying reviewers about {context.DocumentId}");
    }

    // OnExit without payload (OnExit never receives payload)
    private void LogExit()
    {
        Console.WriteLine("Exiting state");
    }
}