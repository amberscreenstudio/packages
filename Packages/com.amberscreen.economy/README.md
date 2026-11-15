# Amber Screen Economy

Shared money, wallet and earnings types for Amber Screen games. Game-agnostic — no job, auction or payout rules.

**Version:** 0.2.0  
**Unity:** 6000.6+  
**Owner:** LWN. Changes go through Packages.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.economy#economy/0.2.0
```

Pin a tag once published; until then use a local `file:` reference (Sandbox) or a commit hash.

## Public API

| Type | Role |
| --- | --- |
| `Money` | `Format` / `FormatMinor` / `ToMinor` / `FromMinor` via `Resources/EconomyCurrency`. Negatives as "-$1.00"; `FormatMinor` is exact (no float) |
| `EconomyCurrencySettings` | ScriptableObject: symbol + amount format |
| `IWallet` / `Wallet` | Balance, Available, Add, TrySpend, TryReserve / Release / Commit, transaction log |
| `EarningsRecord` | Line items (label, amount minor, Pay/Bonus/Penalty) + total |
| `WalletSaveState` | Capture/Restore for balance, holds, log |

## Holds

`TryReserve` freezes funds without changing `Balance`. `Available = Balance − Σ holds`. `Release` drops a hold; `Commit` spends it.

`Wallet.Verbose` (default `false`) gates `Debug.Log` on wallet mutations. This module does not reference DevConsole.

## Saving

**With `com.amberscreen.save` installed** (recommended): at boot call

```csharp
AmberScreen.Economy.Save.WalletSaveProvider.Register(wallet, "wallet");
```

The wallet is then written and read with every `SaveStore.Save` / `SaveStore.Load` (slots, .bak, version stamp). This bridge sits in its own assembly (`AmberScreen.Economy.Save`) that only compiles when the save package is present, so Economy never requires it.

**Without it:** the temporary `ISaveStateProvider` (string) and `JsonSaveBackend` in `AmberScreen.Economy.TempSave` still work as before.

## What each game must supply

1. Its own `Resources/EconomyCurrency` asset (or call `Money.Apply`).
2. Game pay sheets / calculators that **output** an `EarningsRecord` (not owned here).
3. A game bootstrap that constructs `Wallet` and registers `WalletSaveProvider` (or uses `WalletSaveState(key)` with the temporary backend).
4. Any game-specific history (shift tallies, auction lots) lives in the game, not here.

## Dependencies

UnityEngine only. No Input System, URP, UI, or DevConsole assemblies. `com.amberscreen.save` is optional.
