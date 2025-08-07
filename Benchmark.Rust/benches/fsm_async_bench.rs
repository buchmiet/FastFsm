use criterion::{black_box, criterion_group, criterion_main, Criterion, Throughput};
use fsm_benchmark::*;

fn bench_async(c: &mut Criterion) {
    let mut group = c.benchmark_group("statig_async_yield");
    group.throughput(Throughput::Elements(OPS as u64));

    // Build multi-threaded tokio runtime
    let rt = tokio::runtime::Builder::new_multi_thread()
        .worker_threads(num_cpus::get())
        .enable_time()
        .build()
        .unwrap();

    group.bench_function("statig_async", |b| {
        b.iter_custom(|iters| {
            rt.block_on(async {
                let mut sm = black_box(AsyncStateMachine::new());

                let start = std::time::Instant::now();
                for _ in 0..iters {
                    for _ in 0..OPS {
                        sm.try_fire(AsyncEvent::Next).await;
                        black_box(&sm);
                    }
                }
                start.elapsed()
            })
        })
    });

    group.finish();
}

criterion_group!(benches, bench_async);
criterion_main!(benches);