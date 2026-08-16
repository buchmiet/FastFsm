using Abstractions.Attributes;
using Dsl;
using System.Threading;
using System.Threading.Tasks;

namespace ParserComparison.Tests;

/// <summary>
/// Example 2: Multiple Payload Types for different triggers
/// Shows how to use different payload types for different transitions
/// </summary>
[StateMachine(typeof(PaymentState), typeof(PaymentTrigger))]
public partial class FluentPayloadExample2_MultiplePayloads
{
    public enum PaymentState { Pending, Processing, Authorized, Captured, Failed, Refunded }
    public enum PaymentTrigger { Process, Authorize, Capture, Fail, Refund, Retry }

    // Different payload types for different operations
    public sealed class PaymentRequest
    {
        public required string TransactionId { get; init; }
        public decimal Amount { get; init; }
        public required string Currency { get; init; }
        public required string CustomerId { get; init; }
    }

    public sealed class AuthorizationResponse
    {
        public required string AuthCode { get; init; }
        public required string ProcessorId { get; init; }
        public DateTime ExpiresAt { get; init; }
    }

    public sealed class ErrorDetails
    {
        public required string ErrorCode { get; init; }
        public required string Message { get; init; }
        public bool IsRetryable { get; init; }
    }

    public sealed class RefundRequest
    {
        public required string OriginalTransactionId { get; init; }
        public decimal RefundAmount { get; init; }
        public required string Reason { get; init; }
    }

    private static void Configure() => FSM
        .State(PaymentState.Pending)
            .On(PaymentTrigger.Process)
                .Payload<PaymentRequest>()  // Specify payment request payload
                .Guard(nameof(ValidatePayment))
                .Action(nameof(InitiateProcessing))
                .GoTo(PaymentState.Processing)
            .On(PaymentTrigger.Fail)
                .Payload<ErrorDetails>()  // Different payload for failure
                .Action(nameof(RecordFailure))
                .GoTo(PaymentState.Failed)
        
        .State(PaymentState.Processing)
            .OnEntry(nameof(LogProcessingStart))
            .On(PaymentTrigger.Authorize)
                .Payload<AuthorizationResponse>()  // Authorization payload
                .Guard(nameof(ValidateAuthorization))
                .Action(nameof(StoreAuthorization))
                .GoTo(PaymentState.Authorized)
            .On(PaymentTrigger.Fail)
                .Payload<ErrorDetails>()
                .Action(nameof(HandleProcessingError))
                .GoTo(PaymentState.Failed)
        
        .State(PaymentState.Authorized)
            .On(PaymentTrigger.Capture)
                .Payload<PaymentRequest>()  // Reuse PaymentRequest for capture
                .Guard(nameof(CanCapture))
                .Action(nameof(CapturePayment))
                .GoTo(PaymentState.Captured)
            .On(PaymentTrigger.Refund)
                .Payload<RefundRequest>()  // Refund-specific payload
                .Guard(nameof(ValidateRefund))
                .Action(nameof(IssueRefund))
                .GoTo(PaymentState.Refunded)
        
        .State(PaymentState.Captured)
            .On(PaymentTrigger.Refund)
                .Payload<RefundRequest>()
                .Action(nameof(ProcessRefund))
                .GoTo(PaymentState.Refunded)
        
        .State(PaymentState.Failed)
            .OnEntry(nameof(NotifyFailure))
            .On(PaymentTrigger.Retry)
                .Payload<PaymentRequest>()  // Retry with original request
                .Guard(nameof(CanRetry))
                .Action(nameof(PrepareRetry))
                .GoTo(PaymentState.Processing)
        
        .State(PaymentState.Refunded);

    // Guards with specific payload types
    private bool ValidatePayment(PaymentRequest request) =>
        request.Amount > 0 && !string.IsNullOrEmpty(request.Currency);

    private bool ValidateAuthorization(AuthorizationResponse auth) =>
        !string.IsNullOrEmpty(auth.AuthCode) && auth.ExpiresAt > DateTime.UtcNow;

    private bool CanCapture(PaymentRequest request) =>
        request.Amount > 0;

    private bool ValidateRefund(RefundRequest refund) =>
        refund.RefundAmount > 0 && !string.IsNullOrEmpty(refund.Reason);

    private bool CanRetry(PaymentRequest request)
    {
        // Business logic for retry
        return true;
    }

    // Actions with specific payload types
    private void InitiateProcessing(PaymentRequest request)
    {
        Console.WriteLine($"Processing payment {request.TransactionId} for {request.Amount} {request.Currency}");
    }

    private void RecordFailure(ErrorDetails error)
    {
        Console.WriteLine($"Payment failed: {error.ErrorCode} - {error.Message}");
    }

    private void StoreAuthorization(AuthorizationResponse auth)
    {
        Console.WriteLine($"Stored auth code {auth.AuthCode} from processor {auth.ProcessorId}");
    }

    private void HandleProcessingError(ErrorDetails error)
    {
        Console.WriteLine($"Processing error: {error.ErrorCode}, Retryable: {error.IsRetryable}");
    }

    private void CapturePayment(PaymentRequest request)
    {
        Console.WriteLine($"Capturing {request.Amount} {request.Currency}");
    }

    private void IssueRefund(RefundRequest refund)
    {
        Console.WriteLine($"Issuing refund of {refund.RefundAmount} for transaction {refund.OriginalTransactionId}");
    }

    private void ProcessRefund(RefundRequest refund)
    {
        Console.WriteLine($"Processing refund: {refund.Reason}");
    }

    private void PrepareRetry(PaymentRequest request)
    {
        Console.WriteLine($"Preparing to retry transaction {request.TransactionId}");
    }

    // OnEntry callbacks
    private void LogProcessingStart()
    {
        Console.WriteLine("Started processing");
    }

    private void NotifyFailure()
    {
        Console.WriteLine("Payment has failed");
    }
}