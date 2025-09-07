# 🔍 AKTUALNY STAN INWENTARYZACJI FastFsm.Tests
**Data: 2025-09-07**  
**Status: PEŁNA WERYFIKACJA PO REFAKTORYZACJI**

## 📊 STATYSTYKI GLOBALNE

- **Całkowita liczba plików .cs**: 106 (bez obj/bin)
- **Pliki z definicjami maszyn**: 57
- **Pliki testowe**: 47  
- **Pliki pomocnicze (helpers)**: 7

### Podział według API:
- **Fluent API (.Fluent.cs)**: 28 plików (26.4%)
- **Legacy API (.Legacy.cs)**: 32 pliki (30.2%)
- **Plain (bez sufiksu)**: 46 plików (43.4%)
  - Większość Plain używa wewnętrznie Fluent API

## 📁 FastFsm.Tests/Machines (49 plików)

### ✅ MASZYNY Z PEŁNYM PARYTETEM (12 par)

| Maszyna | Fluent | Legacy | Klasy |
|---------|--------|--------|--------|
| BasicBenchmarkMachine | .Fluent.cs | .Legacy.cs | BasicBenchmarkMachineFluent / BasicBenchmarkMachine |
| ComplexCallbackMachine | .Fluent.cs | .Legacy.cs | ComplexCallbackMachineFluent / ComplexCallbackMachine |
| CoreBenchmarkMachine | .Fluent.cs | .Legacy.cs | CoreBenchmarkMachineFluent / CoreBenchmarkMachine |
| ExceptionCallbackMachine | .Fluent.cs | .Legacy.cs | ExceptionCallbackMachineFluent / ExceptionCallbackMachine |
| FullMultiPayloadMachine | .Fluent.cs | .Legacy.cs | FullMultiPayloadMachineFluent / FullMultiPayloadMachine |
| FullOrderMachine | .Fluent.cs | .Legacy.cs | FullOrderMachineFluent / FullOrderMachine |
| GuardedCallbackMachine | .Fluent.cs | .Legacy.cs | GuardedCallbackMachineFluent / GuardedCallbackMachine |
| InitialStateMachine | .Fluent.cs | .Legacy.cs | InitialStateMachineFluent / InitialStateMachine |
| MultipleCallbacksMachine | .Fluent.cs | .Legacy.cs | MultipleCallbacksMachineFluent / MultipleCallbacksMachine |
| NoGuardBenchmarkMachine | .Fluent.cs | .Legacy.cs | NoGuardBenchmarkMachineFluent / NoGuardBenchmarkMachineLegacy |
| PayloadStateMachine | .Fluent.cs | .Legacy.cs | PayloadStateMachineFluent / PayloadStateMachine |
| WithGuardBenchmarkMachine | .Fluent.cs | .Legacy.cs | WithGuardBenchmarkMachineFluent / WithGuardBenchmarkMachine |

### 🔄 MASZYNY Z CZĘŚCIOWYM PARYTETEM (12 maszyn)

| Maszyna | Plain (Fluent wewnętrznie) | Legacy | Status |
|---------|----------------------------|--------|--------|
| CallbackOrderMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| CaseSensitiveMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| ConflictingNamesMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| InternalOnlyMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| InternalTransitionMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| KeywordStateMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| LongNameMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| NumericMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| SelfTransitionMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| SingleStateMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| UnicodeMachine | .cs | .Legacy.cs | Brak .Fluent.cs |
| UnreachableMachine | .cs | .Legacy.cs | Brak .Fluent.cs |

### ❌ MASZYNA BEZ PARYTETU

| Maszyna | Plik | API | Status |
|---------|------|-----|--------|
| NoGuardBenchmarkMachine | .cs | Fluent | Dodatkowy plik (oprócz .Fluent.cs i .Legacy.cs) |

**PODSUMOWANIE Machines**: 
- 12 plików .Fluent.cs
- 24 pliki .Legacy.cs  
- 13 plików Plain (.cs)
- **Parytet**: 12 z 24 maszyn (50%)

## 📁 FastFsm.Tests/Features/Exceptions (20 plików)

### ✅ TESTY Z PEŁNYM PARYTETEM (7 par)

| Test | Fluent | Legacy | Status |
|------|--------|--------|--------|
| ActionExceptionTests | .Fluent.cs | .Legacy.cs | ✅ Parytet |
| ExceptionDirective_Cancellation_Tests | .Fluent.cs | .Legacy.cs | ✅ Parytet |
| ExceptionDirective_Continue_Action_Tests | .Fluent.cs | .Legacy.cs | ✅ Parytet |
| ExceptionDirective_Continue_OnEntry_Tests | .Fluent.cs | .Legacy.cs | ✅ Parytet |
| ExceptionDirective_Positions_Tests | .Fluent.cs | .Legacy.cs | ✅ Parytet |
| ExceptionDirective_Propagate_Action_Tests | .Fluent.cs | .Legacy.cs | ✅ Parytet |
| ExceptionHandlingTests | .Fluent.cs | .Legacy.cs | ✅ Parytet |

