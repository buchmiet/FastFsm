# HSM (Hierarchical State Machines) Legacy API Parity - Plan Implementacji

Generated: 2025-09-11

## 📊 Stan obecny - BRAK PARYTETU

### Maszyny HSM wymagające implementacji Legacy

| Maszyna | Fluent | Legacy | Priorytet |
|---------|--------|--------|----------|
| DebugHsmTest | ✅ YES | ❌ NO | P2 |
| DeepHistoryTests | ✅ YES | ❌ NO | P0 |
| HsmIsInHierarchyTests | ✅ YES | ❌ NO | P0 |
| InheritanceTests | ✅ YES | ❌ NO | P1 |
| InitialChildTests | ✅ YES | ❌ NO | P0 |
| InternalTransitionTests | ✅ YES | ❌ NO | P1 |
| ShallowHistoryTests | ✅ YES | ❌ NO | P0 |
| SimpleParentChildMachine | ✅ YES | ❌ NO | P0 |

### Maszyny HSM tylko Legacy (do analizy)

| Maszyna | Fluent | Legacy | Uwagi |
|---------|--------|--------|-------|
| HierarchicalRuntime | ❌ NO | ✅ YES | Do migracji na Fluent |
| HsmAdditionalCompilationTests | ❌ NO | ✅ YES | Testy kompilacji |

## 🎯 STRATEGIA SZYBKIEJ IMPLEMENTACJI

### Krok 1: Infrastruktura (30 minut)

#### 1.1 Skopiuj wspólne enumy HSM

```csharp
// Plik: FastFsm.Tests/Features/Hsm/Common/HsmEnums.cs
namespace FastFsm.Tests.Features.Hsm.Common
{
    // WSPÓLNE ENUMY DLA FLUENT I LEGACY
    public enum HsmState
    {
        // Root states
        Idle, Working, Completed, Error, Paused,
        
        // Working substates (2nd level)
        Working_Initializing, Working_Processing, Working_Validating, Working_Cleanup,
        
        // Working_Processing substates (3rd level)
        Working_Processing_Reading, Working_Processing_Computing, Working_Processing_Writing,
        
        // Working_Processing_Computing substates (4th level)
        Working_Processing_Computing_Loading,
        Working_Processing_Computing_Calculating,
        Working_Processing_Computing_Storing,
        
        // History test states
        HistoryParent, HistoryParent_Child1, HistoryParent_Child2, HistoryParent_Child3,
        
        // Deep history states
        DeepHistoryParent, DeepHistoryParent_Child1,
        DeepHistoryParent_Child1_SubChild1, DeepHistoryParent_Child1_SubChild2,
        DeepHistoryParent_Child2,
        
        // Priority states
        Priority_Low, Priority_Medium, Priority_High,
        
        // Internal transition states
        InternalParent, InternalParent_Child1, InternalParent_Child2,
        
        // Cross-hierarchy states
        Branch1, Branch1_Leaf1, Branch1_Leaf2,
        Branch2, Branch2_Leaf1, Branch2_Leaf2,
        
        // Complex test states
        ComplexParent, ComplexParent_Child1, ComplexParent_Child2, ComplexParent_Child3,
        
        // Edge case states
        EdgeParent, EdgeParent_Child,
        EdgeComplexParent, EdgeComplexParent_Child1, EdgeComplexParent_Child2,
        
        // Initial child states
        InitialParent, InitialParent_FirstChild, InitialParent_SecondChild
    }

    public enum HsmTrigger
    {
        Start, Process, Complete, Validate, Execute,
        Pause, Resume, Reset, Initialize,
        Activate, Deactivate, Submit, Approve, Reject,
        Timeout, Error, Recover,
        InternalUpdate, InternalProcess,
        MoveNext, MovePrevious, CrossBranch,
        Abort, Finish, Cancel, Retry, Skip,
        Go, Next, Back, Enter, Exit,
        ToChild1, ToChild2, ToChild3,
        ToParent, ToSibling,
        EnterWork, LeaveWork, EnterParent, LeaveParent,
        Switch, Toggle
    }
}
```

#### 1.2 Zaktualizuj MachineTypeRegistry

