Oczywiście. Oto przepisany, szczegółowy plan implementacji, który w pełni integruje wszystkie Twoje uwagi i poprawki.

---

# Async-aware FastFSM - Finalna Specyfikacja Implementacji

**Wersja: 1.1**  
**Status: Zatwierdzony do realizacji**

## Executive Summary

Proponujemy rozszerzenie generatora FastFSM o naturalne wsparcie dla programowania asynchronicznego. Kluczowe założenie: **zero nowych atrybutów, zero dodatkowej konfiguracji**. Użytkownik zmienia tylko sygnatury metod (`void → Task/ValueTask`, `bool → ValueTask<bool>`), a generator automatycznie generuje odpowiednie async API, jednocześnie chroniąc programistę przed typowymi pułapkami asynchroniczności.

## 1. Cele biznesowe i techniczne

### 1.1 Motywacja
- **70%+ aplikacji .NET** wykorzystuje async/await (UI frameworks, HTTP clients, I/O operations).
- Obecne obejścia (`Task.Run`, `GetAwaiter().GetResult()`) są niewydajne i podatne na deadlocki.
- Konkurencyjne rozwiązania (Stateless, Appccelerate) oferują async, ale z gorszą wydajnością.

### 1.2 Cele projektu
1. **Intuicyjność** - programista pisze idiomatyczny kod C# bez uczenia się nowego API.
2. **Zero-overhead dla sync** - istniejący kod nie płaci za funkcje asynchroniczne.
3. **Type-safety** - wykrywanie błędów na etapie kompilacji i pisania kodu.
4. **Performance** - wykorzystanie `ValueTask` i konfigurowalnej polityki `ConfigureAwait`.

## 2. Design rozwiązania

### 2.1 Model detekcji

Generator automatycznie wykrywa tryb asynchroniczny na podstawie sygnatur metod zdefiniowanych w callbackach.

| Typ Callbacku | Sygnatura Synchroniczna | Sygnatura Asynchroniczna | Wpływ na detekcję |
| :--- | :--- | :--- | :--- |
| **OnEntry/OnExit** | `void` | `Task` lub `ValueTask` | → tryb async FSM |
| **Action** | `void` | `Task` lub `ValueTask` | → tryb async FSM |
| **Guard** | `bool` | `ValueTask<bool>` (**tylko!**) | → tryb async FSM |

**Kluczowe zasady**:
1.  Jedna maszyna stanów jest albo **w pełni synchroniczna**, albo **w pełni asynchroniczna**. Mieszanie jest zabronione i wykrywane przez analizator (błąd FSM011).
2.  Asynchroniczne guardy **muszą** zwracać `ValueTask<bool>`, a nie `Task<bool>`, aby uniknąć niepotrzebnych alokacji na stercie. Analizator (FSM012) zgłosi błąd w przeciwnym wypadku.

### 2.2 Przykład użycia

```csharp
// Użytkownik pisze naturalny async kod
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
public partial class OrderProcessingMachine
{
    // ...pola i konstruktor...
    
    // Async OnEntry i OnExit (z ValueTask)
    [State(OrderState.Processing, 
           OnEntry = nameof(StartProcessingAsync),
           OnExit = nameof(CleanupProcessingAsync))]
    private async Task StartProcessingAsync()
    {
        await _orderService.LockInventoryAsync(CurrentOrder).ConfigureAwait(false);
        await _orderService.NotifyWarehouseAsync(CurrentOrder).ConfigureAwait(false);
    }
    
    private async ValueTask CleanupProcessingAsync()
    {
        await using var _ = await _orderService.GetProcessingLockAsync();
        await _orderService.ReleaseResourcesAsync().ConfigureAwait(false);
    }
    
    // Async Guard (zwraca ValueTask<bool>)
    [Transition(OrderState.Pending, OrderTrigger.Process, OrderState.Processing,
                Guard = nameof(CanProcessAsync),
                Action = nameof(InitiateProcessingAsync))]
    private async ValueTask<bool> CanProcessAsync()
    {
        var inventory = await _orderService.CheckInventoryAsync(CurrentOrder);
        return inventory.AllItemsAvailable;
    }
    
    // Async Action
    private async Task InitiateProcessingAsync()
    {
        var result = await _paymentGateway.AuthorizePaymentAsync(CurrentOrder.PaymentId);
        if (!result.Success)
        {
            await TryFireAsync(OrderTrigger.PaymentFailed).ConfigureAwait(false);
        }
    }
}
```

### 2.3 Wygenerowane API

Generator tworzy asynchroniczny wariant API, blokując synchroniczne odpowiedniki z pomocnym komunikatem.

