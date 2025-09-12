Cel i zakres
- Cel: osiągnąć 100% parytetu testowego (Attribute vs FluentFsm) w FastFsm.Logging.Tests, w oparciu o infrastrukturę macierzy/wrapperów opisaną w testinfrastructure.md oraz doświadczenia z pełnej konwersji FastFsm.Async.Tests.
- Zakres: wszystkie maszyny (bazowe klasy z [StateMachine]) i kluczowe scenariusze logowania (lifecycle, extensions, HSM, payload, internal transitions, extensible version, integracja logowania).

Stan bieżący (szybki inwentarz)
- Pliki i testy: 45 metod testowych (Fact/Theory)
  - LifecycleLoggingTests.cs: 9
  - ExtensionLoggingTests.cs: 9
  - LoggingIntegrationTests.cs: 8
  - SpecialCasesLoggingTests.cs: 8
  - HsmLoggingTests.cs: 5
  - LoggingExamples.cs: 3
  - HsmRuntimeLoggingTests.cs: 3
- Maszyny (16 bazowych klas z [StateMachine]):
  - LoggingIntegrationTests.cs: InitialOnEntryStateMachineActions, FullMultiPayloadMachine
  - Machines.cs: PureStateMachine, BasicStateMachine, PayloadStateMachine, ExtensionsStateMachine, FullStateMachine, MultiPayloadStateMachine
  - SpecialCasesLoggingTests.cs: InternalTransitionMachine, StructStateMachine
  - LifecycleLoggingTests.cs: LifecycleMachine, AsyncLifecycleMachine
  - LoggingExamples.cs: ExampleStateMachine, GuardedStateMachine, ExtensibleMachine
  - HsmRuntimeLoggingTests.cs: HsmMachine (EnableHierarchy)

Plan parytetu (wysokopoziomowy)
1) Wprowadzić infrastrukturę TestHelpers do FastFsm.Logging.Tests (kopie z Async/Tests):
   - IStateMachineTestWrapper, ApiCapabilities, MachineTypeRegistry, StateMachineWrapperFactory,
     MatrixConfig, InitialStateResolver, (opcjonalnie) EnumConverterExtensions.
2) Dla każdej maszyny dodać odpowiednik FluentFsm (jeśli brak), wzorując się na DSL użytym w Async.Tests.
3) Zaimplementować wrappery (Legacy/Fluent) dla każdej maszyny (obsługa extensions i payloadów).
4) Zarejestrować typy enumów i fabryki w rejestrach.
5) Dodać MatrixConfig z inicjalnym stanem, minimalną sekwencją triggerów, payloadami i (opcjonalnie) extension listą.
6) Utworzyć Parity/DualApiMatrixTests (Logging) – test macierzowy dla obu składni, skupiony na smoke (start/tryfire/permitted), bez asercji na treść loga.

Praktyczne porady (nauki z Async parity)
- Namespace i styl:
  - Ujednolić do file-scoped namespace (namespace X.Y;), pojedyncze spacje – ułatwia automaty i generację.
  - Zadbać o pełne kwalifikacje typów w MachineTypeRegistry, zwłaszcza dla typów z plików testowych (zagnieżdżenia i różne przestrzenie nazw).
- FluentFsm i DSL:
  - W Async korzystaliśmy ze stuba `Dsl.FSM`. Tu zalecane jest `Abstractions.Fluent.FSM` lub lokalny alias – ważna jest spójność i brak konfliktów.
  - W maszynach z `GenerateExtensibleVersion = true` odwzorować konfigurację w DSL 1:1.
- Wrappery i sygnatury async:
  - Nie używać nazwanych parametrów typu `ct:` gdy sygnatury różnią się między maszynami; preferować dostępne przeciążenia.
  - Dla payloadów używać `dynamic` przy wywołaniach TryFireAsync/FireAsync, by bezpiecznie przekazać różne typy payloadów.
- Extensions i logowanie:
  - Wrappery powinny umożliwiać przekazanie tablicy `IStateMachineExtension` (np. do testów integracyjnych logowania). W matrixie domyślnie można nie podawać extensions (smoke), a scenariusze walidacji logów zostawić w wyspecjalizowanych testach ( istniejące pliki ExtensionLoggingTests/LoggingIntegrationTests ).
  - W maszynach z `GenerateExtensibleVersion = true` testy matrixowe nie muszą asercyjnie sprawdzać logów – wystarczy, że przejścia przebiegają poprawnie.
- Aliasy nazw i kompletność:
  - W Async dodaliśmy aliasy bazowych nazw (np. `InitialChildMachine` → `InitialChild`) i wpisy 1:1 w MatrixConfig. W Logging zalecane jest 1:1 nazewnictwo na bazie nazw klas, aby test macierzowy obejmował wszystkie 16 maszyn.
