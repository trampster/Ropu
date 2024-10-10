```

BenchmarkDotNet v0.14.0, Ubuntu 20.04.6 LTS (Focal Fossa)
Intel Core i7-2600 CPU 3.40GHz (Sandy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX
  DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX


```
| Method    | value | Mean     | Error     | StdDev    | Median   | Allocated |
|---------- |------ |---------:|----------:|----------:|---------:|----------:|
| TryFormat | 13456 | 7.686 ns | 0.1909 ns | 0.3942 ns | 7.478 ns |         - |
