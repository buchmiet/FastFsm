# Kompletny Raport Diagnostyk FSM

Ten raport zawiera kompletną listę wszystkich diagnostyk `FSM...` znalezionych w projektach `Generator` i `Generator.Rules`. Celem raportu jest dostarczenie pełnego obrazu do dalszej analizy i modyfikacji numeracji.

---

## Część 1: Diagnostyki zdefiniowane w `Generator.Rules`

Są to reguły walidacyjne, które dotyczą bezpośrednio kodu maszyn stanów pisanego przez użytkownika.

### Kategoria: Ogólne (FSM001-FSM014)
- **FSM001**: `DuplicateTransition`
  - **Opis:** Zduplikowane przejście dla tego samego stanu i wyzwalacza.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L7`
- **FSM002**: `UnreachableState`
  - **Opis:** Stan jest niemożliwy do osiągnięcia.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L8`
- **FSM003**: `InvalidMethodSignature`
  - **Opis:** Sygnatura metody (np. akcji, gardy) jest nieprawidłowa.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L9`
- **FSM004**: `MissingStateMachineAttribute`
  - **Opis:** Brak atrybutu `[StateMachine]` na klasie, która wygląda na maszynę stanów.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L10`
- **FSM005**: `InvalidTypesInAttribute`
  - **Opis:** Nieprawidłowe typy podane w atrybucie.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L11`
- **FSM006**: `InvalidEnumValueInTransition`
  - **Opis:** Nieprawidłowa wartość `enum` w definicji przejścia.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L12`
- **FSM007**: `MissingPayloadType`
  - **Opis:** Brak definicji typu payloadu, gdy jest on wymagany.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L13`
- **FSM008**: `ConflictingPayloadConfiguration`
  - **Opis:** Konflikt w konfiguracji payloadu.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L14`
- **FSM009**: `InvalidForcedVariantConfiguration`
  - **Opis:** Nieprawidłowa konfiguracja wymuszonego wariantu maszyny.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L15`
- **FSM010**: `GuardWithPayloadInNonPayloadMachine`
  - **Opis:** Garda oczekuje payloadu w maszynie, która go nie obsługuje.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L16`
- **FSM011**: `MixedSyncAsyncCallbacks`
  - **Opis:** Mieszanie synchronicznych i asynchronicznych callbacków.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L17`
- **FSM012**: `InvalidGuardTaskReturnType`
  - **Opis:** Garda asynchroniczna zwraca `Task<bool>` zamiast `ValueTask<bool>`.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L18`
- **FSM013**: `AsyncCallbackInSyncMachine`
  - **Opis:** Użycie asynchronicznego callbacku w maszynie synchronicznej.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L19`
- **FSM014**: `InvalidAsyncVoid`
  - **Opis:** Użycie `async void` w callbacku.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L20`

### Kategoria: Hierarchiczne Maszyny Stanów (HSM) (FSM100-FSM105)
- **FSM100**: `CircularHierarchy`
  - **Opis:** Wykryto cykliczną zależność w hierarchii stanów.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L23`
- **FSM101**: `OrphanSubstate`
  - **Opis:** Podstan nie ma zdefiniowanego rodzica lub ma ich wielu.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L24`
- **FSM102**: `InvalidHierarchyConfiguration`
  - **Opis:** Stan złożony nie ma zdefiniowanego stanu początkowego.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L25`
- **FSM103**: `MultipleInitialSubstates`
  - **Opis:** Stan złożony ma wiele podstanów oznaczonych jako początkowe.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L26`
- **FSM104**: `InvalidHistoryConfiguration`
  - **Opis:** Użycie trybu historii na stanie, który nie jest stanem złożonym.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L27`
- **FSM105**: `ConflictingTransitionTargets`
  - **Opis:** Przejście do stanu złożonego bez wskazania konkretnego podstanu.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L28`

### Kategoria: Fluent API (FSM200-FSM206)
- **FSM200**: `OpenTransition`
  - **Opis:** Przejście nie zostało zakończone przez `GoTo()` lub `Internal()`.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L31`
- **FSM201**: `AutoFinalizedTransition`
  - **Opis:** Przejście zostało automatycznie sfinalizowane jako wewnętrzne.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L32`
- **FSM202**: `MultiplePayloadsOnTransition`
  - **Opis:** Wielokrotne wywołanie `Payload()` dla tego samego przejścia.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L33`
- **FSM203**: `IncompatibleHandlerSignature`
  - **Opis:** Sygnatura handlera nie pasuje do wymagań.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L34`
