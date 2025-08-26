# Zlecenie: domknięcie testów logowania i dopracowanie generatora (FastFsm.Net 0.8.0.11)

Dokument opisuje aktualny stan, kontekst działania generatora i loggera, proces bumpowania pakietów oraz listę prac do wykonania, aby domknąć pozostałe testy w `FastFsm.Logging.Tests`.

## Kontekst i architektura

- Generator źródeł (Roslyn, projekt `Generator`) emituje kod maszyn stanów na podstawie atrybutów z przestrzeni `Abstractions.Attributes` (`[StateMachine]`, `[Transition]`, `[State]`, itd.).
- Emisja kodu odbywa się dwiema drogami:
  - „Ścieżka bazowa/flat” (klasa bazowa `StateMachineCodeGenerator`) – bez rozszerzeń,
  - „Ścieżka unified/Extensions” (`UnifiedStateMachineGenerator`) – wariant z rozszerzeniami (GenerateExtensibleVersion = true), a także agregacja wspólnych ścieżek.
- Logowanie jest realizowane przez per‑maszynowe klasy `{ClassName}Log` dodawane przez `context.AddSource(...)` (AddSource). Nazwa pliku: `Namespace.ClassName.Log.g.cs`. Klasa jest `internal static` i udostępnia metody:
  - `TransitionSucceeded`, `GuardFailed`, `TransitionFailed`,
  - `OnEntryExecuted`, `OnExitExecuted`, `ActionExecuted`,
  - oraz pomocnicze dla HSM/payload (np. `PayloadValidationFailed`, `HierarchicalTransition`, `ActivePath`, itp.).
- Warunek generowania logowania: MSBuild property `FsmGenerateLogging=true` (eksportowane w `FastFsm/build/FastFsm.Net.props`, włączane przez pakiet `FastFsm.Net.Logging`). Flaga `FSM_LOGGING_ENABLED` umożliwia ewentualne warunkowanie kompilacji.

## Co zostało już zrobione (0.8.0.10 → 0.8.0.11)

- Przywrócono model `{ClassName}Log.*(...)` (bez LogAdapter) i uproszczono hint name AddSource.
- Dodano brakujące logi w krytycznych miejscach:
  - Extensions path: w `WriteTransitionLogicSyncWithExtensions(...)` logujemy teraz:
    - `OnExitExecuted` (po OnExit), `ActionExecuted` (po Action), `OnEntryExecuted` (po OnEntry),
    - `TransitionSucceeded` (na końcu ścieżki sukcesu),
    - `GuardFailed` + `TransitionFailed` przy porażce guarda.
  - Brak dopasowania przejścia (flat/HSM): dopisano `TransitionFailed` tuż przed `return false`.
  - OnInitialEntry/OnInitialEntryAsync: dopisano `OnEntryExecuted` (Debug) dla OnEntry wykonywanych przy starcie.
- Dodane diagnostyki generatora (FSM99x) ułatwiające śledzenie AddSource i MSBuild properties.
- Bump do 0.8.0.11: `FastFsm.Net`, `FastFsm.Net.Logging`, `FastFsm.Net.DependencyInjection`; testy zaktualizowano do nowych wersji.

## Obecny stan testów i luki do domknięcia

Po przejściu na 0.8.0.11 trzy testy nadal nie przechodzą:

1) `LoggingIntegrationTests.FullVariant_CompleteScenario_AllLogsPresent`
   - Oczekuje obecności: `ActionExecuted`, `OnEntryExecuted`, `TransitionSucceeded` dla maszyny z rozszerzeniami i payloadem.
   - Generator w ścieżce Extensions emituje już te logi – podejrzenie: test nadal korzysta z nieświeżych plików wygenerowanych / cache NuGet lub występuje różnica co do poziomu/filtrów (IsEnabled).

2) `SpecialCasesLoggingTests.StateMachine_WithStructTypes_LogsCorrectly`
   - Oczekuje `TransitionSucceeded` dla wariantu strukturalnego (enumy o typach wartościowych).
   - Dla płaskiego wariantu non‑payload generator emituje `TransitionSucceeded`; należy zweryfikować, czy fast‑path lub zwykła ścieżka jest w użyciu i czy log jest faktycznie wywoływany.

3) `SpecialCasesLoggingTests.PayloadStateMachine_NullPayloadWithExpectedType_HandledGracefully`
   - Oczekuje `TransitionSucceeded` po `TryFire(Start, payload: null)` (zastępowalność parametrem bezpayloadowym).
   - W ścieżce płaskiej/payload log powinien się pojawić; do potwierdzenia, czy guard/action/OnEntry nie krótką drogą nie kończą przed emisją logu.

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
