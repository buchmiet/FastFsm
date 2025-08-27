# Analiza dziedziczenia i zależności klas generatorów FastFSM

## Struktura hierarchii klas

### 1. StateMachineGenerator
**Lokalizacja:** `/Generator/Generator.cs:26`
**Przestrzeń nazw:** `Generator`
**Typ:** Główny generator Roslyn (Incremental Generator)

#### Charakterystyka:
- Implementuje interfejs `IIncrementalGenerator`
- Jest klasą publiczną z atrybutem `[Generator]`
- Nie dziedziczy z żadnej klasy bazowej (poza Object)
- Nie ma klas pochodnych

#### Główne odpowiedzialności:
1. **Inicjalizacja generatora** (`Initialize` - linia 249)
   - Konfiguruje pipeline generatora inkrementalnego
   - Wykrywa klasy z atrybutem `[StateMachine]`
   - Filtruje kandydatów do generowania kodu

2. **Przetwarzanie kandydatów** (`ProcessCandidateAndGenerate` - linia 383)
   - Parsuje model maszyny stanów za pomocą `StateMachineParser`
   - Tworzy instancję `UnifiedStateMachineGenerator` do generowania kodu (linia 597)
   - Wywołuje `generator.Generate()` (linia 599)
   - Obsługuje generowanie dodatkowych plików (logging, DI)

3. **Generowanie raportu odkrycia** (`GenerateDiscoveryDumpV2` - linia 337)
   - Tworzy plik z listą wszystkich znalezionych kandydatów

### 2. StateMachineCodeGenerator (klasa abstrakcyjna)
**Lokalizacja:** `/Generator/SourceGenerators/StateMachineCodeGenerator.cs:21`
**Przestrzeń nazw:** `Generator.SourceGenerators`
**Typ:** Abstrakcyjna klasa bazowa dla generatorów kodu

#### Charakterystyka:
- Klasa abstrakcyjna (`internal abstract class`)
- Bazowa klasa dla wszystkich wariantów generatorów
- Ma jedną klasę pochodną: `UnifiedStateMachineGenerator`
- Zapewnia infrastrukturę i szablony emisyjne

#### Główne komponenty:
1. **Pola chronione:**
   - `Model: StateMachineModel` - model maszyny stanów
   - `Sb: IndentedStringBuilder` - builder do generowania kodu
   - `TypeHelper: TypeSystemHelper` - helper do obsługi typów
   - `IsAsyncMachine: bool` - flaga maszyny asynchronicznej
   - `ShouldGenerateLogging: bool` - flaga generowania logowania

2. **Metody wirtualne i abstrakcyjne:**
   - `Generate()` - **metoda wirtualna** (nie abstrakcyjna!) z implementacją w klasie bazowej (linia 40):
     ```csharp
     public virtual string Generate()
     {
         WriteHeader();
         WriteNamespaceAndClass();
         return Sb.ToString();
     }
     ```
   - `WriteNamespaceAndClass()` - **metoda abstrakcyjna** (linia 2545), musi być implementowana w klasach pochodnych
   - `WriteHeader()` - generuje nagłówek pliku z using'ami
   - `WriteMethodAttribute()` - generuje atrybuty metod używane przez pochodną

3. **Wsparcie dla HSM (Hierarchical State Machines):**
   - `WriteHierarchyArrays()` - generuje statyczne tablice dla hierarchii stanów:
     - `g_parent` - tablica rodziców
     - `g_depth` - głębokość w hierarchii
     - `g_initialChild` - początkowe dziecko dla stanów kompozytowych
     - `g_history` - tryb historii (None/Shallow/Deep)
   - `WriteHierarchyMethods()` - generuje metody dla HSM:
     - `IsIn()` - sprawdza czy stan jest w aktywnej ścieżce
     - `GetActivePath()` - zwraca ścieżkę od korzenia do liścia
     - Wersja ze `Span<T>` dla zero-alokacji
   - `WriteHierarchyRuntimeFieldsAndHelpers()` - **WAŻNE: obecnie NIE emituje kodu** (linie 313-319)
     - Komentarz w kodzie: "All runtime fields and methods are now in the base class"
     - Runtime'owe pola i metody są w klasie bazowej runtime'u, nie w generowanym kodzie
   - `GenerateHsmPermittedTriggerArrays()` - prekomputacja tablic permisji dla HSM
     - Generuje tablicę `s_perm__Mask` ze wszystkimi kombinacjami masek

