# WSL Build Fix - Rozwiązanie problemu ścieżek NuGet

## Problem
NuGet nie potrafi poprawnie interpretować ścieżek WSL (`\\wsl$\...`) podczas restore/pack operacji w Visual Studio 2022 na Windows.

## Rozwiązanie
Zastosowano dwa mechanizmy:
1. **Zmienna środowiskowa `FASTFSM_LOCAL_FEED`** - konsumowana przez `Directory.Build.props`
2. **Tymczasowy config NuGet** z Windowsowymi ścieżkami

## Wprowadzone zmiany

### 1. Directory.Build.props
Dodano obsługę zmiennej środowiskowej dla lokalnego feedu:
```xml
<PropertyGroup>
  <FastFsmLocalFeed>$(FASTFSM_LOCAL_FEED)</FastFsmLocalFeed>
</PropertyGroup>

<PropertyGroup Condition=" '$(FastFsmLocalFeed)' != '' ">
  <RestoreAdditionalProjectSources>
    $(RestoreAdditionalProjectSources);$(FastFsmLocalFeed)
  </RestoreAdditionalProjectSources>
</PropertyGroup>
```

### 2. nuget.config
Uproszczono do tylko nuget.org (lokalny feed podawany dynamicznie):
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### 3. build-for-vs.ps1
Kompletnie przepisany do obsługi WSL:
- Pakuje do WSL (`\\wsl.localhost\...\nuget`)
- Kopiuje paczki do Windows (`%LOCALAPPDATA%\FastFsm\nuget`)
- Ustawia zmienną `FASTFSM_LOCAL_FEED`
- Tworzy tymczasowy config NuGet
- Używa `--configfile` dla restore/build/test

### 4. build-and-test.ps1
Dodano obsługę WSL przez `pushd`:
- Wykrywa ścieżki `\\wsl*`
- Używa `pushd` do mapowania na literę dysku
- Generuje lokalny `.nuget.windows.config`
- Używa `--configfile` dla operacji NuGet
- `popd` w finally block

## Użycie

### Z Visual Studio 2022 (kod na WSL)
```powershell
# W Package Manager Console lub PowerShell:
.\build-for-vs.ps1

# Lub pełny build z testami:
.\build-and-test.ps1
```

### Parametry build-for-vs.ps1
```powershell
.\build-for-vs.ps1 `
    -WslRoot "\\wsl.localhost\Ubuntu-24.04\home\lukasz\FastFsm" `
    -WinFeed "$env:LOCALAPPDATA\FastFsm\nuget" `
    -QuickMode  # Pomija testy
```

## Jak to działa

1. **Pakowanie** odbywa się w WSL (`\\wsl.localhost\...\nuget`)
2. **Kopiowanie** paczek do Windows feedu (`%LOCALAPPDATA%\FastFsm\nuget`)
3. **Zmienna środowiskowa** `FASTFSM_LOCAL_FEED` wskazuje na Windows feed
4. **Tymczasowy config** NuGet używa tylko ścieżek Windows
5. **Restore/Build/Test** używają `--configfile` z tym configiem

## Zalety
- ✅ Brak błędów NU1301 (Invalid URI)
- ✅ Działa z VS2022 na Windows
- ✅ Kod pozostaje na WSL
- ✅ Nie wymaga stałego mapowania dysków
- ✅ Automatyczne zarządzanie ścieżkami

## Testowanie
```powershell
# Test 1: Quick build
.\build-for-vs.ps1 -QuickMode

# Test 2: Full build z testami
.\build-for-vs.ps1

# Test 3: Build-and-test z WSL path
cd \\wsl.localhost\Ubuntu-24.04\home\lukasz\FastFsm
.\build-and-test.ps1
```

## Troubleshooting

### Problem: "Failed to verify the root directory"
**Rozwiązanie:** Upewnij się, że używasz `build-for-vs.ps1` zamiast bezpośrednio `dotnet restore`

### Problem: "Package not found"
**Rozwiązanie:** 
1. Sprawdź czy istnieje `%LOCALAPPDATA%\FastFsm\nuget`
2. Wyczyść cache: `dotnet nuget locals all --clear`
3. Uruchom ponownie `.\build-for-vs.ps1`

### Problem: "Access denied"
**Rozwiązanie:** Uruchom PowerShell jako Administrator lub sprawdź uprawnienia do WSL