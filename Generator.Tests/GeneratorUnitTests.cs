using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;
using Shouldly;
using StateMachine.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public class StateMachineGeneratorUnitTests(ITestOutputHelper output):GeneratorBaseClass(output)
{
    private void OutputGeneratedCode(Dictionary<string, string> generatedSources, string filterKeyword)
    {
        foreach (var source in generatedSources.Where(kvp => kvp.Key.Contains(filterKeyword)))
        {
            output.WriteLine($"=== {source.Key} ===");
            output.WriteLine(source.Value);
            output.WriteLine("=== END ===\n");
        }
    }
    [Fact]
    public void Test_FullVariant_DefaultPayloadGeneration()
    {
        const string source = """
                              using Abstractions.Attributes;

                              namespace TestNamespace 
                              {
                                  public enum State { A, B }
                                  public enum Trigger { Go }
                              
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  [GenerationMode(GenerationMode.Full, Force = true)]
                                  public partial class FullDefaultPayload
                                  {
                                      [Transition(State.A, Trigger.Go, State.B)]
                                      private void Cfg() { }
                                  }
                              }
                              """;

        var (asm, diags, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        // --- Oczekujemy diagnostyki FSM007 ---------------------------------------
        var missingPayload = diags.SingleOrDefault(d =>
            d.Id == "FSM007" && d.Severity == DiagnosticSeverity.Error);

        missingPayload.ShouldNotBeNull();
        output.WriteLine($"✓ Caught expected diagnostic: {missingPayload.Id}");

        // --- Kod nie powinien być wygenerowany -----------------------------------
        generatedSources.ShouldBeEmpty();

        // Dalsza analiza wygenerowanego kodu nie dotyczy tego scenariusza
    }

    [Fact]
    public void Test_VariantSelector_ForcedFull_BugConfirmation()
    {
        // Test pokazujący że ForceMode=Full niepoprawnie ustawia HasPayload=true
        const string source = """
        using Abstractions.Attributes;
        
        namespace TestNamespace 
        {
            public enum State { A, B }
            public enum Trigger { Go }
            
            // Maszyna BEZ payloadów, ale z Force=true dla Full
            [StateMachine(typeof(State), typeof(Trigger))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class FullForcedNoPayload
            {
                [Transition(State.A, Trigger.Go, State.B)]
                private void Cfg() { }
            }
            
            // Dla porównania - WithExtensions bez payloadów
            [StateMachine(typeof(State), typeof(Trigger))]
            [GenerationMode(GenerationMode.WithExtensions, Force = true)]
            public partial class ExtensionsForcedNoPayload
            {
                [Transition(State.A, Trigger.Go, State.B)]
                private void Cfg() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        // Analiza FullForcedNoPayload
        output.WriteLine("=== FullForcedNoPayload ===");
        var fullCode = generatedSources
            .FirstOrDefault(kvp => kvp.Key.Contains("FullForcedNoPayload.Generated")).Value;

        if (fullCode != null)
        {
            // Sprawdź czy ma payload interface (nie powinien!)
            var hasPayloadInterface = fullCode.Contains("IStateMachineWithPayload");
            output.WriteLine($"Has IStateMachineWithPayload: {hasPayloadInterface}");

            if (hasPayloadInterface)
            {
                output.WriteLine("✗ BUG CONFIRMED: Force=Full incorrectly adds payload support");

                // Znajdź typ payloadu
                var match = System.Text.RegularExpressions.Regex.Match(
                    fullCode, @"IStateMachineWithPayload<[^,]+,\s*[^,]+,\s*([^>]+)>");
                if (match.Success)
                {
                    output.WriteLine($"  Incorrect payload type: {match.Groups[1].Value}");
                }
            }
        }

        // Analiza ExtensionsForcedNoPayload (powinno działać poprawnie)
        output.WriteLine("\n=== ExtensionsForcedNoPayload ===");
        var extCode = generatedSources
            .FirstOrDefault(kvp => kvp.Key.Contains("ExtensionsForcedNoPayload.Generated")).Value;

        if (extCode != null)
        {
            var hasPayloadInterface = extCode.Contains("IStateMachineWithPayload");
            output.WriteLine($"Has IStateMachineWithPayload: {hasPayloadInterface}");

            if (!hasPayloadInterface)
            {
                output.WriteLine("✓ WithExtensions correctly does not add payload support");
            }
        }
    }
    [Fact]
    public void OnEntry_WithoutPayload_IsCalledCorrectly_InFullVariant()
    {
        const string source = """
        using Abstractions.Attributes;
        using System;

        namespace TestNamespace 
        {
            public enum State   { Idle, Working }
            public enum Trigger { Start }
            
            [StateMachine(typeof(State), typeof(Trigger))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            [PayloadType(typeof(object))]                         // spełnia FSM007
            public partial class BasicOnEntryMachine
            {
                public static string Log { get; set; } = "";
                
                [Transition(State.Idle, Trigger.Start, State.Working)]
                private void Cfg() { }
                
                [State(State.Working, OnEntry = nameof(OnEnterWorking))]
                private void StateCfg() {}
                
                // Wersja OnEntry bez payloadu
                public void OnEnterWorking()
                {
                    Log += "EnteredWorking;";
                }
            }
        }
        """;

        // --- kompilacja + generator ---
        var (asm, diags, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        OutputGeneratedCode(generatedSources, "BasicOnEntryMachine");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(asm);

        // --- tworzenie instancji ---
        var machineType = asm.GetType("TestNamespace.BasicOnEntryMachine")!;
        var stateType = asm.GetType("TestNamespace.State")!;
        var triggerType = asm.GetType("TestNamespace.Trigger")!;

        // Konstruktor: (State initialState, IEnumerable<IStateMachineExtension>? extensions = null)
        var machine = Activator.CreateInstance(
            machineType,
            Enum.Parse(stateType, "Idle"),   // initialState
            null                             // extensions
        )!;

        // --- wywołanie TryFire bez payloadu ---
        var tryFireMethod = machineType.GetMethod(
            "TryFire",
            new[] { triggerType, typeof(object) })!;

        tryFireMethod.Invoke(machine, new object?[]
        {
        Enum.Parse(triggerType, "Start"),
        null                                       // brak payloadu
        });

        // --- weryfikacja, że OnEnterWorking() zadziałało ---
        var logValue = (string?)machineType.GetProperty("Log")!.GetValue(null);
        Assert.Equal("EnteredWorking;", logValue);
    }

    [Fact]
    public void Fails_With_FSM007_When_FullVariantForced_WithoutPayload()
    {
        // ARRANGE: Konfiguracja, która jest teraz celowo nieprawidłowa
        const string source = """
                              using Abstractions.Attributes;

                              namespace TestNamespace 
                              {
                                  public enum State   { Idle, Working }
                                  public enum Trigger { Start }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  [GenerationMode(GenerationMode.Full, Force = true)] // Wymuszamy Full...
                                  public partial class InvalidMachine // ...ale nie dajemy PayloadType
                                  {
                                      [Transition(State.Idle, Trigger.Start, State.Working)]
                                      private void Cfg() { }
                                  }
                              }
                              """;

        // ACT
        var (_, diags, _) =
            CompileAndRunGenerator(new[] { source }, new StateMachineGenerator());

        // ASSERT: Sprawdzamy, czy pojawił się DOKŁADNIE jeden błąd i czy jest to FSM007
        var errorDiags = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Single(errorDiags);
        Assert.Equal("FSM007", errorDiags[0].Id);
    }

    [Fact]
    public void Test2_OnEntryWithSinglePayload()
    {
        // Test dla wariantu z jednym typem payloadu
        const string source = """
                              using Abstractions.Attributes;

                              namespace TestNamespace 
                              {
                                  public enum State { Idle, Working }
                                  public enum Trigger { Start }
                                  public class Config { public string Value { get; set; } }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  [PayloadType(typeof(Config))]  // Single payload dla całej maszyny
                                  [GenerationMode(GenerationMode.Full, Force = true)]
                                  public partial class SinglePayloadOnEntryMachine
                                  {
                                      public static string Log { get; set; } = "";
                                      
                                      [Transition(State.Idle, Trigger.Start, State.Working)]
                                      private void Cfg() { }
                                      
                                      [State(State.Working, OnEntry = nameof(OnEnterWorking))]
                                      private void StateCfg() {}
                                      
                                      public void OnEnterWorking(Config config) 
                                      {
                                          Log += $"Entered:{config.Value};";
                                      }
                                  }
                              }
                              """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        output.WriteLine("=== GENERATED CODE ===");
        OutputGeneratedCode(generatedSources, "SinglePayloadOnEntryMachine");

        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        foreach (var error in errors)
        {
            output.WriteLine($"ERROR: {error}");
        }
        Assert.Empty(errors);
    }

    [Fact]
    public void Test3_IsolateMultiPayloadProblem()
    {
        // Minimalny test pokazujący problem
        const string source = """
                              using Abstractions.Attributes;

                              namespace TestNamespace 
                              {
                                  public enum State { A, B }
                                  public enum Trigger { T1 }
                                  public class Payload1 { public int X { get; set; } }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  [PayloadType(Trigger.T1, typeof(Payload1))]  // Multi-payload
                                  [GenerationMode(GenerationMode.Full, Force = true)]
                                  public partial class MinimalMultiPayloadMachine
                                  {
                                      [Transition(State.A, Trigger.T1, State.B)]
                                      private void Cfg() { }
                                      
                                      [State(State.B, OnEntry = nameof(OnEnterB))]
                                      private void StateCfg() {}
                                      
                                      public void OnEnterB(Payload1 p) { }
                                  }
                              }
                              """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        // Wypisz fragment kodu gdzie następuje wywołanie OnEntry
        output.WriteLine("=== SEARCHING FOR OnEnterB CALL ===");
        var generatedCode = generatedSources.Values.First();
        var lines = generatedCode.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("OnEnterB"))
            {
                output.WriteLine($"Line {i}: {lines[i]}");
                // Pokaż też kilka linii kontekstu
                for (int j = Math.Max(0, i - 3); j <= Math.Min(lines.Length - 1, i + 3); j++)
                {
                    if (j != i) output.WriteLine($"Line {j}: {lines[j]}");
                }
            }
        }

        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
    }
    [Fact]
    public void FullVariant_MultiPayload_OnEntryWithPayload_ShowsGeneratedCode()
    {
        // Arrange – wariant Full + pojedynczy payload + dwa przeciążenia OnEntry
        const string source = """
        using Abstractions.Attributes;
        using System;
        
        namespace TestNamespace 
        {
            public enum State   { Idle, Processing, Done }
            public enum Trigger { Start, Finish }

            public class StartPayload { public string Config { get; set; } }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(Trigger.Start, typeof(StartPayload))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class OnEntryMultiPayloadMachine
            {
                public static string Log { get; set; } = "";
                
                [Transition(State.Idle, Trigger.Start, State.Processing)]
                private void Cfg1() { }

                [State(State.Processing, OnEntry = nameof(OnEnterProcessing))]
                private void StateCfg() { }
                
                // Prawidłowe OnEntry – z payloadem
                public void OnEnterProcessing(StartPayload payload)
                {
                    Log += $"EnterWithPayload:{payload.Config};";
                }

                // „Fałszywe” przeciążenie bez payloadu
                public void OnEnterProcessing()
                {
                    Log += "EnterWithoutPayload;";
                }
            }
        }
        """;

        // Act – kompilacja i uruchomienie generatora
        var (asm, diags, generatedSources) =
            CompileAndRunGenerator(new[] { source }, new StateMachineGenerator());

        // --- diagnostyka ---
        output.WriteLine("--- DIAGNOSTICS ---");
        foreach (var d in diags.Where(d => d.Severity == DiagnosticSeverity.Error))
            output.WriteLine($"ERROR: {d.Id} - {d.GetMessage()} at {d.Location}");
        output.WriteLine("-------------------");

        OutputGeneratedCode(generatedSources, "OnEntryMultiPayloadMachine");

        // --- weryfikacja, że kompilacja bezbłędna ---
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(asm);

        // --- refleksja nad wygenerowaną maszyną ---
        var machineType = asm.GetType("TestNamespace.OnEntryMultiPayloadMachine")!;
        var stateType = asm.GetType("TestNamespace.State")!;
        var triggerType = asm.GetType("TestNamespace.Trigger")!;
        var payloadType = asm.GetType("TestNamespace.StartPayload")!;

        var logProp = machineType.GetProperty("Log", BindingFlags.Public | BindingFlags.Static)!;
        // Full-variant posiada TryFire(Trigger, object)
        var tryFireMethod = machineType.GetMethod("TryFire", new[] { triggerType, typeof(object) })!;

        // Utwórz instancję (konstruktor: State, IEnumerable<IStateMachineExtension>? )
        var machineInstance = Activator.CreateInstance(
            machineType,
            Enum.Parse(stateType, "Idle"),   // initialState
            null                             // extensions
        )!;

        // Przygotuj payload
        var payloadInstance = Activator.CreateInstance(payloadType)!;
        payloadType.GetProperty("Config")!.SetValue(payloadInstance, "FAST");

        // Wywołaj przejście Idle → Processing
        var triggerStart = Enum.Parse(triggerType, "Start");
        tryFireMethod.Invoke(machineInstance, new[] { triggerStart, payloadInstance });

        // --- końcowa asercja ---
        var logValue = (string?)logProp.GetValue(null);
        output.WriteLine($"Final Log Value: '{logValue}'");

        // Jeśli generator użyje złego przeciążenia OnEnterProcessing(), w logu będzie "EnterWithoutPayload;"
        Assert.Equal("EnterWithPayload:FAST;", logValue);
    }


    [Fact]
    public void FullVariant_MultiPayload_OnEntryWithPayload_FailsToGenerateCorrectCall()
    {
        // Arrange ­– maszyna łącząca wariant Full + multi-payload + OnEntry z payloadem
        const string source = """
        using Abstractions.Attributes;
        using System;

        namespace TestNamespace 
        {
            public enum State   { Idle, Processing, Done }
            public enum Trigger { Start, Finish }

            public class StartPayload  { public string Config  { get; set; } }
            public class FinishPayload { public bool   Success { get; set; } }

            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(Trigger.Start,  typeof(StartPayload))]
            [PayloadType(Trigger.Finish, typeof(FinishPayload))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class OnEntryMultiPayloadMachine
            {
                public static string Log { get; set; } = "";

                [Transition(State.Idle,        Trigger.Start,  State.Processing)]
                private void Cfg1() { }

                [Transition(State.Processing,  Trigger.Finish, State.Done)]
                private void Cfg2() { }

                // OnEntry oczekujące payloadu
                [State(State.Processing, OnEntry = nameof(OnEnterProcessing))]
                private void StateCfg() { }

                public void OnEnterProcessing(StartPayload payload)
                {
                    Log += $"Entered Processing with config: {payload.Config};";
                }
            }
        }
        """;

        // Act ­– uruchom generator
        var (asm, diags, generatedSources) =
            CompileAndRunGenerator(new[] { source }, new StateMachineGenerator());

        // --- diagnostyka ---
        output.WriteLine("--- DIAGNOSTICS ---");
        foreach (var d in diags) output.WriteLine(d.ToString());
        output.WriteLine("-------------------");
        OutputGeneratedCode(generatedSources, "OnEntryMultiPayloadMachine");

        // --- weryfikacja kompilacji ---
        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
        Assert.NotNull(asm);

        // --- refleksja nad wygenerowaną maszyną ---
        var machineType = asm.GetType("TestNamespace.OnEntryMultiPayloadMachine");
        Assert.NotNull(machineType);

        var stateType = asm.GetType("TestNamespace.State")!;
        var triggerType = asm.GetType("TestNamespace.Trigger")!;
        var payloadType = asm.GetType("TestNamespace.StartPayload")!;

        var logProp = machineType.GetProperty("Log", BindingFlags.Public | BindingFlags.Static)!;
        // Full-variant generuje jedną wersję TryFire(Trigger, object)
        var tryFireMethod = machineType.GetMethod("TryFire", new[] { triggerType, typeof(object) })!;

        // Utwórz instancję (konstruktor: State, IEnumerable<IStateMachineExtension>?  )
        var machineInstance = Activator.CreateInstance(
            machineType,
            Enum.Parse(stateType, "Idle"),   // initialState
            null                             // extensions
        )!;

        // Przygotuj payload
        var payloadInstance = Activator.CreateInstance(payloadType)!;
        payloadType.GetProperty("Config")!.SetValue(payloadInstance, "FAST_MODE");

        // Wywołaj przejście Idle → Processing
        var triggerStart = Enum.Parse(triggerType, "Start");
        tryFireMethod.Invoke(machineInstance, new[] { triggerStart, payloadInstance });

        // --- ostateczna asercja ---
        var logValue = (string?)logProp.GetValue(null);
        Assert.Equal("Entered Processing with config: FAST_MODE;", logValue);
    }

    [Fact]
    public void Test4_CheckTransitionMetadata()
    {
        // Test sprawdzający czy transition.ExpectedPayloadType jest poprawnie ustawiony
        const string source = """
                              using Abstractions.Attributes;

                              namespace TestNamespace 
                              {
                                  public enum State { A, B }
                                  public enum Trigger { T1 }
                                  public class MyPayload { }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  [PayloadType(Trigger.T1, typeof(MyPayload))]
                                  public partial class MetadataTestMachine
                                  {
                                      [Transition(State.A, Trigger.T1, State.B)]
                                      private void Cfg() { }
                                      
                                      [State(State.B, OnEntry = nameof(OnEnterB))]
                                      private void StateCfg() {}
                                      
                                      public void OnEnterB(MyPayload p) { }
                                  }
                              }
                              """;

        // Tu możemy dodać breakpoint w generatorze w metodzie WriteOnEntryCall
        // i sprawdzić wartości transition.ExpectedPayloadType
        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        output.WriteLine("Searching for payload type casting in generated code:");
        var code = generatedSources.Values.First();
        if (code.Contains("is MyPayload"))
        {
            output.WriteLine("✓ Found proper type check for MyPayload");
        }
        else if (code.Contains("OnEnterB(payload)"))
        {
            output.WriteLine("✗ Found direct object passing - this is the bug!");
        }
    }

    [Fact]
    public void Test_FullVariant_Constructor_Conflict()
    {
        // Test sprawdzający czy własny konstruktor powoduje problem
        const string source = """
                              using Abstractions.Attributes;
                              using System.Collections.Generic;
                              using StateMachine.Contracts;

                              namespace TestNamespace 
                              {
                                  public enum State { A, B }
                                  public enum Trigger { Go }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  [GenerationMode(GenerationMode.Full, Force = true)]
                                  public partial class MachineWithCustomConstructor
                                  {
                                      // Własny konstruktor
                                      public MachineWithCustomConstructor(State initialState, 
                                          IEnumerable<IStateMachineExtension>? extensions)
                                          : base(initialState)
                                      {
                                          // Puste ciało
                                      }
                                      
                                      [Transition(State.A, Trigger.Go, State.B)]
                                      private void Cfg() { }
                                  }
                              }
                              """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        output.WriteLine("=== GENERATED CODE ===");
        OutputGeneratedCode(generatedSources, "MachineWithCustomConstructor");

        output.WriteLine("=== DIAGNOSTICS ===");
        foreach (var d in diags)
        {
            output.WriteLine($"{d.Severity}: {d.GetMessage()}");
        }

        // Sprawdź czy są konflikty konstruktorów
        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error &&
                                      d.GetMessage().Contains("already defines")).ToList();

        if (errors.Any())
        {
            output.WriteLine("✗ Constructor conflict detected!");
        }
    }


  
    [Fact]
    public void Test_FullVariant_Classification_Fix()
    {
        // Test pokazujący jak powinna wyglądać poprawna klasyfikacja
        const string source = """
        using Abstractions.Attributes;
        
        namespace TestNamespace 
        {
            public enum State { A, B }
            public enum Trigger { Go }
            
            // Przypadek 1: Full bez payloadów - powinien używać ExtensionsVariantGenerator
            [StateMachine(typeof(State), typeof(Trigger))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class FullNoPayload
            {
                [Transition(State.A, Trigger.Go, State.B)]
                private void Cfg() { }
            }
            
            // Przypadek 2: Full z payloadem - powinien używać FullVariantGenerator
            public class MyPayload { }
            
            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(MyPayload))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class FullWithPayload
            {
                [Transition(State.A, Trigger.Go, State.B)]
                private void Cfg() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        // Sprawdź FullNoPayload
        var noPayloadCode = generatedSources
            .Where(kvp => kvp.Key.Contains("FullNoPayload") && !kvp.Key.Contains("Factory"))
            .Select(kvp => kvp.Value)
            .FirstOrDefault();

        if (noPayloadCode != null)
        {
            output.WriteLine("=== FullNoPayload Analysis ===");

            // Nie powinien mieć IStateMachineWithPayload
            if (noPayloadCode.Contains("IStateMachineWithPayload"))
            {
                output.WriteLine("✗ BUG: Has IStateMachineWithPayload interface");
            }
            else
            {
                output.WriteLine("✓ Correctly does not have IStateMachineWithPayload");
            }

            // Powinien mieć tylko IExtensibleStateMachine
            if (noPayloadCode.Contains("IExtensibleStateMachine<State, Trigger>") &&
                !noPayloadCode.Contains("IStateMachineWithPayload"))
            {
                output.WriteLine("✓ Correctly implements only IExtensibleStateMachine");
            }

            // Nie powinien mieć _payloadMap
            if (noPayloadCode.Contains("_payloadMap"))
            {
                output.WriteLine("✗ BUG: Has _payloadMap field");
            }
            else
            {
                output.WriteLine("✓ Correctly does not have _payloadMap");
            }
        }

        // Sprawdź FullWithPayload
        var withPayloadCode = generatedSources
            .Where(kvp => kvp.Key.Contains("FullWithPayload") && !kvp.Key.Contains("Factory"))
            .Select(kvp => kvp.Value)
            .FirstOrDefault();

        if (withPayloadCode != null)
        {
            output.WriteLine("\n=== FullWithPayload Analysis ===");

            if (withPayloadCode.Contains("IStateMachineWithPayload<State, Trigger, MyPayload>"))
            {
                output.WriteLine("✓ Correctly has IStateMachineWithPayload<State, Trigger, MyPayload>");
            }
        }
    }

    [Fact]
    public void Test_IsSinglePayloadVariant_Logic()
    {
        const string source = """
                              using Abstractions.Attributes;

                              namespace TestNamespace 
                              {
                                  public enum State { A }
                                  public enum Trigger { T }
                                  
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  [GenerationMode(GenerationMode.Full, Force = true)]
                                  public partial class TestMachine { }
                              }
                              """;

        var (asm, diags, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        // --- Oczekujemy diagnostyki FSM007 ---------------------------------------
        var missingPayload = diags.SingleOrDefault(d =>
            d.Id == "FSM007" && d.Severity == DiagnosticSeverity.Error);

        missingPayload.ShouldNotBeNull();
        output.WriteLine($"✓ Caught expected diagnostic: {missingPayload.Id}");

        // --- Kod nie powinien być wygenerowany -----------------------------------
        generatedSources.ShouldBeEmpty();

        // Dalsza analiza wygenerowanego kodu nie ma sensu w tym scenariuszu
    }


    [Fact]
    public void Test_PayloadTypeDetection()
    {
        // Test sprawdzający jak generator określa typ payloadu
        const string source = """
        using Abstractions.Attributes;
        
        namespace TestNamespace 
        {
            public enum State { A, B }
            public enum Trigger { T1, T2 }
            
            // Przypadek 1: Brak payloadów
            [StateMachine(typeof(State), typeof(Trigger))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class NoPayloadMachine
            {
                [Transition(State.A, Trigger.T1, State.B)]
                private void Cfg() { }
            }
            
            // Przypadek 2: Single payload
            public class Payload1 { }
            
            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(typeof(Payload1))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class SinglePayloadMachine
            {
                [Transition(State.A, Trigger.T1, State.B)]
                private void Cfg() { }
            }
            
            // Przypadek 3: Multi payload
            public class Payload2 { }
            
            [StateMachine(typeof(State), typeof(Trigger))]
            [PayloadType(Trigger.T1, typeof(Payload1))]
            [PayloadType(Trigger.T2, typeof(Payload2))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class MultiPayloadMachine
            {
                [Transition(State.A, Trigger.T1, State.B)]
                [Transition(State.A, Trigger.T2, State.B)]
                private void Cfg() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        foreach (var kvp in generatedSources)
        {
            if (kvp.Key.Contains("NoPayloadMachine") && !kvp.Key.Contains("Factory"))
            {
                output.WriteLine("=== NoPayloadMachine ===");
                var hasPayloadInterface = kvp.Value.Contains("IStateMachineWithPayload");
                output.WriteLine($"Has IStateMachineWithPayload: {hasPayloadInterface}");

                if (hasPayloadInterface)
                {
                    output.WriteLine("✗ BUG: NoPayloadMachine should NOT implement IStateMachineWithPayload");
                }
            }
        }
    }
    [Fact]
    public void Test_VariantClassification_FullWithoutPayload()
    {
        // Maszyna Full bez payloadów nie powinna być traktowana jako WithPayload
        const string source = """
        using Abstractions.Attributes;
        
        namespace TestNamespace 
        {
            public enum State { A, B }
            public enum Trigger { Go }
            
            [StateMachine(typeof(State), typeof(Trigger))]
            [GenerationMode(GenerationMode.Full, Force = true)]
            public partial class FullNoPayloadMachine
            {
                [Transition(State.A, Trigger.Go, State.B)]
                private void Cfg() { }
            }
        }
        """;

        var (asm, diags, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        // oczekujemy FSM007 
        var missingPayload = diags.SingleOrDefault(d =>
            d is { Id: "FSM007", Severity: DiagnosticSeverity.Error });
        missingPayload.ShouldNotBeNull();
        output.WriteLine($"✓ Caught expected diagnostic: {missingPayload.Id}");

        // generator nie powinien nic wygenerować
        generatedSources.ShouldBeEmpty();

    }

    [Fact]
    public void Test_FullVariant_BasicOperation_NoExtensions()
    {
        const string source = """
                              using Abstractions.Attributes;
                              namespace TestNamespace 
                              {
                                  public enum State   { A, B }
                                  public enum Trigger { Go }
                                  
                                  public class MyPayload { public string Data { get; set; } }
                              
                                  [StateMachine(typeof(State), typeof(Trigger))]
                                  [GenerationMode(GenerationMode.Full, Force = true)]
                                  [PayloadType(typeof(MyPayload))]               // Full wymaga payload
                                  public partial class SimpleFullMachine
                                  {
                                      [Transition(State.A, Trigger.Go, State.B)]
                                      private void Cfg() { }
                                  }
                              }
                              """;

        // --- kompilacja + generator ---
        var (asm, diags, generatedSources) =
            CompileAndRunGenerator(new[] { source }, new StateMachineGenerator());

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(asm);

        // --- tworzenie instancji ---
        var machineType = asm!.GetType("TestNamespace.SimpleFullMachine")!;
        var stateType = asm.GetType("TestNamespace.State")!;

        output.WriteLine("Creating machine instance...");

        // Konstruktor: (State initialState, IEnumerable<IStateMachineExtension>? extensions = null)
        var machine = Activator.CreateInstance(
            machineType,
            Enum.Parse(stateType, "A"),   // initialState
            null                          // extensions
        );

        output.WriteLine("Machine created successfully");
        Assert.NotNull(machine);

        // ­— weryfikacja interfejsów —
        var interfaces = machineType.GetInterfaces();
        Assert.Contains(interfaces, i => i.Name.Contains("IStateMachineWithPayload"));
        Assert.Contains(interfaces, i => i.Name.Contains("IExtensibleStateMachine"));
    }



    [Fact]
    public void FullVariant_WithMixedPayloadAndNoPayloadTransitions_GeneratesCorrectly()
    {
        // Arrange: Maszyna z wariantem Full, ale mieszanymi przejściami.
        // Jedno przejście używa payloadu, a drugie nie.
        const string source = """
                          using Abstractions.Attributes;
                          
                          namespace TestNamespace 
                          {
                              public enum State { A, B, C }
                              public enum Trigger { GoWithPayload, GoWithoutPayload }
                              public class MyPayload { public int Value { get; set; } }

                              [StateMachine(typeof(State), typeof(Trigger))]
                              [PayloadType(Trigger.GoWithPayload, typeof(MyPayload))] // Tylko jeden trigger ma payload
                              [GenerationMode(GenerationMode.Full, Force = true)]
                              public partial class MixedPayloadFullMachine
                              {
                                  // Przejście Z payloadem
                                  [Transition(State.A, Trigger.GoWithPayload, State.B, Action = nameof(ActionWithPayload))]
                                  
                                  // Przejście BEZ payloadu
                                  [Transition(State.B, Trigger.GoWithoutPayload, State.C, Action = nameof(ActionWithoutPayload))]
                                  private void Cfg() { }
                                  
                                  private void ActionWithPayload(MyPayload payload) { }
                                  private void ActionWithoutPayload() { }
                              }
                          }
                          """;

        // Act
        var (asm, diags, generatedSources) = CompileAndRunGenerator([source], new StateMachineGenerator());

        // Assert: Kompilacja musi przejść bez błędów.
        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
        Assert.NotNull(asm);

        // Zdobądź wygenerowany kod maszyny.
        var generatedCode = generatedSources["MixedPayloadFullMachine.Generated.cs"];
        Assert.NotNull(generatedCode);

        output.WriteLine("// ----- GENERATED CODE FOR MixedPayloadFullMachine -----");
        output.WriteLine(generatedCode);
        output.WriteLine("// ----------------------------------------------------");

        // Asercja 1: Sprawdź, czy kod dla przejścia Z PAYLOADEM jest poprawny
        Assert.Contains("case Trigger.GoWithPayload:", generatedCode);
        Assert.Contains("if (payload is MyPayload typedActionPayload)", generatedCode);
        Assert.Contains("ActionWithPayload(typedActionPayload);", generatedCode);

        // Asercja 2: Sprawdź, czy kod dla przejścia BEZ PAYLOADU jest poprawny
        Assert.Contains("case Trigger.GoWithoutPayload:", generatedCode);
        // W bloku dla GoWithoutPayload NIE POWINNO być próby rzutowania `payload`
        var goWithoutPayloadBlock = generatedCode.Substring(generatedCode.IndexOf("case Trigger.GoWithoutPayload:"));
        goWithoutPayloadBlock = goWithoutPayloadBlock.Substring(0, goWithoutPayloadBlock.IndexOf("break;"));

        Assert.DoesNotContain("if (payload is", goWithoutPayloadBlock);
        Assert.Contains("ActionWithoutPayload();", goWithoutPayloadBlock);
    }

    // ------------ TEST: Basic state machine generation (PURE variant by default) ------------------------------
    [Fact]
    public void Generator_should_compile_pure_state_machine_by_default()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum MyState   { Idle, Working, Done }
            public enum MyTrigger { Start, Complete, Reset }

            [StateMachine(typeof(MyState), typeof(MyTrigger))]
            public partial class PureMachineTest // Zmieniona nazwa klasy testowej
            {
                [Transition(MyState.Idle, MyTrigger.Start, MyState.Working)]
                [Transition(MyState.Working, MyTrigger.Complete, MyState.Done)]
                [Transition(MyState.Done, MyTrigger.Reset, MyState.Idle)]
                private void ConfigureTr() { } // Nazwa metody nie ma znaczenia
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([  userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "PureMachineTest");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm); // Dodatkowa asercja, że asm nie jest null

        Type? machine = asm!.GetType("TestNamespace.PureMachineTest"); // Użycie ! po Assert.NotNull(asm)
        Assert.NotNull(machine);
        Assert.True(machine!.BaseType!.Name.StartsWith("StateMachineBase"));

        Type? interfaceType = asm.GetType("TestNamespace.IPureMachineTest"); // Generator tworzy interfejs I{ClassName}
        Assert.NotNull(interfaceType);
        Assert.Contains(interfaceType, machine.GetInterfaces());

        Type enumState = asm.GetType("TestNamespace.MyState")!;
        ConstructorInfo? ctor = machine.GetConstructor([enumState]);
        Assert.NotNull(ctor);

        object instance = ctor!.Invoke([Enum.Parse(enumState, "Idle")]);

        Assert.NotNull(machine.GetMethod("TryFire", [asm.GetType("TestNamespace.MyTrigger")!, typeof(object)]));
        Assert.NotNull(machine.GetMethod("Fire", [asm.GetType("TestNamespace.MyTrigger")!, typeof(object)]));
        Assert.NotNull(machine.GetMethod("CanFire", [asm.GetType("TestNamespace.MyTrigger")!]));
        Assert.NotNull(machine.GetMethod("GetPermittedTriggers"));
    }

    // ------------ TEST: Guards functionality (PURE variant) ------------------------------
    [Fact]
    public void Generator_should_handle_guards_in_pure_variant()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B, C }
            public enum Trigger { Go, Back }

            [StateMachine(typeof(State), typeof(Trigger))] // Domyślnie Pure
            public partial class GuardedMachinePure
            {
                private bool _canGo = true;

                [Transition(State.A, Trigger.Go, State.B, Guard = nameof(CanGo))]
                [Transition(State.B, Trigger.Go, State.C, Guard = nameof(AlwaysFalse))]
                [Transition(State.B, Trigger.Back, State.A)]
                private void ConfigureTr() { }

                private bool CanGo() => _canGo;
                private bool AlwaysFalse() => false;
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "GuardedMachinePure");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.GuardedMachinePure");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");

        object stateA = Enum.Parse(stateType!, "A");
        object machine = Activator.CreateInstance(machineType!, stateA)!;

        MethodInfo tryFireMethod = machineType!.GetMethod("TryFire", [triggerType, typeof(object)])!;
        PropertyInfo currentStateProperty = machineType.GetProperty("CurrentState")!;

        object triggerGo = Enum.Parse(triggerType!, "Go");
        bool result = (bool)tryFireMethod.Invoke(machine, [triggerGo, null])!;
        Assert.True(result);
        Assert.Equal("B", currentStateProperty.GetValue(machine)!.ToString());

        result = (bool)tryFireMethod.Invoke(machine, [triggerGo, null])!;
        Assert.False(result);
        Assert.Equal("B", currentStateProperty.GetValue(machine)!.ToString());

        object triggerBack = Enum.Parse(triggerType!, "Back");
        result = (bool)tryFireMethod.Invoke(machine, [triggerBack, null])!;
        Assert.True(result);
        Assert.Equal("A", currentStateProperty.GetValue(machine)!.ToString());
    }



    [Fact]
    public void Generator_should_handle_state_callbacks_for_basic_variant()
    {
        const string userSource = """
    using Abstractions.Attributes;
    namespace TestNamespace {
        public enum State { InitialStateForCtorTest, Start, Middle, End } 
        public enum Trigger { Next }

        [StateMachine(typeof(State), typeof(Trigger))]
        public partial class CallbackMachineBasic
        {
            public string Log { get; set; } = ""; 

            [Transition(State.Start, Trigger.Next, State.Middle)]
            [Transition(State.Middle, Trigger.Next, State.End)]
            private void ConfigureTr() { }

            [State(State.InitialStateForCtorTest, OnEntry = nameof(OnEntryInitialCustom))] 
            [State(State.Start, OnExit = nameof(OnExitStartCustom))] 
            [State(State.Middle, OnEntry = nameof(OnEntryMiddleCustom), OnExit = nameof(OnExitMiddleCustom))]
            [State(State.End, OnEntry = nameof(OnEntryEndCustom))]
            private void StateCallbacks() { }

            private void OnEntryInitialCustom() => Log += "EntryInitial;";
            private void OnExitStartCustom() => Log += "ExitStart;";
            private void OnEntryMiddleCustom() => Log += "EntryMiddle;";
            private void OnExitMiddleCustom() => Log += "ExitMiddle;";
            private void OnEntryEndCustom() => Log += "EntryEnd;";
        }
    }
    """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "CallbackMachineBasic");

        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.CallbackMachineBasic");
        Assert.NotNull(machineType);

        Type? stateType = asm.GetType("TestNamespace.State");
        Assert.NotNull(stateType);

        Type? triggerType = asm.GetType("TestNamespace.Trigger");
        Assert.NotNull(triggerType);

        object initialStateForCtor = Enum.Parse(stateType!, "InitialStateForCtorTest");
        object machineInstance = Activator.CreateInstance(machineType!, initialStateForCtor)!;
        Assert.NotNull(machineInstance);

        PropertyInfo? logProperty = machineType!.GetProperty("Log");
        Assert.NotNull(logProperty);
        Assert.Equal("EntryInitial;", logProperty!.GetValue(machineInstance));

        Type? baseType = machineType.BaseType;
        Assert.NotNull(baseType);
        FieldInfo? currentStateField = baseType!.GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(currentStateField); // Ta asercja jest już obecna, nie trzeba jej zmieniać.

        object stateStart = Enum.Parse(stateType!, "Start");
        currentStateField!.SetValue(machineInstance, stateStart);
        output.WriteLine($"Manually set _currentState to: {stateStart}");

        PropertyInfo? currentStateProp = machineType.GetProperty("CurrentState");
        Assert.NotNull(currentStateProp);
        Assert.Equal(stateStart, currentStateProp!.GetValue(machineInstance));

        Assert.NotNull(logProperty);
        logProperty!.SetValue(machineInstance, "");


        MethodInfo? tryFireMethod = machineType.GetMethod("TryFire", [triggerType, typeof(object)]);
        Assert.NotNull(tryFireMethod);

        object triggerNext = Enum.Parse(triggerType!, "Next");

        bool transitionResult1 = (bool)tryFireMethod!.Invoke(machineInstance, [triggerNext, null])!;
        Assert.True(transitionResult1, "Transition Start -> Middle failed.");
        Assert.Equal(Enum.Parse(stateType!, "Middle"), currentStateProp.GetValue(machineInstance));
        Assert.Equal("ExitStart;EntryMiddle;", logProperty.GetValue(machineInstance));

        logProperty.SetValue(machineInstance, "");
        bool transitionResult2 = (bool)tryFireMethod.Invoke(machineInstance, [triggerNext, null])!;
        Assert.True(transitionResult2, "Transition Middle -> End failed.");
        Assert.Equal(Enum.Parse(stateType!, "End"), currentStateProp.GetValue(machineInstance));
        Assert.Equal("ExitMiddle;EntryEnd;", logProperty.GetValue(machineInstance));
    }

    // ------------ TEST: Internal transitions (BASIC variant, aby sprawdzić OnEntry/OnExit) ------------------------------
    [Fact]
    public void Generator_should_handle_internal_transitions_in_basic_variant()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Active }
            public enum Trigger { Update, Exit }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class InternalMachineBasic // Będzie Basic z powodu StateAttribute
            {
                public string Log { get; set; } = ""; 

                [InternalTransition(State.Active, Trigger.Update, nameof(HandleUpdate))]
                private void ConfigureTr() { }

                [State(State.Active, OnEntry = nameof(OnEnterActive), OnExit = nameof(OnExitActive))] // Wymusza Basic
                private void StateCfg() {}

                private void HandleUpdate() => Log += "Update;";
                private void OnEnterActive() => Log += "EnterActive;";
                private void OnExitActive() => Log += "ExitActive;"; 
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "InternalMachineBasic");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.InternalMachineBasic");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");

        object stateActive = Enum.Parse(stateType!, "Active");
        object machine = Activator.CreateInstance(machineType!, stateActive)!;

        MethodInfo tryFireMethod = machineType!.GetMethod("TryFire", [triggerType, typeof(object)])!;
        PropertyInfo logProperty = machineType.GetProperty("Log")!;
        PropertyInfo currentStateProperty = machineType.GetProperty("CurrentState")!;

        Assert.Equal("EnterActive;", logProperty.GetValue(machine));
        logProperty.SetValue(machine, "");

        object triggerUpdate = Enum.Parse(triggerType!, "Update");
        bool result = (bool)tryFireMethod.Invoke(machine, [triggerUpdate, null])!;
        Assert.True(result);
        Assert.Equal("Update;", logProperty.GetValue(machine));
        Assert.Equal("Active", currentStateProperty.GetValue(machine)!.ToString());
    }

    [Fact]
    public void Generator_should_implement_GetPermittedTriggers_correctly()
    {
        const string userSource = """
    using Abstractions.Attributes;
    namespace TestNamespace {
        public enum State { A, B, C_NoOut } 
        public enum Trigger { X, Y, Z }

        [StateMachine(typeof(State), typeof(Trigger))]
        public partial class PermittedTriggersMachine
        {
            [Transition(State.A, Trigger.X, State.B)]
            [Transition(State.A, Trigger.Y, State.C_NoOut)]
            [Transition(State.B, Trigger.Z, State.A)]
            private void ConfigureTr() { }
        }
    }
    """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "PermittedTriggersMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.PermittedTriggersMachine");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Assert.NotNull(stateType);
        Type? triggerType = asm.GetType("TestNamespace.Trigger");
        Assert.NotNull(triggerType);

        object stateA = Enum.Parse(stateType!, "A");
        object stateCNoOut = Enum.Parse(stateType!, "C_NoOut");
        object machine = Activator.CreateInstance(machineType!, stateA)!;

        MethodInfo getPermittedTriggersMethod = machineType!.GetMethod("GetPermittedTriggers")!;

        var permittedInA = getPermittedTriggersMethod.Invoke(machine, null) as System.Collections.IEnumerable;
        Assert.NotNull(permittedInA);
        var triggersInA = new List<string>();
        foreach (var t in permittedInA!) triggersInA.Add(t.ToString()!);
        triggersInA.Sort();

        Assert.Equal(2, triggersInA.Count);
        Assert.Equal("X", triggersInA[0]);
        Assert.Equal("Y", triggersInA[1]);

        Type? baseType = machineType.BaseType;
        Assert.NotNull(baseType);
        FieldInfo? currentStateField = baseType!.GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(currentStateField);

        currentStateField!.SetValue(machine, stateCNoOut);
        output.WriteLine($"Manually set _currentState to: {stateCNoOut}");

        PropertyInfo? currentStateProp = machineType.GetProperty("CurrentState");
        Assert.NotNull(currentStateProp);
        Assert.Equal(stateCNoOut, currentStateProp!.GetValue(machine));

        var permittedInC = getPermittedTriggersMethod.Invoke(machine, null) as System.Collections.IEnumerable;
        Assert.NotNull(permittedInC);
        var triggersInC = new List<string>();
        foreach (var t in permittedInC!) triggersInC.Add(t.ToString()!);
        Assert.Empty(triggersInC);
    }

    // ------------ TEST: Pure variant generation when forced ------------------------------
    [Fact]
    public void Generator_should_create_pure_variant_when_forced_and_ignore_state_callbacks()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B }
            public enum Trigger { Go }

            [StateMachine(typeof(State), typeof(Trigger))]
            [GenerationMode(GenerationMode.Pure, Force = true)] // Wymuszenie Pure
            public partial class ForcedPureMachineTest
            {
                public string Log { get; set; } = "";

                [Transition(State.A, Trigger.Go, State.B)]
                private void ConfigureTr() { }

                [State(State.A, OnEntry = nameof(OnEnterA))] // To powinno być zignorowane
                private void StateCfg() {}
                
                private void OnEnterA() => Log += "EnterA;"; 
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "ForcedPureMachineTest");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.ForcedPureMachineTest");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");

        object stateA = Enum.Parse(stateType!, "A");
        object machine = Activator.CreateInstance(machineType!, stateA)!;

        PropertyInfo logProperty = machineType!.GetProperty("Log")!;
        Assert.Equal("", logProperty.GetValue(machine));

        MethodInfo tryFireMethod = machineType.GetMethod("TryFire", [triggerType, typeof(object)])!;
        object triggerGo = Enum.Parse(triggerType!, "Go");
        tryFireMethod.Invoke(machine, [triggerGo, null]);

        Assert.Equal("", logProperty.GetValue(machine));
    }


    // ------------ TEST: Complex scenario with actions (PURE variant) ------------------------------
    [Fact]
    public void Generator_should_handle_transition_actions_in_pure_variant()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { Idle, Processing, Complete }
            public enum Trigger { Start, Finish }

            [StateMachine(typeof(State), typeof(Trigger))] // Domyślnie Pure
            public partial class ActionMachinePure
            {
                public string LastAction { get; private set; } = "";

                [Transition(State.Idle, Trigger.Start, State.Processing, Action = nameof(OnStart))]
                [Transition(State.Processing, Trigger.Finish, State.Complete, Action = nameof(OnFinish))]
                private void ConfigureTr() { }

                private void OnStart() => LastAction = "Started";
                private void OnFinish() => LastAction = "Finished";
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "ActionMachinePure");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.ActionMachinePure");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");

        object stateIdle = Enum.Parse(stateType!, "Idle");
        object machine = Activator.CreateInstance(machineType!, stateIdle)!;

        MethodInfo tryFireMethod = machineType!.GetMethod("TryFire", [triggerType, typeof(object)])!;
        PropertyInfo lastActionProperty = machineType.GetProperty("LastAction")!;

        object triggerStart = Enum.Parse(triggerType!, "Start");
        object triggerFinish = Enum.Parse(triggerType!, "Finish");

        tryFireMethod.Invoke(machine, [triggerStart, null]);
        Assert.Equal("Started", lastActionProperty.GetValue(machine));

        tryFireMethod.Invoke(machine, [triggerFinish, null]);
        Assert.Equal("Finished", lastActionProperty.GetValue(machine));
    }

    // ------------ TEST: Fire method behavior ------------------------------
    [Fact]
    public void Generator_Fire_should_throw_on_invalid_transition()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B }
            public enum Trigger { Go, Invalid }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class FireTestMachine
            {
                [Transition(State.A, Trigger.Go, State.B)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "FireTestMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.FireTestMachine");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");

        object stateA = Enum.Parse(stateType!, "A");
        object machine = Activator.CreateInstance(machineType!, stateA)!;

        MethodInfo fireMethod = machineType!.GetMethod("Fire", [triggerType, typeof(object)])!;
        PropertyInfo currentStateProperty = machineType.GetProperty("CurrentState")!;

        object triggerGo = Enum.Parse(triggerType!, "Go");
        object triggerInvalid = Enum.Parse(triggerType!, "Invalid");

        fireMethod.Invoke(machine, [triggerGo, null]);
        Assert.Equal("B", currentStateProperty.GetValue(machine)!.ToString());

        var ex = Assert.Throws<TargetInvocationException>(() =>
            fireMethod.Invoke(machine, [triggerInvalid, null]));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    // ------------ TEST: CanFire method ------------------------------
    [Fact]
    public void Generator_CanFire_should_check_transitions()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B }
            public enum Trigger { Go, Back }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class CanFireMachine
            {
                [Transition(State.A, Trigger.Go, State.B)]
                [Transition(State.B, Trigger.Back, State.A)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "CanFireMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.CanFireMachine");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");

        object stateA = Enum.Parse(stateType!, "A");
        object machine = Activator.CreateInstance(machineType!, stateA)!;

        MethodInfo canFireMethod = machineType!.GetMethod("CanFire", [triggerType!])!;
        MethodInfo tryFireMethod = machineType.GetMethod("TryFire", [triggerType, typeof(object)])!;

        object triggerGo = Enum.Parse(triggerType!, "Go");
        object triggerBack = Enum.Parse(triggerType!, "Back");

        Assert.True((bool)canFireMethod.Invoke(machine, [triggerGo])!);
        Assert.False((bool)canFireMethod.Invoke(machine, [triggerBack])!);

        tryFireMethod.Invoke(machine, [triggerGo, null]);

        Assert.False((bool)canFireMethod.Invoke(machine, [triggerGo])!);
        Assert.True((bool)canFireMethod.Invoke(machine, [triggerBack])!);
    }

    // ------------ TEST: Auto-select Basic mode ------------------------------
    [Fact]
    public void Generator_should_auto_select_basic_mode_when_state_callbacks_exist()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B }
            public enum Trigger { Go }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class AutoSelectBasicMachine
            {
                public string Log {get; set;} = "";
                [Transition(State.A, Trigger.Go, State.B)]
                private void ConfigureTr() { }
                
                [State(State.B, OnEntry = nameof(OnEnterB))] // To wymusi Basic
                private void StateCfg() {}

                private void OnEnterB() => Log = "EnteredB";
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "AutoSelectBasicMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.AutoSelectBasicMachine");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.State");
        Type? triggerType = asm.GetType("TestNamespace.Trigger");

        object stateA = Enum.Parse(stateType!, "A");
        object machine = Activator.CreateInstance(machineType!, stateA)!;

        PropertyInfo logProperty = machineType!.GetProperty("Log")!;
        Assert.Equal("", logProperty.GetValue(machine));

        MethodInfo tryFireMethod = machineType.GetMethod("TryFire", [triggerType, typeof(object)])!;
        object triggerGo = Enum.Parse(triggerType!, "Go");
        tryFireMethod.Invoke(machine, [triggerGo, null]);

        Assert.Equal("EnteredB", logProperty.GetValue(machine));
    }


    // ------------ TEST: Duplicate transition diagnostic ------------------------------
    [Fact]
    public void Generator_should_report_duplicate_transition_diagnostic()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B }
            public enum Trigger { Go }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class DuplicateMachine
            {
                [Transition(State.A, Trigger.Go, State.B)]
                [Transition(State.A, Trigger.Go, State.B)] // Duplicate!
                private void ConfigureTr() { }
            }
        }
        """;

        var (_, diags, _) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        var duplicateDiag = diags.FirstOrDefault(d => d.Id == RuleIdentifiers.DuplicateTransition);
        Assert.NotNull(duplicateDiag);
        Assert.Equal(DiagnosticSeverity.Warning, duplicateDiag?.Severity);
    }

    // ------------ TEST: Missing action method (kompilator C# zgłosi błąd) ------------------------------
    [Fact]
    public void Generator_should_lead_to_compile_error_for_missing_action_method()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B }
            public enum Trigger { Go }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class MissingActionMachine
            {
                [Transition(State.A, Trigger.Go, State.B, Action = nameof(NonExistentMethod))]
                private void ConfigureTr() { }
            }
        }
        """;

        // Assembly (pierwszy element krotki) jest ignorowany, ponieważ oczekujemy błędu kompilacji.
        var (_, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "MissingActionMachine");

        // Oczekujemy błędu kompilacji (np. CS0103), który teraz będzie w 'diags' dzięki zmianom w CompileAndRunGenerator.
        // Ten błąd pochodzi z `nameof(NonExistentMethod)` w kodzie wejściowym.
        Assert.Contains(diags, d => d.Severity == DiagnosticSeverity.Error && d.Id.StartsWith("CS"));
    }

    // ------------ TEST: Invalid guard signature (Parser zgłosi FSM003) ------------------------------
    [Fact]
    public void Generator_should_report_FSM003_for_invalid_guard_signature()
    {
        const string userSource = """
                                  using Abstractions.Attributes;
                                  namespace TestNamespace {
                                      public enum State { A, B }
                                      public enum Trigger { Go }
                                  
                                      [StateMachine(typeof(State), typeof(Trigger))]
                                      public partial class InvalidGuardMachine
                                      {
                                          [Transition(State.A, Trigger.Go, State.B, Guard = nameof(InvalidGuard))]
                                          private void ConfigureTr() { }
                                          
                                          private void InvalidGuard() { } // Guard powinien zwracać bool
                                      }
                                  }
                                  """;

        var (_, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "InvalidGuardMachine");

        var fsm003Diag = diags.FirstOrDefault(d => d.Id == RuleIdentifiers.InvalidMethodSignature);
        Assert.NotNull(fsm003Diag);
        Assert.Equal(DiagnosticSeverity.Error, fsm003Diag.Severity);

        string errorMessage = fsm003Diag.GetMessage().ToString();
        output.WriteLine($"FSM003 Error Message: {errorMessage}");

        Assert.Contains("InvalidGuard", errorMessage);
        Assert.Contains("Guard", errorMessage);
        Assert.Contains("bool InvalidGuard()", errorMessage);
    }

    // ------------ TEST: Callback (np. Akcja) z parametrami (Parser zgłosi błąd FSM003) ------------------------------
    [Fact]
    public void Generator_should_report_FSM003_for_callback_with_parameters()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum State { A, B }
            public enum Trigger { Go }

            [StateMachine(typeof(State), typeof(Trigger))]
            public partial class InvalidCallbackSignatureMachine
            {
                [Transition(State.A, Trigger.Go, State.B, Action = nameof(ActionWithParam))]
                private void ConfigureTr() { }
                
                private void ActionWithParam(int x) { } 
            }
        }
        """;

        var (_, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "InvalidCallbackSignatureMachine");

        var fsm003Diag = diags.FirstOrDefault(d => d.Id == RuleIdentifiers.InvalidMethodSignature);
        Assert.NotNull(fsm003Diag);
        Assert.Equal(DiagnosticSeverity.Error, fsm003Diag.Severity);
        Assert.Contains("ActionWithParam", fsm003Diag.GetMessage().ToString());
        Assert.Contains("parameterless", fsm003Diag.GetMessage().ToString()); // Lub bardziej ogólny komunikat o sygnaturze
    }


    // ------------ TEST: Enum with non-int underlying type ------------------------------
    [Fact]
    public void Generator_should_handle_enum_with_non_int_underlying_type()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum ByteState : byte { Low = 0, High = 255 }
            public enum ByteTrigger : byte { Toggle = 1 }

            [StateMachine(typeof(ByteState), typeof(ByteTrigger))]
            public partial class ByteEnumMachine
            {
                [Transition(ByteState.Low, ByteTrigger.Toggle, ByteState.High)]
                [Transition(ByteState.High, ByteTrigger.Toggle, ByteState.Low)]
                private void ConfigureTr() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "ByteEnumMachine");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineType = asm!.GetType("TestNamespace.ByteEnumMachine");
        Assert.NotNull(machineType);
        Type? stateType = asm.GetType("TestNamespace.ByteState");
        Type? triggerType = asm.GetType("TestNamespace.ByteTrigger");

        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(stateType!));
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(triggerType!));

        object stateLow = Enum.Parse(stateType!, "Low");
        object machine = Activator.CreateInstance(machineType!, stateLow)!;

        MethodInfo tryFireMethod = machineType!.GetMethod("TryFire", [triggerType, typeof(object)])!;
        PropertyInfo currentStateProp = machineType.GetProperty("CurrentState")!;

        object triggerToggle = Enum.Parse(triggerType!, "Toggle");

        bool result = (bool)tryFireMethod.Invoke(machine, [triggerToggle, null])!;
        Assert.True(result);
        Assert.Equal("High", currentStateProp.GetValue(machine)!.ToString());
    }

    // ------------ TEST: Invalid state value in transition (Parser zgłosi FSM006) ------------------------------
    [Fact]
    public void Generator_invalid_state_in_transition_causes_FSM006_diagnostic()
    {
        const string userSource = """
                                  using Abstractions.Attributes;
                                  namespace TestNamespace {
                                      public enum MyStateEnum { A, B } 
                                      public enum MyTriggerEnum { Go }
                                  
                                      [StateMachine(typeof(MyStateEnum), typeof(MyTriggerEnum))]
                                      public partial class InvalidStateValueMachine
                                      {
                                          [Transition((MyStateEnum)999, MyTriggerEnum.Go, MyStateEnum.B)] 
                                          private void ConfigureTr() { }
                                      }
                                  }
                                  """;
        var (_, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "InvalidStateValueMachine");

        var fsm006Diag = diags.FirstOrDefault(d => d.Id == RuleIdentifiers.InvalidEnumValueInTransition);
        Assert.NotNull(fsm006Diag);
        Assert.Equal(DiagnosticSeverity.Error, fsm006Diag.Severity);

        string errorMessage = fsm006Diag.GetMessage().ToString();
        output.WriteLine($"FSM006 Error: {errorMessage}");

        Assert.Contains("999", errorMessage);
        Assert.Contains("MyStateEnum", errorMessage);
    }

    // ------------ TEST: Multiple state machines in one compilation unit ------------------------------
    [Fact]
    public void Generator_multiple_state_machines_in_one_compilation_unit()
    {
        const string userSource = """
        using Abstractions.Attributes;
        namespace TestNamespace {
            public enum StateA { A1, A2 } public enum TriggerA { GoA }
            public enum StateB { B1, B2 } public enum TriggerB { GoB }

            [StateMachine(typeof(StateA), typeof(TriggerA))]
            public partial class MachineA
            {
                [Transition(StateA.A1, TriggerA.GoA, StateA.A2)]
                private void CfgA() { }
            }

            [StateMachine(typeof(StateB), typeof(TriggerB))]
            public partial class MachineB
            {
                [Transition(StateB.B1, TriggerB.GoB, StateB.B2)]
                private void CfgB() { }
            }
        }
        """;

        var (asm, diags, generatedSources) = CompileAndRunGenerator([userSource], new StateMachineGenerator());
        OutputGeneratedCode(generatedSources, "MachineA");
        OutputGeneratedCode(generatedSources, "MachineB");
        Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(asm);

        Type? machineA = asm!.GetType("TestNamespace.MachineA");
        Type? machineB = asm.GetType("TestNamespace.MachineB");
        Type? interfaceA = asm.GetType("TestNamespace.IMachineA");
        Type? interfaceB = asm.GetType("TestNamespace.IMachineB");

        Assert.NotNull(machineA); Assert.NotNull(machineB);
        Assert.NotNull(interfaceA); Assert.NotNull(interfaceB);

        Type? stateAType = asm.GetType("TestNamespace.StateA");
        Type? stateBType = asm.GetType("TestNamespace.StateB");

        object stateA1 = Enum.Parse(stateAType!, "A1");
        object stateB1 = Enum.Parse(stateBType!, "B1");

        object instanceA = Activator.CreateInstance(machineA!, stateA1)!;
        object instanceB = Activator.CreateInstance(machineB!, stateB1)!;

        Assert.NotEqual(instanceA.GetType(), instanceB.GetType());
    }

    // ------------ TEST: Thread safety of generator ------------------------------
    [Fact]
    public async Task Generator_should_be_thread_safe_when_compiling_multiple_machines()
    {
        const string userSourceTemplate = """
        using Abstractions.Attributes;
        namespace TestNamespace{0} {{
            public enum State{0} {{ S1, S2 }}
            public enum Trigger{0} {{ T1 }}

            [StateMachine(typeof(State{0}), typeof(Trigger{0}))]
            public partial class Machine{0} {{
                [Transition(State{0}.S1, Trigger{0}.T1, State{0}.S2)]
                private void Cfg() {{ }}
            }}
        }}
        """;

        var sources = Enumerable.Range(1, 5)
            .Select(i => string.Format(userSourceTemplate, i))
            .ToArray();

        var tasks = sources.Select(source => Task.Run(() => {
            try
            {
                // W tym teście asm nie jest używane, więc można je zignorować
                var (_, diags, _) = CompileAndRunGenerator(
                    [  source],
                    new StateMachineGenerator());
                return diags.Any(d => d.Severity == DiagnosticSeverity.Error);
            }
            catch
            {
                return true;
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        Assert.All(tasks, t => Assert.False(t.Result));
    }

   

}