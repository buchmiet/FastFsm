```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.300
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-ZNFXLH : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 9.0  IterationCount=15  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error      | StdDev     | Gen0   | Code Size | Allocated |
|------------------------------- |--------------:|-----------:|-----------:|-------:|----------:|----------:|
| FastFSM_Hsm_AsyncYield         |   418.1654 ns |  2.7330 ns |  2.5564 ns | 0.0124 |   6,356 B |     376 B |
| Stateless_Hsm_AsyncYield       | 1,118.4881 ns | 14.6151 ns | 13.6709 ns | 0.1030 |   1,445 B |    3103 B |
| FastFSM_Hsm_Basic_EnterLeave   |     4.7637 ns |  0.1703 ns |  0.1593 ns |      - |   1,631 B |         - |
| Stateless_Hsm_Basic_EnterLeave |   564.0282 ns |  7.2529 ns |  6.7843 ns | 0.1431 |  11,616 B |    3952 B |
| FastFSM_Hsm_History_Shallow    |     8.2706 ns |  0.0547 ns |  0.0485 ns |      - |   2,529 B |         - |
| FastFSM_Hsm_Internal           |     0.9036 ns |  0.0029 ns |  0.0026 ns |      - |     852 B |         - |
| Stateless_Hsm_Internal         |   245.9614 ns |  2.2420 ns |  1.9875 ns | 0.0467 |  15,039 B |    1408 B |