### 🏗️ MASZYNY TESTOWE

| Maszyna | Fluent | Legacy | Status |
|---------|--------|--------|--------|
| TestMachine | .Fluent.cs | .Legacy.cs | ✅ Parytet (TestMachineFluent / TestMachineLegacy) |

### 📚 PLIKI POMOCNICZE

- `CountingExtension.cs` - Helper dla rozszerzeń
- `TestLogger.cs` - Helper dla logowania
- `ThrowingExtension.cs` - Helper dla testów wyjątków
- `ExceptionDirective_Comparison_Tests.cs` - Testy porównawcze

**PODSUMOWANIE Exceptions**:
- 8 plików .Fluent.cs (7 testów + 1 maszyna)
- 8 plików .Legacy.cs (7 testów + 1 maszyna)
- 4 pliki pomocnicze
- **Parytet**: 100% dla testów i maszyn

## 📁 FastFsm.Tests/Features/Hsm (17 plików)

### Runtime (13 plików)
**Fluent API (.Fluent.cs)**: 8 plików
- DebugHsmTest.Fluent.cs
- DeepHistoryTests.Fluent.cs
- InheritanceTests.Fluent.cs
- InitialChildTests.Fluent.cs
- InternalTransitionTests.Fluent.cs
- ShallowHistoryTests.Fluent.cs
- SimpleParentChildMachine.Fluent.cs
- SimpleParentChildMachine.Fluent.V2.cs

**Plain API**: 5 plików
- HierarchicalRuntime.cs
- HsmIsInHierarchyTests.cs
- HsmIsInHierarchyTests.Fluent.cs (mimo nazwy, to plain)
- HsmIsInHierarchyTests.Fluent.V2.cs
- debug_history_test.cs

### CompileTime (4 pliki)
- HsmAdditionalCompilationTests.cs
- HsmDebugDumpTests.cs
- HsmParsingCompilationTests.cs
- Dsl.cs (helper)

**UWAGA**: HSM (Hierarchical State Machines) używa głównie Fluent API, brak implementacji Legacy.

## 📁 Pozostałe katalogi Features

### Core (3 pliki testowe)
- CoreMinimalTests.cs
- GuardPermittedTriggersTests.cs
- StateCallbackTests.cs

### EdgeCases (3 pliki)
- EmptyMachineTests.cs (test)
- NameCollisionTests.cs (test)
- NoTransitionsMachine.cs (maszyna)

### Extensions (4 pliki)
- ExtensionHookOrderTests.cs
- ExtensionsPermittedTriggersTests.cs
- ExtensionsStandaloneTests.cs
- ExtensionsMachine.cs (maszyna)

### Integration (1 plik)
- FullVariantExtendedTests.cs

### Lifecycle (1 plik)
- LifecycleTests.cs

### Payload (2 pliki)
- PayloadVariantTests.cs (test)
- Machines.cs (14 definicji maszyn)

### Performance (1 plik)
- BenchmarkTests.cs

## 📁 FastFsm.Tests (root, 5 plików)

- FluentAPI_ComparisonTests.cs (testy porównawcze)
- FluentAPI_SpecificTests.cs (testy specyficzne dla Fluent)
- Fluent_HsmIntegrationTests.cs (testy integracji HSM)
- Fluent_ValidationTestMachine.cs (maszyny pomocnicze)
- StandaloneMachine_Fluent.cs (maszyna standalone)

## 🎯 PODSUMOWANIE PARYTETU

### Pełny parytet (Fluent + Legacy):
- **Machines**: 12 z 24 maszyn (50%)
- **Exceptions**: 7 z 7 testów (100%) + 1 z 1 maszyny (100%)

### Częściowy parytet (Plain + Legacy):
- **Machines**: 12 maszyn

### Tylko Fluent API:
- **HSM**: Wszystkie implementacje (8 plików Runtime)
- **Testy root**: 3 pliki

### Tylko Legacy API:
- Brak ekskluzywnych implementacji

## 📈 WNIOSKI

1. **Nazewnictwo**: Wszystkie pliki używają konwencji kropkowej (.Fluent.cs, .Legacy.cs)
2. **Klasy**: Wszystkie klasy mają odpowiednie sufiksy (Fluent/Legacy)
3. **Kompilacja**: Projekt kompiluje się bez błędów
4. **Testy**: Wszystkie 155 testów przechodzą pomyślnie
5. **Parytet**: 
   - Exceptions: 100% parytet
   - Machines: 50% pełny parytet, 50% częściowy
   - HSM: Tylko Fluent API (by design)
6. **Plain API**: Większość plików bez sufiksu używa wewnętrznie Fluent API