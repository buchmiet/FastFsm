\# FastFSM Fluent Refinement (0.8.0 – Final)



\## 1) Decyzja i zakres



\*\*Decyzja:\*\* od \*\*0.8.0\*\* przyjmujemy \*\*instancyjne\*\* `Configure()` jako \*\*domyślny i preferowany\*\* model dla Fluent DSL.

\*\*Cel 0.8.0 pozostaje bez zmian:\*\* \*\*eliminacja `nameof(...)`\*\* na rzecz \*\*method groups\*\*; parser Roslyn wydobywa symbole metod z method groups i przekłada je na wygenerowany kod – runtime bez zmian, \*\*zero-alloc\*\* pozostaje gwarantowane. 



\*\*Istotne doprecyzowania po dyskusji:\*\*



\* \*\*Rezygnujemy\*\* z `\[Conditional("FASTFSM\_FLUENT")]` na przeciążeniach DSL — nie wycinamy wywołań DSL z IL, żeby nie mylić debugowania i testów (kwestia raised w recenzji). 

\* \*\*`nameof(...)` zostaje tylko jako „awaryjny rozstrzygacz"\*\* kolizji (ambiguous method group). \*\*Nie promujemy\*\* w dokumentacji; wskazujemy wyłącznie jako \*\*last resort\*\*. (Obecne docs i tak miejscami go pokazują – to zmienimy przy edycji przykładów.) 

\* \*\*Static `Configure()`\*\* będzie \*\*dozwolone\*\* jako \*\*ścieżka alternatywna\*\* (niepromowana). W docs: „Advanced / Alternative". (To odpowiada na konserwatywne oczekiwania części użytkowników bez psucia głównego UX).



\*\*Zakres releasu 0.8.0:\*\*



\* Nowy kształt DSL (delegaty + method groups) – bez lambd i bez `nameof` w przykładach. 

\* Parser: rozpoznanie method groups (`IdentifierName`/`MemberAccess`) i ekstrakcja `IMethodSymbol`. 

\* Analyzer: \*\*twarde reguły „czystości DSL"\*\* (patrz §4) i \*\*nowe kody diagnostyczne\*\* (FSM3070–FSM3083).

\* Dokumentacja: pełna podmiana `nameof(...)` → method groups; sekcja „What's legal in Configure()" + FAQ z pułapkami. 

\* Testy: parser/analyzer/HSM/OnException end-to-end. 



---



\## 2) Dlaczego instancyjne `Configure()` (po debacie)



