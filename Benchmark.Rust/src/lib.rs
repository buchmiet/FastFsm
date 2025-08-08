//! FSM implementations using statig library for benchmarking

/// Number of iterations for benchmarks - matches C#
pub const OPS: usize = 1_048_576; // 1M operations like in C#

// Sync modules from separate files
mod basic;
mod guard;
mod payload;

// Async modules from separate files
mod async_hot;
mod async_cold;
pub mod async_event;         
pub use async_event::AsyncEvent; 

// Re-exports for sync
pub use basic::{BasicMachine, BasicEvent};
pub use guard::{GuardMachine, GuardEvent};
pub use payload::{PayloadMachine, PayloadEvent, Payload};

// Re-exports for async
pub use async_hot::{AsyncHotMachine};
pub use async_cold::{AsyncColdMachine};