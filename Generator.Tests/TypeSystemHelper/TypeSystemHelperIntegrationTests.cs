using System.Linq;
using Shouldly;
using Xunit;

namespace Generator.Tests.TypeSystemHelper;

public class TypeSystemHelperIntegrationTests
{
    private readonly Infrastructure.TypeSystemHelper _helper = new();

    [Fact]
    public void ComplexGenericType_ProcessedThroughAllMethods_ReturnsCorrectResults()
    {
        // Arrange
        var complexType = "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[MyApp.Models.User, MyApp]]";

        // Act & Assert - Format for usage
        var formatted = _helper.FormatTypeForUsage(complexType);
        formatted.ShouldBe("Dictionary<string, User>");

        // Namespace extraction
        var ns = _helper.GetNamespace(complexType);
        ns.ShouldBe("System.Collections.Generic");

        // Simple type name
        var simpleName = _helper.GetSimpleTypeName(complexType);
        simpleName.ShouldBe("Dictionary");

        // Type checks
        _helper.IsGenericType(complexType).ShouldBeTrue();
        _helper.IsNestedType(complexType).ShouldBeFalse();

        // Required namespaces
        var requiredNamespaces = _helper.GetRequiredNamespaces(complexType).ToList();
        requiredNamespaces.ShouldContain("System.Collections.Generic");
        requiredNamespaces.ShouldContain("MyApp.Models");
    }

    [Fact]
    public void NestedGenericType_ProcessedCorrectly()
    {
        // Arrange
        var nestedGeneric = "MyApp.Container+GenericNested`1[[System.Int32]]";

        // Act & Assert
        var formatted = _helper.FormatTypeForUsage(nestedGeneric);
        formatted.ShouldBe("MyApp.Container.GenericNested<int>");

        _helper.IsNestedType(nestedGeneric).ShouldBeTrue();
        _helper.IsGenericType(nestedGeneric).ShouldBeTrue();

        var ns = _helper.GetNamespace(nestedGeneric);
        ns.ShouldBe("MyApp");
    }

    [Fact]
    public void MultiLevelNestedType_FormattedCorrectly()
    {
        // Arrange
        var multiNested = "Company.Product.OuterClass+MiddleClass+InnerClass";

        // Act
        var formatted = _helper.FormatTypeForUsage(multiNested);
        var simpleName = _helper.GetSimpleTypeName(multiNested);
        var ns = _helper.GetNamespace(multiNested);

        // Assert
        formatted.ShouldBe("Company.Product.OuterClass.MiddleClass.InnerClass");
        simpleName.ShouldBe("Product.OuterClass.MiddleClass.InnerClass");
        ns.ShouldBe("Company.Product");
        _helper.IsNestedType(multiNested).ShouldBeTrue();
    }

    [Fact]
    public void ComplexGenericWithNestedTypeArguments_ProcessedCorrectly()
    {
        // Arrange
        var complexType = "System.Collections.Generic.Dictionary`2[[Company.Outer+Inner, Company],[System.Collections.Generic.List`1[[System.String]], mscorlib]]";

        // Act
        var formatted = _helper.FormatTypeForUsage(complexType);

        // Assert - This is a complex case
        formatted.ShouldContain("Dictionary");
        _helper.IsGenericType(complexType).ShouldBeTrue();
    }

    [Fact]
    public void TypeWithGlobalPrefix_HandledCorrectly()
    {
        // Arrange
        var typeWithGlobal = "global::System.Collections.Generic.List`1[[global::MyApp.Models.User]]";

        // Act
        var formatted = _helper.FormatTypeForUsage(typeWithGlobal);
        var ns = _helper.GetNamespace(typeWithGlobal);

        // Assert
        formatted.ShouldBe("List<User>");
        ns.ShouldBe("System.Collections.Generic");
    }

    [Fact]
    public void ArrayTypes_ProcessedCorrectly()
    {
        // Test array type handling
        var arrayType = "System.String[]";
        var formatted = _helper.FormatTypeForUsage(arrayType);
        formatted.ShouldBe("string[]");

        var multiDimArray = "System.Int32[,]";
        formatted = _helper.FormatTypeForUsage(multiDimArray);
        formatted.ShouldBe("int[,]");

        var jaggedArray = "System.String[][]";
        formatted = _helper.FormatTypeForUsage(jaggedArray);
        formatted.ShouldBe("string[][]");
    }