\* \*\*Realizacja celu 0.8.0:\*\* przejście na \*\*method groups\*\* do \*\*metod instancyjnych\*\* jest naturalne i ergonomiczne. Statyczne `Configure()` wymusza powrót do `nameof`/adapterów/lambd (a lambdy w Twoim DSL są \*\*niedozwolone\*\* – „Zero lambdas"). 

\* \*\*DX bliski Stateless\*\* (świadomie): `.Guard(CanX) / .Action(DoX) / .OnEntry(Init)` — niski próg wejścia i refactor-safe (rename w Roslyn). 

\* \*\*Runtime bez zmian:\*\* DSL nadal jest \*\*compile-time only\*\*, a wygenerowany kod zachowuje \*\*0 B alloc\*\* na ścieżce wykonania. 

\* \*\*Ryzyko „mylenia z runtime"\*\* eliminujemy \*\*nie przez „static", ale przez wczesne, jednoznaczne błędy kompilacji\*\* (analyzer) i krótką ramkę w docs: „`Configure()` is analyzed at \*\*compile-time\*\* only — not executed at runtime." (Dodamy wprost w `FluentAPI.md`). 



---



\## 3) API (delegaty + minimalne przeciążenia)



> \*\*No lambdas. No expressions. Method groups only.\*\*



\*\*Delegates (no change vs plan):\*\* (comments in English, zgodnie z preferencją)



```csharp

// Guards

public delegate bool Guard();

public delegate ValueTask<bool> GuardAsync(CancellationToken ct);

public delegate bool Guard<T>(in T payload);

public delegate ValueTask<bool> GuardAsync<T>(in T payload, CancellationToken ct);



// Actions / Entry / Exit shapes:

public delegate void Act();

public delegate ValueTask ActAsync(CancellationToken ct);

public delegate void Act<T>(in T payload);

public delegate ValueTask ActAsync<T>(in T payload, CancellationToken ct);



// Type aliases for clarity (optional)

public delegate void Entry();

public delegate ValueTask EntryAsync(CancellationToken ct);

public delegate void Entry<T>(in T payload);

public delegate ValueTask EntryAsync<T>(in T payload, CancellationToken ct);



public delegate void Exit();

public delegate ValueTask ExitAsync(CancellationToken ct);

public delegate void Exit<T>(in T payload);

public delegate ValueTask ExitAsync<T>(in T payload, CancellationToken ct);

```



\*\*Minimal DSL overloads\*\* (bez `\[Conditional]`):



```csharp

public interface IStateBuilder

{

&nbsp;   IStateBuilder OnEntry(Entry cb);

&nbsp;   IStateBuilder OnEntry(EntryAsync cb);

&nbsp;   IStateBuilder OnEntry<T>(Entry<T> cb);

&nbsp;   IStateBuilder OnEntry<T>(EntryAsync<T> cb);



&nbsp;   IStateBuilder OnExit(Exit cb);

&nbsp;   IStateBuilder OnExit(ExitAsync cb);

&nbsp;   IStateBuilder OnExit<T>(Exit<T> cb);

&nbsp;   IStateBuilder OnExit<T>(ExitAsync<T> cb);

}



public interface ITransitionBuilder

{

&nbsp;   ITransitionBuilder Guard(Guard cb);

&nbsp;   ITransitionBuilder Guard(GuardAsync cb);

&nbsp;   ITransitionBuilder Guard<T>(Guard<T> cb);

&nbsp;   ITransitionBuilder Guard<T>(GuardAsync<T> cb);



&nbsp;   ITransitionBuilder Action(Act cb);

&nbsp;   ITransitionBuilder Action(ActAsync cb);

&nbsp;   ITransitionBuilder Action<T>(Act<T> cb);

&nbsp;   ITransitionBuilder Action<T>(ActAsync<T> cb);

}

```



> \*\*Uwaga:\*\* docelowy zestaw musi pokrywać \*\*wszystkie\*\* kształty wspierane dziś przez `CallbackSignatureAnalyzer` — ta lista zostaje jako „źródło prawdy". (Weryfikacja w testach parsera/analyzera). 



---



\## 4) „Purity rules" — proste, syntaktyczne, egzekwowalne



\*\*Akceptujemy w argumentach DSL wyłącznie:\*\*



\* \*\*method groups\*\* do metod \*\*tej samej klasy\*\* (`IdentifierName` lub `this.Member`)

\* \*\*literały/enum/typeof\*\* tam, gdzie DSL przewiduje wartości (np. `.State(State.Idle)` / `.On(Trigger.Start)` / `.Priority(100)`)



\*\*Absolutnie zabronione\*\* w DSL (błąd kompilacji):



\* \*\*lambdy i wyrażenia\*\*: `() => ...`, `x => ...`, wywołania metod, operatory, interpolacje, warunki (`?:`)

\* \*\*odczyty pól/właściwości\*\*: `this.\_flag`, `this.Property`, `Config.MaxRetries`

\* \*\*method groups do właściwości/indexerów\*\*: `.Guard(CanProcess)` gdzie `CanProcess` jest property

\* \*\*zewnętrzne method groups\*\*: `.Guard(OtherClass.Method)`, `.Guard(service.Method)`



\## 5) Complete Diagnostic Reference (FSM3070-FSM3089)



\### DSL Purity Diagnostics (FSM3070-FSM3077)



\*\*FSM3070 — Ambiguous method group reference\*\*

```csharp

// Problem:

bool CanStart() => true;

bool CanStart(in PayloadData data) => data.IsValid;

.Guard(CanStart)  // ERROR: Which overload?



// Fix:

.Guard(nameof(CanStart))  // Explicitly select parameterless version

// OR rename methods to be unambiguous

```



\*\*FSM3071 — Impure DSL: expression not allowed\*\*

```csharp

// All of these produce FSM3071:

.Guard(this.\_isReady ? CanProceed : CannotProceed)  // Conditional

.Action(() => DoWork())  // Lambda

.GoTo(GetNextState())  // Method invocation

.Priority(DefaultPriority)  // Property/field access



// Fix: Use only method groups and literals

.Guard(CanProceed)

.Action(DoWork)

.GoTo(State.Active)

.Priority(100)

```



\*\*FSM3072 — Property or indexer used where method expected\*\*

```csharp

// Problem:

public bool IsReady { get; set; }

.Guard(IsReady)  // ERROR: IsReady is a property



// Fix:

private bool CheckReady() => IsReady;

.Guard(CheckReady)

```



\*\*FSM3073 — External method group not allowed\*\*

```csharp

// Problem:

.Guard(ValidationService.IsValid)  // External class

.Action(this.logger.Log)  // External instance



// Fix: Create wrapper methods

private bool ValidateLocally() => ValidationService.IsValid();

private void LogLocally() => this.logger.Log();

.Guard(ValidateLocally)

.Action(LogLocally)

```



\*\*FSM3074 — Signature mismatch for DSL position\*\*

```csharp

// Problem:

ValueTask<bool> CheckAsync(CancellationToken ct);

.Guard(CheckAsync)  // Used in sync-only context



// Fix: Match the expected signature

bool CheckSync() => CheckAsync(CancellationToken.None).Result;

.Guard(CheckSync)

```



\*\*FSM3075 — Lambda expression not allowed\*\*

```csharp

// Problem:

.Guard(() => \_retryCount < 3)

.Action(x => ProcessData(x))



// Fix: Extract to named methods

private bool CanRetry() => \_retryCount < 3;

private void ProcessDataWrapper(Data x) => ProcessData(x);

.Guard(CanRetry)

.Action(ProcessDataWrapper)

```



\*\*FSM3076 — Field or property access in DSL\*\*

```csharp

// Problem:

private int MaxRetries = 3;

.Priority(MaxRetries)  // Reading field



// Fix: Use literal

.Priority(3)

```



\*\*FSM3077 — Method invocation in DSL\*\*

```csharp

// Problem:

.GoTo(CalculateNextState())

.Priority(GetPriority())



// Fix: Use compile-time constants

.GoTo(State.Processing)

.Priority(100)

```



\### Configure() Method Diagnostics (FSM3080-FSM3084)



\*\*FSM3080 — Multiple Configure methods detected\*\*

```csharp

// Problem:

private void Configure() => FSM.State(State.A);

private void Configure2() => FSM.State(State.B);  // ERROR



// Fix: Single Configure method

private void Configure() => FSM

&nbsp;   .State(State.A)

&nbsp;   .State(State.B);

```



\*\*FSM3081 — Invalid Configure method signature\*\*



Sub-diagnostics:

\- \*\*FSM3081a\*\*: Configure must be private

\- \*\*FSM3081b\*\*: Configure must be parameterless

\- \*\*FSM3081c\*\*: Configure cannot be virtual/override

\- \*\*FSM3081d\*\*: Configure must be instance method



```csharp

// Problems:

public void Configure() => ...        // FSM3081a: Must be private

private void Configure(int x) => ...  // FSM3081b: No parameters

private virtual void Configure() => ...// FSM3081c: No virtual

private static void Configure() => ... // FSM3081d: Must be instance



// Correct:

private void Configure() => FSM.State(State.Initial);

```



\*\*FSM3082 — Configure inherited from base class\*\*

```csharp

// Problem:

public class BaseMachine 

{

&nbsp;   protected void Configure() => FSM.State(State.A);

}



\[StateMachine(typeof(State), typeof(Trigger))]

public partial class DerivedMachine : BaseMachine  // ERROR



// Fix: Define Configure in the partial class

\[StateMachine(typeof(State), typeof(Trigger))]

public partial class Machine

{

&nbsp;   private void Configure() => FSM.State(State.A);

}

```



\*\*FSM3083 — Partial method not supported for Configure\*\*

```csharp

// Problem:

partial void Configure();

partial void Configure() => FSM.State(State.A);



// Fix: Regular method

private void Configure() => FSM.State(State.A);

```



\*\*FSM3084 — Configure contains runtime-only constructs\*\*

```csharp

// Problem:

private async void Configure() 

{

&nbsp;   await LoadConfigAsync();  // ERROR: No async in Configure

&nbsp;   try { FSM.State(State.A); } catch { }  // ERROR: No try/catch

}



// Fix: Configure is compile-time only

private void Configure() => FSM.State(State.A);



> Dzięki temu \*\*nie\*\* robimy „analizy przepływu danych" — wystarcza \*\*kontrola syntaktyczna i symboliczna\*\*. To adresuje recenzencki zarzut o „niekończącą się grę w kotka i myszkę" — \*\*nie gramy\*\*: wszystko poza method group jest błędem już na etapie kompilacji.



---



\## 6) Parser (Roslyn) — Implementation Details



\### Parser Changes in `FluentParser.cs`



```csharp

private string? ParseMethodGroup(ExpressionSyntax expr, string dslPosition)

{

&nbsp;   switch (expr)

&nbsp;   {

&nbsp;       // Direct method reference: Guard(CanProcess)

&nbsp;       case IdentifierNameSyntax identifier:

&nbsp;           var symbol = \_semanticModel.GetSymbolInfo(identifier).Symbol;

&nbsp;           return ValidateMethodSymbol(symbol, identifier, dslPosition);

&nbsp;       

&nbsp;       // this.Method reference: Guard(this.CanProcess)

&nbsp;       case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } memberAccess:

&nbsp;           var memberSymbol = \_semanticModel.GetSymbolInfo(memberAccess).Symbol;

&nbsp;           return ValidateMethodSymbol(memberSymbol, memberAccess, dslPosition);

&nbsp;       

&nbsp;       // Legacy nameof support (not promoted)

&nbsp;       case InvocationExpressionSyntax invocation 

&nbsp;           when invocation.Expression.ToString() == "nameof":

&nbsp;           return ParseNameof(invocation);

&nbsp;       

&nbsp;       // Everything else is impure

&nbsp;       default:

&nbsp;           ReportImpureDsl(expr, dslPosition);

&nbsp;           return null;

&nbsp;   }

}



