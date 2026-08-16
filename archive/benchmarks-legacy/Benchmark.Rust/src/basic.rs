use statig::prelude::*;

#[derive(Default)]
pub struct BasicMachine;

#[derive(Debug, Clone)]
pub enum BasicEvent {
    Next,
}

#[state_machine(initial = "State::a()")]
impl BasicMachine {
    #[state]
    fn a(event: &BasicEvent) -> Outcome<State> {
        match event {
            BasicEvent::Next => Transition(State::b()),
        }
    }

    #[state]
    fn b(event: &BasicEvent) -> Outcome<State> {
        match event {
            BasicEvent::Next => Transition(State::c()),
        }
    }

    #[state]
    fn c(event: &BasicEvent) -> Outcome<State> {
        match event {
            BasicEvent::Next => Transition(State::a()),
        }
    }
}