# FastFSM Portable Build System - Architecture Diagrams

## System Overview

```mermaid
graph TB
    subgraph "Entry Points"
        A[Developer] --> B{Location?}
        B -->|WSL Path| C[build.ps1<br/>detects WSL]
        B -->|Windows Path| D[build.ps1<br/>detects Windows]
        B -->|Pure WSL| E[build-and-test.sh]
    end
    
    subgraph "Execution Strategy"
        C --> F[Delegate to WSL<br/>via wsl.exe]
        D --> G[Build Locally<br/>on Windows]
        E --> H[Build in WSL]
        F --> H
    end
    
    subgraph "Package Output"
        H --> I[Write to /mnt/c/.../]
        G --> J[Write to C:\...\]
        I --> K[Local Feed<br/>C:\Users\...\AppData\Local\FastFsm\nuget]
        J --> K
    end
    
    subgraph "Consumption"
        K --> L[Visual Studio 2022]
        K --> M[dotnet CLI]
        K --> N[Test Projects]
    end
```

## Path Detection Flow

```mermaid
flowchart TD
    A[build.ps1 starts] --> B[Get current path<br/>$PWD.Path]
    B --> C{Contains<br/>\\wsl or<br/>wsl.localhost?}
    
    C -->|Yes| D[Parse WSL components]
    D --> E[Extract distro name]
    E --> F[Convert to Unix path]
    F --> G[Build WSL command]
    G --> H[Execute via wsl.exe]
    
    C -->|No| I[Use Windows strategy]
    I --> J[Create temp nuget.config]
    J --> K[Add local feed]
    K --> L[Run dotnet directly]
    
    H --> M[Success]
    L --> M
```

## Data Flow Architecture

```mermaid
graph LR
    subgraph "WSL Environment"
        A1[Source Code<br/>/home/user/FastFsm]
        A2[build-and-test.sh]
        A3[dotnet pack]
        A1 --> A2
        A2 --> A3
    end
    
    subgraph "Bridge"
        A3 --> B1[/mnt/c mount]
        B1 --> B2[File System<br/>Translation]
    end
    
    subgraph "Windows Environment"
        B2 --> C1[Local Feed<br/>C:\Users\...\AppData\Local\FastFsm\nuget]
        C1 --> C2[Visual Studio 2022]
        C1 --> C3[Windows dotnet CLI]
        C1 --> C4[Package Manager]
    end
```

## Configuration Hierarchy

```mermaid
graph TD
    A[Repository Root] --> B[nuget.config<br/>minimal - only nuget.org]
    A --> C[Directory.Build.props<br/>supports FASTFSM_LOCAL_FEED]
    A --> D[version.json<br/>version management]
    
    B --> E[Temporary nuget.config<br/>created at build time]
    C --> E
    E --> F[Includes local feed<br/>C:\Users\...\AppData\Local\FastFsm\nuget]
    
    F --> G[dotnet restore<br/>--configfile temp.config]
    F --> H[dotnet build<br/>--configfile temp.config]
    F --> I[dotnet pack<br/>--output local-feed]
```

## Error Prevention Strategy

```mermaid
flowchart TD
    A[UNC Path<br/>\\wsl.localhost\...] --> B{build.ps1<br/>Detection}
    
    B -->|Detected| C[Never passed to<br/>NuGet/dotnet]
    C --> D[Delegate to WSL]
    D --> E[WSL uses<br/>native paths]
    E --> F[Output via<br/>/mnt/c]
    
    B -->|Not detected| G[Windows path]
    G --> H[Direct execution]
    
    F --> I[No NU1301 Error]
    H --> I
    
    style A fill:#ffcccc
    style I fill:#ccffcc
```

## Script Interaction Model

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant PS as build.ps1
    participant WSL as wsl.exe
    participant SH as build-and-test.sh
    participant DN as dotnet
    participant LF as Local Feed
    
    Dev->>PS: .\build.ps1
    PS->>PS: Detect current path
    
    alt WSL Path Detected
        PS->>WSL: Execute build-and-test.sh
        WSL->>SH: Run in Linux environment
        SH->>DN: dotnet build/test/pack
        DN->>LF: Write packages via /mnt/c
    else Windows Path
        PS->>PS: Create temp config
        PS->>DN: dotnet restore/build/pack
        DN->>LF: Write packages directly
    end
    
    LF-->>Dev: Packages available
