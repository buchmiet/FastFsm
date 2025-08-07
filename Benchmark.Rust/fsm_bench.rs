use criterion::{black_box, criterion_group, criterion_main, BenchmarkId, Criterion, Throughput};
use rustfsm_bench::*;
use rustfsm_bench::{Trigger as T, State as S};

fn bench_basic(c: &mut Criterion) {
    let mut group = c.benchmark_group("basic");
    group.throughput(Throughput::Elements(1024));
    group.bench_function(BenchmarkId::new("rust-fsm", "basic"), |b| {
        b.iter_batched(
            || BasicMachine::new(S::A),
            |mut sm| {
                for _ in 0..1024 {
                    black_box(sm.fire(T::Next)).unwrap();
                }
            },
            criterion::BatchSize::SmallInput,
        )
    });
    group.finish();
}

fn bench_guards_actions(c: &mut Criterion) {
    let mut group = c.benchmark_group("guards_actions");
    group.throughput(Throughput::Elements(1024));
    group.bench_function(BenchmarkId::new("rust-fsm", "guard"), |b| {
        b.iter_batched(
            || GuardActionMachine::new(S::A),
            |mut sm| {
                for _ in 0..1024 {
                    black_box(sm.fire(T::Next)).unwrap();
                }
            },
            criterion::BatchSize::SmallInput,
        )
    });
    group.finish();
}

fn bench_payload(c: &mut Criterion) {
    let mut group = c.benchmark_group("payload");
    group.throughput(Throughput::Elements(1024));
    let payload = PayloadData { value: 42, message: "test" };
    group.bench_function(BenchmarkId::new("rust-fsm", "payload"), |b| {
        b.iter_batched(
            || PayloadMachine::new(S::A),
            |mut sm| {
                for _ in 0..1024 {
                    black_box(sm.fire_with_payload(T::Next, &payload)).unwrap();
                }
            },
            criterion::BatchSize::SmallInput,
        )
    });
    group.finish();
}

criterion_group!(benches, bench_basic, bench_guards_actions, bench_payload);
criterion_main!(benches);
