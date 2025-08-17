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
| FastFSM_Hsm_AsyncYield         |   433.057 ns |  7.0272 ns |  6.2294 ns |   6,348 B | 0.0181 | 0.0010 | 0.0010 |    2159 B |
| Stateless_Hsm_AsyncYield       | 1,190.418 ns | 81.1601 ns | 75.9172 ns |   1,445 B | 0.1163 | 0.0019 | 0.0019 |         - |
| FastFSM_Hsm_Basic_EnterLeave   |    10.925 ns |  0.1079 ns |  0.1010 ns |   2,199 B |      - |      - |      - |         - |
| Stateless_Hsm_Basic_EnterLeave |   609.817 ns | 20.0070 ns | 17.7357 ns |  11,638 B | 0.1431 |      - |      - |    3952 B |
| FastFSM_Hsm_History_Shallow    |    15.118 ns |  0.6349 ns |  0.5628 ns |   3,626 B |      - |      - |      - |         - |
| FastFSM_Hsm_Internal           |     4.315 ns |  0.1319 ns |  0.1101 ns |   2,750 B |      - |      - |      - |         - |
| Stateless_Hsm_Internal         |   245.830 ns |  6.8818 ns |  6.1005 ns |  14,711 B | 0.0467 |      - |      - |    1408 B |
