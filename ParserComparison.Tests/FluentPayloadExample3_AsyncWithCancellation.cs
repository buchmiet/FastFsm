using Abstractions.Attributes;
using Abstractions.Fluent;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ParserComparison.Tests;

/// <summary>
/// Example 3: Async methods with Payload and CancellationToken
/// Shows how payload works with async operations and cancellation tokens
/// </summary>
[StateMachine(typeof(DownloadState), typeof(DownloadTrigger))]
public partial class FluentPayloadExample3_AsyncWithCancellation
{
    public enum DownloadState { Idle, Downloading, Paused, Completed, Failed }
    public enum DownloadTrigger { Start, Pause, Resume, Complete, Fail, Cancel }

    // Payload types
    public sealed class DownloadRequest
    {
        public required string Url { get; init; }
        public required string DestinationPath { get; init; }
        public long FileSize { get; init; }
        public int ChunkSize { get; init; } = 8192;
    }

    public sealed class ProgressUpdate
    {
        public long BytesDownloaded { get; init; }
        public long TotalBytes { get; init; }
        public double Percentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
        public TimeSpan ElapsedTime { get; init; }
    }

    public sealed class DownloadError
    {
        public required string ErrorMessage { get; init; }
        public Exception? Exception { get; init; }
        public bool CanRetry { get; init; }
    }

    private long _bytesDownloaded;
    private string? _currentUrl;

    private static void Configure() => FSM
        .State(DownloadState.Idle)
            .OnEntryAsync(nameof(InitializeAsync))
            .On(DownloadTrigger.Start)
                .Payload<DownloadRequest>()
                .GuardAsync(nameof(CanStartDownloadAsync))  // Async guard with payload
                .ActionAsync(nameof(StartDownloadAsync))     // Async action with payload
                .GoTo(DownloadState.Downloading)
        
        .State(DownloadState.Downloading)
            .OnEntryAsync(nameof(OnDownloadingEntryAsync))  // Async OnEntry
            .OnExitAsync(nameof(OnDownloadingExitAsync))    // Async OnExit (no payload)
            .On(DownloadTrigger.Pause)
                .ActionAsync(nameof(PauseDownloadAsync))
                .GoTo(DownloadState.Paused)
            .On(DownloadTrigger.Complete)
                .Payload<ProgressUpdate>()
                .GuardAsync(nameof(ValidateCompletionAsync))
                .ActionAsync(nameof(FinalizeDownloadAsync))
                .GoTo(DownloadState.Completed)
            .On(DownloadTrigger.Fail)
                .Payload<DownloadError>()
                .ActionAsync(nameof(HandleDownloadErrorAsync))
                .GoTo(DownloadState.Failed)
            .OnInternal(DownloadTrigger.Cancel)  // Internal transition with async action
                .ActionAsync(nameof(CancelCurrentChunkAsync))
                .Internal()
        
        .State(DownloadState.Paused)
            .On(DownloadTrigger.Resume)
                .Payload<DownloadRequest>()
                .GuardAsync(nameof(CanResumeAsync))
                .ActionAsync(nameof(ResumeDownloadAsync))
                .GoTo(DownloadState.Downloading)
            .On(DownloadTrigger.Cancel)
                .ActionAsync(nameof(CancelDownloadAsync))
                .GoTo(DownloadState.Idle)
        
        .State(DownloadState.Completed)
            .OnEntryAsync(nameof(CleanupAsync))
        
        .State(DownloadState.Failed)
            .On(DownloadTrigger.Start)
                .Payload<DownloadRequest>()
                .ActionAsync(nameof(RetryDownloadAsync))
                .GoTo(DownloadState.Downloading);

    // Async guards with payload and CancellationToken
    private async ValueTask<bool> CanStartDownloadAsync(DownloadRequest request, CancellationToken ct)
    {
        // Validate URL is accessible
        await Task.Delay(100, ct); // Simulate network check
        return Uri.TryCreate(request.Url, UriKind.Absolute, out _);
    }

    private async ValueTask<bool> ValidateCompletionAsync(ProgressUpdate progress, CancellationToken ct)
    {
        await Task.Delay(50, ct); // Simulate validation
        return progress.Percentage >= 100;
    }

    private async ValueTask<bool> CanResumeAsync(DownloadRequest request, CancellationToken ct)
    {
        // Check if partial file exists
        await Task.Delay(10, ct);
        return _bytesDownloaded > 0 && _currentUrl == request.Url;
    }

    // Async actions with payload and CancellationToken
    private async Task StartDownloadAsync(DownloadRequest request, CancellationToken ct)
    {
        _currentUrl = request.Url;
        _bytesDownloaded = 0;
        Console.WriteLine($"Starting download from {request.Url}");
        
        // Simulate download initialization
        await Task.Delay(200, ct);
    }

    private async Task FinalizeDownloadAsync(ProgressUpdate progress, CancellationToken ct)
    {
        Console.WriteLine($"Download completed: {progress.BytesDownloaded} bytes in {progress.ElapsedTime}");
        await Task.Delay(100, ct); // Simulate finalization
    }

    private async Task HandleDownloadErrorAsync(DownloadError error, CancellationToken ct)
    {
        Console.WriteLine($"Download error: {error.ErrorMessage}");
        if (error.Exception != null)
        {
            Console.WriteLine($"Exception: {error.Exception.Message}");
        }
        await Task.Delay(50, ct);
    }

    private async Task RetryDownloadAsync(DownloadRequest request, CancellationToken ct)
    {
        Console.WriteLine($"Retrying download from {request.Url}");
        _bytesDownloaded = 0;
        await Task.Delay(100, ct);
    }

    private async Task ResumeDownloadAsync(DownloadRequest request, CancellationToken ct)
    {
        Console.WriteLine($"Resuming download from byte {_bytesDownloaded}");
        await Task.Delay(100, ct);
    }

    // Async actions without payload (but with CancellationToken)
    private async Task PauseDownloadAsync(CancellationToken ct)
    {
        Console.WriteLine("Pausing download...");
        await Task.Delay(50, ct);
    }

    private async Task CancelDownloadAsync(CancellationToken ct)
    {
        Console.WriteLine("Cancelling download...");
        _currentUrl = null;
        _bytesDownloaded = 0;
        await Task.Delay(100, ct);
    }

    private async Task CancelCurrentChunkAsync(CancellationToken ct)
    {
        Console.WriteLine("Cancelling current chunk...");
        await Task.Delay(20, ct);
    }

    // Async OnEntry/OnExit callbacks
    private async Task InitializeAsync(CancellationToken ct)
    {
        Console.WriteLine("Initializing download manager...");
        await Task.Delay(100, ct);
    }

    private async Task OnDownloadingEntryAsync(CancellationToken ct)
    {
        Console.WriteLine("Entered downloading state");
        await Task.Delay(10, ct);
    }

    private async ValueTask OnDownloadingExitAsync()  // OnExit never receives payload
    {
        Console.WriteLine("Exiting downloading state");
        await Task.Delay(10);
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        Console.WriteLine("Cleaning up temporary files...");
        await Task.Delay(50, ct);
    }
}