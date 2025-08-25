# FastFSM Dual Environment Build Issue

## Zadanie
Stworzenie systemu budowania dla projektu FastFSM, który:
- Ma działać w Visual Studio 2022 na Windows
- Kod źródłowy leży na dysku WSL (Windows Subsystem for Linux)
- Buduje paczki NuGet lokalnie
- Projekty testowe używają lokalnych paczek NuGet zamiast referencji projektowych
- Obsługuje automatyczne wersjonowanie

## Wykonane kroki

### 1. System wersjonowania
Utworzono `version.json` do zarządzania wersjami:
```json
{
  "version": "0.8.0",
  "suffix": "dev",
  "buildNumber": 2,
  "autoIncrement": true
}
```

### 2. Skrypty budowania
Stworzono następujące skrypty:
- `build-and-test.ps1` - główny skrypt PowerShell dla Windows/VS2022
- `build-and-test.sh` - równoważny skrypt Bash dla Linux
- `build-for-vs.ps1` - szybki build dla Visual Studio
- `quick-build.sh` - minimalny skrypt dla Linux

### 3. Konfiguracja NuGet
Utworzono/zaktualizowano:
- `nuget.config` - konfiguracja źródeł pakietów
- `Directory.Build.props` - wspólne właściwości MSBuild
- Plik `.props` w paczce NuGet dla propagacji global usings

### 4. Propagacja Global Usings
Dodano do `FastFsm/build/FastFsm.Net.props`:
```xml
<ItemGroup Condition="'$(FASTFSM_NoGlobalUsings)' != 'true'">
  <Using Include="Abstractions.Attributes" />
</ItemGroup>
```

## Napotkany problem

### Opis błędu
Przy uruchamianiu skryptu PowerShell z Windows na kodzie w WSL pojawia się błąd związany ze ścieżkami UNC:

```
Build succeeded in 3.9s
✓ Created FastFsm.Net.0.8.0.2-dev.nupkg
Building FastFsm.Net.Logging package...
    \\wsl$\Ubuntu-24.04\home\lukasz\FastFsm\FastFsm.Logging\FastFsm.Logging.csproj : error NU1301:
      Failed to verify the root directory of local source '\\wsl$\Ubuntu-24.04\home\lukasz\FastFsm\nuget'.
        Invalid URI: The hostname could not be parsed.

Restore failed with 1 error(s) in 0.6s
```

### Analiza problemu

1. **Przyczyna**: NuGet nie potrafi poprawnie interpretować ścieżek WSL w formacie `\\wsl$\...`
2. **Kontekst**: Problem występuje gdy:
   - Visual Studio 2022 jest uruchomione na Windows
   - Projekt znajduje się w systemie plików WSL
   - Używamy lokalnych pakietów NuGet ze ścieżką względną (`./nuget`)

3. **Miejsca wystąpienia błędu**:
   - Budowanie pakietu `FastFsm.Net.Logging`
   - Przywracanie pakietów (restore)
   - Wszystkie operacje związane z lokalnym źródłem NuGet

### Wpływ na workflow

- Pierwszy pakiet (`FastFsm.Net`) buduje się poprawnie
- Kolejne pakiety i operacje restore kończą się błędem
- Nie można używać lokalnych pakietów NuGet w projektach testowych

## Potencjalne rozwiązania

### 1. Mapowanie dysku WSL
```powershell
# Mapowanie WSL jako dysku sieciowego
net use W: \\wsl$\Ubuntu-24.04 /persistent:yes
# Używanie ścieżek W:\home\lukasz\FastFsm zamiast \\wsl$\...
```

### 2. Użycie ścieżek absolutnych Windows
Konwersja ścieżek WSL na format Windows:
```powershell
$wslPath = "\\wsl.localhost\Ubuntu-24.04\home\lukasz\FastFsm"
# lub użycie wsl.exe do konwersji
$winPath = wsl wslpath -w /home/lukasz/FastFsm
```

### 3. Modyfikacja nuget.config
Użycie pełnej ścieżki UNC lub mapowanego dysku:
```xml
<packageSources>
  <add key="LocalFastFsm" value="W:\home\lukasz\FastFsm\nuget" />
</packageSources>
```

### 4. Praca całkowicie w WSL
- Używanie VS Code z WSL extension
- Budowanie tylko przez skrypty bash w WSL
- Visual Studio 2022 tylko do debugowania

### 5. Hybrydowe podejście
- Budowanie pakietów w WSL (bash)
- Kopiowanie pakietów do lokalizacji Windows
- Visual Studio używa pakietów z lokalizacji Windows

## Rekomendowane rozwiązanie

### Skrypt adaptacyjny dla środowiska dual-build

Utworzenie skryptu PowerShell, który:
1. Wykrywa czy działa w kontekście WSL
2. Automatycznie mapuje dysk WSL jeśli potrzebne
3. Konwertuje ścieżki między formatami
4. Używa odpowiednich ścieżek dla NuGet

```powershell
# Przykład detekcji i konwersji
if ($PWD.Path -like "\\wsl$*" -or $PWD.Path -like "\\wsl.localhost*") {
    # Mapuj dysk jeśli nie istnieje
    if (-not (Test-Path "W:")) {
        net use W: \\wsl$\Ubuntu-24.04 /persistent:yes
    }
    # Konwertuj ścieżki
    $ProjectRoot = $PWD.Path -replace '\\\\wsl\$\\Ubuntu-24.04', 'W:'
}
```

## Wymagane działania

1. **Utworzenie skryptu mapowania dysków**
2. **Modyfikacja build-and-test.ps1** do obsługi ścieżek WSL
3. **Alternatywny nuget.config** dla środowiska Windows
4. **Dokumentacja procesu** dla innych developerów
5. **Testy w obu środowiskach**

## Notatki dodatkowe

- Problem nie występuje przy budowaniu bezpośrednio w WSL
- Visual Studio 2022 ma ograniczone wsparcie dla projektów w WSL
- Rozważyć migrację do VS Code z WSL extension dla lepszej integracji