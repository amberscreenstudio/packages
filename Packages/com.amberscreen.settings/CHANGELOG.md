# Changelog

All notable changes to `com.amberscreen.settings` are documented here.

## [0.2.0] - 2026-11-15

### Added

- Game-defined options: `GameSettings.AddCustomSlider` / `AddCustomToggle` / `AddCustomStepper` (key, label, default, callback, section heading). They show in the built-in menu under their heading, are saved in the same JSON (`custom` list), apply at once when added, on change and on Reset. Read with `GetCustom` / `GetCustomBool` / `GetCustomInt`, write with `SetCustom`; event `CustomChanged`.
- Text size 75-160%: `GameSettings.TextScale`, `SetTextScale`, `ScaledFontSize(baseSize)`, saved as `textScale`. Shown as "Text size" in the Graphics block (`SettingsProfile.showTextSize`). The built-in menu scales its own text live.
- `SettingsMenuTheme` asset (colours, font, title): `SettingsMenu.theme`, `ApplyTheme`, or `Resources/AmberScreenSettingsMenuTheme` for every menu. Defaults are the old dark look.
- Esc signal: `SettingsMenu.ConsumedCloseThisFrame` (the menu closed on Esc / B this frame) and `SettingsMenu.AnyOpen`, so one Esc doesn't also step a pause menu back.
- One-time import of old values: `GameSettings.ImportLegacyOnce(data => ...)` with `LegacyPrefs.TryFloat/TryInt/TryBool` for PlayerPrefs. Runs once per save file (`legacyImported`), never again after a reset.
- Steam Deck level by name: `SettingsProfile.steamDeckQualityName` (default "Performant"). The index is used when the name isn't one of the quality levels. `ISettingsQualityNames` for custom appliers.
- `SettingsMenuInput` is public (Esc / gamepad B helper). `SettingsMenu.Rebuild()`.
- Demo: three game options, a text-size sample line and an Esc counter that shows Esc reaching a pause menu only when the settings menu didn't use it.

### Changed

- Default menu `sortingOrder` is 1010 (was 500), so it draws above a pause menu at 1000 and below a dev console at 1050. It is applied every time the menu opens.
- Save format 2 (adds `textScale`, `custom`, `legacyImported`). Format 1 files load as before.

## [0.1.1] - 2026-11-14 (verified 2026-11-14)

### Fixed

- Menu could draw far too large and off screen, so it could not be closed. The canvas now uses Scale With Screen Size at 1920x1080 (match 0.5); the panel has a fixed reference size (920x990) at the centre and is shrunk to fit inside the screen, re-checked when the screen size changes.
- The option list scrolls when it doesn't fit; the header with the Close (Esc) button and the footer with Reset are always on screen.
- Font sizes set for the 1920x1080 reference. Esc and gamepad B/East close it every frame while open. Gamepad selection scrolls into view.

### Added

- Layout tests at 1280x800, 1920x1080, 3840x2160 and smaller/portrait screens.

## [0.1.0] - 2026-11-14

### Added

- `GameSettings` static API and `SettingsSession`: quality level, resolution, display mode, vSync, frame limit, master/music/effects volume, mouse sensitivity, invert Y, Input System binding overrides.
- JSON save in `Application.persistentDataPath` (temp-file swap), applied on startup when a `SettingsProfile` named `AmberScreenSettingsProfile` is in a Resources folder.
- `SteamDeckDetector` heuristic with a first-run Steam Deck preset (low quality level, 30 FPS).
- `SettingsMenu`: plain uGUI menu built in code (no TextMeshPro, no render-pipeline parts).
- Optional `SettingsRebinding` when `com.unity.inputsystem` is installed.
- Demo scene menu: Tools > AmberScreen > Settings > Create Settings Demo Scene.
