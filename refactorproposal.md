Obecna architektura generatorów i jej ograniczenia

Obecnie FastFSM generuje różne warianty maszyn stanów w zależności od użytych funkcji (OnEntry/OnExit, payload, extensions itp.). Mechanizm wyboru wariantu jest zrealizowany poprzez klasę VariantSelector oraz instrukcję switch, która wybiera odpowiedni generator źródła

GitHub

. Przykładowo mamy osobne klasy: CoreVariantGenerator (dla “Pure”/“Basic”), PayloadVariantGenerator (dla maszyn z payload), ExtensionsVariantGenerator (dla maszyn z extensions) oraz FullVariantGenerator (dla maszyn z payload + extensions). Taka architektura wprowadza złożoną hierarchię dziedziczenia – np. FullVariantGenerator dziedziczy po PayloadVariantGenerator, aby ponownie wykorzystać logikę obsługi payload

GitHub

. To rozwiązanie działa, ale odbiega od zasady pojedynczej odpowiedzialności (SRP): każda klasa generatora zajmuje się wieloma aspektami jednocześnie (generowanie kodu bazowego, obsługa OnEntry/OnExit, obsługa payload, obsługa extensions, HSM itp.), a logika jest rozproszona między wieloma klasami. W efekcie pojawiły się poprawki ad-hoc (np. przy dodawaniu HSM), które trudno wkomponować elegancko w istniejącą hierarchię. Warianty vs cechy: W dokumentacji architektury widać, że dotychczasowe „warianty” to kombinacje cech

GitHub

. Np. Pure – tylko przejścia, Basic – przejścia + OnEntry/OnExit, WithPayload – wsparcie payload, WithExtensions – wsparcie rozszerzeń, Full – wszystkie funkcje. Obecna implementacja odwzorowuje te kombinacje jako osobne klasy generatorów. Jak sam zauważyłeś, taki podział zwiększa złożoność – dodanie nowej cechy (np. HSM) wymagało przeróbek w wielu miejscach. W przypadku HSM zdecydowałeś nie tworzyć nowego wariantu, tylko warunkowo włączyć logikę hierarchii w istniejących generatorach (np. metody WriteHierarchyArrays/WriteHierarchyMethods są wywoływane tylko gdy Model.HierarchyEnabled jest true). To podejście „horyzontalne” okazało się skuteczne – kod HSM został wkomponowany jako dodatkowa funkcjonalność w generatorze, a nie kolejny osobny wariant, dzięki czemu HSM współdziała z innymi cechami bez mnożenia klas.

Propozycje refaktoryzacji architektury

Poniżej przedstawiam kilka kierunków refaktoryzacji, które pomogą uprościć kod generatorów FastFSM przy zachowaniu 100% dotychczasowej funkcjonalności. Kluczowa idea to traktowanie obsługiwanych opcji jako niezależnych cech, a nie sztywne warianty. Dzięki temu można spłaszczyć hierarchię i wyeliminować duplikację kodu, unikając jednocześnie nadmiernej komplikacji.

Aktualizacja po fazie 4 (zrealizowane)

- Ujednolicono kontrakt hooków rozszerzeń zgodnie z UML (run‑to‑completion):
  Before → GuardEvaluation → GuardEvaluated → Exit → Action → State change → Entry → After(success/fail).
- Uzupełniono wywołania `OnGuardEvaluated` we wszystkich ścieżkach generatora (sync/async, payload/no‑payload) używających extensions.
- W ścieżce sync + extensions ustawienie stanu wykonywane jest przed `OnEntry`, a `Action` przed `OnEntry` (zgodnie z UML). W wariantach bez extensions utrzymano dotychczasową kolejność (kompatybilność z obecnymi testami Basic/Core).
- README uzupełnione o sekcję „Extension Hooks” z dokładną kolejnością i zasadami.
- Testy przechodzą — brak regresji w obszarze hooków/kolejności.

1\. Spłaszczenie hierarchii generatorów – warianty jako cechy

Zrezygnuj z oddzielnych klas dla kombinacji cech na rzecz pojedynczego generatora (lub minimalnego zestawu generatorów), który obsługuje różne funkcjonalności warunkowo. Zamiast czterech klas wariantów wybieranych switch’em

GitHub

, można zaimplementować np. jedną klasę StateMachineGenerator (dziedziczącą po obecnej bazowej StateMachineCodeGenerator), która wewnątrz metod generujących kod sprawdza flagi typu: Model.HasPayload, Model.HasExtensions, Model.HierarchyEnabled, itp. i na tej podstawie włącza lub pomija fragmenty kodu. Takie podejście uprości logikę wyboru – nie będzie już potrzeby ustalania model.Variant ani tworzenia obiektów różnych klas

GitHub

. Każda cecha stanie się niezależnym wymiarem:

Payload: Jeśli maszyna ma zdefiniowany payload (PayloadTypeAttribute), generator wygeneruje odpowiednie metody Fire/TryFire/CanFire z parametrem payload (bądź generyczne dla multi-payload) oraz strukturę PayloadMap dla walidacji typu. Jeśli brak payload – te fragmenty kodu są pomijane.

Extensions: Jeśli włączono mechanizm rozszerzeń (np. użyto interfejsów IStateMachineExtension), generator doda pola i logikę \_extensions oraz wstawi wywołania \_extensionRunner.RunBeforeTransition/RunAfterTransition w odpowiednich miejscach. Bez extensions – nie generujemy tego kodu.

OnEntry/OnExit: Można uzależnić generowanie wywołań callbacków od tego, czy jakikolwiek stan ma zdefiniowane metody OnEntry/OnExit. Jeżeli nie – pomijamy generowanie kodu wywołującego te metody (co już częściowo jest robione przez kontrolę ShouldGenerateOnEntryExit() w kodzie).

HSM: Zachować dotychczasowe sterowanie przez Model.HierarchyEnabled. Gdy true – generujemy tablice s\_parent, s\_initialChild itd. oraz nadpisujemy Start()/StartAsync() i IsIn()/GetActivePath()

GitHub

GitHub

. Gdy false – kod HSM się nie pojawi (co już jest realizowane przez warunki w metodach WriteHierarchy\*).

Async vs Sync: Nadal warunkowo generujemy warianty metod z async/ValueTask lub zwykłe, ale to już jest opanowane przez flagę IsAsyncMachine w Model.

Takie horyzontalne podejście eliminuje potrzebę dziedziczenia jak w FullVariantGenerator -> PayloadVariantGenerator

GitHub

. Zamiast tego, pojedynczy generator może w ramach jednej klasy obsłużyć pełen zakres funkcji – od najprostszego FSM do pełnego FSM z payload, HSM i extensions – poprzez instrukcje warunkowe i wywołania pomocniczych metod. W praktyce oznacza to, że „wariant” nie będzie już wybierany z góry, tylko każda cecha dołoży swój kod w trakcie generowania. To uprości też rozwój: dodanie nowej cechy (lub rozszerzenie istniejącej) wymaga dopisania obsługi w jednym miejscu, zamiast tworzenia nowej klasy wariantu i duplikacji logiki. Zachowanie optymalizacji: Mimo ujednolicenia kodu, nadal możesz zachować zasadę generowania “tylko potrzebnego” kodu. Warunki if/else w generatorze spowodują, że np. dla maszyny bez payload nie pojawi się zbędny słownik typów czy parametry payload w metodach. Przykład już istnieje przy obsłudze multi-payload: kod generatora dodaje mapę typów tylko jeśli wykryto wiele różnych typów payload

