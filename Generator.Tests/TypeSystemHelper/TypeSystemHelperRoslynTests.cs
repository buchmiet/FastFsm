using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.TypeSystemHelper;

public class TypeSystemHelperRoslynTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    private readonly Infrastructure.TypeSystemHelper _helper = new();

    [Fact]
    public void BuildFullTypeName_SimpleTypes_ReturnsCorrectName()
    {
        // Arrange
        var code = @"
namespace System
{
    public class String { }
}

namespace MyApp.Models
{
    public class User { }
}";

        var compilation = CreateCompilation(code);

        // Act & Assert - System.String
        var stringSymbol = compilation.GetTypeByMetadataName("System.String");
        stringSymbol.ShouldNotBeNull();
        var stringName = _helper.BuildFullTypeName(stringSymbol);
        stringName.ShouldBe("System.String");

        // MyApp.Models.User
        var userSymbol = compilation.GetTypeByMetadataName("MyApp.Models.User");
        userSymbol.ShouldNotBeNull();
        var userName = _helper.BuildFullTypeName(userSymbol);
        userName.ShouldBe("MyApp.Models.User");
    }

    [Fact]
    public void BuildFullTypeName_NestedTypes_ReturnsWithPlusSeparator()
    {
        // Arrange
        var code = @"
namespace MyApp
{
    public class OuterClass
    {
        public class InnerClass
        {
            public class DeepClass { }
        }
    }
}";

        var compilation = CreateCompilation(code);

        // Act & Assert
        var outerSymbol = compilation.GetTypeByMetadataName("MyApp.OuterClass");
        outerSymbol.ShouldNotBeNull();

        var innerSymbol = outerSymbol.GetTypeMembers("InnerClass").FirstOrDefault();
        innerSymbol.ShouldNotBeNull();
        var innerName = _helper.BuildFullTypeName(innerSymbol);
        innerName.ShouldBe("MyApp.OuterClass+InnerClass");

        var deepSymbol = innerSymbol.GetTypeMembers("DeepClass").FirstOrDefault();
        deepSymbol.ShouldNotBeNull();
        var deepName = _helper.BuildFullTypeName(deepSymbol);
        deepName.ShouldBe("MyApp.OuterClass+InnerClass+DeepClass");
    }

    [Fact]
    public void BuildFullTypeName_GenericTypes_ReturnsWithTypeArguments()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;

namespace MyApp
{
    public class Container<T> { }
    
    public class Usage
    {
        public List<string> StringList { get; set; }
        public Dictionary<string, int> StringIntDict { get; set; }
        public Container<List<int>> NestedGeneric { get; set; }
    }
}";

        var compilation = CreateCompilation(code);
        var usageSymbol = compilation.GetTypeByMetadataName("MyApp.Usage");
        usageSymbol.ShouldNotBeNull();

        // Act & Assert - List<string>
        var stringListProp = usageSymbol.GetMembers("StringList").OfType<IPropertySymbol>().First();
        var listType = stringListProp.Type as INamedTypeSymbol;
        listType.ShouldNotBeNull();
        var listName = _helper.BuildFullTypeName(listType);
        listName.ShouldContain("List");
        listName.ShouldContain("String");

        // Dictionary<string, int>
        var dictProp = usageSymbol.GetMembers("StringIntDict").OfType<IPropertySymbol>().First();
        var dictType = dictProp.Type as INamedTypeSymbol;
        dictType.ShouldNotBeNull();
        var dictName = _helper.BuildFullTypeName(dictType);
        dictName.ShouldContain("Dictionary");
        dictName.ShouldContain("String");
        dictName.ShouldContain("Int32");
    }

    [Fact]
    public void Integration_ProcessRoslynSymbolThroughAllMethods()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;

namespace MyApp.Services
{
    public class DataService
    {
        public class Options
        {
            public int Timeout { get; set; }
        }
        
