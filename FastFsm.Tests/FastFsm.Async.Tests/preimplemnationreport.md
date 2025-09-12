**Cel**
- Celem jest wprowadzenie 100% parytetu testów w FastFsm.Async.Tests (Atrybutowa vs Fluent/DSL) w oparciu o infrastrukturę macierzy/wrapperów z FastFsm.Tests (testinfrastructure.md).
- Ten raport opisuje stan bieżący FastFsm.Async.Tests, wskazuje niespójności i luki w parytecie oraz proponuje ujednolicenia przed refaktorem.

**Podstawa Analizy**
- Przegląd: `testinfrastructure.md` (opis infrastruktury parytetu w FastFsm.Tests).
- Projekty: `FastFsm.Async.Tests`, porównawczo `FastFsm.Tests` (dla konwencji i wzorca).

**Drzewo Projektów**
- `FastFsm.Async.Tests` (katalogi istotne dla testów)
  - Features/
    - Cancellation/
    - Concurrency/
    - Core/
    - Exceptions/
    - Extensions/
    - Hsm/
      - CompileTime/
      - Runtime/
    - Lifecycle/
    - Payload/
- `FastFsm.Tests` (dla odniesienia do infrastruktury parytetu)
  - Features/
    - Core/, EdgeCases/, Exceptions/, Extensions/, Hsm/{Common, CompileTime, Runtime}/, Integration/, Lifecycle/, Parity/, Payload/, Performance/
  - Machines/
  - TestHelpers/ (IStateMachineTestWrapper, StateMachineWrapperFactory, MachineTypeRegistry, MatrixConfig, itp.)

**Kategorie Testów (FastFsm.Async.Tests)**
- Core: 10 testów ([Fact]/[Theory]).
- Payload: 20 testów.
- Cancellation: 25 testów.
- Hsm (CompileTime/Runtime): 13 testów (testy funkcjonalne HSM).
- Exceptions: 11 testów.
- Extensions: 8 testów.
- Lifecycle: 2 testy.
- Concurrency: 1 test.

**Maszyny i Parytet Implementacyjny**
- W FastFsm.Async.Tests zidentyfikowano 37 unikalnych „bazowych” maszyn oznaczonych [StateMachine].
- Rozkład parytetu implementacji (czy istnieją obie wersje: Atrybutowa i Fluent):
  - 17 bazowych maszyn posiada parę (Atrybutowa + FluentFsm).
  - 20 bazowych maszyn istnieje wyłącznie jako Atrybutowe (brak odpowiednika FluentFsm).
  - 0 maszyn występuje tylko jako FluentFsm.
- Przykłady par „obie implementacje”: SimpleAsyncMachine, RcMachine, SpecificationComplianceMachine, TokenMachine, TinyAsyncHsm, (szeroki zestaw maszyn HSM w Runtime), ExceptionAsyncMachine, AsyncExtensionsMachine.
- Przykłady „tylko Atrybutowa”: BasicAsyncPayloadMachine, OverloadedAsyncMachine, MultiPayloadAsyncMachine, ExceptionAsyncPayloadMachine, ConcurrentAsyncPayloadMachine, InitialOnEntryAsyncPayloadMachine, itd.

**Parytet Wykorzystania w Testach (Atrybutowa vs FluentFsm)**
- Dla 17 bazowych maszyn z obiema implementacjami sprawdzono użycia w testach (instancje `new <Base>(...)` vs `new <Base>FluentFsm(...)`).
- Wynik globalnie:
  - Maszyny, gdzie testy używają obu implementacji (pełny parytet testów): 3 (SimpleAsyncMachine, SpecificationComplianceMachine, TinyAsyncHsm).
  - Używane wyłącznie Atrybutowe: 8 (np. InitialChildMachine, ShallowHistoryMachine, DeepHistoryMachine, InternalMachine, PriorityMachine, ChildOverridesMachine, SourceOrderTieMachine, InheritanceMachine — głównie HSM Runtime).
  - Używane wyłącznie FluentFsm: 5 (AsyncExtensionsMachine, ExceptionAsyncMachine, PayloadMachine, RcMachine, TokenMachine).

**Parytet wg Kategorii (implementacje vs użycia)**
- Implementacje (bazowe maszyny z [StateMachine] w danej kategorii):
  - Hsm: 9 bazowych, wszystkie mają parę (Atrybutowa+FluentFsm).
  - Cancellation: 8 bazowych, pary: 4, tylko Atrybutowe: 4.
  - Payload: 7 bazowych, pary: 0, tylko Atrybutowe: 7.
  - Exceptions: 6 bazowych, pary: 0, tylko Atrybutowe: 6.
  - Extensions: 3 bazowe, pary: 1, tylko Atrybutowe: 2.
  - Core: 1 bazowa, para: 1.
  - Concurrency: 1 bazowa, para: 1.
