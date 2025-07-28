using Shouldly;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.TypeSystemHelper;

public class TypeSystemHelperTests(ITestOutputHelper output)
{
    private readonly Infrastructure.TypeSystemHelper _helper = new();


    // W pliku Generator.Tests/TypeSystemHelper/TypeSystemHelperTests.cs

    [Fact]
    public void TypeSystemHelper_DoesNotCrash_OnFullMultiPayloadMachineTypes()
    {
        // Arrange: Dokładne typy z problematycznej maszyny
        var typesToProcess = new[]
        {
        // Typy enumów
        "StateMachine.Tests.Machines.OrderState",
        "StateMachine.Tests.Machines.OrderTrigger",
        // Typy payloadów
        "StateMachine.Tests.Machines.OrderPayload",
        "StateMachine.Tests.Machines.PaymentPayload",
        "StateMachine.Tests.Machines.ShippingPayload"
    };

        // Act & Assert: Wywołaj wszystkie publiczne metody i sprawdź, czy nie rzucają wyjątku
        try
        {
            foreach (var typeName in typesToProcess)
            {
                output.WriteLine($"Processing type: {typeName}");

                // Każde z tych wywołań może być źródłem przepełnienia stosu
                var formattedUsage = _helper.FormatTypeForUsage(typeName);
                var formattedTypeof = _helper.FormatForTypeof(typeName);
                var ns = _helper.GetNamespace(typeName);
                var simpleName = _helper.GetSimpleTypeName(typeName);
                var isGeneric = _helper.IsGenericType(typeName);
                var isNested = _helper.IsNestedType(typeName);
                var requiredNs = _helper.GetRequiredNamespaces(typeName).ToList();

                // Wypisz wyniki, żeby zobaczyć, czy są sensowne
                output.WriteLine($"  -> Formatted Usage: {formattedUsage}");
                output.WriteLine($"  -> Formatted Typeof: {formattedTypeof}");
                output.WriteLine($"  -> Namespace: {ns ?? "null"}");
                output.WriteLine($"  -> Simple Name: {simpleName}");
                output.WriteLine($"  -> Is Generic: {isGeneric}");
                output.WriteLine($"  -> Is Nested: {isNested}");
                output.WriteLine($"  -> Required NS: {string.Join(", ", requiredNs)}");
            }
        }
        catch (Exception ex)
        {
            // Jeśli test się zawiesza, ten blok może się nie wykonać,
            // ale jeśli rzuci inny wyjątek, zobaczymy go.
            Assert.Fail($"TypeSystemHelper threw an exception: {ex.Message}");
        }

        // Jeśli test dotrze do tego miejsca bez zawieszenia,
        // oznacza to, że TypeSystemHelper prawdopodobnie nie jest problemem.
        Assert.True(true, "Test completed without crashing.");
    }

    [Fact]
    public void GetRequiredNamespaces_NestedGenericArgument_ReturnsOuterNamespace()
    {
        var input =
            "System.Collections.Generic.Dictionary<System.String, MyNamespace.Outer+Inner>";

        var nsList = _helper.GetRequiredNamespaces(input).Distinct().OrderBy(s => s).ToArray();

        nsList.ShouldBe(new[] { "MyNamespace", "System", "System.Collections.Generic" });
    }


    [Theory]
    [InlineData("Outer+Inner", new string[0])]
    [InlineData("MyNamespace.Outer+Inner", new string[0])]
    [InlineData("System.Collections.Generic.Dictionary<System.String, MyNamespace.Outer+Inner>",
        new[] { "System.Collections.Generic", "System", "MyNamespace" })]
    public void GetRequiredNamespaces_MixedCases(string input, string[] expected)
    {
        var result = _helper.GetRequiredNamespaces(input)
            .Distinct()
            .OrderBy(s => s)      // porównujemy uporządkowaną listę
            .ToArray();

        result.ShouldBe(expected.OrderBy(s => s).ToArray());
    }


    #region FormatTypeForUsage Tests