```csharp
// Dodaj do FastFsm.Tests/TestHelpers/MachineTypeRegistry.cs
["SimpleParentChild"] = new EnumTypePair(
    typeof(HsmState),  // Używamy wspólnych enumów!
    typeof(HsmState),  // Te same dla Legacy
    typeof(HsmTrigger),
    typeof(HsmTrigger)
),
["DeepHistory"] = new EnumTypePair(
    typeof(HsmState), typeof(HsmState),
    typeof(HsmTrigger), typeof(HsmTrigger)
),
["ShallowHistory"] = new EnumTypePair(
    typeof(HsmState), typeof(HsmState),
    typeof(HsmTrigger), typeof(HsmTrigger)
),
["InitialChild"] = new EnumTypePair(
    typeof(HsmState), typeof(HsmState),
    typeof(HsmTrigger), typeof(HsmTrigger)
),
// Dodaj pozostałe...
```

### Krok 2: Szablon konwersji Fluent → Legacy (10 minut na maszynę)

#### 2.1 Wzorzec konwersji

**Z Fluent API:**
```csharp
[StateMachine(typeof(HsmStateFluent), typeof(HsmTriggerFluent), EnableHierarchy = true)]
public partial class SimpleParentChildMachineFluent
{
    public static void Configure()
    {
        FSM.State(HsmStateFluent.Working)
           .Initial(HsmStateFluent.Working_Initializing)
           .OnEntry(nameof(OnWorkingEntry))
           .OnExit(nameof(OnWorkingExit));
           
        FSM.State(HsmStateFluent.Working_Initializing)
           .ChildOf(HsmStateFluent.Working)
           .OnEntry(nameof(OnInitializingEntry));
           
        FSM.State(HsmStateFluent.Idle)
           .On(HsmTriggerFluent.Start).GoTo(HsmStateFluent.Working);
    }
    
    private void OnWorkingEntry() => WorkingEntered = true;
    private void OnWorkingExit() => WorkingExited = true;
    private void OnInitializingEntry() => InitializingEntered = true;
}
```

**Na Legacy API:**
```csharp
[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class SimpleParentChildMachineLegacy
{
    // STAN: Working (parent)
    [State(HsmState.Working, 
           OnEntry = nameof(OnWorkingEntry), 
           OnExit = nameof(OnWorkingExit),
           IsInitial = false)]
    [InitialChild(HsmState.Working, HsmState.Working_Initializing)]
    private void ConfigureWorking() { }
    
    // STAN: Working_Initializing (child)
    [State(HsmState.Working_Initializing, 
           Parent = HsmState.Working,
           OnEntry = nameof(OnInitializingEntry))]
    private void ConfigureWorkingInitializing() { }
    
    // STAN: Idle
    [State(HsmState.Idle)]
    private void ConfigureIdle() { }
    
    // TRANZYCJE
    [Transition(HsmState.Idle, HsmTrigger.Start, HsmState.Working)]
    private void TransitionIdleToWorking() { }
    
    // CALLBACKI (skopiuj 1:1 z Fluent)
    private void OnWorkingEntry() => WorkingEntered = true;
    private void OnWorkingExit() => WorkingExited = true;
    private void OnInitializingEntry() => InitializingEntered = true;
}
```

#### 2.2 Mapowanie konstrukcji Fluent → Legacy

| Fluent API | Legacy Attribute | Uwagi |
|------------|-----------------|-------|
| `.ChildOf(Parent)` | `[State(Child, Parent = Parent)]` | Relacja parent-child |
| `.Initial(Child)` | `[InitialChild(Parent, Child)]` | Początkowy stan dziecka |
| `.OnEntry(method)` | `[State(S, OnEntry = method)]` | Callback wejścia |
| `.OnExit(method)` | `[State(S, OnExit = method)]` | Callback wyjścia |
| `.On(T).GoTo(S)` | `[Transition(From, T, To)]` | Tranzycja |
| `.OnInternal(T).Action(m)` | `[Transition(S, T, S, IsInternal = true, Action = m)]` | Tranzycja wewnętrzna |
| `.Guard(method)` | `[Transition(..., Guard = method)]` | Guard |
| `.Priority(n)` | `[Transition(..., Priority = n)]` | Priorytet |
| `.HistoryShallow()` | `[State(S, History = HistoryType.Shallow)]` | Historia płytka |
| `.HistoryDeep()` | `[State(S, History = HistoryType.Deep)]` | Historia głęboka |