- **FSM204**: `AsyncPathWithSyncFire`
  - **Opis:** Użycie `Fire()` gdy ścieżka zawiera handlery asynchroniczne.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L35`
- **FSM205**: `AsyncMethodWithoutSuffix`
  - **Opis:** Metoda asynchroniczna nie ma wymaganego suffixu `Async`.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L36`
- **FSM206**: `SyncMethodInRequiredAsyncMode`
  - **Opis:** Metoda synchroniczna w trybie, który wymaga metod asynchronicznych.
  - **Lokalizacja:** `Generator.Rules/Definitions/RuleIdentifiers.cs:L37`

---

## Część 2: Diagnostyki zdefiniowane w `Generator`

Są to diagnostyki, które **nie mają swojego źródła w `Generator.Rules`**. Służą głównie do wewnętrznego logowania, śledzenia i obsługi błędów samego generatora.

### Kategoria: Wewnętrzne/Parsera (FSM9xx i inne)
- **FSM207**: `Multiple initials per parent (placeholder)`
  - **Opis:** Placeholder dla reguły walidującej wielokrotne stany początkowe.
  - **Lokalizacja:** `Generator/Parsers/FluentParser.cs:L1771`
- **FSM981**: `No transitions`
  - **Opis:** Maszyna stanów nie posiada żadnych zdefiniowanych przejść.
  - **Lokalizacja:** `Generator/Parsers/StateMachineParser.cs:L888`
- **FSM982**: `Internal-only machine`
  - **Opis:** Maszyna stanów posiada wyłącznie przejścia wewnętrzne.
  - **Lokalizacja:** `Generator/Parsers/StateMachineParser.cs:L880`
- **FSM983**: `Missing action method`
  - **Opis:** Zdefiniowane w akcji przejścia odwołanie do metody nie zostało znalezione.
  - **Lokalizacja:** `Generator/Parsers/StateMachineParser.cs:L1231`
- **FSM989**: `Configuration sections diagnostic`
  - **Opis:** Diagnostyka dotycząca odczytanych sekcji konfiguracyjnych.
  - **Lokalizacja:** `Generator/Generator.cs:L204` (Definicja), `Generator/Parsers/StateMachineParser.cs:L1017` (Użycie)
  - **Warianty:** `FSM989D` (Lokalizacja: `Generator/Generator.cs:L397`)
- **FSM990**: `Logowanie śledzące`
  - **Opis:** Ogólna diagnostyka używana do śledzenia różnych etapów pracy generatora.
  - **Lokalizacja:** `Generator/Generator.cs:L164` (FSM990_PRE), `Generator/Generator.cs:L172` (FSM990_PROP), `Generator/Generator.cs:L638` (FSM990_HSM_FLAG)
- **FSM991**: `Variant decision diagnostic`
  - **Opis:** Diagnostyka informująca o wariancie maszyny stanów, który został wybrany.
  - **Lokalizacja:** `Generator/Generator.cs:L196`
- **FSM992**: `Declaration plan diagnostic`
  - **Opis:** Diagnostyka dotycząca planu deklaracji dla maszyny stanów.
  - **Lokalizacja:** `Generator/Generator.cs:L188`
- **FSM993**: `Empty code generated diagnostic`
  - **Opis:** Informuje, że wygenerowany kod źródłowy jest pusty.
  - **Lokalizacja:** `Generator/Generator.cs:L180`
- **FSM994**: `Enum-only states fallback diagnostic`
  - **Opis:** Informuje o użyciu mechanizmu zapasowego dla maszyn opartych na `enum`.
  - **Lokalizacja:** `Generator/Generator.cs:L148`
- **FSM995**: `MSBuild analyzer properties diagnostic`
  - **Opis:** Diagnostyka dotycząca właściwości MSBuild odczytanych przez analizator.
  - **Lokalizacja:** `Generator/Generator.cs:L156`
- **FSM996**: `AddSource succeeded diagnostic`
  - **Opis:** Informuje, że źródło zostało pomyślnie dodane do kompilacji.
  - **Lokalizacja:** `Generator/Generator.cs:L140`
- **FSM997**: `State machine candidate skipped diagnostic`
  - **Opis:** Informuje, że kandydat na maszynę stanów został pominięty.
  - **Lokalizacja:** `Generator/Generator.cs:L132`
- **FSM998**: `State machine candidate found diagnostic`
  - **Opis:** Informuje, że znaleziono kandydata na maszynę stanów.
  - **Lokalizacja:** `Generator/Generator.cs:L124`
  - **Warianty:** `FSM998A` (Lokalizacja: `Generator/Generator.cs:L474`)
- **FSM999**: `Parser critical error`
  - **Opis:** Informuje o krytycznym błędzie parsera.
  - **Lokalizacja:** `Generator/Parsers/StateMachineParser.cs:L920`
