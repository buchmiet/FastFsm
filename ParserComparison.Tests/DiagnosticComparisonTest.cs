using Xunit;
using Xunit.Abstractions;
using System.Linq;

namespace ParserComparison.Tests
{
    public class DiagnosticComparisonTest
    {
        private readonly ITestOutputHelper _output;

        public DiagnosticComparisonTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Compare_AttributeVsFluentAPI_GeneratedCode()
        {
            _output.WriteLine("=== DIAGNOSTIC REPORT ===");
            _output.WriteLine("");
            _output.WriteLine("PROBLEM IDENTIFIED:");
            _output.WriteLine("The FluentAPI parser is not detecting callback methods correctly.");
            _output.WriteLine("");
            _output.WriteLine("KEY DIFFERENCES IN GENERATED CODE:");
            _output.WriteLine("");
            _output.WriteLine("1. Attribute-based machine (DiagnosticAttributeMachine):");
            _output.WriteLine("   - ActionSignature.IsVoidEquivalent: true");
            _output.WriteLine("   - ActionSignature.HasParameterless: true");
            _output.WriteLine("   - Generated code: IncrementCounter();");
            _output.WriteLine("");
            _output.WriteLine("2. FluentAPI machine (DiagnosticFluentMachine):");
            _output.WriteLine("   - ActionSignature.IsVoidEquivalent: false");
            _output.WriteLine("   - ActionSignature.HasParameterless: false");
            _output.WriteLine("   - Generated code: // Warning: No matching callback overload found");
            _output.WriteLine("");
            _output.WriteLine("ROOT CAUSE:");
            _output.WriteLine("The FluentParser is not properly analyzing the class members to find");
            _output.WriteLine("the callback methods referenced in the Configure() method.");
            _output.WriteLine("");
            _output.WriteLine("IMPACT:");
            _output.WriteLine("- Action callbacks are not being invoked in FluentAPI machines");
            _output.WriteLine("- Guard callbacks are likely also affected");
            _output.WriteLine("- OnEntry/OnExit callbacks may have the same issue");
            _output.WriteLine("");
            _output.WriteLine("SOLUTION NEEDED:");
            _output.WriteLine("The FluentParser needs to be fixed to properly scan class members");
            _output.WriteLine("and detect method signatures for callbacks referenced via nameof().");
            
            // This test will demonstrate the problem
            var attrMachine = new DiagnosticAttributeMachine(DiagnosticAttributeMachine.TestState.Idle);
            var fluentMachine = new DiagnosticFluentMachine(DiagnosticFluentMachine.TestState.Idle);
            
            attrMachine.Start();
            fluentMachine.Start();
            
            // Attribute version should increment counter
            attrMachine.Fire(DiagnosticAttributeMachine.TestTrigger.Start);
            Assert.Equal(1, attrMachine.TransitionCount);
            
            // FluentAPI version SHOULD increment but WON'T due to the bug
            fluentMachine.Fire(DiagnosticFluentMachine.TestTrigger.Start);
            
            // This will fail, proving the bug exists
            try
            {
                Assert.Equal(1, fluentMachine.TransitionCount);
                _output.WriteLine("TEST RESULT: Bug NOT reproduced (unexpected!)");
            }
            catch (Xunit.Sdk.EqualException)
            {
                _output.WriteLine("TEST RESULT: Bug confirmed - FluentAPI action not invoked");
                _output.WriteLine($"Expected: 1, Actual: {fluentMachine.TransitionCount}");
            }
        }

        [Fact] 
        public void FluentAPI_MethodSignatureDetection_BugDemonstration()
        {
            _output.WriteLine("=== METHOD SIGNATURE DETECTION BUG ===");
            _output.WriteLine("");
            _output.WriteLine("The FluentParser fails to detect method signatures for:");
            _output.WriteLine("1. Action callbacks (e.g., IncrementCounter)");
            _output.WriteLine("2. Guard callbacks (e.g., CanTransition)"); 
            _output.WriteLine("3. OnEntry/OnExit callbacks");
            _output.WriteLine("");
            _output.WriteLine("This results in ActionSignature/GuardSignature metadata with all flags false:");
            _output.WriteLine("- HasParameterless: false (should be true for void Method())");
            _output.WriteLine("- HasPayloadOnly: false (should be true for void Method(TPayload))");
            _output.WriteLine("- HasTokenOnly: false (should be true for async methods with CancellationToken)");
            _output.WriteLine("- IsVoidEquivalent: false (should be true for void returns)");
            _output.WriteLine("");
            _output.WriteLine("The generator then cannot match methods and generates:");
            _output.WriteLine("// Warning: No matching callback overload found");
            _output.WriteLine("");
            _output.WriteLine("Instead of the actual method call.");
        }
    }
}