### Krok 3: Implementacja maszyn (5 godzin)

#### Kolejność implementacji (od najprostszych):

1. **SimpleParentChildMachine.Legacy.cs** (30 min)
   - Podstawowa hierarchia 2-poziomowa
   - Initial child
   - Entry/Exit callbacks

2. **InitialChildTests.Legacy.cs** (30 min)
   - Focus na Initial child functionality
   - Automatyczne przejście do dziecka

3. **HsmIsInHierarchyTests.Legacy.cs** (30 min)
   - Testowanie IsInHierarchy()
   - Weryfikacja relacji parent-child

4. **InternalTransitionTests.Legacy.cs** (45 min)
   - Tranzycje wewnętrzne
   - Akcje bez zmiany stanu

5. **ShallowHistoryTests.Legacy.cs** (45 min)
   - Historia płytka
   - Powrót do ostatniego stanu

6. **DeepHistoryTests.Legacy.cs** (45 min)
   - Historia głęboka
   - Powrót do zagnieżdżonych stanów

7. **InheritanceTests.Legacy.cs** (45 min)
   - Dziedziczenie stanów
   - Złożone hierarchie

8. **DebugHsmTest.Legacy.cs** (30 min)
   - Debugowanie HSM
   - Trace'owanie przejść

### Krok 4: Wrappery (2 godziny)

#### 4.1 Szablon wrappera HSM

```csharp
// Plik: FastFsm.Tests/TestHelpers/HsmWrappers.cs
namespace FastFsm.Tests.TestHelpers
{
    // SimpleParentChild wrappers
    public class SimpleParentChildFluentWrapper : IStateMachineTestWrapper
    {
        private readonly SimpleParentChildMachineFluent _machine;
        
        public SimpleParentChildFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<HsmState>(
                "SimpleParentChild", initialStateName);
            var state = (HsmState)Enum.Parse(typeof(HsmState), resolvedName);
            _machine = new SimpleParentChildMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (HsmTrigger)Enum.Parse(
                typeof(HsmTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        // Reszta metod...
    }
    
    public class SimpleParentChildLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly SimpleParentChildMachineLegacy _machine;
        // Identyczna implementacja jak Fluent, tylko z Legacy typem
    }
}
```

#### 4.2 Rejestracja w StateMachineWrapperFactory

```csharp
// Dodaj do StateMachineWrapperFactory.cs
["SimpleParentChild"] = CreateSimpleParentChildWrapper,
["DeepHistory"] = CreateDeepHistoryWrapper,
["ShallowHistory"] = CreateShallowHistoryWrapper,
["InitialChild"] = CreateInitialChildWrapper,
// ...

private static IStateMachineTestWrapper CreateSimpleParentChildWrapper(
    ApiType apiType, string initialStateName)
{
    return apiType switch
    {
        ApiType.Fluent => new SimpleParentChildFluentWrapper(initialStateName),
        ApiType.Legacy => new SimpleParentChildLegacyWrapper(initialStateName),
        _ => throw new ArgumentException($"Unknown API type: {apiType}")
    };
}
```

### Krok 5: Testy integracyjne (1 godzina)

#### 5.1 Dodaj do MatrixConfig

```csharp
// FastFsm.Tests/TestHelpers/MatrixConfig.cs
["SimpleParentChild"] = new MachineTestConfig
{
    MachineName = "SimpleParentChild",
    InitialState = "Idle",
    TriggerSequence = new[] { "Start", "Process", "Complete" }
},
["DeepHistory"] = new MachineTestConfig
{
    MachineName = "DeepHistory",
    InitialState = "Out",
    TriggerSequence = new[] { "EnterWork", "Next", "Abort", "EnterWork" }
},
// ...
```

#### 5.2 Dodaj do MatrixEntries

```csharp
new MatrixEntry("SimpleParentChild", null, ApiCapabilities.IsHierarchical),
new MatrixEntry("DeepHistory", null, ApiCapabilities.IsHierarchical | ApiCapabilities.HasHistory),
new MatrixEntry("ShallowHistory", null, ApiCapabilities.IsHierarchical | ApiCapabilities.HasHistory),
new MatrixEntry("InitialChild", null, ApiCapabilities.IsHierarchical),
// ...
```

