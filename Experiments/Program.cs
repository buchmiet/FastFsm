using Generator.FeatureDetection;
using Generator.Model;
using System;
using System.Collections.Generic;
using System.IO;

Console.WriteLine("=== Milestone 3 Testing - Payload Support ===\n");

// Test Single Payload
TestSinglePayload();

// Test Multi Payload  
TestMultiPayload();

// Test Guard Policy combinations
TestGuardPolicyCombinations();

Console.WriteLine("\n✨ Milestone 3 Testing Complete!");

void TestSinglePayload()
{
    Console.WriteLine("\n--- Testing Single Payload ---");

    var runner = new ParallelGeneratorRunner(enableModernGenerator: true);

    var model = new StateMachineModel
    {
        ClassName = "SinglePayloadMachine",
        Namespace = "TestNamespace",
        StateType = "MachineState",
        TriggerType = "MachineTrigger",
        GenerationConfig = new GenerationConfig
        {
            Variant = GenerationVariant.WithPayload,
            IsAsync = false,
            HasPayload = true
        },
        DefaultPayloadType = "PayloadData", // Single payload type
        States = new Dictionary<string, StateModel>
        {
            ["Idle"] = new StateModel { Name = "Idle" },
            ["Processing"] = new StateModel
            {
                Name = "Processing",
                OnEntryMethod = "OnProcessingEntry",
                OnEntryExpectsPayload = true,
                OnEntryHasParameterlessOverload = false
            }
        },
        Transitions = new List<TransitionModel>
        {
            new TransitionModel
            {
                FromState = "Idle",
                Trigger = "StartProcessing",
                ToState = "Processing",
                GuardMethod = "CanStartProcessing",
                GuardExpectsPayload = true,
                GuardHasParameterlessOverload = true,
                ActionMethod = "DoProcess",
                ActionExpectsPayload = true
            }
        },
        EmitStructuralHelpers = true
    };

    var result = runner.GenerateBoth(model);
    var comparison = runner.Compare(result.LegacyCode, result.ModernCode);

    Console.WriteLine($"Legacy: {result.LegacyCode.Length} chars");
    Console.WriteLine($"Modern: {result.ModernCode?.Length ?? 0} chars");
    Console.WriteLine($"Status: {comparison.Status}");

    if (comparison.Status != ComparisonStatus.Identical)
    {
        Console.WriteLine("⚠️ Differences found!");
        SaveFiles("SinglePayload", result);
    }
    else
    {
        Console.WriteLine("✅ Identical!");
    }
}

void TestMultiPayload()
{
    Console.WriteLine("\n--- Testing Multi Payload ---");

    var runner = new ParallelGeneratorRunner(enableModernGenerator: true);

    var model = new StateMachineModel
    {
        ClassName = "MultiPayloadMachine",
        Namespace = "TestNamespace",
        StateType = "MachineState",
        TriggerType = "MachineTrigger",
        GenerationConfig = new GenerationConfig
        {
            Variant = GenerationVariant.WithPayload,
            IsAsync = true,
            HasPayload = true
        },
        TriggerPayloadTypes = new Dictionary<string, string>
        {
            ["SendMessage"] = "MessagePayload",
            ["ProcessData"] = "DataPayload",
            ["UpdateConfig"] = "ConfigPayload"
        },
        States = new Dictionary<string, StateModel>
        {
            ["Ready"] = new StateModel { Name = "Ready" },
            ["Sending"] = new StateModel { Name = "Sending" },
            ["Processing"] = new StateModel { Name = "Processing" },
            ["Configuring"] = new StateModel { Name = "Configuring" }
        },
        Transitions = new List<TransitionModel>
        {
            new TransitionModel
            {
                FromState = "Ready",
                Trigger = "SendMessage",
                ToState = "Sending",
                ExpectedPayloadType = "MessagePayload",
                GuardMethod = "CanSendMessage",
                GuardExpectsPayload = true,
                GuardIsAsync = true
            },
            new TransitionModel
            {
                FromState = "Ready",
                Trigger = "ProcessData",
                ToState = "Processing",
                ExpectedPayloadType = "DataPayload",
                ActionMethod = "StartProcessing",
                ActionExpectsPayload = true,
                ActionIsAsync = true
            }
        },
        EmitStructuralHelpers = true,
        ContinueOnCapturedContext = false
    };

    var result = runner.GenerateBoth(model);
    var comparison = runner.Compare(result.LegacyCode, result.ModernCode);

    Console.WriteLine($"Legacy: {result.LegacyCode.Length} chars");
    Console.WriteLine($"Modern: {result.ModernCode?.Length ?? 0} chars");
    Console.WriteLine($"Status: {comparison.Status}");

    if (comparison.Status != ComparisonStatus.Identical)
    {
        Console.WriteLine("⚠️ Differences found!");
        SaveFiles("MultiPayload", result);
    }
    else
    {
        Console.WriteLine("✅ Identical!");
    }
}

