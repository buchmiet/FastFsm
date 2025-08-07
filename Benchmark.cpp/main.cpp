#include <benchmark/benchmark.h>
#include <boost/sml.hpp>
#include <thread>

namespace sml = boost::sml;
constexpr int OPS = 1024;

// -----------------------------------------------------------------
//  BASIC (bez guardów i akcji) - baseline
// -----------------------------------------------------------------
namespace basic {
    struct Next {};
    struct A {}; 
    struct B {}; 
    struct C {};

    struct sm {
        auto operator()() const noexcept {
            using namespace sml;
            return make_transition_table(
                *state<A> + event<Next> = state<B>,
                 state<B> + event<Next> = state<C>,
                 state<C> + event<Next> = state<A>
            );
        }
    };
}

class BasicFixture : public benchmark::Fixture {
public:
    sml::sm<basic::sm> fsm;
    basic::Next evt;
};

BENCHMARK_F(BasicFixture, sml_basic)(benchmark::State& st) {
    for (auto _ : st) {
        for (int i = 0; i < OPS; ++i) {
            benchmark::DoNotOptimize(fsm.process_event(evt));
        }
    }
    st.SetItemsProcessed(st.iterations() * OPS);
}

// -----------------------------------------------------------------
//  GUARDS + ACTIONS (razem)
// -----------------------------------------------------------------
namespace guards_actions {
    struct Next {};
    struct A {}; 
    struct B {}; 
    struct C {};
    
    struct Context {
        int counter = 0;
        static constexpr int GUARD_LIMIT = INT_MAX;
    };
    
    struct sm {
        auto operator()() const noexcept {
            using namespace sml;
            
            auto guard = [](Context& ctx) { 
                return ctx.counter < Context::GUARD_LIMIT; 
            };
            auto action = [](Context& ctx) { 
                ++ctx.counter; 
            };
            
            return make_transition_table(
                *state<A> + event<Next> [guard] / action = state<B>,
                 state<B> + event<Next> [guard] / action = state<C>,
                 state<C> + event<Next> [guard] / action = state<A>
            );
        }
    };
}

class GuardsActionsFixture : public benchmark::Fixture {
public:
    guards_actions::Context ctx;
    sml::sm<guards_actions::sm> fsm{ctx};
    guards_actions::Next evt;
};

BENCHMARK_F(GuardsActionsFixture, sml_guards_actions)(benchmark::State& st) {
    for (auto _ : st) {
        for (int i = 0; i < OPS; ++i) {
            benchmark::DoNotOptimize(fsm.process_event(evt));
        }
    }
    st.SetItemsProcessed(st.iterations() * OPS);
}

// -----------------------------------------------------------------
//  PAYLOAD (dane przekazywane podczas przejść)
// -----------------------------------------------------------------
namespace payload {
    struct PayloadData {
        int value;
        const char* message;
    };
    
    struct NextWithPayload {
        const PayloadData* data;  // Używamy wskaźnika jak w C#/Rust (reference)
    };
    
    struct A {}; 
    struct B {}; 
    struct C {};
    
    struct Context {
        int sum = 0;
    };
    
    struct sm {
        auto operator()() const noexcept {
            using namespace sml;
            
            auto process_payload = [](Context& ctx, const NextWithPayload& evt) { 
                ctx.sum += evt.data->value;
            };
            
            return make_transition_table(
                *state<A> + event<NextWithPayload> / process_payload = state<B>,
                 state<B> + event<NextWithPayload> / process_payload = state<C>,
                 state<C> + event<NextWithPayload> / process_payload = state<A>
            );
        }
    };
}

class PayloadFixture : public benchmark::Fixture {
public:
    payload::Context ctx;
    sml::sm<payload::sm> fsm{ctx};
    payload::PayloadData data{42, "test"};
    payload::NextWithPayload evt{&data};
};

BENCHMARK_F(PayloadFixture, sml_payload)(benchmark::State& st) {
    for (auto _ : st) {
        for (int i = 0; i < OPS; ++i) {
            benchmark::DoNotOptimize(fsm.process_event(evt));
        }
    }
    st.SetItemsProcessed(st.iterations() * OPS);
}

