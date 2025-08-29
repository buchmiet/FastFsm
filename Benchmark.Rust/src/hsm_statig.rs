use statig::prelude::*;
use statig::blocking::StateMachine;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum HsmEvent {
    Next,
    SwitchParent,
    Ping,
    Restore,
    RestoreDeep,
    ToComplete,
    EnterDeep,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum P1Child { S1, S2 }

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum DeepChild { G1, G2 }

pub struct StatigHsm {
    // Shallow history for P1
    last_p1: P1Child,
    // Deep history for P1.A
    last_deep: DeepChild,
}

impl Default for StatigHsm {
    fn default() -> Self {
        Self { last_p1: P1Child::S1, last_deep: DeepChild::G1 }
    }
}

#[state_machine(
    initial = "State::p1_s1()",
    state(derive(Copy, Clone)),
    superstate(derive(Copy, Clone))
)]
impl StatigHsm {
    // ===================== P1 subtree =====================
    #[state(superstate = "p1", exit_action = "exit_p1_s1")]
    fn p1_s1(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::Next => Transition(State::p1_s2()),
            _ => Super,
        }
    }

    #[state(superstate = "p1", exit_action = "exit_p1_s2")]
    fn p1_s2(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::Next => Transition(State::p1_s1()),
            _ => Super,
        }
    }

    #[action]
    fn exit_p1_s1(&mut self) { self.last_p1 = P1Child::S1; }
    #[action]
    fn exit_p1_s2(&mut self) { self.last_p1 = P1Child::S2; }

    // Parent-level handler (internal) and switches
    #[superstate]
    fn p1(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::Ping => Handled, // internal, no transition
            HsmEvent::SwitchParent => Transition(State::p2_t1()),
            HsmEvent::ToComplete => Transition(State::complete()),
            HsmEvent::EnterDeep => Transition(State::p1a_g1()),
            HsmEvent::Restore => {
                // Shallow restore to last direct child
                match self.last_p1 {
                    P1Child::S1 => Transition(State::p1_s1()),
                    P1Child::S2 => Transition(State::p1_s2()),
                }
            }
            _ => Super,
        }
    }

    // ===================== P2 subtree =====================
    #[state(superstate = "p2")]
    fn p2_t1(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::Next => Transition(State::p2_t2()),
            _ => Super,
        }
    }

    #[state(superstate = "p2")]
    fn p2_t2(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::Next => Transition(State::p2_t1()),
            _ => Super,
        }
    }

    #[superstate]
    fn p2(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::SwitchParent => Transition(State::p1_s1()),
            HsmEvent::ToComplete => Transition(State::complete()),
            _ => Super,
        }
    }

    // ===================== Deep subtree P1.A =====================
    #[state(superstate = "p1a", exit_action = "exit_p1a_g1")]
    fn p1a_g1(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::Next => Transition(State::p1a_g2()),
            _ => Super,
        }
    }

    #[state(superstate = "p1a", exit_action = "exit_p1a_g2")]
    fn p1a_g2(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::Next => Transition(State::p1a_g1()),
            _ => Super,
        }
    }

    #[action]
    fn exit_p1a_g1(&mut self) { self.last_deep = DeepChild::G1; }
    #[action]
    fn exit_p1a_g2(&mut self) { self.last_deep = DeepChild::G2; }

    #[superstate(superstate = "p1")]
    fn p1a(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            HsmEvent::RestoreDeep => match self.last_deep {
                DeepChild::G1 => Transition(State::p1a_g1()),
                DeepChild::G2 => Transition(State::p1a_g2()),
            },
            _ => Super,
        }
    }

    // ===================== Terminal =====================
    #[state]
    fn complete(&mut self, event: &HsmEvent) -> Outcome<State> {
        match event {
            // Allow going back to P1 or P1.A via Restore for history benches
            HsmEvent::Restore => match self.last_p1 {
                P1Child::S1 => Transition(State::p1_s1()),
                P1Child::S2 => Transition(State::p1_s2()),
            },
            HsmEvent::RestoreDeep => match self.last_deep {
                DeepChild::G1 => Transition(State::p1a_g1()),
                DeepChild::G2 => Transition(State::p1a_g2()),
            },
            _ => Handled,
        }
    }
}

impl StatigHsm {
    pub fn sm_init() -> StateMachine<StatigHsm> {
        let mut sm = StatigHsm { last_p1: P1Child::S1, last_deep: DeepChild::G1 }.state_machine();
        // Ensure initial history marks are set
        sm
    }
}
