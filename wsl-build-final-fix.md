# WSL Build - Final Fix Documentation

## Problem rozwiązany
Błąd `NU1301: Invalid URI: The hostname could not be parsed` wynikał z używania ścieżek UNC (`\\wsl$\...`) w operacjach NuGet.

## Rozwiązanie - 3 kluczowe zmiany

### 1. **pushd dla konwersji UNC → litera dysku**
Skrypty automatycznie konwertują ścieżki WSL na literę dysku:
```powershell
if ($PWD.Path -like '\\wsl*') {
    pushd "\\wsl.localhost\Ubuntu-24.04\home\lukasz\FastFsm"
    # Teraz mamy np. Z:\ zamiast \\wsl$\
}
```

### 2. **Rozdzielenie restore i pack**
- Najpierw: `dotnet restore --configfile` na całej solucji
- Potem: `dotnet pack --no-restore` (nie wyzwala własnego restore)

### 3. **Bezpieczne parsowanie JSON**
Zamiast regex używamy `ConvertFrom-Json` i `ConvertTo-Json`:
```powershell
$ver = Get-Content -Raw version.json | ConvertFrom-Json
$ver.buildNumber = [int]$ver.buildNumber + 1
$ver | ConvertTo-Json | Set-Content version.json
```

## Skrypty - jak używać

### build-and-test.ps1
Główny skrypt do pełnego buildu:
```powershell
# Z dowolnego miejsca (nawet \\wsl$\...)
.\build-and-test.ps1

# Opcje:
.\build-and-test.ps1 -SkipTests        # Bez testów
.\build-and-test.ps1 -Configuration Debug
.\build-and-test.ps1 -Clean            # Wyczyść przed buildem
.\build-and-test.ps1 -NoIncrement      # Nie zwiększaj wersji
```

### build-for-vs.ps1
Szybki build dla Visual Studio 2022:
```powershell
# Domyślnie
.\build-for-vs.ps1

# Quick mode (bez testów)
.\build-for-vs.ps1 -QuickMode

# Custom paths
.\build-for-vs.ps1 `
    -WslRoot "\\wsl.localhost\Ubuntu-24.04\home\user\FastFsm" `
    -WinFeed "C:\MyPackages"
```

## Jak to działa

### Przepływ build-and-test.ps1:
1. **Wykrycie WSL** → `pushd` zamienia `\\wsl$\` na literę (np. `Z:\`)
2. **Wersjonowanie** → JSON parsing, increment, zapis
3. **Temp config** → `.nuget.windows.config` z literą dysku
4. **Restore** → `dotnet restore --configfile` na solucji
5. **Pack** → `dotnet pack --no-restore` do `Z:\nuget`
6. **Update testów** → Podmiana wersji w .csproj
7. **Final restore** → Z nowymi pakietami
8. **Build & Test** → Wszystko z `--configfile`
9. **Cleanup** → `popd` przywraca oryginalną ścieżkę

### Przepływ build-for-vs.ps1:
1. **Windows feed** → `%LOCALAPPDATA%\FastFsm\nuget`
2. **Temp config** → Z Windows feed path
3. **Restore** → Na całej solucji z configiem
4. **Pack** → Bezpośrednio do Windows feed
5. **Mirror** → Kopia do WSL dla spójności
6. **Update & Test** → Jak wyżej

## Kluczowe zasady

### ✅ ZAWSZE:
- `pushd` przed używaniem ścieżek WSL
- `--configfile` z tymczasowym configiem
- `--no-restore` przy `dotnet pack`
- JSON cmdlets zamiast regex
- Windows paths w NuGet config

### ❌ NIGDY:
- Ścieżki `\\wsl$\` w NuGet config
- `dotnet pack` bez wcześniejszego restore
- Regex do parsowania JSON
- Stałe mapowania dysków

## Troubleshooting

### "Failed to verify the root directory"
**Przyczyna:** Używasz ścieżki UNC w NuGet
**Fix:** Uruchom skrypt który używa pushd

### "Package not found" 
**Przyczyna:** Cache lub stara wersja
**Fix:** 
```powershell
dotnet nuget locals all --clear
.\build-for-vs.ps1
```

### "Access denied"
**Przyczyna:** Brak uprawnień do WSL
**Fix:** Uruchom PowerShell jako Administrator

## Zmienne środowiskowe

Skrypty ustawiają:
- `NUGET_PACKAGES` → Windows cache location
- `FASTFSM_LOCAL_FEED` → Windows feed (tylko build-for-vs.ps1)

## Pliki tymczasowe

Tworzone automatycznie:
- `.nuget.windows.config` → W repo root (build-and-test.ps1)
- `fastfsm.nuget.windows.config` → W %TEMP% (build-for-vs.ps1)

Nie commituj tych plików do git!

## Testowane środowisko
- Windows 11 + WSL2 (Ubuntu 24.04)
- Visual Studio 2022
- .NET 9.0
- PowerShell 7.x

## Podsumowanie
Problem z `\\wsl$\` został całkowicie wyeliminowany przez:
1. Konwersję na literę dysku (`pushd`)
2. Tymczasowe configi NuGet
3. Rozdzielenie restore/pack
4. Bezpieczne parsowanie JSON

Skrypty są teraz w pełni kompatybilne z workflow WSL + VS2022.