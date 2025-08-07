use criterion::{criterion_group, criterion_main, Criterion, Throughput};
use fsm_benchmark::*;
use std::hint::black_box;

fn bench_basic(c: &mut Criterion) {
    let mut group = c.benchmark_group("basic");
    group.throughput(Throughput::Elements(OPS as u64));

    group.bench_function("statig", |b| {
        b.iter_custom(|iters| {
            let mut sm = black_box(BasicStateMachine::new());

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    let ok = sm.try_fire(BasicEvent::Next);
                    black_box(ok);
                    black_box(sm.state());
                }
            }
            start.elapsed()
        })
    });

    group.finish();
}

fn bench_guards_actions(c: &mut Criterion) {
    let mut group = c.benchmark_group("guards_actions");
    group.throughput(Throughput::Elements(OPS as u64));

    group.bench_function("statig", |b| {
        b.iter_custom(|iters| {
            let mut sm = black_box(GuardStateMachine::new());

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    let ok = sm.try_fire(GuardEvent::Next);
                    black_box(ok);
                    black_box(sm.state());
                }
            }
            start.elapsed()
        })
    });

    group.finish();
}

fn bench_payload(c: &mut Criterion) {
    let mut group = c.benchmark_group("payload");
    group.throughput(Throughput::Elements(OPS as u64));
    
    let payload = Payload { value: 42 };

    group.bench_function("statig", |b| {
        b.iter_custom(|iters| {
            let mut sm = black_box(PayloadStateMachine::new());
            let p = black_box(payload);

            let start = std::time::Instant::now();
            for _ in 0..iters {
                for _ in 0..OPS {
                    let ok = sm.try_fire(PayloadEvent::Next(p));
                    black_box(ok);
                    black_box(sm.state());
                }
            }
            start.elapsed()
        })
    });

    group.finish();
}

criterion_group!(benches, bench_basic, bench_guards_actions, bench_payload);
criterion_main!(benches);