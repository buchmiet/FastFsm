// Benchmark JavaScript: XState and Robot3 vs minimal
import { bench, run, group } from 'mitata';
import { createMachine as createXMachine, createActor as createXActor, assign as xAssign } from 'xstate';
import { createMachine as createRMachine, state as rState, transition as rTransition, guard as rGuard, reduce as rReduce, interpret as rInterpret } from 'robot3';

const OPS = 1024;

// Minimal reference (pure JS)
const State = { A: 'A', B: 'B', C: 'C' };
const Trigger = { Next: 'NEXT' };

class MinimalFSM {
  constructor() { this.state = State.A; }
  transition(trigger) {
    switch (this.state) {
      case State.A: if (trigger === Trigger.Next) this.state = State.B; break;
      case State.B: if (trigger === Trigger.Next) this.state = State.C; break;
      case State.C: if (trigger === Trigger.Next) this.state = State.A; break;
    }
  }
}

const minimal = new MinimalFSM();
const payload = { value: 42, message: 'test' };
const GUARD_LIMIT = 2147483647;

function runOps(fn) { for (let i = 0; i < OPS; i++) fn(); }

console.log(`\n${'='.repeat(60)}`);
console.log(`🚀 JavaScript Library Benchmarks: XState and Robot3`);
console.log(`${'='.repeat(60)}\n`);

// ======================= XSTATE =======================
const xBasicMachine = createXMachine({
  id: 'x-basic', initial: 'A', context: {}, states: {
    A: { on: { NEXT: 'B' } },
    B: { on: { NEXT: 'C' } },
    C: { on: { NEXT: 'A' } },
  }
});
const xBasic = createXActor(xBasicMachine).start();

const xGuardMachine = createXMachine({
  id: 'x-guard', initial: 'A', context: { counter: 0, GUARD_LIMIT }, states: {
    A: { on: { NEXT: { target: 'B', guard: ({ context }) => context.counter < context.GUARD_LIMIT, actions: xAssign(({ context }) => ({ ...context, counter: context.counter + 1 })) } } },
    B: { on: { NEXT: { target: 'C', guard: ({ context }) => context.counter < context.GUARD_LIMIT, actions: xAssign(({ context }) => ({ ...context, counter: context.counter + 1 })) } } },
    C: { on: { NEXT: { target: 'A', guard: ({ context }) => context.counter < context.GUARD_LIMIT, actions: xAssign(({ context }) => ({ ...context, counter: context.counter + 1 })) } } },
  }
});
const xGuard = createXActor(xGuardMachine).start();

const xPayloadMachine = createXMachine({
  id: 'x-payload', initial: 'A', context: { sum: 0 }, states: {
    A: { on: { NEXT: { target: 'B', actions: xAssign((args) => ({ sum: args.context.sum + (args.event?.value ?? 0) })) } } },
    B: { on: { NEXT: { target: 'C', actions: xAssign((args) => ({ sum: args.context.sum + (args.event?.value ?? 0) })) } } },
    C: { on: { NEXT: { target: 'A', actions: xAssign((args) => ({ sum: args.context.sum + (args.event?.value ?? 0) })) } } },
  }
});
const xPayload = createXActor(xPayloadMachine).start();

group('XState (library)', () => {
  bench('XState Basic', () => {
    runOps(() => xBasic.send({ type: 'NEXT' }));
  });
  bench('XState Guards + Actions', () => {
    runOps(() => xGuard.send({ type: 'NEXT' }));
  });
  bench('XState Payload', () => {
    runOps(() => xPayload.send({ type: 'NEXT', value: payload.value }));
  });
});

// ======================= XSTATE HSM =======================
// 1) Hierarchical transition between parents
const xHsmBasicMachine = createXMachine({
  id: 'xhsm-basic-js',
  initial: 'P1',
  states: {
    P1: {
      id: 'xhsm-js-p1',
      initial: 'S1',
      on: {
        PING: { actions: () => {} },
        SWITCH: { target: 'P2.T1' }
      },
      states: {
        S1: { on: { NEXT: 'S2' } },
        S2: { on: { NEXT: 'S1' } },
      }
    },
    P2: {
      id: 'xhsm-js-p2',
      initial: 'T1',
      states: {
        T1: { on: { NEXT: 'T2' } },
        T2: { on: { NEXT: 'T1' } },
      }
    }
  }
});
const xHsmBasic = createXActor(xHsmBasicMachine).start();

