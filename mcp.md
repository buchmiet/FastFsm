Sugestie funkcjonalności:

  1. Analiza zależności metod

  mcp__roslyn-index__get_method_dependencies
  - Które metody wywołuje dana metoda
  - Które pola/właściwości używa
  - To by bardzo pomogło przy refaktoryzacji - wiedziałbym od razu, co muszę przenieść razem

  2. Znajdowanie nieużywanych członków

  mcp__roslyn-index__find_unused_members
  - Metody/pola/właściwości bez referencji
  - Przydatne przy czyszczeniu kodu

  3. Analiza wpływu zmian (Impact Analysis)

  mcp__roslyn-index__what_breaks_if_i_change
  - Co się zepsuje, jeśli zmienię sygnaturę metody
  - Kto dziedziczy z tej klasy
  - Kto implementuje ten interfejs

  4. Porównanie klas

  mcp__roslyn-index__compare_classes
  - Różnice między dwiema klasami
  - Wspólne metody/różne sygnatury
  - Idealne do scalania klas jak w naszym przypadku

  5. Batch refactoring

  mcp__roslyn-index__rename_symbol_everywhere
  mcp__roslyn-index__change_method_signature
  - Zmiana nazwy symbolu we wszystkich miejscach
  - Zmiana sygnatury metody z automatyczną aktualizacją wywołań

  6. Analiza "code smells"

  mcp__roslyn-index__find_code_smells
  - Zbyt długie metody
  - Cykliczne zależności
  - Naruszenia DRY (duplikaty kodu)

  7. Generowanie kodu

  mcp__roslyn-index__generate_interface_from_class
  mcp__roslyn-index__generate_builder_pattern
  - Wyciągnięcie interfejsu z klasy
  - Generowanie wzorców (Builder, Factory, etc.)

  8. Nawigacja po hierarchii

  mcp__roslyn-index__get_inheritance_tree
  mcp__roslyn-index__get_all_implementations
  - Pełne drzewo dziedziczenia
  - Wszystkie implementacje interfejsu

  9. Analiza async/await

  mcp__roslyn-index__find_sync_over_async
  mcp__roslyn-index__find_missing_await
  - Znajdowanie miejsc gdzie brakuje await
  - Wykrywanie blokującego kodu (.Result, .Wait())

  10. Eksport diagramów

  mcp__roslyn-index__export_class_diagram
  mcp__roslyn-index__export_dependency_graph
  - Generowanie diagramów UML
  - Grafy zależności między klasami/namespace'ami

  Najbardziej brakujące teraz:

  Podczas naszej pracy najbardziej przydałyby się:
  1. Analiza zależności metod - żeby wiedzieć, co mogę bezpiecznie przenieść
  2. Porównanie klas - do identyfikacji różnic między StateMachineCodeGenerator i
  UnifiedStateMachineGenerator
  3. Impact analysis - co się zepsuje po przeniesieniu metody