GitHub

. Podobnie, możesz kontrolować generowanie metod przeciążonych Fire<TPayload> i sprawdzania typu payload w CanFire wyłącznie gdy Model.TriggerPayloadTypes jest niepusty (multi-payload). Dzięki temu ostateczny wygenerowany kod pozostanie minimalny dla danej konfiguracji, nawet jeśli sam generator obsługuje wiele scenariuszy. Interfejsy i klasa bazowa: Upewnij się, że rezygnacja z osobnych wariantów zgrywa się z doborem bazowej klasy runtime. Aktualnie GetBaseClassName i GetInterfaceName wybierają odpowiednią bazę (np. StateMachineAsync vs StateMachineSync, rozszerzalną vs nie) zależnie od wariantu. W nowym podejściu wciąż możesz decydować dynamicznie: np. jeśli HasExtensions -> użyj bazowej klasy rozszerzalnej ExtensibleStateMachineSync/Async, jeśli nie -> zwykłej. Analogicznie, jeśli HasPayload -> może bazowa klasa ma obsługę payload (choć prawdopodobnie i tak obsługujesz payload w kodzie wygenerowanym, więc może wystarczyć jedna baza). Ważne, że ten wybór nadal może być zrobiony prostym if-em na początku generacji, zamiast przez odrębne klasy generatorów.

2\. Wydzielenie logiki do klas pomocniczych (helpers)

Odciążenie klas generatorów: Aby zbliżyć się do SRP, staraj się, by klasa generatora pełniła głównie rolę orkiestratora procesu generowania kodu, podczas gdy szczegółowe fragmenty kodu będą produkowane przez wyspecjalizowane funkcje pomocnicze. Już teraz część takiej architektury istnieje – np. wywołujesz GuardGenerationHelper.EmitGuardCheck(...) w wielu miejscach zamiast generować ręcznie try-catch dla guardów

GitHub

GitHub

. Podobnie CallbackGenerationHelper.EmitCallbackInvocation(...) zajmuje się wywołaniem OnEntry/OnExit/Action z prawidłowym doborem przeciążenia (z payload, z tokenem, async vs sync) i obsługą wyjątków

GitHub

GitHub

. To dobry kierunek – rozszerz go na inne aspekty:

Rozważ dodanie helpera do części payload. Może to być klasa lub zestaw metod statycznych, które generują np. słownik PayloadMap i logikę walidacji typu. Możesz np. mieć PayloadGenerationHelper.GeneratePayloadSupport(Sb, Model) który:

Jeśli Model.HasPayload: wygeneruje deklaracje zmiennych/dynamiczny słownik typów dla multi-payload,

Wygeneruje ewentualne dodatkowe metody (np. dodatkowe przeciążenia Fire/CanFire już jako gotowe fragmenty kodu).

Dla multi-payload może wygenerować też wewnętrzne rzutowanie/casting payload do właściwego typu w FireAsync itp.

Jeśli brak payload – metoda może nic nie dodawać albo nie być wywoływana.

Dzięki temu kod obsługi payload nie będzie rozsiany po wielu miejscach generatora, tylko skoncentrowany w jednym helperze. Będzie go też łatwiej zmodyfikować w przyszłości (np. gdybyś zmieniał sposób przechowywania payloadów).

Podobnie dla extensions: widzę, że już masz pewną kapsułkę ExtensionsFeatureWriter używaną w ExtensionsVariantGenerator i FullVariantGenerator

GitHub

GitHub

. Możesz pójść krok dalej i sprawić, by logika extensions była dodawana poprzez wywołanie metod tego writera z poziomu głównego generatora, zamiast poprzez dziedziczenie. Np. zamiast FullVariantGenerator : PayloadVariantGenerator wywołującego \_ext.WriteFields() wewnątrz klasy, mógłbyś w uniwersalnym generatorze zrobić:

if (Model.HasExtensions) {

&nbsp;   var extWriter = new ExtensionsFeatureWriter();

&nbsp;   extWriter.WriteFields(Sb);

}

i analogicznie wstawki \_ext.WriteManagementMethods(Sb) itp. Warunkowe wstawienie fragmentów kodu extension na poziomie generatora uprości hierarchię i usunie potrzebę istnienia osobnej klasy tylko po to, by wywołać te metody. Sam ExtensionsFeatureWriter (który już istnieje) pełni rolę “helpera” generującego kod pól \_extensions i metod zarządzających rozszerzeniami – to zgodne z Twoim celem trzymania jak najwięcej kodu w helperach.

Struktura przejść (Transitions): Aktualnie generowanie logiki przejść jest dość złożone i częściowo obsłużone przez planner’y (flat vs hierarchical) oraz metodę WriteTransitionLogic (przeciążaną w różnych wariantach). Możesz zostawić obecną strukturę planowania (to już odseparowało część logiki decyzyjnej – np. HierarchicalTransitionPlanner vs FlatTransitionPlanner – od samego generatora). Warto jednak uprościć samo WriteTransitionLogic. Zamiast mieć różne implementacje w klasach CoreVariantGenerator, PayloadVariantGenerator, ExtensionsVariantGenerator, spróbuj scalić je:

W jednej metodzie, która warunkowo wykonuje poszczególne kroki: np. przed główną logiką przejścia sprawdza i wywołuje extension hooki BeforeTransition i GuardEvaluation jeśli HasExtensions, wywołuje sprawdzenie guarda zawsze (helper już to robi), owija główny blok try { … } catch { … } tylko jeśli potrzebujesz finalizacji dla extension (RunAfterTransition false) lub jeśli chcesz przechwycić wyjątek dla własnego ExceptionHandler.

Innymi słowy, Twój WriteTransitionLogic może zawierać wewnątrz kilka if (Model.HasExtensions) bloków do wstawienia fragmentów, zamiast istnienia oddzielnej wersji metody w każdej klasie wariantu. Choć to wprowadzi kilka if/else w kod generatora, będzie to czytelniejsze niż skakanie po klasach w hierarchii. Pamiętaj – te warunki wykonują się na etapie generacji (kompilacji), więc nie wpływają na runtime performance wygenerowanej maszyny.

Przykład podejścia jednolitego: w klasie generatora możemy mieć jedną metodę WriteTransitionLogic(transition) taką, która zrobi:

if (Model.HasExtensions)

&nbsp;   WriteBeforeTransitionHook(...);

if (!string.IsNullOrEmpty(transition.GuardMethod)) {

&nbsp;   if (Model.HasExtensions)

&nbsp;       WriteGuardEvaluationHook(...);

&nbsp;   WriteGuardCall(...);  // używa GuardGenerationHelper

}

if (Model.HasExtensions) Sb.AppendLine("try"); 

using (Sb.Block(Model.HasExtensions ? "" : null)) { 

&nbsp;   // OnExit (o ile nie internal transition i jest OnExitMethod)

&nbsp;   if (!transition.IsInternal \&\& hasOnEntryExit \&\& fromStateDef?.OnExitMethod != null)

&nbsp;       ... // wygeneruj OnExit (tu też ewentualnie można użyć CallbackGenerationHelper do OnExit)

&nbsp;   // Action (jeśli zdefiniowana)

&nbsp;   if (!string.IsNullOrEmpty(transition.ActionMethod))

&nbsp;       ... // wygeneruj Action, np. przez CallbackGenerationHelper lub WriteActionCall

&nbsp;   // OnEntry (jeśli nie internal i toState ma OnEntry)

&nbsp;   if (!transition.IsInternal \&\& hasOnEntryExit \&\& toStateDef?.OnEntryMethod != null)

&nbsp;       ... // wygeneruj OnEntry

&nbsp;   Sb.AppendLine("success = true;");

}

