using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests
{
    // Test machine to verify new Fluent API methods
    [StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger))]
    public partial class NewApiTestMachine
    {
        public enum WorkflowState { Idle, Processing, Complete, Failed }
        public enum WorkflowTrigger { Start, UpdateProgress, Finish, Error }
        
        public class JobData 
        { 
            public string JobId { get; set; } = "";
            public int Progress { get; set; }
        }
        
        private static void Configure() => FSM
            .State(WorkflowState.Idle)
                .OnEntryAsync(nameof(OnIdleEntryAsync))
                .On(WorkflowTrigger.Start)
                    .Payload<JobData>()
                    .GuardAsync(nameof(CanStartAsync))
                    .ActionAsync(nameof(StartJobAsync))
                    .GoTo(WorkflowState.Processing)
                    
            .State(WorkflowState.Processing)
                .OnEntryAsync(nameof(OnProcessingEnterAsync))
                .OnExitAsync(nameof(OnProcessingExitAsync))
                .OnInternal(WorkflowTrigger.UpdateProgress)
                    .Payload<JobData>()
                    .Action(nameof(LogProgress))
                    .Internal()
                .On(WorkflowTrigger.Finish)
                    .Action(nameof(Finalize))
                    .GoTo(WorkflowState.Complete)
                .On(WorkflowTrigger.Error)
                    .GoTo(WorkflowState.Failed)
                    
            .At(WorkflowState.Failed)  // Using At() alias
                .OnEntry(nameof(OnFailedEntry))
                
            .State(WorkflowState.Complete)
                .OnExit(nameof(OnCompleteExit));
        
        // Async guard with payload and CT
        private async Task<bool> CanStartAsync(JobData data, CancellationToken ct = default)
        {
            await Task.Delay(10, ct);
            return !string.IsNullOrEmpty(data.JobId);
        }
        
        // Async action with payload and CT
        private async Task StartJobAsync(JobData data, CancellationToken ct = default)
        {
            await Task.Delay(100, ct);
            Console.WriteLine($"Starting job {data.JobId}");
        }
        
        // Async entry without payload
        private async Task OnIdleEntryAsync(CancellationToken ct = default)
        {
            await Task.Delay(5, ct);
            Console.WriteLine("Entered Idle");
        }
        
        // Async entry with payload
        private async Task OnProcessingEnterAsync(JobData data, CancellationToken ct = default)
        {
            await Task.Delay(10, ct);
            Console.WriteLine($"Processing job {data.JobId}");
        }
        
        // Async exit
        private async ValueTask OnProcessingExitAsync(CancellationToken ct = default)
        {
            await Task.Delay(5, ct);
            Console.WriteLine("Exiting Processing");
        }
        
        // Sync action with payload
        private void LogProgress(JobData data)
        {
            Console.WriteLine($"Progress: {data.Progress}%");
        }
        
        // Sync action
        private void Finalize()
        {
            Console.WriteLine("Finalizing...");
        }
        
        // Sync entry
        private void OnFailedEntry()
        {
            Console.WriteLine("Entered Failed state");
        }
        
        // Sync exit
        private void OnCompleteExit()
        {
            Console.WriteLine("Exiting Complete state");
        }
    }
}