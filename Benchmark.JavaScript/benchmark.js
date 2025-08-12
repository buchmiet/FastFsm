// benchmark.js
import { bench, run, group } from 'mitata';

// Stałe
const OPS = 1024;

// "Enumy" jako obiekty
const State = { A: 'A', B: 'B', C: 'C' };
const Trigger = { Next: 'NEXT' };

// ============================================================
// 1. MINIMALNA IMPLEMENTACJA JavaScript
// ============================================================

class MinimalFSM {
  constructor() {
    this.state = State.A;
  }
  
  transition(trigger) {
    switch (this.state) {
      case State.A:
        if (trigger === Trigger.Next) this.state = State.B;
        break;
      case State.B:
        if (trigger === Trigger.Next) this.state = State.C;
        break;
      case State.C:
        if (trigger === Trigger.Next) this.state = State.A;
        break;
    }
  }
  
  canFire(trigger) {
    return trigger === Trigger.Next;
  }
  
  getPermittedTriggers() {
    return [Trigger.Next];
  }
}

class MinimalFSMWithGuards {
  constructor() {
    this.state = State.A;
    this.counter = 0;
    this.GUARD_LIMIT = 2147483647; // INT32_MAX
  }
  
  transition(trigger) {
    if (this.counter >= this.GUARD_LIMIT) return;
    
    switch (this.state) {
      case State.A:
        if (trigger === Trigger.Next) {
          this.counter++;
          this.state = State.B;
        }
        break;
      case State.B:
        if (trigger === Trigger.Next) {
          this.counter++;
          this.state = State.C;
        }
        break;
      case State.C:
        if (trigger === Trigger.Next) {
          this.counter++;
          this.state = State.A;
        }
        break;
    }
  }
}

class MinimalFSMWithPayload {
  constructor() {
    this.state = State.A;
    this.sum = 0;
  }
  
  transition(trigger, payload) {
    switch (this.state) {
      case State.A:
        if (trigger === Trigger.Next) {
          this.sum += payload.value;
          this.state = State.B;
        }
        break;
      case State.B:
        if (trigger === Trigger.Next) {
          this.sum += payload.value;
          this.state = State.C;
        }
        break;
      case State.C:
        if (trigger === Trigger.Next) {
          this.sum += payload.value;
          this.state = State.A;
        }
        break;
    }
  }
}

// ============================================================
// 2. WERSJA BEZ KLAS (funkcjonalna)
// ============================================================

function createFunctionalFSM() {
  let state = State.A;
  
  return {
    transition(trigger) {
      switch (state) {
        case State.A:
          if (trigger === Trigger.Next) state = State.B;
          break;
        case State.B:
          if (trigger === Trigger.Next) state = State.C;
          break;
        case State.C:
          if (trigger === Trigger.Next) state = State.A;
          break;
      }
    },
    
    canFire(trigger) {
      return trigger === Trigger.Next;
    },
    
    getCurrentState() {
      return state;
    }
  };
}

// ============================================================
// 3. WERSJA Z LOOKUP TABLE (bez switch)
// ============================================================

class LookupTableFSM {
  constructor() {
    this.state = State.A;
    this.transitions = {
      [State.A]: { [Trigger.Next]: State.B },
      [State.B]: { [Trigger.Next]: State.C },
      [State.C]: { [Trigger.Next]: State.A }
    };
  }
  
  transition(trigger) {
    const nextState = this.transitions[this.state]?.[trigger];
    if (nextState) {
      this.state = nextState;
    }
  }
  
  canFire(trigger) {
    return !!this.transitions[this.state]?.[trigger];
  }
}

// ============================================================
// 4. ASYNC VERSIONS
// ============================================================

class MinimalFSMAsyncHot {
  constructor() {
    this.state = State.A;
    this.asyncCounter = 0;
  }
  