4. **Metoda dla płaskiego rdzenia bez rozszerzeń:**
   - `WriteTransitionLogicForFlatNonPayload()` (linia 520) - rdzeń logiki przejścia dla płaskiej maszyny bez payload i bez Extensions
   - Używana przez pochodną w wariantach bez rozszerzeń

### 3. UnifiedStateMachineGenerator
**Lokalizacja:** `/Generator/SourceGenerators/UnifiedStateMachineGenerator.cs:21`
**Przestrzeń nazw:** `Generator.SourceGenerators`
**Typ:** Konkretna implementacja generatora

#### Charakterystyka:
- Dziedziczy z `StateMachineCodeGenerator`
- Klasa wewnętrzna (`internal class`)
- Zunifikowany generator obsługujący wszystkie warianty przez flagi funkcji

#### Flagi funkcji (feature flags):
```csharp
private bool HasPayload => Model.GenerationConfig.HasPayload;
private bool HasExtensions => Model.GenerationConfig.HasExtensions;
private bool ExtensionsOn => HasExtensions || IsExtensionsVariant();
private bool HasOnEntryExit => Model.GenerationConfig.HasOnEntryExit;
private bool IsHierarchical => Model.HierarchyEnabled;
private bool HasMultiPayload => Model.TriggerPayloadTypes?.Any() == true;
```

#### Nadpisane metody:
1. **Generate()** (linia 38)
   - Nadpisuje wirtualną metodę z klasy bazowej
   - **Zachowuje identyczny szkielet** co bazowa implementacja:
   ```csharp
   public override string Generate()
   {
       WriteHeader();                    // Z klasy bazowej
       WriteNamespaceAndClass();         // Własna implementacja abstrakcyjnej metody
       return Sb.ToString();
   }
   ```

2. **WriteNamespaceAndClass()** (linia 45)
   - Implementacja abstrakcyjnej metody z klasy bazowej
   - Generuje namespace i strukturę klas
   - Obsługuje **zagnieżdżone klasy kontenerowe** przez `Model.ContainerClasses`
   - Generuje interfejs `I{ClassName}` i klasę częściową

#### Komponenty pomocnicze:
- **ExtensionsFeatureWriter** (`_ext`) - osobny writer używany warunkowo gdy `ExtensionsOn == true`:
  - `_ext.WriteFields()` - emituje pola `_extensions` i `_extensionRunner`
  - `_ext.WriteConstructorBody()` - inicjalizacja rozszerzeń
  - `_ext.WriteManagementMethods()` - metody zarządzania rozszerzeniami
  - W logice przejść używa hooków: `RunBeforeTransition`, `RunGuardEvaluation`, `RunGuardEvaluated`, `RunAfterTransition`

## Klasy bazowe runtime'u wygenerowanych maszyn stanów

### Bazowe klasy runtime (z biblioteki FastFsm)

**Wygenerowana klasa maszyny stanów dziedziczy po jednej z następujących klas bazowych:**

1. **Dla maszyn synchronicznych:**
   - `StateMachineBase<TState, TTrigger>` - podstawowa klasa bazowa
   - Zawiera wszystkie runtime'owe pola i metody dla maszyn stanów

2. **Dla maszyn asynchronicznych:**
   - `AsyncStateMachineBase<TState, TTrigger>` - asynchroniczna klasa bazowa
   - Rozszerza funkcjonalność o metody async/await

3. **Dla maszyn hierarchicznych (HSM):**
   - Runtime'owe pola HSM (historia stanów, ścieżka aktywna) są w tych samych klasach bazowych
   - **Generator NIE emituje pól runtime** - wszystkie są w klasie bazowej
   - Generator emituje tylko statyczne tablice hierarchii i override'y właściwości

> **KLUCZOWY PUNKT:** Metoda `WriteHierarchyRuntimeFieldsAndHelpers()` w generatorze **NIE emituje już żadnych pól ani metod**. Wszystkie runtime'owe elementy HSM są zaimplementowane w klasach bazowych runtime'u, a nie są generowane.

## Wygenerowany interfejs I{ClassName}

