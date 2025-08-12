package com.fastfsm.bench;

import org.openjdk.jmh.annotations.*;
import org.squirrelframework.foundation.fsm.UntypedStateMachineBuilder;
import org.squirrelframework.foundation.fsm.StateMachineBuilderFactory;
import org.squirrelframework.foundation.fsm.UntypedStateMachine;
import org.squirrelframework.foundation.fsm.impl.AbstractUntypedStateMachine;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;

@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.NANOSECONDS)
@Measurement(iterations = 15, time = 200, timeUnit = TimeUnit.MILLISECONDS)
@Warmup(iterations = 3, time = 200, timeUnit = TimeUnit.MILLISECONDS)
@Fork(value = 1)
@State(Scope.Benchmark)
public class SquirrelBenchmarks {

    private static final int OPS = 1024;

    enum S { A, B, C }
    enum E { NEXT }

    static final class Payload {
        int value = 42;
        String msg = "test";
    }

    static class BasicFsm extends AbstractUntypedStateMachine {
    }

    static class GuardedFsm extends AbstractUntypedStateMachine {
        static void incrementCounter(S from, S to, E event, Object context) {
            counter++;
        }
    }

    static class PayloadFsm extends AbstractUntypedStateMachine {
        static void addValue(S from, S to, E event, Payload context) {
            sum += context.value;
        }
    }

    static class AsyncYieldFsm extends AbstractUntypedStateMachine {
        static void asyncIncrement(S from, S to, E event, Object context) {
            CompletableFuture.runAsync(() -> asyncCtr++).join();
        }
    }

    static class AsyncHotFsm extends AbstractUntypedStateMachine {
        static void hotIncrement(S from, S to, E event, Object context) {
            asyncCtrHot++;
        }
    }

    private UntypedStateMachine basic;
    private UntypedStateMachine guarded;
    private UntypedStateMachine payloadFsm;
    private UntypedStateMachine asyncYield;
    private UntypedStateMachine asyncHot;

    static int counter;
    static int sum;
    static int asyncCtr;
    static int asyncCtrHot;


    @Setup(Level.Trial)
    public void setup() throws Exception {
        // ---- BASIC ----------------------------------------------------
        UntypedStateMachineBuilder basicBuilder = 
            StateMachineBuilderFactory.create(BasicFsm.class);
        basicBuilder.externalTransition().from(S.A).to(S.B).on(E.NEXT);
        basicBuilder.externalTransition().from(S.B).to(S.C).on(E.NEXT);
        basicBuilder.externalTransition().from(S.C).to(S.A).on(E.NEXT);
        this.basic = basicBuilder.newStateMachine(S.A);
        this.basic.start();

        // ---- GUARDS + ACTIONS ----------------------------------------
        UntypedStateMachineBuilder guardedBuilder = 
            StateMachineBuilderFactory.create(GuardedFsm.class);
        guardedBuilder.externalTransition().from(S.A).to(S.B).on(E.NEXT)
            .callMethod("incrementCounter");
        guardedBuilder.externalTransition().from(S.B).to(S.C).on(E.NEXT)
            .callMethod("incrementCounter");
        guardedBuilder.externalTransition().from(S.C).to(S.A).on(E.NEXT)
            .callMethod("incrementCounter");
        this.guarded = guardedBuilder.newStateMachine(S.A);
        this.guarded.start();

        // ---- PAYLOAD --------------------------------------------------
        UntypedStateMachineBuilder payloadBuilder = 
            StateMachineBuilderFactory.create(PayloadFsm.class);
        payloadBuilder.externalTransition().from(S.A).to(S.B).on(E.NEXT)
            .callMethod("addValue");
        payloadBuilder.externalTransition().from(S.B).to(S.C).on(E.NEXT)
            .callMethod("addValue");
        payloadBuilder.externalTransition().from(S.C).to(S.A).on(E.NEXT)
            .callMethod("addValue");
        this.payloadFsm = payloadBuilder.newStateMachine(S.A);
        this.payloadFsm.start();

        // ---- ASYNC with yield (real scheduler hop) -------------------
        UntypedStateMachineBuilder asyncYieldBuilder = 
            StateMachineBuilderFactory.create(AsyncYieldFsm.class);
        asyncYieldBuilder.externalTransition().from(S.A).to(S.B).on(E.NEXT)
            .callMethod("asyncIncrement");
        asyncYieldBuilder.externalTransition().from(S.B).to(S.C).on(E.NEXT)
            .callMethod("asyncIncrement");
        asyncYieldBuilder.externalTransition().from(S.C).to(S.A).on(E.NEXT)
            .callMethod("asyncIncrement");
        this.asyncYield = asyncYieldBuilder.newStateMachine(S.A);
        this.asyncYield.start();

        // ---- ASYNC hot path (no yield) --------------------------------
        UntypedStateMachineBuilder asyncHotBuilder = 
            StateMachineBuilderFactory.create(AsyncHotFsm.class);
        asyncHotBuilder.externalTransition().from(S.A).to(S.B).on(E.NEXT)
            .callMethod("hotIncrement");
        asyncHotBuilder.externalTransition().from(S.B).to(S.C).on(E.NEXT)
            .callMethod("hotIncrement");
        asyncHotBuilder.externalTransition().from(S.C).to(S.A).on(E.NEXT)
            .callMethod("hotIncrement");
        this.asyncHot = asyncHotBuilder.newStateMachine(S.A);
        this.asyncHot.start();
    }
    
    @Benchmark @OperationsPerInvocation(OPS)
    public void squirrel_basic() {
        for (int i = 0; i < OPS; i++) basic.fire(E.NEXT);
    }

    @Benchmark @OperationsPerInvocation(OPS)
    public void squirrel_guard_actions() {
        for (int i = 0; i < OPS; i++) guarded.fire(E.NEXT);
    }

    @Benchmark @OperationsPerInvocation(OPS)
    public void squirrel_payload() {
        Payload p = new Payload();
        for (int i = 0; i < OPS; i++) {
            payloadFsm.fire(E.NEXT, p);
        }
    }

    @Benchmark @OperationsPerInvocation(OPS)
    public void squirrel_async_yield() {
        for (int i = 0; i < OPS; i++) asyncYield.fire(E.NEXT);
    }

    @Benchmark @OperationsPerInvocation(OPS)
    public void squirrel_async_hot() {
        for (int i = 0; i < OPS; i++) asyncHot.fire(E.NEXT);
    }
}