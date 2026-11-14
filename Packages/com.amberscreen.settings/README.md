# AmberScreen Settings

A settings menu every game can ship: graphics, audio and controls, saved as JSON and applied on startup, with a Steam Deck first-run preset. Assembly `AmberScreen.Settings`, namespace `AmberScreen.Settings`. Render-pipeline neutral (no URP or HDRP references), no TextMeshPro.

**Version:** 0.1.1
**Unity:** 6000.0+
**Owner:** Packages.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.settings#settings/0.1.1
```

## Quick start

1. Create a profile: Project window > right-click > Create > AmberScreen > Settings Profile. Put it in a `Resources` folder and keep the name `AmberScreenSettingsProfile`. The saved choices are now loaded and applied before the first scene loads.
2. Optional: assign your `AudioMixer` and type the names of its exposed volume parameters (defaults `MasterVolume`, `MusicVolume`, `SfxVolume`). With no mixer, the master volume drives `AudioListener.volume` and music/effects do nothing.
3. Show the menu: add `SettingsMenu` to an empty GameObject (or `SettingsMenu.Create()`) and call `Open()` from your pause menu. Escape, gamepad B or the Close button closes it, and it saves on close. It is laid out for 1920x1080 (Scale With Screen Size, match 0.5), shrinks to fit smaller screens and scrolls if the options don't fit.

Without a profile in Resources nothing happens at startup until you call `GameSettings.Initialize(profile)`.

## What it covers

| Area | Choices | How it is applied |
|------|---------|-------------------|
| Graphics | quality level (names from Quality settings), resolution, display mode, vSync, frame limit | `QualitySettings`, `Screen.SetResolution`, `Application.targetFrameRate` (resolution and window mode are skipped in the Editor) |
| Audio | master, music, effects (0..1) | mixer exposed parameters in dB (0 = -80 dB) |
| Controls | mouse sensitivity, invert Y | plain values plus the `GameSettings.ControlsChanged` event; your camera code reads them |
| Rebinding | optional | `SettingsRebinding` (only compiled when `com.unity.inputsystem` is installed) |

## Steam Deck

`SteamDeckDetector.IsSteamDeck(os, gpu, width, height, steamDeckEnv)` is one static heuristic:

- Steam's `SteamDeck=1` environment variable, or
- the Deck's own GPU name (`AMD Custom GPU 0405` LCD, `0932` OLED; also counts under Proton/Windows builds), or
- SteamOS/Linux plus a 1280x800 native screen.

On the first run on a Deck (no save file yet) the profile's `steamDeckQualityLevel` (clamped to the levels that exist), `steamDeckTargetFrameRate` (30) and vSync off are chosen. Set `SteamDeckDetector.Override = true` to fake a Deck for testing.

## Public API (`GameSettings`)

| Member | Role |
|--------|------|
| `Initialize(profile)`, `IsInitialized`, `Current`, `FilePath` | Load or create, then apply |
| `ApplyPreset(level)`, `ApplySteamDeckPreset()` | Quality presets |
| `SetResolution`, `SetFullScreenMode`, `SetVSync`, `SetTargetFrameRate` | Graphics, applied at once |
| `GetVolume` / `SetVolume(VolumeChannel, 0..1)` | Audio, applied at once |
| `MouseSensitivity`, `InvertY`, `SetMouseSensitivity`, `SetInvertY` | Controls |
| `Save()`, `ResetToDefaults()`, `ResetAsFirstRun()` | Keep, undo, start over |
| `QualityNames`, `GetResolutionOptions()`, `FrameRateOptions` | Lists for your own menu |
| Events: `Changed(data)`, `ControlsChanged(sensitivity, invertY)` | React to changes |

Changes apply immediately; call `Save()` to keep them (the built-in menu saves on close).

## Stays in games

The pause menu that opens this, any extra game-only options, the look of a custom menu, and the camera code that uses sensitivity and invert Y.

## Demo

Tools > AmberScreen > Settings > Create Settings Demo Scene, then Play. In the Editor the demo saves to `<project>/Temp/AmberScreenSettingsDemo/settings.json` and puts quality, vSync, frame rate and listener volume back when you stop.
