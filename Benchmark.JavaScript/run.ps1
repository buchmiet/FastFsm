# run.ps1
Write-Host "TypeScript State Machine Benchmarks" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

# Sprawdź czy Bun jest zainstalowany
if (-not (Get-Command bun -ErrorAction SilentlyContinue)) {
    Write-Host "Bun nie jest zainstalowany! Zainstaluj z: https://bun.sh" -ForegroundColor Red
    exit 1
}

Write-Host "`nInstalowanie zależności..." -ForegroundColor Yellow
bun install

Write-Host "`nUruchamianie benchmarków..." -ForegroundColor Green
Write-Host "To może potrwać kilka minut...`n" -ForegroundColor Gray

# Uruchom benchmark
bun run benchmark.ts

# Opcjonalnie: uruchom też z Node.js dla porównania
if (Get-Command node -ErrorAction SilentlyContinue) {
    Write-Host "`n`nDla porównania - ten sam kod na Node.js:" -ForegroundColor Yellow
    npx tsx benchmark.ts
}
