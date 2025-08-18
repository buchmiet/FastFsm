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
| FastFsm_AsyncActions_HotPath      |   417.7024 ns |  2.9483 ns |  2.7579 ns | 2.180 |    0.02 | 0.0124 |   8,050 B |      - |      - |     384 B |        0.29 |
| Stateless_AsyncActions_HotPath    |   282.5868 ns |  2.2364 ns |  2.0919 ns | 1.475 |    0.01 | 0.0486 |   1,106 B |      - |      - |    1472 B |        1.11 |
| LiquidState_AsyncActions_HotPath  |    64.7963 ns |  1.2771 ns |  1.1321 ns | 0.338 |    0.01 | 0.0080 |   1,133 B |      - |      - |     240 B |        0.18 |
| Appccelerate_AsyncActions_HotPath |   469.5607 ns | 18.4262 ns | 17.2359 ns | 2.450 |    0.09 | 0.0610 |   1,084 B |      - |      - |    1840 B |        1.39 |
| Stateless_AsyncActions            |   998.2406 ns | 13.5860 ns | 12.7083 ns | 5.209 |    0.07 | 0.0801 |   3,436 B | 0.0019 | 0.0019 |         - |        0.00 |
| FastFsm_AsyncActions              |   419.8606 ns |  7.4642 ns |  6.9820 ns | 2.191 |    0.04 | 0.0172 |   8,050 B |      - |      - |     384 B |        0.29 |
| LiquidState_AsyncActions          |   461.1750 ns |  4.6764 ns |  4.3743 ns | 2.407 |    0.03 | 0.0224 |   3,496 B | 0.0005 | 0.0005 |    2981 B |        2.24 |
| Appccelerate_AsyncActions         | 1,421.4701 ns | 83.7414 ns | 78.3318 ns | 7.418 |    0.40 | 0.1068 |   3,400 B |      - |      - |    3169 B |        2.39 |
| Stateless_Basic                   |   191.6447 ns |  1.4851 ns |  1.3165 ns | 1.000 |    0.01 | 0.0441 |  19,574 B |      - |      - |    1328 B |        1.00 |
| FastFsm_Basic                     |     0.6920 ns |  0.0097 ns |  0.0086 ns | 0.004 |    0.00 |      - |     960 B |      - |      - |         - |        0.00 |
| LiquidState_Basic                 |    23.0539 ns |  0.3210 ns |  0.3002 ns | 0.120 |    0.00 | 0.0063 |      64 B |      - |      - |     136 B |        0.10 |
| Appccelerate_Basic                |   198.2740 ns |  3.6952 ns |  3.4565 ns | 1.035 |    0.02 | 0.0534 |   3,711 B |      - |      - |    1608 B |        1.21 |
| Stateless_GuardsActions           |   211.1840 ns |  3.9789 ns |  3.7219 ns | 1.102 |    0.02 | 0.0455 |  20,517 B |      - |      - |    1368 B |        1.03 |
| FastFsm_GuardsActions             |     0.7639 ns |  0.0067 ns |  0.0062 ns | 0.004 |    0.00 |      - |     995 B |      - |      - |         - |        0.00 |
| Appccelerate_GuardsActions        |   210.4150 ns |  2.0861 ns |  1.9514 ns | 1.098 |    0.01 | 0.0548 |   3,711 B |      - |      - |    1648 B |        1.24 |
| Stateless_CanFire                 |   100.1493 ns |  3.4678 ns |  3.2438 ns | 0.523 |    0.02 | 0.0243 |  12,856 B |      - |      - |     608 B |        0.46 |
| FastFsm_CanFire                   |     0.2986 ns |  0.0021 ns |  0.0020 ns | 0.002 |    0.00 |      - |     890 B |      - |      - |         - |        0.00 |
| Stateless_GetPermittedTriggers    |    24.3588 ns |  0.3043 ns |  0.2847 ns | 0.127 |    0.00 | 0.0081 |   4,060 B |      - |      - |     224 B |        0.17 |
| FastFsm_GetPermittedTriggers      |     1.0228 ns |  0.0162 ns |  0.0152 ns | 0.005 |    0.00 |      - |   1,032 B |      - |      - |         - |        0.00 |
| Stateless_Payload                 |   225.6219 ns |  3.1027 ns |  2.7504 ns | 1.177 |    0.02 | 0.0787 |  21,451 B |      - |      - |    1424 B |        1.07 |
| FastFsm_Payload                   |     0.6876 ns |  0.0096 ns |  0.0090 ns | 0.004 |    0.00 |      - |     983 B |      - |      - |         - |        0.00 |
| LiquidState_Payload               |    24.6084 ns |  0.4471 ns |  0.3963 ns | 0.128 |    0.00 | 0.0055 |      92 B |      - |      - |     136 B |        0.10 |
| Appccelerate_Payload              |   225.3924 ns |  2.9028 ns |  2.4240 ns | 1.176 |    0.01 | 0.0548 |   3,721 B |      - |      - |    1648 B |        1.24 |
