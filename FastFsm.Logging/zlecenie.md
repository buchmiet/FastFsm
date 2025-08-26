# Referat: Przywrócenie generowania klas logujących w generatorze (bez LogAdapter)

## Cel
Przywrócić pierwotny mechanizm generowania logów w generatorze źródeł tak, aby:
- generator znów emitował per‑maszynowe klasy pomocnicze `{ClassName}Log` (AddSource),
- wygenerowany kod odwoływał się do metod `{ClassName}Log.*(...)`,
- nie było potrzeby dystrybuowania wspólnego pliku źródłowego (LogAdapter) przez pakiety.

Docelowo chcemy usunąć doraźną łatkę (LogAdapter) i wrócić do samowystarczalnego generatora.

---

## Jak było (model pierwotny)
- Wygenerowany kod (np. `TryFire`) wołał metody logujące na klasie `{ClassName}Log`, np.:
  - `PureStateMachineLog.TransitionSucceeded(_logger, _instanceId, ...);`
- Generator (w `Generator/Generator.cs`) miał gałąź wykonywaną, gdy `model.GenerateLogging == true`, która dodawała do kompilacji (AddSource) dodatkowy plik z definicją klasy `internal static class {ClassName}Log`.
- Kod tej klasy był wytwarzany przez `Generator.Logger/LoggingClassGenerator.cs` (metoda `Generate()`), zawierał zestaw wywołań `ILogger.Log(...)` z ustalonymi EventId i message template.
- Warunek włączenia logowania pochodził z MSBuild:
  - `FastFsm/build/FastFsm.Net.props` – właściwość `FsmGenerateLogging` eksportowana do kompilatora jako `CompilerVisibleProperty`.
  - Pakiet `FastFsm.Net.Logging` ustawiał tę właściwość na `true` i dodawał `FSM_LOGGING_ENABLED`.
- Efekt: nic poza referencją do pakietu `.Logging` nie było potrzebne, a wszystkie pliki logujące trafiały do kompilacji przez generator.

---

## Jak jest teraz (po łatce)
- Aby szybko usunąć błędy kompilacji (CS0103: `{ClassName}Log` nie istnieje), generator został przełączony na wspólny adapter:
  - Zamiast `{ClassName}Log.*(...)` generowany kod woła `global::FastFsm.Runtime.Logging.LogAdapter.*(...)`.
- Plik `LogAdapter` jest dostarczany jako `contentFiles` przez paczki:
  - `FastFsm.Net.Logging/shared/LogAdapter.cs`,
  - dodatkowo analogiczny plik w `FastFsm.Net.DependencyInjection`.
- Zalety łatki: stabilność i brak zależności od dodatkowych AddSource; wady: dodatkowy plik źródłowy w projekcie konsumenta.

---

## Co się zepsuło po refaktorze (przyczyna)
- Po refaktorze “Unified” w generatorze:
  - wywołania logowania zostały po staremu (do `{ClassName}Log`),
  - ale gałąź dodająca pomocnicze pliki `{ClassName}Log` (AddSource) nie dostarczała już tych plików do kompilacji (w spakowanej wersji nuget),
  - w efekcie kompilator nie znajdował klasy `{ClassName}Log` i zgłaszał CS0103.
- W repo wciąż istnieje gałąź dodająca te pliki oraz `LoggingClassGenerator`, więc najpewniej problem był w logice warunku/wywołania AddSource lub w rozjeździe wersji generatora w `FastFsm.Net.nupkg`.

---

## Zmiany do przywrócenia (plan naprawy)
1) Generator – przywrócić AddSource dla `{ClassName}Log` i spójność nazw:
   - Plik: `Generator/Generator.cs`
   - Sekcja: “Logging helpers (opcjonalnie)” – musi być wykonywana dla każdego kandydata z `model.GenerateLogging == true`.
   - Generowany hintName dla źródeł: obecnie `var loggingHintName = GetUniqueHintName($"{fqn}Log", ...)`. Zweryfikować, że `fqn` i nazwa klasy w `LoggingClassGenerator` (używa `model.ClassName`) są zgodne (namespace + nazwa typu).
   - Upewnić się, że `LoggingClassGenerator.Generate()` zwraca kod z poprawnym `namespace` (model.Namespace) i `internal static class {ClassName}Log`.

2) Generator – z powrotem emitować wywołania do `{ClassName}Log`:
   - Plik: `Generator.Logger/LoggingClassGenerator.cs`, metoda statyczna `WriteLogStatement(...)` powinna znów wpisywać: `{className}Log.{logMethodCall}`.
   - Usunąć/wycofać przełączkę na `LogAdapter` w `WriteLogStatement` oraz w miejscach, gdzie generator dodał `using` do `FastFsm.Runtime.Logging`.

