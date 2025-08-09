use statig::prelude::*;
pub use crate::AsyncEvent; 


#[derive(Default)]
pub struct AsyncColdMachine;



// Cold path with yield_now (like C# Task.Yield)
#[state_machine(initial = "State::a()")]
impl AsyncColdMachine {
    #[state]
    async fn a(event: &AsyncEvent) -> Outcome<State> {
        match event {
            AsyncEvent::Next => {
                tokio::task::yield_now().await; // Like Task.Yield in C#
                Transition(State::b())
            }
        }
    }

    #[state]
    async fn b(event: &AsyncEvent) -> Outcome<State> {
        match event {
            AsyncEvent::Next => {
                tokio::task::yield_now().await; // Like Task.Yield in C#
                Transition(State::c())
            }
        }
    }

    #[state]
    async fn c(event: &AsyncEvent) -> Outcome<State> {
        match event {
            AsyncEvent::Next => {
                tokio::task::yield_now().await; // Like Task.Yield in C#
                Transition(State::a())
            }
        }
    }
}