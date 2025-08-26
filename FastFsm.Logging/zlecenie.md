# Zlecenie: domknięcie testów logowania i dopracowanie generatora (FastFsm.Net 0.8.0.16)

Dokument opisuje aktualny stan, kontekst działania generatora i loggera, proces bumpowania pakietów oraz listę prac do wykonania, aby domknąć pozostałe testy i emisje logów HSM.

## Kontekst i architektura

- Generator źródeł (Roslyn, projekt `Generator`) emituje kod maszyn stanów na podstawie atrybutów z przestrzeni `Abstractions.Attributes` (`[StateMachine]`, `[Transition]`, `[State]`, itd.).
- Emisja kodu odbywa się dwiema drogami:
  - „Ścieżka bazowa/flat” (klasa bazowa `StateMachineCodeGenerator`),
  - „Ścieżka unified/Extensions” (`UnifiedStateMachineGenerator`) – m.in. dla GenerateExtensibleVersion.
- Logowanie: per‑maszynowe klasy `{ClassName}Log` dodawane przez `AddSource(...)` (plik `Namespace.ClassName.Log.g.cs`) – aktualnie generowane wyłącznie przez `Generator.Logger/LoggingClassGenerator` (single source of truth).
- Warunek generowania logowania: `FsmGenerateLogging=true` (props) i kompilacyjna flaga `FSM_LOGGING_ENABLED`.

## Co zostało już zrobione (0.8.0.10 → 0.8.0.16)

- Ujednolicenie i centralizacja klasy logującej: przejście na `LoggingClassGenerator` (zamiast inline `GenerateLoggingHelper`).
- Ujednolicenie szablonów/poziomów/EventId (np. `InternalTransitionOnAncestor` → Debug spójnie w całym projekcie).
- Naprawy core logowania:
  - Extensions sync: pełna sekwencja `OnExitExecuted` → `ActionExecuted` → `OnEntryExecuted` → `TransitionSucceeded`; przy porażce guarda: `GuardFailed` + `TransitionFailed`.
  - Brak dopasowania (flat/HSM): `TransitionFailed` przed `return false`.
  - OnInitialEntry/OnInitialEntryAsync: `OnEntryExecuted` (Debug).
  - Wariant payload sync direct: dodano brakujące `TransitionSucceeded`.
  - Fast‑path (flat, bez guardów/akcji): dodano `TransitionSucceeded` (wcześniej brak logów).
- HSM diagnostyka (częściowo w bazowym generatorze):
  - Emisje: `CompositeStateEntry`, `HistoryRestored`, `HierarchicalTransition` (z LCA i licznikami exit/entry), `ActivePath`.
  - Wyłączono HSM fast‑path przy włączonym logowaniu, aby nie tracić diagnostyki.
- Testy:
  - Trzy pierwotnie padające testy – naprawione poprzez emisję brakujących logów.
  - Dodano testy helperów HSM (10–14) – `HsmLoggingTests` (bez runtime).
  - Dodano szkice testów end‑to‑end HSM – `HsmRuntimeLoggingTests` (A/A1/A2 ↔ B/B1, Shallow history) – patrz „Do zrobienia”.
- Wersjonowanie: kolejne bump’y do 0.8.0.12…0.8.0.16 (lokalny feed `./nuget`).

## Obecny stan testów i luki do domknięcia

- Core logi (1–7, 1001) – ZIELONE: runtime pokryty testami (w tym doprecyzowany `ExtensionError`).
- Helpery HSM (10–14) – ZIELONE: testy wywołujące `{ClassName}Log.*`.
- Runtime HSM – CZĘŚCIOWO: 
  - W bazowym generatorze pojawiają się `CompositeStateEntry`, `HistoryRestored`, `HierarchicalTransition`, `ActivePath`.
  - `InternalTransitionOnAncestor` – brak emisji w wygenerowanej maszynie (wymagane dociągnięcie indeksu przodka i logu w unified/general HSM path).
  - Unified‑path (Extensions) wymaga dopisania emisji HSM (analogicznych do bazowego).

## Zadania do wykonania (checklista)

1) Dokończyć emisje HSM w unified‑path (`UnifiedStateMachineGenerator`):
   - `CompositeStateEntry` + `HistoryRestored` tuż po `GetCompositeEntryTarget`.
   - `HierarchicalTransition` (po wyznaczeniu LCA) + `ActivePath` (po ustaleniu `_currentState`).
   - Zostawić wyłączony fast‑path HSM, jeśli `ShouldGenerateLogging`.

