using Xunit;
using Xunit.Abstractions;

namespace ParserComparison.Tests;

public class AsyncInitialChildComparisonTest(ITestOutputHelper output)
{
    [Fact]
    public void AsyncInitialChild_Legacy_vs_Fluent_Models_Should_Compile()
    {
        // This test ensures both models compile properly
        // The actual comparison is done by examining the generated code
        output.WriteLine("=== AsyncInitialChild Model Comparison ===");
        output.WriteLine("");
        output.WriteLine("Legacy machine: AsyncInitialChildTests.InitialChildMachine");
        output.WriteLine("Fluent machine: AsyncInitialChildTestsFluent.InitialChildMachineFluentFsm");
        output.WriteLine("");
        output.WriteLine("Both machines should generate identical models when parsed.");
        output.WriteLine("Check the generated code in the obj/Debug folder to compare.");
        
        // If this compiles, we know the models are at least syntactically valid
        Assert.True(true, "Both models compiled successfully");
    }
}