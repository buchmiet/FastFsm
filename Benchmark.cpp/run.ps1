<#
    run.ps1  –  kompiluje i uruchamia benchmark C++
    Użycie:   ./run.ps1            # Release, x64-windows-release
             ./run.ps1 -clean      # najpierw rm -r build
#>

[CmdletBinding()]
param(
    [switch]$clean
)

$ErrorActionPreference = 'Stop'

# ------------------------------------------------------------------
# 1. Sprzątanie
# ------------------------------------------------------------------
$build = "build"
if ($clean -and (Test-Path $build)) { Remove-Item -Recurse -Force $build }

# ------------------------------------------------------------------
# 2. Konfiguracja CMake
# ------------------------------------------------------------------
$toolchain = "$env:VCPKG_ROOT\scripts\buildsystems\vcpkg.cmake"
cmake -B $build -S . -G "Visual Studio 17 2022" `
      "-DCMAKE_TOOLCHAIN_FILE=$toolchain" `
      "-DVCPKG_TARGET_TRIPLET=x64-windows-release" `
      "-DCMAKE_BUILD_TYPE=Release" `
      "-DCMAKE_CXX_FLAGS_RELEASE=/O2 /GL /arch:AVX512" `
      "-DCMAKE_C_FLAGS_RELEASE=/O2 /GL /arch:AVX512"

# ------------------------------------------------------------------
# 3. Kompilacja
# ------------------------------------------------------------------
cmake --build $build --config Release --parallel

# ------------------------------------------------------------------
# 4. Uruchomienie benchmarku
# ------------------------------------------------------------------
& ".\${build}\Release\fastfsm_cpp_bench.exe"
