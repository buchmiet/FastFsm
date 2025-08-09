use criterion::{criterion_group, criterion_main, Criterion, Throughput};
use fsm_benchmark::*;
use statig::prelude::*;
use std::hint::black_box;

fn bench_basic(c: &mut Criterion) {
    let mut group = c.benchmark_group("basic");
    group.throughput(Throughput::Elements(OPS as u64));

    group.bench_function("statig", |b| {
        // Create state machine ONCE before measurements (like C# GlobalSetup)
        let mut sm = BasicMachine::default().uninitialized_state_machine().init();
        let event = BasicEvent::Next;
        
        b.iter(|| {
            // Only measure the actual state transitions
            for _ in 0..OPS {
                sm.handle(black_box(&event));
                black_box(sm.state());
            }
        })
    });

    group.finish();
}

fn bench_guards_actions(c: &mut Criterion) {
    let mut group = c.benchmark_group("guards_actions");
    group.throughput(Throughput::Elements(OPS as u64));

    group.bench_function("statig", |b| {
        // Create state machine ONCE before measurements
        let mut sm = GuardMachine::default().uninitialized_state_machine().init();
        let event = GuardEvent::Next;
        
        b.iter(|| {
            // Only measure the actual state transitions
            for _ in 0..OPS {
                sm.handle(black_box(&event));
                black_box(sm.state());
            }
        })
    });

    group.finish();
}

fn bench_payload(c: &mut Criterion) {
    let mut group = c.benchmark_group("payload");
    group.throughput(Throughput::Elements(OPS as u64));
    
    let payload = Payload { value: 42 };
    let event = PayloadEvent::Next(payload);

    group.bench_function("statig", |b| {
        // Create state machine ONCE before measurements
        let mut sm = PayloadMachine::default().uninitialized_state_machine().init();
        
        b.iter(|| {
            // Only measure the actual state transitions
            for _ in 0..OPS {
                sm.handle(black_box(&event));
                black_box(sm.state());
            }
        })
    });

    group.finish();
}

criterion_group!(benches, bench_basic, bench_guards_actions, bench_payload);
criterion_main!(benches);