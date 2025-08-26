# Zdarzenia logowania w FastFsm.Net (0.8.0.13)

Poniżej opis, jakie zdarzenia powodują emisję logów w aktualnej implementacji generatora oraz gdzie w wygenerowanym kodzie są one umieszczane. Nazwy metod odnoszą się do statycznej klasy `{ClassName}Log` generowanej dla każdej maszyny (np. `BasicStateMachineLog`).

## Identyfikatory i poziomy logów

- TransitionSucceeded: Informacja (EventId=1) — udane przejście stanu.
- GuardFailed: Ostrzeżenie (EventId=2) — guard zablokował przejście.
- TransitionFailed: Ostrzeżenie (EventId=3) — brak dopasowania przejścia (no-match) lub porażka scenariusza.
- OnEntryExecuted: Debug (EventId=4) — wykonanie metody OnEntry.
- OnExitExecuted: Debug (EventId=5) — wykonanie metody OnExit.
- ActionExecuted: Debug (EventId=6) — wykonanie akcji zdefiniowanej na przejściu.
- PayloadValidationFailed: Ostrzeżenie (EventId=7) — nieprawidłowy typ payloadu (multi‑payload).
- InternalTransitionOnAncestor: Debug (EventId=10) — przejście wewnętrzne na przodku (HSM).
- HierarchicalTransition: Debug (EventId=11) — przejście hierarchiczne (HSM, z LCA oraz liczbą exit/entry).
- CompositeStateEntry: Debug (EventId=12) — wejście do stanu złożonego i jego rozstrzygnięcie (HSM).
- HistoryRestored: Debug (EventId=13) — przywrócenie historii stanu złożonego (HSM).
- ActivePath: Trace (EventId=14) — bieżąca ścieżka aktywnych stanów (HSM).
- ExtensionError: Błąd (EventId=1001) — wyjątek z rozszerzenia (ExtensionRunner).

Uwagi:
- Wszystkie wpisy są warunkowane `ILogger.IsEnabled(level)`.
- Nazwa zdarzenia pochodzi z nazwy metody (np. `nameof(TransitionSucceeded)`).

## Kiedy emitujemy poszczególne logi

- TransitionSucceeded (Informacja, 1):
  - Po pomyślnym przejściu, po wykonaniu OnEntry i Action (jeśli istnieją), we wszystkich ścieżkach:
    - płaska, bez payloadu,
    - z rozszerzeniami,
    - płaska z payloadem (sync direct path),
    - warianty asynchroniczne,
    - „fast‑path” dla prostych maszyn (dodane w 0.8.0.13).

- TransitionFailed (Ostrzeżenie, 3):
  - Gdy nie znaleziono dopasowania przejścia (no‑match) i metoda zwraca `false`.
  - W ścieżkach z rozszerzeniami (async) dodatkowo wywoływany jest `AfterTransition(success:false)` na rozszerzeniach, a log występuje po zakończeniu oceny (sekcja `END_TRY_FIRE`).
  - W części ścieżek występuje także razem z GuardFailed (patrz niżej), gdy guard zablokuje przejście.

- GuardFailed (Ostrzeżenie, 2):
  - Gdy guard zwróci `false`:
    - Płaska ścieżka bez payloadu — log emitowany, następnie TransitionFailed.
    - Ścieżki z rozszerzeniami (w szczególności async) — log emitowany przed TransitionFailed.
    - Płaska ścieżka „payload sync direct” — obecnie brak GuardFailed; metoda zwraca `false` bez dodatkowego logu guardu.

- OnExitExecuted (Debug, 5):
  - Po wywołaniu OnExit (jeśli zdefiniowane) w ścieżkach przejścia.
  - Nie dotyczy przejść wewnętrznych (internal), gdzie stan nie zmienia się.

- ActionExecuted (Debug, 6):
  - Po wywołaniu akcji (jeśli zdefiniowana) na przejściu.

- OnEntryExecuted (Debug, 4):
  - Po wejściu do stanu docelowego (OnEntry) w ramach przejścia.
  - Dodatkowo log generowany jest także podczas inicjalnego wejścia (OnInitialEntry), gdy bieżący stan startowy posiada OnEntry — log pojawia się po `Start()`.

- PayloadValidationFailed (Ostrzeżenie, 7):
  - Tylko w maszynach z wieloma typami payloadu (multi‑payload).
  - Gdy przekazany payload jest `null` (wymagany typ) lub typ różni się od oczekiwanego dla danego triggera — przejście kończy się `false`.

- HSM‑specyficzne logi (Debug/Trace; 10–14):
  - InternalTransitionOnAncestor — przejście wewnętrzne wykonywane na przodku.
  - HierarchicalTransition — gdy wyznaczany jest plan przejścia z LCA i liczbą wyjść/wejść.
  - CompositeStateEntry — wejście do stanu złożonego i rozstrzygnięcie podstanu.
  - HistoryRestored — przywrócenie historii (shallow/deep) dla stanu złożonego.
  - ActivePath — diagnostyczny zapis aktualnej ścieżki aktywnych stanów.

- ExtensionError (Błąd, 1001):
  - Emisja z `ExtensionRunner`, gdy dowolna metoda rozszerzenia (`OnBeforeTransition`, `OnAfterTransition`, `OnGuardEvaluation`, `OnGuardEvaluated`) zgłosi wyjątek.
  - Wpis zawiera m.in. `ExtensionType`, `MethodName`, `ExceptionMessage`, `InstanceId`, `FromState`, `Trigger`, `ToState`.
  - Wyjątek w rozszerzeniu nie przerywa działania maszyny — log jest jedynie informacyjny/diagnostyczny.

## Dodatkowe uwagi implementacyjne

- Logi Debug (OnExit/Action/OnEntry) są filtrowane przez poziom logowania — przy wyłączonym Debug widoczne będą zwykle tylko wpisy Informacja/Ostrzeżenie/Błąd.
- Dla ścieżek asynchronicznych agregujemy wynik w zmiennej `success`; po zakończeniu generujemy `TransitionFailed`, jeśli `success == false`.
- Wariant „fast‑path” (płaskie, proste maszyny bez guardów/akcji/onEntry/onExit) od 0.8.0.13 również emituje `TransitionSucceeded`.
- Każda instancja maszyny posiada `InstanceId` (GUID w treści logu) pozwalający korelować wpisy.