void TestGuardPolicyCombinations()
{
    Console.WriteLine("\n--- Testing Guard Policy Combinations ---");

    // Test wszystkich 8 kombinacji guardów
    var guardCombinations = new[]
    {
        (Name: "No payload, sync", ExpectsPayload: false, HasOverload: false, IsAsync: false),
        (Name: "No payload, async", ExpectsPayload: false, HasOverload: false, IsAsync: true),
        (Name: "Payload required, sync", ExpectsPayload: true, HasOverload: false, IsAsync: false),
        (Name: "Payload required, async", ExpectsPayload: true, HasOverload: false, IsAsync: true),
        (Name: "Payload + overload, sync", ExpectsPayload: true, HasOverload: true, IsAsync: false),
        (Name: "Payload + overload, async", ExpectsPayload: true, HasOverload: true, IsAsync: true),
    };

    foreach (var combo in guardCombinations)
    {
        Console.WriteLine($"\n  Testing: {combo.Name}");
        TestGuardCombination(combo.ExpectsPayload, combo.HasOverload, combo.IsAsync);
    }
}

void TestGuardCombination(bool expectsPayload, bool hasOverload, bool isAsync)
{
    var runner = new ParallelGeneratorRunner(enableModernGenerator: true);

    var model = new StateMachineModel
    {
        ClassName = "GuardTestMachine",
        Namespace = "TestNamespace",
        StateType = "TestState",
        TriggerType = "TestTrigger",
        GenerationConfig = new GenerationConfig
        {
            Variant = expectsPayload ? GenerationVariant.WithPayload : GenerationVariant.Basic,
            IsAsync = isAsync,
            HasPayload = expectsPayload
        },
        DefaultPayloadType = expectsPayload ? "TestPayload" : null,
        States = new Dictionary<string, StateModel>
        {
            ["StateA"] = new StateModel { Name = "StateA" },
            ["StateB"] = new StateModel { Name = "StateB" }
        },
        Transitions = new List<TransitionModel>
        {
            new TransitionModel
            {
                FromState = "StateA",
                Trigger = "GoToB",
                ToState = "StateB",
                GuardMethod = "TestGuard",
                GuardExpectsPayload = expectsPayload,
                GuardHasParameterlessOverload = hasOverload,
                GuardIsAsync = isAsync
            }
        }
    };

    GenerationResult result = runner.GenerateBoth(model);
    var comparison = runner.Compare(result.LegacyCode, result.ModernCode);

    Console.WriteLine($"    Status: {comparison.Status}");

    if (comparison.Status != ComparisonStatus.Identical)
    {
        var name = $"Guard_{(expectsPayload ? "P" : "NoP")}_{(hasOverload ? "O" : "NoO")}_{(isAsync ? "A" : "S")}";
        SaveFiles(name, result);
    }
}

void SaveFiles(string prefix, GenerationResult result)
{
    if (string.IsNullOrEmpty(result.ModernCode))
        Console.WriteLine("    ⚠️  Modern generator nic nie zwrócił – zapisuję tylko legacy");

    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var currentDir = Directory.GetCurrentDirectory();

    var legacyPath = Path.Combine(currentDir, $"{prefix}_legacy_{timestamp}.cs");
    File.WriteAllText(legacyPath, result.LegacyCode);

    if (!string.IsNullOrEmpty(result.ModernCode))
    {
        var modernPath = Path.Combine(currentDir, $"{prefix}_modern_{timestamp}.cs");
        File.WriteAllText(modernPath, result.ModernCode);
        Console.WriteLine($"    📁 Zapisano: {Path.GetFileName(legacyPath)} & {Path.GetFileName(modernPath)}");
    }
    else
    {
        Console.WriteLine($"    📁 Zapisano: {Path.GetFileName(legacyPath)}");
    }
}