### Pełna lista członków interfejsu

Generator emituje interfejs `I{ClassName}` dziedziczący po odpowiednim interfejsie bazowym:

**Dla maszyn synchronicznych (bez rozszerzeń):**
- Dziedziczy po: `IStateMachine<TState, TTrigger>`
- Członkowie:
  - `TState CurrentState { get; }` - bieżący stan
  - `bool IsStarted { get; }` - czy maszyna jest uruchomiona
  - `void Start()` - uruchamia maszynę
  - `bool TryFire(TTrigger trigger)` - próba przejścia
  - `bool TryFire(TTrigger trigger, TPayload payload)` - z payloadem (jeśli HasPayload)
  - `void Fire(TTrigger trigger)` - przejście z wyjątkiem przy błędzie
  - `void Fire(TTrigger trigger, TPayload payload)` - z payloadem (jeśli HasPayload)
  - `bool CanFire(TTrigger trigger)` - sprawdza możliwość przejścia
  - `bool CanFire(TTrigger trigger, TPayload payload)` - z payloadem (jeśli HasPayload)
  - `IReadOnlyList<TTrigger> GetPermittedTriggers()` - lista dozwolonych triggerów
  - `bool IsIn(TState state)` - czy stan jest aktywny (dla HSM rozszerzone)
  - `IReadOnlyList<TState> GetActivePath()` - ścieżka aktywnych stanów (dla HSM)

**Dla maszyn asynchronicznych (bez rozszerzeń):**
- Dziedziczy po: `IAsyncStateMachine<TState, TTrigger>`
- Członkowie (wszystkie async):
  - `ValueTask StartAsync(CancellationToken ct = default)`
  - `ValueTask<bool> TryFireAsync(TTrigger trigger, CancellationToken ct = default)`
  - `ValueTask<bool> TryFireAsync(TTrigger trigger, TPayload payload, CancellationToken ct = default)`
  - `Task FireAsync(TTrigger trigger, CancellationToken ct = default)`
  - `Task FireAsync(TTrigger trigger, TPayload payload, CancellationToken ct = default)`
  - `ValueTask<bool> CanFireAsync(TTrigger trigger, CancellationToken ct = default)`
  - `ValueTask<bool> CanFireAsync(TTrigger trigger, TPayload payload, CancellationToken ct = default)`
  - `ValueTask<IReadOnlyList<TTrigger>> GetPermittedTriggersAsync(CancellationToken ct = default)`
  - `ValueTask<bool> IsInAsync(TState state, CancellationToken ct = default)`
  - `ValueTask<IReadOnlyList<TState>> GetActivePathAsync(CancellationToken ct = default)`

**Dla maszyn z rozszerzeniami (Extensions):**
- Sync: dziedziczy po `IExtensibleStateMachineSync<TState, TTrigger>`
- Async: dziedziczy po `IExtensibleStateMachineAsync<TState, TTrigger>`
- Dodatkowe członkowie:
  - `void AddExtension(IStateMachineExtension extension)`
  - `void RemoveExtension(IStateMachineExtension extension)`
  - `IReadOnlyList<IStateMachineExtension> GetExtensions()`

## Przepływ wywołań między klasami

### Sekwencja generowania kodu:

1. **StateMachineGenerator.Initialize()**
   - Konfiguruje pipeline Roslyn
   - Rejestruje `ProcessCandidateAndGenerate` jako handler

2. **StateMachineGenerator.ProcessCandidateAndGenerate()**
   ```csharp
   // Linia 597: Tworzenie generatora (potwierdzone w kodzie)
   var generator = new Generator.SourceGenerators.UnifiedStateMachineGenerator(model);
   
   // Linia 599: Generowanie kodu (potwierdzone w kodzie)
   var source = generator.Generate();
   ```

3. **UnifiedStateMachineGenerator.Generate()**
   ```csharp
   // Linia 40-43
   WriteHeader();                    // Z klasy bazowej
   WriteNamespaceAndClass();         // Własna implementacja abstrakcyjnej metody
   return Sb.ToString();
   ```

