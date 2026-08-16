# FastFSM Fluent API Refinement - Implementation Status

## STATUS IMPLEMENTACJI (2025-01-19)

### ✅ Zaimplementowane (Iteracja 1/4 - Guards)
- **Delegaty Guard** - wszystkie 4 warianty (sync/async × payload/no-payload)
- **Przeciążenia TransitionBuilder** - obsługa method groups dla Guards
- **Rozszerzenie FluentParser** - rozpoznawanie method groups przez Roslyn SemanticModel
- **Diagnostyka FSM3070** - wykrywanie niejednoznacznych method groups
- **Testy** - kompletny zestaw testów w ParserComparison.Tests/GuardMethodGroupTests.cs
- **Dokumentacja** - zaktualizowana FluentAPI.md z przykładami method groups
- **Kompatybilność wsteczna** - zachowano obsługę `nameof(...)`

### ⏳ Do zrobienia (kolejne iteracje)
- [ ] **Iteracja 2/4: Actions** - method groups dla Action/ActionAsync
- [ ] **Iteracja 3/4: Entry/Exit** - method groups dla OnEntry/OnExit/OnEntryAsync/OnExitAsync
- [ ] **Iteracja 4/4: OnException** - method groups dla OnException
- [ ] **Krok 5: Cleanup** - usunięcie przeciążeń string/nameof po pełnej migracji

---

## 1. Cel i zakres

**Cel:** uprościć i uczytelnić DSL dla FastFSM (0.7.5 → 0.8.0) poprzez **stopniową migrację z `nameof(...)`** na **method groups / delegatów**, zachowując:

* **zero alokacji** i **maksymalną wydajność** w kodzie *wygenerowanym*,
* spójność z dotychczasowym modelem semantycznym (guards, actions, entry/exit, payload, async, `CancellationToken`),
* pełną obsługę FSM/HSM (hierarchia, priorytety, internal transitions, OnException).

**Zakres:**

* Zmiana API Fluent (dodanie delegatów i przeciążeń DSL).
* Zmiany parsera (Roslyn) dla wykrywania method groups i symboli metod.
* Aktualizacja diagnostyki, dokumentacji, przykładów i testów.

**Poza zakresem (non-goals):**

* Zmiana semantyki generatora kodu lub runtime'u.
* Zmiana zasad walidacji podpisów callbacków (pozostają jak dotąd).

---

## 2. Implementacja Guards (COMPLETED ✅)

### 2.1 Dodane delegaty (Abstractions/Fluent/FSM.cs)
```csharp
public delegate bool Guard();
public delegate ValueTask<bool> GuardAsync(CancellationToken ct);
public delegate bool Guard<TPayload>(in TPayload payload);
public delegate ValueTask<bool> GuardAsync<TPayload>(in TPayload payload, CancellationToken ct);
```

### 2.2 Przeciążenia w TransitionBuilder
```csharp
public TransitionBuilder<TState, TTrigger> Guard(Guard guard) => this;
public TransitionBuilder<TState, TTrigger> Guard(GuardAsync guard) => this;
public TransitionBuilder<TState, TTrigger> Guard<TPayload>(Guard<TPayload> guard) => this;
public TransitionBuilder<TState, TTrigger> Guard<TPayload>(GuardAsync<TPayload> guard) => this;
```

Uwaga: Usunięto `[Conditional("FASTFSM_FLUENT")]` ponieważ nie działa z metodami zwracającymi wartość.

### 2.3 Zmiany w FluentParser

Lokalizacja: `/Generator/Parsers/FluentParser.cs`, metoda `ParseGuard` (linia 969)

Dodano obsługę:
- `IdentifierNameSyntax` - proste method groups (np. `CanGo`)
- `MemberAccessExpressionSyntax` - member access (np. `this.CanGo`)
- Rozwiązywanie symboli przez `SemanticModel.GetSymbolInfo()`
- Emisja FSM3070 gdy `CandidateSymbols.Length > 1`

### 2.4 Nowa diagnostyka FSM3070

