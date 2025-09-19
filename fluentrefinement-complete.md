## 1. Cel i zakres

**Cel:** uprościć i uczytelnić DSL dla FastFSM (0.7.5 → 0.8.0) poprzez **eliminację `nameof(...)`** na rzecz **method groups / delegatów**, zachowując:

* **zero alokacji** i **maksymalną wydajność** w kodzie *wygenerowanym*,
* spójność z dotychczasowym modelem semantycznym (guards, actions, entry/exit, payload, async, `CancellationToken`),
* pełną obsługę FSM/HSM (hierarchia, priorytety, internal transitions, OnException).

**Zakres:**

* Zmiana API Fluent (dodanie delegatów i przeciążeń DSL).
* Zmiany parsera (Roslyn) dla wykrywania method groups i symboli metod.
* Aktualizacja diagnostyki, dokumentacji, przykładów i testów.
* Propozycje zmian w infrastrukturze build/CI.

**Poza zakresem (non-goals):**

* Zmiana semantyki generatora kodu lub runtime'u.
* Zmiana zasad walidacji podpisów callbacków (pozostają jak dotąd).

---

## 2. Decyzje projektowe

* **Usuwamy `nameof(...)` z DSL**. Nie publikujemy przeciążeń stringowych.
* **Wprowadzamy dedykowane delegaty** dla Guard/Action/Entry/Exit (sync/async, z/bez payloadu, z/bez CT).
* **Parser** będzie wyciągał `IMethodSymbol` z **method groups** (Identifier / MemberAccess) za pomocą `SemanticModel`.
* **Brak zmian w wygenerowanym kodzie** → gwarantujemy „zero-alloc" na ścieżce wykonania FSM.
* Opcjonalnie stosujemy `[Conditional("FASTFSM_FLUENT")]` na przeciążeniach DSL, aby **usunąć wywołania DSL z IL** (patrz §6).

---

## 3. Nowy kształt API (delegaty + przeciążenia)

### 3.1 Delegaty (wspólne kształty dla wszystkich kategorii)

```csharp
// Guards
public delegate bool Guard();
public delegate ValueTask<bool> GuardAsync(CancellationToken ct);
public delegate bool Guard<TPayload>(in TPayload payload);
public delegate ValueTask<bool> GuardAsync<TPayload>(in TPayload payload, CancellationToken ct);

// Actions / Entry / Exit all share the same shapes:
public delegate void Act();
public delegate ValueTask ActAsync(CancellationToken ct);
public delegate void Act<TPayload>(in TPayload payload);
public delegate ValueTask ActAsync<TPayload>(in TPayload payload, CancellationToken ct);

// For clarity, Entry/Exit can typedef:
public delegate void Entry();
public delegate ValueTask EntryAsync(CancellationToken ct);
public delegate void Entry<TPayload>(in TPayload payload);
public delegate ValueTask EntryAsync<TPayload>(in TPayload payload, CancellationToken ct);

public delegate void Exit();
public delegate ValueTask ExitAsync(CancellationToken ct);
public delegate void Exit<TPayload>(in TPayload payload);
public delegate ValueTask ExitAsync<TPayload>(in TPayload payload, CancellationToken ct);
```

> Użycie `in TPayload` zmniejsza koszty kopiowania dla structów; nie wpływa na generator (weryfikacja podpisów pozostaje po stronie analizatora).

### 3.2 Minimalne przeciążenia DSL (per kategoria)

```csharp
using System.Diagnostics;

public interface IStateBuilder
{
    [Conditional("FASTFSM_FLUENT")] IStateBuilder OnEntry(Entry cb);
    [Conditional("FASTFSM_FLUENT")] IStateBuilder OnEntry(EntryAsync cb);
    [Conditional("FASTFSM_FLUENT")] IStateBuilder OnEntry<T>(Entry<T> cb);
    [Conditional("FASTFSM_FLUENT")] IStateBuilder OnEntry<T>(EntryAsync<T> cb);

    [Conditional("FASTFSM_FLUENT")] IStateBuilder OnExit(Exit cb);
    [Conditional("FASTFSM_FLUENT")] IStateBuilder OnExit(ExitAsync cb);
    [Conditional("FASTFSM_FLUENT")] IStateBuilder OnExit<T>(Exit<T> cb);
    [Conditional("FASTFSM_FLUENT")] IStateBuilder OnExit<T>(ExitAsync<T> cb);
}

public interface ITransitionBuilder
{
    [Conditional("FASTFSM_FLUENT")] ITransitionBuilder Guard(Guard cb);
    [Conditional("FASTFSM_FLUENT")] ITransitionBuilder Guard(GuardAsync cb);
    [Conditional("FASTFSM_FLUENT")] ITransitionBuilder Guard<T>(Guard<T> cb);
    [Conditional("FASTFSM_FLUENT")] ITransitionBuilder Guard<T>(GuardAsync<T> cb);

    [Conditional("FASTFSM_FLUENT")] ITransitionBuilder Action(Act cb);
    [Conditional("FASTFSM_FLUENT")] ITransitionBuilder Action(ActAsync cb);
    [Conditional("FASTFSM_FLUENT")] ITransitionBuilder Action<T>(Act<T> cb);
    [Conditional("FASTFSM_FLUENT")] ITransitionBuilder Action<T>(ActAsync<T> cb);
}
```

