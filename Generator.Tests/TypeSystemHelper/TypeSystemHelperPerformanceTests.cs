using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.TypeSystemHelper;

public class TypeSystemHelperPerformanceTests(ITestOutputHelper output)
{
    private readonly Infrastructure.TypeSystemHelper _helper = new();

    [Fact]
    public void Performance_ProcessManyTypes_CompletesQuickly()
    {
        // Arrange - Generate many different type combinations
        var testTypes = GenerateTestTypes().ToList();
        output.WriteLine($"Testing with {testTypes.Count} different types");

        // Warmup
        foreach (var type in testTypes.Take(100))
        {
            _ = _helper.FormatTypeForUsage(type);
        }

        // Act - Measure processing time
        var sw = Stopwatch.StartNew();

        foreach (var type in testTypes)
        {
            _ = _helper.FormatTypeForUsage(type);
            _ = _helper.GetNamespace(type);
            _ = _helper.GetSimpleTypeName(type);
            _ = _helper.IsGenericType(type);
            _ = _helper.IsNestedType(type);
        }

        sw.Stop();

        // Assert
        var totalOperations = testTypes.Count * 5; // 5 operations per type
        var avgTimePerOperation = sw.Elapsed.TotalMilliseconds / totalOperations;

        output.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");
        output.WriteLine($"Total operations: {totalOperations}");
        output.WriteLine($"Average time per operation: {avgTimePerOperation:F4}ms");

        avgTimePerOperation.ShouldBeLessThan(0.1); // Less than 0.1ms per operation
    }

    [Fact]
    public void Performance_ComplexGenericTypes_ProcessedEfficiently()
    {
        // Arrange - Very complex nested generic types
        var complexTypes = new[]
        {
            "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Collections.Generic.List`1[[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib],[System.String, mscorlib]], mscorlib]], mscorlib]]",
            "System.Func`5[[System.String, mscorlib],[System.Int32, mscorlib],[System.Collections.Generic.List`1[[System.Boolean, mscorlib]], mscorlib],[System.Threading.Tasks.Task`1[[System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Object, mscorlib]], mscorlib]], mscorlib],[System.Threading.CancellationToken, mscorlib]]",
            "MyApp.Generic`3[[System.Collections.Generic.List`1[[System.String, mscorlib]], mscorlib],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib],[System.Collections.Generic.HashSet`1[[System.String, mscorlib]], mscorlib]], mscorlib],[System.Tuple`4[[System.String, mscorlib],[System.Int32, mscorlib],[System.Boolean, mscorlib],[System.DateTime, mscorlib]], mscorlib]]"
        };

        // Act
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 100; i++)
        {
            foreach (var complexType in complexTypes)
            {
                var formatted = _helper.FormatTypeForUsage(complexType);
                var namespaces = _helper.GetRequiredNamespaces(complexType).ToList();
            }
        }

        sw.Stop();

        // Assert
        output.WriteLine($"Processing {complexTypes.Length * 100} complex types took {sw.ElapsedMilliseconds}ms");
        var avgTime = sw.ElapsedMilliseconds / (double)(complexTypes.Length * 100);
        output.WriteLine($"Average time per complex type: {avgTime:F2}ms");

