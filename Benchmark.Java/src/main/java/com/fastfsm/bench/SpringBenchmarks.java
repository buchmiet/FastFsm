package com.fastfsm.bench;

import org.openjdk.jmh.annotations.*;
import org.springframework.messaging.support.MessageBuilder;
import org.springframework.statemachine.StateMachine;
import org.springframework.statemachine.config.StateMachineBuilder;

import java.util.EnumSet;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;

@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.NANOSECONDS)
@Measurement(iterations = 15, time = 200, timeUnit = TimeUnit.MILLISECONDS)
@Warmup(iterations = 3, time = 200, timeUnit = TimeUnit.MILLISECONDS)
@Fork(value = 1)
@State(Scope.Benchmark)
public class SpringBenchmarks {

    private static final int OPS = 1024;

    enum S { A, B, C }
    enum E { NEXT }

    static final class Payload {
        int value = 42;
        String msg = "test";
    }

    private StateMachine<S, E> basic;
    private StateMachine<S, E> guarded;
    private StateMachine<S, E> payloadFsm;
    private StateMachine<S, E> asyncYield;
    private StateMachine<S, E> asyncHot;

    int counter;
    int sum;
    int asyncCtr;
    int asyncCtrHot;

    @Setup(Level.Trial)
    public void setup() throws Exception {
        // ---- BASIC ----------------------------------------------------
        StateMachineBuilder.Builder<S, E> basicBuilder = StateMachineBuilder.builder();
        basicBuilder.configureStates()
            .withStates()
                .initial(S.A)
                .states(EnumSet.allOf(S.class));
        basicBuilder.configureTransitions()
            .withExternal().source(S.A).target(S.B).event(E.NEXT)
            .and().withExternal().source(S.B).target(S.C).event(E.NEXT)
            .and().withExternal().source(S.C).target(S.A).event(E.NEXT);
        this.basic = basicBuilder.build();
        this.basic.start();

        // ---- GUARDS + ACTIONS ----------------------------------------
        StateMachineBuilder.Builder<S, E> guardedBuilder = StateMachineBuilder.builder();
        guardedBuilder.configureStates()
            .withStates()
                .initial(S.A)
                .states(EnumSet.allOf(S.class));
        guardedBuilder.configureTransitions()
            .withExternal().source(S.A).target(S.B).event(E.NEXT).guard(ctx -> counter < Integer.MAX_VALUE).action(ctx -> counter++)
            .and().withExternal().source(S.B).target(S.C).event(E.NEXT).guard(ctx -> counter < Integer.MAX_VALUE).action(ctx -> counter++)
            .and().withExternal().source(S.C).target(S.A).event(E.NEXT).guard(ctx -> counter < Integer.MAX_VALUE).action(ctx -> counter++);
        this.guarded = guardedBuilder.build();
        this.guarded.start();

        // ---- PAYLOAD --------------------------------------------------
        StateMachineBuilder.Builder<S, E> payloadBuilder = StateMachineBuilder.builder();
        payloadBuilder.configureStates()
            .withStates()
                .initial(S.A)
                .states(EnumSet.allOf(S.class));
        payloadBuilder.configureTransitions()
            .withExternal().source(S.A).target(S.B).event(E.NEXT).action(ctx -> sum += ctx.getMessageHeaders().get("payload", Payload.class).value)
            .and().withExternal().source(S.B).target(S.C).event(E.NEXT).action(ctx -> sum += ctx.getMessageHeaders().get("payload", Payload.class).value)
            .and().withExternal().source(S.C).target(S.A).event(E.NEXT).action(ctx -> sum += ctx.getMessageHeaders().get("payload", Payload.class).value);
        this.payloadFsm = payloadBuilder.build();
        this.payloadFsm.start();

        // ---- ASYNC with yield (real scheduler hop) -------------------
        StateMachineBuilder.Builder<S, E> asyncYieldBuilder = StateMachineBuilder.builder();
        asyncYieldBuilder.configureStates()
                .withStates()
                .initial(S.A)
                .states(EnumSet.allOf(S.class));
        asyncYieldBuilder.configureTransitions()
                .withExternal().source(S.A).target(S.B).event(E.NEXT).action(ctx -> CompletableFuture.runAsync(() -> asyncCtr++).join())
                .and().withExternal().source(S.B).target(S.C).event(E.NEXT).action(ctx -> CompletableFuture.runAsync(() -> asyncCtr++).join())
                .and().withExternal().source(S.C).target(S.A).event(E.NEXT).action(ctx -> CompletableFuture.runAsync(() -> asyncCtr++).join());
        this.asyncYield = asyncYieldBuilder.build();
        this.asyncYield.start();

        // ---- ASYNC hot path (no yield) --------------------------------
        StateMachineBuilder.Builder<S, E> asyncHotBuilder = StateMachineBuilder.builder();
        asyncHotBuilder.configureStates()
                .withStates()
                .initial(S.A)
                .states(EnumSet.allOf(S.class));
        asyncHotBuilder.configureTransitions()
                .withExternal().source(S.A).target(S.B).event(E.NEXT).action(ctx -> asyncCtrHot++)
                .and().withExternal().source(S.B).target(S.C).event(E.NEXT).action(ctx -> asyncCtrHot++)
                .and().withExternal().source(S.C).target(S.A).event(E.NEXT).action(ctx -> asyncCtrHot++);
        this.asyncHot = asyncHotBuilder.build();
        this.asyncHot.start();
    }
    
    @Benchmark @OperationsPerInvocation(OPS)
    public void spring_basic() {
        for (int i = 0; i < OPS; i++) basic.sendEvent(E.NEXT);
    }

    @Benchmark @OperationsPerInvocation(OPS)
    public void spring_guard_actions() {
        for (int i = 0; i < OPS; i++) guarded.sendEvent(E.NEXT);
    }

    @Benchmark @OperationsPerInvocation(OPS)
    public void spring_payload() {
        var p = new Payload();
        for (int i = 0; i < OPS; i++) {
            payloadFsm.sendEvent(MessageBuilder.withPayload(E.NEXT).setHeader("payload", p).build());
        }
    }

    @Benchmark @OperationsPerInvocation(OPS)
    public void spring_async_yield() {
        for (int i = 0; i < OPS; i++) asyncYield.sendEvent(E.NEXT);
    }

    @Benchmark @OperationsPerInvocation(OPS)
    public void spring_async_hot() {
        for (int i = 0; i < OPS; i++) asyncHot.sendEvent(E.NEXT);
    }
}