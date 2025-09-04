using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests
{
    // This class intentionally has Configure() but no fluent DSL calls
    // to trigger enum-only fallback in FluentParser.
    [StateMachine(typeof(FallbackState), typeof(FallbackTrigger))]
    public partial class FluentFallbackMachine
    {
        public enum FallbackState { S1, S2, S3 }
        public enum FallbackTrigger { Go, Back }

        private static void Configure() => _ = typeof(FSM); // no .State/.On calls on purpose
    }
}