```csharp
// Interfejs odzwierciedla async naturę maszyny
public interface IOrderProcessingMachine : IAsyncStateMachine<OrderState, OrderTrigger> { }

public partial class OrderProcessingMachine : 
    AsyncStateMachineBase<OrderState, OrderTrigger>, 
    IOrderProcessingMachine
{
    // Metody sync rzucają wyjątek kierujący do async API
    
    /// <summary>
    /// This state machine is asynchronous; all synchronous members throw InvalidOperationException. 
    /// Use TryFireAsync instead.
    /// </summary>
    public sealed override bool TryFire(OrderTrigger trigger, object? payload = null)
        => throw new InvalidOperationException(
            "This state machine is async. Use TryFireAsync instead.");
    
    // Async API z wbudowanym zabezpieczeniem przed współbieżnością
    public async ValueTask<bool> TryFireAsync(
        OrderTrigger trigger, 
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        // Thread-safe z SemaphoreSlim
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(_continueOnCapturedContext);
        try
        {
            return await TryFireInternalAsync(trigger, payload, cancellationToken)
                .ConfigureAwait(_continueOnCapturedContext);
        }
        finally
        {
            _gate.Release();
        }
    }
    
    // ... reszta async API (FireAsync, CanFireAsync, GetPermittedTriggersAsync)
}
```

## 3. Architektura techniczna

### 3.1 Rozszerzenia modelu

```csharp
// Generator.Model.GenerationConfig
public class GenerationConfig
{
    public bool IsAsync { get; set; }  // Nowa flaga
}

// Generator.Model.StateMachineModel
public class StateMachineModel
{
    public bool ContinueOnCapturedContext { get; set; } // Nowa flaga
    // ...
}

// Generator.Model.StateModel
public class StateModel
{
    public bool OnEntryIsAsync { get; set; }
    public bool OnExitIsAsync { get; set; }
    // ...
}

// Generator.Model.TransitionModel  
public class TransitionModel
{
    public bool GuardIsAsync { get; set; }
    public bool ActionIsAsync { get; set; }
    // ...
}
```

### 3.2 Hierarchia runtime

```csharp
// Bazowa klasa dla maszyn asynchronicznych
public abstract class AsyncStateMachineBase<TState, TTrigger> : 
    StateMachineBase<TState, TTrigger>,
    IAsyncStateMachine<TState, TTrigger>
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    protected readonly bool _continueOnCapturedContext; // Kontroluje zachowanie `ConfigureAwait`

    protected AsyncStateMachineBase(TState initialState, bool continueOnCapturedContext = false)
        : base(initialState)
    {
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    // Blokujemy sync API z pomocnym komunikatem
    public sealed override bool TryFire(...) => throw new InvalidOperationException(...);
    
    // Implementacja async API
    public async ValueTask<bool> TryFireAsync(...)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(_continueOnCapturedContext);
        try { /* ... */ }
        finally { _gate.Release(); }
    }
    
    protected abstract ValueTask<bool> TryFireInternalAsync(...);
}
```

### 3.3 Extensions - projekt "async-first"

```csharp
// Jeden interfejs, async-first, z ValueTask dla minimalizacji alokacji
public interface IStateMachineExtension
{
    ValueTask OnBeforeTransitionAsync<TContext>(TContext context) where TContext : IStateMachineContext;
    ValueTask OnAfterTransitionAsync<TContext>(TContext context, bool success) where TContext : IStateMachineContext;
    // ... pozostałe metody
}

// Dwa wyspecjalizowane runnery dla optymalizacji
internal sealed class ExtensionRunnerSync
{
    public void RunBeforeTransition<TContext>(...)
    {
        // UWAGA: To wywołanie zablokuje wątek, jeśli rozszerzenie używa I/O.
        // Jest to świadomy kompromis dla zachowania zerowego narzutu w ścieżce synchronicznej.
        extension.OnBeforeTransitionAsync(context).GetAwaiter().GetResult();
    }
}

internal sealed class ExtensionRunnerAsync
{
    public async ValueTask RunBeforeTransitionAsync<TContext>(...)
    {
        await extension.OnBeforeTransitionAsync(context).ConfigureAwait(false);
    }
}
```

## 4. Zmiany w generatorze

### 4.1 Parser - rozszerzona walidacja

1.  **Odczyt `ContinueOnCapturedContext`**: Parser odczyta nową właściwość nazwaną z atrybutu `[StateMachine(ContinueOnCapturedContext = true)]` i zapisze ją w `StateMachineModel`. Domyślnie `false`.
2.  **Walidacja sygnatur**: Metoda `ValidateCallbackMethodSignature` zostanie rozszerzona o:
    *   Rozpoznawanie `Task`, `ValueTask` oraz `ValueTask<bool>` jako sygnatur asynchronicznych.
    *   Wykrywanie `async void` i zgłaszanie ostrzeżenia **FSM014**.
    *   Wykrywanie `Task<bool>` dla guardów i zgłaszanie błędu **FSM012**.
    *   Śledzenie stanu `IsAsync` w modelu. Jeśli parser napotka sygnaturę `async` a potem `sync` (lub na odwrót), zgłosi błąd **FSM011**.

### 4.2 Generowanie kodu - ścieżki async

