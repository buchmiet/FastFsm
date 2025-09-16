using System;
using Abstractions.Fluent;

namespace FastFsm.Logging.Tests;

// Test-only Fluent DSL compatibility shims
// Provides .If(...) and .And() used in logging test definitions.
static class FluentCompatExtensions
{
    // Sugar for guards used in older samples: treat as no-op for parser
    public static TransitionBuilder<TState, TTrigger> If<TState, TTrigger>(
        this TransitionBuilder<TState, TTrigger> builder,
        string methodName)
        where TState : Enum
        where TTrigger : Enum
    {
        // Intentionally no-op; parser recognizes Guard/GuardAsync only.
        // Tests default guards to true, so the transition remains permitted.
        return builder;
    }

    // Chain helper after GoTo()/OnEntry()/etc.
    public static StateBuilder<TState> And<TState>(
        this StateBuilder<TState> builder)
        where TState : Enum
        => builder;
}

