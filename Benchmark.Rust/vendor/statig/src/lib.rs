//! Minimal stub of the `statig` crate so that the benchmark project can be
//! compiled in an offline environment.  THIS IS **NOT** THE REAL CRATE.
//! It only provides a very small subset of functionality that is needed by
//! the benchmark sources contained in this repository.

/// A trivial finite-state-machine wrapper that stores the current state in a
/// field and gives direct accessors for reading / writing it.  The public API
/// intentionally resembles what one would expect from a real FSM helper – but
/// its implementation is purposely kept minimal.
#[derive(Debug, Clone)]
pub struct Machine<S> {
    state: S,
}

impl<S> Machine<S> {
    /// Creates a new `Machine` initialised with `initial` state.
    pub fn new(initial: S) -> Self { Self { state: initial } }

    /// Returns an immutable reference to the current state.
    pub fn state(&self) -> &S { &self.state }

    /// Consumes the machine and returns its contained state.
    pub fn into_state(self) -> S { self.state }

    /// Replaces the current state with `next`.
    pub fn set_state(&mut self, next: S) { self.state = next; }
}

// Re-export a dummy `states!` macro so code that expects it still compiles.
#[macro_export]
macro_rules! states {
    ( $( $tt:tt )* ) => { /* ignored – just for compatibility */ };
}

