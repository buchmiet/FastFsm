using System.Threading.Tasks;
using Generator.Async.Tests.Analysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using System;
using Generator.Helpers;

namespace Generator.Async.Tests;

public class AsyncSignatureAnalyzerTests
{
    // Zamiast dziedziczyć, będziemy tworzyć instancję CSharpAnalyzerTest w każdej metodzie testowej.
    // To daje nam pełną kontrolę nad tym, co i jak jest testowane.

    private sealed class Test : CSharpAnalyzerTest<AnalyzerTestWrapper, XUnitVerifier>
    {
        private readonly Action<AsyncSignatureInfo> _assertion;

        public Test(string source, Action<AsyncSignatureInfo> assertion)
        {
            // Przechowujemy asercję do późniejszego użycia
            _assertion = assertion;

            // Konfiguracja, którą robiliśmy w konstruktorze klasy testowej
            TestCode = source;
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
            ExpectedDiagnostics.Clear();
        }

        // Framework wywoła tę metodę, aby uzyskać instancję analizatora.
        // Tutaj tworzymy nasz wrapper i przekazujemy mu callback z asercją.
        protected  DiagnosticAnalyzer CreateAnalyzer()
        {
            return new AnalyzerTestWrapper
            {
                OnResultCallback = _assertion
            };
        }
    }

    // --- Testy dla sygnatur równoważnych `void` ---

    [Fact]
    public Task Analyze_WithSyncVoidMethod_ReturnsCorrectInfo()
    {
        var source = "public class T { public void MethodToTest() {} }";

        var test = new Test(source, result =>
        {
            Assert.False(result.IsAsync);
            Assert.True(result.IsVoidEquivalent);
        });

        return test.RunAsync();
    }

    [Fact]
    public Task Analyze_WithAsyncTaskMethod_ReturnsCorrectInfo()
    {
        var source = "using System.Threading.Tasks; public class T { public async Task MethodToTest() => await Task.CompletedTask; }";

        var test = new Test(source, result =>
        {
            Assert.True(result.IsAsync);
            Assert.True(result.IsVoidEquivalent);
        });

        return test.RunAsync();
    }

    [Fact]
    public Task Analyze_WithAsyncValueTaskMethod_ReturnsCorrectInfo()
    {
        var source = "using System.Threading.Tasks; public class T { public async ValueTask MethodToTest() => await Task.CompletedTask; }";

        var test = new Test(source, result =>
        {
            Assert.True(result.IsAsync);
            Assert.True(result.IsVoidEquivalent);
        });

        return test.RunAsync();
    }

    // --- Testy dla sygnatur równoważnych `bool` ---

    [Fact]
    public Task Analyze_WithSyncBoolMethod_ReturnsCorrectInfo()
    {
        var source = "public class T { public bool MethodToTest() => true; }";

        var test = new Test(source, result =>
        {
            Assert.False(result.IsAsync);
            Assert.True(result.IsBoolEquivalent);
        });

        return test.RunAsync();
    }

    [Fact]
    public Task Analyze_WithAsyncValueTaskOfBoolMethod_ReturnsCorrectInfo()
    {
        var source = "using System.Threading.Tasks; public class T { public async ValueTask<bool> MethodToTest() => await Task.FromResult(true); }";

        var test = new Test(source, result =>
        {
            Assert.True(result.IsAsync);
            Assert.True(result.IsBoolEquivalent);
            Assert.False(result.IsInvalidGuardTask);
        });

        return test.RunAsync();
    }

    // --- Testy dla niepoprawnych sygnatur ---

    [Fact]
    public Task Analyze_WithInvalidAsyncVoidMethod_DetectsInvalidSignature()
    {
        var source = "using System.Threading.Tasks; public class T { public async void MethodToTest() => await Task.CompletedTask; }";

        var test = new Test(source, result =>
        {
            Assert.True(result.IsAsync);
            Assert.True(result.IsInvalidAsyncVoid);
        });

        return test.RunAsync();
    }

    [Fact]
    public Task Analyze_WithInvalidAsyncTaskOfBoolForGuard_DetectsInvalidSignature()
    {
        var source = "using System.Threading.Tasks; public class T { public async Task<bool> MethodToTest() => await Task.FromResult(true); }";

        var test = new Test(source, result =>
        {
            Assert.True(result.IsAsync);
            Assert.True(result.IsBoolEquivalent);
            Assert.True(result.IsInvalidGuardTask);
        });

        return test.RunAsync();
    }

    [Fact]
    public Task Analyze_WithUnsupportedReturnType_ReturnsDefault()
    {
        var source = "public class T { public int MethodToTest() => 42; }";

        var test = new Test(source, result =>
        {
            Assert.False(result.IsAsync);
            Assert.False(result.IsBoolEquivalent);
            Assert.False(result.IsVoidEquivalent);
        });

        return test.RunAsync();
    }
}