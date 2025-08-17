using Benchmark;
using BenchmarkDotNet.Running;



// Run standard benchmarks
// BenchmarkRunner.Run<StateMachineBenchmarks>();

// Run HSM benchmarks
BenchmarkRunner.Run<HsmBenchmarks>();