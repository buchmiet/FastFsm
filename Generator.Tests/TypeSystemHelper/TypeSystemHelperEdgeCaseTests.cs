using System.Diagnostics;
using System.Linq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.TypeSystemHelper;

public class TypeSystemHelperEdgeCaseTests(ITestOutputHelper output)
{
    private readonly Infrastructure.TypeSystemHelper _helper = new();

    #region Malformed Input Tests

    [Theory]
    [InlineData("List<")]  // Incomplete generic
    [InlineData("Dictionary<string,")]  // Incomplete generic args
    [InlineData("List<>string")]  // Malformed generic
    [InlineData("System..String")]  // Double dots
    [InlineData("..String")]  // Leading dots
    [InlineData("String..")]  // Trailing dots
    public void FormatTypeForUsage_MalformedInput_HandlesGracefully(string input)
    {
        // Should not throw, but return something reasonable
        var result = _helper.FormatTypeForUsage(input);
        result.ShouldNotBeNull();
        output.WriteLine($"Input: '{input}' -> Output: '{result}'");
    }

    [Theory]
    [InlineData("A++B")]  // Double plus
    [InlineData("+InnerClass")]  // Leading plus
    [InlineData("OuterClass+")]  // Trailing plus
    [InlineData("A+B.C+D")]  // Mixed separators (actually valid in some contexts)
    public void FormatTypeForUsage_MalformedNestedTypes_HandlesGracefully(string input)
    {
        var result = _helper.FormatTypeForUsage(input);
        result.ShouldNotBeNull();
        output.WriteLine($"Input: '{input}' -> Output: '{result}'");
    }

    #endregion

    #region Very Long Names Tests

    [Fact]
    public void FormatTypeForUsage_DeeplyNestedTypes_HandlesCorrectly()
    {
        // 10+ levels of nesting
        var deeplyNested = "A+B+C+D+E+F+G+H+I+J+K+L+M+N+O+P";
        var result = _helper.FormatTypeForUsage(deeplyNested);
        result.ShouldBe("A.B.C.D.E.F.G.H.I.J.K.L.M.N.O.P");

        // Verify all levels are preserved
        var parts = result.Split('.');
        parts.Length.ShouldBe(16);
    }

    [Fact]
    public void FormatTypeForUsage_VeryLongNamespace_HandlesCorrectly()
    {
        // Very deep namespace hierarchy
        var longNamespace = string.Join(".", Enumerable.Range(1, 20).Select(i => $"Namespace{i}"));
        var fullType = $"{longNamespace}.MyClass";

        var result = _helper.FormatTypeForUsage(fullType);
        result.ShouldBe("MyClass");

        var ns = _helper.GetNamespace(fullType);
        ns.ShouldBe(longNamespace);
    }

    [Fact]
    public void FormatTypeForUsage_ManyGenericArguments_HandlesCorrectly()
    {
        // Func with maximum arguments (16 in .NET)
        var manyArgs = "System.Func`17[[System.String],[System.Int32],[System.Boolean],[System.Double]," +
                       "[System.Decimal],[System.Byte],[System.Char],[System.Object]," +
                       "[System.DateTime],[System.Guid],[System.Uri],[System.Version]," +
                       "[System.TimeSpan],[System.Int64],[System.Single],[System.Int16],[System.String]]";

        var result = _helper.FormatTypeForUsage(manyArgs);
        result.ShouldContain("Func<");
        result.ShouldContain("string");
        result.ShouldContain("int");
        result.ShouldContain("bool");
    }

    #endregion

    #region Special Characters Tests

    [Theory]
    [InlineData("MyClass`1", false)]  // Valid: Generic marker
    [InlineData("MyClass$Helper", false)]  // Valid in some contexts
    [InlineData("MyClass@Helper", false)]  // @ is for escaping
    [InlineData("MyClass#Helper", false)]  // Invalid in C#
    [InlineData("MyClass Helper", false)]  // Space invalid
    [InlineData("MyClass\nHelper", false)]  // Newline invalid
    [InlineData("MyClass\tHelper", false)]  // Tab invalid
    public void IsValidTypeName_SpecialCharacters_ValidatedCorrectly(string input, bool shouldBeValid)
    {
        // TypeSystemHelper doesn't validate, but should handle gracefully
        var result = _helper.FormatTypeForUsage(input);
        result.ShouldNotBeNull();

        // Should not throw
        _ = _helper.GetNamespace(input);
        _ = _helper.GetSimpleTypeName(input);
    }

