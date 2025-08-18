# Raport: Błąd kompilacji w HSM – GetPermittedTriggers(Span<…>) i guardy z payloadem

## Podsumowanie
- Po włączeniu generatora (paczka FastFsm.Net zawiera analyzer) testy `StateMachine.Tests` nie kompilują się.
- Błąd dotyczy wygenerowanej metody `GetPermittedTriggers(Span<TTrigger>)` dla HSM: w gałęzi z guardami generator wywołuje metody guard bez przekazania payloadu, co skutkuje błędem kompilacji, gdy guard wymaga parametru (np. `bool CanSubmit(PayloadData p)`).

## Komunikat kompilatora (oryginał)
```
C:\\Users\\Lukasz.Buchmiet\\source\\repos\\FastFsm\\StateMachine.Tests\\obj\\GeneratedFiles\\Generator\\Generator.StateMachineGenerator\\global__StateMachine.Tests.Features.Hsm.CompileTime.HsmAdditionalCompilationTests.HsmPayloadMachine.Generated.cs(445,37): error CS7036: There is no argument given that corresponds to the required parameter 'p' of 'HsmAdditionalCompilationTests.HsmPayloadMachine.CanSubmit(HsmAdditionalCompilationTests.PayloadData)'
```

## Lokalizacja błędnego wygenerowanego kodu
- Plik: `StateMachine.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__StateMachine.Tests.Features.Hsm.CompileTime.HsmAdditionalCompilationTests.HsmPayloadMachine.Generated.cs`
- Linia: 445 (wywołanie guarda bez argumentu)

Fragment (z numeracją linii):
```csharp
431     // Track seen triggers to avoid duplicates (2 unique triggers)
432     Span<bool> seen = stackalloc bool[2];
...
444     if (!seen[1]) {
445         if (CanSubmit())
446         {
447             if (writeIndex >= destination.Length) return -1;
448             destination[writeIndex++] = HP_Trigger.Submit;
449             seen[1] = true;
450         }
451     }
```
Kontekst: to jest implementacja HSM-owej metody zero‑alokacyjnej:
```csharp
public int GetPermittedTriggers(Span<HP_Trigger> destination)
```

## Skąd pochodzi ten kod w generatorze
- Plik źródłowy generatora: `Generator/SourceGenerators/StateMachineCodeGenerator.cs`
- Sekcja generująca „Span-based version for GetPermittedTriggers (only for HSM)”.
- Dokładne linie (z numeracją):
  - 1633–1710: Nagłówek i pętla po stanach oraz grupowanie po triggerach
  - 1690–1696: Krytyczna gałąź generująca wywołanie guarda bez payloadu

Wyciąg ze źródła (z numeracją):
```csharp
1633     // Add Span-based version for GetPermittedTriggers (only for HSM)
1634     if (Model.HierarchyEnabled)
1635     {
...
1690             // All transitions are guarded - need runtime check
1691             bool first = true;
1692             foreach (var transition in transitionsForTrigger)
1693             {
1694                 if (!string.IsNullOrEmpty(transition.GuardMethod))
1695                 {
1696                     Sb.AppendLine($"{(first ? "if" : "else if")} ({transition.GuardMethod}())");
```
Jak widać, generator emituje `if (GuardMethod())` bez żadnego argumentu, co jest niepoprawne, jeśli guard ma sygnaturę wymagającą payloadu.

## Dlaczego drugi wariant działa
- Wersja listowa z resolverem payloadu jest generowana osobno i poprawnie:
  - Metoda: `GetPermittedTriggers(Func<TTrigger, object?> payloadResolver)`
  - Wygenerowany kod używa resolvera, sprawdza typ, a następnie wywołuje guarda z przekazaniem `typedGuardPayload`.
- Plik wygenerowany: ten sam co wyżej, linie ~482–489:
```csharp
482     var payload_Submit = payloadResolver(HP_Trigger.Submit);
...
485     if (payload_Submit is PayloadData typedGuardPayload) {
486         canFire = CanSubmit(typedGuardPayload);
```