if (Model.HasExtensions) {

&nbsp;   Sb.AppendLine("} catch { success = false; }");

&nbsp;   WriteAfterTransitionHook(..., success:true/false);

}

Sb.AppendLine("return success;");

Powyższy pseudokod pokazuje, że jednym ciągiem obsługujesz zarówno przypadek z extension (otaczając główną logikę try/catch i ustawiając success, aby wywołać hooki RunAfterTransition) jak i bez extension (gdzie nie generujesz w ogóle bloku try/catch ani zmiennej success – zamiast tego ewentualny wyjątek propaguje się lub jest obsłużony przez Twój mechanizm ExceptionHandler). To upraszcza architekturę: nie musisz już mieć rozdzielonych metod WriteTransitionLogic w 3 klasach, co redukuje liczbę miejsc do modyfikacji przy przyszłych zmianach. Kod będzie dłuższy w jednej metodzie, ale za to linearny i łatwiej zrozumieć pełną sekwencję przejścia. Naturalnie, powyższe należy dostosować do istniejących helperów (np. korzystać z EmitActionWithExceptionPolicy dla obsługi ExceptionHandler itp.). Ideą jest jednak, by logika nie była pochowana w gąszczu klas, tylko skupiona i uwarunkowana flagami.

3\. Konsolidacja duplikującej się logiki

Przy refaktoryzacji zwróć uwagę na fragmenty powtarzające się w różnych wariantach, które można zunifikować:

Generowanie interfejsu i klas: W każdej klasie wariantu pojawia się kod otwierający przestrzeń nazw, generujący interfejs I<ClassName> i definicję klasy partial z odpowiednią bazą

GitHub

GitHub

. Te fragmenty są bardzo podobne, różnią się właściwie tylko wyborem bazowej klasy/interfejsu (z extension lub bez). Można napisać jedną metodę w generatorze, np. WriteNamespaceAndClassContent(), która:

Otoczy logikę w namespace { } jeśli trzeba,

Otworzy klasy kontenerów (jeśli klasa docelowa jest zagnieżdżona),

Wygeneruje interfejs i klasę z dziedziczeniem. Bazując na flagach, wybierze nazwę interfejsu: jeśli HasExtensions to IExtensibleStateMachineSync/Async, inaczej zwykłe IStateMachineSync/Async (co i tak robi GetInterfaceName).

Analogicznie wybierze bazową klasę poprzez istniejącą metodę GetBaseClassName.

Taka uniwersalna metoda zastąpi 4 osobne implementacje w wariantach. Będzie trochę warunków w środku, ale jak wyżej – to warunki kompilacyjne, generujące albo jedną, albo drugą wersję kodu.

Obsługa Async vs Sync: Sporo metod ma wariant async i sync (np. Start() vs StartAsync(), Fire() vs FireAsync(), CanFire() vs CanFireAsync()). Aktualnie rozwiązane jest to tak, że generator sprawdza IsAsyncMachine i generuje odpowiednie metody (często obie wersje w razie potrzeby, z tym że w async maszynie wersja sync rzuca wyjątkiem, jak w Fire() dla async – SyncCallOnAsyncMachineException). Ten mechanizm możesz zachować, ale przenieść go do pojedynczych metod pomocniczych:

Widzimy np. metodę WriteFireMethods() w PayloadVariantGenerator

GitHub

GitHub

&nbsp;– generuje cztery warianty w zależności od (async/sync) × (single/multi payload). Można to uprościć, np. rozbić na dwie metody: WriteFireMethodsSync() i WriteFireMethodsAsync(), każda z wewnętrznymi if-ami dla single/multi. Lub nawet jedna metoda z if IsAsyncMachine na górze, jak jest teraz, jest OK – ale może warto przenieść ją do np. AsyncGenerationHelper czy nowego FireMethodHelper dla czytelności. Podobnie dla CanFireMethods. Ważne, że te wzorce (rzucenie wyjątku w sync metodzie dla async maszyny, itp.) są powtarzalne – ujęcie ich w jednym miejscu ułatwi utrzymanie.

Rozważ, czy maszyny bez payload potrzebują osobnej implementacji Fire(). Obecnie w wariancie Core generuje się public void Fire(TTrigger trigger) i TryFireInternal(TTrigger trigger) bez parametru payload. W wariancie payload generuje się inne sygnatury. W podejściu zunifikowanym prawdopodobnie będziesz musiał generować obie wersje zależnie od flag:

Jeśli brak payload: wygeneruj metodę Fire(TTrigger) bez parametru (i analogicznie TryFireInternal bez parametru).

Jeśli jest payload: generuj Fire(TTrigger, TPayload) lub generyczne Fire<TPayload> jak dotychczas.

Możesz to osiągnąć np. pisząc w generatorze:

if (!Model.HasPayload) {

&nbsp;   Sb.Block($"public void Fire({triggerType} trigger)") { ... }

} else if (Model.TriggerPayloadTypes.Any()) {

&nbsp;   // multi-payload

&nbsp;   Sb.Block($"public void Fire<TPayload>({triggerType} trigger, TPayload payload)") { ... }

} else {

&nbsp;   // single payload

&nbsp;   string payloadType = Model.DefaultPayloadType;

&nbsp;   Sb.Block($"public void Fire({triggerType} trigger, {payloadType} payload)") { ... }

}

i analogicznie dla FireAsync i TryFireInternal. W ten sposób sygnatury pozostaną optymalne (nie dodajemy niepotrzebnego parametru object? payload do maszyn bez payload – zachowujemy zero-overhead), a kod jest generowany z jednej klasy.

Upewnij się przy tym, że dopasujesz się do interfejsów. Może się okazać, że np. IStateMachineSync deklaruje Fire(TTrigger) i TryFire(TTrigger) bez payload, podczas gdy IExtensibleStateMachineSync (dla extensions) może deklarować wersje z object? payload. Jeśli tak, to faktycznie maszyna z extensions nawet bez payload musiała mieć inny podpis metody, co komplikowało sprawę. W nowym designie możesz to rozwiązać definiując własny interfejs I<ClassName> (co już generujesz) rozszerzający odpowiedni bazowy. W nim możesz ukryć niepotrzebne metody lub doprecyzować te z payloadem. Przykładowo, dla maszyny bez payload implementującej IStateMachineSync, Twój I<ClassName> może po prostu dziedziczyć z IStateMachineSync<TState,TTrigger> (który ma Fire(trigger)); dla maszyny z payload generujesz interfejs dziedziczący z IStateMachineSync ale nie z IStateMachineSync<TState,TTrigger> tylko definiujesz potrzebne metody sam (lub dziedziczysz z innego interfejsu, jeśli jest). To jednak szczegół – pointa jest, że generując interfejs per klasa (co już robisz) masz swobodę dostosować API publiczne do faktycznych cech maszyny.

4\. Lepsza organizacja kodu generatora – czytelność ponad „polish”

Chcesz uniknąć over-engineeringu, więc nie ma potrzeby tworzyć skomplikowanego systemu pluginów do generowania. Zamiast tego, uporządkuj istniejący kod w logiczne sekcje i usuń to, co zbędne:

Sekcje kodu w generatorze: Podziel kod generatora na wyraźne fragmenty odpowiadające kolejnym częściom klasy wynikowej:

Header/usings – (to już jest w WriteHeader()),