// -----------------------------------------------------------------
//  ASYNC HOT PATH (bez yield - porównywalny z C#/Rust hot path)
// -----------------------------------------------------------------
namespace async_hot {
    struct Next {};
    struct A {}; 
    struct B {}; 
    struct C {};
    
    struct Context {
        int async_counter = 0;
        
        // Symulacja async action bez yield - jak ValueTask.CompletedTask
        void async_action() {
            ++async_counter;
        }
    };
    
    struct sm {
        auto operator()() const noexcept {
            using namespace sml;
            
            auto action = [](Context& ctx) { 
                ctx.async_action();
            };
            
            return make_transition_table(
                *state<A> + event<Next> / action = state<B>,
                 state<B> + event<Next> / action = state<C>,
                 state<C> + event<Next> / action = state<A>
            );
        }
    };
}

class AsyncHotFixture : public benchmark::Fixture {
public:
    async_hot::Context ctx;
    sml::sm<async_hot::sm> fsm{ctx};
    async_hot::Next evt;
};

BENCHMARK_F(AsyncHotFixture, sml_async_hot)(benchmark::State& st) {
    for (auto _ : st) {
        for (int i = 0; i < OPS; ++i) {
            benchmark::DoNotOptimize(fsm.process_event(evt));
        }
    }
    st.SetItemsProcessed(st.iterations() * OPS);
}

// -----------------------------------------------------------------
//  ASYNC WITH YIELD - OPCJONALNY
//  std::this_thread::yield() nie jest porównywalny z Task.Yield()
//  Zostawiam zakomentowany - można odkomentować dla kompletności
// -----------------------------------------------------------------
/*
namespace async_yield {
    struct Next {};
    struct A {}; 
    struct B {}; 
    struct C {};
    
    struct Context {
        int async_counter = 0;
        
        void async_action_yield() {
            std::this_thread::yield();  // UWAGA: to jest cięższe niż Task.Yield()!
            ++async_counter;
        }
    };
    
    struct sm {
        auto operator()() const noexcept {
            using namespace sml;
            
            auto action = [](Context& ctx) { 
                ctx.async_action_yield();
            };
            
            return make_transition_table(
                *state<A> + event<Next> / action = state<B>,
                 state<B> + event<Next> / action = state<C>,
                 state<C> + event<Next> / action = state<A>
            );
        }
    };
}

class AsyncYieldFixture : public benchmark::Fixture {
public:
    async_yield::Context ctx;
    sml::sm<async_yield::sm> fsm{ctx};
    async_yield::Next evt;
};

BENCHMARK_F(AsyncYieldFixture, sml_async_yield)(benchmark::State& st) {
    for (auto _ : st) {
        for (int i = 0; i < OPS; ++i) {
            benchmark::DoNotOptimize(fsm.process_event(evt));
        }
    }
    st.SetItemsProcessed(st.iterations() * OPS);
}
*/

// -----------------------------------------------------------------
//  HELPER BENCHMARKS - CanFire i GetPermittedTriggers
// -----------------------------------------------------------------
namespace helpers {
    using namespace basic;  // Używamy basic FSM dla helperów
    
    class CanFireFixture : public benchmark::Fixture {
    public:
        sml::sm<sm> fsm;
        Next evt;
    };
    
    BENCHMARK_F(CanFireFixture, sml_can_fire)(benchmark::State& st) {
        for (auto _ : st) {
            for (int i = 0; i < OPS; ++i) {
                // Boost.SML nie ma bezpośredniego CanFire, ale można sprawdzić przez is()
                benchmark::DoNotOptimize(fsm.is(sml::state<A>));
            }
        }
        st.SetItemsProcessed(st.iterations() * OPS);
    }
    
    // GetPermittedTriggers - Boost.SML nie ma takiej funkcji built-in
    // Można by ją zaimplementować, ale to wykracza poza standardowe API
}

// Punkt wejścia Google Benchmark
BENCHMARK_MAIN();