namespace Machines.Tests.Payloads;

public class WorkflowPayload
{
    public string WorkflowId { get; set; } = null!;
    public int Priority { get; set; }
    public string ApprovedBy { get; set; } = null!;
    public string Result { get; set; } = null!;
}