Deklaracja interfejsu i klasy (namespace, interfejs, podpis klasy),

Pola statyczne i instancyjne – tu kolejno: identyfikator \_instanceId dla async (jeśli dotyczy), pola loggera, tablice HSM (jeśli HSM), mapy payload (jeśli multi-payload), itp. Wygeneruj je w zorganizowany sposób, komentarzami oddzielając sekcje (np. już dodajesz komentarz // Hierarchical state machine support arrays przed tablicami HSM

GitHub

).

Konstruktor – generuj po polach.

Metody lifecycle (Start/StartAsync) – tylko jeśli HSM lub inne wymagania (jak już robisz w WriteStartMethod()

GitHub

GitHub

).

Metody OnInitialEntry/OnInitialEntryAsync – jeśli potrzebne (gdy są jakieś OnEntry w stanach początkowych)

GitHub

GitHub

.

Metody główne FSM: TryFireInternal/TryFireInternalAsync, Fire/FireAsync, CanFire/… – tu możesz grupować je razem dla czytelności (wygeneruj najpierw wszystkie TryFire, potem Fire, potem CanFire).

Metody pomocnicze API: GetPermittedTriggers, GetActiveStates/IsIn (HSM), ewentualnie Reset czy inne strukturalne.

Inne: rozszerzenia, hooki – chociaż większość hooków zaimplementujesz wewnątrz powyższych metod, nie jako osobne publiczne API.

Uporządkowanie generacji w takiej kolejności sprawi, że kod wynikowy w pliku \*.Generated.cs też będzie lepiej czytelny dla użytkownika biblioteki, co jest wartością dodaną.

Usunięcie zbędnych elementów: Jeśli jakieś części starej architektury stają się niepotrzebne, usuń je zdecydowanie:

Enum GenerationVariant i całą logikę DetermineVariant() można uprościć lub wyrzucić. Być może w StateMachineModel nadal warto mieć boole typu HasPayload, HasExtensions (albo po prostu wnioskować z danych: np. TriggerPayloadTypes.Any() oznacza payload; ExtensionsUsed flaga z atrybutu?). Zamiast jednak ustalać jeden z czterech wariantów, możesz pozostawić Variant tylko dla logowania/debug (informacja), a sam nie używać go do przepływu sterowania. Docelowo możesz w ogóle zrezygnować z model.Variant, bo nie będzie potrzebny przy generowaniu (generator i tak sprawdzi poszczególne flagi).

Oddzielne klasy wariantów (CoreVariantGenerator, itp.) – po przeniesieniu ich logiki do jednego generatora, te klasy staną się zbędne. Możesz je usunąć, zmniejszając liczbę plików i potencjalnego chaosu. Mniej klas = mniejszy dług poznawczy dla nowych kontrybutorów projektu.

Stare helpery/narzędzia, które były potrzebne tylko przez zawiłości architektury wariantów, np. jeśli istniały jakieś konstrukcje typu ContinueOnCapturedContext czy inne przekazywane parametry, które teraz można uprościć, warto to zrobić. Jednak bądź ostrożny, by nie usunąć wsparcia dla istniejących ustawień (np. parametru w konstruktorze async maszyn dot. continueOnCapturedContext – to jest funkcjonalność publiczna, więc musi zostać).

Partial classes / organizacja plików: Rozważ użycie klas partial lub przynajmniej podzielenia kodu generatora na kilka plików źródłowych logicznie (to dla porządku wewnętrznego projektu). Na przykład, możesz mieć plik StateMachineGenerator\_Base.cs z metodami głównymi i np. payload, StateMachineGenerator\_Extensions.cs z metodami tylko generującymi fragmenty extension, albo inny podział (np. osobno część WriteTransitionLogic bo jest długa). Partiale pozwolą Ci edytować osobno różne aspekty bez scrollowania 3000 linijek jednej klasy. To nie zmienia działania programu (wygenerowana klasa jest jedna), ale może pomóc uniknąć chaosu. Jeśli jednak uznasz to za nadmierny polish – nie jest to konieczne; możesz równie dobrze osiągnąć czytelność poprzez dobre komentarze i sekcje w jednym pliku.

5\. Zachowanie zgodności i testowanie regresji

Refaktoryzując architekturę, kluczowe jest niezmienienie zewnętrznej funkcjonalności i API. Masz już bogaty zestaw testów jednostkowych (wspomniałeś, że HSM przechodzi wszystkie testy, ale pojawiła się regresja w wariancie payload FSM). W pierwszej kolejności napraw oczywiście tę regresję w payload – testy wskażą co jest nie tak. Następnie, podczas refaktoryzacji:

Uruchamiaj wszystkie testy po każdym większym kroku refaktoryzacji. To natychmiast wychwyci, jeśli np. pominąłeś generowanie jakiegoś przypadku. Mając 100% pokrycia funkcjonalności, testy powinny pozostać zielone. Jeżeli jakikolwiek test zacznie failować, będzie to sygnał, że nowy generator nie uwzględnił jakiegoś detalu (np. wyjątkowego zachowania przy payload+HSM – zwróć uwagę na notatkę z f07.md że OnEntry z payload nie jest wołany przy automatycznym zejściu do initial child

GitHub

&nbsp;– upewnij się, że takie niuanse są odwzorowane w kodzie generowanym).

Porównaj wygenerowany kod przed i po refaktoryzacji dla kilku kombinacji cech. Możesz wziąć przykładowe maszyny:

prostą bez payload i bez HSM,

z OnEntry ale bez payload,

z payload single,

z payload multi,

z HSM + payload,

z extension, itd.,

i wygenerować ich kod źródłowy oboma wersjami generatora (starym i nowym). Różnice powinny dotyczyć tylko ewentualnie kosmetyki (np. kolejność metod w pliku czy drobne zmiany w nazwach zmiennych, jeśli to sprzątniesz), ale semantycznie i wydajnościowo kod powinien być równoważny. Taki diff da Ci pewność, że nic nie zgubiono.

Zachowaj wydajność: FastFSM chlubi się przejściami <1 ns i zerowymi alokacjami, więc upewnij się, że refaktoryzacja tego nie popsuje. Skoro generujesz kod, to dopóki generowany kod pozostaje taki sam (lub bardzo zbliżony), wydajność nie ucierpi. Uważaj, by np. nie wprowadzić niezamierzonych dodatkowych sprawdzeń runtime. Na przykład, nie chciałbyś, żeby w każdej metodzie Fire() generowała się instrukcja if (HasPayload) ... – na szczęście, Ty wykonujesz te if-y w generatorze podczas kompilacji, a nie w wygenerowanym kodzie. To jest poprawne podejście. Pilnuj tylko, by nie dodawać np. globalnych flag w runtime zamiast decyzji w compile-time. Wygląda na to, że w pełni to rozumiesz i zamierzasz trzymać.

6\. Unikanie over-engineeringu i zbędnego polerowania

Na koniec, pamiętaj o swoim założeniu: zmiany mają uprościć kod, a nie wprowadzić nową abstrakcję dla sztuki. Kilka konkretnych rad, by nie przekombinować:

Nie twórz skomplikowanych wzorców projektowych, jeśli nie są absolutnie potrzebne. Np. wzorzec Strategy dla „cech” mógłby polegać na tym, że masz listę obiektów implementujących interfejs IFeatureGenerator z metodami GenerateFields(), GenerateMethods() itd., które wywołujesz w pętli. Choć to brzmi elegancko, prawdopodobnie doda niepotrzebnej złożoności (trzeba by przekazywać kontekst generatora, StringBuilder itp. do każdego takiego obiektu). Prostsze if (cecha) { ... } w kodzie generatora jest bardziej przejrzyste i łatwiejsze do debugowania. Twoim celem jest zmniejszenie liczby klas i powiązań, więc nie zastępuj starych wariantów nowym, równie złożonym mechanizmem plug-inów. Wykorzystaj to, co już masz: statyczne helpery i proste warunki.

