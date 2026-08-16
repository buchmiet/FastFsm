// benchmark-final.ts
import { bench, run, group } from 'mitata';
// Library benchmarks
import { createMachine as createXMachine, createActor as createXActor, assign as xAssign } from 'xstate';
// Robot3 may present different exports in ESM/CJS; resolve dynamically for Node/Bun
let robot3ns: any;
try {
  robot3ns = await import('robot3');
} catch {
  // Fallback for Node resolution if needed
  robot3ns = await import('robot3/dist/machine.js');
}
const { createMachine: createRMachine, state: rState, transition: rTransition, guard: rGuard, reduce: rReduce, interpret: rInterpret } = robot3ns as any;

// Constants
const OPS = 1024;

// Shared types
enum State { A = 'A', B = 'B', C = 'C' }
enum Trigger { Next = 'NEXT' }

interface PayloadData {
  value: number;
  message: string;
}

// ============================================================
// 1) Minimal implementation (FastFSM-equivalent)
// ============================================================

class MinimalFSM {
  private state: State = State.A;
  
  transition(trigger: Trigger): void {
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
  
  canFire(trigger: Trigger): boolean {
    return trigger === Trigger.Next;
  }
  
  getPermittedTriggers(): Trigger[] {
    return [Trigger.Next];
  }
}

class MinimalFSMWithGuards {
  private state: State = State.A;
  private counter: number = 0;
  private readonly GUARD_LIMIT = 2147483647; // INT32_MAX
  
