# 📊 RAPORT POKRYCIA TESTOWEGO MASZYN LEGACY

## 📈 PODSUMOWANIE WYKONAWCZE

### Ogólne statystyki:
- **Całkowita liczba maszyn Legacy**: 38
  - Machines/: 24 maszyny
  - Features/Payload/: 14 maszyn
- **Maszyny używane w testach**: 26 (68.4%)
- **Maszyny nieużywane**: 12 (31.6%)

### ⚠️ KLUCZOWE ODKRYCIE:
**Wszystkie 12 nieużywanych maszyn Legacy ma swoje odpowiedniki Fluent, które SĄ używane w testach.**

To oznacza, że testy zostały napisane tylko dla wersji Fluent, a nie dla obu wersji API.

## 🔍 SZCZEGÓŁOWA ANALIZA

### ✅ Machines/ - MASZYNY UŻYWANE (12/24 = 50%)

| Maszyna Legacy | Użycie | Uwagi |
|----------------|--------|-------|
| BasicBenchmarkMachine | 1x | Używana w FluentAPI_ComparisonTests |
| ComplexCallbackMachine | 2x | Aktywnie testowana |
| CoreBenchmarkMachine | 10x | Najczęściej używana (testy wydajności) |
| ExceptionCallbackMachine | 2x | Testy obsługi wyjątków |
| FullMultiPayloadMachine | 5x | Testy z wieloma typami payload |
| FullOrderMachine | 7x | Testy pełnego flow |
| GuardedCallbackMachine | 2x | Testy z guards |
| InitialStateMachine | 2x | Testy stanu początkowego |
| InternalTransitionMachineLegacy | 1x | Testy HSM |
| MultipleCallbacksMachine | 2x | Testy wielu callbacków |
| PayloadStateMachine | 1x | Testy z payload |
| WithGuardBenchmarkMachine | 4x | Testy wydajności z guards |

### ⚠️ Machines/ - MASZYNY NIEUŻYWANE (12/24 = 50%)

| Maszyna Legacy | Status | Fluent używany? |
|----------------|--------|-----------------|
| CallbackOrderMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (1x) |
| CaseSensitiveMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (1x) |
| ConflictingNamesMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (2x) |
| InternalOnlyMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (2x) |
| KeywordStateMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (1x) |
| LongNameMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (1x) |
| NoGuardBenchmarkMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (2x) |
| NumericMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (1x) |
| SelfTransitionMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (1x) |
| SingleStateMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (2x) |
| UnicodeMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (1x) |
| UnreachableMachineLegacy | ❌ NIE UŻYWANA | ✅ Tak (1x) |

### ✅ Features/Payload/ - WSZYSTKIE UŻYWANE (14/14 = 100%)

| Maszyna Legacy | Użycie |
|----------------|--------|
| OrderStateMachineLegacy | 2x |
| PaymentMachineLegacy | 2x |
| NotificationMachineLegacy | 1x |
| ProcessingMachineLegacy | 1x |
| MultiPayloadMachineLegacy | 4x |
| OverloadedMachineLegacy | 2x |
| InternalPayloadMachineLegacy | 1x |
| MixedPayloadMachineLegacy | 1x |
| InitialPayloadMachineLegacy | 1x |
| ExitCallbackMachineLegacy | 1x |
| WorkflowMachineLegacy | 1x |
| ConditionalPayloadMachineLegacy | 1x |
| PermittedTriggersMachineLegacy | 1x |
| StrictMultiPayloadMachineLegacy | 1x |

## 🎯 REKOMENDACJE

### Problem:
12 maszyn Legacy w katalogu Machines/ nie ma testów, podczas gdy ich odpowiedniki Fluent są testowane.

### Rozwiązania:
1. **OPCJA A: Dodać testy dla maszyn Legacy**
   - Skopiować istniejące testy Fluent
   - Dostosować do używania maszyn Legacy
   - Zapewnić pełne pokrycie obu API

2. **OPCJA B: Utworzyć testy parametryzowane**
   - Używać Theory/InlineData w xUnit
   - Testować obie wersje (Fluent i Legacy) tym samym kodem
   - Zmniejszyć duplikację kodu

3. **OPCJA C: Zaakceptować obecny stan**
   - Jeśli Legacy API jest w fazie wycofywania
   - Skupić się na testowaniu Fluent API
   - Udokumentować decyzję

## 📋 LISTA ZADAŃ

Jeśli wybrano OPCJĘ A lub B, należy dodać testy dla:
- [ ] CallbackOrderMachineLegacy
- [ ] CaseSensitiveMachineLegacy
- [ ] ConflictingNamesMachineLegacy
- [ ] InternalOnlyMachineLegacy
- [ ] KeywordStateMachineLegacy
- [ ] LongNameMachineLegacy
- [ ] NoGuardBenchmarkMachineLegacy
- [ ] NumericMachineLegacy
- [ ] SelfTransitionMachineLegacy
- [ ] SingleStateMachineLegacy
- [ ] UnicodeMachineLegacy
- [ ] UnreachableMachineLegacy

## 📊 METRYKI KOŃCOWE

| Kategoria | Fluent | Legacy |
|-----------|--------|--------|
| **Machines/** | 24/24 (100%) | 12/24 (50%) |
| **Payload/** | 14/14 (100%) | 14/14 (100%) |
| **RAZEM** | 38/38 (100%) | 26/38 (68.4%) |

## ✨ WNIOSKI

1. **Payload ma pełne pokrycie** - wszystkie maszyny Legacy i Fluent są testowane
2. **Machines ma problem** - tylko 50% maszyn Legacy jest testowanych
3. **Wzorzec jest jasny** - testy zostały napisane głównie dla Fluent API
4. **Decyzja potrzebna** - czy dodać brakujące testy Legacy czy zaakceptować obecny stan