Zachowaj przejrzystość kosztem drobnego powielenia kodu: Dużo już osiągnąłeś wyciągając logikę do helperów (guards, callbacks). Jeśli jakieś fragmenty kodu nadal będą się powtarzać w generatorze dla różnych gałęzi, a próba ich pełnego uogólnienia wprowadza tylko zamęt – zaakceptuj pewien duplikat. Lepiej mieć czytelne dwa bloki kodu dla sync i async niż jedną mega-funkcję, która poprzez parametry stara się obsłużyć oba na raz, mieszając logikę. Patrząc na Twój kod, wydaje się że rozsądnie balansujesz tę kwestię (np. WriteGetPermittedTriggersMethod jest w CoreVariantGenerator osobno dla async vs sync, co jest OK

GitHub

). Nie musisz na siłę scalać wszystkiego – skup się na redukcji wielkich struktur klas i ich zależności, a nie na każdej linijce. Innymi słowy, refaktor architekturę, niekoniecznie każdy drobny fragment kodu.

Dokumentuj nowe podejście: Skoro projekt jest Twój, dobrze rozumiesz “serce” FastFSM – ale warto ułatwić przyszłe modyfikacje. Możesz w pliku README (albo w osobnym dokumencie architecture.md) opisać krótko nową filozofię: że generator działa modułowo, cechy włączane są warunkowo, i wypisać listę cech (payload, HSM, extensions, logging…) wraz z informacją gdzie w kodzie generatora są obsługiwane. To nie jest over-engineering, a raczej inwestycja w utrzymanie. Nawet Tobie za pół roku taka notatka pomoże szybciej odnaleźć się w kodzie.

Podsumowując, refaktoryzacja powinna uczynić kod generatorów prostszym w zrozumieniu i modyfikacji. Spłaszczenie hierarchii i uczynienie z „wariantów” zwykłych cech konfiguracyjnych uprości dodawanie kolejnych funkcjonalności (np. jeśli w przyszłości dodałbyś kolejny aspekt FSM, jak np. regiony ortogonalne czy coś podobnego, nie będziesz musiał tworzyć “wariantu FullWithNewFeature” – po prostu dodasz nową cechę). Przeniesienie logiki do helperów sprawi, że klasy generatorów będą bardziej przejrzyste i bliższe SRP – staną się koordynatorami generowania, podczas gdy szczegółowe operacje wykonują wyspecjalizowane funkcje (nawet jeśli mają dużo parametrów – to akceptowalne, bo izolujemy w ten sposób złożoność). Dzięki temu łatane wcześniej „na szybko” fragmenty kodu nabiorą uporządkowania. Na koniec dnia, po takim refaktorze, FastFSM nadal będzie działać tak samo szybko i poprawnie, ale jego wewnętrzny kod stanie się bardziej harmonijny. Mniej klas i wariantów, bardziej czytelna struktura warunkowa – to wszystko ułatwi Ci dalszy rozwój bez obaw, że drobna zmiana znów wprowadzi regresję. Powodzenia w refaktoryzacji! Wszystkie testy przechodzące przy nowej strukturze na pewno potwierdzą słuszność obranej drogi.


Postęp prac (Faza 2 — Core/Basic)

- Data: 2025-08-15
- Status: zakończona (bez regresji w Core; 2 znane testy poza zakresem)

Wykonane (szczegółowo):
- UnifiedStateMachineGenerator: skonsolidowano generowanie struktury TryFire — klasa deleguje teraz do bazowej `StateMachineCodeGenerator.WriteTryFireStructure(...)` z `WriteTransitionLogic`, co zapewnia parytet dla flat/HSM i usuwa rozbieżną implementację.
- Dodano generowanie konstruktora (parametry, base-call, logger, inicjalizacja HSM `_lastActiveChild`).
- Dodano `OnInitialEntryAsync(...)` (HSM: łańcuch root→leaf, uwzględnienie tokenu; non-HSM: pojedynczy case).
- Nadpisano `ShouldGenerateInitialOnEntry()` i `ShouldGenerateOnEntryExit()` jak w Core: wyłączone dla wariantu Pure.
- Potwierdzono: dla wariantów Pure/Basic UnifiedStateMachineGenerator nie używa już delegacji do `CoreVariantGenerator` (obsługa bezpośrednia z flagami cech typu `HasOnEntryExit`, `IsHierarchical`).
- Usunięto zbędne, lokalne implementacje `WriteTransitionLogicAsync/Sync` i pomocnicze (guard/callback), ponieważ całość pokrywa baza — mniejsze ryzyko driftu.
- Dodano asynchroniczne `CanFireInternalAsync(...)` dla maszyn async, zgodnie z Core; dla sync używana jest bazowa wersja `CanFireInternal(...)`.
- Nadpisano `WriteTransitionLogic(...)` w Unified, aby wymusić token-aware wywołania OnExit/OnEntry/Action poprzez helpery (`WriteOnExitCall`, `EmitOnEntryWithExceptionPolicy`, `EmitActionWithExceptionPolicy`) i async-aware guardy — usuwa przypadki typu `OnEntryAsync()` bez przekazanego tokena w TryFire.

Naprawione regresje (Core):
- CS7036 (brak argumentu CT dla OnEntry token-only) — OnInitialEntry oraz TryFire (async) przekazują `cancellationToken` przez helpery.
- CS0159/CS0103 (brak etykiety/`success`) i “fall through” w sync — sync używa teraz ścieżki “direct return” bez etykiet.
- Polityka wyjątków dla Action z `Continue` — wyjątek nie propaguje (handler Continue), stan pozostaje zmieniony zgodnie z oczekiwaniami.

Weryfikacja:
- Build `StateMachine.csproj` generuje pakiet NuGet. `StateMachine.Tests` kompilują się i przechodzą (poza 2 znanymi testami poza zakresem refaktoru).

Kolejne fazy:
 - Faza 3 (Payload): przeniesienie obsługi payload do Unified (single/multi payload, mapa typów), usunięcie delegacji do `PayloadVariantGenerator`.

Status Fazy 3 — Payload (ZAKOŃCZONA)

**Data**: 2025-08-15
**Status**: ✅ Zaimplementowane i przetestowane (97/99 testów przechodzi)

**Wykonane**:
- Zaimplementowano pełną obsługę payload w UnifiedStateMachineGenerator:
  - Wsparcie dla sync/async, single i multi-payload, flat i HSM, internal transitions
  - Zachowano politykę wyjątków (stan zmieniony przed Action; handler Continue/Propagate)
  - Przekazywanie CancellationToken w ścieżkach async przez Guard/Callback helpery
- Dodano walidację typu payload dla multi-payload (słownik trigger→typ generowany tylko gdy potrzebny)
- Wpięto dedykowane emitery przejść:
  - `WriteTransitionLogicPayloadAsync` dla async (z success var + END_TRY_FIRE)
  - `WriteTransitionLogicPayloadSyncDirect` dla sync (direct return, bez etykiet)
- Dodano brakujące metody API:
  - Fire methods z payloadem (sync/async, single/multi)
  - Typed TryFire wrappers dla payload
  - GetPermittedTriggersWithResolver dla payload-aware guard resolution
