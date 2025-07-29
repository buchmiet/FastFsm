using Generator.FeatureDetection;
using Generator.Model;
using System;
using System.Collections.Generic;
using System.IO;

Console.WriteLine("=== Milestone 2 Testing - CoreFeature ===\n");

// Test tylko Pure i Basic wariantów (które używają nowego CoreFeature)
TestVariant("Pure", GenerationVariant.Pure, false);
TestVariant("Basic", GenerationVariant.Basic, true);
TestVariant("Async Pure", GenerationVariant.Pure, false, isAsync: true);
TestVariant("Async Basic", GenerationVariant.Basic, true, isAsync: true);

// Test że inne warianty nadal działają przez legacy
Console.WriteLine("\n--- Legacy variants (should still work) ---");
TestVariant("WithPayload", GenerationVariant.WithPayload, true);
TestVariant("WithExtensions", GenerationVariant.WithExtensions, true);
TestVariant("Full", GenerationVariant.Full, true);

// Test AsyncPolicy
Console.WriteLine("\n=== AsyncPolicy Test ===");
TestAsyncPolicy();

Console.WriteLine("\n✨ Milestone 2 Testing Complete!");

void TestVariant(string name, GenerationVariant variant, bool hasOnEntryExit, bool isAsync = false)
{
    Console.WriteLine($"\n--- Testing {name} ---");

    var runner = new ParallelGeneratorRunner(enableModernGenerator: true);

    var model = new StateMachineModel
    {
        ClassName = "TestMachine",
        Namespace = "TestNamespace",
        StateType = "TestState",
        TriggerType = "TestTrigger",
        GenerationConfig = new GenerationConfig
        {
            Variant = variant,
            IsAsync = isAsync,
            HasOnEntryExit = hasOnEntryExit
        },
        States = new Dictionary<string, StateModel>
        {
            ["Off"] = new StateModel
            {
                Name = "Off",
                OnEntryMethod = hasOnEntryExit ? "OnOffEntry" : null,
                OnExitMethod = hasOnEntryExit ? "OnOffExit" : null
            },
            ["On"] = new StateModel
            {
                Name = "On",
                OnEntryMethod = hasOnEntryExit ? "OnOnEntry" : null,
                OnExitMethod = hasOnEntryExit ? "OnOnExit" : null
            }
        },
        Transitions = new List<TransitionModel>
        {
            new TransitionModel
            {
                FromState = "Off",
                Trigger = "TurnOn",
                ToState = "On",
                GuardMethod = "CanTurnOn",
                ActionMethod = "LogTransition"
            },
            new TransitionModel
            {
                FromState = "On",
                Trigger = "TurnOff",
                ToState = "Off"
            }
        },
        GenerateLogging = false,
        GenerateDependencyInjection = false,
        EmitStructuralHelpers = true,
        ContinueOnCapturedContext = false
    };

    var result = runner.GenerateBoth(model);
    var comparison = runner.Compare(result.LegacyCode, result.ModernCode);

    Console.WriteLine($"Legacy: {result.LegacyCode.Length} chars");
    Console.WriteLine($"Modern: {result.ModernCode?.Length ?? 0} chars");
    Console.WriteLine($"Status: {comparison.Status}");

    // Zapisuj pliki dla async lub gdy są różnice
    if (comparison.Status != ComparisonStatus.Identical || isAsync)
    {
        if (comparison.Status != ComparisonStatus.Identical)
        {
            Console.WriteLine("⚠️ Differences found!");
            Console.WriteLine(comparison.GetSummary());

            // Wypisz pierwsze różnice
            Console.WriteLine("\n=== First 1000 chars of legacy ===");
            Console.WriteLine(result.LegacyCode.Substring(0, Math.Min(1000, result.LegacyCode.Length)));
            Console.WriteLine("\n=== First 1000 chars of modern ===");
            Console.WriteLine(result.ModernCode?.Substring(0, Math.Min(1000, result.ModernCode?.Length ?? 0)) ?? "NULL");

            // Szukaj konkretnych różnic
            Console.WriteLine("\n=== Specific checks ===");
            Console.WriteLine($"Legacy has interface I{model.ClassName}: {result.LegacyCode.Contains($"interface I{model.ClassName}")}");
            Console.WriteLine($"Modern has interface I{model.ClassName}: {result.ModernCode?.Contains($"interface I{model.ClassName}") ?? false}");
            Console.WriteLine($"Legacy has CurrentState property: {result.LegacyCode.Contains("CurrentState =>")}");
            Console.WriteLine($"Modern has CurrentState property: {result.ModernCode?.Contains("CurrentState =>") ?? false}");
            Console.WriteLine($"Legacy has _currentState field: {result.LegacyCode.Contains("_currentState")}");
            Console.WriteLine($"Modern has _currentState field: {result.ModernCode?.Contains("_currentState") ?? false}");

            // Liczba metod
            var legacyMethods = System.Text.RegularExpressions.Regex.Matches(result.LegacyCode, @"(public|protected|private)\s+.*?\s+\w+\s*\(").Count;
            var modernMethods = System.Text.RegularExpressions.Regex.Matches(result.ModernCode ?? "", @"(public|protected|private)\s+.*?\s+\w+\s*\(").Count;
            Console.WriteLine($"Legacy method count: {legacyMethods}");
            Console.WriteLine($"Modern method count: {modernMethods}");
        }
        else
        {
            Console.WriteLine("✅ Identical! (but saving async files for inspection)");
        }

        // Zapisz pliki
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{name.Replace(" ", "_")}_{timestamp}";
        var currentDir = Directory.GetCurrentDirectory();
        var legacyPath = Path.Combine(currentDir, $"legacy_{fileName}.cs");
        var modernPath = Path.Combine(currentDir, $"modern_{fileName}.cs");

        File.WriteAllText(legacyPath, result.LegacyCode);
        if (result.ModernCode != null)
            File.WriteAllText(modernPath, result.ModernCode);

        Console.WriteLine($"\n📁 Saved files to:");
        Console.WriteLine($"   {legacyPath}");
        Console.WriteLine($"   {modernPath}");
    }
    else
    {
        Console.WriteLine("✅ Identical!");
    }
}

