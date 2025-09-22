# FastFSM Diagnostics (v0.7.5)

Standardized taxonomy and numbering. Prefix FSM + four digits.

## A. Model & Declarations (0100–0599)

- FSM0100 — Potentially missing StateMachine attribute: If this class is intended to be a FSM, it needs the [StateMachine] attribute and must be declared as partial.
- FSM0101 — State/Trigger types must be enums: The StateType and TriggerType arguments of the StateMachineAttribute must be enum types.
- FSM0200 — Invalid enum value in transition: Enum values in transition attributes must be valid members of the specified enum type.
- FSM0300 — Invalid method signature for FSM callback: Guard, Action, OnEntry, or OnExit methods must have a specific signature (e.g., guards return bool; actions are void; both can optionally take object? payload).
- FSM0301 — Guard with payload in non-payload machine: Guards that expect payload parameters cannot be used in state machines without payload support.
- FSM0302 — Callback returns 'async void': 'async void' methods are fire-and-forget and can lead to unhandled exceptions and race conditions. State machine callbacks should always be awaitable.
- FSM0400 — Duplicate transition detected: There are multiple transitions defined for the same 'from state' and 'trigger'. The generator will only consider the first one encountered.
- FSM0500 — Unreachable state detected: A state exists in the state enum that may not be reachable from the initial state or any other state via the defined transitions. This is a simplified check.

## B. Async Consistency (1100–1199)

- FSM1100 — Mixed synchronous and asynchronous callbacks: All state machine callbacks (OnEntry, OnExit, Action, Guard) must be either all synchronous or all asynchronous to ensure consistent behavior.
- FSM1110 — Invalid async guard return type: Using Task<bool> for guards causes unnecessary memory allocations. Use ValueTask<bool> for optimal performance.
- FSM1120 — Asynchronous callback in synchronous state machine: A state machine must be consistently synchronous or asynchronous. Mixing callback types can lead to unexpected behavior and deadlocks.

## C. HSM – Hierarchy (2000–2099)

- FSM2000 — Circular hierarchy detected: A state cannot be its own ancestor or descendant; remove circular parent-child relationships.
- FSM2010 — Multiple or divergent parent: All parent states referenced by substates must be defined; check for typos or missing [State] on parent.
- FSM2020 — Composite without initial state: Composite states must have an initial substate to determine which child state to enter. Either define an initial substate or use history mode.
- FSM2030 — Multiple initial children: A composite state can only have one initial substate. Remove duplicate initial markers.
- FSM2040 — History on non-composite: Only composite states (states with children) can have history mode. History remembers which child was last active.

## D. Fluent DSL (3000–3099)

- FSM3000 — Open transition not finalized: Every transition must be finalized with either GoTo(targetState) for external transitions or Internal() for internal transitions.
- FSM3010 — Transition auto-finalized as internal: When a new On() or State() is encountered without finalizing the previous transition, it is auto-finalized as internal. This may not be intended.
- FSM3020 — Multiple payload definitions on transition: Each transition should have at most one payload type. Multiple Payload() calls use the last specified type.
- FSM3030 — Invalid priority argument: The Priority() fluent call accepts only an integer literal argument used for transition ordering.
- FSM3040 — Priority() without active transition: Priority() is valid only in the context of an active transition builder (after On()/OnInternal()).
- FSM3050 — Multiple global OnException handlers: Exactly one global exception handler is allowed. Remove duplicates.
- FSM3060 — Invalid OnException handler signature: Handler must return ExceptionDirective or ValueTask<ExceptionDirective> and accept ExceptionContext<TState,TTrigger> as first parameter with optional CancellationToken.

Na podstawie analizy dokumentów, oto uzupełniona lista diagnostyk dla wersji 0.8.0:

## Nowe diagnostyki Fluent DSL (3070-3099)

**FSM3070** – Ambiguous method group reference
- **Kiedy**: Multiple overloads match (np. `CanStart()` i `CanStart(PayloadData)`)
- **Uzasadnienie**: Method groups nie rozstrzygają przeciążeń automatycznie

