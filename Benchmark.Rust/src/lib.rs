//! Minimal finite state machine implementations for benchmarking

// ─── lightweight stub so benchmarks compile without external crate ─────────────
pub mod fsmentry {
    #[derive(Clone)]
    pub struct StateMachine<S: Copy> { 
        state: S 
    }
    
    impl<S: Copy> StateMachine<S> {
        pub fn new(initial: S) -> Self { Self { state: initial } }
        pub fn state(&self) -> S { self.state }
        pub fn set_state(&mut self, s: S) { self.state = s; }
        pub fn replace(&mut self, s: S) { self.state = s; }
    }
}

/// Number of iterations an individual benchmark should perform.
pub const OPS: usize = 1_024;

/* ------------------------------------------------------------------------- */
/*  1. BASIC – A → B → C → A                                               */
/* ------------------------------------------------------------------------- */

// --- Simple manual implementation for statig-style benchmark --------------

#[derive(Copy, Clone, Debug)]
enum BasicState { A, B, C }

pub struct BasicStatig {
    state: BasicState,
}

impl BasicStatig {
    pub fn new() -> Self { 
        Self { state: BasicState::A } 
    }

    #[inline(never)]
    pub fn step(&mut self) {
        let next = match self.state {
            BasicState::A => BasicState::B,
            BasicState::B => BasicState::C,
            BasicState::C => BasicState::A,
        };
        self.state = next;
    }
}

// --- Simple manual implementation for fsmentry-style benchmark ------------

pub struct BasicFsmentry {
    fsm: fsmentry::StateMachine<BasicState>,
}

impl BasicFsmentry {
    pub fn new() -> Self { 
        Self { fsm: fsmentry::StateMachine::new(BasicState::A) } 
    }

    #[inline(never)]
    pub fn step(&mut self) {
        let next = match self.fsm.state() {
            BasicState::A => BasicState::B,
            BasicState::B => BasicState::C,
            BasicState::C => BasicState::A,
        };
        self.fsm.replace(next);
    }
}

/* ------------------------------------------------------------------------- */
/*  2. GUARDS + ACTIONS – counter-based guard and increment action          */
/* ------------------------------------------------------------------------- */

const MAX_COUNT: usize = 10;

#[derive(Copy, Clone, Debug)]
enum CounterState { Idle, Counting, Finished }

// Guard & action for statig variant
pub struct GuardActionStatig {
    state: CounterState,
    counter: usize,
}

impl GuardActionStatig {
    pub fn new() -> Self {
        Self { state: CounterState::Idle, counter: 0 }
    }

    #[inline(never)]
    pub fn step(&mut self) {
        use CounterState::*;

        let next = match self.state {
            Idle => {
                // action – reset counter
                self.counter = 0;
                Counting
            }
            Counting => {
                // guard: only proceed while counter < MAX_COUNT
                if self.counter < MAX_COUNT {
                    self.counter += 1; // action – increment
                    Counting
                } else {
                    Finished
                }
            }
            Finished => Idle,
        };
        self.state = next;
    }
}

// Guard & action for fsmentry variant
pub struct GuardActionFsmentry {
    fsm: fsmentry::StateMachine<CounterState>,
    counter: usize,
}

impl GuardActionFsmentry {
    pub fn new() -> Self {
        Self { fsm: fsmentry::StateMachine::new(CounterState::Idle), counter: 0 }
    }

    #[inline(never)]
    pub fn step(&mut self) {
        use CounterState::*;

        let next = match self.fsm.state() {
            Idle => {
                self.counter = 0;
                Counting
            }
            Counting => {
                if self.counter < MAX_COUNT {
                    self.counter += 1;
                    Counting
                } else {
                    Finished
                }
            }
            Finished => Idle,
        };
        self.fsm.replace(next);
    }
}

/* ------------------------------------------------------------------------- */
/*  3. PAYLOAD – struct passed during transitions that affects an aggregate */
/* ------------------------------------------------------------------------- */

#[derive(Copy, Clone, Debug, Default)]
pub struct Payload { pub value: i32 }

#[derive(Copy, Clone, Debug)]
enum PayloadState { Start, Accumulate, Done }

pub struct PayloadStatig {
    state: PayloadState,
    sum: i32,
}

impl PayloadStatig {
    pub fn new() -> Self {
        Self { state: PayloadState::Start, sum: 0 }
    }

    #[inline(never)]
    pub fn step(&mut self, payload: Payload) {
        use PayloadState::*;

        let next = match self.state {
            Start => {
                self.sum = 0;
                Accumulate
            }
            Accumulate => {
                self.sum += payload.value;
                if self.sum.abs() > 1_000 {
                    Done
                } else {
                    Accumulate
                }
            }
            Done => Start,
        };
        self.state = next;
    }
}

pub struct PayloadFsmentry {
    fsm: fsmentry::StateMachine<PayloadState>,
    sum: i32,
}

impl PayloadFsmentry {
    pub fn new() -> Self {
        Self { fsm: fsmentry::StateMachine::new(PayloadState::Start), sum: 0 }
    }

    #[inline(never)]
    pub fn step(&mut self, payload: Payload) {
        use PayloadState::*;

        let next = match self.fsm.state() {
            Start => {
                self.sum = 0;
                Accumulate
            }
            Accumulate => {
                self.sum += payload.value;
                if self.sum.abs() > 1_000 {
                    Done
                } else {
                    Accumulate
                }
            }
            Done => Start,
        };
        self.fsm.replace(next);
    }
}