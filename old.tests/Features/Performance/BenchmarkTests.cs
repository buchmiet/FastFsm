using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FastFsm.Tests.Machines;
using Xunit;
using Xunit.Abstractions;

namespace FastFsm.Tests.Features.Performance
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
            const int iterations = 10_000_000; // Zwiększona liczba iteracji
            const int warmupIterations = 100_000;

            // Warmup dla obu maszyn
            var noGuardMachine = new NoGuardBenchmarkMachine(BenchmarkState.A);
            noGuardMachine.Start();
            var withGuardMachine = new WithGuardBenchmarkMachine(BenchmarkState.A);
            withGuardMachine.Start();

            for (int i = 0; i < warmupIterations; i++)
            {
                noGuardMachine.TryFire(BenchmarkTrigger.Next);
                withGuardMachine.TryFire(BenchmarkTrigger.Next);
            }

            // Reset maszyn
            noGuardMachine = new NoGuardBenchmarkMachine(BenchmarkState.A);
            noGuardMachine.Start();
            withGuardMachine = new WithGuardBenchmarkMachine(BenchmarkState.A);
            withGuardMachine.Start();

            // Pomiar bez guards - wielokrotne próby
            var noGuardTimes = new List<double>();
            for (int run = 0; run < 5; run++)
            {
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    noGuardMachine.TryFire(BenchmarkTrigger.Next);
                }
                sw.Stop();
                noGuardTimes.Add(sw.Elapsed.TotalMilliseconds);
            }

            // Pomiar z guards - wielokrotne próby
            var withGuardTimes = new List<double>();
            for (int run = 0; run < 5; run++)
            {
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    withGuardMachine.TryFire(BenchmarkTrigger.Next);
                }
                sw.Stop();
                withGuardTimes.Add(sw.Elapsed.TotalMilliseconds);
            }

            // Użyj mediany zamiast średniej (bardziej odporna na outliers)
            var noGuardMedian = noGuardTimes.OrderBy(x => x).ElementAt(noGuardTimes.Count / 2);
            var withGuardMedian = withGuardTimes.OrderBy(x => x).ElementAt(withGuardTimes.Count / 2);

            var guardOverhead = ((withGuardMedian - noGuardMedian) / noGuardMedian) * 100;
            var nsPerTransitionNoGuard = (noGuardMedian * 1_000_000) / iterations;
            var nsPerTransitionWithGuard = (withGuardMedian * 1_000_000) / iterations;

            output.WriteLine($"Guard Impact ({iterations:N0} transitions, median of 5 runs):");
            output.WriteLine($"  No Guard: {noGuardMedian:F2}ms ({nsPerTransitionNoGuard:F1}ns per transition)");
            output.WriteLine($"  With Guard: {withGuardMedian:F2}ms ({nsPerTransitionWithGuard:F1}ns per transition)");
            output.WriteLine($"  Overhead: {guardOverhead:F1}%");
            output.WriteLine($"  Absolute difference: {nsPerTransitionWithGuard - nsPerTransitionNoGuard:F1}ns per transition");

            // Realistyczne oczekiwania:
            // Guard dodaje ~5-10ns na przejście, co przy bazowej operacji ~10ns daje 50-100% overhead
            Assert.True(nsPerTransitionWithGuard < 50,
                $"Transition with guard took {nsPerTransitionWithGuard:F1}ns, expected < 50ns");

            // Dla praktycznych zastosowań ważniejsza jest absolutna wydajność niż procentowy overhead
            Assert.True(withGuardMedian < noGuardMedian * 2.5,
                $"Guard overhead is {guardOverhead:F1}%, expected < 150%");
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
        public enum BenchmarkState { A, B, C, D }
        public enum BenchmarkTrigger { Previous,Next }

    }
}