2) `InternalTransitionOnAncestor` (Id=10, Debug):
   - W general HSM selection (bazowy/unified) dodać śledzenie indeksu zwycięskiego przodka (np. `bestAncestorIndex = check;`).
   - W gałęzi `bestIsInternal` – wyemitować
     `InternalTransitionOnAncestor(_logger, _instanceId, ((TState)bestAncestorIndex).ToString(), __fromName, trigger.ToString())`.

3) Zweryfikować runtime testy HSM (`HsmRuntimeLoggingTests`) – powinny przejść po pkt 1–2:
   - `HierarchicalTransition_CompositeEntry_ActivePath_AreLogged` – Composite→B1, HierarchicalTransition A1→B1, ActivePath B/B1.
   - `HistoryRestored_WhenReturningToA_IsLogged` – powrót B→A: CompositeEntry A→A2 + HistoryRestored Shallow (A2).
   - `InternalTransitionOnAncestor_IsLogged` – Refresh w A z poziomu A1/A2.

4) (Opcjonalnie) Ujednolicić emisje w helperze `WriteStateChangeWithCompositeHandling(...)` z innymi ścieżkami, aby nie dublować logów, a zachować spójność.

5) Po każdej zmianie generatora:
   - Bump pakietów (FastFsm.Net, FastFsm.Net.Logging) – ten sam numer.
   - W testach czyścić cache NuGet: `~/.nuget/packages/fastfsm.net/<ver>` i `fastfsm.net.logging/<ver>`.
   - Sprawdzić, czy Analyzer (Generator.dll) z paczki ma tę samą sumę SHA co lokalny build.

## Wskazówki diagnostyczne

- Wygenerowane pliki: `obj/GeneratedFiles/Generator/Generator.StateMachineGenerator` – zarówno `*.Generated.cs`, jak i `*.Log.g.cs`.
- Diagnostyki FSM99x: `FSM996 AddSource ok`, `FSM990_PRE/PROP` – podgląd AddSource i wykrytych właściwości MSBuild.
- W testach dumpuj `LoggedMessages` (poziom, `EventId.Id/Name`, `Message`), gdy asercja nie przechodzi.
- Jeśli asercje opierają się na `EventId.Name`, a środowisko nie ustawia nazw – dopuszczalne są asercje po `EventId.Id`.

## Szybkie komendy

- Pakowanie lokalnego feedu:
  - `dotnet build FastFsm/FastFsm.csproj -c Release`
  - `dotnet build FastFsm.Logging/FastFsm.Logging.csproj -c Release`
  - (opcjonalnie) `dotnet build FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj -c Release`
- Cache flush (przykład):
  - `rm -rf ~/.nuget/packages/fastfsm.net/<ver> ~/.nuget/packages/fastfsm.net.logging/<ver> ~/.nuget/packages/fastfsm.net.dependencyinjection/<ver>`
- Testy selektywne:
  - triada core: `dotnet test FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj -c Release --filter "FullyQualifiedName~FullVariant_CompleteScenario_AllLogsPresent|StateMachine_WithStructTypes_LogsCorrectly|PayloadStateMachine_NullPayloadWithExpectedType_HandledGracefully"`
  - HSM helpery: `--filter "FullyQualifiedName~HsmLoggingTests"`
  - HSM runtime: `--filter "FullyQualifiedName~HsmRuntimeLoggingTests"`

Powodzenia – obecnie gros przypadków jest zielonych; do domknięcia pozostała emisja `InternalTransitionOnAncestor` i synchronizacja emisji HSM w unified‑path. Po wdrożeniu tych punktów komplet testów HSM runtime powinien przejść.

## Jak zbudować i bumpować pakiety (procedura powtarzalna)

1) Zmień wersje:
   - `FastFsm/FastFsm.csproj` → target `StampVersionForNupkg` → `<Version>0.8.0.XX</Version>`
   - `FastFsm.Logging/FastFsm.Logging.csproj` → `<Version>..</Version>` oraz referencja do `FastFsm.Net` na tę samą wersję.
   - `FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj` → jw.
   - Testy: `FastFsm.Logging.Tests.csproj`, `FastFsm.DependencyInjection.Tests.csproj` → referencje do nowych wersji pakietów.

2) Pakowanie (lokalny feed `./nuget`):
   - `dotnet build FastFsm/FastFsm.csproj -c Release`
   - `dotnet build FastFsm.Logging/FastFsm.Logging.csproj -c Release`
   - (opcjonalnie) `dotnet build FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj -c Release`