3) Usunąć zależność od LogAdapter w pakietach overlay:
   - `FastFsm.Logging/FastFsm.Logging.csproj` – usunąć linie pakujące `shared/LogAdapter.cs` jako `contentFiles`.
   - `FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj` – analogicznie usunąć `shared/LogAdapter.cs` z contentFiles.
   - Pozostawić wszystkie pozostałe ustawienia MSBuild (`FsmGenerateLogging=true`, `FSM_LOGGING_ENABLED`).

4) Porządki w generatorze (opcjonalnie po przywróceniu funkcji):
   - Jeżeli pozostawimy tylko mechanizm `{ClassName}Log`, warto:
     - usunąć martwy kod związany z `LogAdapter` (jeśli dodane),
     - upewnić się, że `Generator.Logger.dll` jest potrzebny (zawiera helpery logujące i generuje klasy logujące),
     - ewentualnie zostawić tylko statyczne helpery, jeżeli są wykorzystywane gdzie indziej.

---

## Miejsca w kodzie (mapa)
- Emisja logów w generatorze:
  - `Generator/SourceGenerators/StateMachineCodeGenerator.cs` (bazowe `WriteLogStatement(...)` wywoływane w wielu miejscach)
  - `Generator/SourceGenerators/UnifiedStateMachineGenerator.cs` (dużo miejsc z `WriteLogStatement(...)`)
- Wstrzykiwanie logera:
  - `LoggingClassGenerator.WriteLoggerField(...)`, `GetLoggerConstructorParameter(...)`, `WriteLoggerAssignment(...)` – klasa: `Generator.Logger/LoggingClassGenerator.cs`.
- Emisja klasy `{ClassName}Log` (AddSource):
  - `Generator/Generator.cs` – sekcja “// 3) Logging helpers (opcjonalnie)”.
- Budowa klasy `{ClassName}Log`:
  - `Generator.Logger/LoggingClassGenerator.cs` – `Generate()` i metody `Write*Method()`.
- Flagi MSBuild (detekcja `GenerateLogging`):
  - `Generator/Helpers/BuildProperties.cs` → `GetGenerateLogging(...)` (czyta `build_property.FsmGenerateLogging`).
  - `FastFsm/build/FastFsm.Net.props` – eksport `FsmGenerateLogging` do kompilatora.
  - Overlay `FastFsm.Logging/build/FastFsm.Net.Logging.props` – ustawia `FsmGenerateLogging=true; FSM_LOGGING_ENABLED`.

---

## Testowanie – jak odtworzyć i zweryfikować naprawę
1) Przygotowanie lokalnego feedu:
   - W repo jest `nuget.config` z wpisem do `./nuget`. Paczki będą trafiać do `./nuget`.

2) Porządek budowania i bump wersji:
   - Zwiększ wersję `FastFsm.Net` – w `FastFsm/FastFsm.csproj` (target `StampVersionForNupkg` → `<Version>0.8.0.x</Version>`).
   - Zwiększ wersję `FastFsm.Net.Logging` – w `FastFsm.Logging/FastFsm.Logging.csproj` (oraz ustaw referencję `FastFsm.Net` na nową wersję).
   - Zwiększ wersję `FastFsm.Net.DependencyInjection` – w `FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj` (i ustaw referencję `FastFsm.Net`).
   - Zaktualizuj projekty testowe (FastFsm.Logging.Tests, FastFsm.DependencyInjection.Tests), by wskazywały nowe wersje paczek.

3) Budowanie i czyszczenie cache:
   - Usuń stare paczki z `./nuget` i z lokalnego cache NuGet:
     - `rm -f ./nuget/FastFsm.Net.*.nupkg ./nuget/FastFsm.Net.Logging.*.nupkg ./nuget/FastFsm.Net.DependencyInjection.*.nupkg`
     - `rm -rf ~/.nuget/packages/fastfsm.net/<stara_wersja> ~/.nuget/packages/fastfsm.net.logging/<stara_wersja> ~/.nuget/packages/fastfsm.net.dependencyinjection/<stara_wersja>`
   - Zbuduj paczki (w tej kolejności):
     - `dotnet build FastFsm/FastFsm.csproj -c Release`
     - `dotnet build FastFsm.Logging/FastFsm.Logging.csproj -c Release`
     - `dotnet build FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj -c Release`

4) Weryfikacja generatora – kompilacja testów:
   - `dotnet restore FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj`
   - `dotnet build FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj -c Release`
   - Otwórz `obj/GeneratedFiles/...` i sprawdź: oprócz plików maszyn powinien pojawić się dodatkowy plik `{Something}Log` (nazwa wg hintName, zwykle `global__<FQN>Log.Generated.cs` albo analogiczna).
   - Upewnij się, że wygenerowane maszyny odwołują się do `{ClassName}Log.*(...)`, a klasa taka istnieje w `obj/GeneratedFiles`.

5) Szybki smoke test (opcjonalnie):
   - `dotnet test FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj -c Release` – wystarczy, że przechodzi kompilacja; pełną zgodność logiki dopracujemy osobno.
   - Analogicznie dla DI: `dotnet build` i `dotnet test` aby potwierdzić brak regresji kompilacyjnej.

