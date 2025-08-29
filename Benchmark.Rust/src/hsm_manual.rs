//! Manual hierarchical state machine to mirror FastFSM HSM scenarios.
//! This avoids external macros to keep control over exact semantics.

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum HsmEvent {
    Next,
    SwitchParent,
    Ping,       // handled at parent only, internal (no state change)
    Restore,    // restore history
    ToComplete, // jump to Complete top-level to simulate external loop
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum P1Child { S1, S2 }

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum P2Child { T1, T2 }

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum DeepChild { G1, G2 }

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum AChild { Deep(DeepChild) }

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum HsmState {
    // Parent 1 with two direct children
    P1(P1Child),
    // Parent 2 with two direct children
    P2(P2Child),
    // For deep history scenario (P1 has nested A{G1,G2} / B)
    P1DeepA(AChild),
    P1DeepB,
    Complete,
}

// Setup knobs: choose shallow or deep model
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum HsmHierarchicalSetup { Basic }

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum HsmHistorySetup { Shallow, Deep }

#[derive(Debug)]
pub struct HsmMachine {
    pub state: HsmState,
    // History slots
    last_p1: Option<P1Child>,
    last_deep: Option<DeepChild>,
}

impl HsmMachine {
    pub fn new_basic() -> Self {
        Self { state: HsmState::P1(P1Child::S1), last_p1: None, last_deep: None }
    }

    pub fn new_shallow_history() -> Self {
        // start at P1.S1
        Self { state: HsmState::P1(P1Child::S1), last_p1: Some(P1Child::S1), last_deep: None }
    }

    pub fn new_deep_history() -> Self {
        // start at P1.A.G1
        Self { state: HsmState::P1DeepA(AChild::Deep(DeepChild::G1)), last_p1: None, last_deep: Some(DeepChild::G1) }
    }

    #[inline]
    pub fn state(&self) -> &HsmState { &self.state }

    // Parent-level handler (internal) – does not change child
    #[inline]
    fn on_parent_internal(&mut self, ev: &HsmEvent) -> bool {
        match ev {
            HsmEvent::Ping => true, // handled with no state change
            _ => false,
        }
    }

    // Hierarchical: cross-parent transition
    #[inline]
    fn switch_parent(&mut self) {
        self.state = match self.state {
            HsmState::P1(_) | HsmState::P1DeepA(_) | HsmState::P1DeepB => HsmState::P2(P2Child::T1),
            HsmState::P2(_) => HsmState::P1(P1Child::S1),
            HsmState::Complete => HsmState::P1(P1Child::S1),
        };
    }

    // Step inside children (used for preparation only)
    #[inline]
    pub fn step_child(&mut self) {
        self.state = match self.state {
            HsmState::P1(P1Child::S1) => { self.last_p1 = Some(P1Child::S2); HsmState::P1(P1Child::S2) }
            HsmState::P1(P1Child::S2) => { self.last_p1 = Some(P1Child::S1); HsmState::P1(P1Child::S1) }
            HsmState::P2(P2Child::T1) => HsmState::P2(P2Child::T2),
            HsmState::P2(P2Child::T2) => HsmState::P2(P2Child::T1),
            HsmState::P1DeepA(AChild::Deep(DeepChild::G1)) => { self.last_deep = Some(DeepChild::G2); HsmState::P1DeepA(AChild::Deep(DeepChild::G2)) }
            HsmState::P1DeepA(AChild::Deep(DeepChild::G2)) => { self.last_deep = Some(DeepChild::G1); HsmState::P1DeepA(AChild::Deep(DeepChild::G1)) }
            s => s,
        };
    }

    #[inline]
    pub fn handle_basic(&mut self, ev: &HsmEvent) {
        // Parent internal first
        if self.on_parent_internal(ev) { return; }
        match ev {
            HsmEvent::SwitchParent => self.switch_parent(),
            HsmEvent::Next => self.step_child(),
            HsmEvent::ToComplete => { self.state = HsmState::Complete; },
            HsmEvent::Restore => {
                // No-op in basic mode
            }
            HsmEvent::Ping => {}
        }
    }

    #[inline]
    pub fn handle_shallow_history(&mut self, ev: &HsmEvent) {
        if self.on_parent_internal(ev) { return; }
        match ev {
            HsmEvent::SwitchParent => self.switch_parent(),
            HsmEvent::Next => self.step_child(),
            HsmEvent::ToComplete => { self.state = HsmState::Complete; },
            HsmEvent::Restore => {
                // Restore last direct child of P1
                let child = self.last_p1.unwrap_or(P1Child::S1);
                self.state = HsmState::P1(child);
            }
            HsmEvent::Ping => {}
        }
    }

    #[inline]
    pub fn handle_deep_history(&mut self, ev: &HsmEvent) {
        if self.on_parent_internal(ev) { return; }
        match ev {
            HsmEvent::SwitchParent => self.switch_parent(),
            HsmEvent::Next => self.step_child(),
            HsmEvent::ToComplete => { self.state = HsmState::Complete; },
            HsmEvent::Restore => {
                // Restore last nested descendant
                let g = self.last_deep.unwrap_or(DeepChild::G1);
                self.state = HsmState::P1DeepA(AChild::Deep(g));
            }
            HsmEvent::Ping => {}
        }
    }
}