private string? ValidateMethodSymbol(ISymbol? symbol, SyntaxNode location, string dslPosition)

{

&nbsp;   if (symbol is null)

&nbsp;   {

&nbsp;       // Symbol not found - compilation error elsewhere

&nbsp;       return null;

&nbsp;   }

&nbsp;   

&nbsp;   if (symbol is IPropertySymbol)

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3072, location, symbol.Name));

&nbsp;       return null;

&nbsp;   }

&nbsp;   

&nbsp;   if (symbol is not IMethodSymbol method)

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3071, location));

&nbsp;       return null;

&nbsp;   }

&nbsp;   

&nbsp;   // Check if method belongs to the state machine class

&nbsp;   if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, \_stateMachineType))

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3073, location, method.Name));

&nbsp;       return null;

&nbsp;   }

&nbsp;   

&nbsp;   // Check for ambiguous overloads

&nbsp;   var candidates = \_semanticModel.GetMemberGroup(location);

&nbsp;   if (candidates.Length > 1)

&nbsp;   {

&nbsp;       var validOverloads = candidates

&nbsp;           .OfType<IMethodSymbol>()

&nbsp;           .Where(m => IsValidSignatureForPosition(m, dslPosition))

&nbsp;           .ToList();

&nbsp;           

&nbsp;       if (validOverloads.Count > 1)

&nbsp;       {

&nbsp;           \_diagnostics.Add(CreateDiagnostic(FSM3070, location, method.Name));

&nbsp;           return null;

&nbsp;       }

&nbsp;   }

