# Changelog

All notable changes to `com.amberscreen.settings` are documented here.

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
