use criterion::{black_box, criterion_group, criterion_main, Criterion, Throughput};
use futures::executor::LocalPool;
use futures::task::LocalSpawnExt;
use async_trait::async_trait;
use std::future::Future;
use std::pin::Pin;
use std::task::{Context, Poll};

const OPS: u32 = 1024;

// Simple yield_now implementation
pub fn yield_now() -> impl Future<Output = ()> {
    struct YieldNow {
        yielded: bool,
    }

    impl Future for YieldNow {
        type Output = ();

        fn poll(mut self: Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<Self::Output> {
            if self.yielded {
                return Poll::Ready(());
            }

            self.yielded = true;
            cx.waker().wake_by_ref();
            Poll::Pending
        }
    }

    YieldNow { yielded: false }
}

// Common async state machine trait
#[async_trait]
trait AsyncMachine: Send {
    async fn fire(&mut self);
}

// Simple two-state async machine using statig
#[derive(Clone, Copy, Debug)]
enum State {
    A,
    B,
}

// Statig async machine: A ⇄ B with yield in state B
// fsmentry crate (0.4.x) nie posiada wersji async – dlatego w wariancie
// async benchmarkowany jest wyłącznie statig.
struct StatigAsync {
    state: State,
}

impl StatigAsync {
    fn new() -> Self {
        Self { state: State::A }
    }
}

#[async_trait]
impl AsyncMachine for StatigAsync {
    #[inline(never)]
    async fn fire(&mut self) {
        match self.state {
            State::A => {
                self.state = State::B;
                black_box(&self.state);
            }
            State::B => {
                // Yield control back to executor - similar to Task.Yield() in .NET
                // LocalPool is single-threaded, mimicking .NET's cooperative scheduling
                yield_now().await;
                self.state = State::A;
                black_box(&self.state);
            }
        }
    }
}

fn bench_async(c: &mut Criterion) {
    let mut group = c.benchmark_group("statig_async_yield");
    group.throughput(Throughput::Elements(OPS.into()));

    // Benchmark only StatigAsync since fsmentry doesn't support async
    group.bench_function("statig_async", |b| {
        b.iter_custom(|iters| {
            // Setup executor - LocalPool is single-threaded like .NET's Task.Yield behavior
            let mut pool = LocalPool::new();
            let spawner = pool.spawner();
            
            let start = std::time::Instant::now();
            
            for _ in 0..iters {
                // Create machine outside of timing (setup not measured)
                let mut machine = black_box(StatigAsync::new());
                
                // Spawn async task that performs OPS transitions
                spawner.spawn_local(async move {
                    for _ in 0..OPS {
                        machine.fire().await;
                    }
                    // Prevent dead code elimination
                    std::mem::forget(black_box(machine));
                }).unwrap();
            }
            
            // Run all spawned futures to completion
            pool.run();
            
            start.elapsed()
        })
    });

    group.finish();
}

criterion_group!(benches, bench_async);
criterion_main!(benches);