## Dodatkowe obserwacje (powiązane, ale nie-blokujące kompilacji)
- W `UnifiedStateMachineGenerator2.cs` wywołania do helpera guarda często przekazują `payloadVar: "null"`. To nie sypie kompilacją (helper emituje fallback), ale logicznie spowoduje, że guardy wymagające payloadu będą zwracały `false`, jeśli payload nie będzie dalej przekazany. Do rozważenia w kolejnym kroku.
  - Plik: `Generator/SourceGenerators/UnifiedStateMachineGenerator2.cs`
  - Przykłady:
    - `WriteTransitionLogic(...)`: linie ~445–472 (dla sync) – `WriteGuardCall(transition, "guardResult", "null", ...)`
    - `WriteAsyncCanFireMethod(...)` oraz `WriteCanFireMethodSyncCore(...)` – analogicznie przekazują `"null"`.

## Przyczyna główna
- Implementacja `GetPermittedTriggers(Span<TTrigger>)` dla HSM nie obsługuje guardów wymagających payloadu i generuje wywołania bezargumentowe `GuardMethod()`, co kończy się błędem kompilacji przy podpisach `Guard(TPayload payload)` lub `Guard(TPayload payload, CancellationToken ct)`.

## Propozycje kierunku naprawy (wysokopoziomowo)
1) Krótkoterminowo (bez łamania API):
   - W wersji spanowej NIE dodawać triggerów, które mają wyłącznie przejścia strzeżone guardami wymagającymi payloadu (tj. gdy brak przejścia bez guarda). Pozostać przy dodawaniu wyłącznie unguarded lub guardów bezparametrowych/token‑only.
   - Ewentualnie, jeśli są guardy bez wymagania payloadu, generować warunki tylko dla nich.

2) Docelowo (pełna funkcjonalność zero‑alokacyjna):
   - Rozszerzyć API o wariant spanowy z resolverem payloadu, np.:
     ```csharp
     public int GetPermittedTriggers(Span<TTrigger> destination, Func<TTrigger, object?> payloadResolver)
     ```
     i w nim użyć tej samej ścieżki co w już istniejącej wersji listowej.

3) Alternatywnie (bardziej ryzykowne):
   - W `GetPermittedTriggers(Span<...>)` spróbować użyć `GuardGenerationHelper.EmitGuardCheck(...)` z `payloadVar` – ale bez dodatkowego parametru/resolvera payload nie mamy skąd go wziąć. Ta opcja nie jest obecnie wykonalna bez zmiany sygnatury metody.

## Zakres wpływu
- Wszystkie maszyny HSM, w których istnieją triggery dostępne wyłącznie przez guardy wymagające payloadu, będą generowały niekompilowalny kod w `GetPermittedTriggers(Span<...>)`.
- Test reprodukujący: `StateMachine.Tests/Features/Hsm/CompileTime/HsmAdditionalCompilationTests.cs` – klasa wewnętrzna `HsmPayloadMachine` (guard `CanSubmit(PayloadData)`), a błąd w wygenerowanym pliku wskazany powyżej.

## Rekomendowane następne kroki
- Zaimplementować jedną z propozycji naprawy (preferencja: dodać wariant spanowy z resolverem payloadu lub pominąć takie triggery w wersji bezresolverowej).
- Dodatkowo przejrzeć miejsca w `UnifiedStateMachineGenerator2.cs`, gdzie do `WriteGuardCall` przekazywane jest `payloadVar: "null"` – i w ścieżkach runtime (TryFire/CanFire) przekazać realny `payload` tak, aby gardy payloadowe były oceniane poprawnie.

---

W razie potrzeby mogę dodać tymczasowy warning/diagnostykę w parserze (FSM0xx) wykrywającą sytuację: „HSM + Span GetPermittedTriggers + guard wymagający payloadu” – żeby użytkownik otrzymywał zrozumiały komunikat zanim dojdzie do błędu kompilacji.
