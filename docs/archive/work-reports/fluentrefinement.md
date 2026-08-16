

\## 1. Cel i zakres



\*\*Cel:\*\* uprościć i uczytelnić DSL dla FastFSM (0.7.5 → 0.8.0) poprzez \*\*eliminację `nameof(...)`\*\* na rzecz \*\*method groups / delegatów\*\*, zachowując:



\* \*\*zero alokacji\*\* i \*\*maksymalną wydajność\*\* w kodzie \*wygenerowanym\*,

\* spójność z dotychczasowym modelem semantycznym (guards, actions, entry/exit, payload, async, `CancellationToken`),

\* pełną obsługę FSM/HSM (hierarchia, priorytety, internal transitions, OnException).



\*\*Zakres:\*\*



\* Zmiana API Fluent (dodanie delegatów i przeciążeń DSL).

\* Zmiany parsera (Roslyn) dla wykrywania method groups i symboli metod.

\* Aktualizacja diagnostyki, dokumentacji, przykładów i testów.

\* Propozycje zmian w infrastrukturze build/CI.



\*\*Poza zakresem (non-goals):\*\*



\* Zmiana semantyki generatora kodu lub runtime’u.

\* Zmiana zasad walidacji podpisów callbacków (pozostają jak dotąd).



---



\## 2. Decyzje projektowe



\* \*\*Usuwamy `nameof(...)` z DSL\*\*. Nie publikujemy przeciążeń stringowych.

\* \*\*Wprowadzamy dedykowane delegaty\*\* dla Guard/Action/Entry/Exit (sync/async, z/bez payloadu, z/bez CT).

\* \*\*Parser\*\* będzie wyciągał `IMethodSymbol` z \*\*method groups\*\* (Identifier / MemberAccess) za pomocą `SemanticModel`.

\* \*\*Brak zmian w wygenerowanym kodzie\*\* → gwarantujemy „zero-alloc” na ścieżce wykonania FSM.

\* Opcjonalnie stosujemy `\[Conditional("FASTFSM\_FLUENT")]` na przeciążeniach DSL, aby \*\*usunąć wywołania DSL z IL\*\* (patrz §6).



---



\## 3. Nowy kształt API (delegaty + przeciążenia)



\### 3.1 Delegaty (wspólne kształty dla wszystkich kategorii)



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



\### 3.2 Minimalne przeciążenia DSL (per kategoria)



```csharp

using System.Diagnostics;



public interface IStateBuilder

{

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] IStateBuilder OnEntry(Entry cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] IStateBuilder OnEntry(EntryAsync cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] IStateBuilder OnEntry<T>(Entry<T> cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] IStateBuilder OnEntry<T>(EntryAsync<T> cb);



&nbsp;   \[Conditional("FASTFSM\_FLUENT")] IStateBuilder OnExit(Exit cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] IStateBuilder OnExit(ExitAsync cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] IStateBuilder OnExit<T>(Exit<T> cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] IStateBuilder OnExit<T>(ExitAsync<T> cb);

}



public interface ITransitionBuilder

{

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] ITransitionBuilder Guard(Guard cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] ITransitionBuilder Guard(GuardAsync cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] ITransitionBuilder Guard<T>(Guard<T> cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] ITransitionBuilder Guard<T>(GuardAsync<T> cb);



&nbsp;   \[Conditional("FASTFSM\_FLUENT")] ITransitionBuilder Action(Act cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] ITransitionBuilder Action(ActAsync cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] ITransitionBuilder Action<T>(Act<T> cb);

&nbsp;   \[Conditional("FASTFSM\_FLUENT")] ITransitionBuilder Action<T>(ActAsync<T> cb);

}

```



> \*\*Uwaga:\*\* To \*\*docelowy minimalny zestaw\*\*. \*\*Miejsce na weryfikację finalnej liczby przeciążeń\*\* (zależnej od faktycznych wariantów akceptowanych przez `CallbackSignatureAnalyzer`):

> \*\*TODO(Agent):\*\* \*Wypełnij tabelę wariantów sygnatur i potwierdź, że powyższy zestaw pokrywa wszystkie przypadki (sync/async, payload/no-payload, z/bez CancellationToken) dla Action/Guard/Entry/Exit. Dodaj brakujące przeciążenia, jeśli istnieją „egzotyczne” sygnatury wspierane dziś przez analizator.\*



---



\## 4. Zmiany w parserze (Roslyn)



\*\*Dziś:\*\* parser rozpoznaje `nameof(...)` i literały string.



\*\*Po zmianie:\*\* dodajemy obsługę \*\*method groups\*\*:



\* `IdentifierNameSyntax` oraz `MemberAccessExpressionSyntax` → `IMethodSymbol` przez `\_semanticModel.GetSymbolInfo(expr).Symbol`.

