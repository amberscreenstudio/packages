# Changelog

All notable changes to `com.amberscreen.economy` are documented here.

## [0.1.0] - 2026-10-21

### Added

- Lifted from LWN `Assets/Shared/Economy` (commit `f5a5eeb`): `Money`, `EconomyCurrencySettings`, `IWallet` / `Wallet`, `EarningsRecord` / `EarningsLineType`, `WalletSaveState`.
- Temporary save bits (`ISaveStateProvider`, `JsonSaveBackend`) under `Runtime/TempSave` (`AmberScreen.Economy.TempSave`), marked `// replace with com.amberscreen.save`.
