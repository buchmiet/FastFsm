```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4946/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.300
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-ZNFXLH : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 9.0  IterationCount=15  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean          | Error      | StdDev     | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------------------------- |--------------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|-------:|----------:|------------:|
| FastFsm_AsyncActions_HotPath      |   411.8848 ns |  7.7782 ns |  7.2757 ns | 2.134 |    0.04 | 0.0153 |   8,068 B | 0.0010 | 0.0010 |         - |        0.00 |
| Stateless_AsyncActions_HotPath    |   288.5247 ns | 12.7547 ns | 11.9308 ns | 1.495 |    0.06 | 0.0486 |   1,106 B |      - |      - |    1472 B |        1.11 |
| LiquidState_AsyncActions_HotPath  |    63.1323 ns |  0.7343 ns |  0.6868 ns | 0.327 |    0.00 | 0.0094 |   1,133 B | 0.0001 | 0.0001 |         - |        0.00 |
| Appccelerate_AsyncActions_HotPath |   437.8073 ns |  5.5399 ns |  4.9110 ns | 2.268 |    0.03 | 0.0739 |   1,084 B |      - |      - |    1840 B |        1.39 |
| Stateless_AsyncActions            | 1,002.0095 ns |  9.8738 ns |  8.2450 ns | 5.191 |    0.06 | 0.0763 |   3,436 B |      - |      - |    2295 B |        1.73 |
| FastFsm_AsyncActions              |   412.8943 ns |  7.8654 ns |  7.3573 ns | 2.139 |    0.04 | 0.0758 |   8,068 B |      - |      - |     383 B |        0.29 |
| LiquidState_AsyncActions          |   450.7167 ns |  5.0724 ns |  4.7447 ns | 2.335 |    0.03 | 0.0234 |   3,496 B | 0.0005 | 0.0005 |         - |        0.00 |
| Appccelerate_AsyncActions         | 1,315.1799 ns | 14.2770 ns | 13.3547 ns | 6.813 |    0.09 | 0.1068 |   3,400 B |      - |      - |    3168 B |        2.39 |
| Stateless_Basic                   |   193.0581 ns |  1.7852 ns |  1.5825 ns | 1.000 |    0.01 | 0.0441 |  19,579 B |      - |      - |    1328 B |        1.00 |
| FastFsm_Basic                     |     0.6784 ns |  0.0123 ns |  0.0115 ns | 0.004 |    0.00 |      - |     975 B |      - |      - |         - |        0.00 |
| LiquidState_Basic                 |    22.1385 ns |  0.5208 ns |  0.4871 ns | 0.115 |    0.00 | 0.0065 |      64 B |      - |      - |     136 B |        0.10 |
| Appccelerate_Basic                |   187.2108 ns |  2.4535 ns |  2.1750 ns | 0.970 |    0.01 | 0.0534 |   3,722 B |      - |      - |    1608 B |        1.21 |
| Stateless_GuardsActions           |   210.3316 ns |  2.6559 ns |  2.4843 ns | 1.090 |    0.02 | 0.0455 |  20,510 B |      - |      - |    1368 B |        1.03 |
| FastFsm_GuardsActions             |     0.5591 ns |  0.0036 ns |  0.0032 ns | 0.003 |    0.00 |      - |   1,012 B |      - |      - |         - |        0.00 |
| Appccelerate_GuardsActions        |   193.0686 ns |  1.6757 ns |  1.5674 ns | 1.000 |    0.01 | 0.2081 |   3,711 B |      - |      - |    1648 B |        1.24 |
| Stateless_CanFire                 |   101.8809 ns |  1.5512 ns |  1.3751 ns | 0.528 |    0.01 | 0.0243 |  13,031 B |      - |      - |     608 B |        0.46 |
| FastFsm_CanFire                   |     0.3078 ns |  0.0044 ns |  0.0041 ns | 0.002 |    0.00 |      - |     902 B |      - |      - |         - |        0.00 |
| Stateless_GetPermittedTriggers    |    24.8505 ns |  0.3852 ns |  0.3603 ns | 0.129 |    0.00 | 0.0081 |   3,760 B |      - |      - |     224 B |        0.17 |
| FastFsm_GetPermittedTriggers      |     1.0609 ns |  0.0139 ns |  0.0130 ns | 0.005 |    0.00 |      - |   1,037 B |      - |      - |         - |        0.00 |
| Stateless_Payload                 |   231.1420 ns |  4.4515 ns |  4.1639 ns | 1.197 |    0.02 | 0.0472 |  20,732 B |      - |      - |    1424 B |        1.07 |
| FastFsm_Payload                   |     0.7201 ns |  0.0098 ns |  0.0092 ns | 0.004 |    0.00 |      - |     995 B |      - |      - |         - |        0.00 |
| LiquidState_Payload               |    24.9332 ns |  0.4026 ns |  0.3766 ns | 0.129 |    0.00 | 0.0073 |      92 B |      - |      - |     136 B |        0.10 |
| Appccelerate_Payload              |   205.4944 ns |  2.7289 ns |  2.5526 ns | 1.064 |    0.02 | 0.0548 |   3,721 B |      - |      - |    1648 B |        1.24 |