\* Dla `Action/Guard/OnEntry/OnExit/OnException` zapisujemy `MethodName = symbol.Name`, flagi async/payload/CT pozostają do rozstrzygnięcia w `CallbackSignatureAnalyzer` (bez zmian semantyki).



\*\*Miejsca do modyfikacji (nazwy metod parsera):\*\*



\* `ParseAction`, `ParseGuard`, `ParseOnEntry`, `ParseOnExit`, `ParseOnException`

&nbsp; \*\*TODO(Agent):\*\* \*Wstaw dokładne linki do regionów i linii w `FluentParser.cs` oraz opisz krótkie patche (pseudokod → gotowy diff).\*



---







\## 5. Diagnostyka i walidacja



\* \*\*Brak zmian w ogólnych regułach walidacji podpisów\*\* – nadal obowiązuje zestaw z `CallbackSignatureAnalyzer` i istniejące kody FSM03xx oraz FSM11xx.



&nbsp; \* FSM0300 (invalid callback signature), FSM0301 (payload in non-payload machine), FSM0302 (async void), FSM1100/1110/1120 (spójność async) pozostają aktualne.

&nbsp; \* Nowe API (method groups) \*\*nie zmienia semantyki\*\* – zamiast walidować string literal/`nameof`, walidator otrzyma symbol metody z Roslyn.



\* \*\*Nowe komunikaty tylko dla specyficznych przypadków method groups\*\*:



&nbsp; \* Jeśli parser napotka method group, ale `SemanticModel` zwróci wiele kandydatów (np. przeciążone metody różniące się nieistotnie z punktu widzenia FSM), emitujemy komunikat diagnostyczny w kategorii \*Fluent DSL (3000–3099)\*.

&nbsp; \* Proponowany kod: \*\*FSM3070 — Ambiguous method group reference\*\*.



&nbsp;   \* \*Opis\*: Odwołanie do metody (np. `Action(DoWork)`) odpowiada wielu przeciążeniom i nie może być jednoznacznie zmapowane na delegata FSM.

&nbsp;   \* \*Jak naprawić\*: Upewnij się, że metoda ma unikalny podpis wspierany przez FSM (np. `void DoWork()` zamiast wielu przeciążeń o różnych parametrach).



\* \*\*OnException\*\* – istniejące reguły FSM3050 (multiple handlers) i FSM3060 (invalid handler signature) nadal obowiązują. Weryfikacja odbywa się na poziomie symbolu metody, więc nie ma potrzeby wprowadzać nowych kodów. W dokumentacji i komunikatach należy jedynie uaktualnić przykłady (method group zamiast `nameof(...)`).



\* \*\*Identyfikatory i copy\*\*:



&nbsp; \* Istniejące kody \*\*FSM0100–FSM3060\*\* pozostają bez zmian, ich treść wymaga tylko kosmetycznej korekty w komunikatach i przykładach (`nameof(...)` → method group).

&nbsp; \* \*\*TODO(Agent):\*\* przygotować listę miejsc w komunikatach/plikach lokalizacyjnych, gdzie występuje literalne `nameof`, i zaktualizować na neutralne sformułowania („callback reference” albo „method group”).

&nbsp; \* \*\*TODO(Agent):\*\* dodać pełny opis i testy diagnostyki \*\*FSM3070 — Ambiguous method group reference\*\*.







---



\## 6. Wpływ na wydajność i alokacje



\* \*\*Wygenerowany kod\*\* (runtime FSM) – \*\*bez zmian\*\* → nadal \*\*0 B\*\* alokacji na ścieżce wykonania.

\* \*\*`Configure()`\*\* nie jest wykonywane w runtime, więc \*\*przekazanie delegata\*\* nie skutkuje realną alokacją w scenariuszach produkcyjnych.

\* Opcjonalnie: `\[Conditional("FASTFSM\_FLUENT")]` usuwa całe wywołania (i argumenty) z IL – \*\*brak śladu\*\* w assembly.

\* \*\*Benchmarki\*\* – brak zmian w ścieżkach testowanych wcześniej (ale uruchomimy sanity-check po zmianach interfejsów).





---



\## 7. Zgodność wstecz / migracja



\* \*\*Brak zachowania wstecznej kompatybilności\*\* dla `nameof(...)` i przeciążeń stringowych (0.7.5 nie był opublikowany).

\* Migracja DSL: z



&nbsp; ```csharp

&nbsp; .OnEntry(nameof(OnX))

&nbsp; .Action(nameof(DoX))

&nbsp; .Guard(nameof(CanX))

&nbsp; ```



&nbsp; na



&nbsp; ```csharp

&nbsp; .OnEntry(OnX)

&nbsp; .Action(DoX)

&nbsp; .Guard(CanX)

&nbsp; ```