void TestAsyncPolicy()
{
    var syncPolicy = new Generator.ModernGeneration.Policies.AsyncPolicySync();
    var asyncPolicy = new Generator.ModernGeneration.Policies.AsyncPolicyAsync();

    // Test return types
    Console.WriteLine("\nReturn Type Transformations:");
    Console.WriteLine($"  Sync: bool -> {syncPolicy.ReturnType("bool")}");
    Console.WriteLine($"  Async: bool -> {asyncPolicy.ReturnType("bool")}");
    Console.WriteLine($"  Sync: void -> {syncPolicy.ReturnType("void")}");
    Console.WriteLine($"  Async: void -> {asyncPolicy.ReturnType("void")}");

    // Test method names
    Console.WriteLine("\nMethod Name Transformations:");
    Console.WriteLine($"  Sync: TryFire -> {syncPolicy.MethodName("TryFire")}");
    Console.WriteLine($"  Async: TryFire -> {asyncPolicy.MethodName("TryFire")}");
    Console.WriteLine($"  Async: FireAsync -> {asyncPolicy.MethodName("FireAsync")}");

    // Test keywords
    Console.WriteLine("\nKeywords:");
    Console.WriteLine($"  Sync async keyword: '{syncPolicy.AsyncKeyword()}'");
    Console.WriteLine($"  Async async keyword: '{asyncPolicy.AsyncKeyword()}'");
    Console.WriteLine($"  Sync await keyword: '{syncPolicy.AwaitKeyword(true)}'");
    Console.WriteLine($"  Async await keyword: '{asyncPolicy.AwaitKeyword(true)}'");

    // Test invocation
    Console.WriteLine("\nMethod Invocation:");
    var sb = new System.Text.StringBuilder();

    Console.WriteLine("  Sync call:");
    syncPolicy.EmitInvocation(sb, "DoWork", false, "arg1", "arg2");
    Console.WriteLine($"    {sb.ToString().Trim()}");

    sb.Clear();
    Console.WriteLine("  Async call:");
    asyncPolicy.EmitInvocation(sb, "DoWorkAsync", true, "arg1", "arg2");
    Console.WriteLine($"    {sb.ToString().Trim()}");
}