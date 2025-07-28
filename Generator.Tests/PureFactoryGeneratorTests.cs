using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public sealed class PureFactoryGeneratorTests(ITestOutputHelper output) : GeneratorBaseClass( output)
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
   

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path => path;
        public override SourceText GetText(CancellationToken _) => SourceText.From(text, Encoding.UTF8);
    }

    [Fact]
    public void Generator_should_generate_factory_for_pure_machine()
    {
        // ──────────── kod użytkownika ────────────
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

        // ──────────── uruchom generator ────────────
        var (asm, diags, generated) =
            CompileAndRunGenerator(
                new[] { userSource },
                new StateMachineGenerator(),
                enableLogging: true,                 // DI paczka ustawia to automatycznie
                enableDependencyInjection: true);    // ← kluczowe

        Assert.Contains("PureMachine.Factory.g.cs", generated.Keys);


        OutputGeneratedCode(generated, "PureMachine.Factory");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        var factoryType = asm!.GetType("TestNamespace.PureMachineFactory");
        Assert.NotNull(factoryType);
    }


}