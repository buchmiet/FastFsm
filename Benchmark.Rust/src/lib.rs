//! Optimized finite state machine implementations for benchmarking

/// Number of iterations for benchmarks
pub const OPS: usize = 1_024;

/* ------------------------------------------------------------------------- */
/*  1. BASIC – A → B → C → A                                               */
/* ------------------------------------------------------------------------- */

#[derive(Copy, Clone, Debug)]
pub enum BasicState { A, B, C }

pub struct BasicStateMachine {
    state: BasicState,
}

impl BasicStateMachine {
    pub fn new() -> Self { 
        Self { state: BasicState::A } 
    }

    #[inline(always)]
    pub fn try_fire(&mut self, _event: BasicEvent) -> bool {
        let next = match self.state {
            BasicState::A => BasicState::B,
            BasicState::B => BasicState::C,
            BasicState::C => BasicState::A,
        };
        self.state = next;
        true
    }
    
    #[inline(always)]
    pub fn state(&self) -> BasicState {
        self.state
    }
}

#[derive(Debug, Clone)]
pub enum BasicEvent {
    Next,
}

/* ------------------------------------------------------------------------- */
/*  2. GUARDS + ACTIONS – counter-based guard and increment action          */
/* ------------------------------------------------------------------------- */

const MAX_COUNT: usize = 10;

#[derive(Copy, Clone, Debug)]
pub enum GuardState { Idle, Counting, Finished }

pub struct GuardStateMachine {
    state: GuardState,
    counter: usize,
}

impl GuardStateMachine {
    pub fn new() -> Self {
        Self { state: GuardState::Idle, counter: 0 }
    }

    #[inline(always)]
    pub fn try_fire(&mut self, _event: GuardEvent) -> bool {
        use GuardState::*;

        let next = match self.state {
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
        self.state = next;
        true
    }
    
    #[inline(always)]
    pub fn state(&self) -> GuardState {
        self.state
    }
}

#[derive(Debug, Clone)]
pub enum GuardEvent {
    Next,
}

/* ------------------------------------------------------------------------- */
/*  3. PAYLOAD – struct passed during transitions that affects an aggregate */
/* ------------------------------------------------------------------------- */

#[derive(Copy, Clone, Debug, Default)]
pub struct Payload { pub value: i32 }

#[derive(Copy, Clone, Debug)]
pub enum PayloadState { Start, Accumulate, Done }

pub struct PayloadStateMachine {
    state: PayloadState,
    sum: i32,
}

impl PayloadStateMachine {
    pub fn new() -> Self {
        Self { state: PayloadState::Start, sum: 0 }
    }

    #[inline(always)]
    pub fn try_fire(&mut self, event: PayloadEvent) -> bool {
        use PayloadState::*;
        
        let PayloadEvent::Next(payload) = event;

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
        true
    }
    
    #[inline(always)]
    pub fn state(&self) -> PayloadState {
        self.state
    }
}

#[derive(Debug, Clone)]
pub enum PayloadEvent {
    Next(Payload),
}

/* ------------------------------------------------------------------------- */
/*  4. ASYNC – State machine with async transitions using tokio            */
/* ------------------------------------------------------------------------- */

#[derive(Copy, Clone, Debug)]
pub enum AsyncState { A, B }

pub struct AsyncStateMachine {
    state: AsyncState,
}

impl AsyncStateMachine {
    pub fn new() -> Self {
        Self { state: AsyncState::A }
    }

    #[inline(always)]
    pub async fn try_fire(&mut self, _event: AsyncEvent) -> bool {
        tokio::task::yield_now().await;
        
        let next = match self.state {
            AsyncState::A => AsyncState::B,
            AsyncState::B => AsyncState::A,
        };
        self.state = next;
        true
    }
    
    #[inline(always)]
    pub fn state(&self) -> AsyncState {
        self.state
    }
}

#[derive(Debug, Clone)]
pub enum AsyncEvent {
    Next,
}

/* ------------------------------------------------------------------------- */
/*  5. ASYNC HOT PATH – State machine without yield_now                    */
/* ------------------------------------------------------------------------- */

pub struct AsyncHotStateMachine {
    state: AsyncState,
}

impl AsyncHotStateMachine {
    pub fn new() -> Self {
        Self { state: AsyncState::A }
    }

    #[inline(always)]
    pub async fn try_fire(&mut self, _event: AsyncEvent) -> bool {
        // No yield_now - hot path
        let next = match self.state {
            AsyncState::A => AsyncState::B,
            AsyncState::B => AsyncState::A,
        };
        self.state = next;
        true
    }
    
    #[inline(always)]
    pub fn state(&self) -> AsyncState {
        self.state
    }
}