  transition(trigger: Trigger): void {
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
  private state: State = State.A;
  private sum: number = 0;
  
  transition(trigger: Trigger, payload: PayloadData): void {
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
// 2) Async hot path
// ============================================================

class MinimalFSMAsyncHot {
  private state: State = State.A;
  private asyncCounter: number = 0;
  
  async transitionAsync(trigger: Trigger): Promise<void> {
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

// ============================================================
// 3) Async with explicit yield
// ============================================================

class MinimalFSMAsyncYield {
  private state: State = State.A;
  private asyncCounter: number = 0;
  
  async transitionAsync(trigger: Trigger): Promise<void> {
    // Simulate Task.Yield(): force a scheduler switch
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
// Setup
// ============================================================

const minimalBasic = new MinimalFSM();
const minimalGuarded = new MinimalFSMWithGuards();
const minimalPayload = new MinimalFSMWithPayload();
const minimalAsyncHot = new MinimalFSMAsyncHot();
const minimalAsyncYield = new MinimalFSMAsyncYield();

const payload: PayloadData = { value: 42, message: "test" };

// ============================================================
// Benchmarks
// ============================================================

function runOps(fn: () => void): void {
  for (let i = 0; i < OPS; i++) {
    fn();
  }
}

console.log(`\n${'='.repeat(60)}`);
console.log(`🚀 TypeScript State Machine Benchmarks`);
console.log(`${'='.repeat(60)}`);
const runtime = (globalThis as any).Bun?.version ? `Bun ${(globalThis as any).Bun.version}` : `Node ${process.versions.node}`;
console.log(`Runtime: ${runtime}`);
console.log(`CPU: AMD Ryzen 5 9600X`);
console.log(`Operations per iteration: ${OPS}`);
console.log(`${'='.repeat(60)}\n`);

// Basic transitions
group('Basic Transitions', () => {
  bench('TypeScript Minimal (switch)', () => {
    runOps(() => minimalBasic.transition(Trigger.Next));
  });
});

// Guards + actions
group('Guards + Actions', () => {
  bench('TypeScript Minimal (switch)', () => {
    runOps(() => minimalGuarded.transition(Trigger.Next));
  });
});

// Payload
group('Payload', () => {
  bench('TypeScript Minimal (switch)', () => {
    runOps(() => minimalPayload.transition(Trigger.Next, payload));
  });
});

// CanFire check
group('Can Fire Check', () => {
  bench('TypeScript Minimal (switch)', () => {
    runOps(() => minimalBasic.canFire(Trigger.Next));
  });
});

// GetPermittedTriggers
group('Get Permitted Triggers', () => {
  bench('TypeScript Minimal (switch)', () => {
    runOps(() => minimalBasic.getPermittedTriggers());
  });
});

// Async hot path
group('Async Hot Path (no yield)', () => {
  bench('TypeScript Minimal - async hot', async () => {
    for (let i = 0; i < OPS; i++) {
      await minimalAsyncHot.transitionAsync(Trigger.Next);
    }
  });
});

// Async with yield
group('Async With Yield', () => {
  bench('TypeScript Minimal - async yield', async () => {
    for (let i = 0; i < OPS; i++) {
      await minimalAsyncYield.transitionAsync(Trigger.Next);
    }
  });
});

// ============================================================
// Library: XState
// ============================================================

// XState - basic A->B->C->A loop
const xBasicMachine = createXMachine({
  id: 'x-basic',
  initial: 'A',
  context: {},
  states: {
    A: { on: { NEXT: 'B' } },
    B: { on: { NEXT: 'C' } },
    C: { on: { NEXT: 'A' } }
  }
});
const xBasic = createXActor(xBasicMachine).start();

// XState - guards + actions (counter++) with guard limit
const GUARD_LIMIT = 2147483647;
const xGuardMachine = createXMachine({
  id: 'x-guard',
  initial: 'A',
  context: { counter: 0, GUARD_LIMIT },
  states: {
    A: { on: { NEXT: { target: 'B', guard: ({ context }: any) => context.counter < context.GUARD_LIMIT, actions: xAssign(({ context }: any) => ({ ...context, counter: context.counter + 1 })) } } },
    B: { on: { NEXT: { target: 'C', guard: ({ context }: any) => context.counter < context.GUARD_LIMIT, actions: xAssign(({ context }: any) => ({ ...context, counter: context.counter + 1 })) } } },
    C: { on: { NEXT: { target: 'A', guard: ({ context }: any) => context.counter < context.GUARD_LIMIT, actions: xAssign(({ context }: any) => ({ ...context, counter: context.counter + 1 })) } } }
  }
});
const xGuard = createXActor(xGuardMachine).start();

// XState - payload (sum += event.value)
const xPayloadMachine = createXMachine({
  id: 'x-payload',
  initial: 'A',
  context: { sum: 0 },
  states: {
    A: { on: { NEXT: { target: 'B', actions: xAssign((args: any) => ({ sum: args.context.sum + (args.event?.value ?? 0) })) } } },
    B: { on: { NEXT: { target: 'C', actions: xAssign((args: any) => ({ sum: args.context.sum + (args.event?.value ?? 0) })) } } },
    C: { on: { NEXT: { target: 'A', actions: xAssign((args: any) => ({ sum: args.context.sum + (args.event?.value ?? 0) })) } } }
  }
});
const xPayload = createXActor(xPayloadMachine).start();

// ============================================================
// XState HSM: nested states + shallow/deep history
// ============================================================

// 1) Hierarchical transition between parents (exit child+parent -> enter parent+child)
const xHsmBasicMachine = createXMachine({
  id: 'xhsm-basic',
  initial: 'P1',
  states: {
    P1: {
      id: 'xhsm-p1',
      initial: 'S1',
      // Parent-level internal handler (no state change)
      on: {
        PING: { actions: () => {} },
        // Cross-parent transition to P2.T1
        SWITCH: { target: 'P2.T1' }
      },
      states: {
        S1: { on: { NEXT: 'S2' } },
        S2: { on: { NEXT: 'S1' } }
      }
    },
    P2: {
      id: 'xhsm-p2',
      initial: 'T1',
      states: {
        T1: { on: { NEXT: 'T2' } },
        T2: { on: { NEXT: 'T1' } }
      }
    }
  }
});
const xHsmBasic = createXActor(xHsmBasicMachine).start();

// 2) Shallow history on P1
const xHsmShallowMachine = createXMachine({
  id: 'xhsm-shallow',
  initial: 'P1',
  on: {
    TO_COMPLETE: '.Complete'
  },
  states: {
    P1: {
      id: 'xhsm-shallow-p1',
      initial: 'S1',
      states: {
        S1: { on: { TO_S2: 'S2' } },
        S2: {},
        history: { type: 'history', history: 'shallow' }
      },
      // From outside, restore to last child via history node
      on: {
        RESTORE: '#xhsm-shallow.P1.history'
      }
    },
    Complete: {}
  }
});
const xHsmShallow = createXActor(xHsmShallowMachine).start();
// Prepare: visit S2, then go to Complete so RESTORE brings us back to S2
xHsmShallow.send({ type: 'TO_S2' } as any);
xHsmShallow.send({ type: 'TO_COMPLETE' } as any);

// 3) Deep history on P1 (P1 -> A -> {G1,G2}, deep remembers nested descendant)
const xHsmDeepMachine = createXMachine({
  id: 'xhsm-deep',
  initial: 'P1',
  on: {
    TO_COMPLETE: '.Complete'
  },
  states: {
    P1: {
      id: 'xhsm-deep-p1',
      initial: 'A',
      states: {
        A: {
          initial: 'G1',
          states: {
            G1: { on: { TO_G2: 'G2' } },
            G2: {}
          }
        },
        B: {},
        history: { type: 'history', history: 'deep' }
      },
      on: {
        RESTORE: '#xhsm-deep.P1.history'
      }
    },
    Complete: {}
  }
});
const xHsmDeep = createXActor(xHsmDeepMachine).start();
// Prepare: reach deep child G2, then leave to Complete
xHsmDeep.send({ type: 'TO_G2' } as any);
xHsmDeep.send({ type: 'TO_COMPLETE' } as any);

group('XState (library)', () => {
  bench('XState Basic', () => {
    runOps(() => xBasic.send({ type: 'NEXT' } as any));
  });
  bench('XState Guards + Actions', () => {
    runOps(() => xGuard.send({ type: 'NEXT' } as any));
  });
  bench('XState Payload', () => {
    runOps(() => xPayload.send({ type: 'NEXT', value: payload.value } as any));
  });
});

group('XState HSM', () => {
  bench('HSM hierarchical transition', () => {
    runOps(() => xHsmBasic.send({ type: 'SWITCH' } as any));
  });
  bench('HSM internal transition (parent)', () => {
    runOps(() => xHsmBasic.send({ type: 'PING' } as any));
  });
  bench('HSM shallow history restore', () => {
    // Loop: RESTORE to P1.history, then back to Complete
    runOps(() => { xHsmShallow.send({ type: 'RESTORE' } as any); xHsmShallow.send({ type: 'TO_COMPLETE' } as any); });
  });
  bench('HSM deep history restore', () => {
    // Loop: RESTORE deep history, then back to Complete
    runOps(() => { xHsmDeep.send({ type: 'RESTORE' } as any); xHsmDeep.send({ type: 'TO_COMPLETE' } as any); });
  });
});

// ============================================================
// Library: Robot (robot3)
// ============================================================

// Robot - basic
const rBasicMachine = createRMachine({
  A: rState(rTransition('NEXT', 'B')),
  B: rState(rTransition('NEXT', 'C')),
  C: rState(rTransition('NEXT', 'A')),
});
const rBasic = rInterpret(rBasicMachine, () => {}, {} as any);

// Robot - guards + actions
const rGuardMachine = createRMachine({
  A: rState(rTransition('NEXT', 'B', rGuard((ctx: any) => ctx.counter < ctx.GUARD_LIMIT), rReduce((ctx: any) => ({ ...ctx, counter: ctx.counter + 1 })))),
  B: rState(rTransition('NEXT', 'C', rGuard((ctx: any) => ctx.counter < ctx.GUARD_LIMIT), rReduce((ctx: any) => ({ ...ctx, counter: ctx.counter + 1 })))),
  C: rState(rTransition('NEXT', 'A', rGuard((ctx: any) => ctx.counter < ctx.GUARD_LIMIT), rReduce((ctx: any) => ({ ...ctx, counter: ctx.counter + 1 })))),
});
const rGuardSvc = rInterpret(rGuardMachine, () => {}, { counter: 0, GUARD_LIMIT } as any);

// Robot - payload
const rPayloadMachine = createRMachine({
  A: rState(rTransition('NEXT', 'B', rReduce((ctx: any, ev: any) => ({ ...ctx, sum: ctx.sum + (ev?.value ?? 0) })))),
  B: rState(rTransition('NEXT', 'C', rReduce((ctx: any, ev: any) => ({ ...ctx, sum: ctx.sum + (ev?.value ?? 0) })))),
  C: rState(rTransition('NEXT', 'A', rReduce((ctx: any, ev: any) => ({ ...ctx, sum: ctx.sum + (ev?.value ?? 0) })))),
});
const rPayloadSvc = rInterpret(rPayloadMachine, () => {}, { sum: 0 } as any);

group('Robot (library)', () => {
  bench('Robot Basic', () => {
    runOps(() => rBasic.send('NEXT'));
  });
  bench('Robot Guards + Actions', () => {
    runOps(() => rGuardSvc.send('NEXT'));
  });
  bench('Robot Payload', () => {
    runOps(() => rPayloadSvc.send({ type: 'NEXT', value: payload.value } as any));
  });
});

// Run benchmarks
await run({
  avg: true,
  json: false,
  colors: true,
  min_max: true,
  percentiles: false,
});

