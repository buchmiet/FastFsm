using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests
{
    public class LoggingGeneratorTests(ITestOutputHelper output) : GeneratorBaseClass(output)
    {
        private void OutputGeneratedCode(Dictionary<string, string> generatedSources, string filterKeyword)
        {
            foreach (var source in generatedSources.Where(kvp => kvp.Key.Contains(filterKeyword)))
            {
                output.WriteLine($"=== {source.Key} ===");
                output.WriteLine(source.Value);
                output.WriteLine("=== END ===\n");
            }
        }
        [Fact]
        public void Generator_should_generate_logging_for_pure_machine()
        {
            // ───────── kod użytkownika ─────────
            const string userSource = """
                                      using Abstractions.Attributes;
                                      namespace TestNamespace
                                      {
                                          public enum State   { Idle, Ready }
                                          public enum Trigger { Init }
                                      
                                          [StateMachine(typeof(State), typeof(Trigger))]
                                          public partial class PureMachine
                                          {
                                              [Transition(State.Idle, Trigger.Init, State.Ready)]
                                              private void OnInit() { }
                                          }
                                      }
                                      """;

            // ───────── uruchom generator ─────────
            var (asm, diags, generated) = CompileAndRunGenerator(
                new[] { userSource },
                new StateMachineGenerator(),
                enableLogging: true,            // ← tylko logowanie
                enableDependencyInjection: false);
            OutputGeneratedCode(generated, "PureMachine");
            // ───────── asercje ─────────
            Assert.Contains("PureMachineLog.g.cs", generated.Keys);


            Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(asm);

            var logType = asm!.GetType("TestNamespace.PureMachineLog");
            Assert.NotNull(logType);           // klasa logująca istnieje
        }
    }
}
