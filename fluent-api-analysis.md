# Analiza złożoności konwersji na Fluent API - SimpleCancellationMachine

## 1. Struktura oryginalna (atrybuty)

### Kod atrybutowy:
```csharp
[StateMachine(typeof(SimpleStates), typeof(SimpleTriggers))]
public partial class SimpleCancellationMachine
{
    [State(SimpleStates.Ready, OnEntry = nameof(OnEnterReady))]
    [State(SimpleStates.Working)]
    [State(SimpleStates.Done)]
    private void ConfigureStates() { }

    [Transition(SimpleStates.Ready, SimpleTriggers.Start, SimpleStates.Working,
        Guard = nameof(CanStart), Action = nameof(DoStart))]
    [Transition(SimpleStates.Working, SimpleTriggers.Finish, SimpleStates.Done)]
    private void ConfigureTransitions() { }
}
```

### Próba konwersji na Fluent API:
```csharp
private static void Configure() => FSM
    .State(SimpleStates.Ready)
        .OnEntry(nameof(OnEnterReady))
        .On(SimpleTriggers.Start).GoTo(SimpleStates.Working)
            .Guard(nameof(CanStart)).Action(nameof(DoStart))
    .State(SimpleStates.Working)
        .On(SimpleTriggers.Finish).GoTo(SimpleStates.Done)
    .State(SimpleStates.Done);
```

## 2. Problemy strukturalne w obecnym Fluent API

### Problem #1: Brak możliwości kontynuacji po modyfikatorach
**Obecny stan:** Po wywołaniu `.Action()` lub `.Guard()` na `TransitionBuilder`, nie ma metody do zdefiniowania kolejnego przejścia z tego samego stanu.

```csharp
// TO NIE DZIAŁA:
.State(SimpleStates.Ready)
    .OnEntry(nameof(OnEnterReady))
    .On(SimpleTriggers.Start).GoTo(SimpleStates.Working)
        .Guard(nameof(CanStart)).Action(nameof(DoStart))
    .On(SimpleTriggers.AnotherTrigger).GoTo(SimpleStates.Other)  // ❌ BŁĄD!
```

**Przyczyna:** `TransitionBuilder<TState, TTrigger>` nie ma metody `On()` - tylko `State()`.

### Problem #2: Wymuszenie powtarzania stanu
**Obecne obejście:** Trzeba ponownie definiować stan, aby dodać kolejne przejście:

```csharp
.State(SimpleStates.Ready)
    .OnEntry(nameof(OnEnterReady))
    .On(SimpleTriggers.Start).GoTo(SimpleStates.Working)
        .Guard(nameof(CanStart)).Action(nameof(DoStart))
.State(SimpleStates.Ready)  // ⚠️ Powtórzenie!
    .On(SimpleTriggers.AnotherTrigger).GoTo(SimpleStates.Other)
```

To prowadzi do:
- **Duplikacji** definicji stanu
- **Konfliktów** - które OnEntry/OnExit ma pierwszeństwo?
- **Nieczytelności** - stan jest rozbity na wiele miejsc

### Problem #3: Brak separacji konfiguracji stanu od przejść
W atrybutach jest jasny podział:
- `ConfigureStates()` - definicje stanów z OnEntry/OnExit
- `ConfigureTransitions()` - definicje przejść

W Fluent API wszystko jest wymieszane, co utrudnia:
- Przegląd wszystkich stanów
- Przegląd wszystkich przejść
- Debugowanie i utrzymanie kodu

## 3. Szczególne przypadki wymagające uwagi

### A. Stany z wieloma przejściami wychodzącymi
```csharp
// Atrybuty - czytelne:
[Transition(StateA, TriggerX, StateB, Guard = "G1", Action = "A1")]
[Transition(StateA, TriggerY, StateC, Guard = "G2", Action = "A2")]
[Transition(StateA, TriggerZ, StateD, Guard = "G3", Action = "A3")]

// Fluent - problematyczne:
.State(StateA)
    .On(TriggerX).GoTo(StateB).Guard("G1").Action("A1")
    // Jak dodać TriggerY i TriggerZ? 🤔
```

### B. Stany z callback async z CancellationToken
SimpleCancellationMachine używa metod z `CancellationToken ct = default`:
```csharp
private async ValueTask<bool> CanStart(CancellationToken ct = default)
private async Task DoStart(CancellationToken ct = default)
private async Task OnEnterReady(CancellationToken ct = default)
```