1.  **Wybór klasy bazowej**: Generator wybierze `AsyncStateMachineBase` jeśli `model.GenerationConfig.IsAsync` jest `true`.
2.  **Konstruktor**: Wygenerowany konstruktor przekaże flagę `continueOnCapturedContext` do konstruktora klasy bazowej.
3.  **Generowanie `await`**: Wszystkie wywołania asynchronicznych callbacków (`OnEntry`/`OnExit`/`Action`/`Guard`) będą poprzedzone `await` i zakończone `.ConfigureAwait(_continueOnCapturedContext)`.
4.  **Generowanie XML-doc**: Dla maszyn asynchronicznych, generator doda odpowiedni komentarz `/// <summary>...` do zablokowanych metod synchronicznych (`TryFire`, `Fire` etc.), aby IntelliSense informował o konieczności użycia `...Async`.

## 5. Analizatory i diagnostyka

### 5.1 Nowe reguły

| ID | Severity | Tytuł | Komunikat |
| :--- | :--- | :--- | :--- |
| **FSM011** | Error | Mieszane synchroniczne i asynchroniczne callbacki | Nie można mieszać synchronicznych i asynchronicznych callbacków w tej samej maszynie stanów. Wszystkie muszą być albo synchroniczne, albo asynchroniczne. |
| **FSM012** | Error | Nieprawidłowa sygnatura asynchronicznego guarda | Asynchroniczne guardy muszą zwracać `ValueTask<bool>`, a nie `Task<bool>`, w celu optymalizacji alokacji. |
| **FSM013** | Error | Asynchroniczny callback w synchronicznej maszynie | Metoda '{0}' zwraca {1}, ale maszyna stanów jest synchroniczna. Zmień typ zwracany na void/bool lub uczyń wszystkie callbacki asynchronicznymi. |
| **FSM014** | Warning | Nieprawidłowy typ zwracany `async void` | Callbacki nie powinny zwracać `async void`. Użyj `Task` lub `ValueTask`, aby zapewnić poprawne śledzenie zakończenia i obsługę wyjątków. |

## 6. Plan wdrożenia

Plan pozostaje bez zmian, ale poszczególne fazy uwzględnią zaktualizowane wymagania (np. Faza 5 będzie zawierać aktualizację dokumentacji o re-entrancy).

**Total: ~5 tygodni** przy jednym developerze.

## 7. Testy i weryfikacja

### 7.1 Strategia testowania

1.  **Testy migawkowe (Snapshot tests)**: Weryfikacja, że generowany kod dla maszyn async dziedziczy po `AsyncStateMachineBase`, zawiera `await`, `ConfigureAwait` i blokuje stare API.
2.  **Benchmarki regresji**:
    *   Porównanie `stare-sync` vs `nowe-sync`. Oczekiwanie: < 5% różnicy.
    *   **Cel benchmarku**: Test musi odzwierciedlać realny "hot-path", dlatego **musi być skonfigurowany z `ExtensionRunnerSync`**, aby uwzględnić wywołanie `.GetResult()` na hookach rozszerzeń.

### 7.2 Scenariusze testowe

1.  **Happy path** - async callbacks działają poprawnie.
2.  **Re-entrancy** - Semafor chroni przed *równoczesnymi* wywołaniami z różnych wątków.
3.  **Exception handling** - wyjątki w async callbacks są poprawnie propagowane.
4.  **Deadlock prevention** - `ConfigureAwait(false)` jest stosowane wszędzie (w trybie domyślnym).
5.  **Propagacja CancellationToken** - weryfikacja, że `OperationCanceledException` jest rzucane, gdy token jest anulowany *przed* wejściem do sekcji krytycznej `SemaphoreSlim.WaitAsync`.
6.  **Cancellation w trakcie** - weryfikacja, że `CancellationToken` jest przekazywany do `TryFireInternalAsync` i może być użyty przez użytkownika.

## 8. Ryzyka i mitigacje

| Ryzyko | Prawd. | Wpływ | Mitigacja |
| :--- | :--- | :--- | :--- |
| Regresja wydajności sync | Niskie | Wysokie | - Oddzielne ścieżki kodu (`StateMachineBase` vs `AsyncStateMachineBase`). <br>- Benchmarki w CI. |
| Problem z **re-entrancy** w async | Średnie | Średnie | - `SemaphoreSlim` nie chroni przed ponownym wejściem z tego samego wątku (np. `await TryFireAsync()` wewnątrz `OnEntry`). <br>- Świadoma decyzja o braku wbudowanej ochrony (zgodność z wersją sync). <br>- **Jasne udokumentowanie problemu i przykładów w FAQ/README.** |
| Zwiększona złożoność generatora | Wysokie | Niskie | - Dobra separacja klas generatorów (np. `AsyncStateMachineCodeGenerator`). <br>- Kompleksowe testy jednostkowe i migawkowe. |

## 9. Podsumowanie

Dodanie wsparcia `async` do FastFSM to naturalna ewolucja narzędzia. Proponowane, zaktualizowane rozwiązanie:
- **Zachowuje prostotę** - zero nowych atrybutów dla podstawowej funkcjonalności.
- **Jest wydajne** - ścieżka synchroniczna bez regresji, a asynchroniczna zoptymalizowana.
- **Jest bezpieczne** - analizatory wykrywają typowe błędy i pułapki `async`.
- **Jest idiomatyczne i elastyczne** - zgodne ze wzorcami .NET, z opcjonalną furtką dla `ConfigureAwait`.