        public Dictionary<string, Options> GetConfiguration()
        {
            return new Dictionary<string, Options>();
        }
    }
}";

        var compilation = CreateCompilation(code);
        var serviceSymbol = compilation.GetTypeByMetadataName("MyApp.Services.DataService");
        serviceSymbol.ShouldNotBeNull();

        var methodSymbol = serviceSymbol.GetMembers("GetConfiguration").OfType<IMethodSymbol>().First();
        var returnType = methodSymbol.ReturnType as INamedTypeSymbol;
        returnType.ShouldNotBeNull();

        // Act - Build full type name from symbol
        var fullTypeName = _helper.BuildFullTypeName(returnType);
        output.WriteLine($"Full type name: {fullTypeName}");

        // Process through other methods
        var formatted = _helper.FormatTypeForUsage(fullTypeName);
        output.WriteLine($"Formatted: {formatted}");
        formatted.ShouldContain("Dictionary");

        var ns = _helper.GetNamespace(fullTypeName);
        ns.ShouldNotBeNull();
        output.WriteLine($"Namespace: {ns}");

        var isGeneric = _helper.IsGenericType(fullTypeName);
        isGeneric.ShouldBeTrue();

        var requiredNamespaces = _helper.GetRequiredNamespaces(fullTypeName).ToList();
        requiredNamespaces.ShouldNotBeEmpty();
        output.WriteLine($"Required namespaces: {string.Join(", ", requiredNamespaces)}");
    }

    [Fact]
    public void ComplexScenario_StateMachineTypes_ProcessedCorrectly()
    {
        // Arrange - Simulate state machine generator scenario
        var code = @"
using System;

namespace StateMachine.Tests
{
    public enum OrderState { New, Processing, Shipped }
    public enum OrderTrigger { Process, Ship }
    
    public class OrderPayload 
    { 
        public int OrderId { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public partial class OrderStateMachine
    {
        public OrderState CurrentState { get; private set; }
        
        public bool TryFire(OrderTrigger trigger, OrderPayload payload)
        {
            // Implementation
            return true;
        }
    }
}";

        var compilation = CreateCompilation(code);

        // Get all relevant symbols
        var stateEnum = compilation.GetTypeByMetadataName("StateMachine.Tests.OrderState");
        var triggerEnum = compilation.GetTypeByMetadataName("StateMachine.Tests.OrderTrigger");
        var payloadClass = compilation.GetTypeByMetadataName("StateMachine.Tests.OrderPayload");
        var machineClass = compilation.GetTypeByMetadataName("StateMachine.Tests.OrderStateMachine");

        stateEnum.ShouldNotBeNull();
        triggerEnum.ShouldNotBeNull();
        payloadClass.ShouldNotBeNull();
        machineClass.ShouldNotBeNull();

        // Build full names
        var stateTypeName = _helper.BuildFullTypeName(stateEnum);
        var triggerTypeName = _helper.BuildFullTypeName(triggerEnum);
        var payloadTypeName = _helper.BuildFullTypeName(payloadClass);

        stateTypeName.ShouldBe("StateMachine.Tests.OrderState");
        triggerTypeName.ShouldBe("StateMachine.Tests.OrderTrigger");
        payloadTypeName.ShouldBe("StateMachine.Tests.OrderPayload");

        // Format for code generation
        var formattedState = _helper.FormatTypeForUsage(stateTypeName);
        var formattedTrigger = _helper.FormatTypeForUsage(triggerTypeName);
        var formattedPayload = _helper.FormatTypeForUsage(payloadTypeName);

        formattedState.ShouldBe("OrderState");
        formattedTrigger.ShouldBe("OrderTrigger");
        formattedPayload.ShouldBe("OrderPayload");

        // Get required namespaces for using statements
        var namespaces = new[] { stateTypeName, triggerTypeName, payloadTypeName }
            .SelectMany(t => _helper.GetRequiredNamespaces(t))
            .Distinct()
            .ToList();

        namespaces.ShouldContain("StateMachine.Tests");
    }

    [Fact]
    public void AttributeScenario_ProcessAttributeTypes()
    {
        // Arrange
        var code = @"
using System;

namespace Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StateMachineAttribute : Attribute
    {
        public Type StateType { get; }
        public Type TriggerType { get; }
        
        public StateMachineAttribute(Type stateType, Type triggerType)
        {
            StateType = stateType;
            TriggerType = triggerType;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TransitionAttribute : Attribute
    {
        public object FromState { get; set; }
        public object Trigger { get; set; }
        public object ToState { get; set; }
    }
}

namespace MyApp
{
    public enum State { A, B }
    public enum Trigger { Next }
    
    [Abstractions.Attributes.StateMachine(typeof(State), typeof(Trigger))]
    public partial class MyMachine
    {
        [Abstractions.Attributes.Transition(FromState = State.A, Trigger = Trigger.Next, ToState = State.B)]
        private void Configure() { }
    }
}";

        var compilation = CreateCompilation(code);
        var machineSymbol = compilation.GetTypeByMetadataName("MyApp.MyMachine");
        machineSymbol.ShouldNotBeNull();

        // Get attribute data
        var stateMachineAttr = machineSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "StateMachineAttribute");
        stateMachineAttr.ShouldNotBeNull();

        // Process constructor arguments (typeof expressions)
        var stateTypeArg = stateMachineAttr.ConstructorArguments[0];
        var triggerTypeArg = stateMachineAttr.ConstructorArguments[1];

        if (stateTypeArg.Value is INamedTypeSymbol stateType)
        {
            var stateTypeName = _helper.BuildFullTypeName(stateType);
            stateTypeName.ShouldBe("MyApp.State");

            var formattedForTypeof = _helper.FormatForTypeof(stateTypeName);
            formattedForTypeof.ShouldBe("global::MyApp.State");
        }
    }

    private CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}