- UnifiedStateMachineGenerator obsługuje teraz bezpośrednio warianty:
  - Pure, Basic, WithPayload, WithExtensions (bez delegacji)
  - Tylko Full nadal deleguje do FullVariantGenerator (do Fazy 5)
- Zweryfikowano brak regresji — testy kompilują się i generowany kod zachowuje semantykę

**Kluczowe decyzje**:
- Zachowano compile-time feature flags — zero runtime branching
- Payload map (_payloadMap) generowana tylko dla multi-payload
- Fire/TryFire z typowanym payloadem tylko gdy HasPayload
- GetPermittedTriggersWithResolver tylko dla payload variants
- WriteTransitionLogic deleguje do payload-aware writers gdy HasPayload

**Szczegółowe zmiany w kodzie**:

1. **Dodane metody w UnifiedStateMachineGenerator.cs**:
   - `WriteFireMethods()` - generuje Fire methods z payloadem (sync/async, single/multi)
   - `WriteGetPermittedTriggersWithResolver()` - obsługa payload-aware guard resolution
   - Rozszerzone `WriteTryFireMethods()` o typed TryFire wrappers dla payload

2. **Poprawki błędów kompilacji**:
   - Naprawiono generowanie failure hook dla sync machines z Extensions
   - Usunięto niepoprawne odwołanie do `success` variable w sync TryFire wrapper
   - Zinline'owano failure hook body dla sync aby uniknąć problemów ze scope zmiennych

3. **Struktura kodu dla payload**:
   ```csharp
   // Dla async machines z payload:
   WriteTransitionLogicPayloadAsync() - używa success var + END_TRY_FIRE pattern
   
   // Dla sync machines z payload:  
   WriteTransitionLogicPayloadSyncDirect() - direct return, bez etykiet/goto
   ```

4. **Walidacja typów payload (multi-payload)**:
   - Generowany słownik `_payloadMap` tylko gdy HasMultiPayload
   - Walidacja typu w TryFireInternal przed procesowaniem przejścia
   - Type checking w CanFire dla multi-payload variants

5. **Integracja z helperami**:
   - `GuardGenerationHelper.EmitGuardCheck()` - obsługa payload + CT w async
   - `CallbackGenerationHelper.EmitOnEntryCall()` - payload-aware OnEntry
   - `CallbackGenerationHelper.EmitOnExitCall()` - payload-aware OnExit  
   - `CallbackGenerationHelper.EmitActionCall()` - payload-aware Action

6. **API surface (compile-time conditional)**:
   - `Fire(trigger, TPayload)` - tylko gdy HasPayload
   - `TryFire(trigger, TPayload)` - tylko gdy HasPayload
   - `CanFire(trigger, TPayload)` - tylko gdy HasPayload
   - `GetPermittedTriggers(payloadResolver)` - tylko gdy HasPayload

**Przykłady wygenerowanego kodu**:

Single payload variant (sync):
```csharp
public bool TryFire(OrderTrigger trigger, OrderData payload)
{
    EnsureStarted();
    return TryFireInternal(trigger, payload);
}

protected override bool TryFireInternal(OrderTrigger trigger, object? payload)
{
    switch (_currentState) {
        case OrderState.New:
            switch (trigger) {
                case OrderTrigger.Submit:
                    _currentState = OrderState.Submitted;
                    if (payload is OrderData typedPayload) {
                        ProcessSubmission(typedPayload);
                    }
                    return true;
            }
    }
    return false;
}
```

Multi-payload variant z walidacją typu:
```csharp
private static readonly Dictionary<MultiTrigger, Type> _payloadMap = new()
{
    { MultiTrigger.Configure, typeof(ConfigData) },
    { MultiTrigger.Process, typeof(string) },
    { MultiTrigger.Error, typeof(ErrorPayload) }
};

protected override bool TryFireInternal(MultiTrigger trigger, object? payload)
{
    // Walidacja typu payload
    if (_payloadMap.TryGetValue(trigger, out var expectedType) && 
        (payload == null || !expectedType.IsInstanceOfType(payload)))
    {
        return false; // wrong payload type
    }
    // ... logika przejść
}
```

Extensions z failure hook (sync):
```csharp
public override bool TryFire(TestTrigger trigger, object? payload = null)
{
    EnsureStarted();
    var originalState = _currentState;
    var result = TryFireInternal(trigger, payload);
    if (!result)
    {
        var failCtx = new StateMachineContext<TestState, TestTrigger>(
            Guid.NewGuid().ToString(),
            originalState,
            trigger,
            originalState,
            payload);
        _extensionRunner.RunAfterTransition(_extensions, failCtx, false);
    }
    return result;
}
```

Status Fazy 4 — Extensions (W TRAKCIE)

**Data**: 2025-08-15  
**Status**: ⚠️ Częściowo zaimplementowane (98/99 testów przechodzi)

**Wykonane**:
- Zaimplementowano pełną obsługę extensions w UnifiedStateMachineGenerator
- Dodano ExtensionsFeatureWriter do generowania pól i metod zarządzania
- Zaimplementowano hooki (BeforeTransition, AfterTransition) w logice przejść
- Dodano metody AddExtension/RemoveExtension
- Integracja z payload dla wariantu Full (delegacja nadal aktywna)

**Zidentyfikowane problemy**:
- Test `ActionThrow_DoesNotChangeState_TryFireFalse_FireThrows_ExtensionsNotified` nie przechodzi
- Polityka obsługi wyjątków dla WithExtensions wymaga, aby stan NIE był zmieniany gdy akcja rzuci wyjątek
- Implementacja w `WriteTransitionLogicSyncWithExtensions` jest poprawna, ale kod nie jest używany
- Potrzebna głębsza analiza ścieżki generowania kodu dla sync WithExtensions

**Kluczowe zmiany**:
1. Użycie `ExtensionsFeatureWriter` dla pól i konstruktora
2. Implementacja hooków w metodach przejść
3. Specjalna obsługa wyjątków dla WithExtensions (try-catch wokół całej logiki)
4. UnifiedStateMachineGenerator obsługuje teraz: Pure, Basic, WithPayload, WithExtensions

**Do dokończenia**:
- Naprawić politykę obsługi wyjątków dla sync WithExtensions
- Usunąć delegację do ExtensionsVariantGenerator gdy wszystkie testy przejdą
- Zintegrować Full variant (Faza 5)

Następne kroki

- Dokończyć Fazę 4: naprawić obsługę wyjątków w WithExtensions
- Faza 5 (Full): scalenie payload + extensions bez delegacji do FullVariantGenerator

Szczegóły implementacji — Faza 3 (Payload)

- Wykrywanie cech: sterowanie kompilacyjne oparte na Model.GenerationConfig i danych modelu.
  - HasPayload: `Model.GenerationConfig.HasPayload`.
  - Single vs Multi: `IsSinglePayloadVariant()` / `IsMultiPayloadVariant()` (wnioskowane z `Model.DefaultPayloadType` i `Model.TriggerPayloadTypes`).
  - HSM: `Model.HierarchyEnabled` (tablice `s_parent/s_depth/s_initialChild/s_history` + runtime helpers).

