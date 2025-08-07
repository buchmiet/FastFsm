use criterion::{black_box, criterion_group, criterion_main, Criterion, Throughput};
use fsm_benchmark::*;

const OPS: u32 = 1024;

fn bench_basic(c: &mut Criterion) {
    let mut group = c.benchmark_group("basic");
    group.throughput(Throughput::Elements(OPS.into()));

    group.bench_function("statig", |b| {
        b.iter_custom(|iters| {
            // SETUP - not measured
            let mut sm = black_box(BasicStatig::new());

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    sm.step();
                    black_box(&sm);
                }
            }
            start.elapsed()
        })
    });

    group.bench_function("fsmentry", |b| {
        b.iter_custom(|iters| {
            // SETUP - not measured
            let mut sm = black_box(BasicFsmentry::new());

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    sm.step();
                    black_box(&sm);
                }
            }
            start.elapsed()
        })
    });

    group.finish();
}

fn bench_guards_actions(c: &mut Criterion) {
    let mut group = c.benchmark_group("guards_actions");
    group.throughput(Throughput::Elements(OPS.into()));

    group.bench_function("statig", |b| {
        b.iter_custom(|iters| {
            // SETUP - not measured
            let mut sm = black_box(GuardActionStatig::new());

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    sm.step();
                    black_box(&sm);
                }
            }
            start.elapsed()
        })
    });

    group.bench_function("fsmentry", |b| {
        b.iter_custom(|iters| {
            // SETUP - not measured
            let mut sm = black_box(GuardActionFsmentry::new());

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    sm.step();
                    black_box(&sm);
                }
            }
            start.elapsed()
        })
    });

    group.finish();
}

fn bench_payload(c: &mut Criterion) {
    let mut group = c.benchmark_group("payload");
    group.throughput(Throughput::Elements(OPS.into()));
    
    let payload = Payload { value: 42 };

    group.bench_function("statig", |b| {
        b.iter_custom(|iters| {
            // SETUP - not measured
            let mut sm = black_box(PayloadStatig::new());
            let p = black_box(payload);

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    sm.step(p);
                    black_box(&sm);
                }
            }
            start.elapsed()
        })
    });

    group.bench_function("fsmentry", |b| {
        b.iter_custom(|iters| {
            // SETUP - not measured
            let mut sm = black_box(PayloadFsmentry::new());
            let p = black_box(payload);

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    sm.step(p);
                    black_box(&sm);
                }
            }
            start.elapsed()
        })
    });

    group.finish();
}

criterion_group!(benches, bench_basic, bench_guards_actions, bench_payload);
criterion_main!(benches);