```

## Component Responsibilities

```mermaid
graph TB
    subgraph "build.ps1"
        A1[Path Detection]
        A2[Strategy Selection]
        A3[WSL Delegation]
        A4[Windows Execution]
        A5[Temp Config Management]
    end
    
    subgraph "build-and-test.sh"
        B1[Unix Environment]
        B2[Version Management]
        B3[Package Creation]
        B4[Test Execution]
        B5[Output to Windows]
    end
    
    subgraph "sync-wsl-to-windows.ps1"
        C1[Optional Tool]
        C2[rsync Integration]
        C3[Watch Mode]
        C4[Windows Copy]
    end
    
    subgraph "Shared Components"
        D1[Local Feed]
        D2[version.json]
        D3[Directory.Build.props]
        D4[nuget.config]
    end
    
    A1 --> A2
    A2 --> A3
    A2 --> A4
    A3 --> B1
    A4 --> A5
    B1 --> B3
    B3 --> B5
    B5 --> D1
    A5 --> D1
```

## Deployment Scenarios

```mermaid
graph LR
    subgraph "Development"
        A1[WSL Clone] --> B1[build.ps1]
        A2[Windows Clone] --> B1
        A3[VS2022 Open] --> B1
    end
    
    subgraph "CI/CD"
        C1[GitHub Actions<br/>Windows] --> D1[build.ps1]
        C2[GitHub Actions<br/>Linux] --> D2[build-and-test.sh]
        C3[Azure DevOps] --> D1
    end
    
    subgraph "Output"
        B1 --> E1[Local Feed]
        D1 --> E2[Artifacts]
        D2 --> E2
    end
```

## File System Mapping

```
WSL View                          Windows View
────────                          ────────────
/home/user/FastFsm/              \\wsl.localhost\Ubuntu\home\user\FastFsm\
    ├── build-and-test.sh            ├── build-and-test.sh
    ├── build.ps1                    ├── build.ps1
    └── src/                         └── src/
        
/mnt/c/Users/user/               C:\Users\user\
    └── AppData/Local/               └── AppData\Local\
        └── FastFsm/                     └── FastFsm\
            └── nuget/                       └── nuget\
                ├── pkg1.nupkg                   ├── pkg1.nupkg
                └── pkg2.nupkg                   └── pkg2.nupkg

Key: WSL writes to /mnt/c → Windows sees in C:\
     No UNC paths involved in the process
```

## State Machine: Build Process

```mermaid
stateDiagram-v2
    [*] --> Start
    Start --> DetectPath
    
    DetectPath --> WSLPath: Path contains \\wsl
    DetectPath --> WinPath: Normal Windows path
    
    WSLPath --> ExtractComponents
    ExtractComponents --> BuildWSLCommand
    BuildWSLCommand --> ExecuteInWSL
    
    WinPath --> CreateTempConfig
    CreateTempConfig --> RestorePackages
    RestorePackages --> BuildSolution
    
    ExecuteInWSL --> PackToWindows
    BuildSolution --> PackLocally
    
    PackToWindows --> LocalFeed
    PackLocally --> LocalFeed
    
    LocalFeed --> Success
    Success --> [*]
```

## Package Resolution Flow

```mermaid
flowchart TD
    A[Project needs FastFsm package] --> B{Where to look?}
    
    B --> C[Check Directory.Build.props]
    C --> D{FASTFSM_LOCAL_FEED set?}
    
    D -->|Yes| E[Add to RestoreAdditionalProjectSources]
    D -->|No| F[Skip local feed]
    
    E --> G[Check temp nuget.config]
    F --> G
    
    G --> H[Combine all sources]
    H --> I[1. Local Feed<br/>2. nuget.org]
    
    I --> J{Package found?}
    J -->|In Local| K[Use local package]
    J -->|In nuget.org| L[Download from nuget.org]
    J -->|Not found| M[NU1101 Error]
    
    K --> N[Success]
    L --> N
```

## Summary

These diagrams illustrate the complete architecture of the FastFSM portable build system, showing:

1. **Entry point detection** and strategy selection
2. **Path translation** avoiding UNC issues
3. **Data flow** from source to packages
4. **Configuration hierarchy** and temporary configs
5. **Error prevention** through architectural design
6. **Component interactions** via sequence diagrams
7. **Deployment flexibility** across environments
8. **File system mapping** between WSL and Windows
9. **State transitions** during build process
10. **Package resolution** logic

The key architectural insight is that by detecting the execution context and choosing the appropriate strategy, we completely avoid exposing UNC paths to NuGet/dotnet tools, eliminating the NU1301 errors while maintaining full portability.