Fluent API musi zapewnić, że FluentParser:
1. Wykryje async sygnaturę ✅ (już działa)
2. Rozpozna parametr CancellationToken ⚠️ (do weryfikacji)
3. Oznaczy odpowiednie flagi w modelu

### C. Mieszanie stanów z przejściami wewnętrznymi
```csharp
.State(StateA)
    .OnEntry("Enter")
    .OnInternal(TriggerRefresh).Action("Refresh")  // Przejście wewnętrzne
    .On(TriggerNext).GoTo(StateB)  // ❌ Nie ma jak wrócić do On() po OnInternal()
```

## 4. Propozycje rozwiązań

### Rozwiązanie A: Dodanie metody `On()` do wszystkich builderów
```csharp
public class TransitionBuilder<TState, TTrigger>
{
    // Istniejące metody...
    
    /// <summary>
    /// Define another transition from the same state.
    /// </summary>
    public TransitionBuilder<TState, TTrigger> On(TTrigger trigger) => new TransitionBuilder<TState, TTrigger>();
}
```

**Zalety:** 
- Naturalna kontynuacja łańcucha
- Brak duplikacji stanów

**Wady:**
- Zmiana API może złamać istniejący kod
- Parser musi śledzić kontekst bieżącego stanu

### Rozwiązanie B: Separacja definicji stanów i przejść
```csharp
private static void Configure() => FSM
    // Najpierw wszystkie stany
    .States(states => states
        .Define(SimpleStates.Ready).OnEntry(nameof(OnEnterReady))
        .Define(SimpleStates.Working)
        .Define(SimpleStates.Done)
    )
    // Potem wszystkie przejścia
    .Transitions(transitions => transitions
        .From(SimpleStates.Ready).On(SimpleTriggers.Start).GoTo(SimpleStates.Working)
            .Guard(nameof(CanStart)).Action(nameof(DoStart))
        .From(SimpleStates.Working).On(SimpleTriggers.Finish).GoTo(SimpleStates.Done)
    );
```

**Zalety:**
- Czysta separacja jak w atrybutach
- Łatwiejszy parsing
- Bardziej przewidywalne

**Wady:**
- Większa zmiana API
- Dwa osobne łańcuchy do parsowania

### Rozwiązanie C: Akceptacja powtórzeń z merge w parserze
Pozostawić API jak jest, ale FluentParser powinien:
1. Akumulować wszystkie definicje tego samego stanu
2. Mergować OnEntry/OnExit (z walidacją konfliktów)
3. Zbierać wszystkie przejścia

```csharp
// Parser zobaczy:
.State(Ready).OnEntry("Enter")
.State(Ready).On(T1).GoTo(S1)  // Merge: Ready ma OnEntry + transition T1
.State(Ready).On(T2).GoTo(S2)  // Merge: Ready ma OnEntry + T1 + T2
```

**Zalety:**
- Nie wymaga zmian API
- Działa z obecnym kodem

**Wady:**
- Nieoczywiste dla użytkownika
- Potencjalne konflikty do rozwiązania

## 5. Rekomendacje

### Krótkoterminowe (0.7.5):
1. **Implementować Rozwiązanie C** - akceptować powtórzenia stanów w FluentParser
2. **Dodać walidację** konfliktów (np. dwa różne OnEntry dla tego samego stanu)
3. **Dokumentować** to zachowanie wyraźnie

### Długoterminowe (0.8+):
1. **Rozważyć Rozwiązanie B** - separacja definicji dla czystszego API
2. **Dodać pomocnicze metody** jak `.And()` lub `.Also()` do łączenia przejść
3. **Stworzyć migratory** automatyczny z atrybutów na Fluent

## 6. Podsumowanie

SimpleCancellationMachine ujawnia fundamentalne ograniczenia obecnego Fluent API:
- **Brak kontynuacji** po modyfikatorach przejść
- **Wymuszenie duplikacji** definicji stanów
- **Mieszanie** różnych aspektów konfiguracji

Te problemy są szczególnie widoczne w maszynach z:
- Wieloma przejściami z jednego stanu
- Złożonymi callback (async, CancellationToken)
- Mieszanymi przejściami (normalne + wewnętrzne)

Rekomendowane rozwiązanie krótkoterminowe (merge w parserze) pozwoli na kontynuację prac, ale długoterminowo API wymaga przemyślenia dla lepszej ergonomii.