- Użycia w testach (dla bazowych posiadających dwie implementacje):
  - Hsm: 1 z par używana w obu wariantach; 8 używane tylko jako Atrybutowe.
  - Cancellation: 1 z par używana w obu wariantach; 2 tylko FluentFsm; 4 tylko Atrybutowe.
  - Core: parytet pełny (oba warianty użyte).
  - Concurrency: tylko FluentFsm użyte.
  - Extensions: 1 tylko FluentFsm; 2 tylko Atrybutowe; brak pełnego parytetu.
  - Payload, Exceptions: brak par implementacyjnych → brak parytetu wykorzystania.

**Syntaktyka i Wzorce Nazewnicze**
- Warianty implementacyjne:
  - Wersja Atrybutowa: klasy bez sufiksu, np. `SimpleAsyncMachine`.
  - Wersja Fluent: sufiks `FluentFsm`, np. `SimpleAsyncMachineFluentFsm` (różni się od FastFsm.Tests, gdzie stosowane są nazwy typu `PayloadStateMachineFluent`/`Legacy`).
- DSL:
  - W FastFsm.Async.Tests obecny jest lokalny stub DSL w `Dsl.cs` (`namespace Dsl; class FSM...`), używany w klasach FluentFsm (np. `using Dsl;` w HSM Runtime). W FastFsm.Tests używane jest `Abstractions.Fluent.FSM` bez lokalnego stuba.
  - Uwaga: dla integracji z infrastrukturą z FastFsm.Tests lepsze będzie spójne użycie `Abstractions.Fluent` lub jasne odseparowanie lokalnego stuba, aby wrappery/macierze nie wymagały wyjątków.
- Enums i nazwy typów:
  - Enums są często zagnieżdżone lokalnie w plikach testowych (HSM Runtime), co utrudnia centralną rejestrację w `MachineTypeRegistry` w stylu FastFsm.Tests. To można zachować (rejestrator wspiera typy z różnych namespace), ale wymaga konsekwentnego mapowania.
  - Brak jednolitego sufiksu „Legacy” — w Async nie ma oddzielnej klasy „Legacy”; wariant atrybutowy pełni rolę „Legacy” w nomenklaturze FastFsm.Tests.

