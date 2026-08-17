; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FSM989 | FSM.Generator.Parser | Info | Configuration sections debug
FSM990_PRE | FSM.Generator.AddSource | Info | Logging helper pre-AddSource debug
FSM990_PROP | FSM.Generator.Config | Info | MSBuild logging flags debug
FSM991 | FSM.Generator | Info | Variant decision debug
FSM992 | FSM.Generator | Info | Declaration plan debug
FSM993 | FSM.Generator | Warning | Empty code generated
FSM994 | FSM.Generator | Info | Enum-only states fallback
FSM995 | FSM.Generator.Config | Info | MSBuild analyzer properties debug
FSM996 | FSM.Generator.AddSource | Info | AddSource succeeded debug
FSM997 | FSM.Generator.Discovery | Info | State machine candidate skipped
FSM998 | FSM.Generator.Discovery | Info | State machine candidate found
