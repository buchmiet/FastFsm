use criterion::{criterion_group, criterion_main, Criterion, Throughput};
use fsm_benchmark::*;
use statig::prelude::*;
use std::hint::black_box;

fn bench_async(c: &mut Criterion) {
    // Build multi-threaded tokio runtime (like .NET thread pool)
    let rt = tokio::runtime::Runtime::new().unwrap();
    
    // Benchmark HOT PATH (no yield - like C# Task.CompletedTask)
    let mut group = c.benchmark_group("async_hot");
    group.throughput(Throughput::Elements(OPS as u64));
    
    group.bench_function("statig_async_hot", |b| {
        b.to_async(&rt).iter(|| async {
            // Create state machine ONCE per iteration (like C# does)
            let mut sm = AsyncHotMachine::default().uninitialized_state_machine().init().await;
            let event = AsyncEvent::Next;
            
            // Measure only the state transitions
            for _ in 0..OPS {
                sm.handle(black_box(&event)).await;
                black_box(sm.state());
            }
        })
    });
    group.finish();

    // Benchmark COLD PATH with yield_now (like C# Task.Yield)
    let mut group = c.benchmark_group("async_cold_yield");
    group.throughput(Throughput::Elements(OPS as u64));
    
    group.bench_function("statig_async_cold", |b| {
        b.to_async(&rt).iter(|| async {
            // Create state machine ONCE per iteration (like C# does)
            let mut sm = AsyncColdMachine::default().uninitialized_state_machine().init().await;
            let event = AsyncEvent::Next;
            
            // Measure only the state transitions
            for _ in 0..OPS {
                sm.handle(black_box(&event)).await;
                black_box(sm.state());
            }
        })
    });
    group.finish();
}

criterion_group!(benches, bench_async);
criterion_main!(benches);