- Logika przejść (TryFire) dla payload:
  - Async: `WriteTransitionLogicPayloadAsync` (zachowuje wzorzec success + etykieta `END_TRY_FIRE`).
    - Kolejność: Guard (z payload) → OnExit → zmiana stanu → OnEntry (z polityką wyjątków) → Action (z polityką wyjątków).
    - Guard/Callbacki emitowane przez `GuardGenerationHelper`/`CallbackGenerationHelper` z prawidłowym `cancellationToken` i `ContinueOnCapturedContext`.
  - Sync: `WriteTransitionLogicPayloadSyncDirect` (direct return, bez etykiet/goto).
    - OnExit w sync opakowany lokalnym `try { ... } catch { return false; }` (bez `success`/`END_TRY_FIRE`).
    - OnEntry/Action emitowane poprzez `EmitOnEntryWithExceptionPolicyPayload(...)` i `EmitActionWithExceptionPolicyPayload(...)` (zachowana semantyka polityki wyjątków: stan zmieniony przed Action; handler może Continue/Propagate).
  - Selekcja emitera w `WriteTryFireMethod*`: jeśli HasPayload → użyj wariantu payload, inaczej bazowy.

- Multi‑payload:
  - Generowany słownik `private static readonly Dictionary<Trigger, Type> _payloadMap` tylko gdy Multi.
  - Walidacja typu payload wykonywana od razu na początku `TryFireInternal`/`TryFireInternalAsync`/`CanFireWithPayload*` — zwrot `false` i opcjonalny log z informacją o typie oczekiwanym/rzeczywistym.
  - Dobór przeciążeń callbacków i fallback (payload/token/bezparametrowe) realizowany przez `CallbackGenerationHelper` na podstawie sygnatur.

- Guards z payloadem:
  - `GuardGenerationHelper.EmitGuardCheck(...)` wywoływany z `payloadVar` i (w async) z `cancellationToken` oraz polityką `TreatCancellationAsFailure` zgodną z poprzednim generatorem.
  - Po niepowodzeniu guardów: logi GuardFailed/TransitionFailed + wyjście ze ścieżki przejścia zgodnie z trybem (direct return w sync; `success=false; goto END_TRY_FIRE;` w async).

- OnEntry przy starcie (Initial OnEntry):
  - `OnInitialEntry()` / `OnInitialEntryAsync(...)` wywołują wyłącznie przeciążenia bezparametrowe (`OnEntryHasParameterlessOverload == true`).
  - Jeśli stan ma tylko OnEntry z payloadem — nie jest wywoływany przy starcie (utrzymanie dotychczasowej semantyki). W async przekazywany jest `CancellationToken` przez helpery.

- HSM × Payload:
  - Zachowane `RecordHistoryForCurrentPath()` i `GetCompositeEntryTarget(...)` dla wejść do kompozytów oraz sekwencje wyjść/wejść w hierarchii.
  - Automatyczne zejście do stanu początkowego nie „wstrzykuje” payloadu; payload trafia do OnEntry tylko w ramach przejścia, zgodnie z dawną logiką.

- Internal transitions:
  - Brak zmiany stanu; akcja wykonana z payloadem jeśli oczekiwany; zachowane logi i raportowanie sukcesu.

- API powierzchnia:
  - Publiczne metody z `object? payload` utrzymane; dodatkowe typed overloads (np. CanFire(TPayload)) generowane tylko, gdy HasPayload (bez wpływu na maszyny bez payloadu).
  - Brak dodawania API dla maszyn bez payloadu (zero‑cost path).

- Logowanie i wydajność:
  - Logi jak dotychczas (GuardFailed/TransitionFailed/OnEntryExecuted/OnExitExecuted/ActionExecuted/TransitionSucceeded), sterowane flagą generowania logów.
  - Zero runtime flag: wszystkie ścieżki warunkowane w generatorze, a nie przez if‑y w kodzie run‑time. Ścieżka sync zachowuje direct return dla wydajności.

- Diagnostyka:
  - Wygenerowane pliki w `StateMachine.Tests/obj/GeneratedFiles/...` pozwalają szybko zweryfikować rzutowania payloadu, przekazywanie tokena i kolejność wywołań.
- Faza 4 (Extensions): integracja `ExtensionsFeatureWriter` w Unified i usunięcie delegacji do `ExtensionsVariantGenerator`.
- Faza 5–6: unifikacja Full i usunięcie starych klas wariantów.

Wskazówki do Fazy 3 — Payload (praktyczne)

- Zakres: przeniesienie logiki payload z `PayloadVariantGenerator` do Unified tak, aby decyzje pozostawały w compile‑time (zero runtime flag), a API i semantyka pozostały zgodne z dotychczasowymi testami.
- Flagi cech: używaj istniejących wzorców z bazy:
  - HasPayload: `Model.GenerationConfig.HasPayload`
  - Single vs Multi: `IsSinglePayloadVariant()` / `IsMultiPayloadVariant()` z bazy (wykorzystują `Model.TriggerPayloadTypes` i `Model.DefaultPayloadType`).
- Emisja wywołań: nie pisz ręcznie — użyj istniejących helperów payloadowych z bazy:
  - OnEntry: `EmitOnEntryWithExceptionPolicyPayload(...)`
  - Action: `EmitActionWithExceptionPolicyPayload(...)`
  - OnExit: w payload zwykle też przez helper (`EmitOnExitCall` obsługuje multi, raw‑object itp.).
  - Dzięki temu dostaniesz poprawny CT, wybór przeciążenia (payload/payload+token/token/parameterless) i politykę wyjątków bez duplikacji.
- Struktura TryFire:
  - Zostaw bazowy `WriteTryFireStructure(...)` (flat/HSM), tak jak w Fazie 2.
  - Podstaw `writeTransitionLogic` zależnie od wariantu (payload vs non‑payload; sync vs async). Dla payload przygotuj dedykowane: `WriteTransitionLogicPayloadSyncDirect` i `WriteTransitionLogicPayloadAsync` oparte na helperach payload.
- Kolejność kroków w przejściu (payload):
  - Guard (z payload, jeśli wymagany) — użyj `GuardGenerationHelper.EmitGuardCheck(...)` z `payloadVar` wskazującym na właściwą zmienną (w TryFireInternal przy payloadzie nie używaj "null").
  - OnExit (payload jeśli stan tego wymaga) — helper, bez polityki w sync, z polityką/try‑catch w async (jak w bazie).
  - Zmiana stanu → OnEntry (payload) — OnEntry musi być wywołane po zmianie stanu; użyj wariantu payload helpera (zapewnia CT i politykę wyjątków).
  - Action (payload) — wywołaj przez `EmitActionWithExceptionPolicyPayload(...)` (po zmianie stanu), aby handler mógł zwrócić Continue (wyjątek nie propaguje) lub Propagate (rzutuj wyjątek; stan pozostaje zmieniony w flat FSM).
- Walidacja typu payload (multi):
  - Wzorzec bazowy: jeśli guard/OnEntry/Action oczekuje payloadu, a `payload` nie daje się zrzutować do oczekiwanego typu, zwróć `false` (tak robi stary generator — zob. miejsca z komentarzem "wrong payload type").
  - Rozważ centralizację mapowania typów w pomocniku `PayloadGenerationHelper` (np. mapy trigger→type/enum index) — ale trzymaj się istniejącej semantyki: eliminuj koszt gdy HasPayload==false.
- CanFire/GetPermittedTriggers:
  - Bez payloadu wejściowego, jeśli guard wymaga payload — nie próbuj zgadywać, trzymaj się dotychczasowego zachowania: guardy payload‑only zwykle „failują” w CanFire bez payload (generator bazowy używa `payloadVar: "null"`).
  - Dla async wariantów CanFire/GetPermittedTriggers pamiętaj o CT i `ConfigureAwait` — wzór masz w Fazie 2.
