Tak — podejmę się. Poniżej masz \*\*systemowy\*\*, wieloetapowy plan naprawy generatora (bez „łatek”), z jasnymi kryteriami akceptacji, testami weryfikującymi i gotowymi promptami do odpalenia na Twoim agencie Claude Code. Plan uwzględnia realne ograniczenia architektury HSM, w tym krytyczny problem współdzielenia statyków w klasie bazowej między \*\*różne maszyny\*\* mające \*\*te same\*\* typy enumów (co dyskwalifikuje pomysł „statyczny konstruktor przypisujący pola bazy” — konflikt danych!). Obecnie generator emituje kolidujące pola/metody (np. `s\_parent`, `s\_depth`, `s\_initialChild`, `s\_history`, `\_lastActiveChild`, `GetCompositeEntryTarget`, `DescendToInitialIfComposite`) i korzysta z nich bezpośrednio, co generuje CS0108.   \&#x20;



\# Założenia architektoniczne (docelowe)



1\. \*\*Zero współdzielonych statyków w bazie\*\* dla danych układu HSM. Każda maszyna ma \*\*swoje\*\* tablice statyczne (w klasie generowanej).

2\. \*\*Baza\*\* udostępnia \*\*abstrakcyjny interfejs odczytu układu\*\* (np. właściwości `ParentArray`, `DepthArray`, `InitialChildArray`, `HistoryArray`) oraz implementuje \*\*runtime’owe algorytmy\*\* (np. `GetCompositeEntryTarget`, `DescendToInitialIfComposite`, `IsInHierarchy`, debugowe ścieżki). Dzięki temu:



&nbsp;  \* generator \*\*nie duplikuje logiki\*\* runtime, tylko dostarcza dane,

&nbsp;  \* nie ma ukrywania członków (CS0108 znika), bo \*\*baza nie deklaruje już tych statyków\*\*, a klasa generowana nie kopiuje metod runtime z bazy.

3\. Generator emituje \*\*prywatne, unikalnie nazwane\*\* tablice (np. `g\_parent`, `g\_depth`, `g\_initialChild`, `g\_history`) \*\*oraz nadpisuje\*\* abstrakcyjne właściwości bazy, zwracając te tablice.

4\. \*\*\\\_lastActiveChild\*\* pozostaje polem \*\*instancyjnym\*\* w bazie (alokowanym/zerowanym przez bazę), a generator tylko dostarcza długość/kształt (pośrednio przez `InitialChildArray.Length`).

5\. Wszelki kod generowany, który dziś czyta `s\_parent/s\_depth/...` (np. debug helpers), przechodzi na API bazy \*\*albo\*\* przenosimy go do bazy.



To usuwa CS0108 u źródła i upraszcza generator (dane ≠ logika).



---



\# Roadmap (5 faz) z bramkami „Go/No-Go”



\## Faza 0 — Baseline (diagnoza i snapshot)



\*\*Cel:\*\* zebrać punkt odniesienia (ostrzeżenia/diagnozy, generowane źródła), by później precyzyjnie potwierdzać poprawę.



\*\*Kryteria akceptacji:\*\*



\* Mamy pełną listę CS0108 z plikami/wierszami.

\* Mamy zapisany artefakt generowanego kodu dla 2–3 maszyn HSM (np. `ShallowHistoryMachine`) do porównań.



\*\*PROMPT 0 (Claude):\*\*



```

Wykonaj:

1\) dotnet build -v:m

2\) Wypisz wszystkie ostrzeżenia CS0108 z plikami i liniami.

3\) Wygeneruj źródła i zapisz do artefaktów: 

&nbsp;  - pełny output dla ShallowHistoryMachine (plik \*.Generated.cs)

&nbsp;  - dowolnej drugiej maszyny HSM, jeśli jest.

4\) Zrób grep w generowanych plikach na: " s\_parent", " s\_depth", " s\_initialChild", " s\_history", " \_lastActiveChild", "GetCompositeEntryTarget(", "DescendToInitialIfComposite(" i policz wystąpienia.

```



(Przykładowy „stan obecny” dla `ShallowHistoryMachine` widać w dostarczonym pliku — generator emituje kolidujące pola i metody. )



---



\## Faza 1 — API „Layout Provider” w bazie



\*\*Cel:\*\* wprowadzić w \*\*bazie\*\* kontrakt odczytu układu HSM i przenieść do bazy \*\*logikę\*\* HSM, tak by generator nie musiał jej dublować.



\*\*Zmiany (baza):\*\*



\* Dodać \*\*abstrakcyjne właściwości\*\*:



&nbsp; ```csharp

&nbsp; protected abstract int\[] ParentArray { get; }

&nbsp; protected abstract int\[] DepthArray { get; }

&nbsp; protected abstract int\[] InitialChildArray { get; }

&nbsp; protected abstract HistoryMode\[] HistoryArray { get; }

&nbsp; ```



\* Dodać w bazie implementacje:



&nbsp; \* `protected int GetCompositeEntryTarget(int compositeIndex)` – skopiuj treść z generatora. (Obecnie generator emituje tę metodę prywatną; logika jest w `WriteHierarchyRuntimeFieldsAndHelpers` i korzysta z `s\_parent`, `s\_initialChild`, `s\_history`, `\_lastActiveChild`. Zastąp je odwołaniami do właściwości: `ParentArray`, `InitialChildArray`, `HistoryArray` oraz pola `\_lastActiveChild`.) \&#x20;

&nbsp; \* `protected void DescendToInitialIfComposite()` — jw. (z kodu generowanego).\&#x20;

&nbsp; \* `public bool IsInHierarchy(TState ancestor)` i \*\*DEBUG\*\* `DumpActivePath()` — przenieś treść z generatora i zamień bezpośrednie użycia `s\_parent` na `ParentArray`. \&#x20;



\* Zapewnij alokację `\_lastActiveChild` w bazie (np. w `Start()/StartAsync` lub w konstruktorze bazy po uzyskaniu długości `InitialChildArray.Length`), tak jak dziś robi to wygenerowany konstruktor. (W pliku generowanym `\_lastActiveChild = new int\[s\_initialChild.Length];` i zerowanie do `-1` — przenieś to do bazy).\&#x20;



\*\*Kryteria akceptacji:\*\*



\* Baza kompiluje się sama (jeszcze bez zmian w generatorze).

\* W bazie nie ma już \*\*statycznych\*\* `s\_parent/s\_depth/s\_initialChild/s\_history` — tylko \*\*abstrakcyjne właściwości\*\* i jedna kopia metod runtime.



\*\*PROMPT 1 (Claude):\*\*



```

W pliku Runtime/StateMachineBase\*.cs:

1\) Dodaj abstrakcyjne właściwości:

&nbsp;  protected abstract int\[] ParentArray { get; }

&nbsp;  protected abstract int\[] DepthArray { get; }

&nbsp;  protected abstract int\[] InitialChildArray { get; }

&nbsp;  protected abstract HistoryMode\[] HistoryArray { get; }



2\) PRZENIEŚ do bazy treści metod z generatora:

&nbsp;  - GetCompositeEntryTarget(int)

&nbsp;  - DescendToInitialIfComposite()

&nbsp;  - IsInHierarchy(TState ancestor)

&nbsp;  - #if DEBUG: DumpActivePath()

&nbsp;  Zamień odwołania do s\_parent/s\_depth/... na właściwości ParentArray/DepthArray/...



3\) Zapewnij alokację i wyzerowanie \_lastActiveChild w bazie:

&nbsp;  - Alokuj: \_lastActiveChild = new int\[InitialChildArray.Length];

&nbsp;  - Ustaw -1 w pętli.

&nbsp;  Zrób to w Start()/StartAsync (przed pierwszym użyciem) albo w konstruktorze bazowym, jeśli masz pewność, że InitialChildArray jest gotowe.



4\) Usuń z bazy ewentualne statyczne s\_parent/s\_depth/s\_initialChild/s\_history (jeśli istnieją).

5\) dotnet build

Pokaż diff i wynik kompilacji.

```



---



\## Faza 2 — Generator: dane zamiast logiki



\*\*Cel:\*\* generator \*\*przestaje\*\* emitować metody runtime i bezpośrednio używać `s\_parent` itd.; emituje \*\*tylko dane\*\* i \*\*override\*\* właściwości bazy.



\*\*Zmiany (generator):\*\*



\* W `StateMachineCodeGenerator.WriteHierarchyArrays(...)`:



&nbsp; \* \*\*Zmień nazwy\*\* emitowanych tablic na \*\*unikalne\*\* (np. `g\_parent`, `g\_depth`, `g\_initialChild`, `g\_history`) i zostaw je jako `private static readonly`.

&nbsp; \* \*\*Dodaj\*\* override właściwości:



&nbsp;   ```csharp

&nbsp;   protected override int\[] ParentArray => g\_parent;

&nbsp;   protected override int\[] DepthArray => g\_depth;

&nbsp;   protected override int\[] InitialChildArray => g\_initialChild;

&nbsp;   protected override HistoryMode\[] HistoryArray => g\_history;

&nbsp;   ```



&nbsp; (Obecnie generator wypisuje: `private static readonly int\[] s\_parent = ...` itd. — trzeba to przekształcić. Zob. aktualny kod emisji tablic. )



\* W `WriteHierarchyRuntimeFieldsAndHelpers(...)`:



&nbsp; \* \*\*Usuń emisję\*\*:



&nbsp;   \* pola instancyjnego `\_lastActiveChild` (baza nim zarządza),

&nbsp;   \* metod `RecordHistoryForCurrentPath`, `GetCompositeEntryTarget`, `DescendToInitialIfComposite`.

&nbsp; \* Jeżeli jakaś logika nadal jest potrzebna po stronie generatora (np. `RecordHistoryForCurrentPath`), rozważ przenosiny do bazy. (Kod aktualnie generowany tu: patrz fragmenty z `\_lastActiveChild`, `GetCompositeEntryTarget`, `DescendToInitialIfComposite` — to trzeba usunąć z emisji).  \&#x20;



\* \*\*Wszędzie\*\*, gdzie generator dziś odwołuje się do `s\_parent/s\_depth/s\_initialChild/s\_history` (np. DEBUG helpers, planowanie wejść/wyjść), przełącz użycie na API bazy (metody w bazie lub właściwości). Przykładowo, generowane `WriteStateChangeWithCompositeHandling(...)` już woła `GetCompositeEntryTarget` — po przeniesieniu metody do bazy nic nie zmieniamy tu.\&#x20;



\*\*Kryteria akceptacji:\*\*



\* W żadnym generowanym pliku nie występuje fraza ` s\_parent`/` s\_depth`/` s\_initialChild`/` s\_history` \*\*poza\*\* definicjami `g\_\*` i override’ami właściwości.

\* Generator \*\*nie emituje\*\* `GetCompositeEntryTarget`/`DescendToInitialIfComposite`/`\_lastActiveChild`.

\* Build przechodzi, testy HSM przechodzą (np. Shallow/Deep History). Fragmenty dot. ShallowHistory masz w pliku i będą dobrym papierkiem lakmusowym.\&#x20;



\*\*PROMPT 2 (Claude):\*\*



```

Zmień generator:

1\) W StateMachineCodeGenerator.WriteHierarchyArrays(...):

&nbsp;  - Zamiast s\_parent/s\_depth/s\_initialChild/s\_history emituj PRIVATE STATIC READONLY

&nbsp;    o nazwach: g\_parent, g\_depth, g\_initialChild, g\_history.

&nbsp;  - Dodaj override właściwości z bazy:

&nbsp;      protected override int\[] ParentArray => g\_parent; // itd.



2\) W WriteHierarchyRuntimeFieldsAndHelpers(...):

&nbsp;  - Usuń emisję pola \_lastActiveChild i metod:

&nbsp;      RecordHistoryForCurrentPath(), GetCompositeEntryTarget(int), DescendToInitialIfComposite().

&nbsp;  (Te metody są teraz w bazie.)



3\) Przeskanuj wszystkie miejsca emisji, które referują s\_parent/s\_depth/... 

&nbsp;  - Zamień na odpowiednie wywołania metod bazy albo odczyt właściwości ParentArray/...



4\) dotnet build

5\) Uruchom wszystkie testy Generator.Tests i Runtime tests (HSM Shallow/Deep).

6\) Grep po generowanych plikach: " s\_parent", " s\_depth", " s\_initialChild", " s\_history", 

&nbsp;  "GetCompositeEntryTarget(", "DescendToInitialIfComposite(" 

&nbsp;  - spodziewane: brak poza bazą i nazwami g\_\* oraz wywołaniem GetCompositeEntryTarget z bazy.

7\) Wypisz listę diagnoz: oczekujemy 0 x CS0108.

```



---



\## Faza 3 — Testy regresji + „problem współdzielenia enumów”



\*\*Cel:\*\* potwierdzić brak interferencji między \*\*dwoma maszynami\*\* używającymi \*\*tych samych enumów\*\* (wcześniej to byłby killer dla „statycznych pól w bazie”).



\*\*Nowe testy (Generator.Tests):\*\*



\* `HSM\_TwoMachines\_SameEnums\_NoInterference`:



&nbsp; \* Zdefiniuj `enum S { ... }`, `enum T { ... }` \*\*jednorazowo\*\* w namespace.

&nbsp; \* Wygeneruj \*\*dwie różne\*\* maszyny `M1` i `M2` z \*\*innymi\*\* układami parent/initial/history.

&nbsp; \* Uruchom proste sekwencje (np. start → kilka triggerów) na obu instancjach i asercje, że stany się nie mieszają.

\* `HSM\_No\_CS0108\_Warnings`:



&nbsp; \* Kompilacja prostej maszyny HSM i asercja, że `diags.Where(d => d.Id=="CS0108")` jest puste.



\*\*PROMPT 3 (Claude):\*\*