**FSM3071** – Impure DSL: expression not allowed
- **Kiedy**: Użycie czegokolwiek poza method group/literałem (np. `this._field`, `GetMethod()`, `flag ? A : B`)
- **Uzasadnienie**: Najważniejsza diagnostyka - zapobiega myleniu compile-time z runtime

**FSM3072** – Property or indexer used where method expected
- **Kiedy**: `.Guard(IsReady)` gdzie `IsReady` to property, nie metoda
- **Uzasadnienie**: Properties wyglądają jak metody w C#, częsty błąd

**FSM3073** – External method group not allowed
- **Kiedy**: `.Guard(OtherClass.Method)` lub `.Guard(service.Method)`
- **Uzasadnienie**: DSL akceptuje tylko metody tej samej klasy

**FSM3074** – Signature mismatch for DSL position
- **Kiedy**: Np. async guard użyty w sync-only kontekście
- **Uzasadnienie**: Zapobiega runtime errors z niepasującymi sygnaturami

**FSM3075** – Lambda expression not allowed
- **Kiedy**: `.Guard(() => true)` lub `.Action(x => ProcessX(x))`
- **Uzasadnienie**: Lambdy sugerują runtime execution, łamią zasadę "zero lambdas"

**FSM3076** – Field or property access in DSL
- **Kiedy**: `.Priority(_defaultPriority)` zamiast literału
- **Uzasadnienie**: DSL nie może czytać stanu instancji podczas kompilacji

**FSM3077** – Method invocation in DSL
- **Kiedy**: `.GoTo(GetNextState())` zamiast stałej wartości
- **Uzasadnienie**: DSL nie wykonuje kodu, tylko parsuje strukturę

## Diagnostyki dla Configure() (3080-3089)

**FSM3080** – Multiple Configure methods detected
- **Uzasadnienie**: Tylko jedna metoda Configure per typ dla jednoznaczności

**FSM3081** – Invalid Configure method signature
- **Podkody**:
  - 3081a: Must be private
  - 3081b: Must be parameterless  
  - 3081c: Cannot be virtual/override
  - 3081d: Must be instance method (w 0.8.0)
- **Uzasadnienie**: Ścisła konwencja zapobiega pomyłkom

**FSM3082** – Configure inherited from base class
- **Kiedy**: Configure() zdefiniowane w klasie bazowej
- **Uzasadnienie**: Configure musi być w tej samej partial class co [StateMachine]

**FSM3083** – Partial method not supported for Configure
- **Kiedy**: `partial void Configure()`
- **Uzasadnienie**: Parser potrzebuje pełnej implementacji do analizy

**FSM3084** – Configure() contains runtime-only constructs
- **Kiedy**: Użycie DI, await, try/catch w Configure()
- **Uzasadnienie**: Dodatkowe zabezpieczenie przed myleniem z runtime

## Rozszerzone diagnostyki istniejące

**FSM3010** (rozszerzenie) – Transition auto-finalized warning
- **Dodać**: Sugestię użycia `.Internal()` lub `.GoTo()` explicite
- **Uzasadnienie**: W 0.8.0 z method groups łatwiej zapomnieć o finalizacji

**FSM3060** (rozszerzenie) – Invalid OnException handler
- **Dodać**: Sprawdzenie czy to method group, nie nameof
- **Uzasadnienie**: Spójność z nowym API

## Uzasadnienie ogólne

Te diagnostyki są konieczne, bo instancyjne `Configure()` stwarza **iluzję runtime execution**. Bez nich użytkownicy będą próbować:
- Używać pól/właściwości w warunkach
- Wywoływać metody do określenia stanów
- Używać DI w Configure()
- Tworzyć dynamiczne konfiguracje

Każda diagnostyka musi mieć **konkretny fix suggestion**, np.:
- FSM3071 → "Create a guard method that checks this condition"
- FSM3072 → "Convert property to method: `bool CanProcess() => IsReady;`"
- FSM3075 → "Extract lambda to named method in your class"

To minimalizuje frustrację przy migracji na nowy model.
