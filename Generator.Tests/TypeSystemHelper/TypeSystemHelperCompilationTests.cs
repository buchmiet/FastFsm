using Microsoft.CodeAnalysis;
using Shouldly;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.TypeSystemHelper
{

    public class TypeSystemHelperCompilationTests(ITestOutputHelper output) : GeneratorBaseClass(output)
    {
        private readonly Infrastructure.TypeSystemHelper _helper = new();

        [Fact]
        public void GeneratedTypeDeclarations_Compile_Successfully()
        {
            // Arrange
            var types = new[]
            {
        ("System.String",                                         "string"),
        ("System.Collections.Generic.List<System.String>",        "List<string>"),
        ("System.Collections.Generic.Dictionary<System.String, System.Int32>", "Dictionary<string, int>"),
        ("MyApp.Models.User",                                     "User"),
        ("OuterClass+InnerClass",                                 "OuterClass.InnerClass")
    };

            var sb = new StringBuilder(@"
using System;
using System.Collections.Generic;
using MyApp.Models;

namespace TestNamespace
{
    public class User { }
    public class OuterClass 
    { 
        public class InnerClass { }
    }

    public class TestClass
    {
");

            // ---------- pola --------------------------------------------------------
            foreach (var (fullType, _) in types)
            {
                var formatted = _helper.FormatTypeForUsage(fullType);

                // nazwa pola: usuwamy wszystkie znaki nielegalne w identyfikatorach
                var fieldName = formatted
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty)
                    .Replace(",", string.Empty)
                    .Replace(".", string.Empty)
                    .Replace(" ", string.Empty);

                sb.AppendLine($"        public {formatted} Field{fieldName};");
            }

            // ---------- ciało metody -----------------------------------------------
            sb.AppendLine(@"
        public void TestMethod()
        {
            // Method parameters
");

            foreach (var (fullType, _) in types)
            {
                var formatted = _helper.FormatTypeForUsage(fullType);
                var varName = formatted
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty)
                    .Replace(",", string.Empty)
                    .Replace(".", string.Empty)
                    .Replace(" ", string.Empty);

                sb.AppendLine($"            {formatted} local{varName} = default({formatted});");
            }

            sb.AppendLine(@"
        }
    }
}");

            // ---------- dodatkowy namespace ----------------------------------------
            sb.AppendLine(@"
namespace MyApp.Models
{
    public class User { }
}");

            var code = sb.ToString();

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();
        }



        [Fact]
        public void GeneratedUsingStatements_FromGetRequiredNamespaces_CompileSuccessfully()
        {
            // Arrange
            var typesToProcess = new[]
            {
                "System.Collections.Generic.List<MyApp.Models.User>",
                "System.Threading.Tasks.Task<System.String>",
                "System.Collections.Generic.Dictionary<MyApp.Data.Entity, System.Int32>"
            };

            var namespaces = typesToProcess
                .SelectMany(t => _helper.GetRequiredNamespaces(t))
                .Distinct()
                .OrderBy(ns => ns);

            var code = "";
            foreach (var ns in namespaces)
            {
                code += $"using {ns};\n";
            }

            code += @"
namespace TestApp
{
    public class TestClass
    {
        public List<User> Users { get; set; }
        public Task<string> GetNameAsync() => Task.FromResult(""test"");
        public Dictionary<Entity, int> EntityMap { get; set; }
    }
}

namespace MyApp.Models
{
    public class User { }
}

namespace MyApp.Data
{
    public class Entity { }
}";

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();
        }

        [Fact]
        public void TypeofExpressions_UsingFormatForTypeof_CompileSuccessfully()
        {
            // Arrange
            var genericTypes = new[]
            {
                ("List`1", "List<>"),
                ("Dictionary`2", "Dictionary<,>"),
                ("Func`3", "Func<,,>"),
                ("Action`1", "Action<>"),
                ("Tuple`4", "Tuple<,,,>")
            };

            var code = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class TypeofTest
    {
        public void TestTypeofExpressions()
        {
";
            foreach (var (clrType, _) in genericTypes)
            {
                var formatted = _helper.FormatForTypeof(clrType);
                code += $"            var type{clrType.Replace("`", "")} = typeof({formatted});\n";
            }

            // Add some non-generic types
            var nonGenericTypes = new[] { "System.String", "System.Int32", "MyClass" };
            foreach (var type in nonGenericTypes)
            {
                var formatted = _helper.FormatForTypeof(type);
                code += $"            var type{type.Replace(".", "")} = typeof({formatted});\n";
            }

            code += @"
        }
    }
    
    public class MyClass { }
}";

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            foreach (var error in errors)
            {
                output.WriteLine($"Compilation error: {error.GetMessage()}");
            }
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();
        }

        [Fact]
        public void ComplexNestedGenerics_CompileSuccessfully()
        {
            // Arrange
            var code = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class ComplexGenericsTest
    {
        // Nested generics
        public List<Dictionary<string, List<int>>> NestedCollection { get; set; }
        
        // Task with generic result
        public Task<List<Dictionary<string, object>>> GetComplexDataAsync()
        {
            return Task.FromResult(new List<Dictionary<string, object>>());
        }
        
        // Func with multiple parameters
        public Func<string, int, Task<bool>> ComplexFunc { get; set; }
        
        // Nullable generics
        public List<int?> NullableList { get; set; }
        
        // Array of generics
        public Dictionary<string, int>[] ArrayOfDictionaries { get; set; }
    }
}";

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();
        }

        [Fact]
        public void EscapedKeywords_AsIdentifiers_CompileSuccessfully()
        {
            // Arrange
            var keywords = new[] { "class", "event", "delegate", "interface", "namespace", "void", "async", "await" };

            var code = @"
using System;

namespace TestNamespace
{
    public enum MyEnum
    {
";
            foreach (var keyword in keywords)
            {
                var escaped = _helper.EscapeIdentifier(keyword);
                code += $"        {escaped},\n";
            }

            code += @"
    }
    
    public class TestClass
    {
";
            foreach (var keyword in keywords)
            {
                var escaped = _helper.EscapeIdentifier(keyword);
                code += $"        public string {escaped} {{ get; set; }}\n";
            }

            code += @"
        public void TestMethod()
        {
";
            foreach (var keyword in keywords)
            {
                var escaped = _helper.EscapeIdentifier(keyword);
                code += $"            var {escaped} = MyEnum.{escaped};\n";
            }

            code += @"
        }
    }
}";

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();
        }

        [Fact]
        public void GlobalPrefixedTypes_CompileSuccessfully()
        {
            // Arrange
            var code = @"
namespace System
{
    // Ambiguous type name
    public class MyType { }
}

namespace MyApp
{
    // Another ambiguous type
    public class MyType { }
    
    public class TestClass
    {
        // Using global:: to disambiguate
        public global::System.MyType SystemType { get; set; }
        public global::MyApp.MyType AppType { get; set; }
        
        // Generic with global prefix
        public global::System.Collections.Generic.List<global::MyApp.MyType> TypeList { get; set; }
    }
}";

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();
        }

        [Fact]
        public void NestedClassReferences_CompileSuccessfully()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;

namespace TestNamespace
{
    public class OuterClass
    {
        public class MiddleClass
        {
            public class InnerClass
            {
                public string Value { get; set; }
            }
        }
        
        public enum NestedEnum
        {
            Value1,
            Value2
        }
    }
    
    public class TestClass
    {
        // Reference nested classes
        public OuterClass.MiddleClass.InnerClass NestedField { get; set; }
        public OuterClass.NestedEnum EnumField { get; set; }
        
        // In methods
        public OuterClass.MiddleClass.InnerClass GetNested()
        {
            return new OuterClass.MiddleClass.InnerClass { Value = ""test"" };
        }
        
        // Generic with nested type
        public List<OuterClass.MiddleClass.InnerClass> NestedList { get; set; }
    }
}
";

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();

            // Verify the types exist
            var testType = asm.GetType("TestNamespace.TestClass");
            testType.ShouldNotBeNull();

            var nestedProperty = testType.GetProperty("NestedField");
            nestedProperty.ShouldNotBeNull();
            nestedProperty.PropertyType.Name.ShouldBe("InnerClass");
        }

        [Fact]
        public void ArrayAndNullableTypes_CompileSuccessfully()
        {
            // Arrange
            var code = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class ArrayAndNullableTest
    {
        // Simple arrays
        public string[] StringArray { get; set; }
        public int[] IntArray { get; set; }
        
        // Multi-dimensional arrays
        public string[,] TwoDimArray { get; set; }
        public int[,,] ThreeDimArray { get; set; }
        
        // Jagged arrays
        public string[][] JaggedArray { get; set; }
        public int[][][] TripleJaggedArray { get; set; }
        
        // Nullable value types
        public int? NullableInt { get; set; }
        public DateTime? NullableDate { get; set; }
        public bool? NullableBool { get; set; }
        
        // Arrays of nullables
        public int?[] NullableIntArray { get; set; }
        
        // Generics with arrays
        public List<string[]> ListOfArrays { get; set; }
        public Dictionary<string, int[]> DictOfArrays { get; set; }
        
        // Complex combinations
        public List<int?>[] ArrayOfListsOfNullables { get; set; }
    }
}";

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();
        }

        [Fact]
        public void TupleTypes_CompileSuccessfully()
        {
            // Arrange
            var code = @"
using System;

namespace TestNamespace
{
    public class TupleTest
    {
        // ValueTuple syntax
        public (string Name, int Age) GetPerson()
        {
            return (""John"", 30);
        }
        
        // Tuple class
        public Tuple<string, int> OldStyleTuple { get; set; }
        
        // Nested tuples
        public (string, (int, bool)) NestedTuple { get; set; }
        
        // In generics
        public System.Collections.Generic.List<(string Key, int Value)> TupleList { get; set; }
        
        // Multiple elements
        public (string, int, bool, DateTime, decimal) FiveElementTuple { get; set; }
        
        // As method parameter
        public void ProcessTuple((string Name, int Count) data)
        {
            var name = data.Name;
            var count = data.Count;
        }
    }
}";

            // Act
            var (asm, diags, _) = CompileAndRunGenerator(new[] { code }, new StateMachineGenerator());

            // Assert
            var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            errors.ShouldBeEmpty();
            asm.ShouldNotBeNull();
        }
    }
}