        avgTime.ShouldBeLessThan(5.0); // Even complex types should process in under 5ms
    }

    [Fact]
    public void Memory_NoMemoryLeaks_WhenProcessingManyTypes()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        output.WriteLine($"Initial memory: {initialMemory / 1024 / 1024}MB");

        // Act - Process many types multiple times
        for (int iteration = 0; iteration < 10; iteration++)
        {
            var types = GenerateTestTypes().Take(1000).ToList();

            foreach (var type in types)
            {
                _ = _helper.FormatTypeForUsage(type);
                _ = _helper.GetNamespace(type);
                _ = _helper.GetSimpleTypeName(type);
                _ = _helper.GetRequiredNamespaces(type).ToList();
            }

            if (iteration % 3 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        // Force final collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        output.WriteLine($"Final memory: {finalMemory / 1024 / 1024}MB");

        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / 1024.0 / 1024.0;
        output.WriteLine($"Memory increase: {memoryIncreaseMB:F2}MB");

        // Assert - Should not leak significant memory
        memoryIncreaseMB.ShouldBeLessThan(10.0); // Less than 10MB increase
    }

    [Fact]
    public void Performance_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var types = GenerateTestTypes().Take(100).ToList();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Process types from multiple threads
        var sw = Stopwatch.StartNew();

        System.Threading.Tasks.Parallel.ForEach(types, new System.Threading.Tasks.ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        }, type =>
        {
            try
            {
                var formatted = _helper.FormatTypeForUsage(type);
                var ns = _helper.GetNamespace(type);
                var simple = _helper.GetSimpleTypeName(type);
                var isGeneric = _helper.IsGenericType(type);
                var isNested = _helper.IsNestedType(type);

                results.Add($"{type} -> {formatted}");
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        sw.Stop();

        // Assert
        errors.ShouldBeEmpty();
        results.Count.ShouldBe(types.Count);

        output.WriteLine($"Processed {types.Count} types concurrently in {sw.ElapsedMilliseconds}ms");
        output.WriteLine($"Using {Environment.ProcessorCount} threads");
    }

    [Fact]
    public void Performance_CachingBehavior_ConsistentPerformance()
    {
        // Test if repeated processing of same types shows caching behavior
        var testType = "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Collections.Generic.List`1[[System.Int32, mscorlib]], mscorlib]]";

        // First run - cold
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = _helper.FormatTypeForUsage(testType);
        }
        sw1.Stop();

        // Second run - potentially cached
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = _helper.FormatTypeForUsage(testType);
        }
        sw2.Stop();

        output.WriteLine($"First run: {sw1.ElapsedMilliseconds}ms");
        output.WriteLine($"Second run: {sw2.ElapsedMilliseconds}ms");

        // Performance should be consistent (no significant degradation)
        var ratio = (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds;
        ratio.ShouldBeLessThan(2.0); // Second run should not be more than 2x slower
    }

    private IEnumerable<string> GenerateTestTypes()
    {
        var namespaces = new[] { "System", "System.Collections.Generic", "MyApp", "Company.Product.Module" };
        var simpleTypes = new[] { "String", "Int32", "Boolean", "DateTime", "Guid", "User", "Order", "Product" };
        var genericTypes = new[] { "List", "Dictionary", "HashSet", "Task", "Func", "Action", "Nullable" };

        // Simple types
        foreach (var ns in namespaces)
        {
            foreach (var type in simpleTypes)
            {
                yield return $"{ns}.{type}";
            }
        }

        // Generic types with one argument
        foreach (var ns in namespaces)
        {
            foreach (var generic in genericTypes.Take(4))
            {
                foreach (var arg in simpleTypes)
                {
                    yield return $"{ns}.{generic}`1[[{ns}.{arg}, mscorlib]]";
                    yield return $"{ns}.{generic}<{arg}>"; // Friendly format
                }
            }
        }

        // Nested types
        foreach (var ns in namespaces)
        {
            yield return $"{ns}.Outer+Inner";
            yield return $"{ns}.Outer+Middle+Inner";
            yield return $"{ns}.Class1+Class2+Class3+Class4";
        }

        // Complex generic combinations
        yield return "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Int32, mscorlib]]";
        yield return "System.Func`3[[System.String, mscorlib],[System.Int32, mscorlib],[System.Boolean, mscorlib]]";
        yield return "System.Threading.Tasks.Task`1[[System.Collections.Generic.List`1[[System.String, mscorlib]], mscorlib]]";

        // Arrays
        foreach (var type in simpleTypes)
        {
            yield return $"System.{type}[]";
            yield return $"System.{type}[,]";
            yield return $"System.{type}[][]";
        }
    }
}