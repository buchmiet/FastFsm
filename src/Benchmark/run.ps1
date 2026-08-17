# Packaged-mode benchmarks (consumer-like): requires FastFsm.Sharp in ./nuget.
# CI and solution builds use the default UsePackages=false (project references).

Remove-Item -Recurse -Force .\bin, .\obj -ErrorAction SilentlyContinue

dotnet pack ..\Fsm\Fsm.Core\Fsm.Core.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet run -c Release -f net10.0 -p:UsePackages=true --project .\Benchmark.csproj