    [Fact]
    public void NullableValueTypes_FormattedCorrectly()
    {
        // Using both syntaxes for nullable
        var nullable1 = "System.Nullable`1[[System.Int32]]";
        var formatted1 = _helper.FormatTypeForUsage(nullable1);
        formatted1.ShouldBe("Nullable<int>");

        var nullable2 = "System.Int32?";
        var formatted2 = _helper.FormatTypeForUsage(nullable2);
        formatted2.ShouldBe("int?");
    }

    [Fact]
    public void TupleTypes_ProcessedCorrectly()
    {
        var valueTuple = "System.ValueTuple`2[[System.String],[System.Int32]]";
        var formatted = _helper.FormatTypeForUsage(valueTuple);
        formatted.ShouldBe("ValueTuple<string, int>");

        var tupleWith3 = "System.ValueTuple`3[[System.String],[System.Int32],[System.Boolean]]";
        formatted = _helper.FormatTypeForUsage(tupleWith3);
        formatted.ShouldBe("ValueTuple<string, int, bool>");
    }

    [Fact]
    public void NamespaceHierarchy_ExtractedCorrectly()
    {
        // Deep namespace hierarchy
        var deepType = "Company.Product.Module.SubModule.Features.StateMachines.MyStateMachine";

        var ns = _helper.GetNamespace(deepType);
        ns.ShouldBe("Company.Product.Module.SubModule.Features.StateMachines");

        var simpleName = _helper.GetSimpleTypeName(deepType);
        simpleName.ShouldBe("MyStateMachine");

        var requiredNamespaces = _helper.GetRequiredNamespaces(deepType).ToList();
        requiredNamespaces.ShouldContain("Company.Product.Module.SubModule.Features.StateMachines");
    }

    [Fact]
    public void GenericConstraints_TypesFormattedCorrectly()
    {
        // Types that might appear in generic constraints
        var interfaceType = "System.IComparable`1[[System.String]]";
        var formatted = _helper.FormatTypeForUsage(interfaceType);
        formatted.ShouldBe("IComparable<string>");

        var delegateType = "System.Func`2[[System.String],[System.Int32]]";
        formatted = _helper.FormatTypeForUsage(delegateType);
        formatted.ShouldBe("Func<string, int>");
    }

    [Fact]
    public void SpecialCharactersInTypeNames_HandledCorrectly()
    {
        // Type names with numbers and underscores
        var typeWithNumbers = "MyApp.Class123_Helper";
        var formatted = _helper.FormatTypeForUsage(typeWithNumbers);
        formatted.ShouldBe("Class123_Helper");

        var ns = _helper.GetNamespace(typeWithNumbers);
        ns.ShouldBe("MyApp");
    }

    [Fact]
    public void EmptyNamespace_HandledCorrectly()
    {
        // Type in global namespace
        var globalType = "MyGlobalClass";

        var ns = _helper.GetNamespace(globalType);
        ns.ShouldBeNull();

        var formatted = _helper.FormatTypeForUsage(globalType);
        formatted.ShouldBe("MyGlobalClass");

        var requiredNamespaces = _helper.GetRequiredNamespaces(globalType).ToList();
        requiredNamespaces.ShouldBeEmpty();
    }

    [Fact]
    public void KeywordEscaping_InComplexScenarios()
    {
        // Enum values that are keywords
        _helper.EscapeIdentifier("class").ShouldBe("@class");
        _helper.EscapeIdentifier("event").ShouldBe("@event");

        // Combined with type processing
        var typeWithKeyword = "MyApp.@class";  // Already escaped
        var formatted = _helper.FormatTypeForUsage(typeWithKeyword);
        formatted.ShouldBe("@class");
    }

    [Fact]
    public void CircularReference_Scenario()
    {
        // Type that references itself (like in tree structures)
        var treeNodeType = "MyApp.TreeNode`1[[MyApp.TreeNode`1[[System.String]]]]";

        var formatted = _helper.FormatTypeForUsage(treeNodeType);
        formatted.ShouldContain("TreeNode");
        _helper.IsGenericType(treeNodeType).ShouldBeTrue();
    }
}