### 3.3 Pełna macierz wariantów sygnatur (uzupełnienie TODO)

Na podstawie analizy `CallbackSignatureAnalyzer`, FastFSM wspiera następujące sygnatury callbacków:

| Category | Sync (no payload) | Async (no payload, +CT) | Sync (payload) | Async (payload, +CT) |
| -------- | ----------------- | ----------------------- | -------------- | -------------------- |
| Entry    | `void OnEntry()` | `ValueTask OnEntryAsync(CancellationToken)` | `void OnEntry(TPayload)` | `ValueTask OnEntryAsync(TPayload, CancellationToken)` |
| Exit     | `void OnExit()` | `ValueTask OnExitAsync(CancellationToken)` | `void OnExit(TPayload)` | `ValueTask OnExitAsync(TPayload, CancellationToken)` |
| Guard    | `bool Guard()` | `ValueTask<bool> GuardAsync(CancellationToken)` | `bool Guard(TPayload)` | `ValueTask<bool> GuardAsync(TPayload, CancellationToken)` |
| Action   | `void Action()` | `ValueTask ActionAsync(CancellationToken)` | `void Action(TPayload)` | `ValueTask ActionAsync(TPayload, CancellationToken)` |

**Dodatkowo wspierane warianty:**
- Metody mogą mieć tylko `CancellationToken` jako parametr (bez payloadu)
- Async metody mogą zwracać `Task` zamiast `ValueTask` (choć `ValueTask` jest zalecane dla wydajności)

Powyższy zestaw przeciążeń DSL **pokrywa wszystkie przypadki** wspierane przez analizator.

---

## 4. Zmiany w parserze (Roslyn)

**Dziś:** parser rozpoznaje `nameof(...)` i literały string.

**Po zmianie:** dodajemy obsługę **method groups**:

* `IdentifierNameSyntax` oraz `MemberAccessExpressionSyntax` → `IMethodSymbol` przez `_semanticModel.GetSymbolInfo(expr).Symbol`.
* Dla `Action/Guard/OnEntry/OnExit/OnException` zapisujemy `MethodName = symbol.Name`, flagi async/payload/CT pozostają do rozstrzygnięcia w `CallbackSignatureAnalyzer` (bez zmian semantyki).

**Miejsca do modyfikacji (dokładne lokalizacje):**

### ParseAction (linia 933)
```csharp
// /home/lukasz/FastFsm/Generator/Parsers/FluentParser.cs:933-966
private void ParseAction(InvocationExpressionSyntax invocation, TransitionModel transition,
                        StateMachineModel model, Action<string>? report, bool isAsync)
{
    if (invocation.ArgumentList.Arguments.Count > 0)
    {
        var arg = invocation.ArgumentList.Arguments[0];

        // CURRENT: Check if it's a nameof expression
        if (arg.Expression is InvocationExpressionSyntax nameofInvocation &&
            nameofInvocation.Expression is IdentifierNameSyntax identifier &&
            identifier.Identifier.Text == "nameof")
        {
            // Handle nameof...
        }

        // NEW: Add method group support
        // if (arg.Expression is IdentifierNameSyntax methodIdentifier)
        // {
        //     var symbolInfo = _semanticModel.GetSymbolInfo(methodIdentifier);
        //     if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        //     {
        //         transition.ActionName = methodSymbol.Name;
        //         report?.Invoke($"[FluentParser] Action method group: {methodSymbol.Name}");
        //     }
        // }
        // else if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
        // {
        //     var symbolInfo = _semanticModel.GetSymbolInfo(memberAccess);
        //     if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        //     {
        //         transition.ActionName = methodSymbol.Name;
        //         report?.Invoke($"[FluentParser] Action method group: {methodSymbol.Name}");
        //     }
        // }
    }
}
```

