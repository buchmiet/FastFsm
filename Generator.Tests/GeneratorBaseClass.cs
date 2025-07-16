using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace Generator.Tests;
/*
 *
 *### Propozycja zestawu test-case’ów (tylko nazwy + krótki opis)
   
   | #        | Scenariusz                                                                               | Cel testu                                                                                                                |
   | -------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
   | **F-01** | **Forced WithPayload bez `[PayloadType]`**                                               | Sprawdzić, że generator zgłasza błąd **FSM007** (brak typu payload) zamiast wyjątku wewnętrznego.                        |
   | **F-02** | **Forced WithPayload + wiele `[PayloadType(trigger,…)]`**                                | Oczekiwana eskalacja do wariantu *multi* lub błąd diagnostyczny – upewnić się, że Force nie tworzy niepoprawnej maszyny. |
   | **F-03** | **Forced WithPayload + guard z parametrem innego typu**                                  | Walidacja **FSM003** powinna wychwycić złą sygnaturę metody.                                                             |
   | **F-04** | **Forced WithPayload + brak overloadu bezparametrowego**                                 | Zweryfikować, że payload-metody działają, a brak parametru powoduje poprawne wywołanie/odrzucenie.                       |
   | **F-05** | **Forced WithPayload + wywołanie `TryFire(trigger, object?)` z błędnym typem w runtime** | Potwierdzić, że wariant *single* nie rzuca wyjątku, tylko przechodzi ścieżkę „fallback” (is + wersja bezparametrowa).    |
   | **F-06** | **Forced Full bez `[PayloadType]`**                                                      | Błąd diagnostyczny jak w F-01.                                                                                           |
   | **F-07** | **Forced Full + `GenerateExtensibleVersion=false`**                                      | Generator powinien zignorować flagę Force lub zgłosić spójny błąd („Full wymaga extensible”).                            |
   | **F-08** | **Forced Pure przy obecnych `[PayloadType]`**                                            | Oczekiwana seria błędów **FSM003** (nadmiarowe parametry) – kod nie powinien się wygenerować.                            |
   | **F-09** | **Forced Pure, ale klasa zawiera OnEntry/OnExit**                                        | Should downgrade zawartość (ignorować metody) lub wyemitować ostrzeżenie, brak crashu.                                   |
   | **F-10** | **Brak Force → autodetekcja Single vs Multi**                                            | Referencyjny przypadek poprawnej detekcji (kontrola regresji).                                                           |
   | **F-11** | **Multi-payload: złe dane w runtime (`_payloadMap` check)**                              | `TryFire` zwraca `false`, `Fire` rzuca `InvalidOperationException`.                                                      |
   | **F-12** | **Single-payload: poprawny typ, ale `null` payload**                                     | Metody z parametrem pomijane, wywołania bezparametrowe działają – brak wyjątków.                                         |
   
   > *Lista obejmuje zarówno walidację kompilacji (diagnostyki FSM00x) jak i zachowanie w trakcie wykonywania dla najczęstszych „mismatchy” pomiędzy deklaracjami a realnym użyciem.*
   
 *
 */
