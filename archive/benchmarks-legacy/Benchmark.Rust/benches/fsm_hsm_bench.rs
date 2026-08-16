use criterion::{criterion_group, criterion_main, Criterion, Throughput};
use fsm_benchmark::*;
use std::hint::black_box;

fn bench_hsm_basic_transition(c: &mut Criterion) {
    let mut group = c.benchmark_group("hsm_hierarchical_transition");
    group.throughput(Throughput::Elements(OPS as u64));

    group.bench_function("hsm_manual", |b| {
        let mut sm = HsmMachine::new_basic();
        let ev = HsmEvent::SwitchParent;
        b.iter(|| {
            for _ in 0..OPS {
                sm.handle_basic(black_box(&ev));
                black_box(sm.state());
            }
        })
    });

    group.finish();

    let mut group = c.benchmark_group("hsm_hierarchical_transition");
    group.throughput(Throughput::Elements(OPS as u64));
    group.bench_function("statig", |b| {
        let mut sm = StatigHsm::sm_init();
        let ev = StatigHsmEvent::SwitchParent;
        b.iter(|| {
            for _ in 0..OPS {
                sm.handle(black_box(&ev));
            }
        })
    });
    group.finish();
}

fn bench_hsm_internal_parent(c: &mut Criterion) {
    let mut group = c.benchmark_group("hsm_internal_parent");
    group.throughput(Throughput::Elements(OPS as u64));

    group.bench_function("hsm_manual", |b| {
        let mut sm = HsmMachine::new_basic();
        let ev = HsmEvent::Ping;
        b.iter(|| {
            for _ in 0..OPS {
                sm.handle_basic(black_box(&ev));
                black_box(sm.state());
            }
        })
    });

    group.finish();

    let mut group = c.benchmark_group("hsm_internal_parent");
    group.throughput(Throughput::Elements(OPS as u64));
    group.bench_function("statig", |b| {
        let mut sm = StatigHsm::sm_init();
        let ev = StatigHsmEvent::Ping;
        b.iter(|| {
            for _ in 0..OPS {
                sm.handle(black_box(&ev));
            }
        })
    });
    group.finish();
}

fn bench_hsm_history_shallow(c: &mut Criterion) {
    let mut group = c.benchmark_group("hsm_history_shallow");
    group.throughput(Throughput::Elements(OPS as u64));

    group.bench_function("hsm_manual", |b| {
        let mut sm = HsmMachine::new_shallow_history();
        // Prepare: move to S2, then leave to Complete so each restore starts same way
        sm.step_child(); // P1.S2
        sm.handle_shallow_history(&HsmEvent::ToComplete);
        b.iter(|| {
            for _ in 0..OPS {
                sm.handle_shallow_history(black_box(&HsmEvent::Restore));
                sm.handle_shallow_history(black_box(&HsmEvent::ToComplete));
                black_box(sm.state());
            }
        })
    });

    group.finish();

    let mut group = c.benchmark_group("hsm_history_shallow");
    group.throughput(Throughput::Elements(OPS as u64));
    group.bench_function("statig", |b| {
        let mut sm = StatigHsm::sm_init();
        // Prepare: go to S2 and then to Complete
        sm.handle(&StatigHsmEvent::Next);
        sm.handle(&StatigHsmEvent::ToComplete);
        b.iter(|| {
            for _ in 0..OPS {
                sm.handle(black_box(&StatigHsmEvent::Restore));
                sm.handle(black_box(&StatigHsmEvent::ToComplete));
            }
        })
    });
    group.finish();
}

fn bench_hsm_history_deep(c: &mut Criterion) {
    let mut group = c.benchmark_group("hsm_history_deep");
    group.throughput(Throughput::Elements(OPS as u64));

    group.bench_function("hsm_manual", |b| {
        let mut sm = HsmMachine::new_deep_history();
        // Prepare: reach deep G2, then leave to Complete
        sm.step_child(); // A.G2
        sm.handle_deep_history(&HsmEvent::ToComplete);
        b.iter(|| {
            for _ in 0..OPS {
                sm.handle_deep_history(black_box(&HsmEvent::Restore));
                sm.handle_deep_history(black_box(&HsmEvent::ToComplete));
                black_box(sm.state());
            }
        })
    });

    group.finish();

    let mut group = c.benchmark_group("hsm_history_deep");
    group.throughput(Throughput::Elements(OPS as u64));
    group.bench_function("statig", |b| {
        let mut sm = StatigHsm::sm_init();
        // Enter deep branch and reach G2, then go to Complete
        sm.handle(&StatigHsmEvent::EnterDeep); // P1.A.G1
        sm.handle(&StatigHsmEvent::Next);      // P1.A.G2
        sm.handle(&StatigHsmEvent::ToComplete);
        b.iter(|| {
            for _ in 0..OPS {
                sm.handle(black_box(&StatigHsmEvent::RestoreDeep));
                sm.handle(black_box(&StatigHsmEvent::ToComplete));
            }
        })
    });
    group.finish();
}

criterion_group!(benches,
    bench_hsm_basic_transition,
    bench_hsm_internal_parent,
    bench_hsm_history_shallow,
    bench_hsm_history_deep);
criterion_main!(benches);
