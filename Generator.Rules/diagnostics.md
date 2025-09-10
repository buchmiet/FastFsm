# FastFSM Diagnostics (v0.7.5)

Ustandaryzowana taksonomia i numeracja z buforami. Prefiks FSM + cztery cyfry.

## A. Model & Deklaracje (0100–0599)

- FSM0100 — Brak atrybutu [StateMachine].
- FSM0101 — Typy TState/TTrigger muszą być enumami.
- FSM0200 — Nieprawidłowa wartość enuma w atrybucie.
- FSM0300 — Nieprawidłowa sygnatura metody (guard/action/callback).
- FSM0301 — Guard z payloadem bez wsparcia payload.
- FSM0302 — async void niedozwolone.
- FSM0400 — Zduplikowane przejście.
- FSM0500 — Stan nieosiągalny.

## B. Spójność async (1100–1199)

- FSM1100 — Mieszanie sync/async w jednej maszynie.
- FSM1110 — Zły typ zwrotny async guard (Task<bool> zamiast ValueTask<bool>).
- FSM1120 — Async callback w maszynie synchronicznej.

## C. HSM – hierarchia (2000–2099)

- FSM2000 — Cykl w hierarchii.
- FSM2010 — „Sierota”/brakujący rodzic.
- FSM2020 — Kompozyt bez stanu początkowego.
- FSM2030 — Wiele stanów początkowych.
- FSM2040 — Historia na nie‑kompozycie.

## D. Fluent DSL (3000–3099)

- FSM3000 — Niedomknięte przejście.
- FSM3010 — Auto‑domknięcie jako internal.
- FSM3020 — Wiele wywołań Payload().
- FSM3030 — Priority() wymaga literału int.
- FSM3040 — Priority() poza kontekstem przejścia.
- FSM3050 — Wiele globalnych OnException.
- FSM3060 — Nieprawidłowa sygnatura OnException.

## E. Trace/Discovery (9000–9099) — domyślnie wyłączone

- FSM9000 — Processing candidate.
- FSM9001 — Declaration plan.
- FSM9002 — Empty code generated.
- FSM9003 — Enum‑only states fallback.
- FSM9004 — MSBuild analyzer props.
- FSM9005 — AddSource ok.
- FSM9006 — Skipped candidate.
- FSM9007 — Generator trace.
- FSM9008 — Starting parse.
- FSM9009 — Variant decision.
- FSM9010 — Parser/config sections.
- FSM9011 — HSM flag tracking.
- FSM9012 — Pre‑AddSource log helper.
- FSM9013 — MSBuild logging flags.

## Migracja: stare → nowe

- FSM001 → FSM0400
- FSM002 → FSM0500
- FSM003 → FSM0300
- FSM004 → FSM0100
- FSM005 → FSM0101
- FSM006 → FSM0200
- FSM010 → FSM0301
- FSM011 → FSM1100
- FSM012 → FSM1110
- FSM013 → FSM1120
- FSM014 → FSM0302
- FSM100 → FSM2000
- FSM101 → FSM2010
- FSM102 → FSM2020
- FSM103 → FSM2030
- FSM104 → FSM2040
- FSM200 → FSM3000
- FSM201 → FSM3010
- FSM202 → FSM3020
- FSM207 → FSM3030
- FSM208 → FSM3050
- FSM209 → FSM3060
- FSM210 → FSM3040
- FSM989..998A → FSM9000..9013

Uwagi: Wszystkie diagnostyki są zdefiniowane centralnie w Generator.Rules i emitowane poprzez DiagnosticFactory/RuleLookup. Ślady 9000+ mają IsEnabledByDefault=false i można je włączyć lokalnie.
