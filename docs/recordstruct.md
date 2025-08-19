Fast FSM obsługuje dziś “klasyczne” enumy (Int32 pod spodem). Tymczasem w nowoczesnych mikro-serwisach .NET coraz częściej spotyka się \*\*“string-enumy”\*\* – stałe symboliczne oparte na `record struct` lub klasach “Smart Enum”. Poniżej wyjaśniam, skąd ta moda, jakie są typowe implementacje i co to oznacza dla roadmapy FastFSM 0.8.



\## 1 Dlaczego string-enum?



\### 1.1 Problemy z tradycyjnym enum \\:int



\* \*\*JSON i Swagger\*\* – w Minimal API enum-y serializują się domyślnie do liczb; trzeba ręcznie dodawać `JsonStringEnumConverter`, a Swagger i tak lubi pokazywać wartości 0/1/2 zamiast tekstu (\[Stack Overflow]\[1]).

\* \*\*Wersjonowanie kontraktów\*\* – gdy klient dostanie nieznaną wartość liczbową, nie da się jej zignorować; string można bezpiecznie przepuścić dalej (“tolerant reader”).

\* \*\*Otwarte zbiory\*\* – w event-driven DDD definitywny enum bywa za sztywny; obiekty typu \*value\* (np. `Currency`, `OrderStatus`) łatwiej rozszerzyć bez migracji BD (\[Microsoft Learn]\[2]).



\### 1.2 Nowe narzędzia w .NET 8



