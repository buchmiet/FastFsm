FastFSM-owe testy przechodzą, ale poruszyłeś dwie słuszne bolączki – “start-time `OnEntry`” oraz pomyłki z `CancellationToken` traktowanym jak payload. Poniżej zebrałem, jak radzą sobie z tym konkurencyjne biblioteki, jakie ryzyka zostają po stronie FastFSM i jak można to naprawić bez hacków w konstruktorze.

## 1 ️⃣  Fire-and-forget `OnEntry` – problem systemowy

### 1.1 Dlaczego konstruktor + `Task.Run()` jest kłopotliwy

* Wyścigi w testach (widziałeś wpis *“OnEntry\:Begin”* zanim zacząłeś asercje).
* Brak możliwości propagacji `CancellationToken` przy starcie.
* Potencjalny wyjątek w `OnEntry` ląduje na nieobserwowanym `Task`-u.

### 1.2 Jak robi to konkurencja

| Biblioteka                       | Jak uruchamia inicjalne akcje                                                                                                                                                                | Źródło                        |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| **Stateless**                    | Maszyna nie „wie”, kiedy jest *started*; autorzy zalecają najpierw stworzyć instancję, a potem wywołać `Activate()` (lub strzelić wirtualnym triggerem) – wtedy wykonywane są entry-akcje 🔗 | ([GitHub][1])                 |
|                                  | Async-entry dostępne przez `OnEntryAsync`, ale wywoływane dopiero przy **pierwszym** `FireAsync` 🔗                                                                                          | ([nblumhardt.com][2])         |
| **Appccelerate**                 | Wywołujesz `stateMachine.Initialize(State.Initial)` – dopiero wtedy entry-akcje są wykonywane; dokumentacja ostrzega, że przy init nie ma event-argumentu 🔗                                 | ([appccelerate.github.io][3]) |
| **Automatonymous (MassTransit)** | Instancja jest „pusta”, a pierwsze entry dzieje się dopiero, gdy nadleci komunikat i uruchomi się `Initially`/`During` 🔗                                                                    | ([masstransit.io][4])         |
| **LiquidState / inne**           | Również wymagają jawnego `StartAsync()` lub pierwszego triggera; brak konstruktorowej magii 🔗                                                                                               | ([Stack Overflow][5])         |

### 1.3 Propozycja dla FastFSM

1. **Nowy interfejs `IAsyncStateMachineStarter`** z metodą:

   ```csharp
   ValueTask StartAsync(CancellationToken ct = default);
   ```

   Generator emituje tę metodę **tylko**, gdy maszyna ma jakieś `OnEntry` w stanie startowym.
2. Konstruktor *nie* odpala entry. Użytkownik **musi** wywołać `StartAsync()` (lub `FireAsync(...)`, które wewnętrznie zrobi lazy-start).
3. Dla wygody możesz dodać statyczną pomocniczą `StateMachineFactory.CreateStarted(...)`, która utworzy instancję i od razu zawoła `StartAsync`.

Zalety: brak wyścigów, pełna obsługa `CancellationToken`, wyraźny punkt awarii (wyjątek trafia do `await`ującego kodu).

---

## 2 ️⃣  `CancellationToken` jako drugi parametr → mylący payload

### 2.1 Skąd wziął się błąd

Sygnatura FastFSM to

```csharp
TryFireAsync(trigger, object? payload = null, CancellationToken ct = default)
```

Jeśli ktoś wywoła `TryFireAsync(trigger, ct)`, kompilator przypisze token do **`payload`**, a `ct` pozostanie `None`. W async-kodzie łatwo tego nie zauważyć.

W Stateless trwa dyskusja o dodaniu wersji z tokenem – tam planują osobne przeciążenie, żeby uniknąć dwuznaczności 🔗 ([GitHub][6]).

### 2.2 Diagnoza do generatora

* **Analizator Roslyn** – jeśli generowana maszyna ma *payload-less* API (drugi parametr `object?`) i typ użytkownika przekazany przy wywołaniu to `CancellationToken`, wypisz `CSFASTFSM0001`:

  > “Argument typu `CancellationToken` został przekazany jako `payload`. Prawdopodobnie zapomniałeś użyć nazwanego parametru `cancellationToken:`.”

* Przy okazji: jeśli użytkownik *typuje* payload (przez `[PayloadType]`), to i tak token nie przejdzie castu – podwójna ochrona.

### 2.3 Dodatkowe ułatwienia API

* Dodaj **przeciążenie** bez `payload`:

  ```csharp
  ValueTask<bool> TryFireAsync(TTrigger trigger,
                               CancellationToken ct = default);
  ```

  – tak robią biblioteki, które wprowadziły token na późniejszym etapie (por. Stateless issue #527) 🔗 ([GitHub][6]).
* Z czasem można oznaczyć obecne trzyparametrowe API jako `[Obsolete("Use named argument or overload X")]`.

---

## 3 ️⃣  Podsumowanie

* Konstruktorowe **fire-and-forget** powoduje wyścigi i ucina obsługę anulowania. Konkurencja wymaga jawnego `Start()`/`Activate()` – FastFSM powinien pójść tą samą drogą, dodając `StartAsync()` i leniwe uruchamianie przy pierwszym triggerze.
* Dwuznaczność `payload` vs `CancellationToken` jest źródłem cichych błędów; najbezpieczniej wprowadzić przeciążenie bez payloadu **i** dodać analizator ostrzegający, gdy token trafia na złe miejsce.

Zmiany są niewielkie w kodzie generatora, a potencjalnie oszczędzą użytkownikom wielu godzin debugowania.

[1]: https://github.com/dotnet-state-machine/stateless?utm_source=chatgpt.com "dotnet-state-machine/stateless: A simple library for creating ..."
[2]: https://nblumhardt.com/2016/11/stateless-30/?utm_source=chatgpt.com "Stateless 3.0"
[3]: https://appccelerate.github.io/statemachineactions.html "Appccelerate - StateMachine"
[4]: https://masstransit.io/documentation/patterns/saga/state-machine?utm_source=chatgpt.com "State Machine"
[5]: https://stackoverflow.com/questions/5923767/simple-state-machine-example-in-c?utm_source=chatgpt.com "Simple state machine example in C#? - ..."
[6]: https://github.com/dotnet-state-machine/stateless/issues/527?utm_source=chatgpt.com "dotnet-state-machine/stateless - Introduce FiringMode.Serial"
3 Diagnostyka „CancellationToken jako payload”
Analizator Roslyn
Reguła FFSM0001 – jeśli do parametru payload trafia obiekt typu CancellationToken, wypisz Warning:

vbnet
Copy
Edit
FFSM0001: A CancellationToken was passed as the 'payload' argument.
Did you mean to use the 'cancellationToken:' named parameter?
Implementacja: sprawdzasz InvocationOperation, patrzysz na pozycję
argumentów i typ.