3) Oczyść cache NuGet (szczególnie po zmianach generatora):
   - `rm -rf ~/.nuget/packages/fastfsm.net/<wersja>`
   - `rm -rf ~/.nuget/packages/fastfsm.net.logging/<wersja>`
   - `rm -rf ~/.nuget/packages/fastfsm.net.dependencyinjection/<wersja>`

4) Buduj i uruchamiaj testy:
   - `dotnet clean FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj -c Release`
   - `dotnet restore FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj`
   - `dotnet test FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj -c Release`

## Wskazówki diagnostyczne

- Sprawdzaj, czy AddSource wrzuca oba pliki:
  - `*.Generated.cs` (maszyna) i `Namespace.ClassName.Log.g.cs` (klasa logująca). Ścieżka: `obj/GeneratedFiles/Generator/Generator.StateMachineGenerator`.
- Szukaj diagnostyk w build logu:
  - `FSM996 AddSource ok: ...` (sukces), `FSM990_PRE` (pre-AddSource summary), `FSM990_PROP` (MSBuild logging flags widoczne dla generatora).
- W razie niejasności dodawaj tymczasowy dump do testów (po akcji), np.:
  - wypisz `Level`, `EventId.Id`, `EventId.Name`, `Message` dla każdego wpisu, aby zobaczyć, co faktycznie zostało zalogowane.
- Uwaga: Testy asercji korzystają z `EventId.Name`. My tworzymy `new EventId(id, nameof(Method))`, więc `Name` powinno być ustawione. Jeśli środowisko pokaże braki nazwy, można asercje oprzeć o `EventId.Id` (1: TransitionSucceeded, 2: GuardFailed, 3: TransitionFailed, 4: OnEntryExecuted, 5: OnExitExecuted, 6: ActionExecuted).

## Zadania do wykonania (checklista)

1) FullVariant_CompleteScenario_AllLogsPresent (Extensions + payload):
   - Zbuduj 0.8.0.11 i oczyść cache NuGet (patrz wyżej).
   - Uruchom selektywnie test: `dotnet test -c Release --filter "FullyQualifiedName~FullVariant_CompleteScenario_AllLogsPresent"`.
   - Jeśli asercja `transitionLog.ShouldNotBe(default)` nadal pada, wydrukuj `LoggedMessages` i potwierdź obecność:
     - Debug: `ActionExecuted`, Debug: `OnEntryExecuted`, Information: `TransitionSucceeded`.
   - Jeśli brakuje któregoś z powyższych, otwórz wygenerowany plik `global__FastFsm.Logging.Tests.FullStateMachine.Generated.cs` i sprawdź, czy odpowiednie wywołania są na swoich miejscach (OnExit→Action→StateChange→OnEntry, a na końcu `TransitionSucceeded`).
   - W razie braku: popraw `WriteTransitionLogicSyncWithExtensions(...)` (Generator/SourceGenerators/UnifiedStateMachineGenerator.cs). Obecnie mamy już komplet logów – spodziewamy się, że problemem będą artefakty cache lub różnica asercji.

2) StateMachine_WithStructTypes_LogsCorrectly (struct enum):
   - Uruchom selektywnie test i w razie potrzeby dodaj dump `LoggedMessages`.
   - Potwierdź, że `PureStateMachine` (lub odpowiednia maszyna) w `*.Generated.cs` loguje `TransitionSucceeded` w ścieżce fast‑path (wygenerowano to w `WriteTransitionLogicForFlatNonPayload`).
   - Jeśli log jest w kodzie, a test go nie widzi – sprawdź poziomy `IsEnabled` i ewentualnie asercję po `EventId.Id`.

3) PayloadStateMachine_NullPayloadWithExpectedType_HandledGracefully:
   - Po `machine.Start()` i `TryFire(Start, null)` powinien pojawić się `TransitionSucceeded`.
   - Zweryfikuj w wygenerowanym kodzie, że guard/action/onEntry przyjmują parametry opcjonalne i logi są emitowane po zmianie stanu.
   - W razie braku – lokalizuj ścieżkę w `StateMachineCodeGenerator` (payload branch) i dopisz brakujący log (ale wg aktualnego stanu powinno być OK).

4) Posprzątać tymczasowe dumpy z testów (jeżeli były dodane) po domknięciu sprawy.

## Uwagi i dobre praktyki