```

Dodaj do Generator.Tests:

1\) Klasę HsmValidationDiagnosticTests (lub dopisz w istniejącej):

&nbsp;  \[Fact]

&nbsp;  public void HSM\_No\_CS0108\_Warnings() { ... } 

&nbsp;  (użyj CompileAndRunGenerator i asercji braku CS0108 — jak w przykładzie od użytkownika)



2\) Klasę HsmInterferenceTests:

&nbsp;  \[Fact]

&nbsp;  public void HSM\_TwoMachines\_SameEnums\_NoInterference() { 

&nbsp;      // Jedne enumy S,T; dwie maszyny M1 i M2 z innym układem; 

&nbsp;      // start + kilka triggerów; asercje końcowego stanu obu maszyn

&nbsp;  }



3\) Uruchom testy i pokaż wyniki.

```



> W przykładach runtime i w planowaniu HSM występują ścieżki `GetCompositeEntryTarget`/`RecordHistoryForCurrentPath`/LCA/exit/enter — te mechanizmy pozostają, ale ich \*\*jedyna implementacja\*\* jest teraz w bazie, a generator tylko wywołuje (por. dotychczasową emisję wołania `GetCompositeEntryTarget` po zmianie stanu). \&#x20;



---



\## Faza 4 — Sprzątanie i twarde gwarancje



\*\*Cel:\*\* domknięcie migracji, eliminacja „starych ścieżek”, testy źródłowe, linters.



\*\*Działania:\*\*



\* Usuń z generatora nieużywane helpery dot. HSM (jeżeli jakieś zostały).

\* Dodaj \*\*test źródłowy\*\*: asercja, że \*\*wygenerowany\*\* plik nie zawiera stringów ` s\_parent`, `GetCompositeEntryTarget(` itp. (poza bazą).

\* Dodaj prosty \*\*analyzer\*\* (lub test snapshotów), który wykryje, gdyby generator znów zaczął emitować sporne identyfikatory.

\* Upewnij się, że `HierarchicalTransitionPlanner` nic nie zakłada nt. lokalnych `s\_\*` (on planuje kroki, runtime robi resztę — tak jest teraz, bo rozwiązywanie kompozytów jest po stronie runtime’u).\&#x20;



\*\*PROMPT 4 (Claude):\*\*



```

1\) Grep po repo: gdziekolwiek w generatorze emitujemy "s\_parent"/"s\_depth"/"s\_initialChild"/"s\_history" 

&nbsp;  - pokaż wyniki. Powinno zostać wyłącznie generowanie g\_\* i override'y właściwości.

2\) Dodaj test/snapshot wymuszający brak tych tokenów w gotowym pliku.

3\) dotnet test

4\) Przygotuj krótkie podsumowanie diffów (generator + baza).

```



---



\# Uwaga na alternatywę „statyczny konstruktor przypisujący bazę”



To kuszące, ale \*\*błędne\*\* architektonicznie: gdy dwie maszyny mają \*\*te same enumy\*\*, nadpisują sobie pola bazy (bo statyki są per-zamknięty typ generyczny bazy, nie per konkretna maszyna). Dlatego \*\*nie\*\* idziemy tą drogą. (To też było pierwotnym źródłem „czemu w ogóle mamy lokalne tablice” — żeby rozdzielić dane na maszynę.)



---



\# Co zmienia się w generatorze (esencja)



\* `WriteHierarchyArrays(...)` — z `s\_parent` → `g\_parent` \*\*+ override\*\* właściwości bazy.\&#x20;

\* `WriteHierarchyRuntimeFieldsAndHelpers(...)` — \*\*usuń\*\* emisję `\_lastActiveChild` i metod runtime; to wszystko przenosimy do bazy.\&#x20;

\* Miejsca w emisji, które korzystały z metod runtime (np. `WriteStateChangeWithCompositeHandling`) zostają bez zmian, bo wywołują metody bazy.\&#x20;



---



\# Co sprawdzamy po każdej fazie



\* \*\*Build\*\* i \*\*pełny zestaw testów\*\* (zwłaszcza HSM: Shallow/Deep History). Patrz przykładowy wygenerowany test z historią płytką — jest dobrym detektorem.\&#x20;

\* \*\*Diagnozy\*\*: 0 × CS0108.

\* \*\*Grep\*\*: brak emisji starych symboli w generatach.

\* \*\*Test interferencji\*\*: dwie maszyny – te same enumy – różne układy – niezależne wyniki.



---





Jeśli zaakceptujesz ten plan, jedziemy wg promptów Faza 0 → 4 i po każdym kroku wklejasz tu odpowiedź Claude’a (build/logi/diffy). Dzięki temu każdy krok będzie \*\*zweryfikowany\*\* zanim przejdziemy dalej.



