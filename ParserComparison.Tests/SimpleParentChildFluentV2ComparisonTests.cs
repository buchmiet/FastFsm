using System.IO;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;

namespace ParserComparison.Tests
{
    public class SimpleParentChildFluentV2ComparisonTests
    {
        private readonly ITestOutputHelper _output;

        public SimpleParentChildFluentV2ComparisonTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void FluentV2_Should_Match_LegacyHierarchy()
        {
            // Load both models
            var fluentModel = TestHelpers.ParseWithFluentParser(
                "SimpleParentChildFluentV2Test.cs",
                nameof(SimpleParentChildFluentV2Test));
                
            var legacyModel = TestHelpers.ParseWithStateMachineParser(
                "SimpleParentChildFluentV2LegacyComparison.cs",
                nameof(SimpleParentChildFluentV2LegacyComparison));

            // Debug output
            _output.WriteLine("=== FLUENT V2 MODEL ===");
            _output.WriteLine(JsonSerializer.Serialize(fluentModel, TestHelpers.JsonOptions));
            _output.WriteLine("\n=== LEGACY MODEL ===");
            _output.WriteLine(JsonSerializer.Serialize(legacyModel, TestHelpers.JsonOptions));

            // Check hierarchy
            _output.WriteLine("\n=== HIERARCHY COMPARISON ===");
            _output.WriteLine("Fluent ParentOf:");
            foreach (var kvp in fluentModel.ParentOf)
            {
                _output.WriteLine($"  {kvp.Key} -> {kvp.Value}");
            }
            
            _output.WriteLine("\nLegacy ParentOf:");
            foreach (var kvp in legacyModel.ParentOf)
            {
                _output.WriteLine($"  {kvp.Key} -> {kvp.Value}");
            }

            // Assertions
            Assert.Equal(legacyModel.ParentOf.Count, fluentModel.ParentOf.Count);
            
            // Check specific parent relationships
            Assert.Equal("Working", legacyModel.ParentOf["Working_Initializing"]);
            Assert.Equal("Working", legacyModel.ParentOf["Working_Processing"]);
            Assert.Equal("Working", legacyModel.ParentOf["Working_Validating"]);
            
            // These should match in Fluent but currently don't
            Assert.Equal(legacyModel.ParentOf["Working_Initializing"], fluentModel.ParentOf["Working_Initializing"]);
            Assert.Equal(legacyModel.ParentOf["Working_Processing"], fluentModel.ParentOf["Working_Processing"]);
            Assert.Equal(legacyModel.ParentOf["Working_Validating"], fluentModel.ParentOf["Working_Validating"]);
        }
    }
}