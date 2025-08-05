```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4652/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.300
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-ZNFXLH : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 9.0  IterationCount=15  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev      | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|------------:|------:|--------:|----------:|-------:|-------:|-------:|----------:|------------:|
| FastFsm_AsyncActions_HotPath   |   453.5714 ns |   9.6017 ns |   8.9814 ns | 1.690 |    0.12 |   8,050 B | 0.0124 |      - |      - |     383 B |        0.29 |
| Stateless_AsyncActions         | 1,055.0909 ns |  70.5795 ns |  66.0202 ns | 3.932 |    0.35 |   3,436 B | 0.0763 |      - |      - |    2295 B |        1.73 |
| FastFsm_AsyncActions           |   436.9857 ns |   7.4541 ns |   6.9726 ns | 1.628 |    0.11 |   8,050 B | 0.0257 |      - |      - |     383 B |        0.29 |
| LiquidState_AsyncActions       |   482.4262 ns |   9.6802 ns |   9.0549 ns | 1.798 |    0.12 |   3,496 B | 0.0329 | 0.0005 | 0.0005 |         - |        0.00 |
| Appccelerate_AsyncActions      | 1,558.9563 ns | 124.7415 ns | 116.6833 ns | 5.809 |    0.57 |   3,400 B | 0.1087 | 0.0019 | 0.0019 |   28853 B |       21.73 |
| Stateless_Basic                |   269.4826 ns |  19.9230 ns |  17.6613 ns | 1.004 |    0.09 |  20,413 B | 0.0441 |      - |      - |    1328 B |        1.00 |
| FastFsm_Basic                  |     0.7567 ns |   0.0575 ns |   0.0538 ns | 0.003 |    0.00 |     135 B |      - |      - |      - |         - |        0.00 |
| LiquidState_Basic              |    25.1447 ns |   0.7899 ns |   0.7389 ns | 0.094 |    0.01 |      64 B | 0.0049 |      - |      - |     136 B |        0.10 |
| Appccelerate_Basic             |   244.0788 ns |  14.7975 ns |  13.8416 ns | 0.910 |    0.08 |   3,704 B | 0.0534 |      - |      - |    1608 B |        1.21 |
| Stateless_GuardsActions        |   265.0102 ns |  16.4832 ns |  15.4184 ns | 0.988 |    0.09 |  21,296 B | 0.0455 |      - |      - |    1368 B |        1.03 |
| FastFsm_GuardsActions          |     1.8340 ns |   0.2257 ns |   0.2112 ns | 0.007 |    0.00 |      57 B |      - |      - |      - |         - |        0.00 |
| Appccelerate_GuardsActions     |   270.2072 ns |  16.6338 ns |  15.5593 ns | 1.007 |    0.09 |   3,672 B | 0.0548 |      - |      - |    1648 B |        1.24 |
| Stateless_CanFire              |   131.6976 ns |  11.0766 ns |  10.3610 ns | 0.491 |    0.05 |  13,426 B | 0.0201 |      - |      - |     608 B |        0.46 |
| FastFsm_CanFire                |     0.2036 ns |   0.0050 ns |   0.0047 ns | 0.001 |    0.00 |      73 B |      - |      - |      - |         - |        0.00 |
| Stateless_GetPermittedTriggers |    29.2226 ns |   0.2205 ns |   0.2063 ns | 0.109 |    0.01 |   3,761 B | 0.0075 |      - |      - |     224 B |        0.17 |
| FastFsm_GetPermittedTriggers   |     3.1896 ns |   0.0327 ns |   0.0290 ns | 0.012 |    0.00 |     161 B | 0.0011 |      - |      - |      32 B |        0.02 |
| Stateless_Payload              |   256.5361 ns |   7.4204 ns |   6.9411 ns | 0.956 |    0.07 |  21,835 B | 0.0472 |      - |      - |    1424 B |        1.07 |
| FastFsm_Payload                |     0.6052 ns |   0.0523 ns |   0.0489 ns | 0.002 |    0.00 |     122 B |      - |      - |      - |         - |        0.00 |
| LiquidState_Payload            |    29.7013 ns |   1.2574 ns |   1.1146 ns | 0.111 |    0.01 |      92 B | 0.0048 | 0.0000 | 0.0000 |         - |        0.00 |
| Appccelerate_Payload           |   255.4097 ns |  17.5709 ns |  16.4358 ns | 0.952 |    0.09 |   3,733 B | 0.0548 |      - |      - |    1648 B |        1.24 |