4. **UnifiedStateMachineGenerator.WriteNamespaceAndClass()**
   - Obsługuje namespace
   - Obsługuje zagnieżdżone klasy kontenerowe (`Model.ContainerClasses`)
   - Wywołuje `WriteContainingTypesAndClass()` (linia 56/61)
   - Ta z kolei wywołuje:
     - `WriteInterface()` (linia 70) - generuje `I{ClassName}`
     - `WriteClass()` (linia 71) - generuje klasę częściową

5. **UnifiedStateMachineGenerator.WriteClass()**
   Generuje całą zawartość klasy w **ustalonej kolejności** (dokładnie jak w kodzie):
   - `WriteFields()` (linia 120) - pola instancji
   - `WriteActionExceptionHook()` (linia 122) - opcjonalny partial hook dla wyjątków akcji
   - `WriteConstructor()` (linia 123)
   - `WriteStartMethods()` (linia 124) - **warunkowe**: wywołuje bazowy `WriteStartMethod()` tylko gdy `IsHierarchical || HasOnEntryExit`
   - `WriteInitialEntryMethods()` (linia 125) - własne implementacje sync/async
   - `WriteTryFireMethods()` (linia 126) - publiczne API + rdzeń wewnętrzny
   - `WriteFireMethods()` (linia 127) - metody Fire z walidacją
   - `WriteCanFireMethods()` (linia 128)
   - `WriteGetPermittedTriggersMethods()` (linia 129)
   - **Warunkowo** dla rozszerzeń: `_ext.WriteManagementMethods()` (linia 132)
   - `WriteStructuralApiMethods()` (linia 134)
   - `WriteHierarchyMethods()` (linia 135) - dla HSM
   - `WriteGuardHelperMethods()` (linia 140) - dla sync lub HSM

## Specyficzne implementacje i optymalizacje

### Podział odpowiedzialności dla GetPermittedTriggers()

**Kto co robi - w jednej linii:**
> **Flat FSM:** tablice per stan generuje **UnifiedStateMachineGenerator** | **HSM:** tablice masek generuje **StateMachineCodeGenerator** (baza)

1. **Dla flat FSM** - odpowiedzialność **UnifiedStateMachineGenerator**:
   - Metoda `WritePermittedTriggerArrays()` (linia 257)
   - Emituje statyczne tablice `s_perm__{stateFieldSuffix}` per stan
   - Obsługuje maski guardów dla efektywnego wyboru dozwolonych triggerów
   - Optymalizacja zero-alokacji przez prekomputację

2. **Dla HSM** - odpowiedzialność **StateMachineCodeGenerator** (baza):
   - Metoda `GenerateHsmPermittedTriggerArrays()` (linia 146)
   - Generuje tablicę `s_perm__Mask` z wszystkimi możliwymi kombinacjami masek
   - Używa podejścia "mask table" dla wszystkich kombinacji

### Emisja pomocniczych metod guardów (UnifiedStateMachineGenerator)
Emituje **pary metod per przejście** z guardem:
- `Guard__{from}__{trigger}(object? payload)` - bezpośrednie wywołanie guarda bez try/catch
- `EvaluateGuard__{from}__{trigger}(object? payload)` - wrapper z obsługą wyjątków:
  ```csharp
  #if FASTFSM_SAFE_GUARDS
  try { return Guard__{from}__{trigger}(payload); }
  catch (OperationCanceledException) { return false; }
  catch (Exception) { return false; }
  #else
  return Guard__{from}__{trigger}(payload);
  #endif
  ```

### Metody Start i Initial Entry

1. **WriteStartMethods()** w UnifiedStateMachineGenerator:
   - Wywołuje bazowy `WriteStartMethod()` **tylko gdy** `IsHierarchical || HasOnEntryExit`
   - Bazowa metoda zawsze emituje `DescendToInitialIfComposite()` dla HSM przed `base.Start()`

2. **Initial Entry - różne strategie alokacji:**
   - **Sync** (`WriteOnInitialEntryMethod`): 
     - Używa `stackalloc int[depth]` dla depth ≤ 128
     - Fallback na `new int[depth]` dla większych głębokości
     - Zero-alokacyjne podejście gdzie możliwe
   - **Async** (`WriteOnInitialEntryAsyncMethod`): 
     - Używa `ArrayPool<int>.Shared.Rent(depth)`
     - Nie może użyć `Span` ze względu na `await`
     - Zwraca bufor w bloku `finally`

### Delegacja do klasy bazowej

