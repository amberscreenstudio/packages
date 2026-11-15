# Changelog

All notable changes to `com.amberscreen.economy` are documented here.

## [0.2.0] - 2026-11-15

### Added

- Save hook for `com.amberscreen.save`: `AmberScreen.Economy.Save.WalletSaveProvider` (`WalletSaveProvider.Register(wallet, "wallet")`). It lives in its own assembly that only compiles when the save package is installed, so Economy still has no dependencies.
- `WalletSaveState.CaptureData()` / `RestoreData(WalletSaveData)` for keyed saves.

### Fixed

- Negative money is "-$1.00" (sign before the symbol), not "$-1.00". `Money.FormatMinor` formats cents exactly without going through float. A zero amount never shows a minus.

### Kept

- The temporary `TempSave` string interface and `JsonSaveBackend` still work for games that use them.

## [0.1.0] - 2026-10-21

### Added

- Lifted from LWN `Assets/Shared/Economy` (commit `f5a5eeb`): `Money`, `EconomyCurrencySettings`, `IWallet` / `Wallet`, `EarningsRecord` / `EarningsLineType`, `WalletSaveState`.
- Temporary save bits (`ISaveStateProvider`, `JsonSaveBackend`) under `Runtime/TempSave` (`AmberScreen.Economy.TempSave`), marked `// replace with com.amberscreen.save`.
