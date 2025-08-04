(Dodatkowa uwaga: nadmiarowe overloady z parametrami domyślnymi – np. CanFireAsync(trigger, object? payload = null, CancellationToken = default) – mogą prowadzić do niejednoznaczności wywołań, choć same w sobie nie blokują kompilacji.)

Jasne — przejrzałem. Najkrócej:

**Co jest na plus:**

* Dobra separacja wariantów (bazowy `StateMachineCodeGenerator` + `CoreVariantGenerator`), wspólne hooki i helpery.
* Spójne wsparcie async w generatorze (ValueTask/Task, `ConfigureAwait`, `continueOnCapturedContext`).
* Sensowna walidacja sygnatur (`AsyncSignatureAnalyzer`: blokuje `Task<bool>` dla guardów, „async void” itp.).
* API pomocnicze (`CanFire`, `GetPermittedTriggers`, wersje async) i logowanie/hooki gotowe do włączenia.

**Uwagi / ryzyka (najważniejsze na górze):**

1. **Błąd kompilacji przy asynchronicznych guardach z payloadem.**
   W `WriteAsyncGetPermittedTriggersMethod` i `WriteAsyncCanFireMethod` async-guard jest wywoływany jako `Guard()` bez parametru. Gdy guard **wymaga payloadu** i **nie ma** przeciążenia bezparametrowego, wygenerowany kod nie zbuduje się. Użyj analogicznej ścieżki jak w sync (`WriteGuardCall`) lub „uniwersalnej” (`WriteGuardCallUniversal`) – gdy brak payloadu, zwróć `false` albo wybierz przeciążenie bezparametrowe, jeśli istnieje.

2. **Initial OnEntry w async jest „fire-and-forget”.**
   `Task.Run(async () => …)` nie jest obserwowany — wyjątki trafią do `UnobservedTaskException`. Rozważ:

   * logowanie błędów (`ContinueWith`),
   * opcję konfiguracji: uruchomić OnEntry synchronicznie przy starcie via `IHostedService`/fabryka lub ekspozycję `Task` do awaitowania przez użytkownika.

3. **`CancellationToken` nie jest propagowany.**
   Parametr istnieje w `TryFireInternalAsync`, ale nie trafia do guardów/akcji/entry/exit. Warto:

   * dodać przeciążenia callbacków z tokenem i przekazywać go,
   * co najmniej sprawdzać `cancellationToken.ThrowIfCancellationRequested()` w kluczowych miejscach.

5. **Catches i logi.**
   W kilku miejscach masz `catch (Exception ex)` bez użycia `ex`; jeśli logowanie jest włączone, warto logować szczegóły (albo zamienić na `catch`).

6. **Drobiazgi generacyjne.**
   Wygenerowany kod ma sporadyczne artefakty formatowania (spacje/nowe linie przy `WriteCallbackInvocation`). Da się to lekko wygładzić w builderze.

**Szybkie propozycje zmian:**

* Zastąp ręczne wywołania async-guardów w `CanFireAsync`/`GetPermittedTriggersAsync` wywołaniami `WriteGuardCallUniversal(...)` (obsłuży payload/overload/await/try-catch).
* W `WriteAsyncInitialOnEntryDispatch` dodaj `.ContinueWith(t => log(t.Exception), TaskContinuationOptions.OnlyOnFaulted)` lub skonfigurowalny „initial OnEntry strategy”.
* Rozszerz generator o opcjonalny token w sygnaturach callbacków i przekazuj `cancellationToken`.
* Zapewnij ochronę sekcji krytycznych w bazie (jeśli już nie ma).

Jeśli chcesz, mogę przygotować patch do `CoreVariantGenerator` pokazujący użycie `WriteGuardCallUniversal` w obu miejscach oraz drobne poprawki obsługi wyjątków.