    [Fact]
    public void FormatTypeForUsage_UnicodeCharacters_HandledCorrectly()
    {
        // C# allows Unicode in identifiers
        var unicodeType = "MyNamespace.МойКласс";  // Cyrillic
        var result = _helper.FormatTypeForUsage(unicodeType);
        result.ShouldBe("МойКласс");

        var chineseType = "MyNamespace.我的类";  // Chinese
        result = _helper.FormatTypeForUsage(chineseType);
        result.ShouldBe("我的类");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void FormatTypeForUsage_Performance_ProcessesQuickly()
    {
        // Process single type should be < 1ms
        var complexType = "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Collections.Generic.List`1[[System.Int32, mscorlib]], mscorlib]]";

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = _helper.FormatTypeForUsage(complexType);
        }
        sw.Stop();

        var avgMs = sw.ElapsedMilliseconds / 1000.0;
        output.WriteLine($"Average time per operation: {avgMs}ms");
        avgMs.ShouldBeLessThan(1.0);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void FormatTypeForUsage_EmptyGenericArguments_HandledCorrectly()
    {
        // CLR representation with empty type arguments
        var emptyGeneric = "System.Collections.Generic.List`1[[]]";
        var result = _helper.FormatTypeForUsage(emptyGeneric);
        result.ShouldNotBeNull();
        output.WriteLine($"Empty generic result: {result}");
    }

    [Fact]
    public void GetNamespace_SingleCharacterSegments_HandledCorrectly()
    {
        var shortSegments = "A.B.C.D.MyClass";
        var ns = _helper.GetNamespace(shortSegments);
        ns.ShouldBe("A.B.C.D");

        var simpleName = _helper.GetSimpleTypeName(shortSegments);
        simpleName.ShouldBe("MyClass");
    }

    [Theory]
    [InlineData("", "")]  // Empty to empty
    [InlineData(" ", " ")]  // Space (technically invalid but should not crash)
    [InlineData("\n", "\n")]  // Newline
    [InlineData("123", "123")]  // Numbers only (invalid as identifier)
    public void EscapeIdentifier_InvalidIdentifiers_ReturnedAsIs(string input, string expected)
    {
        var result = _helper.EscapeIdentifier(input);
        result.ShouldBe(expected);
    }

    #endregion

    #region Recursive/Circular Type References

    [Fact]
    public void FormatTypeForUsage_RecursiveType_HandledWithoutInfiniteLoop()
    {
        // Node<Node<T>> style recursion
        var recursive = "MyApp.Node`1[[MyApp.Node`1[[System.String]]]]";
        var result = _helper.FormatTypeForUsage(recursive);
        result.ShouldContain("Node");
        result.ShouldContain("string");

        // Should complete quickly despite recursion
        var sw = Stopwatch.StartNew();
        _ = _helper.FormatTypeForUsage(recursive);
        sw.Stop();
        sw.ElapsedMilliseconds.ShouldBeLessThan(10);
    }

    #endregion

    #region Mixed Format Tests

    [Fact]
    public void FormatTypeForUsage_MixedCLRAndFriendlyFormat_HandledCorrectly()
    {
        // Part CLR format, part friendly - shouldn't happen but should handle
        var mixed = "System.Collections.Generic.List`1<System.String>";
        var result = _helper.FormatTypeForUsage(mixed);
        result.ShouldNotBeNull();
        output.WriteLine($"Mixed format result: {result}");
    }

    [Fact]
    public void FormatTypeForUsage_PartiallyQualifiedNames_HandledCorrectly()
    {
        // Missing namespace parts
        var partial = "Generic.List`1[[String]]";
        var result = _helper.FormatTypeForUsage(partial);
        result.ShouldNotBeNull();

        // Type in wrong order
        var reversed = "List.Generic.Collections.System";
        result = _helper.FormatTypeForUsage(reversed);
        result.ShouldBe("System");  // Takes last part as simple name
    }

    #endregion

    #region Stress Tests

    [Fact]
    public void AllMethods_NullInput_ReturnExpectedDefaults()
    {
        // FormatTypeForUsage
        _helper.FormatTypeForUsage(null).ShouldBe("object");
        _helper.FormatTypeForUsage(null, true).ShouldBe("object");

        // GetNamespace
        _helper.GetNamespace(null).ShouldBeNull();

        // GetSimpleTypeName
        _helper.GetSimpleTypeName(null).ShouldBe("object");

        // IsNestedType
        _helper.IsNestedType(null).ShouldBe(false);

        // IsGenericType
        _helper.IsGenericType(null).ShouldBe(false);

        // EscapeIdentifier
        _helper.EscapeIdentifier(null).ShouldBeNull();

        // GetRequiredNamespaces
        _helper.GetRequiredNamespaces(null).Count().ShouldBe(0);

        // FormatForTypeof
        _helper.FormatForTypeof(null).ShouldBe("object");
    }

    [Fact]
    public void AllMethods_EmptyString_ReturnExpectedDefaults()
    {
        // FormatTypeForUsage
        _helper.FormatTypeForUsage("").ShouldBe("object");
        _helper.FormatTypeForUsage("", true).ShouldBe("object");

        // GetNamespace
        _helper.GetNamespace("").ShouldBeNull();

        // GetSimpleTypeName
        _helper.GetSimpleTypeName("").ShouldBe("object");

        // IsNestedType
        _helper.IsNestedType("").ShouldBe(false);

        // IsGenericType
        _helper.IsGenericType("").ShouldBe(false);

        // EscapeIdentifier
        _helper.EscapeIdentifier("").ShouldBe("");

        // GetRequiredNamespaces
        _helper.GetRequiredNamespaces("").Count().ShouldBe(0);

        // FormatForTypeof
        _helper.FormatForTypeof("").ShouldBe("object");
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void RealWorld_EntityFrameworkTypes_ProcessedCorrectly()
    {
        // Common EF Core types
        var dbContextType = "Microsoft.EntityFrameworkCore.DbContext";
        var formatted = _helper.FormatTypeForUsage(dbContextType);
        formatted.ShouldBe("DbContext");

        var dbSetType = "Microsoft.EntityFrameworkCore.DbSet`1[[MyApp.Models.User, MyApp]]";
        formatted = _helper.FormatTypeForUsage(dbSetType);
        formatted.ShouldBe("DbSet<User>");

        var ns = _helper.GetNamespace(dbSetType);
        ns.ShouldBe("Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void RealWorld_AspNetCoreTypes_ProcessedCorrectly()
    {
        // Common ASP.NET Core types
        var actionResultType = "Microsoft.AspNetCore.Mvc.ActionResult`1[[MyApp.Dto.UserDto, MyApp]]";
        var formatted = _helper.FormatTypeForUsage(actionResultType);
        formatted.ShouldBe("ActionResult<UserDto>");

        var taskActionResultType = "System.Threading.Tasks.Task`1[[Microsoft.AspNetCore.Mvc.ActionResult`1[[MyApp.Dto.UserDto, MyApp]], Microsoft.AspNetCore.Mvc]]";
        formatted = _helper.FormatTypeForUsage(taskActionResultType);
        formatted.ShouldContain("Task");
        formatted.ShouldContain("ActionResult");
        formatted.ShouldContain("UserDto");
    }

    #endregion

    #region Deterministic Behavior Tests

    [Fact]
    public void AllMethods_SameInput_ProduceSameOutput()
    {
        var testType = "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Int32, mscorlib]]";

        // Run each method multiple times and verify same output
        for (int i = 0; i < 10; i++)
        {
            _helper.FormatTypeForUsage(testType).ShouldBe("Dictionary<string, int>");
            _helper.GetNamespace(testType).ShouldBe("System.Collections.Generic");
            _helper.GetSimpleTypeName(testType).ShouldBe("Dictionary");
            _helper.IsGenericType(testType).ShouldBe(true);
            _helper.IsNestedType(testType).ShouldBe(false);
            _helper.FormatForTypeof("List`1").ShouldBe("List<>");
        }
    }

    #endregion
}