# Zadanie: Ujednolicenie nazewnictwa katalogów na FastFsm

## Kontekst
Projekt FastFsm ma niespójne nazewnictwo katalogów - część używa "FastFSM" (wielkie litery), a część "FastFsm" (małe litery). Wszystkie pliki .csproj zostały już zaktualizowane aby używać spójnego nazewnictwa "FastFsm" w AssemblyName, RootNamespace i PackageId.

## Zadanie do wykonania

Zmień nazwy wszystkich katalogów z "FastFSM" na "FastFsm" używając git mv:

```bash
# 1. Najpierw upewnij się, że nie ma żadnych procesów build
dotnet build-server shutdown

# 2. Zmień nazwy katalogów używając git mv
git mv FastFSM.Generator FastFsm.Generator
git mv FastFSM.Generator.DependencyInjection FastFsm.Generator.DependencyInjection  
git mv FastFSM.Generator.Logger FastFsm.Generator.Logger
git mv FastFSM.Generator.Model FastFsm.Generator.Model
git mv FastFSM.Generator.Rules FastFsm.Generator.Rules
git mv FastFSM.Generator.Tests FastFsm.Generator.Tests
git mv FastFSM.IndentedStringBuilder FastFsm.IndentedStringBuilder
git mv FastFSM.Runtime FastFsm.Runtime
git mv FastFSM.Attributes FastFsm.Attributes

# 3. Zaktualizuj referencje w plikach .csproj które mogły zostać pominięte
find . -name "*.csproj" -exec sed -i 's/\\FastFSM\./\\FastFsm./g' {} \;

# 4. Wyczyść i zbuduj całe rozwiązanie aby sprawdzić czy wszystko działa
dotnet clean
dotnet build

# 5. Uruchom testy aby potwierdzić że generator działa
dotnet test FastFsm.Tests/FastFsm.Tests.csproj

# 6. Jeśli wszystko działa, zatwierdź zmiany
git add -A
git status
```

## Oczekiwany rezultat
- Wszystkie katalogi powinny używać konwencji "FastFsm" (małe litery fsm)
- Projekt powinien się kompilować bez błędów
- Generator powinien poprawnie generować kod dla testów
- Testy powinny przechodzić

## Uwagi
- Operacja git mv zachowa historię plików
- Po wykonaniu tych kroków nazewnictwo będzie w pełni spójne
- Jeśli wystąpią problemy z uprawnieniami, może być konieczne użycie sudo