**Rdzeń przejścia bez rozszerzeń:**
- Dla wariantu **flat, non-payload, bez Extensions** UnifiedStateMachineGenerator deleguje do bazowej metody:
  ```csharp
  // Linia 1197 w UnifiedStateMachineGenerator
  base.WriteTransitionLogicForFlatNonPayload(transition, stateTypeForUsage, triggerTypeForUsage);
  ```
- Dla wariantu **z Extensions** generuje własną wersję z try/catch i hookami rozszerzeń

### Optymalizacje Fast Path

#### Tabela warunków fast-path

| Fast Path | Flat/HSM | Sync/Async | Guards | OnEntry/Exit | Payload | Extensions | Jeden trigger | Priorytety | Inne warunki |
|-----------|----------|------------|--------|--------------|---------|------------|---------------|------------|---------------|
| **Pure Basic** | Flat | Sync | ❌ | ❌ | ❌ | ❌ | ✅ (dokładnie 1) | N/A | Max 1 przejście/stan, ≥2 stany |
| **HSM Guardless** | HSM | Sync | ❌ | ✅/❌ | ❌ | ❌ | ❌ | Równe (=0) | Max 1 przejście per (stan,trigger) |

✅ = wymagane | ❌ = niedozwolone | ✅/❌ = dozwolone ale nie wymagane | N/A = nie dotyczy

#### 1. Pure Basic Fast Path (`IsPureBasicFastPath` - linia 875)
**Warunki:**
- Flat (nie HSM), sync, bez payload/guardów/akcji/extensions/OnEntryExit
- **Dokładnie jeden trigger** dla wszystkich przejść
- Co najwyżej jedno przejście per stan
- Co najmniej 2 stany (dla marginalnego zysku)

**Implementacja (`EmitTryFireInternalFastPath` - linia 931):**
```csharp
if (trigger != {jedyny_trigger}) return false;
switch (_currentState)
{
    case State.A: _currentState = State.B; [log]; return true;
    case State.B: _currentState = State.C; [log]; return true;
    // ...
    default: return false;
}
```

#### 2. HSM Guardless Equal Priority Fast Path (`IsHsmGuardlessEqualPriorityFastPath` - linia 970)
**Warunki:**
- HSM, sync, bez guardów
- Równe priorytety (wszystkie Priority == 0)
- Maksymalnie jedno przejście per (stan, trigger)
- Brak multi-transitions na tym samym (state, trigger)

**Implementacja (`EmitHsmTryFireFastPath` - linia 993):**
- Spacer w górę hierarchii od bieżącego stanu
- Strategia "first-match wins" - pierwsze dopasowanie wygrywa
- Brak dalszej analizy po znalezieniu dopasowania

## Integracja z rozszerzeniami (Extensions)

### ExtensionsFeatureWriter
Osobny komponent używany gdy `ExtensionsOn == true`:

1. **Pola** (`_ext.WriteFields()`):
   - `_extensions: List<IStateMachineExtension>` - lista rozszerzeń
   - `_extensionRunner: ExtensionRunner` - runner do wykonywania hooków

2. **Konstruktor** (`_ext.WriteConstructorBody()`):
   - Inicjalizacja listy rozszerzeń
   - Konfiguracja ExtensionRunner

3. **Metody zarządzania** (`_ext.WriteManagementMethods()`):
   - Dodawanie/usuwanie rozszerzeń
   - Metody diagnostyczne

4. **Hooki w logice przejść** (używane w generowanym kodzie):
   - `_extensionRunner.RunBeforeTransition(_extensions, ctx)`
   - `_extensionRunner.RunGuardEvaluation(_extensions, ctx)`
   - `_extensionRunner.RunGuardEvaluated(_extensions, ctx, guardResult)`
   - `_extensionRunner.RunAfterTransition(_extensions, ctx, success)`

5. **Interfejsy**:
   - Sync: `IExtensibleStateMachineSync<TState, TTrigger>`
   - Async: `IExtensibleStateMachineAsync<TState, TTrigger>`

## Ważne szczegóły implementacyjne

### Hierarchical State Machine (HSM)
1. **Tablice statyczne** (generowane przez bazę):
   - Wszystkie tablice hierarchii są statyczne i prekomputowane
   - Używane przez runtime do nawigacji po hierarchii

