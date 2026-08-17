# 1. Usuń stare bin/obj (składnia dla PS, nie CMD)
Remove-Item -Recurse -Force .\bin, .\obj

# 2. Przywróć pakiety DLA PROJEKTU benchmarków (upewnij się, że nazwa jest dobra)
dotnet restore .\Benchmark.csproj

# 3. Uruchom benchmarki dla net10.0 
dotnet run -c Release -f net10.0 --project .\Benchmark.csproj