&nbsp;   

&nbsp;   // Validate signature matches DSL position expectations

&nbsp;   if (!IsValidSignatureForPosition(method, dslPosition))

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3074, location, method.Name, dslPosition));

&nbsp;       return null;

&nbsp;   }

&nbsp;   

&nbsp;   return method.Name;

}

```



\### Configure() Method Validation



```csharp

private void ValidateConfigureMethod(IMethodSymbol configureMethod)

{

&nbsp;   // Check visibility

&nbsp;   if (configureMethod.DeclaredAccessibility != Accessibility.Private)

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3081a, configureMethod.Locations.First()));

&nbsp;   }

&nbsp;   

&nbsp;   // Check parameters

&nbsp;   if (configureMethod.Parameters.Length > 0)

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3081b, configureMethod.Locations.First()));

&nbsp;   }

&nbsp;   

&nbsp;   // Check virtual/override

&nbsp;   if (configureMethod.IsVirtual || configureMethod.IsOverride)

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3081c, configureMethod.Locations.First()));

&nbsp;   }

&nbsp;   

&nbsp;   // Check static (for v0.8.0 must be instance)

&nbsp;   if (configureMethod.IsStatic)

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3081d, configureMethod.Locations.First()));

&nbsp;   }

&nbsp;   

&nbsp;   // Check not inherited

&nbsp;   if (!configureMethod.ContainingType.Equals(\_stateMachineType))

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3082, configureMethod.Locations.First()));

&nbsp;   }

&nbsp;   

&nbsp;   // Check not partial

&nbsp;   if (configureMethod.IsPartialDefinition)

&nbsp;   {

&nbsp;       \_diagnostics.Add(CreateDiagnostic(FSM3083, configureMethod.Locations.First()));

&nbsp;   }

} 



---



\## 7) Ambiguity: strategia bez frustracji



\* Zalecenia stylu:

&nbsp; \* \*\*Nie przeciążaj\*\* callbacków minimalnymi różnicami; nazwij jawnie: `DoX()`, `DoXAsync(CancellationToken ct)`, `DoXWithData(in T data)`, `DoXWithDataAsync(in T data, CancellationToken ct)`.

&nbsp; \* \*\*Unikaj parametrów opcjonalnych\*\* i defaultów w callbackach.

\* Gdy i tak trafisz na \*\*FSM3070\*\*:

&nbsp; 1. Zmień nazwy metod na jednoznaczne \*\*albo\*\* 2) \*\*awaryjnie\*\* użyj `nameof(...)` dla tej jednej pozycji DSL. (W docs: „Troubleshooting: Ambiguous method group".) 



\## 8) Implementation Priority \& Timeline



\### Phase 1: Core Parser Changes (2-3 days)

\*\*Critical path - blocks everything else\*\*

\- Modify `ParseGuard`, `ParseAction`, `ParseOnEntry`, `ParseOnExit` to accept method groups

\- Implement `ParseMethodGroup` with strict syntax validation

\- Add support for `IdentifierNameSyntax` and `MemberAccessExpressionSyntax`



\### Phase 2: Diagnostic Implementation (3-4 days)

\*\*Essential for user experience\*\*

\- FSM3071 (Impure DSL) - Most common error, needs clear messages

\- FSM3072 (Property instead of method) - Common C# confusion

\- FSM3070 (Ambiguous overloads) - Needs helpful resolution hints

\- FSM3081a-d (Configure validation) - Enforce conventions



\### Phase 3: Edge Case Handling (2-3 days)

\- Nested classes with Configure()

\- Generic state machines

\- Partial classes split across files

\- Inheritance scenarios



\### Phase 4: Documentation \& Migration (2 days)

\- Update all examples from `nameof` to method groups

\- Create migration guide with before/after examples

\- Add troubleshooting section for each diagnostic



\## 9) Common Implementation Pitfalls



\### Pitfall 1: Incomplete Expression Blocking

\*\*Problem:\*\* Users will try creative workarounds:

```csharp

const bool flag = true;

.Guard(flag ? Method1 : Method2)  // Still an expression!

```

\*\*Solution:\*\* Block ALL non-identifier expressions at syntax level



\### Pitfall 2: Property Detection Complexity

\*\*Problem:\*\* Properties can be expression-bodied:

```csharp

bool CanProcess => \_isReady;  // Looks like a method

```

\*\*Solution:\*\* Use Roslyn's `IPropertySymbol` - it handles all property forms



\### Pitfall 3: Overload Resolution Ambiguity

\*\*Problem:\*\* Context-dependent resolution:

```csharp

.Guard(Process)  // Which Process() overload?

```

\*\*Solution:\*\* Check DSL context (payload type) to narrow candidates, report FSM3070 if still ambiguous



\### Pitfall 4: Error Message Quality

\*\*Bad:\*\* "Invalid DSL usage"

\*\*Good:\*\* "Cannot use field '\_retryCount' in Configure(). Create a method that returns this value: `bool CanRetry() => \_retryCount < 3`"



\### Pitfall 5: Static Configure() Migration Path

\*\*Issue:\*\* Some users may have valid reasons for static Configure

\*\*Solution:\*\* Support both, but document instance as primary. Static = advanced scenario 



---



\## 7) „Model mentalny: to nie runtime" — jak to komunikujemy



\* W \*\*FluentAPI.md\*\* dodajemy widoczną ramkę:



> \*\*Compile-time only\*\*

> `Configure()` is \*\*analyzed at compile-time\*\* by the source generator. It is \*\*not executed\*\* at runtime.

> DSL accepts only \*\*method groups\*\* (no lambdas/expressions). Any attempt to read instance state or compute values inside DSL will produce a \*\*compile error\*\*.



\* Przykłady \*\*korygujące odruchy „jak w Stateless"\*\*:

&nbsp; \* Zamiast `Guard(flag ? A : B)` → dwa `.On(...).Guard(A).Priority(…)` i osobna reguła z `.Guard(B)` (wykorzystaj \*\*`.Priority(...)`\*\*, już opisane w docs). 

&nbsp; \* Zamiast `Action(() => Do(\_retries))` → \*\*callback\*\* `void DoWithRetries()` czerpie dane z pól \*\*w trakcie wykonania akcji\*\* (to jest legalne — callback wykonuje się w runtime i może korzystać ze stanu obiektu).



---



\## 8) OnException — bez zmian semantyki



\* Zasady sygnatur (`ExceptionDirective` / `ValueTask<ExceptionDirective>`, z opcjonalnym `CancellationToken`) pozostają bez zmian; method group zamiast `nameof`. 

\* Diagnostyki `FSM208/209` zaktualizujemy w copy (z „nameof" → „callback reference"). 



---



\## 9) Migracja (0.7.5 → 0.8.0)



\*\*Było (przykładowo):\*\*



```csharp

.OnEntry(nameof(OnX))

.Action(nameof(DoX))

.Guard(nameof(CanX))

```



\*\*Jest (0.8.0):\*\*



```csharp

.OnEntry(OnX)

.Action(DoX)

.Guard(CanX)

```



\*\*Jeżeli trafisz na FSM3070 (ambiguous):\*\*



\* preferuj \*\*zmianę nazw\*\* metod tak, by były jednoznaczne;

\* \*\*awaryjnie\*\* użyj `nameof(DesiredOverload)` dla konkretnej pozycji.



---



\## 10) Testy i CI



\* \*\*Parser tests\*\*: method groups (`IdentifierName`/`MemberAccess`), payload/no-payload, CT, async, ambiguous overloads → `FSM3070`.

\* \*\*Analyzer tests\*\*: wszystkie błędy `FSM3071–FSM3083`; snapshoty diagnostyki z caretami.

\* \*\*HSM regressions\*\*: priorytety, internal transitions, Initial/History (shallow/deep). 

\* \*\*OnException\*\*: poprawne/błędne sygnatury (sync/async/ct). 

\* \*\*CI\*\*: Windows/Linux/macOS, Debug/Release; \*\*brak\*\* `FASTFSM\_FLUENT` (wyłączony).



---



\## 11) FAQ / Common pitfalls (z dyskusji)



\*\*Q:\*\* „Napisałem `.Guard(CanProcess)` i dostaję błąd FSM3072."

\*\*A:\*\* `CanProcess` jest property; DSL wymaga \*\*metody\*\* (method group). Zmień na `bool CanProcess()`.



\*\*Q:\*\* „Chcę `.Action(() => Do(\_n))` – czemu niedozwolone?"

\*\*A:\*\* Lambdy/wyrażenia są zabronione w DSL (compile-time). Zdefiniuj \*\*metodę\*\* `void DoWithN()` i czytaj stan w trakcie \*\*akcji\*\* (callback wykonuje się w runtime).



\*\*Q:\*\* „Dlaczego `.Guard(flag ? A : B)` jest błędem?"

\*\*A:\*\* DSL nie wykonuje kodu – użyj \*\*dwóch\*\* przejść z `.Priority(...)` albo napisz pojedynczą metodę guard, która sama wewnątrz sprawdzi stan (to już runtime).



\*\*Q:\*\* „Czy mogę korzystać z DI w `Configure()`?"

\*\*A:\*\* Nie. `Configure()` jest \*\*compile-time\*\*. DI używaj w \*\*callbackach runtime\*\* (np. akcje/entry/exit). 



\*\*Q:\*\* „Czy instancyjne `Configure()` wpływa na wydajność?"

\*\*A:\*\* Nie – DSL nie jest wykonywany w runtime; wygenerowany kod pozostaje 0-alloc. 



\*\*Q:\*\* „Czy mogę mieć kilka `Configure()` w partialach/klasach bazowych?"

\*\*A:\*\* Nie. Dokładnie \*\*jedna\*\* metoda `Configure` w danym typie; musi być `private`, `non-virtual`, bez parametrów.



---



\## 12) Plan wdrożenia



1\. \*\*API draft\*\* (delegaty + overloady DSL; bez `\[Conditional]`). 

2\. \*\*Parser\*\*: method groups → `IMethodSymbol`; ambiguous → `FSM3070`. 

3\. \*\*Analyzer\*\*: FSM3071–FSM3083 (purity + kontrakty `Configure`).

4\. \*\*Docs\*\*: podmiana `nameof` → method groups; „What's legal in Configure()"; troubleshooting `FSM3070`. 

5\. \*\*Tests\*\*: parser/analyzer/HSM/OnException. 

6\. \*\*Release 0.8.0\*\*: pakiety + release notes („Instance Configure, method groups by default; zero-alloc preserved"). 



---



\## 13) Co z argumentami za „static-only"?



\* \*\*„Jasność compile-time"\*\*: dostarczamy ją \*\*diagnostyką\*\* i krótką ramką w docs. Sam fakt `static` nie powstrzymuje złych intuicji, a odbiera ergonomię method groups (których celem jest \*\*właśnie\*\* pozbycie się `nameof`). 

\* \*\*„Precedens generatorów"\*\*: FastFSM Fluent to \*\*DSL dla autorów FSM\*\*, a nie ogólny kontekst konfiguracyjny jak JsonContext/Regex. Priorytetem jest \*\*DX\*\* zgodny z praktyką w bibliotekach FSM (Stateless-like), przy zachowaniu compile-time natury przez twarde reguły. 

\* \*\*„Purity is hard"\*\*: nie implementujemy analizy przepływu danych; \*\*blokujemy wszystko poza method groups\*\*. To proste i skuteczne.



---



\## 14) Appendix — przykłady (before/after)



\*\*Before (0.7.5 style w docs):\*\*



```csharp

.On(Trigger.Start)

&nbsp;   .Guard(nameof(CanStart))

&nbsp;   .Action(nameof(StartProcess))

&nbsp;   .GoTo(State.Running);

```



\*\*After (0.8.0):\*\*



```csharp

.On(Trigger.Start)

&nbsp;   .Guard(CanStart)

&nbsp;   .Action(StartProcess)

&nbsp;   .GoTo(State.Running);

```



\*\*Ambiguity fallback (rare):\*\*



```csharp

// Prefer renaming to unique signatures.

// If not possible:

.On(Trigger.Start)

&nbsp;   .Guard(nameof(CanStartAsync)) // last-resort only

&nbsp;   .GoTo(State.Running);

```



---



To jest kompletny, zaostrzony po dyskusji plan 0.8.0: \*\*instancyjne `Configure()` + method groups + twarde, syntaktyczne zasady\*\*. Uderzamy w UX „jak w Stateless", a ryzyka mentalne i techniczne eliminujemy \*\*diagnozami w compile-time\*\* zamiast „edukacją w README".



---



\## 15) Critical Parser Implementation Changes



\### Required Change 1: FindConfigureMethod (Static → Instance)

```csharp

// CURRENT CODE (searches for static):

private MethodDeclarationSyntax? FindConfigureMethod(ClassDeclarationSyntax classDeclaration)

{

&nbsp;   return classDeclaration.Members

&nbsp;       .OfType<MethodDeclarationSyntax>()

&nbsp;       .FirstOrDefault(m => (m.Identifier.Text == "Configure" || m.Identifier.Text == "SetupStates") \&\& 

&nbsp;                           m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

}



// REQUIRED v0.8.0 (instance primary, static fallback):

private MethodDeclarationSyntax? FindConfigureMethod(ClassDeclarationSyntax classDeclaration)

{

&nbsp;   var methods = classDeclaration.Members

&nbsp;       .OfType<MethodDeclarationSyntax>()

&nbsp;       .Where(m => m.Identifier.Text == "Configure" || m.Identifier.Text == "SetupStates")

&nbsp;       .ToList();

&nbsp;   

&nbsp;   // Check for multiple Configure methods (FSM3080)

&nbsp;   if (methods.Count > 1)

&nbsp;   {

&nbsp;       var descriptor = DiagnosticFactory.Get("FSM3080");

&nbsp;       \_context.ReportDiagnostic(Diagnostic.Create(descriptor, methods\[1].GetLocation()));

&nbsp;   }

&nbsp;   

&nbsp;   // Prefer instance method (v0.8.0 default)

&nbsp;   var instanceMethod = methods.FirstOrDefault(m => !m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

&nbsp;   if (instanceMethod != null) 

&nbsp;   {

&nbsp;       ValidateConfigureMethod(instanceMethod); // New validation

&nbsp;       return instanceMethod;

&nbsp;   }

&nbsp;   

&nbsp;   // Allow static as fallback (not promoted in docs)

&nbsp;   return methods.FirstOrDefault(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

}

```



\### Required Change 2: Add ValidateConfigureMethod

```csharp

private void ValidateConfigureMethod(MethodDeclarationSyntax configureMethod)

{

&nbsp;   var methodSymbol = \_semanticModel?.GetDeclaredSymbol(configureMethod) as IMethodSymbol;

&nbsp;   if (methodSymbol == null) return;

&nbsp;   

&nbsp;   // FSM3081a: Must be private

&nbsp;   if (methodSymbol.DeclaredAccessibility != Accessibility.Private)

&nbsp;   {

&nbsp;       var descriptor = DiagnosticFactory.Get("FSM3081a");

&nbsp;       \_context.ReportDiagnostic(Diagnostic.Create(descriptor, configureMethod.GetLocation(), "Configure"));

&nbsp;   }

&nbsp;   

&nbsp;   // FSM3081b: Must be parameterless

&nbsp;   if (methodSymbol.Parameters.Length > 0)

&nbsp;   {

&nbsp;       var descriptor = DiagnosticFactory.Get("FSM3081b");

&nbsp;       \_context.ReportDiagnostic(Diagnostic.Create(descriptor, configureMethod.GetLocation(), "Configure"));

&nbsp;   }

&nbsp;   

&nbsp;   // FSM3081c: Cannot be virtual/override

&nbsp;   if (methodSymbol.IsVirtual || methodSymbol.IsOverride)

&nbsp;   {

&nbsp;       var descriptor = DiagnosticFactory.Get("FSM3081c");

&nbsp;       \_context.ReportDiagnostic(Diagnostic.Create(descriptor, configureMethod.GetLocation(), "Configure"));

&nbsp;   }

&nbsp;   

&nbsp;   // FSM3082: Not inherited from base class

&nbsp;   if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, \_classSymbol))

&nbsp;   {

&nbsp;       var descriptor = DiagnosticFactory.Get("FSM3082");

&nbsp;       \_context.ReportDiagnostic(Diagnostic.Create(descriptor, configureMethod.GetLocation(), "Configure"));

&nbsp;   }

&nbsp;   

&nbsp;   // FSM3083: Not partial method

&nbsp;   if (configureMethod.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))

&nbsp;   {

&nbsp;       var descriptor = DiagnosticFactory.Get("FSM3083");

&nbsp;       \_context.ReportDiagnostic(Diagnostic.Create(descriptor, configureMethod.GetLocation(), "Configure"));

&nbsp;   }

}

```



\### Required Change 3: Unify ParseGuard/Action/OnEntry/OnExit

Current parser has method group support ONLY in ParseGuard. Need to:

1\. Apply same pattern to ParseAction, ParseOnEntry, ParseOnExit

2\. Add FSM3071-3077 diagnostics for impure DSL



```csharp

// Example for ParseAction (repeat for OnEntry/OnExit):

private void ParseAction(InvocationExpressionSyntax invocation, TransitionModel transition, 

&nbsp;                       StateMachineModel model, Action<string>? report, bool isAsync)

{

&nbsp;   if (invocation.ArgumentList.Arguments.Count > 0)

&nbsp;   {

&nbsp;       var arg = invocation.ArgumentList.Arguments\[0];

&nbsp;       string? methodName = null;

&nbsp;       

&nbsp;       // 1. Try method group (IdentifierName)

&nbsp;       if (arg.Expression is IdentifierNameSyntax methodGroup \&\& \_semanticModel != null)

&nbsp;       {

&nbsp;           var symbolInfo = \_semanticModel.GetSymbolInfo(methodGroup);

&nbsp;           

&nbsp;           // Check for property (FSM3072)

&nbsp;           if (symbolInfo.Symbol is IPropertySymbol)

&nbsp;           {

&nbsp;               var descriptor = DiagnosticFactory.Get("FSM3072");

&nbsp;               \_context.ReportDiagnostic(Diagnostic.Create(descriptor, methodGroup.GetLocation(), 

&nbsp;                   methodGroup.Identifier.Text));

&nbsp;               return;

&nbsp;           }

&nbsp;           

&nbsp;           // Check for ambiguity (FSM3070)

&nbsp;           if (symbolInfo.CandidateSymbols.Length > 1)

&nbsp;           {

&nbsp;               var descriptor = DiagnosticFactory.Get("FSM3070");

&nbsp;               \_context.ReportDiagnostic(Diagnostic.Create(descriptor, methodGroup.GetLocation(),

&nbsp;                   methodGroup.Identifier.Text, symbolInfo.CandidateSymbols.Length));

&nbsp;               return;

&nbsp;           }

&nbsp;           

&nbsp;           if (symbolInfo.Symbol is IMethodSymbol methodSymbol)

&nbsp;           {

&nbsp;               // Check if external (FSM3073)

&nbsp;               if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, \_classSymbol))

&nbsp;               {

&nbsp;                   var descriptor = DiagnosticFactory.Get("FSM3073");

&nbsp;                   \_context.ReportDiagnostic(Diagnostic.Create(descriptor, methodGroup.GetLocation(),

&nbsp;                       methodSymbol.Name));

&nbsp;                   return;

&nbsp;               }

&nbsp;               methodName = methodSymbol.Name;

&nbsp;           }

&nbsp;       }

&nbsp;       // 2. Try this.Method pattern

&nbsp;       else if (arg.Expression is MemberAccessExpressionSyntax memberAccess \&\&

&nbsp;                memberAccess.Expression is ThisExpressionSyntax \&\& \_semanticModel != null)

&nbsp;       {

&nbsp;           // (similar validation as above)

&nbsp;       }

&nbsp;       // 3. Legacy nameof support (not promoted)

&nbsp;       else if (arg.Expression is InvocationExpressionSyntax nameofInvocation \&\&

&nbsp;                nameofInvocation.Expression is IdentifierNameSyntax identifier \&\&

&nbsp;                identifier.Identifier.Text == "nameof")

&nbsp;       {

&nbsp;           // Existing code

&nbsp;       }

&nbsp;       // 4. Block lambdas and expressions (FSM3071)

&nbsp;       else if (arg.Expression is LambdaExpressionSyntax || 

&nbsp;                arg.Expression is ParenthesizedLambdaExpressionSyntax ||

&nbsp;                arg.Expression is SimpleLambdaExpressionSyntax)

&nbsp;       {

&nbsp;           var descriptor = DiagnosticFactory.Get("FSM3075");

&nbsp;           \_context.ReportDiagnostic(Diagnostic.Create(descriptor, arg.GetLocation()));

&nbsp;           return;

&nbsp;       }

&nbsp;       // 5. Block all other expressions (FSM3071)

&nbsp;       else if (!(arg.Expression is LiteralExpressionSyntax))

&nbsp;       {

&nbsp;           var descriptor = DiagnosticFactory.Get("FSM3071");

&nbsp;           \_context.ReportDiagnostic(Diagnostic.Create(descriptor, arg.GetLocation()));

&nbsp;           return;

&nbsp;       }

&nbsp;       

&nbsp;       if (!string.IsNullOrEmpty(methodName))

&nbsp;       {

&nbsp;           transition.ActionMethod = methodName;

&nbsp;           if (isAsync) transition.ActionIsAsync = true;

&nbsp;           report?.Invoke($"\[FluentParser] Set action via method group: {methodName}");

&nbsp;       }

&nbsp;   }

}

```



\### Required Change 4: Update ParseOnException

ParseOnException needs method group support (currently only supports nameof):

```csharp

// Add after nameof handling in ParseOnException:

else if (arg.Expression is IdentifierNameSyntax methodGroup \&\& \_semanticModel != null)

{

&nbsp;   var symbolInfo = \_semanticModel.GetSymbolInfo(methodGroup);

&nbsp;   if (symbolInfo.Symbol is IMethodSymbol methodSymbol)

&nbsp;   {

&nbsp;       methodName = methodSymbol.Name;

&nbsp;   }

&nbsp;   else if (symbolInfo.CandidateSymbols.Length > 1)

&nbsp;   {

&nbsp;       var descriptor = DiagnosticFactory.Get("FSM3070");

&nbsp;       \_context.ReportDiagnostic(Diagnostic.Create(descriptor, methodGroup.GetLocation(),

&nbsp;           methodGroup.Identifier.Text, symbolInfo.CandidateSymbols.Length));

&nbsp;       return;

&nbsp;   }

}

```



\### Integration with Existing Components

Parser already has all required infrastructure:

\- \*\*EnsureAnalyzers()\*\* - initializes CallbackSignatureAnalyzer

\- \*\*\_callbackAnalyzer\*\* - validates method signatures  

\- \*\*\_typeHelper\*\* - TypeSystemHelper for type resolution

\- \*\*\_semanticModel\*\* - for symbol resolution

\- \*\*DiagnosticFactory\*\* - for creating diagnostics



No new infrastructure needed, just proper application of existing components.



\### Test Coverage Requirements

1\. \*\*Method groups\*\*: IdentifierName, this.Member for all callbacks

2\. \*\*Purity violations\*\*: lambdas (FSM3075), expressions (FSM3071), properties (FSM3072)

3\. \*\*Configure validation\*\*: private, parameterless, non-virtual, instance (FSM3081a-d)

4\. \*\*Ambiguity\*\*: multiple overloads (FSM3070)

5\. \*\*External methods\*\*: FSM3073

6\. \*\*Regression\*\*: HSM, priorities, OnException still work