public abstract class GeneratorBaseClass(ITestOutputHelper output)
{
    protected void AddProjectReferences(List<MetadataReference> refs)
    {
        // Znajdź katalog z projektami
        string testAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string configuration = testAssemblyPath.Contains("Debug") ? "Debug" : "Release";

        // Idź w górę do katalogu rozwiązania
        string currentDir = testAssemblyPath;
        string? solutionDir = null;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.GetFiles(currentDir, "*.sln").Any())
            {
                solutionDir = currentDir;
                break;
            }
            var parent = Directory.GetParent(currentDir);
            if (parent == null) break;
            currentDir = parent.FullName;
        }

        if (solutionDir == null)
            throw new InvalidOperationException("Cannot find solution directory");

        // Dodaj StateMachine.dll
        string fsmFastDllPath = Path.Combine(
            solutionDir, "StateMachine.", "bin", configuration, "net9.0", "StateMachine.dll");

        if (File.Exists(fsmFastDllPath))
        {
            refs.Add(MetadataReference.CreateFromFile(fsmFastDllPath));
            output.WriteLine($"Added reference to: {fsmFastDllPath}");
        }
        else
        {
            throw new FileNotFoundException($"StateMachine.dll not found at: {fsmFastDllPath}. " +
                                            "Make sure StateMachine. project is built before running tests.");
        }

        // Dodaj Abstractions.dll (jeśli nie jest już w atrybutach)
        string abstractionsDllPath = Path.Combine(
            solutionDir, "Abstractions", "bin", configuration, "netstandard2.0", "Abstractions.dll");

        if (File.Exists(abstractionsDllPath))
        {
            refs.Add(MetadataReference.CreateFromFile(abstractionsDllPath));
            output.WriteLine($"Added reference to: {abstractionsDllPath}");
        }
    }


    private string? GetSolutionDir()
    {
        string testAssemblyPath = Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location)!;

        string current = testAssemblyPath;
        for (int i = 0; i < 10; i++)
        {
            // Jeśli w bieżącym katalogu jest plik .sln → to nasz root
            if (Directory.GetFiles(current, "*.sln").Any())
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;                     // dotarliśmy do korzenia dysku

            current = parent.FullName;     // przejdź katalog wyżej
        }

        return null;                       // nie znaleziono pliku .sln
    }


    protected (Assembly? asm, ImmutableArray<Diagnostic> diags, Dictionary<string, string> generatedSources)
        CompileAndRunGenerator(IEnumerable<string> userSources, IIncrementalGenerator generator)
    {
        // 1. Wczytaj kod atrybutów dynamicznie jako listę stringów (każdy plik to osobny string)
        //List<string> attributeSourceCodes = LoadAttributeSourceCodesFromAbstractionsProject();
        //output.WriteLine("Begin: all attributes:");
        //output.WriteLine(string.Join(Environment.NewLine, attributeSourceCodes));
        //output.WriteLine("End: all attributes.");
        //// 2. Połącz wszystkie kody źródłowe
        //var allSourceTexts = new List<string>();
        //allSourceTexts.AddRange(attributeSourceCodes); // Dodaj każdy plik atrybutu jako osobny tekst źródłowy
        //// allSourceTexts.Add(RuntimeSource);          // Dodaj RuntimeSource
        //allSourceTexts.AddRange(userSources);       // Dodaj kody użytkownika



        //var trees = allSourceTexts
        //    .Select(s => CSharpSyntaxTree.ParseText(s)) // Każdy string staje się osobnym drzewem
        //    .ToArray();

        // 1.  ***** NIE ładujemy już źródeł atrybutów *****
        var allSourceTexts = new List<string>();

        var solutionDir = GetSolutionDir();
        if (solutionDir is not null)
        {
            var extRunner = Path.Combine(solutionDir,
                "StateMachine",
                "Runtime",
                "Extensions",
                "ExtensionRunner.cs");
            if (File.Exists(extRunner))
                allSourceTexts.Add(File.ReadAllText(extRunner));
        }

        // 3.  Kody użytkownika
        allSourceTexts.AddRange(userSources);

        var trees = allSourceTexts
            .Select(s => CSharpSyntaxTree.ParseText(s))
            .ToArray();
        // Reszta metody CompileAndRunGenerator pozostaje taka sama ...
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        AddProjectReferences(refs);

        string netstandard = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "netstandard.dll");
        if (File.Exists(netstandard))
            refs.Add(MetadataReference.CreateFromFile(netstandard));

        var compilation = CSharpCompilation.Create(
            "FsmTestAssembly",
            trees, // Przekazujemy tablicę drzew składni
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(generator);

        var driverAfterRun = driver.RunGeneratorsAndUpdateCompilation(compilation,
            out var outCompilation,
            out var generatorRunDiagnostics);

        var generatedSources = new Dictionary<string, string>();
        var generatorDriverRunResult = driverAfterRun.GetRunResult();
        foreach (var generatorResult in generatorDriverRunResult.Results)
        {
            foreach (var generatedSource in generatorResult.GeneratedSources)
            {
                generatedSources[generatedSource.HintName] = generatedSource.SourceText.ToString();
            }
        }

        using var ms = new MemoryStream();
        var emitResult = outCompilation.Emit(ms);
        var allDiagnostics = generatorRunDiagnostics.AddRange(emitResult.Diagnostics);

        Assembly? assembly = null;
        if (emitResult.Success)
        {
            ms.Position = 0;
            assembly = Assembly.Load(ms.ToArray());
        }

        return (assembly, allDiagnostics, generatedSources);
    }

}