namespace FastFsm.Tests.Features.Payload;

// Test Data Classes
public class OrderData
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Customer { get; set; }
}

public class PaymentData
{
    public decimal Amount { get; set; }
}

public class NotificationData
{
    public string Message { get; set; }
    public string[] Recipients { get; set; }
}

public class ProcessConfig
{
    public int ThreadCount { get; set; }
    public int TimeoutSeconds { get; set; }
}

public class ConfigPayload
{
    public string Setting { get; set; }
}

public class DataPayload
{
    public int Value { get; set; }
}

public class ErrorPayload
{
    public string Code { get; set; }
    public string Message { get; set; }
}

public class OverloadPayload
{
    public string Data { get; set; }
}

public class UpdatePayload
{
    public int Increment { get; set; }
}

public class DefaultPayload
{
    public int Id { get; set; }
}

public class SpecialPayload
{
    public string SpecialValue { get; set; }
}

public class ExitPayload
{
    public string Data { get; set; }
}

public class WorkflowPayload
{
    public string WorkflowId { get; set; }
    public int Priority { get; set; }
    public string ApprovedBy { get; set; }
    public string Result { get; set; }
}

public class ConditionalPayload
{
    public bool IsValid { get; set; }
}

public class ExpectedPayload
{
    public string Data { get; set; }
}

public class WrongPayload
{
    public string Wrong { get; set; }
}

// Test State and Trigger Enums
public enum OrderState { New, Submitted, Processing, Completed, Paid, Cancelled, Shipped }
public enum OrderTrigger { Submit, Process, Complete, Pay, Cancel, Ship }

public enum PaymentState { Pending, Processed, Failed }
public enum PaymentTrigger { Process, Retry, Cancel }

public enum NotificationState { Ready, Sent, Failed }
public enum NotificationTrigger { Send, Retry }

public enum ProcessingState { Idle, Running, Completed }
public enum ProcessingTrigger { Start, Stop }

public enum MultiState { Initial, Configured, Processing, Failed }
public enum MultiTrigger { Configure, Process, Error }

public enum OverloadState { A, B }
public enum OverloadTrigger { Go }

public enum InternalPayloadState { Active, Inactive }
public enum InternalPayloadTrigger { Update, Deactivate }

public enum MixedState { Start, Middle, End }
public enum MixedTrigger { Regular, Special }

public enum InitialPayloadState { Start, Next }
public enum InitialPayloadTrigger { Go }

public enum ExitState { A, B }
public enum ExitTrigger { Go }

public enum WorkflowState { Created, Initialized, Submitted, Approved, Completed }
public enum WorkflowTrigger { Initialize, Submit, Approve, Complete }

public enum ConditionalState { Ready, Done }
public enum ConditionalTrigger { Execute }

public enum PermittedState { A, B, C }
public enum PermittedTrigger { Next, Skip }

public enum StrictState { Ready, Processing }
public enum StrictTrigger { Process }