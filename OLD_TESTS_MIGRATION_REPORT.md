# 📋 RAPORT MIGRACJI TESTÓW Z OLD.TESTS

## 🔍 PODSUMOWANIE ANALIZY

### Statystyki old.tests:
- **Całkowita liczba plików .cs**: 55
- **Maszyny Legacy**: 24 (wszystkie w wersji Legacy API)
- **Pliki testowe**: 31
- **Maszyny używane w testach**: 23 z 24 (95.8%)
- **Maszyny nieużywane**: 1 (PayloadStateMachine)

### Kluczowe odkrycie:
**12 maszyn Legacy które NIE mają testów w FastFsm.Tests MAJĄ testy w old.tests!**

## 🎯 TESTY DO MIGRACJI

### PRIORYTET WYSOKI - Brakujące testy Legacy

Te maszyny nie mają testów Legacy w FastFsm.Tests, ale mają je w old.tests:

| Maszyna | Test w old.tests | Status w FastFsm.Tests |
|---------|------------------|------------------------|
| CallbackOrderMachine | StateCallbackTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| CaseSensitiveMachine | NameCollisionTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| ConflictingNamesMachine | NameCollisionTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| InternalOnlyMachine | EmptyMachineTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| KeywordStateMachine | NameCollisionTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| LongNameMachine | NameCollisionTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| NoGuardBenchmarkMachine | BenchmarkTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| NumericMachine | NameCollisionTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| SelfTransitionMachine | StateCallbackTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| SingleStateMachine | EmptyMachineTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| UnicodeMachine | NameCollisionTests.cs | ✅ Fluent testowany, ❌ Legacy nie |
| UnreachableMachine | EmptyMachineTests.cs | ✅ Fluent testowany, ❌ Legacy nie |

## 📁 PLIKI TESTOWE DO ROZWAŻENIA

### 1. StateCallbackTests.cs
- **Lokalizacja**: old.tests/Features/Core/
- **Maszyny testowane**: CallbackOrderMachine, SelfTransitionMachine, InternalTransitionMachine, ComplexCallbackMachine, MultipleCallbacksMachine, GuardedCallbackMachine, ExceptionCallbackMachine, InitialStateMachine
- **Status**: W FastFsm.Tests używa tylko wersji Fluent
- **Rekomendacja**: Dodać testy Legacy lub utworzyć testy parametryzowane

### 2. NameCollisionTests.cs
- **Lokalizacja**: old.tests/Features/EdgeCases/
- **Maszyny testowane**: KeywordStateMachine, ConflictingNamesMachine, UnicodeMachine, LongNameMachine, NumericMachine, CaseSensitiveMachine
- **Status**: W FastFsm.Tests używa tylko wersji Fluent
- **Rekomendacja**: Dodać testy Legacy lub utworzyć testy parametryzowane

### 3. EmptyMachineTests.cs
- **Lokalizacja**: old.tests/Features/EdgeCases/
- **Maszyny testowane**: SingleStateMachine, UnreachableMachine, InternalOnlyMachine
- **Status**: W FastFsm.Tests używa tylko wersji Fluent
- **Rekomendacja**: Dodać testy Legacy lub utworzyć testy parametryzowane

### 4. BenchmarkTests.cs
- **Lokalizacja**: old.tests/Features/Performance/
- **Maszyny testowane**: CoreBenchmarkMachine, BasicBenchmarkMachine, NoGuardBenchmarkMachine, WithGuardBenchmarkMachine
- **Status**: Częściowo przeniesione (Core i With używane, NoGuard i Basic nie dla Legacy)
- **Rekomendacja**: Uzupełnić brakujące testy Legacy

## 🛠️ STRATEGIA MIGRACJI

### OPCJA A: Minimalna migracja (Rekomendowana)
1. Skopiować maszyny Legacy z old.tests/Machines/ do FastFsm.Tests/Machines/ (już zrobione - pliki .Legacy.cs)
2. Utworzyć pliki testowe .Legacy.cs obok istniejących testów .Fluent.cs
3. Skopiować testy z old.tests zmieniając tylko namespace
4. Uruchomić testy i poprawić ewentualne problemy

### OPCJA B: Testy parametryzowane
1. Zmodyfikować istniejące testy aby używały Theory zamiast Fact
2. Parametryzować typ maszyny (Fluent/Legacy)
3. Jeden test obsługuje obie implementacje

### OPCJA C: Duplikacja testów
1. Utworzyć osobne klasy testowe dla Legacy (np. StateCallbackTestsLegacy)
2. Skopiować testy zmieniając nazwy maszyn
3. Utrzymywać dwa zestawy testów

## 📝 PRZYKŁAD MIGRACJI

### Przed (old.tests):
```csharp
// old.tests/Features/Core/StateCallbackTests.cs
var machine = new Machines.CallbackOrderMachine(CallbackState.A);
```

### Po migracji do FastFsm.Tests:

#### Opcja 1 - Osobny test Legacy:
```csharp
// FastFsm.Tests/Features/Core/StateCallbackTests.Legacy.cs
var machine = new Machines.CallbackOrderMachineLegacy(CallbackState.A);
```

#### Opcja 2 - Test parametryzowany:
```csharp
[Theory]
[InlineData(typeof(CallbackOrderMachineFluent))]
[InlineData(typeof(CallbackOrderMachineLegacy))]
public void OnEntryOnExit_ExecutionOrder_IsCorrect(Type machineType)
{
    var machine = (IStateMachine<CallbackState, CallbackTrigger>)
        Activator.CreateInstance(machineType, CallbackState.A);
    // reszta testu
}
```

## ✅ ZALETY MIGRACJI

1. **Pełne pokrycie testowe** - oba API będą w pełni przetestowane
2. **Wykrywanie regresji** - łatwiejsze znajdowanie problemów w Legacy API
3. **Spójność** - pewność że oba API działają identycznie
4. **Dokumentacja** - testy służą jako przykłady użycia

## ⚠️ WYZWANIA

1. **Duplikacja kodu** - więcej kodu do utrzymania
2. **Czas wykonania testów** - dłuższe testy (x2 jeśli duplikacja)
3. **Maintenance** - konieczność aktualizacji obu zestawów testów

## 🎯 REKOMENDACJA KOŃCOWA

**Przenieść testy dla 12 brakujących maszyn Legacy** używając OPCJI A (minimalna migracja):
1. Utworzyć pliki .Legacy.cs dla testów
2. Skopiować odpowiednie testy z old.tests
3. Zmienić nazwy maszyn na wersje z sufiksem Legacy
4. Uruchomić i zweryfikować

To zapewni 100% pokrycie testowe dla obu API przy minimalnym nakładzie pracy.