**Namespace i Konwencje (niespójności)**
- Nadmiarowe spacje po słowie `namespace` w wielu plikach, np. `namespace  FastFsm.Async.Tests.Features.Core;` (podwójna spacja). Dotyczy m.in.:
  - Features/Core/BasicAsyncStateMachineTests.cs
  - Features/Hsm/Runtime/HierarchicalAsyncRuntimeTests.cs
  - Features/Concurrency/RcMachine.cs
  - Features/Exceptions/AsyncExceptionHandlingTests.cs
  - Features/Cancellation/* (kilka plików)
  - Features/Extensions/*
  - Features/Payload/*
- Błędny namespace w `ExceptionAsyncMachine.cs`: `namespace FastFsmTests.Tests` (powinno być `FastFsm.Async.Tests` lub `FastFsm.Async.Tests.Features.Exceptions`).
- Mieszanie stylów plików: w Async `;` na końcu deklaracji namespace (file‑scoped) jest używany, ale z niespójnościami spacji; w innych miejscach block‑scoped. Zalecana unifikacja (np. file‑scoped z pojedynczą spacją).

**Zgodność z Infrastrukturą z FastFsm.Tests (testinfrastructure.md)**
- Braki w Async względem wzorca:
  - Brak `TestHelpers/` (wrappery, fabryka, rejestr typów, MatrixConfig, resolver stanu początkowego, rozszerzenia konwersji enumów).
  - Brak testów macierzowych w stylu `Features/Parity/DualApiMatrixTests.cs` (testy jednolite dla obu składni przez wrappery).
  - Rozjazd nazewnictwa klas Fluent: `...FluentFsm` vs wzorzec `...Fluent`/`...Legacy` w `FastFsm.Tests/Machines/*`. Nie jest to blokujące, ale wymaga mapowania w `MachineTypeRegistry` i `StateMachineWrapperFactory`.
  - Lokalne, zagnieżdżone definicje enumów i klas testowych (szczególnie HSM Runtime) – do integracji z rejestrem typów potrzebne jawne wpisy.

**Ryzyka i Elementy „niepodlegające regułom”**
- `ExceptionAsyncMachine.cs` — inny namespace niż reszta testów; może „omijać” filtry/konwencje (np. selektory testów/Traits).
- Lokalne DSL `Dsl.FSM` — jeśli pozostanie, wrappery muszą ignorować tę różnicę; preferowane jest użycie wspólnego DSL z `Abstractions.Fluent` dla spójności.
- Brak atrybutów `[Trait]`/kategoryzacji w Async — w FastFsm.Tests sporo testów ma `[Trait("Category", ...)]`. Jeżeli CI lub raportowanie opiera się na kategoriach, Async nie dostarczy tych metadanych.

**Podsumowanie Stanu Bieżącego**
- Implementacje:
  - 37 bazowych maszyn; 17 ma komplet (Atrybutowa+FluentFsm), 20 tylko Atrybutowa.
  - W HSM Runtime wszystkie maszyny mają parę implementacji (dobrze rokuje dla integracji z macierzą).
- Testy:
  - Pełny parytet użycia (oba warianty testowane) występuje rzadko — 3/17 par.
  - Wiele scenariuszy testuje tylko jedną składnię (HSM głównie Atrybutową; Concurrency/Extensions/Exceptions często FluentFsm).
  - Brak wspólnej infrastruktury wrapperów i testów macierzowych.
- Jakość spójności:
  - Niespójności namespace (podwójne spacje, 1 ewidentnie błędny namespace).
  - Nazewnictwo Fluent (`FluentFsm`) odbiega od wzorca z FastFsm.Tests (ale jest systematyczne wewnątrz Async).

**Rekomendacje Ujednolicenia Przed Refaktorem**
- Namespace i styl
  - Ujednolicić namespace do `FastFsm.Async.Tests.[Features.[...]]` (file‑scoped, pojedyncza spacja), poprawić `ExceptionAsyncMachine.cs`.
  - Usunąć podwójne spacje po `namespace` we wszystkich plikach.
- Nazewnictwo maszyn
  - Zachować `...FluentFsm` lub rozważyć dopasowanie do wzorca `...Fluent`/`...Legacy`. Jeśli zostaje `FluentFsm`, dopisać odpowiednie mapowania w rejestrze typów i fabrykach wrapperów.
- DSL
  - Preferencja: korzystać z `Abstractions.Fluent.FSM`; jeśli lokalny `Dsl.FSM` ma zostać, zapewnić, że nie koliduje z integracją (np. alias `using FSM = Abstractions.Fluent.FSM;`).
- Infrastruktura parytetu
  - Dodać `TestHelpers/` w Async (kopie: IStateMachineTestWrapper, ApiCapabilities, StateMachineWrapperFactory, MachineTypeRegistry, MachineTypes, MatrixConfig, InitialStateResolver, EnumConverterExtensions) i skonfigurować z maszynami Async.
  - Zarejestrować maszyny (szczególnie HSM Runtime + wybrane Payload/Cancellation/Exceptions) w `MachineTypeRegistry` oraz dodać fabryki wrapperów.
  - Dodać testy macierzowe (odpowiednik `DualApiMatrixTests`) oraz testy parytetu pokrycia (odpowiednik `CoverageParityTests`).
- Pokrycie parytetem
  - Priorytet I: HSM Runtime — kompletna para implementacji już istnieje; dopisać wrappery + matrix.
  - Priorytet II: Payload/Cancellation/Exceptions — dorobić brakujące warianty FluentFsm (lub odwrotnie) i objąć matrixem.
  - Priorytet III: Concurrency/Extensions — dopisać brakujące warianty i zweryfikować zachowania specyficzne dla async.

**Metryki Startowe (baseline)**
- Testy: 90 metod ([Fact]/[Theory]) w FastFsm.Async.Tests.
- Maszyny: 37 bazowych; 17 par implementacyjnych; parytet testowego użycia: 3 pełne, 13 jednostronnych (8 tylko Atrybutowa, 5 tylko FluentFsm), 1 para niewykorzystana dwustronnie w danej kategorii.

**Następne Kroki**
- Skopiować i dostosować infrastrukturę `TestHelpers` z FastFsm.Tests do Async.
- Przygotować `MatrixConfig` dla maszyn Async (na start HSM Runtime + proste Core/Concurrency).
- Dodać wrappery i fabryki dla każdej maszyny z matrixa; spiąć `MachineTypeRegistry` (uwzględnić typy enumów lokalnych).
- Uruchomić testy macierzowe; iteracyjnie uzupełniać brakujące warianty implementacji i rozszerzać matrix do 100% parytetu.

