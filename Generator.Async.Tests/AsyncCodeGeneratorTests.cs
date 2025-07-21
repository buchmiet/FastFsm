using System.Threading.Tasks;
using Generator.Model;
using Generator.SourceGenerators;
using VerifyXunit;
using Xunit;

namespace Generator.Async.Tests;


public class AsyncCodeGeneratorTests
{
    [Fact]
    public Task Generates_Correct_Async_Machine_With_Async_Guard_And_Action()
    {
        // 1. ARRANGE: Ręcznie stwórz model maszyny stanu
        var model = new StateMachineModel
        {
            ClassName = "TestAsyncMachine",
            Namespace = "MyTest.Machines",
            StateType = "Generator.Async.Tests.Implementations.TestState",
            TriggerType = "Generator.Async.Tests.Implementations.TestTrigger",
            ContinueOnCapturedContext = false,
            GenerationConfig = new GenerationConfig { IsAsync = true },
            States = { /* ... */ },
            Transitions =
            {
                new TransitionModel
                {
                    FromState = "A",
                    ToState = "B",
                    Trigger = "Go",
                    GuardMethod = "CanGoAsync",
                    GuardIsAsync = true,
                    ActionMethod = "OnGoAsync",
                    ActionIsAsync = true
                }
            }
        };

        // 2. ACT: Uruchom generator na modelu
        var generator = new AsyncStateMachineCodeGenerator(model);
        var generatedCode = generator.Generate();

        // 3. ASSERT: Porównaj wygenerowany kod ze wzorcem
        return Verifier.Verify(generatedCode)
            .UseDirectory("Snapshots") // Opcjonalnie, aby trzymać wzorce w osobnym folderze
            .UseTextForParameters("cs"); // Użyj rozszerzenia .cs dla pliku wzorca
    }

    // Alternatywnie, możesz wydzielić wspólną metodę pomocniczą:
    private static Task VerifyGeneratedCode(StateMachineModel model)
    {
        var generator = new AsyncStateMachineCodeGenerator(model);
        var generatedCode = generator.Generate();

        return Verifier.Verify(generatedCode)
            .UseDirectory("Snapshots")
            .UseTextForParameters("cs");
    }

    // Helper do tworzenia bazowego modelu
    private static StateMachineModel CreateBaseModel(string className = "TestAsyncMachine")
    {
        return new StateMachineModel
        {
            ClassName = className,
            Namespace = "MyTest.Machines",
            StateType = "Generator.Async.Tests.Implementations.TestState",
            TriggerType = "Generator.Async.Tests.Implementations.TestTrigger",
            ContinueOnCapturedContext = false,
            GenerationConfig = new GenerationConfig { IsAsync = true },
            States = new Dictionary<string, StateModel>
            {
                ["A"] = new StateModel { Name = "A" },
                ["B"] = new StateModel { Name = "B" },
                ["C"] = new StateModel { Name = "C" }
            }
        };
    }

    // Przykład użycia helpera:
    [Fact]
    public Task Generates_Minimal_Async_Machine()
    {
        var model = CreateBaseModel();
        // Nie dodajemy przejść - minimalna maszyna

        return VerifyGeneratedCode(model);
    }
}