    [Theory]
    [InlineData("System.String", "string")]
    [InlineData("System.Int32", "int")]
    [InlineData("System.Boolean", "bool")]
    [InlineData("System.Void", "void")]
    [InlineData("System.Object", "object")]
    [InlineData("System.Decimal", "decimal")]
    [InlineData("System.Double", "double")]
    [InlineData("System.Single", "float")]
    [InlineData("System.Int64", "long")]
    [InlineData("System.Byte", "byte")]
    [InlineData("System.Char", "char")]
    [InlineData("System.UInt32", "uint")]
    [InlineData("System.UInt64", "ulong")]
    [InlineData("System.Int16", "short")]
    [InlineData("System.UInt16", "ushort")]
    [InlineData("System.SByte", "sbyte")]
    public void FormatTypeForUsage_SystemTypeAliases_ReturnsAlias(string input, string expected)
    {
        _helper.FormatTypeForUsage(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("System.Collections.Generic.List<System.String>", "List<string>")]
    [InlineData("System.Nullable<System.Int32>", "Nullable<int>")]
    [InlineData("System.Collections.Generic.Dictionary<System.String, System.Int32>", "Dictionary<string, int>")]
    public void FormatTypeForUsage_GenericTypesWithAliases_ReturnsFormattedType(string input, string expected)
    {
        _helper.FormatTypeForUsage(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("OuterClass+InnerClass", "OuterClass.InnerClass")]
    [InlineData("Namespace.Outer+Inner", "Namespace.Outer.Inner")]
    [InlineData("A+B+C+D", "A.B.C.D")]
    public void FormatTypeForUsage_NestedTypes_ReturnsWithDotSeparator(string input, string expected)
    {
        _helper.FormatTypeForUsage(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("System.Collections.Generic.List`1[[System.String, mscorlib]]", "List<string>")]
    [InlineData("System.Nullable`1[[System.Int32, mscorlib]]", "Nullable<int>")]
    [InlineData("Dictionary`2[[System.String, mscorlib],[System.Int32, mscorlib]]", "Dictionary<string, int>")]
    public void FormatTypeForUsage_ClrGenericFormat_ReturnsFormattedType(string input, string expected)
    {
        _helper.FormatTypeForUsage(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("System.String", true, "string")] // Aliases don't get global prefix
    [InlineData("MyNamespace.MyClass", true, "global::MyNamespace.MyClass")]
    [InlineData("List<MyClass>", true, "global::List<global::MyClass>")]
    public void FormatTypeForUsage_WithGlobalPrefix_ReturnsCorrectFormat(string input, bool useGlobal, string expected)
    {
        _helper.FormatTypeForUsage(input, useGlobal).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "object")]
    [InlineData("", "object")]
    [InlineData("global::System.String", "string")]
    public void FormatTypeForUsage_EdgeCases_HandlesCorrectly(string input, string expected)
    {
        _helper.FormatTypeForUsage(input).ShouldBe(expected);
    }

    #endregion

    #region GetNamespace Tests

    [Theory]
    [InlineData("System.String", "System")]
    [InlineData("System.Collections.Generic.List", "System.Collections.Generic")]
    [InlineData("MyApp.Models.User", "MyApp.Models")]
    [InlineData("SingleClass", null)]
    public void GetNamespace_StandardCases_ReturnsCorrectNamespace(string input, string expected)
    {
        _helper.GetNamespace(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("MyNamespace.OuterClass+InnerClass", "MyNamespace")]
    [InlineData("A.B.C+D+E", "A.B")]
    public void GetNamespace_NestedTypes_ReturnsContainingNamespace(string input, string expected)
    {
        _helper.GetNamespace(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("System.Collections.Generic.List<T>", "System.Collections.Generic")]
    [InlineData("MyApp.Generic`1[[System.String]]", "MyApp")]
    public void GetNamespace_GenericTypes_ReturnsNamespaceWithoutGenericPart(string input, string expected)
    {
        _helper.GetNamespace(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("global::System.String", "System")]
    public void GetNamespace_EdgeCases_HandlesCorrectly(string input, string expected)
    {
        _helper.GetNamespace(input).ShouldBe(expected);
    }

    #endregion

    #region GetSimpleTypeName Tests

    [Theory]
    [InlineData("System.String", "String")]
    [InlineData("MyNamespace.MyClass", "MyClass")]
    [InlineData("SimpleClass", "SimpleClass")]
    public void GetSimpleTypeName_StandardCases_ReturnsTypeName(string input, string expected)
    {
        _helper.GetSimpleTypeName(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Outer+Inner", "Outer.Inner")]
    [InlineData("A.B+C+D", "B.C.D")]
    public void GetSimpleTypeName_NestedTypes_ReturnsFormattedName(string input, string expected)
    {
        _helper.GetSimpleTypeName(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("List<T>", "List")]
    [InlineData("Dictionary`2", "Dictionary")]
    public void GetSimpleTypeName_GenericTypes_ReturnsNameWithoutGenericPart(string input, string expected)
    {
        _helper.GetSimpleTypeName(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "object")]
    [InlineData("", "object")]
    public void GetSimpleTypeName_EdgeCases_ReturnsObject(string input, string expected)
    {
        _helper.GetSimpleTypeName(input).ShouldBe(expected);
    }

    #endregion

    #region IsNestedType Tests

    [Theory]
    [InlineData("Outer+Inner", true)]
    [InlineData("A.B+C", true)]
    [InlineData("A+B+C+D", true)]
    public void IsNestedType_PositiveCases_ReturnsTrue(string input, bool expected)
    {
        _helper.IsNestedType(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("System.String", false)]
    [InlineData("MyNamespace.MyClass", false)]
    [InlineData("List<T>", false)]
    public void IsNestedType_NegativeCases_ReturnsFalse(string input, bool expected)
    {
        _helper.IsNestedType(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsNestedType_EdgeCases_ReturnsFalse(string input, bool expected)
    {
        _helper.IsNestedType(input).ShouldBe(expected);
    }

    #endregion

    #region IsGenericType Tests

    [Theory]
    [InlineData("List<T>", true)]
    [InlineData("Dictionary`2", true)]
    [InlineData("Nullable`1[[System.Int32]]", true)]
    public void IsGenericType_PositiveCases_ReturnsTrue(string input, bool expected)
    {
        _helper.IsGenericType(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("String", false)]
    [InlineData("System.Collections.ArrayList", false)]
    public void IsGenericType_NegativeCases_ReturnsFalse(string input, bool expected)
    {
        _helper.IsGenericType(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsGenericType_EdgeCases_ReturnsFalse(string input, bool expected)
    {
        _helper.IsGenericType(input).ShouldBe(expected);
    }

    #endregion

    #region EscapeIdentifier Tests

    [Theory]
    [InlineData("class", "@class")]
    [InlineData("event", "@event")]
    [InlineData("async", "@async")]
    [InlineData("await", "@await")]
    [InlineData("bool", "@bool")]
    [InlineData("interface", "@interface")]
    [InlineData("delegate", "@delegate")]
    [InlineData("void", "@void")]
    [InlineData("namespace", "@namespace")]
    [InlineData("using", "@using")]
    public void EscapeIdentifier_CSharpKeywords_ReturnsEscaped(string input, string expected)
    {
        _helper.EscapeIdentifier(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("myVariable", "myVariable")]
    [InlineData("Class", "Class")]
    [InlineData("EVENT", "EVENT")]
    [InlineData("MyClass", "MyClass")]
    public void EscapeIdentifier_NonKeywords_ReturnsUnchanged(string input, string expected)
    {
        _helper.EscapeIdentifier(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void EscapeIdentifier_EdgeCases_ReturnsInput(string input, string expected)
    {
        _helper.EscapeIdentifier(input).ShouldBe(expected);
    }

    #endregion

    #region GetRequiredNamespaces Tests

    [Theory]
    [InlineData("System.String", new[] { "System" })]
    [InlineData("MyApp.Models.User", new[] { "MyApp.Models" })]
    public void GetRequiredNamespaces_SimpleTypes_ReturnsNamespace(string input, string[] expected)
    {
        var result = _helper.GetRequiredNamespaces(input).ToArray();
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Outer+Inner", new string[0])]
    [InlineData("MyNamespace.Outer+Inner", new string[0])]
    public void GetRequiredNamespaces_NestedTypes_ReturnsEmpty(string input, string[] expected)
    {
        var result = _helper.GetRequiredNamespaces(input).ToArray();
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("System.Collections.Generic.List<MyApp.User>", new[] { "System.Collections.Generic", "MyApp" })]
    [InlineData("System.Collections.Generic.Dictionary<System.String, MyApp.Models.User>",
        new[] { "System.Collections.Generic", "MyApp.Models" })]
    public void GetRequiredNamespaces_GenericTypes_ReturnsAllNamespaces(string input, string[] expected)
    {
        var result = _helper.GetRequiredNamespaces(input).ToArray();
        result.ShouldContain(expected[0]);
        if (expected.Length > 1)
            result.ShouldContain(expected[1]);
    }

    [Theory]
    [InlineData(null, new string[0])]
    [InlineData("", new string[0])]
    public void GetRequiredNamespaces_EdgeCases_ReturnsEmpty(string input, string[] expected)
    {
        var result = _helper.GetRequiredNamespaces(input).ToArray();
        result.ShouldBe(expected);
    }

    #endregion

    #region FormatForTypeof Tests

    [Theory]
    [InlineData("System.String", "global::System.String")]
    [InlineData("MyClass", "MyClass")]
    public void FormatForTypeof_SimpleTypes_ReturnsWithGlobalPrefix(string input, string expected)
    {
        _helper.FormatForTypeof(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("List`1", "List<>")]
    [InlineData("Dictionary`2", "Dictionary<,>")]
    [InlineData("Func`3", "Func<,,>")]
    public void FormatForTypeof_GenericTypes_ReturnsSpecialSyntax(string input, string expected)
    {
        _helper.FormatForTypeof(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "object")]
    [InlineData("", "object")]
    public void FormatForTypeof_EdgeCases_ReturnsObject(string input, string expected)
    {
        _helper.FormatForTypeof(input).ShouldBe(expected);
    }

    #endregion
}