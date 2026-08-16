# homelab (lima) — BenchmarkDotNet results

Measured **2026-08-16** on the homelab Lima VM.

## Environment

| Field | Value |
|-------|-------|
| Host | `homelab` (Lima VM, Apple hypervisor) |
| OS | Ubuntu 26.04 LTS (aarch64) |
| CPU | 4 vCPU, Apple vendor (ARM64) |
| RAM | 7.7 GiB |
| .NET SDK | 10.0.400 |
| .NET runtime | 10.0.11 |
| BenchmarkDotNet | 0.15.8 |
| FastFsm.Net | 0.9.0 (local `./nuget` feed, `UsePackages=true`) |
| Repository commit | `548ea01` (+ `Benchmark/Benchmark.csproj` harness fix) |

### Comparison library versions

| Package | Version |
|---------|---------|
| Stateless | 5.17.0 |
| LiquidState | 8.2.0 |
| Appccelerate.StateMachine | 6.0.0 |

### Job configuration

`Runtime=.NET 10.0`, `IterationCount=15`, `LaunchCount=1`, `WarmupCount=3`

## StateMachineBenchmarks (flat FSM)

```
| Method                            | Mean          | Error      | StdDev     | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------------------------- |--------------:|-----------:|-----------:|------:|--------:|----------:|-------:|-------:|-------:|----------:|------------:|
| FastFsm_AsyncActions_HotPath      |   834.7534 ns | 12.5945 ns | 10.5170 ns | 1.911 |    0.02 |   2,924 B | 0.0763 |      - |      - |     392 B |        0.33 |
| Stateless_AsyncActions_HotPath    |   495.6130 ns |  5.0941 ns |  4.7650 ns | 1.134 |    0.01 |   1,284 B | 0.1326 |      - |      - |    1344 B |        1.12 |
| LiquidState_AsyncActions_HotPath  |   119.2241 ns |  0.5074 ns |  0.4498 ns | 0.273 |    0.00 |   1,284 B | 0.0174 |      - |      - |     176 B |        0.15 |
| Appccelerate_AsyncActions_HotPath |   741.2195 ns |  6.2139 ns |  5.5085 ns | 1.697 |    0.01 |   1,284 B | 0.1822 |      - |      - |    1840 B |        1.53 |
| Stateless_AsyncActions            | 1,575.0732 ns | 21.6258 ns | 20.2288 ns | 3.605 |    0.05 |   1,236 B | 0.2632 | 0.0076 | 0.0076 |    1931 B |        1.61 |
| FastFsm_AsyncActions              |   801.3514 ns | 15.4395 ns | 14.4421 ns | 1.834 |    0.03 |   2,928 B | 0.0381 |      - |      - |     392 B |        0.33 |
| LiquidState_AsyncActions          |   933.5231 ns | 14.0751 ns | 13.1659 ns | 2.137 |    0.03 |   1,236 B | 0.1040 | 0.0019 | 0.0019 |     528 B |        0.44 |
| Appccelerate_AsyncActions         | 2,376.4669 ns | 44.0082 ns | 39.0121 ns | 5.440 |    0.09 |   1,240 B | 0.3128 |      - |      - |    3176 B |        2.65 |
| Stateless_Basic                   |   436.8771 ns |  2.3990 ns |  2.1267 ns | 1.000 |    0.01 |  25,032 B | 0.1187 |      - |      - |    1200 B |        1.00 |
| FastFsm_Basic                     |     1.2409 ns |  0.0376 ns |  0.0352 ns | 0.003 |    0.00 |   2,440 B |      - |      - |      - |         - |        0.00 |
| LiquidState_Basic                 |    25.4385 ns |  0.1449 ns |  0.1356 ns | 0.058 |    0.00 |      68 B | 0.0071 |      - |      - |      72 B |        0.06 |
| Appccelerate_Basic                |   491.3888 ns |  5.8791 ns |  5.4993 ns | 1.125 |    0.01 |  23,328 B | 0.1593 |      - |      - |    1608 B |        1.34 |
| Stateless_GuardsActions           |   371.9329 ns |  3.1652 ns |  2.8059 ns | 0.851 |    0.01 |  10,772 B | 0.1187 |      - |      - |    1200 B |        1.00 |
| FastFsm_GuardsActions             |     1.3178 ns |  0.0361 ns |  0.0282 ns | 0.003 |    0.00 |   2,496 B |      - |      - |      - |         - |        0.00 |
| Appccelerate_GuardsActions        |   441.2059 ns |  2.1394 ns |  2.0012 ns | 1.010 |    0.01 |  23,820 B | 0.1593 |      - |      - |    1608 B |        1.34 |
| Stateless_CanFire                 |   194.8927 ns |  2.2515 ns |  1.9959 ns | 0.446 |    0.00 |  15,068 B | 0.0601 |      - |      - |     608 B |        0.51 |
| FastFsm_CanFire                   |     0.5421 ns |  0.0023 ns |  0.0021 ns | 0.001 |    0.00 |   2,384 B |      - |      - |      - |         - |        0.00 |
| Stateless_GetPermittedTriggers    |    54.3006 ns |  1.2332 ns |  1.1535 ns | 0.124 |    0.00 |   4,824 B | 0.0222 |      - |      - |     224 B |        0.19 |
| FastFsm_GetPermittedTriggers      |     1.2774 ns |  0.0395 ns |  0.0370 ns | 0.003 |    0.00 |   2,572 B |      - |      - |      - |         - |        0.00 |
| Stateless_Payload                 |   517.3740 ns |  2.8172 ns |  2.3525 ns | 1.184 |    0.01 |  24,368 B | 0.1278 |      - |      - |    1296 B |        1.08 |
| FastFsm_Payload                   |     2.0866 ns |  0.0049 ns |  0.0046 ns | 0.005 |    0.00 |   2,460 B |      - |      - |      - |         - |        0.00 |
| LiquidState_Payload               |    35.2308 ns |  0.4481 ns |  0.4192 ns | 0.081 |    0.00 |     260 B | 0.0071 |      - |      - |      72 B |        0.06 |
| Appccelerate_Payload              |   504.9934 ns |  4.9393 ns |  4.6202 ns | 1.156 |    0.01 |   4,732 B | 0.1593 |      - |      - |    1608 B |        1.34 |
```

