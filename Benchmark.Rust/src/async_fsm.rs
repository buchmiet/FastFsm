// Async benchmarks - not implemented yet
// Focus on sync benchmarks only

use criterion::{criterion_group, criterion_main};

fn dummy_bench(_c: &mut criterion::Criterion) {
    // Placeholder - async not needed for now
}

criterion_group!(benches, dummy_bench);
criterion_main!(benches);