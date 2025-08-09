use statig::prelude::*;

#[derive(Copy, Clone, Debug, Default)]
pub struct Payload { 
    pub value: i32 
}

#[derive(Default)]
pub struct PayloadMachine {
    pub sum: i32,
}

#[derive(Debug, Clone)]
pub enum PayloadEvent {
    Next(Payload),
}

#[state_machine(initial = "State::start()")]
impl PayloadMachine {
    #[state]
    fn start(&mut self, event: &PayloadEvent) -> Outcome<State> {
        match event {
            PayloadEvent::Next(_) => {
                self.sum = 0;
                Transition(State::accumulate())
            }
        }
    }

    #[state]
    fn accumulate(&mut self, event: &PayloadEvent) -> Outcome<State> {
        match event {
            PayloadEvent::Next(payload) => {
                self.sum = self.sum.wrapping_add(payload.value);
                Transition(State::done())
            }
        }
    }

    #[state]
    fn done(&mut self, event: &PayloadEvent) -> Outcome<State> {
        match event {
            PayloadEvent::Next(_) => Transition(State::start()),
        }
    }
}