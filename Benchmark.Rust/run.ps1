$Env:RUSTFLAGS = '-C target-cpu=native -C codegen-units=1 -C lto=no'

Write-Host "Cleaning..." -ForegroundColor Yellow
cargo +stable clean

Write-Host "Running sync benchmarks..." -ForegroundColor Green
cargo +stable bench --bench fsm_bench

Write-Host "Running async benchmarks (statig only)..." -ForegroundColor Green
cargo +stable bench --bench fsm_async_bench