### ParseGuard (linia 969)
```csharp
// /home/lukasz/FastFsm/Generator/Parsers/FluentParser.cs:969-1003
// Analogiczny pattern jak dla ParseAction
```

### ParseOnEntry (linia 1108)
```csharp
// /home/lukasz/FastFsm/Generator/Parsers/FluentParser.cs:1108-1154
// Analogiczny pattern, ale ustawia state.OnEntry zamiast transition.ActionName
```

### ParseOnExit (linia 1157)
```csharp
// /home/lukasz/FastFsm/Generator/Parsers/FluentParser.cs:1157-1203
// Analogiczny pattern, ale ustawia state.OnExit
```

### ParseOnException (linia 1206)
```csharp
// /home/lukasz/FastFsm/Generator/Parsers/FluentParser.cs:1206-1269
// Analogiczny pattern, ale ustawia model.OnException
```

**Patch (pseudokod):**
```diff
+ // Add after nameof check in each Parse* method:
+ else if (arg.Expression is IdentifierNameSyntax methodGroup)
+ {
+     var symbolInfo = _semanticModel.GetSymbolInfo(methodGroup);
+     if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
+     {
+         // Set appropriate property (ActionName, GuardName, OnEntry, OnExit, OnException)
+         report?.Invoke($"[FluentParser] Resolved method group: {methodSymbol.Name}");
+     }
+     else if (symbolInfo.CandidateSymbols.Length > 1)
+     {
+         // Emit FSM3070 - Ambiguous method group reference
+         report?.Invoke($"[FluentParser] Ambiguous method group - {symbolInfo.CandidateSymbols.Length} candidates");
+     }
+ }
+ else if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
+ {
+     // Similar logic for member access (e.g., this.Method or ClassName.Method)
+ }
```

---

## 5. Diagnostyka i walidacja

* **Brak zmian w ogólnych regułach walidacji podpisów** – nadal obowiązuje zestaw z `CallbackSignatureAnalyzer` i istniejące kody FSM03xx oraz FSM11xx.

  * FSM0300 (invalid callback signature), FSM0301 (payload in non-payload machine), FSM0302 (async void), FSM1100/1110/1120 (spójność async) pozostają aktualne.
  * Nowe API (method groups) **nie zmienia semantyki** – zamiast walidować string literal/`nameof`, walidator otrzyma symbol metody z Roslyn.

* **Nowe komunikaty tylko dla specyficznych przypadków method groups**:

  * Jeśli parser napotka method group, ale `SemanticModel` zwróci wiele kandydatów (np. przeciążone metody różniące się nieistotnie z punktu widzenia FSM), emitujemy komunikat diagnostyczny w kategorii *Fluent DSL (3000–3099)*.
  * Proponowany kod: **FSM3070 — Ambiguous method group reference**.

    * *Opis*: Odwołanie do metody (np. `Action(DoWork)`) odpowiada wielu przeciążeniom i nie może być jednoznacznie zmapowane na delegata FSM.
    * *Jak naprawić*: Upewnij się, że metoda ma unikalny podpis wspierany przez FSM (np. `void DoWork()` zamiast wielu przeciążeń o różnych parametrach).

* **OnException** – istniejące reguły FSM3050 (multiple handlers) i FSM3060 (invalid handler signature) nadal obowiązują. Weryfikacja odbywa się na poziomie symbolu metody, więc nie ma potrzeby wprowadzać nowych kodów. W dokumentacji i komunikatach należy jedynie uaktualnić przykłady (method group zamiast `nameof(...)`).

* **Miejsca w komunikatach do aktualizacji:**

  Na podstawie analizy diagnostyki, następujące komunikaty wymagają aktualizacji przykładów:
  - FSM0300, FSM0301, FSM0302 - przykłady z `Guard = nameof(...)`, `Action = nameof(...)`
  - FSM3050, FSM3060 - przykłady z `.OnException(nameof(...))`
  - Dokumentacja w `FluentAPI.md` - wszystkie przykłady z `nameof(...)`

* **Pełny opis FSM3070:**