- Po każdej zmianie generatora bumpuj wersję `FastFsm.Net` i zależnych paczek. Konsument (testy) inaczej może widzieć starą wersję analizerów (brak efektu zmian).
- Gdy coś „powinno działać”, ale w testach nadal widać stary efekt – sprawdzaj `obj/GeneratedFiles/...` i printy `FSM996`. Często to tylko cache.
- Jeżeli asercje opierają się na `EventId.Name` i to pole bywa puste w środowisku docelowym – rozważ asercje na `EventId.Id` albo migrację do LoggerMessage.Define (stabilna nazwa/ID bez formatowania stringów w runtime).

## Szybkie komendy

- Bump + pack:
  - FastFsm: `dotnet build FastFsm/FastFsm.csproj -c Release`
  - Logging: `dotnet build FastFsm.Logging/FastFsm.Logging.csproj -c Release`
  - DI: `dotnet build FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj -c Release`
- Cache flush (przykład):
  - `rm -rf ~/.nuget/packages/fastfsm.net/0.8.0.11 ~/.nuget/packages/fastfsm.net.logging/0.8.0.11 ~/.nuget/packages/fastfsm.net.dependencyinjection/0.8.0.11`
- Testy selektywne (triada):
  - `dotnet test FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj -c Release --filter "FullyQualifiedName~FullVariant_CompleteScenario_AllLogsPresent|StateMachine_WithStructTypes_LogsCorrectly|PayloadStateMachine_NullPayloadWithExpectedType_HandledGracefully"`

Powodzenia przy domykaniu testów – po wdrożonych zmianach gros przypadków powinno przechodzić; powyższa checklista i diagnostyka (dumpy, FSM996/FSM990*) pozwolą szybko namierzyć ewentualne różnice pomiędzy oczekiwaniami testów a rzeczywistą emisją logów w wygenerowanym kodzie.

## Mapa kodu (gdzie szukać czego)

- Generator/Generator.cs
  - Rejestracja pipeline Roslyn, AddSource dla plików maszyn i klas `{ClassName}Log`.
  - `GenerateLoggingHelper(ns, className)` – awaryjne generowanie klasy logującej inline.
  - Diagnostyki: `FSM996_AddSourceOk`, `FSM990_PRE`, `FSM990_PROP` i inne pomocne w śledzeniu działania generatora.

- Generator/Helpers/BuildProperties.cs
  - Odczyt MSBuild properties: `GetGenerateLogging(...)`, `GetGenerateDI(...)` z `AnalyzerConfigOptions` (prefiks `build_property.`).

- Generator.Logger/LoggingClassGenerator.cs
  - Konstrukcja klasy `{ClassName}Log` (gdy używamy helpera), reguły `WriteLogStatement(...)` – formuła wywołań do `ILogger.Log(...)` i `EventId`.

- Generator/SourceGenerators/StateMachineCodeGenerator.cs (ścieżka bazowa/flat)
  - `WriteTransitionLogicForFlatNonPayload(...)` – ścieżka sukcesu: OnExit → Action → StateChange → OnEntry → TransitionSucceeded.
  - `WriteGuardCheck(...)` – ścieżka porażki guarda: `GuardFailed` + `TransitionFailed` + return false.
  - `WriteTryFireStructure(...)`/`WriteTryFireStructureFlat(...)`/`WriteTryFireStructureHierarchical(...)` – dyspozycja po stanie/triggerze; dopisane `TransitionFailed` przy braku dopasowania.
  - `WriteOnEntryCall(...)`, `WriteOnExitCall(...)`, `WriteActionCall(...)` – emisja wywołań callbacków i otuliny try/catch (SAFE_ACTIONS) oraz logów `OnEntryExecuted`/`OnExitExecuted`/`ActionExecuted` (w wybranych miejscach).
  - `WriteStartMethod()` – generowanie `Start()`/`StartAsync(...)` z HSM `DescendToInitialIfComposite()` przed wywołaniem base.