  async transitionAsync(trigger) {
    switch (this.state) {
      case State.A:
        if (trigger === Trigger.Next) {
          this.asyncCounter++;
          this.state = State.B;
        }
        break;
      case State.B:
        if (trigger === Trigger.Next) {
          this.asyncCounter++;
          this.state = State.C;
        }
        break;
      case State.C:
        if (trigger === Trigger.Next) {
          this.asyncCounter++;
          this.state = State.A;
        }
        break;
    }
    return Promise.resolve();
  }
}

class MinimalFSMAsyncYield {
  constructor() {
    this.state = State.A;
    this.asyncCounter = 0;
  }
  
  async transitionAsync(trigger) {
    // Symulacja Task.Yield()
    await new Promise(resolve => setImmediate(resolve));
    
    switch (this.state) {
      case State.A:
        if (trigger === Trigger.Next) {
          this.asyncCounter++;
          this.state = State.B;
        }
        break;
      case State.B:
        if (trigger === Trigger.Next) {
          this.asyncCounter++;
          this.state = State.C;
        }
        break;
      case State.C:
        if (trigger === Trigger.Next) {
          this.asyncCounter++;
          this.state = State.A;
        }
        break;
    }
  }
}

// ============================================================
// SETUP
// ============================================================

const minimalBasic = new MinimalFSM();
const minimalGuarded = new MinimalFSMWithGuards();
const minimalPayload = new MinimalFSMWithPayload();
const functionalFSM = createFunctionalFSM();
const lookupFSM = new LookupTableFSM();
const minimalAsyncHot = new MinimalFSMAsyncHot();
const minimalAsyncYield = new MinimalFSMAsyncYield();

const payload = { value: 42, message: "test" };

// ============================================================
// HELPER
// ============================================================

function runOps(fn) {
  for (let i = 0; i < OPS; i++) {
    fn();
  }
}

// ============================================================
// BENCHMARKS
// ============================================================

console.log(`\n${'='.repeat(60)}`);
console.log(`🚀 JavaScript State Machine Benchmarks (Pure JS, no TS)`);
console.log(`${'='.repeat(60)}`);
console.log(`Runtime: Bun ${typeof Bun !== 'undefined' ? Bun.version : '(or Node.js)'}`);
console.log(`Operations per iteration: ${OPS}`);
console.log(`${'='.repeat(60)}\n`);

// Basic Transitions - różne implementacje
group('Basic Transitions', () => {
  bench('Class with switch', () => {
    runOps(() => minimalBasic.transition(Trigger.Next));
  });
  
  bench('Functional (closure)', () => {
    runOps(() => functionalFSM.transition(Trigger.Next));
  });
  
  bench('Lookup table', () => {
    runOps(() => lookupFSM.transition(Trigger.Next));
  });
});

// Guards + Actions
group('Guards + Actions', () => {
  bench('Class with switch', () => {
    runOps(() => minimalGuarded.transition(Trigger.Next));
  });
});

// Payload
group('Payload', () => {
  bench('Class with switch', () => {
    runOps(() => minimalPayload.transition(Trigger.Next, payload));
  });
});

// Can Fire Check
group('Can Fire Check', () => {
  bench('Class with switch', () => {
    runOps(() => minimalBasic.canFire(Trigger.Next));
  });
  
  bench('Lookup table', () => {
    runOps(() => lookupFSM.canFire(Trigger.Next));
  });
});

// Get Permitted Triggers
group('Get Permitted Triggers', () => {
  bench('Class with switch', () => {
    runOps(() => minimalBasic.getPermittedTriggers());
  });
});

// Async Hot Path
group('Async Hot Path (no yield)', () => {
  bench('Class async hot', async () => {
    for (let i = 0; i < OPS; i++) {
      await minimalAsyncHot.transitionAsync(Trigger.Next);
    }
  });
});

// Async With Yield
group('Async With Yield', () => {
  bench('Class async yield', async () => {
    for (let i = 0; i < OPS; i++) {
      await minimalAsyncYield.transitionAsync(Trigger.Next);
    }
  });
});

// Uruchom benchmarki
await run({
  avg: true,
  json: false,
  colors: true,
  min_max: true,
  percentiles: false,
});


