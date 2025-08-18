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
| FastFSM_Hsm_AsyncYield         |   436.271 ns | 12.3546 ns | 11.5565 ns |   6,348 B | 0.0134 | 0.0010 | 0.0010 |         - |
| Stateless_Hsm_AsyncYield       | 1,157.745 ns | 41.3971 ns | 38.7229 ns |   1,445 B | 0.1259 | 0.0019 | 0.0019 |         - |
| FastFSM_Hsm_Basic_EnterLeave   |    17.343 ns |  0.1885 ns |  0.1763 ns |   2,706 B |      - |      - |      - |         - |
| Stateless_Hsm_Basic_EnterLeave |   535.308 ns |  4.9084 ns |  3.8321 ns |  11,847 B | 0.1431 |      - |      - |    3952 B |
| FastFSM_Hsm_History_Shallow    |    18.224 ns |  0.2552 ns |  0.2387 ns |   4,573 B |      - |      - |      - |         - |
| FastFSM_Hsm_Internal           |     4.470 ns |  0.0752 ns |  0.0704 ns |   3,044 B |      - |      - |      - |         - |
| Stateless_Hsm_Internal         |   236.297 ns |  2.7920 ns |  2.6116 ns |  15,037 B | 0.0563 |      - |      - |    1408 B |