- Generator/SourceGenerators/UnifiedStateMachineGenerator.cs (ścieżka unified/Extensions)
  - `WriteTransitionLogicSyncWithExtensions(...)` – pełna ścieżka z rozszerzeniami (Before/AfterTransition + Action/OnExit/OnEntry) oraz logami:
    - sukces: `OnExitExecuted`, `ActionExecuted`, `OnEntryExecuted`, `TransitionSucceeded`;
    - porażka guarda: `GuardFailed` + `TransitionFailed`;
    - porażka/brak dopasowania: `RunAfterTransition(..., false)` i powrót.
  - `WriteOnInitialEntryMethod(...)`, `WriteOnInitialEntryAsyncMethod(...)` – dodane `OnEntryExecuted` przy starcie.
  - `WriteTryFireMethodSync(...)`, `WriteTryFireMethodAsync(...)` – bramki wejściowe TryFire, w async wariantach dopisywane logi `TransitionFailed` na końcu (gdy `SuccessVar=false`).
  - `WriteTryFireStructureWithExtensions(...)` – wariant struktury TryFire dla Extensions (powiadomienie AfterTransition również przy „no transition”).

- FastFsm/build/FastFsm.Net.props
  - Eksport `FsmGenerateLogging`, `FsmGenerateDI` do kompilatora (CompilerVisibleProperty).

- FastFsm.Logging/build/FastFsm.Net.Logging.props
  - Włącza `FsmGenerateLogging=true` i `FSM_LOGGING_ENABLED` po stronie konsumenta.

- Ścieżki wygenerowanych plików (do inspekcji):
  - `obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__{Namespace}.{Class}.Generated.cs` – maszyny.
  - `obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/{Namespace}.{Class}.Log.g.cs` – klasy logujące.

## Przykłady weryfikacji (grep + snippet)

- Sprawdzenie, że AddSource dodał klasy logujące:
  - `dotnet test -v diag > build.log 2>&1`
  - `rg -n "FSM996: AddSource ok: .*\.Log\.g\.cs" build.log`

- Wymuszenie pełnej regeneracji i czystego builda testów logowania:
  - `rm -rf FastFsm.Logging.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator`
  - `dotnet clean FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj -c Release`
  - `dotnet restore FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj`
  - `dotnet build FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj -c Release`

- Sprawdzenie obecności TransitionSucceeded w maszynach (flat/struct):
  - `rg -n "\.TransitionSucceeded\(" FastFsm.Logging.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator`
  - Oczekiwany fragment (snippet):
    - `if (_logger?.IsEnabled(LogLevel.Information) == true) { PureStateMachineLog.TransitionSucceeded(_logger, _instanceId, "Initial", "Processing", "Start"); }`

- Wariant Extensions – weryfikacja kompletu logów w ścieżce sukcesu:
  - `rg -n "ExtensionsStateMachineLog\.(OnExitExecuted|ActionExecuted|OnEntryExecuted|TransitionSucceeded)\(" FastFsm.Logging.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator`
  - Oczekiwane fragmenty (snippety):
    - `ExtensionsStateMachineLog.OnExitExecuted(_logger, _instanceId, "OnInitialExit", "Initial");`
    - `ExtensionsStateMachineLog.ActionExecuted(_logger, _instanceId, "StartAction", "Initial", "Processing", "Start");`
    - `ExtensionsStateMachineLog.OnEntryExecuted(_logger, _instanceId, "OnProcessingEntry", "Processing");`
    - `ExtensionsStateMachineLog.TransitionSucceeded(_logger, _instanceId, "Initial", "Processing", "Start");`

- Weryfikacja TransitionFailed przy braku dopasowania:
  - `rg -n "\.TransitionFailed\(" FastFsm.Logging.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator`
  - Oczekiwany fragment (snippet):
    - `ExtensionsStateMachineLog.TransitionFailed(_logger, _instanceId, "Completed", "Reset");`

- Weryfikacja OnInitialEntry – log przy starcie (maszyna z `[State(..., OnEntry=...)]` dla stanu początkowego):
  - `rg -n "OnInitialEntry\(|OnInitialEntryAsync\(" Generator/SourceGenerators/UnifiedStateMachineGenerator.cs`
  - `rg -n "\.OnEntryExecuted\(" FastFsm.Logging.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator | head`
  - Oczekiwany fragment (snippet):
    - `InitialOnEntryStateMachineActionsLog.OnEntryExecuted(_logger, _instanceId, "OnReadyEntry", "Ready");`

- Szybkie sprawdzenie EventId (jeżeli testy asercji patrzą po `Name` i jest wątpliwość co do środowiska):
  - W klasach `*.Log.g.cs` zdarzenia mają przypisane stałe `EventId`:
    - `new EventId(1, nameof(TransitionSucceeded))`, `2` – GuardFailed, `3` – TransitionFailed, `4` – OnEntryExecuted, `5` – OnExitExecuted, `6` – ActionExecuted.
  - W razie potrzeby asercje można oprzeć o `EventId.Id`.