2. **Runtime fields** - **WAŻNA ZMIANA**:
   - `WriteHierarchyRuntimeFieldsAndHelpers()` **NIE emituje już żadnego kodu**
   - Runtime'owe pola i metody są w klasie bazowej runtime'u (np. `HierarchicalStateMachineBase`)
   - Generator tylko emituje tablice statyczne i overrides właściwości

3. **LCA (Lowest Common Ancestor)** - **ZAKTUALIZOWANE 2025-08-27**:
   - Generator **nie implementuje już ręcznego algorytmu** opartego o `g_parent/g_depth`
   - Zawsze wywołuje metodę runtime: `int lca = FindLowestCommonAncestor(srcLeaf, destLeaf);`
   - Dalsza logika (liczba EXIT/ENTER, `RecordHistoryForCurrentPath()`, wejście do kompozytu, logi) pozostaje bez zmian

4. **Budowanie ścieżki w OnInitialEntry** - **ZAKTUALIZOWANE 2025-08-27**:
   - Generator **nie buduje już ręcznie ścieżki** root→leaf (usunięto pętle leaf→root i odwracanie)
   - **Sync**: Używa `GetActivePath(Span<TState>)` z runtime do wypełnienia bufora
   - **Async**: Używa `GetActivePath(span)` z `ArrayPool<TState>` (zmiana z `int[]` na `TState[]`)
   - Eliminacja rzutowań - switch operuje bezpośrednio na `TState` zamiast `(TState)int`

5. **Inicjalizacja HSM**:
   - `DescendToInitialIfComposite()` - zstąpienie do liścia przed OnInitialEntry
   - Zapewnia prawidłowy stan początkowy dla stanów kompozytowych

### Payload i Multi-Payload
1. **Walidacja typu** (dla multi-payload):
   - Sprawdzanie typu na wejściu do `TryFireInternalAsync`
   - Mapa trigger → expected type w `_payloadMap`

2. **Przeciążenia API**:
   - Generowanie typowanych przeciążeń dla single-payload
   - Generyczne przeciążenia dla multi-payload

## Mapa odpowiedzialności - kto co robi

### Podział odpowiedzialności w HSM:

| Funkcjonalność | Odpowiedzialność | Szczegóły |
|----------------|------------------|------------|
| **LCA (Lowest Common Ancestor)** | Runtime (klasa bazowa) | Generator wywołuje `FindLowestCommonAncestor(srcLeaf, destLeaf)` |
| **GetActivePath (root→leaf)** | Runtime (klasa bazowa) | Generator wywołuje `GetActivePath(Span<TState>)` w OnInitialEntry |
| **Tablice HSM** | Generator | `g_parent`, `g_depth`, `g_initialChild`, `g_history` - statyczne tablice |
| **EXIT/ENTER chains** | Wygenerowany kod + runtime | Generator emituje pętle, runtime dostarcza helpery |
| **RecordHistory** | Runtime helper | Generator wywołuje `RecordHistoryForCurrentPath()` |
| **Composite entry** | Wygenerowany kod | Generator emituje logikę `GetCompositeEntryTarget()` |
| **GetPermittedTriggers (Flat)** | Generator (UnifiedStateMachineGenerator) | Tablice per stan |
| **GetPermittedTriggers (HSM)** | Generator (StateMachineCodeGenerator) | Tablice masek |

## Kluczowe zależności między metodami

### StateMachineGenerator → UnifiedStateMachineGenerator:
- **Tworzenie instancji:** linia 597 w `ProcessCandidateAndGenerate()` (potwierdzone)
- **Wywołanie Generate():** linia 599 (potwierdzone)
- **Przekazywany model:** `StateMachineModel` parsowany przez `StateMachineParser`

### UnifiedStateMachineGenerator → StateMachineCodeGenerator:
- **Dziedziczone pola:**
  - `Model` - model maszyny stanów
  - `Sb` - string builder
  - `TypeHelper` - helper typów
  - `IsAsyncMachine` - flaga async
  - `ShouldGenerateLogging` - flaga logowania

