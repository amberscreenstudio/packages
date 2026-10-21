# Amber Screen Economy

Shared money, wallet and earnings types for Amber Screen games. Game-agnostic — no job, auction or payout rules.

**Version:** 0.1.0 (untagged)  
**Unity:** 6000.6+  
**Owner:** LWN. Changes go through Packages.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.economy#economy/0.1.0
```

Pin a tag once published; until then use a local `file:` reference (Sandbox) or a commit hash.

## Public API

| Type | Role |
| --- | --- |
| `Money` | `Format` / `FormatMinor` / `ToMinor` / `FromMinor` via `Resources/EconomyCurrency` |
| `EconomyCurrencySettings` | ScriptableObject: symbol + amount format |
| `IWallet` / `Wallet` | Balance, Available, Add, TrySpend, TryReserve / Release / Commit, transaction log |
| `EarningsRecord` | Line items (label, amount minor, Pay/Bonus/Penalty) + total |
| `WalletSaveState` | Capture/Restore for balance, holds, log |

## Holds

`TryReserve` freezes funds without changing `Balance`. `Available = Balance − Σ holds`. `Release` drops a hold; `Commit` spends it.

`Wallet.Verbose` (default `false`) gates `Debug.Log` on wallet mutations. This module does not reference DevConsole.

## Save scaffolding (temporary)

`ISaveStateProvider` and `JsonSaveBackend` live in `AmberScreen.Economy.TempSave` until **`com.amberscreen.save`** exists. Both are marked `// replace with com.amberscreen.save`.

## What each game must supply

1. Its own `Resources/EconomyCurrency` asset (or call `Money.Apply`).
2. Game pay sheets / calculators that **output** an `EarningsRecord` (not owned here).
3. A game bootstrap that constructs `Wallet` + `WalletSaveState(key)` and calls Load/Save.
4. Any game-specific history (shift tallies, auction lots) lives in the game, not here.

## Dependencies

UnityEngine only. No Input System, URP, UI, or DevConsole assemblies.
