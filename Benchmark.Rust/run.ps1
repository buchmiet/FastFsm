$env:RUSTFLAGS = "-C target-cpu=native"

Write-Host "Cleaning..." -ForegroundColor Yellow
cargo clean

Write-Host "Running sync benchmarks..." -ForegroundColor Green
cargo bench --bench fsm_bench

Write-Host "Running async benchmarks (statig with tokio)..." -ForegroundColor Green
cargo bench --bench fsm_async_bench