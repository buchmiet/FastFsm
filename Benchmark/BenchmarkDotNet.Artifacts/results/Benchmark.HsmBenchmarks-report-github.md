```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.300
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-ZNFXLH : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 9.0  IterationCount=15  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error      | StdDev     | Gen0   | Code Size | Gen1   | Gen2   | Allocated |
|------------------------------- |--------------:|-----------:|-----------:|-------:|----------:|-------:|-------:|----------:|
| FastFSM_Hsm_AsyncYield         |   425.4098 ns |  5.5806 ns |  4.6600 ns | 0.0315 |   6,354 B | 0.0010 | 0.0010 |         - |
| Stateless_Hsm_AsyncYield       | 1,149.5876 ns | 29.2949 ns | 25.9692 ns | 0.1202 |   1,445 B | 0.0019 | 0.0019 |         - |
| FastFSM_Hsm_Basic_EnterLeave   |     4.5557 ns |  0.0582 ns |  0.0516 ns |      - |   1,597 B |      - |      - |         - |
| Stateless_Hsm_Basic_EnterLeave |   591.7239 ns |  9.3663 ns |  8.7612 ns | 0.1307 |  11,839 B |      - |      - |    3952 B |
| FastFSM_Hsm_History_Shallow    |     8.3063 ns |  0.1888 ns |  0.1766 ns |      - |   2,501 B |      - |      - |         - |
| FastFSM_Hsm_Internal           |     0.8851 ns |  0.0140 ns |  0.0124 ns |      - |     826 B |      - |      - |         - |
| Stateless_Hsm_Internal         |   252.9802 ns |  6.0893 ns |  5.6960 ns | 0.0467 |  14,715 B |      - |      - |    1408 B |
