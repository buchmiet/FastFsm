```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4652/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.300
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-ZNFXLH : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 9.0  IterationCount=15  LaunchCount=1  
WarmupCount=3  

```
| Method                            | Mean          | Error       | StdDev      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------------------------- |--------------:|------------:|------------:|------:|--------:|-------:|----------:|-------:|-------:|----------:|------------:|
| FastFsm_AsyncActions_HotPath      |   444.7676 ns |  10.4623 ns |   8.7365 ns | 1.800 |    0.16 | 0.0134 |   8,050 B | 0.0005 | 0.0005 |    2198 B |          NA |
| Stateless_AsyncActions_HotPath    |   357.1208 ns |  34.4258 ns |  30.5175 ns | 1.446 |    0.17 | 0.0486 |   1,106 B |      - |      - |    1472 B |          NA |
| LiquidState_AsyncActions_HotPath  |    75.8736 ns |   5.5882 ns |   4.9538 ns | 0.307 |    0.03 | 0.0080 |   1,133 B |      - |      - |     240 B |          NA |
| Appccelerate_AsyncActions_HotPath |   504.3677 ns |  46.5029 ns |  43.4989 ns | 2.042 |    0.25 | 0.0610 |   1,084 B |      - |      - |    1840 B |          NA |
| Stateless_AsyncActions            | 1,100.7773 ns |  65.2411 ns |  61.0266 ns | 4.456 |    0.46 | 0.0763 |   3,436 B |      - |      - |    2295 B |          NA |
| FastFsm_AsyncActions              |   456.7177 ns |   9.5045 ns |   8.4255 ns | 1.849 |    0.16 | 0.0129 |   8,050 B |      - |      - |     384 B |          NA |
| LiquidState_AsyncActions          |   490.2154 ns |  15.9519 ns |  14.9214 ns | 1.984 |    0.18 | 0.0219 |   3,496 B |      - |      - |     656 B |          NA |
| Appccelerate_AsyncActions         | 1,738.6204 ns | 130.1788 ns | 108.7052 ns | 7.038 |    0.75 | 0.1049 |   3,400 B |      - |      - |    3166 B |          NA |
| Stateless_Basic                   |   249.0328 ns |  25.3867 ns |  23.7468 ns | 1.008 |    0.13 | 0.0582 |  19,724 B | 0.0005 | 0.0005 |         - |          NA |
| FastFsm_Basic                     |     0.8072 ns |   0.0769 ns |   0.0719 ns | 0.003 |    0.00 |      - |     958 B |      - |      - |         - |          NA |
| LiquidState_Basic                 |    25.3087 ns |   0.6449 ns |   0.6032 ns | 0.102 |    0.01 | 0.0045 |      64 B |      - |      - |     136 B |          NA |
| Appccelerate_Basic                |   260.8463 ns |  11.1830 ns |  10.4605 ns | 1.056 |    0.10 | 0.0534 |   3,710 B |      - |      - |    1608 B |          NA |
| Stateless_GuardsActions           |   267.3683 ns |  22.2094 ns |  20.7747 ns | 1.082 |    0.12 | 0.0453 |  21,417 B |      - |      - |    1368 B |          NA |
| FastFsm_GuardsActions             |     2.1843 ns |   0.2507 ns |   0.2345 ns | 0.009 |    0.00 |      - |   1,307 B |      - |      - |         - |          NA |
| Appccelerate_GuardsActions        |   273.5329 ns |  23.1344 ns |  21.6399 ns | 1.107 |    0.13 | 0.0548 |   3,700 B |      - |      - |    1648 B |          NA |
| Stateless_CanFire                 |   115.5406 ns |   1.0646 ns |   0.8311 ns | 0.468 |    0.04 | 0.0200 |  12,909 B |      - |      - |     608 B |          NA |
| FastFsm_CanFire                   |     0.3115 ns |   0.0183 ns |   0.0171 ns | 0.001 |    0.00 |      - |     890 B |      - |      - |         - |          NA |
| Stateless_GetPermittedTriggers    |    32.6867 ns |   2.9310 ns |   2.5983 ns | 0.132 |    0.02 | 0.0075 |   3,761 B |      - |      - |     224 B |          NA |
| FastFsm_GetPermittedTriggers      |     4.1810 ns |   0.2397 ns |   0.2125 ns | 0.017 |    0.00 | 0.0011 |     979 B |      - |      - |      32 B |          NA |
| Stateless_Payload                 |   300.6333 ns |  29.2027 ns |  27.3162 ns | 1.217 |    0.15 | 0.0472 |  21,397 B |      - |      - |    1424 B |          NA |
| FastFsm_Payload                   |     0.8267 ns |   0.0746 ns |   0.0697 ns | 0.003 |    0.00 |      - |     160 B |      - |      - |         - |          NA |
| LiquidState_Payload               |    30.1339 ns |   2.3156 ns |   2.0527 ns | 0.122 |    0.01 | 0.0049 |      92 B |      - |      - |     136 B |          NA |
| Appccelerate_Payload              |   291.6003 ns |  23.9050 ns |  22.3608 ns | 1.180 |    0.14 | 0.0548 |   3,721 B |      - |      - |    1648 B |          NA |