## ⚡ OPTYMALIZACJE DLA SZYBKIEJ IMPLEMENTACJI

### 1. Użyj wspólnych enumów
- **NIE** twórz osobnych HsmStateFluent i HsmStateLegacy
- Użyj wspólnych HsmState i HsmTrigger dla obu API
- To eliminuje potrzebę konwersji enumów!

### 2. Automatyzacja konwersji
- Stwórz skrypt PowerShell/Python do konwersji Fluent → Legacy
- Regex do przekształcania `.ChildOf()` → `[State(Parent =)]`
- Regex do przekształcania `.On().GoTo()` → `[Transition()]`

### 3. Kopiuj-wklej strategia
- Skopiuj callbacki 1:1 z Fluent do Legacy
- Skopiuj publiczne właściwości/pola
- Skopiuj testy i zamień tylko typ maszyny

### 4. Grupowanie pracy
- Najpierw wszystkie pliki .Legacy.cs
- Potem wszystkie wrappery
- Na końcu rejestracja w fabrykach

### 5. Testowanie przyrostowe
- Po każdej maszynie uruchom: `dotnet test --filter "machineName"`
- Nie czekaj z testowaniem do końca

## 📋 CHECKLIST IMPLEMENTACJI

### Faza 1: Infrastruktura (30 min)
- [ ] Stwórz HsmEnums.cs ze wspólnymi enumami
- [ ] Zaktualizuj MachineTypeRegistry
- [ ] Przygotuj szablon Legacy maszyny

### Faza 2: Maszyny Legacy (5 godzin)
- [ ] SimpleParentChildMachine.Legacy.cs
- [ ] InitialChildTests.Legacy.cs
- [ ] HsmIsInHierarchyTests.Legacy.cs
- [ ] InternalTransitionTests.Legacy.cs
- [ ] ShallowHistoryTests.Legacy.cs
- [ ] DeepHistoryTests.Legacy.cs
- [ ] InheritanceTests.Legacy.cs
- [ ] DebugHsmTest.Legacy.cs

### Faza 3: Wrappery (2 godziny)
- [ ] HsmWrappers.cs ze wszystkimi wrapperami
- [ ] Aktualizacja StateMachineWrapperFactory
- [ ] Test wrapperów

### Faza 4: Integracja (1 godzina)
- [ ] Dodaj do MatrixConfig
- [ ] Dodaj do MatrixEntries
- [ ] Uruchom DualApiMatrixTests
- [ ] Uruchom WrapperSmokeTests

### Faza 5: Weryfikacja (30 min)
- [ ] Uruchom wszystkie testy
- [ ] Zaktualizuj PARYTET.md
- [ ] Commit z message: "feat: Add Legacy API support for HSM machines"

## 🎯 CELE DO OSIĄGNIĘCIA

1. **100% parytet HSM** - wszystkie maszyny HSM mają implementację Legacy
2. **Zero failing tests** - wszystkie testy przechodzą
3. **Czas realizacji: 8-9 godzin** (nie 2 dni!)
4. **Reużywalność kodu** - maksymalne wykorzystanie istniejącego kodu Fluent

## 🚀 KLUCZOWE ZASADY SUKCESU

1. **NIE WYNAJDUJ KOŁA** - kopiuj z Fluent, adaptuj do Legacy
2. **WSPÓLNE ENUMY** - eliminuj potrzebę konwersji
3. **TESTUJ CZĘSTO** - po każdej maszynie
4. **AUTOMATYZUJ** - użyj regex/skryptów gdzie możliwe
5. **PRIORYTETYZUJ** - najpierw P0, potem P1, na końcu P2

## Definition of Done

- [ ] Wszystkie 8 maszyn HSM ma implementację Legacy
- [ ] Wszystkie wrappery utworzone i zarejestrowane
- [ ] DualApiMatrixTests przechodzi dla HSM
- [ ] WrapperSmokeTests przechodzi dla HSM
- [ ] PARYTET.md pokazuje 100% dla HSM
- [ ] Żadne testy nie failują

---

**Szacowany czas realizacji: 8-9 godzin**
**Rzeczywisty czas realizacji: ___ (do wypełnienia)**