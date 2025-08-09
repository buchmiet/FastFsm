use statig::prelude::*;
pub use crate::AsyncEvent; 


#[derive(Default)]
pub struct AsyncHotMachine;



// Hot path - no yielding (like C# Task.CompletedTask)
#[state_machine(initial = "State::a()")]
impl AsyncHotMachine {
    #[state]
    async fn a(event: &AsyncEvent) -> Outcome<State> {
        match event {
            AsyncEvent::Next => {
                // No yield - hot path like Task.CompletedTask
                Transition(State::b())
            }
        }
    }

    #[state]
    async fn b(event: &AsyncEvent) -> Outcome<State> {
        match event {
            AsyncEvent::Next => {
                // No yield - hot path like Task.CompletedTask
                Transition(State::c())
            }
        }
    }

    #[state]
    async fn c(event: &AsyncEvent) -> Outcome<State> {
        match event {
            AsyncEvent::Next => {
                // No yield - hot path like Task.CompletedTask
                Transition(State::a())
            }
        }
    }
}