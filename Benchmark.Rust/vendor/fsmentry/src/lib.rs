//! A *very* small stub of the (hypothetical) `fsmentry` crate so that the
//! benchmarks compile without external network access.  It only delivers what
//! is strictly necessary for the benchmark implementation.

#[derive(Debug, Clone)]
pub struct StateMachine<S> {
    state: S,
}

impl<S> StateMachine<S> {
    pub fn new(initial: S) -> Self { Self { state: initial } }
    pub fn state(&self) -> &S { &self.state }
    pub fn replace(&mut self, next: S) { self.state = next; }
}

// Provide a dummy DSL macro that does nothing but keeps downstream sources
// compiling if they happen to invoke it.
#[macro_export]
macro_rules! fsm {
    ( $( $tt:tt )* ) => { /* no-op */ };
}

