package golang_bench

import (
    "context"
    "testing"
    "github.com/looplab/fsm"
)

const OPS = 1024

// Basic FSM: A -> B -> C -> A on "next"
func newBasicFSM() *fsm.FSM {
    return fsm.NewFSM(
        "A",
        fsm.Events{
            {Name: "next", Src: []string{"A"}, Dst: "B"},
            {Name: "next", Src: []string{"B"}, Dst: "C"},
            {Name: "next", Src: []string{"C"}, Dst: "A"},
        },
        fsm.Callbacks{},
    )
}

func Benchmark_Looplab_Basic(b *testing.B) {
    ctx := context.Background()
    sm := newBasicFSM()
    b.ResetTimer()
    for i := 0; i < b.N; i++ {
        for k := 0; k < OPS; k++ {
            _ = sm.Event(ctx, "next")
        }
    }
}

// Guards + Actions: increment a counter on exit of each state
type guardMachine struct{
    counter int
    f *fsm.FSM
}

func newGuardFSM() *guardMachine {
    gm := &guardMachine{}
    gm.f = fsm.NewFSM(
        "A",
        fsm.Events{
            {Name: "next", Src: []string{"A"}, Dst: "B"},
            {Name: "next", Src: []string{"B"}, Dst: "C"},
            {Name: "next", Src: []string{"C"}, Dst: "A"},
        },
        fsm.Callbacks{
            "leave_A": func(_ context.Context, _ *fsm.Event){ gm.counter++ },
            "leave_B": func(_ context.Context, _ *fsm.Event){ gm.counter++ },
            "leave_C": func(_ context.Context, _ *fsm.Event){ gm.counter++ },
        },
    )
    return gm
}

func Benchmark_Looplab_GuardsActions(b *testing.B) {
    ctx := context.Background()
    gm := newGuardFSM()
    b.ResetTimer()
    for i := 0; i < b.N; i++ {
        for k := 0; k < OPS; k++ {
            _ = gm.f.Event(ctx, "next")
        }
    }
}

// Payload: pass a value via Event args and accumulate in callback
type payloadMachine struct{
    sum int
    f *fsm.FSM
}

func newPayloadFSM() *payloadMachine {
    pm := &payloadMachine{}
    pm.f = fsm.NewFSM(
        "A",
        fsm.Events{
            {Name: "next", Src: []string{"A"}, Dst: "B"},
            {Name: "next", Src: []string{"B"}, Dst: "C"},
            {Name: "next", Src: []string{"C"}, Dst: "A"},
        },
        fsm.Callbacks{
            // after event named 'next'
            "after_next": func(_ context.Context, e *fsm.Event){
                if len(e.Args) > 0 {
                    if v, ok := e.Args[0].(int); ok { pm.sum += v }
                }
            },
        },
    )
    return pm
}

func Benchmark_Looplab_Payload(b *testing.B) {
    ctx := context.Background()
    pm := newPayloadFSM()
    val := 42
    b.ResetTimer()
    for i := 0; i < b.N; i++ {
        for k := 0; k < OPS; k++ {
            _ = pm.f.Event(ctx, "next", val)
        }
    }
}

// Can() check (no transition)
func Benchmark_Looplab_Can(b *testing.B) {
    sm := newBasicFSM()
    b.ResetTimer()
    for i := 0; i < b.N; i++ {
        for k := 0; k < OPS; k++ {
            _ = sm.Can("next")
        }
    }
}

// AvailableTransitions() retrieval (no transition)
func Benchmark_Looplab_AvailableTransitions(b *testing.B) {
    sm := newBasicFSM()
    b.ResetTimer()
    for i := 0; i < b.N; i++ {
        for k := 0; k < OPS; k++ {
            _ = sm.AvailableTransitions()
        }
    }
}