**Wall time:** ~5 min 44 s (23 benchmarks).

## HsmBenchmarks (hierarchical FSM)

```
| Method                         | Mean         | Error      | StdDev     | Gen0   | Code Size | Gen1   | Gen2   | Allocated |
|------------------------------- |-------------:|-----------:|-----------:|-------:|----------:|-------:|-------:|----------:|
| FastFSM_Hsm_AsyncYield         |   817.274 ns | 11.4674 ns | 10.1655 ns | 0.0591 |   2,716 B | 0.0019 | 0.0019 |         - |
| Stateless_Hsm_AsyncYield       | 1,903.849 ns | 40.3627 ns | 37.7553 ns | 0.3853 |   1,236 B |      - |      - |    2944 B |
| FastFSM_Hsm_Basic_EnterLeave   |     4.052 ns |  0.0421 ns |  0.0394 ns |      - |   3,328 B |      - |      - |         - |
| Stateless_Hsm_Basic_EnterLeave | 1,007.644 ns |  7.8792 ns |  6.9847 ns | 0.3338 |  16,784 B |      - |      - |    3376 B |
| FastFSM_Hsm_History_Shallow    |    64.191 ns |  0.4678 ns |  0.4376 ns | 0.0142 |   5,616 B |      - |      - |     144 B |
| FastFSM_Hsm_Internal           |     1.615 ns |  0.0048 ns |  0.0042 ns |      - |   2,000 B |      - |      - |         - |
| Stateless_Hsm_Internal         |   446.071 ns |  3.6954 ns |  3.2759 ns | 0.1392 |  18,788 B |      - |      - |    1408 B |
```

**Wall time:** ~1 min 38 s (7 benchmarks).
