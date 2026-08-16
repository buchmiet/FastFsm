use statig::prelude::*;

const MAX_COUNT: usize = usize::MAX; // Like int.MaxValue in C#

#[derive(Default)]
pub struct GuardMachine {
    pub counter: usize,
}

#[derive(Debug, Clone)]
pub enum GuardEvent {
    Next,
}

// Match C# exactly: Idle -> Counting -> Finished -> Idle
// Counter increments on EXIT from Counting (like C# OnExit)
#[state_machine(initial = "State::idle()")]
impl GuardMachine {
    #[state]
    fn idle(&mut self, event: &GuardEvent) -> Outcome<State> {
        match event {
            GuardEvent::Next => {
                self.counter = 0; // Reset counter like C#
                Transition(State::counting())
            }
        }
    }

    #[state(exit_action = "increment_counter")]
    fn counting(&mut self, event: &GuardEvent) -> Outcome<State> {
        match event {
            GuardEvent::Next => {
                // Check guard (always true with usize::MAX, like C# with int.MaxValue)
                if self.counter < MAX_COUNT {
                    Transition(State::finished())
                } else {
                    // This branch will never execute, but matches C# structure
                    Transition(State::finished())
                }
            }
        }
    }

    #[state]
    fn finished(&mut self, event: &GuardEvent) -> Outcome<State> {
        match event {
            GuardEvent::Next => Transition(State::idle()),
        }
    }

    #[action]
    fn increment_counter(&mut self) {
        // Increment on exit from counting (matches C# OnExit)
        self.counter = self.counter.wrapping_add(1);
    }
}