**Definicja:**
- ID: `FSM3070`
- Tytuł: "Ambiguous method group reference"
- Komunikat: "Method group '{0}' is ambiguous with {1} overload candidates"
- Kategoria: FSM_Generator_Fluent
- Severity: Error

**Lokalizacje:**
- `/Generator.Rules/Definitions/RuleIdentifiers.cs` - dodano stałą
- `/Generator.Rules/Definitions/RuleDefinition.cs` - dodano pełną definicję

---

## 3. Przykłady użycia

### Przed (v0.7.x)
```csharp
.State(State.Idle)
    .On(Trigger.Start)
        .Guard(nameof(CanStart))
        .GoTo(State.Running)
```

### Po (v0.8.0)
```csharp
.State(State.Idle)
    .On(Trigger.Start)
        .Guard(CanStart)  // Method group - czytelniejsze!
        .GoTo(State.Running)
```

### Obsługa niejednoznaczności
```csharp
// Dwa przeciążenia
private bool Validate() => true;
private bool Validate(in OrderData data) => data.Amount > 0;

// Spowoduje FSM3070
.Guard(Validate)  // ERROR: Ambiguous method group

// Rozwiązanie
.Guard(nameof(Validate))  // Używa kontekstu do wyboru właściwego przeciążenia
```

---

## 4. Testy

Lokalizacja: `/ParserComparison.Tests/GuardMethodGroupTests.cs`

Pokrycie:
1. ✅ Synchroniczny guard bez payloadu
2. ✅ Asynchroniczny guard z CancellationToken
3. ✅ Guard z DefaultPayloadType
4. ✅ Guard z .Payload<T>() override
5. ✅ Guard z [PayloadType] per trigger
6. ✅ Kompatybilność wsteczna (nameof)
7. ✅ FSM3070 dla niejednoznacznych method groups
8. ✅ Member access expressions (this.Method)
9. ✅ Generic payloads
10. ✅ Mieszane użycie (method groups + nameof)

---

## 5. Plan dalszych prac

### Iteracja 2: Actions
- Dodać delegaty: `Act`, `ActAsync`, `Act<T>`, `ActAsync<T>`
- Rozszerzyć TransitionBuilder o przeciążenia
- Zaktualizować ParseAction w FluentParser
- Testy i dokumentacja

### Iteracja 3: Entry/Exit
- Dodać delegaty: `Entry`, `EntryAsync`, `Exit`, `ExitAsync` (z wariantami payload)
- Rozszerzyć StateBuilder o przeciążenia
- Zaktualizować ParseOnEntry/ParseOnExit w FluentParser
- Testy i dokumentacja

### Iteracja 4: OnException
- Dodać delegaty dla exception handlers
- Rozszerzyć FSM/StateBuilder o przeciążenia
- Zaktualizować ParseOnException w FluentParser
- Testy i dokumentacja

### Krok 5: Cleanup
- Usunąć przeciążenia string/nameof (breaking change)
- Zaktualizować wszystkie przykłady
- Release notes dla v0.8.0

---

## 6. Wpływ na wydajność

- **Runtime:** BRAK ZMIAN - method groups są rozwiązywane w czasie kompilacji
- **Wygenerowany kod:** IDENTYCZNY - parser ekstraktuje tylko nazwę metody
- **Alokacje:** ZERO - brak delegatów w runtime, tylko metadata w czasie kompilacji
- **Rozmiar IL:** Możliwe zmniejszenie przez usunięcie `[Conditional]` (metody nie są wywoływane)

---

## 7. Kompatybilność

- ✅ Pełna kompatybilność wsteczna w iteracji 1
- ⚠️ Breaking change planowany w kroku 5 (usunięcie nameof)
- 📝 Migracja: prosta zamiana `nameof(X)` → `X`
- 🔧 Niejednoznaczności: FSM3070 wskazuje problemy do rozwiązania

---

## 8. Podsumowanie

Implementacja method groups dla Guards zakończona sukcesem. System działa poprawnie, wykrywa niejednoznaczności, zachowuje kompatybilność wsteczną. Kod jest gotowy do rozszerzenia o kolejne kategorie callbacków w następnych iteracjach.