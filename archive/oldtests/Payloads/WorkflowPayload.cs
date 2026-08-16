namespace FastFsm.Tests.Payloads;

public class WorkflowPayload
{
    public string WorkflowId { get; set; }
    public int Priority { get; set; }
    public string ApprovedBy { get; set; }
    public string Result { get; set; }
}