\* Dokumentacja i przykłady zostaną zaktualizowane.



---



\## 8. Dokumentacja / przykłady



\* \*\*FluentAPI.md\*\*: wymienić wszystkie przykłady `nameof(...)` na method groups.

\* Dodać sekcję \*\*„Payload models”\*\* (DefaultPayloadType, `\[PayloadType]`, `.Payload<T>()`) z przykładami: single, multi-per-trigger, composite (record/struct), tuple.

\* Zaktualizować rozdział o \*\*OnException\*\* (method group).

\* Dodać krótką sekcję \*\*„Why no lambdas?”\*\* (alokacje, brak w runtime, design rationale).



\*\*TODO(Agent):\*\* \*Przygotować PR z aktualizacją dokumentacji i snippetów testowych.\*



---



\## 9. Testy



\* \*\*Parser\*\*: nowe testy syntaktyczne z method groups (Identifier/MemberAccess), z i bez payloadu, z CT, async.

\* \*\*Analyzer\*\*: niezmieniony zakres, ale dodać przypadki mieszane (Guard<T> + EntryAsync<T> itd.).

\* \*\*HSM\*\*: regresja na przykładowych maszynach (priorytety, Internal, ChildOf, Initial/History).

\* \*\*OnException\*\*: testy poprawnej i błędnej sygnatury (sync / ValueTask).



---



\## 10. Propozycje zmian w infrastrukturze



\* \*\*Symbol kompilacyjny\*\*: domyślnie \*\*zdefiniować `FASTFSM\_FLUENT`\*\* w projektach DSL (Abstractions/Fluent), aby `\[Conditional]` działał w dev/test. Można rozważyć \*\*wyłączenie\*\* symbolu w docelowych buildach runtime, jeśli zależy nam na maksymalnym „odchudzeniu” IL.

\* \*\*CI\*\*:



&nbsp; \* macOS + Linux + Windows (spójność Roslyn/Analyzers),

&nbsp; \* matrix: `FASTFSM\_FLUENT` on/off (weryfikacja, że brak symbolu nie psuje kompilacji i generacji),

&nbsp; \* benchmark job (smoke).

\* \*\*Pakiety\*\*:



&nbsp; \* Podbicie wersji do \*\*0.8.0\*\* (semver: breaking change w API DSL).

&nbsp; \* Release notes: „Removed nameof() in favor of method groups; zero-alloc preserved; improved ergonomics”.

\* \*\*Analityka\*\*:



&nbsp; \* Regresja ostrzeżeń/dx – log `(diagnostics.txt)` z runa testów.



---



\## 11. Plan wdrożenia (milestones)



1\. \*\*API draft\*\*: dodanie delegatów i przeciążeń w Abstractions.Fluent (kompilacja z `FASTFSM\_FLUENT`).

2\. \*\*Parser\*\*: implementacja method groups w `ParseAction/Guard/OnEntry/OnExit/OnException`.

3\. \*\*Dokumentacja\*\*: PR do `FluentAPI.md` + README (zmiana przykładów).

4\. \*\*Testy\*\*: parser + HSM + OnException (nowe case’y).

5\. \*\*Benchmark\*\*: sanity-check po zmianach API.

6\. \*\*Release 0.8.0\*\*: pakiety + release notes.



---



\## 12. Otwarte kwestie / miejsca na uzupełnienie



\* \*\*\\\[TODO/Agent] Finalna macierz przeciążeń DSL\*\*

&nbsp; \*Wypełnij tabelę poniżej w oparciu o realne sygnatury wspierane dziś przez `CallbackSignatureAnalyzer`.\*



| Category | Sync (no payload) | Async (no payload, +CT) | Sync (payload) | Async (payload, +CT) |

| -------- | ----------------- | ----------------------- | -------------- | -------------------- |

| Entry    | `Entry`           | `EntryAsync`            | `Entry<T>`     | `EntryAsync<T>`      |

| Exit     | `Exit`            | `ExitAsync`             | `Exit<T>`      | `ExitAsync<T>`       |

| Guard    | `Guard`           | `GuardAsync`            | `Guard<T>`     | `GuardAsync<T>`      |

| Action   | `Act`             | `ActAsync`              | `Act<T>`       | `ActAsync<T>`        |



\* \*\*\\\[TODO/Agent] Patche do `FluentParser.cs`\*\*

&nbsp; \*Linki/zakresy linii i krótkie diffs dla miejsc, gdzie dodajemy rozpoznawanie method groups.\*



\* \*\*\\\[TODO/Agent] Update diagnostyki (teksty/ID)\*\*

&nbsp; \*Czy któreś komunikaty wymagają korekty względem „string name” → „method group”?\*