- HSM × Payload:
  - Zostaw notatkę z `f07.md`: OnEntry z payload nie jest wywoływany przy automatycznym zejściu do initial child, chyba że tak było już w starym generatorze — nie zmieniaj semantyki.
  - Zachowaj `RecordHistoryForCurrentPath()` i obsługę wejścia do composite (initial/history) niezależnie od payload — payload dotyczy wywołań callbacków, nie wyboru ścieżki.
- Internal transitions:
  - Brak zmiany stanu. Jeżeli Action/Guard oczekuje payload — przekaż payload (z typowaniem); w przypadku type‑mismatch zwróć `false` (parytet z dotychczasowym kodem).
- API publiczne:
  - Nie poszerzaj API dla maszyn bez payload (zachowaj „zero cost”): jeżeli HasPayload==false, nie generuj przeciążeń z payloadem.
  - Dla payload pozostań przy obecnym kontrakcie (zgodnie z testami), w razie potrzeby dopisz metody w `I<ClassName>` (już generowany) zamiast zmieniać bazowe interfejsy.
- Logowanie i hooki:
  - Zachowaj logi sukcesów/por. z bazowego `WriteTransitionLogic` oraz hooki (Before/After, GuardEvaluation).
  - Zachowaj deterministykę (kolejność stanów/triggerów) — używaj tych samych grupowań i sortowań co baza.
- Wydajność i minimalizm kodu:
  - Nie wprowadzaj runtime `if(HasPayload)` — decyzje w compile‑time.
  - Emisja tylko potrzebnych fragmentów (np. mapy typów dla multi, nie dla single).
- Strategia wdrożenia:
  - Najpierw Single Payload w Unified (sync/async), potem Multi Payload, na końcu payload×HSM.
  - Po każdym kroku: build `StateMachine.csproj`, testy `StateMachine.Tests` (pakietowe). Zacznij od grup testów PayloadVariant.
  - Po stabilizacji: usuń delegację do `PayloadVariantGenerator` i dopiero wtedy rozważ usunięcie klasy.

Praktyczne porady (dev setup + typowe pułapki)

- Budowanie i testy: zbuduj `StateMachine/StateMachine.csproj` (nie rozwiązanie). To generuje paczkę w `./nupkgs`, a testy (`StateMachine.Tests`) pobierają ją z lokalnego feedu (`nuget.config` ma `LocalPackages`). Nie dodawaj referencji projektowych w testach — mają korzystać z paczki. Część projektów w `.sln` (np. `Experiments*`) może nie istnieć — ignoruj i buduj per‑projekt.
- Zakres testów: testuj tylko `StateMachine.Tests` (na tym etapie). Dwa testy są znane jako czerwone i poza zakresem tej refaktoryzacji — nie próbuj ich naprawiać teraz.
- Async vs Sync w TryFire: async używa zmiennej `success` i etykiety `END_TRY_FIRE` (skokowe wyjście), sync musi zwracać bezpośrednio (żadnych goto/etykiet). Wstawienie etykiety w sync skutkuje CS0103/CS0159.
- CancellationToken: dla async OnEntry/OnExit/Action i guardów przekazuj token przez odpowiednie helpery:
  - OnEntry/OnExit/Action: `CallbackGenerationHelper.Emit*` (podaj `cancellationToken` dla async); nie wywołuj metod „ręcznie”.
  - Guard: `GuardGenerationHelper.EmitGuardCheck` — nazwany parametr to `isAsync`, a nie `callerIsAsync`; przekaż `cancellationTokenVar: "cancellationToken"` gdy jesteś w async.
- Kolejność wywołań: OnExit → zmiana stanu → OnEntry → Action. Polityka wyjątków dla Action zakłada, że stan jest już zmieniony (Continue połyka wyjątek i pozostawia nowy stan; Propagate rzuca, ale stan pozostaje zmieniony w flat FSM).
- HSM inicjalizacja: generuj `Start/StartAsync` tylko gdy HSM lub OnEntry istnieją; zawsze `DescendToInitialIfComposite()` przed wywołaniami `OnInitialEntry/OnInitialEntryAsync`. Pamiętaj o tablicach `s_parent/s_initialChild/s_history` i polu instancyjnym `_lastActiveChild` dla HSM.
- Nazwy i typy: używaj `TypeHelper.EscapeIdentifier(...)` dla nazw enumów i triggerów (np. `_1Start`, `_2Next`), by uniknąć kolizji z identyfikatorami. Dla async interfejsu/klasy bazowej korzystaj z `AsyncGenerationHelper.GetBaseClassName/GetInterfaceName`.
- Diagnostyka: w razie problemów porównuj wygenerowany kod w `StateMachine.Tests/obj/GeneratedFiles/...`. Wzorce takie jak `try { OnEntry... } catch { return false; }` sygnalizują ręczną emisję — zastąp je helperami. Szukaj też błędnych etykiet w sync i braków CT w async.

Następne kroki (Faza 2):
- Zweryfikować spójność ścieżek sync/async w TryFire (logowanie, hooki wyjątków, AfterTransition) i uzupełnić ewentualne braki; w razie potrzeby dostosować do bazowej `WriteTransitionLogic`.
- Sprawdzić OnInitialEntry/OnInitialEntryAsync dla HSM: wywołania OnEntry wyłącznie jeśli `HasOnEntryExit` oraz w kolejności root→leaf.
- Wykonać smoke-test (build/tests) dla wariantów Pure i Basic (płasko oraz HSM).

Notatki:
- Bazowa `WriteTransitionLogic` emituje hooki (Before/After, GuardEvaluation), logowanie sukcesów/niepowodzeń, obsługuje HSM (record history, composite entry) oraz polityki wyjątków — Unified jest z nią w parytecie.

Uwaga:
- Zmiana eliminuje ryzyko regresji w HSM przez wykorzystanie jednej, stabilnej implementacji bazowej, zamiast lokalnej kopii w Unified.


Plan Faza 6: Usunięcie starej hierarchii wariantów

Cel: pozostawić jeden generator (Unified) i cechy sterowane flagami w modelu; pozbyć się klas wariantów i złożonej selekcji.

Zakres prac:
- Usunąć pliki: `CoreVariantGenerator.cs`, `PayloadVariantGenerator.cs`, `ExtensionsVariantGenerator.cs`, `FullVariantGenerator.cs` (po potwierdzeniu braku referencji produkcyjnych).
- Uprościć/wyłączyć `VariantSelector`: pozostawić jedynie ustawianie flag w modelu (lub całkowicie usunąć, jeśli parser i tak je ustawia, a selector nie ma innych konsumentów).
- Zweryfikować `Generator.cs` — już korzysta z `UnifiedStateMachineGenerator`; upewnić się, że nie istnieją ścieżki kodu delegujące do starych klas.
- Przejrzeć projekty testowe pod kątem bezpośrednich odniesień do starych klas generatorów.
- Build i pełny test suite + szybka kontrola wygenerowanych plików (snapshot) dla: pure/basic/payload/extensions/full, także HSM.

Dodatkowe porządki:
- Usunąć martwy kod pomocniczy związany wyłącznie z dawną hierarchią (jeśli występuje).
- Zaktualizować dokumentację architektury (krótka notka: „jeden generator + feature flags”).

Ryzyko i rollback:
- W razie problemów łatwy rollback (przywrócenie usuniętych plików). Użycie Unified pozostaje bez zmian.
- Testy jednostkowe i porównanie snapshotów wygenerowanych plików zapewniają siatkę bezpieczeństwa.
