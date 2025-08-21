# Raport analizy warningów FastFSM

## Podsumowanie

Po analizie kompilacji projektów testowych FastFSM zidentyfikowano następujące główne problemy:

### 1. FastFSM obecnie pakuje się ze stałą nazwą do katalogu `/nuget`
- Lokalizacja: `FastFsm/FastFsm.csproj` linia 17
- Konfiguracja: `<PackageOutputPath>../nuget</PackageOutputPath>`

### 2. Warnings generowane przez StateMachineParser

Podczas budowania projektów występują dwa główne typy warningów:

#### FSM004: Brakujący atrybut StateMachine
- **Liczba wystąpień**: 17 klas
- **Przyczyna**: Klasy częściowe (partial class) bez atrybutu `[StateMachine]` są analizowane przez StateMachineAnalyzer
- **Lokalizacja problemu**: `Generator/Analyzers/StateMachineAnalyzer.cs` i `Generator/Parsers/StateMachineParser.cs`

#### FSM002: Potencjalnie nieosiągalne stany
- **Liczba wystąpień**: ponad 300 warningów
- **Przyczyna**: Stany w hierarchicznych maszynach stanów (HSM) bez zdefiniowanych tranzycji
- **Lokalizacja problemu**: Głównie w testach HSM (`HsmParsingCompilationTests.cs`)

#### FSM001: Duplikaty tranzycji
- **Liczba wystąpień**: 1
- **Przyczyna**: Wielokrotne definicje tej samej tranzycji

## Środki zaradcze

### 1. Problem pakowania NuGet

**Rozwiązanie**: Zmiana konfiguracji pakowania na dynamiczną

```xml
<!-- W FastFsm.csproj zmienić: -->
<PropertyGroup>
  <PackageOutputPath>$(MSBuildThisFileDirectory)../nuget</PackageOutputPath>
  <!-- Lub użyć zmiennej środowiskowej: -->
  <PackageOutputPath Condition="'$(NUGET_OUTPUT_PATH)' != ''">$(NUGET_OUTPUT_PATH)</PackageOutputPath>
  <PackageOutputPath Condition="'$(NUGET_OUTPUT_PATH)' == ''">../nuget</PackageOutputPath>
</PropertyGroup>
```

### 2. Warning FSM004 - Brakujący atrybut StateMachine

**Przyczyna główna**: StateMachineAnalyzer analizuje WSZYSTKIE klasy partial, nawet te które są klasami testowymi lub pomocniczymi.

**Rozwiązania**:

#### Opcja A: Dodanie wykluczeń dla klas testowych
```csharp
// W StateMachineAnalyzer.cs
private bool ShouldAnalyzeClass(INamedTypeSymbol classSymbol)
{
    // Pomiń klasy w namespace testowych
    if (classSymbol.ContainingNamespace?.ToString()?.Contains("Tests") == true)
        return false;
        
    // Pomiń klasy z atrybutem [IgnoreStateMachineAnalysis]
    if (classSymbol.GetAttributes().Any(a => 
        a.AttributeClass?.Name == "IgnoreStateMachineAnalysisAttribute"))
        return false;
        
    return true;
}
```

#### Opcja B: Analiza tylko klas z powiązanymi atrybutami FSM
```csharp
// Analizuj tylko jeśli klasa ma jakiekolwiek atrybuty związane z FSM
private bool HasFsmRelatedAttributes(INamedTypeSymbol classSymbol)
{
    var fsmAttributes = new[] { 
        "TransitionAttribute", 
        "StateAttribute", 
        "InternalTransitionAttribute" 
    };
    
    return classSymbol.GetMembers()
        .OfType<IMethodSymbol>()
        .SelectMany(m => m.GetAttributes())
        .Any(a => fsmAttributes.Contains(a.AttributeClass?.Name));
}
```

#### Opcja C: Zmiana severity na Info dla klas testowych
```csharp
// W MissingStateMachineAttributeRule
public IEnumerable<RuleResult> Validate(MissingStateMachineAttributeContext context)
{
    var severity = context.ClassSymbol.ContainingAssembly.Name.Contains("Tests") 
        ? RuleSeverity.Info 
        : RuleSeverity.Warning;
        
    yield return new RuleResult(
        DiagnosticCode.MissingStateMachineAttribute,
        $"Class '{context.ClassSymbol.Name}' is missing the [StateMachine] attribute",
        severity
    );
}
```

### 3. Warning FSM002 - Nieosiągalne stany

**Przyczyna główna**: W testach HSM definiowane są złożone hierarchie stanów bez pełnych tranzycji (co jest zamierzone dla testów).

**Rozwiązania**:

#### Opcja A: Wyłączenie warningów dla projektów testowych
```xml
<!-- W FastFsm.Tests.csproj -->
<PropertyGroup>
  <NoWarn>$(NoWarn);FSM002</NoWarn>
</PropertyGroup>
```

#### Opcja B: Inteligentna analiza osiągalności w HSM
```csharp
// W UnreachableStateRule
public IEnumerable<RuleResult> Validate(UnreachableStateContext context)
{
    // W HSM, stany potomne są osiągalne przez stany rodzica
    if (context.IsHierarchicalMachine && IsChildState(context.State))
    {
        if (IsParentReachable(context.State))
            yield break; // Nie generuj warningu
    }
    
    // Standardowa logika...
}
```

#### Opcja C: Atrybut do suppressowania warningów
```csharp
[StateMachine(typeof(States), typeof(Triggers))]
[SuppressMessage("FSM", "FSM002:Unreachable state", Justification = "Test scenario")]
public partial class TestMachine { }
```

### 4. Poprawa diagnostyki

**Zalecenie**: Dodanie kontekstu do komunikatów warningów

```csharp
// Zamiast:
"State 'Working' might be unreachable based on defined transitions"

// Lepiej:
"State 'Working' in machine 'SimpleParentChildMachine' might be unreachable. 
No transitions lead to this state. If this is intentional (e.g., for testing), 
consider suppressing this warning."
```

## Rekomendacje priorytetowe

1. **WYSOKI PRIORYTET**: Rozwiązanie problemu FSM004 dla klas testowych - generuje najwięcej szumu
2. **ŚREDNI PRIORYTET**: Poprawa analizy osiągalności stanów w HSM
3. **NISKI PRIORYTET**: Konfigurowalność ścieżki pakowania NuGet

## Implementacja

Sugerowana kolejność implementacji:

1. Dodać filtrowanie klas testowych w `StateMachineAnalyzer` (Opcja 2A)
2. Skonfigurować `NoWarn` dla FSM002 w projektach testowych (Opcja 3A)
3. Rozszerzyć komunikaty diagnostyczne o więcej kontekstu
4. Opcjonalnie: Dodać atrybut `[IgnoreStateMachineAnalysis]` dla edge cases

## Metryki sukcesu

Po implementacji:
- Redukcja warningów FSM004 o ~95% (pozostawienie tylko rzeczywistych problemów)
- Redukcja warningów FSM002 w testach o 100%
- Czystsze logi kompilacji ułatwiające identyfikację prawdziwych problemów