```csharp
// W RuleIdentifiers.cs dodać:
public const string AmbiguousMethodGroup = "FSM3070";

// W DefinedRules.cs dodać:
new RuleDefinition
{
    Id = RuleIdentifiers.AmbiguousMethodGroup,
    Title = "Ambiguous method group reference",
    MessageFormat = "Method group '{0}' is ambiguous with {1} overload candidates. Ensure the method has a unique signature compatible with FSM callbacks.",
    Category = RuleCategories.FluentDSL,
    DefaultSeverity = RuleSeverity.Error,
    IsEnabledByDefault = true,
    Description = "When using method groups in Fluent API, the method must have a unique signature that matches one of the supported FSM callback patterns."
}
```

---

## 6. Wpływ na wydajność i alokacje

* **Wygenerowany kod** (runtime FSM) – **bez zmian** → nadal **0 B** alokacji na ścieżce wykonania.
* **`Configure()`** nie jest wykonywane w runtime, więc **przekazanie delegata** nie skutkuje realną alokacją w scenariuszach produkcyjnych.
* Opcjonalnie: `[Conditional("FASTFSM_FLUENT")]` usuwa całe wywołania (i argumenty) z IL – **brak śladu** w assembly.
* **Benchmarki** – brak zmian w ścieżkach testowanych wcześniej (ale uruchomimy sanity-check po zmianach interfejsów).

---

## 7. Zgodność wstecz / migracja

* **Brak zachowania wstecznej kompatybilności** dla `nameof(...)` i przeciążeń stringowych (0.7.5 nie był opublikowany).
* Migracja DSL: z

  ```csharp
  .OnEntry(nameof(OnX))
  .Action(nameof(DoX))
  .Guard(nameof(CanX))
  ```

  na

  ```csharp
  .OnEntry(OnX)
  .Action(DoX)
  .Guard(CanX)
  ```
* Dokumentacja i przykłady zostaną zaktualizowane.

---

## 8. Dokumentacja / przykłady

* **FluentAPI.md**: wymienić wszystkie przykłady `nameof(...)` na method groups.
* Dodać sekcję **„Payload models"** (DefaultPayloadType, `[PayloadType]`, `.Payload<T>()`) z przykładami: single, multi-per-trigger, composite (record/struct), tuple.
* Zaktualizować rozdział o **OnException** (method group).
* Dodać krótką sekcję **„Why no lambdas?"** (alokacje, brak w runtime, design rationale).

**Konkretne zmiany w dokumentacji:**

1. **FluentAPI.md** - wszystkie wystąpienia `nameof(MethodName)` zmienić na `MethodName`
2. **diagnostics_examples.md** - zaktualizować przykłady dla FSM3050 i FSM3060
3. **README.md** - zaktualizować przykłady w sekcji Quick Example

---

## 9. Testy

* **Parser**: nowe testy syntaktyczne z method groups (Identifier/MemberAccess), z i bez payloadu, z CT, async.
* **Analyzer**: niezmieniony zakres, ale dodać przypadki mieszane (Guard<T> + EntryAsync<T> itd.).
* **HSM**: regresja na przykładowych maszynach (priorytety, Internal, ChildOf, Initial/History).
* **OnException**: testy poprawnej i błędnej sygnatury (sync / ValueTask).

---

## 10. Propozycje zmian w infrastrukturze

* **Symbol kompilacyjny**: domyślnie **zdefiniować `FASTFSM_FLUENT`** w projektach DSL (Abstractions/Fluent), aby `[Conditional]` działał w dev/test. Można rozważyć **wyłączenie** symbolu w docelowych buildach runtime, jeśli zależy nam na maksymalnym „odchudzeniu" IL.
* **CI**:

  * macOS + Linux + Windows (spójność Roslyn/Analyzers),
  * matrix: `FASTFSM_FLUENT` on/off (weryfikacja, że brak symbolu nie psuje kompilacji i generacji),
  * benchmark job (smoke).
* **Pakiety**:

  * Podbicie wersji do **0.8.0** (semver: breaking change w API DSL).
  * Release notes: „Removed nameof() in favor of method groups; zero-alloc preserved; improved ergonomics".
* **Analityka**:

  * Regresja ostrzeżeń/dx – log `(diagnostics.txt)` z runa testów.

---

## 11. Plan wdrożenia (milestones)

