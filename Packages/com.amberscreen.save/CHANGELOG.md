# Changelog

All notable changes to `com.amberscreen.save` are documented here.

## [0.1.0] - 2026-11-15

### Added

- Lifted from the proven `Assets/Shared/Save` copy in a shipped-to-playtest game (assembly `AmberScreen.Save`, same script GUIDs): `SaveStore`, `SaveRegistry`, `SaveEnvelope`, `ISaveStateProvider`, `LoadResult`, `SaveSettings`.
- `SaveStore.MajorOf` / `SaveStore.IsMajorMismatch` are public so games can use the same version rule.
- `SaveSettings.ClearCache()` for tests and tools.
- Editor menu: Tools > AmberScreen > Save > Create Save Settings Asset, and Open Save Folder.
- EditMode tests.

### Fixed

- Version check: `MajorOf` read the first number, which is always 0 under 0.MAJOR.MINOR, so `LoadResult.versionMismatch` never fired. A leading 0 is now skipped ("0.3.4" gives 3).
- Settings asset was only found inside the Editor (AssetDatabase path), so builds always used the built-in defaults. It now loads `Resources/AmberScreenSaveSettings` in the Editor and in builds. In the Editor only, any other SaveSettings asset is still found, with a warning to move it.
