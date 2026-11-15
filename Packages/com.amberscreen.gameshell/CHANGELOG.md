# Changelog

All notable changes to `com.amberscreen.gameshell` are documented here.

## [0.1.0] - 2026-11-15

### Added

- `GameShell.Boot()`: one "GameShell" object kept across scenes with `SceneFlow` and `PauseFrame`. Safe to call twice; `Booted` event. Optional `autoBoot` in the settings.
- `SceneFlow`: `Go(scene)` with a full-screen fade out and in (0.3 s, sort order 5000), autosave slot written first, `FadeThrough(action)`, `Known` / `SetKnownScenes` (or the settings list, or Build Settings), `IsFading`, `SceneChanging` / `SceneChanged`.
- `PauseFrame`: Esc (and gamepad Start / B) pause menu in uGUI built in code. It pauses time and audio, frees the cursor and puts it back afterwards, and stays clickable at timeScale 0 (the Input System is switched to Dynamic update). Resume / Save / Load / game buttons (`AddMainButton`) / Settings (when `GameShell.OpenSettings` is set) / Quit. Save and Load slot pages with overwrite confirm. The panel fits 90% of the screen height, then scrolls. Live text size through `GameShell.TextScale`, theme asset, sort order 1000.
- Version label (bottom left, sort order 1100) with `GameShellVersion.NoteLoaded` / `ShowWarning`, shown for 8 s.
- `GameShell.BlocksGameplay` / `BlocksInput` (pause, fade, or any `AddBlocker` check) and `AddEscapeGuard` (another menu owns Esc).
- Save hooks: `SaveSlot`, `LoadSlot`, `SlotSummary`, `MigrateLegacy`; defaults go through `com.amberscreen.save`. A `shell.scene` provider stores the scene, used when `restoreSceneOnLoad` is on.
- Editor: Tools > AmberScreen > Game Shell > Find Missing References, Create Settings Asset, Create Game Shell Demo Scene.