- HSM (EnableHierarchy):
  - Dla HsmMachine dodać MachineTypeRegistry i InitialStateResolver (np. Outside/Menu lub Parent/Child), a wrapper zaznaczyć `ApiCapabilities.IsHierarchical`.
- Struktury enum (StructStateMachine):
  - Enumy jako struct – wrappery i rejestry operują na Type i object; zadbać, by enumy były public lub dostępne dla testów.
- Internal transitions:
  - Ustawić `ApiCapabilities.HasInternalTransitions` dla InternalTransitionMachine.
- MultiPayload i DefaultPayload:
  - MultiPayloadStateMachine/FullMultiPayloadMachine → `HasMultiPayloads`. PayloadStateMachine (DefaultPayloadType) → `HasDefaultPayload`.

Proponowana mapowanie maszyn → cechy
- Core/basic: PureStateMachine, BasicStateMachine → Caps: None
- Payload: PayloadStateMachine (DefaultPayloadType) → HasDefaultPayload, MultiPayloadStateMachine/FullMultiPayloadMachine → HasMultiPayloads
- Extensions: ExtensionsStateMachine, ExtensibleMachine, AsyncExtensionsMachine (wzór z LoggingExamples/ExtensionTests) → dodat. obsługa extensions
- Lifecycle: LifecycleMachine (sync), AsyncLifecycleMachine (HasAsync)
- HSM: HsmMachine (IsHierarchical)
- SpecialCases: InternalTransitionMachine (HasInternalTransitions), StructStateMachine (specyficzne enumy struct)
- Integration: InitialOnEntryStateMachineActions (sekwencja z OnEntry), inne z LoggingIntegrationTests
- Examples: ExampleStateMachine, GuardedStateMachine (guards/actions logujące zdarzenia)

Kroki implementacyjne (checklista)
1) Skopiować do FastFsm.Logging.Tests/TestHelpers:
   - IStateMachineTestWrapper.cs, ApiCapabilities.cs, MachineTypeRegistry.cs, StateMachineWrapperFactory.cs,
     MatrixConfig.cs, InitialStateResolver.cs
2) W MachineTypeRegistry dodać pary enumów dla wszystkich 16 maszyn (State/Trigger; kwalifikacje po pełnej nazwie).
3) W StateMachineWrapperFactory:
   - Dodać fabryki CreateX(...) dla każdej maszyny i wrappery Legacy/Fluent (opcjonalnie wariant z parametrem extensions).
   - Ustawić Caps: HasAsync (dla maszyn async), HasDefaultPayload/HasMultiPayloads, HasInternalTransitions, IsHierarchical – zgodnie z maszyną.
4) W MatrixConfig:
   - Dla każdej maszyny: InitialState, minimalny TriggerSequence (np. 1–3 kroki), Payloady (tam gdzie wymagane), opcjonalnie pusta tablica extensions.
5) Dodać Features/Parity/DualApiMatrixTests.cs (Logging) – analogiczny do Async.

Minimalne przykłady (schematy, nie-kod)
- Wrapper z extensions:
  - Konstruktor przyjmuje `(initialState, IStateMachineExtension[]? exts = null)` → przekazuje do maszyny.
  - TryFireAsync/FireAsync: rzutowanie triggera na właściwy enum, payload dynamic.
- MatrixConfig wpis (PayloadStateMachine z default payload):
  - InitialState: „Initial”, TriggerSequence: ["Start"], Payloads: [new TestPayload { … }].

Priorytety wdrożenia (sugerowane)
1) Najpierw maszyny bazowe z Machines.cs (8 szt.) + HsmMachine → szybkie przejście cross.
2) Następnie Lifecycle (2 szt.) i SpecialCases (2 szt.).
3) Na końcu Integration/Examples (4 szt.).

Ryzyka i tipy z Async:
- Kwalifikacje typów w rejestrze – pełne ścieżki do typów z plików testowych.
- Unikać named args `ct:` w wywołaniach – różne sygnatury między maszynami.
- Używać aliasów dla bazowych nazw w fabryce, jeśli pojawią się równoważne skróty.
- Test macierzowy: smoke test (Start, TryFire, GetPermittedTriggers), bez sprawdzania zawartości logów (to robią dedykowane testy loggingu).

Docelowy rezultat
- 100% parytetu implementacyjnego (Attribute + FluentFsm) dla 16 maszyn.
- 100% parytetu testowego w MatrixConfig – każdy bazowy typ maszyny ma wpis i wrappery Legacy/Fluent.
- DualApiMatrixTests (Logging) przechodzi przez wszystkie wpisy i weryfikuje podstawowe działania na obu składniach.