\* \*\*\\\[TODO/Agent] Benchmark report\*\*

&nbsp; \*Załącz wynik po zmianach API (CI artifact).\*



---



\## 13. Aneks: przykłady DSL (po zmianie)



\### Single (global default payload)



```csharp

\[StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(GlobalPayload))]

public partial class SinglePayloadFsm

{

&nbsp;   private void OnIdleEntry() { }

&nbsp;   private bool CanStart(in GlobalPayload p) => p.IsReady;

&nbsp;   private void ApplyStart(in GlobalPayload p) { }

&nbsp;   private ValueTask<bool> CanUpdateAsync(in GlobalPayload p, CancellationToken ct) 

&nbsp;       => ValueTask.FromResult(p.Version > 0);

&nbsp;   private ValueTask ApplyUpdateAsync(in GlobalPayload p, CancellationToken ct) 

&nbsp;       => ValueTask.CompletedTask;



&nbsp;   private static void Configure() => FSM

&nbsp;       .State(State.Idle)

&nbsp;           .OnEntry(OnIdleEntry)

&nbsp;           .On(Trigger.Start)

&nbsp;               .GoTo(State.Running)

&nbsp;               .Guard(CanStart)

&nbsp;               .Action(ApplyStart)

&nbsp;       .State(State.Running)

&nbsp;           .On(Trigger.Update)

&nbsp;               .Guard(CanUpdateAsync)

&nbsp;               .Action(ApplyUpdateAsync);

}

```



\### Multi per trigger (przez `\[PayloadType]`)



```csharp

\[PayloadType(Trigger.Start, typeof(StartPayload))]

\[PayloadType(Trigger.Update, typeof(UpdatePayload))]

\[StateMachine(typeof(State), typeof(Trigger))]

public partial class MultiPayloadPerTriggerFsm

{

&nbsp;   private bool CanStart(in StartPayload p) => p.UserId > 0;

&nbsp;   private void ApplyStart(in StartPayload p) { }

&nbsp;   private ValueTask<bool> CanUpdateAsync(in UpdatePayload p, CancellationToken ct)

&nbsp;       => ValueTask.FromResult(p.Version > 0);

&nbsp;   private ValueTask ApplyUpdateAsync(in UpdatePayload p, CancellationToken ct)

&nbsp;       => ValueTask.CompletedTask;



&nbsp;   private static void Configure() => FSM

&nbsp;       .State(State.Idle)

&nbsp;           .On(Trigger.Start)

&nbsp;               .GoTo(State.Running)

&nbsp;               .Guard(CanStart)

&nbsp;               .Action(ApplyStart)

&nbsp;       .State(State.Running)

&nbsp;           .On(Trigger.Update)

&nbsp;               .Guard(CanUpdateAsync)

&nbsp;               .Action(ApplyUpdateAsync);

}

```



\### Lokalnie w DSL (`.Payload<T>()`)



```csharp

\[StateMachine(typeof(State), typeof(Trigger))]

public partial class MultiPayloadFluentFsm

{

&nbsp;   private bool CanStart(in StartPayload p) => p.UserId > 0;

&nbsp;   private void ApplyStart(in StartPayload p) { }

&nbsp;   private bool CanUpdate(in UpdatePayload p) => p.Version > 0;

&nbsp;   private void ApplyUpdate(in UpdatePayload p) { }



&nbsp;   private static void Configure() => FSM

&nbsp;       .State(State.Idle)

&nbsp;           .On(Trigger.Start)

&nbsp;               .Payload<StartPayload>()

&nbsp;               .GoTo(State.Running)

&nbsp;               .Guard(CanStart)

&nbsp;               .Action(ApplyStart)

&nbsp;       .State(State.Running)

&nbsp;           .On(Trigger.Update)

&nbsp;               .Payload<UpdatePayload>()

&nbsp;               .Guard(CanUpdate)

&nbsp;               .Action(ApplyUpdate);

}

```



---



\## 14. Słowniczek



\* \*\*DSL (Domain-Specific Language)\*\* – wyspecjalizowany „język” do deklarowania maszyn stanów w metodzie `Configure()`/`SetupStates()`; przetwarzany przez generator na etapie kompilacji; bez logiki imperatywnej i lambd w runtime.

\* \*\*Method group\*\* – odwołanie do metody bez jej wywołania (np. `Action(DoX)`), które kompilator może rzutować na delegata / symbol.

\* \*\*Zero-alloc\*\* – brak alokacji na gorącej ścieżce wykonania (przejścia, wywołania callbacków) w kodzie wygenerowanym.



---



\*\*Status:\*\* dokument bazowy do implementacji. Miejsca oznaczone `TODO(Agent)` wymagają uzupełnienia po analizie repo.



