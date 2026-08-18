using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Fsm.Performance
{
    public class BenchmarkTests(ITestOutputHelper output)
    {
        [Fact]
        public void Core_MillionTransitions_PerformanceTest()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkState.A);
            machine.Start();
            const int iterations = 1_000_000;

            // Warmup
            for (int i = 0; i < 1000; i++)
            {
                machine.TryFire(BenchmarkTrigger.Next);
            }

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                machine.TryFire(BenchmarkTrigger.Next);
            }
            sw.Stop();

            // Assert & Report
            var nsPerTransition = sw.Elapsed.TotalNanoseconds / iterations;
            var transitionsPerSecond = iterations / sw.Elapsed.TotalSeconds;

            output.WriteLine($"Core (baseline) Performance:");
            output.WriteLine($"  Total time: {sw.ElapsedMilliseconds}ms");
            output.WriteLine($"  Per transition: {nsPerTransition:F1}ns");
            output.WriteLine($"  Throughput: {transitionsPerSecond:N0} transitions/sec");

            // Performance assertions
            Assert.True(nsPerTransition < 100, $"Transition took {nsPerTransition}ns, expected < 100ns");
            Assert.True(transitionsPerSecond > 10_000_000, $"Only {transitionsPerSecond:N0} transitions/sec, expected > 10M");
        }

        [Fact]
        public void CoreWithCallbacks_MillionTransitions_PerformanceTest()
        {
            // Arrange
            var machine = new BasicBenchmarkMachine(BenchmarkState.A);
            machine.Start();
            const int iterations = 1_000_000;

            // Warmup
            for (int i = 0; i < 1000; i++)
            {
                machine.TryFire(BenchmarkTrigger.Next);
            }

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                machine.TryFire(BenchmarkTrigger.Next);
            }
            sw.Stop();

            // Assert & Report
            var nsPerTransition = sw.Elapsed.TotalNanoseconds / iterations;
            var transitionsPerSecond = iterations / sw.Elapsed.TotalSeconds;

            output.WriteLine($"Core+Callbacks Performance (with OnEntry/OnExit):");
            output.WriteLine($"  Total time: {sw.ElapsedMilliseconds}ms");
            output.WriteLine($"  Per transition: {nsPerTransition:F1}ns");
            output.WriteLine($"  Throughput: {transitionsPerSecond:N0} transitions/sec");

            // Basic variant should still be very fast
            Assert.True(nsPerTransition < 200, $"Transition took {nsPerTransition}ns, expected < 200ns");
            Assert.True(transitionsPerSecond > 5_000_000, $"Only {transitionsPerSecond:N0} transitions/sec, expected > 5M");
        }

        [Fact]
        public void CompareCoreVsCoreWithCallbacks_PerformanceOverhead()
        {
            const int iterations = 100_000;

            // Core baseline
            var pureMachine = new CoreBenchmarkMachine(BenchmarkState.A);
            pureMachine.Start();
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                pureMachine.TryFire(BenchmarkTrigger.Next);
            }
            sw1.Stop();
            var pureTime = sw1.Elapsed.TotalMilliseconds;

            // Core + callbacks
            var basicMachine = new BasicBenchmarkMachine(BenchmarkState.A);
            basicMachine.Start();
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                basicMachine.TryFire(BenchmarkTrigger.Next);
            }
            sw2.Stop();
            var basicTime = sw2.Elapsed.TotalMilliseconds;

            // Calculate overhead
            var overhead = ((basicTime - pureTime) / pureTime) * 100;

            output.WriteLine($"Feature Comparison ({iterations:N0} transitions):");
            output.WriteLine($"  Core:           {pureTime:F2}ms");
            output.WriteLine($"  Core+Callbacks: {basicTime:F2}ms");
            output.WriteLine($"  Overhead: {overhead:F1}%");

            // Basic should have minimal overhead (< 50%)
            Assert.True(overhead < 50, $"Basic variant has {overhead:F1}% overhead, expected < 50%");
        }

        [Fact]
        public void GuardEvaluation_PerformanceImpact_Improved()
        {
            const int iterations = 10_000_000;
            const int warmupIterations = 100_000;
            const int pairedRuns = 9;

            WarmupGuardBenchmarkMachines(warmupIterations);

            var overheadSamples = new List<double>(pairedRuns);
            var noGuardNsSamples = new List<double>(pairedRuns);
            var withGuardNsSamples = new List<double>(pairedRuns);

            for (int run = 0; run < pairedRuns; run++)
            {
                PrepareGuardBenchmarkMeasurement();

                var noGuardMachine = CreateNoGuardBenchmarkMachine();
                var withGuardMachine = CreateWithGuardBenchmarkMachine();

                double noGuardMs;
                double withGuardMs;

                // Alternate order to cancel measurement-order bias on noisy shared CI runners.
                if (run % 2 == 0)
                {
                    noGuardMs = MeasureTransitions(noGuardMachine, iterations);
                    withGuardMs = MeasureTransitions(withGuardMachine, iterations);
                }
                else
                {
                    withGuardMs = MeasureTransitions(withGuardMachine, iterations);
                    noGuardMs = MeasureTransitions(noGuardMachine, iterations);
                }

                overheadSamples.Add(((withGuardMs - noGuardMs) / noGuardMs) * 100);
                noGuardNsSamples.Add((noGuardMs * 1_000_000) / iterations);
                withGuardNsSamples.Add((withGuardMs * 1_000_000) / iterations);
            }

            var guardOverhead = Median(overheadSamples);
            var nsPerTransitionNoGuard = Median(noGuardNsSamples);
            var nsPerTransitionWithGuard = Median(withGuardNsSamples);
            var maxGuardOverheadPercent = GetMaxGuardOverheadPercent();

            output.WriteLine($"Guard Impact ({iterations:N0} transitions, median of {pairedRuns} paired runs):");
            output.WriteLine($"  No Guard: {nsPerTransitionNoGuard * iterations / 1_000_000:F2}ms ({nsPerTransitionNoGuard:F1}ns per transition)");
            output.WriteLine($"  With Guard: {nsPerTransitionWithGuard * iterations / 1_000_000:F2}ms ({nsPerTransitionWithGuard:F1}ns per transition)");
            output.WriteLine($"  Overhead: {guardOverhead:F1}%");
            output.WriteLine($"  Absolute difference: {nsPerTransitionWithGuard - nsPerTransitionNoGuard:F1}ns per transition");
            output.WriteLine($"  Threshold: < {maxGuardOverheadPercent:F0}%");

            Assert.True(nsPerTransitionWithGuard < 50,
                $"Transition with guard took {nsPerTransitionWithGuard:F1}ns, expected < 50ns");

            Assert.True(guardOverhead < maxGuardOverheadPercent,
                $"Guard overhead is {guardOverhead:F1}%, expected < {maxGuardOverheadPercent:F0}%");
        }

        private static void WarmupGuardBenchmarkMachines(int warmupIterations)
        {
            var noGuardMachine = CreateNoGuardBenchmarkMachine();
            var withGuardMachine = CreateWithGuardBenchmarkMachine();

            for (int i = 0; i < warmupIterations; i++)
            {
                noGuardMachine.TryFire(BenchmarkTrigger.Next);
                withGuardMachine.TryFire(BenchmarkTrigger.Next);
            }
        }

        private static void PrepareGuardBenchmarkMeasurement()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static NoGuardBenchmarkMachine CreateNoGuardBenchmarkMachine()
        {
            var machine = new NoGuardBenchmarkMachine(BenchmarkState.A);
            machine.Start();
            return machine;
        }

        private static WithGuardBenchmarkMachine CreateWithGuardBenchmarkMachine()
        {
            var machine = new WithGuardBenchmarkMachine(BenchmarkState.A);
            machine.Start();
            return machine;
        }

        private static double MeasureTransitions(NoGuardBenchmarkMachine machine, int iterations)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                machine.TryFire(BenchmarkTrigger.Next);
            }

            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        private static double MeasureTransitions(WithGuardBenchmarkMachine machine, int iterations)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                machine.TryFire(BenchmarkTrigger.Next);
            }

            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        private static double Median(IReadOnlyList<double> samples)
        {
            return samples.OrderBy(x => x).ElementAt(samples.Count / 2);
        }

        private static double GetMaxGuardOverheadPercent()
        {
            // GitHub-hosted macOS runners are noisy for paired microbenchmarks; keep local/dev strict.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                && string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
            {
                return 175;
            }

            return 150;
        }

        // Dodatkowy test sprawdzający rzeczywistą wydajność
        [Fact]
        public void GuardEvaluation_RealWorldPerformance()
        {
            // Test symulujący bardziej realistyczne użycie
            var machine = new WithGuardBenchmarkMachine(BenchmarkState.A);
            machine.Start();
            const int operations = 1_000_000;

            var sw = Stopwatch.StartNew();
            int successfulTransitions = 0;

            for (int i = 0; i < operations; i++)
            {
                if (machine.TryFire(BenchmarkTrigger.Next))
                {
                    successfulTransitions++;
                }

                // Symulacja dodatkowej pracy (typowe w prawdziwych aplikacjach)
                Thread.SpinWait(10);
            }
            sw.Stop();

            var opsPerSecond = operations / sw.Elapsed.TotalSeconds;

            output.WriteLine($"Real-world simulation:");
            output.WriteLine($"  Total operations: {operations:N0}");
            output.WriteLine($"  Successful transitions: {successfulTransitions:N0}");
            output.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms");
            output.WriteLine($"  Throughput: {opsPerSecond:N0} ops/sec");

            // W rzeczywistych zastosowaniach liczy się całkowita przepustowość
            Assert.True(opsPerSecond > 50_000, $"Throughput {opsPerSecond:N0} ops/sec, expected > 50,000");
        }

        // Benchmark state machines

    }
}