// 2) Shallow history
const xHsmShallowMachine = createXMachine({
  id: 'xhsm-shallow-js',
  initial: 'P1',
  on: { TO_COMPLETE: '.Complete' },
  states: {
    P1: {
      id: 'xhsm-shallow-js-p1',
      initial: 'S1',
      states: {
        S1: { on: { TO_S2: 'S2' } },
        S2: {},
        history: { type: 'history', history: 'shallow' },
      },
      on: { RESTORE: '#xhsm-shallow-js.P1.history' }
    },
    Complete: {}
  }
});
const xHsmShallow = createXActor(xHsmShallowMachine).start();
xHsmShallow.send({ type: 'TO_S2' });
xHsmShallow.send({ type: 'TO_COMPLETE' });

// 3) Deep history
const xHsmDeepMachine = createXMachine({
  id: 'xhsm-deep-js',
  initial: 'P1',
  on: { TO_COMPLETE: '.Complete' },
  states: {
    P1: {
      id: 'xhsm-deep-js-p1',
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
        history: { type: 'history', history: 'deep' },
      },
      on: { RESTORE: '#xhsm-deep-js.P1.history' }
    },
    Complete: {}
  }
});
const xHsmDeep = createXActor(xHsmDeepMachine).start();
xHsmDeep.send({ type: 'TO_G2' });
xHsmDeep.send({ type: 'TO_COMPLETE' });

group('XState HSM (library)', () => {
  bench('HSM hierarchical transition', () => {
    runOps(() => xHsmBasic.send({ type: 'SWITCH' }));
  });
  bench('HSM internal transition (parent)', () => {
    runOps(() => xHsmBasic.send({ type: 'PING' }));
  });
  bench('HSM shallow history restore', () => {
    runOps(() => { xHsmShallow.send({ type: 'RESTORE' }); xHsmShallow.send({ type: 'TO_COMPLETE' }); });
  });
  bench('HSM deep history restore', () => {
    runOps(() => { xHsmDeep.send({ type: 'RESTORE' }); xHsmDeep.send({ type: 'TO_COMPLETE' }); });
  });
});

// ======================= ROBOT3 =======================
const rBasicMachine = createRMachine({
  A: rState(rTransition('NEXT', 'B')),
  B: rState(rTransition('NEXT', 'C')),
  C: rState(rTransition('NEXT', 'A')),
});
const rBasic = rInterpret(rBasicMachine, () => {}, {});

const rGuardMachine = createRMachine({
  A: rState(rTransition('NEXT', 'B', rGuard((ctx) => ctx.counter < ctx.GUARD_LIMIT), rReduce((ctx) => ({ ...ctx, counter: ctx.counter + 1 })))),
  B: rState(rTransition('NEXT', 'C', rGuard((ctx) => ctx.counter < ctx.GUARD_LIMIT), rReduce((ctx) => ({ ...ctx, counter: ctx.counter + 1 })))),
  C: rState(rTransition('NEXT', 'A', rGuard((ctx) => ctx.counter < ctx.GUARD_LIMIT), rReduce((ctx) => ({ ...ctx, counter: ctx.counter + 1 })))),
});
const rGuardSvc = rInterpret(rGuardMachine, () => {}, { counter: 0, GUARD_LIMIT });

const rPayloadMachine = createRMachine({
  A: rState(rTransition('NEXT', 'B', rReduce((ctx, ev) => ({ ...ctx, sum: ctx.sum + (ev?.value ?? 0) })))),
  B: rState(rTransition('NEXT', 'C', rReduce((ctx, ev) => ({ ...ctx, sum: ctx.sum + (ev?.value ?? 0) })))),
  C: rState(rTransition('NEXT', 'A', rReduce((ctx, ev) => ({ ...ctx, sum: ctx.sum + (ev?.value ?? 0) })))),
});
const rPayloadSvc = rInterpret(rPayloadMachine, () => {}, { sum: 0 });

group('Robot (library)', () => {
  bench('Robot Basic', () => {
    runOps(() => rBasic.send('NEXT'));
  });
  bench('Robot Guards + Actions', () => {
    runOps(() => rGuardSvc.send('NEXT'));
  });
  bench('Robot Payload', () => {
    runOps(() => rPayloadSvc.send({ type: 'NEXT', value: payload.value }));
  });
});

await run({ avg: true, json: false, colors: true, min_max: true, percentiles: false });
