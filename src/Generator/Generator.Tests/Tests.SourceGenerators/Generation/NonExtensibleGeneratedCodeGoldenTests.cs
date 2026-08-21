using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Generator;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Generation;

public sealed class NonExtensibleGeneratedCodeGoldenTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    private const string ExpectedSha256FirstHalf = "85A3854943BFBAFB64B9F60E871BCBD1";
    private const string ExpectedSha256SecondHalf = "B712C073118DFD9521DDB987B7418D73";

    [Fact]
    public void Non_extensible_machine_generated_code_matches_0_9_1_baseline()
    {
        const string source = """
using Abstractions.Attributes;
namespace Golden;

public enum State { A, B }
public enum Trigger { Go }

[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = false)]
public partial class Machine
{
    [Transition(State.A, Trigger.Go, State.B)]
    private void Configure() { }
}
""";

        var (assembly, diagnostics, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(assembly);
        var generated = generatedSources.Values.Single(text => text.Contains("public partial class Machine"));
        var normalized = Regex.Replace(
            generated.Replace("\r\n", "\n"),
            "// Generator Build: .* UTC",
            "// Generator Build: <normalized> UTC");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        Assert.Equal(ExpectedSha256FirstHalf, hash[..32]);
        Assert.Equal(ExpectedSha256SecondHalf, hash[32..]);
    }
}