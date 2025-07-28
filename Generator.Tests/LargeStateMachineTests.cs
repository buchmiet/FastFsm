using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public class LargeStateMachineTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void LargeStateMachine_With50States_GeneratesCorrectly()
    {
        // Arrange - Generate source code for large state machine
        var sourceCode = GenerateLargeStateMachineSource(50, 200);

        // Act
        var sw = Stopwatch.StartNew();
        var (asm, diagnostics, generatedSources) = CompileAndRunGenerator(
            [sourceCode],
            new StateMachineGenerator());
        sw.Stop();

        // Assert
        Assert.NotNull(asm);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        output.WriteLine($"Generation time for 50 states, 200 transitions: {sw.ElapsedMilliseconds}ms");

        // Verify generated code exists
        Assert.NotEmpty(generatedSources);
        var generatedCode = generatedSources.Values.FirstOrDefault(s => s.Contains("LargeMachine"));
        Assert.NotNull(generatedCode);

        // Verify it contains expected methods
        Assert.Contains("TryFire", generatedCode);
        Assert.Contains("CanFire", generatedCode);
        Assert.Contains("GetPermittedTriggers", generatedCode);

        // Check for performance - generation should be fast
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Generation took too long: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void LargeStateMachine_RuntimePerformance_IsAcceptable()
    {
        // This would require actually instantiating and testing the generated machine
        // For now, we'll verify the generated code structure is optimized

        var sourceCode = GenerateLargeStateMachineSource(20, 50);
        var (asm, diagnostics, generatedSources) = CompileAndRunGenerator(
            [sourceCode],
            new StateMachineGenerator());

        var generatedCode = generatedSources.Values.FirstOrDefault(s => s.Contains("LargeMachine"));
        Assert.NotNull(generatedCode);

        // Verify switch statement structure (should be nested switches)
        var switchCount = generatedCode!.Split(new[] { "switch" }, StringSplitOptions.None).Length - 1;
        Assert.True(switchCount >= 2, "Should use nested switch statements for performance");

        // Verify aggressive inlining
        Assert.Contains("AggressiveInlining", generatedCode);
    }

    [Fact]
    public void VeryLargeEnum_HandledCorrectly()
    {
        // Test with enum having many values
        var sourceCode = @"
                using Abstractions.Attributes;
                namespace Test {
                    public enum BigState { 
                        S0, S1, S2, S3, S4, S5, S6, S7, S8, S9,
                        S10, S11, S12, S13, S14, S15, S16, S17, S18, S19,
                        S20, S21, S22, S23, S24, S25, S26, S27, S28, S29,
                        S30, S31, S32, S33, S34, S35, S36, S37, S38, S39,
                        S40, S41, S42, S43, S44, S45, S46, S47, S48, S49,
                        S50, S51, S52, S53, S54, S55, S56, S57, S58, S59,
                        S60, S61, S62, S63, S64, S65, S66, S67, S68, S69,
                        S70, S71, S72, S73, S74, S75, S76, S77, S78, S79,
                        S80, S81, S82, S83, S84, S85, S86, S87, S88, S89,
                        S90, S91, S92, S93, S94, S95, S96, S97, S98, S99
                    }
                    
                    public enum BigTrigger { T1, T2, T3, T4, T5 }
                    
                    [StateMachine(typeof(BigState), typeof(BigTrigger))]
                    public partial class BigMachine {
                        [Transition(BigState.S0, BigTrigger.T1, BigState.S1)]
                        [Transition(BigState.S1, BigTrigger.T1, BigState.S2)]
                        [Transition(BigState.S99, BigTrigger.T5, BigState.S0)]
                        private void Configure() { }
                    }
                }";

        var (asm, diagnostics, generatedSources) = CompileAndRunGenerator(
            [sourceCode],
            new StateMachineGenerator());

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotEmpty(generatedSources);
        Assert.NotNull(asm);

        // Verify the machine works
        Type? machineType = asm!.GetType("Test.BigMachine");
        Assert.NotNull(machineType);

        Type? stateType = asm.GetType("Test.BigState");
        Type? triggerType = asm.GetType("Test.BigTrigger");

        object stateS0 = Enum.Parse(stateType!, "S0");
        object machine = Activator.CreateInstance(machineType!, stateS0)!;

        // Verify basic functionality
        var tryFireMethod = machineType!.GetMethod("TryFire", [triggerType, typeof(object)])!;
        var currentStateProp = machineType.GetProperty("CurrentState")!;

        object triggerT1 = Enum.Parse(triggerType!, "T1");
        bool result = (bool)tryFireMethod.Invoke(machine, [triggerT1, null])!;

        Assert.True(result);
        Assert.Equal("S1", currentStateProp.GetValue(machine)!.ToString());
    }

    [Fact]
    public void PerformanceTest_ManyTransitionsFromSingleState()
    {
        // Test performance with many transitions from a single state
        var sb = new StringBuilder();
        sb.AppendLine("using Abstractions.Attributes;");
        sb.AppendLine("namespace Test {");
        sb.AppendLine("    public enum HubState { Hub, Target1, Target2, Target3, Target4, Target5 }");
        sb.AppendLine("    public enum HubTrigger {");
        for (int i = 0; i < 50; i++)
        {
            sb.AppendLine($"        T{i},");
        }
        sb.AppendLine("    }");
        sb.AppendLine("    [StateMachine(typeof(HubState), typeof(HubTrigger))]");
        sb.AppendLine("    public partial class HubMachine {");

        // Generate many transitions from Hub state
        for (int i = 0; i < 50; i++)
        {
            var targetState = $"Target{(i % 5) + 1}";
            sb.AppendLine($"        [Transition(HubState.Hub, HubTrigger.T{i}, HubState.{targetState})]");
        }

        sb.AppendLine("        private void Configure() { }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var sourceCode = sb.ToString();

        var sw = Stopwatch.StartNew();
        var (asm, diagnostics, generatedSources) = CompileAndRunGenerator(
            [sourceCode],
            new StateMachineGenerator());
        sw.Stop();

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        output.WriteLine($"Generation time for hub pattern (50 transitions from single state): {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 3000, $"Generation took too long: {sw.ElapsedMilliseconds}ms");

        // Verify the generated code handles this efficiently
        var generatedCode = generatedSources.Values.FirstOrDefault(s => s.Contains("HubMachine"));
        Assert.NotNull(generatedCode);

        // Should use switch for triggers when many transitions from same state
        Assert.Contains("switch", generatedCode);
    }

    private string GenerateLargeStateMachineSource(int stateCount, int transitionCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Abstractions.Attributes;");
        sb.AppendLine("namespace Test {");

        // Generate state enum
        sb.AppendLine("    public enum LargeState {");
        for (int i = 0; i < stateCount; i++)
        {
            sb.AppendLine($"        State{i}{(i < stateCount - 1 ? "," : "")}");
        }
        sb.AppendLine("    }");

        // Generate trigger enum
        sb.AppendLine("    public enum LargeTrigger {");
        for (int i = 0; i < 10; i++) // 10 triggers should be enough
        {
            sb.AppendLine($"        Trigger{i}{(i < 9 ? "," : "")}");
        }
        sb.AppendLine("    }");

        // Generate state machine class
        sb.AppendLine("    [StateMachine(typeof(LargeState), typeof(LargeTrigger))]");
        sb.AppendLine("    public partial class LargeMachine {");

        // Generate transitions
        var random = new Random(42); // Deterministic for testing
        var generatedTransitions = new HashSet<string>();

        for (int i = 0; i < transitionCount; i++)
        {
            var fromState = random.Next(stateCount);
            var toState = random.Next(stateCount);
            var trigger = random.Next(10);

            // Avoid duplicate transitions
            var transitionKey = $"{fromState}-{trigger}";
            if (generatedTransitions.Contains(transitionKey))
            {
                i--; // Try again
                continue;
            }
            generatedTransitions.Add(transitionKey);

            sb.AppendLine($"        [Transition(LargeState.State{fromState}, LargeTrigger.Trigger{trigger}, LargeState.State{toState})]");
        }

        sb.AppendLine("        private void Configure() { }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}