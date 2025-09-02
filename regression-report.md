# 🚨 RAPORT REGRESJI - Fluent API Breaking Changes

## Problem
Modyfikacja Fluent API spowodowała **złamanie kompatybilności wstecznej** w istniejących projektach.

## Zakres wpływu
- **10 plików** z błędną składnią
- **16 wystąpień** problematycznego wzorca
- **2 projekty dotknięte**: FastFsm.Tests, ParserComparison.Tests

## Przyczyna techniczna

### Stary kod (działał przed zmianami):
```csharp
.On(Trigger).GoTo(State).Guard(nameof(Method))
.On(Trigger).GoTo(State).Action(nameof(Method))
```

### Problem:
W nowym API metody `Guard()` i `Action()` zostały przeniesione z `StateBuilder` do `TransitionBuilder`. Dodatkowo, `GoTo()` zwraca teraz `StateBuilder`, nie `TransitionBuilder`.

### Poprawna składnia (nowe API):
```csharp
.On(Trigger).Guard(nameof(Method)).GoTo(State)
.On(Trigger).Action(nameof(Method)).GoTo(State)
```

## Lista dotkniętych plików

### FastFsm.Tests (5 plików):
1. `Machines/WithGuardBenchmarkMachine.cs` - 2 błędy
2. `Machines/CallbackOrderMachine.cs` - 2 błędy  
3. `Machines/SingleStateMachine.cs` - 1 błąd
4. `Machines/SelfTransitionMachine.cs` - 1 błąd
5. `Machines/GuardedCallbackMachineFluentAPI.cs` - 1 błąd

### ParserComparison.Tests (5 plików):
1. `MultiPayloadFluentMachine.cs` - 3 błędy
2. `GuardPayloadFluentMachine.cs` - 1 błąd
3. `GuardAsyncPayloadFluentMachine.cs` - 1 błąd
4. `AsyncPayloadActionFluentMachine.cs` - 2 błędy
5. `AsyncActionFluentMachine.cs` - 2 błędy
6. `AsyncGuardFluentMachine.cs` - 1 błąd

## Analiza pierwotnej przyczyny

### Dlaczego stary kod w ogóle działał?

**HIPOTEZA**: Stary kod najprawdopodobniej **nigdy nie działał poprawnie**. Pliki z błędną składnią były prawdopodobnie:
1. Eksperymentalnymi wersjami Fluent API
2. Nigdy nie były w pełni przetestowane
3. Kompilowały się, ale `Guard()` i `Action()` po `GoTo()` były no-op (nic nie robiły)

### Dowody:
- Brak metod `Guard()` i `Action()` na zwracanym typie z `GoTo()` w starym API
- Parser ignorował te wywołania lub były to metody-zaślepki
- Generowany kod nie zawierał guardów/akcji z tych przejść

## Rozwiązania

### Opcja A: Naprawa wszystkich plików (REKOMENDOWANE)
**Zalety:**
- Spójność z nowym API
- Poprawna semantyka (guard/action PRZED GoTo)
- Czytelniejszy kod

**Wady:**
- Wymaga ręcznej edycji 10 plików
- Ryzyko wprowadzenia błędów

### Opcja B: Dodanie metod compatibility shim
```csharp
public class StateBuilder<TState> 
{
    [Obsolete("Use .On().Guard().GoTo() instead")]
    public StateBuilder<TState> Guard(string name) => this; // no-op
    
    [Obsolete("Use .On().Action().GoTo() instead")]  
    public StateBuilder<TState> Action(string name) => this; // no-op
}
```

**Zalety:**
- Kod się skompiluje
- Stopniowa migracja

**Wady:**
- Ukrywa problem
- Guard/Action nadal nie będą działać
- Wprowadza zamieszanie

### Opcja C: Przywrócenie starego API
**Zalety:**
- Pełna kompatybilność wsteczna

**Wady:**
- Cofa postęp
- Stare API było wadliwe
- Blokuje nowe funkcjonalności

## Rekomendacja

### ⚡ PILNE: Opcja A - Napraw wszystkie pliki

1. **Natychmiast**: Popraw składnię w 10 plikach
2. **Następnie**: Dodaj testy sprawdzające że guard/action faktycznie działają
3. **Długoterminowo**: Rozważ narzędzie do automatycznej migracji

## Przykład migracji

### Przed:
```csharp
.State(StateA)
    .On(Trigger).GoTo(StateB).Guard(nameof(CanTransition)).Action(nameof(OnTransition))
```

### Po:
```csharp
.State(StateA)
    .On(Trigger)
        .Guard(nameof(CanTransition))
        .Action(nameof(OnTransition))
        .GoTo(StateB)
```

## Wnioski

1. **Breaking change był nieunikniony** - stare API było wadliwe
2. **Pliki z błędami prawdopodobnie nie działały** poprawnie wcześniej
3. **Nowe API jest poprawne** semantycznie i składniowo
4. **Migracja jest prosta** ale wymaga ręcznej interwencji

## Akcje do podjęcia

- [ ] Zdecydować o strategii (A/B/C)
- [ ] Jeśli A: naprawić 10 plików
- [ ] Dodać testy regresji
- [ ] Zaktualizować dokumentację migracji
- [ ] Rozważyć semantic versioning (major bump)