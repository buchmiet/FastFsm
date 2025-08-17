```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.300
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-ZNFXLH : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 9.0  IterationCount=15  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean         | Error      | StdDev    | Code Size | Gen0   | Gen1   | Gen2   | Allocated |
|------------------------------- |-------------:|-----------:|----------:|----------:|-------:|-------:|-------:|----------:|
| FastFSM_Hsm_AsyncYield         |   413.932 ns |  7.0527 ns | 6.5971 ns |   6,350 B | 0.0739 |      - |      - |     376 B |
| Stateless_Hsm_AsyncYield       | 1,059.844 ns | 11.5880 ns | 9.6765 ns |   1,445 B | 0.1926 | 0.0038 | 0.0038 |         - |
| FastFSM_Hsm_Basic_EnterLeave   |    11.156 ns |  0.0595 ns | 0.0496 ns |   2,242 B |      - |      - |      - |         - |
| Stateless_Hsm_Basic_EnterLeave |   555.548 ns |  7.5990 ns | 7.1081 ns |  11,773 B | 0.1431 |      - |      - |    3952 B |
| FastFSM_Hsm_History_Shallow    |    14.186 ns |  0.0761 ns | 0.0712 ns |   3,628 B |      - |      - |      - |         - |
| FastFSM_Hsm_Internal           |     4.110 ns |  0.0198 ns | 0.0185 ns |   2,749 B |      - |      - |      - |         - |
| Stateless_Hsm_Internal         |   235.010 ns |  2.5516 ns | 2.2619 ns |  14,692 B | 0.0648 |      - |      - |    1408 B |