- **Wywołania metod bazowych:**
  - `WriteHeader()` - generowanie nagłówka z using'ami
  - `AddUsing()` - dodawanie dyrektyw using
  - `GetTypeNameForUsage()` - konwersja nazw typów
  - `WriteLogStatement()` - generowanie logowania
  - `WriteMethodAttribute()` - atrybuty metod
  - `WriteStartMethod()` - dla HSM (z warunkiem)
  - `WriteTransitionLogicForFlatNonPayload()` - rdzeń bez rozszerzeń (linia 1197)
  - Metody HSM: `WriteHierarchyArrays()`, `WriteHierarchyMethods()`
  - `GenerateHsmPermittedTriggerArrays()` - dla HSM permitted triggers

### Przepływ danych:
1. **Model wejściowy:** `StateMachineModel` zawiera:
   - Definicje stanów (`States`)
   - Definicje przejść (`Transitions`)
   - Konfigurację generacji (`GenerationConfig`)
   - Informacje o hierarchii (dla HSM): `ParentOf`, `InitialChildOf`, `HistoryOf`, `Depth`
   - Klasy kontenerowe (`ContainerClasses`)

2. **Konfiguracja funkcji:** Określana przez flagi w `GenerationConfig`:
   - `HasPayload` - obsługa payload w przejściach
   - `HasExtensions` - rozszerzenia maszyny stanów
   - `HasOnEntryExit` - callbacki OnEntry/OnExit
   - `IsAsync` - generowanie wersji asynchronicznej
   - `HierarchyEnabled` - wsparcie HSM

3. **Wyjście:** Wygenerowany kod C# jako string

## Podsumowanie

### Architektura:
- **Separacja odpowiedzialności:** 
  - `StateMachineGenerator` - integracja z Roslyn, discovery pipeline
  - `StateMachineCodeGenerator` - wspólna logika, szablony, infrastruktura HSM
  - `UnifiedStateMachineGenerator` - konkretna implementacja z wariantami i optymalizacjami

- **Wzorzec Template Method:**
  - Klasa bazowa definiuje szkielet algorytmu (`Generate()` jako virtual)
  - Klasa pochodna implementuje abstrakcyjne szczegóły (`WriteNamespaceAndClass()`)

- **Feature Flags:**
  - Zunifikowany generator obsługuje wszystkie warianty przez flagi
  - Konfiguracja przez `GenerationConfig`
  - Brak potrzeby wielu klas pochodnych dla różnych wariantów

- **Delegacja vs Override:**
  - Pochodna deleguje do bazy gdzie to możliwe (np. flat non-payload core)
  - Override tylko gdzie potrzebna specyficzna logika (np. z Extensions)

### Mocne strony:
1. Czytelna struktura dziedziczenia z jasnym podziałem odpowiedzialności
2. Dobrze zdefiniowane punkty rozszerzeń (virtual/abstract)
3. Wsparcie dla zaawansowanych funkcji (HSM, async, payload, extensions)
4. Optymalizacje dla prostych przypadków (fast paths)
5. Zero-alokacyjne ścieżki gdzie możliwe (stackalloc, prekomputowane tablice)
6. Reużywalność kodu przez delegację do bazy

### Potencjalne obszary ulepszeń:
1. Duża klasa `UnifiedStateMachineGenerator` (~3000 linii)
2. Złożona logika warunkowa oparta na flagach
3. Możliwość dekompozycji na mniejsze komponenty pomocnicze
4. Część fast-path mogłaby być wydzielona do osobnych emiterów
5. ~~Ujednolicić obliczanie LCA w HSM~~ **✅ ZROBIONE (2025-08-27)**

---

## Changelog

### 2025-08-27 - Ujednolicenie LCA i OnInitialEntry path building

#### Refaktoryzacja LCA:
- Generator nie emituje już ręcznych pętli do obliczania LCA
- Wszędzie używane jest `FindLowestCommonAncestor(...)` z runtime
- Redukcja: 13 linii ręcznego kodu → 2 linie wywołania metody (-85%)

#### Refaktoryzacja OnInitialEntry:
- Usunięto ręczne budowanie ścieżki (pętle leaf→root, odwracanie kolejności)
- Sync: Używa `GetActivePath(Span<TState>)` z runtime
- Async: Używa `ArrayPool<TState>` zamiast `ArrayPool<int>`
- Eliminacja rzutowań z `int` na `TState` w switch
- Brak zmian funkcjonalnych i wydajnościowych; uproszczenie kodu