```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.300
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-ZNFXLH : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 9.0  IterationCount=15  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean         | Error      | StdDev     | Code Size | Gen0   | Gen1   | Gen2   | Allocated |
|------------------------------- |-------------:|-----------:|-----------:|----------:|-------:|-------:|-------:|----------:|
| FastFSM_Hsm_AsyncYield         |   409.903 ns |  8.7405 ns |  7.7482 ns |   6,348 B | 0.0124 |      - |      - |     376 B |
| Stateless_Hsm_AsyncYield       | 1,164.604 ns | 59.3832 ns | 55.5471 ns |   1,445 B | 0.1068 | 0.0019 | 0.0019 |   13434 B |
| FastFSM_Hsm_Basic_EnterLeave   |    11.689 ns |  0.3780 ns |  0.3536 ns |   2,242 B |      - |      - |      - |         - |
| Stateless_Hsm_Basic_EnterLeave |   548.906 ns | 15.2348 ns | 14.2507 ns |  11,643 B | 0.1307 |      - |      - |    3952 B |
| FastFSM_Hsm_History_Shallow    |    15.005 ns |  0.5167 ns |  0.4315 ns |   3,626 B |      - |      - |      - |         - |
| FastFSM_Hsm_Internal           |     4.241 ns |  0.1074 ns |  0.0952 ns |   2,750 B |      - |      - |      - |         - |
| Stateless_Hsm_Internal         |   259.033 ns | 24.6690 ns | 23.0754 ns |  15,069 B | 0.0467 |      - |      - |    1408 B |