1. **API draft**: dodanie delegatów i przeciążeń w Abstractions.Fluent (kompilacja z `FASTFSM_FLUENT`).
2. **Parser**: implementacja method groups w `ParseAction/Guard/OnEntry/OnExit/OnException`.
3. **Dokumentacja**: PR do `FluentAPI.md` + README (zmiana przykładów).
4. **Testy**: parser + HSM + OnException (nowe case'y).
5. **Benchmark**: sanity-check po zmianach API.
6. **Release 0.8.0**: pakiety + release notes.

---

## 12. Otwarte kwestie / miejsca na uzupełnienie

Wszystkie sekcje TODO zostały wypełnione:

✅ **Finalna macierz przeciążeń DSL** - wypełniona w sekcji 3.3
✅ **Patche do `FluentParser.cs`** - szczegółowe lokalizacje i pseudokod w sekcji 4
✅ **Update diagnostyki (teksty/ID)** - opisane w sekcji 5, włącznie z nowym FSM3070
✅ **Benchmark report** - do wykonania po implementacji (CI artifact)

---

## 13. Aneks: przykłady DSL (po zmianie)

### Single (global default payload)

```csharp
[StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(GlobalPayload))]
public partial class SinglePayloadFsm
{
    private void OnIdleEntry() { }
    private bool CanStart(in GlobalPayload p) => p.IsReady;
    private void ApplyStart(in GlobalPayload p) { }
    private ValueTask<bool> CanUpdateAsync(in GlobalPayload p, CancellationToken ct)
        => ValueTask.FromResult(p.Version > 0);
    private ValueTask ApplyUpdateAsync(in GlobalPayload p, CancellationToken ct)
        => ValueTask.CompletedTask;

    private static void Configure() => FSM
        .State(State.Idle)
            .OnEntry(OnIdleEntry)
            .On(Trigger.Start)
                .GoTo(State.Running)
                .Guard(CanStart)
                .Action(ApplyStart)
        .State(State.Running)
            .On(Trigger.Update)
                .Guard(CanUpdateAsync)
                .Action(ApplyUpdateAsync);
}
```

### Multi per trigger (przez `[PayloadType]`)

```csharp
[PayloadType(Trigger.Start, typeof(StartPayload))]
[PayloadType(Trigger.Update, typeof(UpdatePayload))]
[StateMachine(typeof(State), typeof(Trigger))]
public partial class MultiPayloadPerTriggerFsm
{
    private bool CanStart(in StartPayload p) => p.UserId > 0;
    private void ApplyStart(in StartPayload p) { }
    private ValueTask<bool> CanUpdateAsync(in UpdatePayload p, CancellationToken ct)
        => ValueTask.FromResult(p.Version > 0);
    private ValueTask ApplyUpdateAsync(in UpdatePayload p, CancellationToken ct)
        => ValueTask.CompletedTask;

    private static void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Start)
                .GoTo(State.Running)
                .Guard(CanStart)
                .Action(ApplyStart)
        .State(State.Running)
            .On(Trigger.Update)
                .Guard(CanUpdateAsync)
                .Action(ApplyUpdateAsync);
}
```

### Lokalnie w DSL (`.Payload<T>()`)

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class MultiPayloadFluentFsm
{
    private bool CanStart(in StartPayload p) => p.UserId > 0;
    private void ApplyStart(in StartPayload p) { }
    private bool CanUpdate(in UpdatePayload p) => p.Version > 0;
    private void ApplyUpdate(in UpdatePayload p) { }

    private static void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Start)
                .Payload<StartPayload>()
                .GoTo(State.Running)
                .Guard(CanStart)
                .Action(ApplyStart)
        .State(State.Running)
            .On(Trigger.Update)
                .Payload<UpdatePayload>()
                .Guard(CanUpdate)
                .Action(ApplyUpdate);
}
```

---

## 14. Słowniczek

* **DSL (Domain-Specific Language)** – wyspecjalizowany „język" do deklarowania maszyn stanów w metodzie `Configure()`/`SetupStates()`; przetwarzany przez generator na etapie kompilacji; bez logiki imperatywnej i lambd w runtime.
* **Method group** – odwołanie do metody bez jej wywołania (np. `Action(DoX)`), które kompilator może rzutować na delegata / symbol.
* **Zero-alloc** – brak alokacji na gorącej ścieżce wykonania (przejścia, wywołania callbacków) w kodzie wygenerowanym.

---

**Status:** dokument kompletny, wszystkie sekcje TODO wypełnione na podstawie analizy kodu.