\* `record struct` (C# 10 / . NET 8) daje “value object” o zerowym koszcie kopiowania – idealny nośnik stałych stringowych (\[Microsoft Learn]\[3]).

\* Wzorzec “smart enum” (np. biblioteka \*\*Ardalis SmartEnum\*\*) dodaje metody i walidację przy zachowaniu semantyki stałej (\[GitHub]\[4]).

\* Record-based enum potrafi zastąpić klasyczną enumerację bez utraty pattern-matching (via `static readonly` pola + `implicit operator`) (\[Stack Overflow]\[5]).



\## 2 Typowe implementacje “string-enum”



| Technika                                   | Przykład kodu                                                                                              | Cechy                                                                               |

| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |

| \*\*record struct\*\*                          | `public readonly record struct Status(string Value) { public static readonly Status New = \\"new\\"; … }`    | immut., pattern-matching, ==, copy-free                                             |

| \*\*SmartEnum base-class\*\*                   | `public class Status : SmartEnum<Status> { public static readonly Status New = new(\\"New\\", \\"new\\"); … }` | bogatsze API (parse, list), możliwość dziedziczenia (\[GitHub]\[4])                   |

| \*\*const string + wrapper\*\*                 | `public const string New = \\"new\\";`                                                                       | najprostsze, brak type-safety                                                       |

| \*\*classic enum + JsonStringEnumConverter\*\* | enum → “NEW” w JSON                                                                                        | wymaga atrybutów i nadal ogranicza się do zamkniętego zbioru (\[Microsoft Learn]\[6]) |



\## 3 Wyzwania dla FastFSM



\### 3.1 Wydajność



\* Porównanie `string` vs `int` = koszt `OrdinalIgnoreCase` i hash-code.

\* Cel “< 1 ns” utrzymasz, jeżeli \*\*wewnątrz\*\* maszyny reprezentujesz stan numerem, a tylko \*\*interfejs publiczny\*\* przyjmuje/zwra­ca string.

\* Źródło numerów: generator mógłby nadawać kolejne `byte`/`ushort` ID podczas parsowania atrybutów.



\### 3.2 Zero-alloc konwersja



\* Zbuduj w generatorze statyczną tablicę `ReadOnlyMemory<char>\[] Names` → `id`.

\* Parse kierunku `string → id` można przyspieszyć generując `switch` po długości + `MemoryMarshal.GetReference` (brak słownika).

\* Jeśli użytkownik poda \*\*record struct\*\*, wygeneruj `static implicit operator string` i skorzystaj z już istniejącego “Name” pola.



\### 3.3 Bit-masky a liczba stanów



\* Po mapowaniu do `byte`/`ushort` zachowujesz dotychczasową logikę mask ≤ 64 / > 64 bez zmian – tylko warstwa tłumaczenia pracuje na stringach.



\### 3.4 Serializacja JSON



\* Dodaj opcjonalny generator `System.Text.Json` converter:

&nbsp; `public class FastFsmStateConverter<TState> : JsonConverter<TState>` – użyje map utworzonych w compile-time.

\* Performance będzie porównywalne z wbudowanym `JsonStringEnumConverter` (\[Reddit]\[7]), ale unikniesz refleksji.



\## 4 Propozycja “StringEnum support” (0.8)



\### 4.1 API użytkownika



```csharp

public readonly record struct OrderStatus(string Value)

{

&nbsp;   public static readonly OrderStatus New = \\"new\\";

&nbsp;   public static readonly OrderStatus Paid = \\"paid\\";

}



// enum-type parameter jest teraz generykiem:

\[StateMachine(typeof(OrderStatus), typeof(OrderTrigger))]

public partial class OrderFsm { … }

```



\* Dla czystego stringa można dodać alias:

&nbsp; `\[StateMachine(typeof(string), …, StringEnum = true)]`



\### 4.2 Generator – flow



1\. \*\*Collect constants\*\* – wykryj pola `static readonly`/`const string` lub `SmartEnum` instancje.

2\. \*\*Assign numeric ID\*\*   → dictionary `ImmutableDictionary<string,byte>` w fase build.

3\. \*\*Emit map + TryParse\*\* – kod bez alokacji, porównuje `ReadOnlySpan<char>` z stałymi (`\\"new\\"u8`).

4\. \*\*Expose helper\*\* `bool IsInState(OrderStatus s)`.



\### 4.3 Edge-cases



\* \*\*Nieznany string\*\* – domyślnie `ArgumentOutOfRangeException`; opcjonalny `UnknownStateStrategy` = \*ignore / mapTo(Error)\*.

\* \*\*Case sensitivity\*\* – flaga `StringComparison`.

\* \*\*Enum > 256 pozycji\*\* – automatyczne przejście na `ushort` ID (masz to już rozwiązane w sekcji >64 bit-mask).



\## 5 Korzyści dla mikro-serwisów



\* \*\*Kontrakty REST\*\* – JSON i OpenAPI pokazują przyjazne wartości tekstowe, bez custom converterów (\[GitHub]\[8]).

\* \*\*Forward compatibility\*\* – dziwny status trafia do pola `string`, logika może go zignorować lub zapisać do loga.

\* \*\*CQRS/Outbox\*\* – event zapisuje “Paid” zamiast `2`; zmiana kolejności nie łamie historycznych zdarzeń (\[ardalis.com]\[9]).



---



\### Podsumowanie



“String-enum” to praktyczny kompromis między ścisłą typizacją a elastycznością kontraktów JSON. Dzięki source-generatorowi możesz zachować \*\*obecną wydajność FastFSM\*\*, mapując napisy na wewnętrzne ID podczas kompilacji. Jeśli zaplanujesz to na wersję 0.8 (po stabilizacji HSM), zyskasz:



\* Łatwiejsze integracje z Minimal API i Swagger.

\* Bezbolesne wersjonowanie zdarzeń i DTO.

\* Nowy argument marketingowy: “pierwsza FSM .NET z natywnym wsparciem record-string-enum”.



\[1]: https://stackoverflow.com/questions/76643787/how-to-make-enum-serialization-default-to-string-in-minimal-api-endpoints-and-sw?utm\_source=chatgpt.com "How to make enum serialization default to string in minimal ..."

\[2]: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types?utm\_source=chatgpt.com "Using Enumeration classes instead of enum types - .NET"

\[3]: https://learn.microsoft.com/en-us/dotnet/csharp/tutorials/records?utm\_source=chatgpt.com "Use record types tutorial - C#"

\[4]: https://github.com/ardalis/SmartEnum?utm\_source=chatgpt.com "ardalis/SmartEnum: A base class for quickly and easily ..."

\[5]: https://stackoverflow.com/questions/63724308/using-c-sharp-9-0-records-to-build-smart-enum-like-discriminated-union-like-sum?utm\_source=chatgpt.com "Using C# 9.0 records to build smart-enum-like ..."

\[6]: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to?utm\_source=chatgpt.com "How to write custom converters for JSON serialization - .NET"

\[7]: https://www.reddit.com/r/dotnet/comments/14xgfjl/should\_you\_use\_newtonsoftjson\_or\_systemtextjson/?utm\_source=chatgpt.com "Should you use Newtonsoft.Json or System.Text. ..."

\[8]: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2293?utm\_source=chatgpt.com "Enum string converter is not respected using .NET 6 ..."

\[9]: https://ardalis.com/enum-alternatives-in-c/?utm\_source=chatgpt.com "Enum Alternatives in C# | Blog"