---

## Kryteria akceptacji (dla tej naprawy)
- [ ] Wygenerowany kod przy włączonym logowaniu woła `{ClassName}Log.*(...)` (nie `LogAdapter`).
- [ ] Generator dodaje pliki `{ClassName}Log` do kompilacji (AddSource) dla każdej maszyny z `GenerateLogging=true`.
- [ ] Testy `FastFsm.Logging.Tests` kompilują się bez błędów CS0103 dotyczących `{ClassName}Log`.
- [ ] Paczki nuget zbudowane i dostępne w `./nuget` (FastFsm.Net, FastFsm.Net.Logging, FastFsm.Net.DependencyInjection) zgodnie z podniesionymi wersjami.
- [ ] (Opcjonalnie) Usunięto `LogAdapter.cs` z `contentFiles` w `.Logging` i `.DependencyInjection`.

---

## Potencjalne pułapki i wskazówki
- Spójność nazw:
  - `LoggingClassGenerator` używa `model.ClassName` oraz `model.Namespace`. Wywołania w wygenerowanym kodzie używają `Model.ClassName` – musi się to zgrywać z zadeklarowaną klasą w AddSource (w tym `namespace`).
  - `hintName` (pod jaką nazwą AddSource dodaje plik) jest niezależny od nazwy typu – ale w razie kolizji (wiele maszyn) używamy `GetUniqueHintName`.
- Warunek `GenerateLogging`:
  - Pochodzi z `build_property.FsmGenerateLogging`. Upewnij się, że `FastFsm.Net.Logging` (i `.DependencyInjection`) ustawiają tę właściwość w `build` oraz `buildTransitive` (props), a `FastFsm.Net` eksportuje ją jako `CompilerVisibleProperty`.
- Zmiany w generatorze a cache nuget:
  - Po każdej zmianie generatora (zawartego w paczce `FastFsm.Net`) trzeba podbić wersję i wyczyścić cache `~/.nuget`, inaczej projekt testowy może mieć stare analyzery.
- Dublowanie `ExtensionRunner.cs`:
  - `.Logging` i `.DependencyInjection` mogą pakować `ExtensionRunner.cs` (contentFiles), co powoduje warning CS2002. Nie jest to krytyczne, ale można przenieść plik tylko do jednego pakietu, aby wyciszyć ostrzeżenie.

---

## Zmiany do wykonania (checklista techniczna)
1) W `Generator.Logger/LoggingClassGenerator.cs` przywrócić `WriteLogStatement` do formy:
   ```csharp
   sb.AppendLine($"{className}Log.{logMethodCall}");
   ```
   (zamiast wywołań `LogAdapter`)

2) W `Generator/Generator.cs` upewnić się, że blok:
   ```csharp
   if (model.GenerateLogging)
   {
       var loggingGenerator = new Generator.Log.LoggingClassGenerator(model.ClassName, model.Namespace);
       var loggingSource = loggingGenerator.Generate();
       var loggingHintName = GetUniqueHintName($"{fqn}Log", usedHintNames);
       context.AddSource(loggingHintName, SourceText.From(loggingSource, Encoding.UTF8));
   }
   ```
   jest wykonywany dla każdego kandydata z `GenerateLogging==true` (bez dodatkowych ukrytych warunków)
   i że `fqn` odpowiada typowi, dla którego powstał kod (namespace/nesting zgodne).

3) Usunąć/wycofać dodawanie `shared/LogAdapter.cs` z pakietów:
   - `FastFsm.Logging/FastFsm.Logging.csproj`
   - `FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj`

4) Podbić wersje paczek i zbudować pipeline (jak w sekcji „Testowanie”).

5) Zweryfikować w `obj/GeneratedFiles`, że klasy `{ClassName}Log` są generowane i używane w wygenerowanym kodzie.

---

## Sugestie dalszych prac (po akceptacji naprawy)
- Rozważyć mikro‑refaktor `Generator.Logger`:
  - Wyodrębnić tylko potrzebne helpery do wspólnego projektu (zmniejszyć rozmiar analyzera).
- Dodać test snapshot/kompilacyjny w `Generator.Tests`, który sprawdza, że dla maszyny z loggingiem generator emituje:
  - odwołanie do `{ClassName}Log.*(...)` oraz
  - plik klasy `{ClassName}Log`.

---

## Kontakt i wsparcie
W razie wątpliwości: sprawdź różnice w commitach, w szczególności pliki:
- `Generator/Generator.cs`
- `Generator.Logger/LoggingClassGenerator.cs`
- `Generator/SourceGenerators/StateMachineCodeGenerator.cs`
- `Generator/SourceGenerators/UnifiedStateMachineGenerator.cs`
- `FastFsm/build/FastFsm.Net.props`
- `FastFsm.Logging/build/FastFsm.Net.Logging.props`

Powodzenia przy przywracaniu poprzedniego modelu generowania!
