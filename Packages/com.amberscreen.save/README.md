# Amber Screen Save

Slot saves for Amber Screen games. Assembly `AmberScreen.Save`, namespace `AmberScreen.Save`. No UI, no render-pipeline parts, UnityEngine only.

**Version:** 0.1.0
**Unity:** 6000.0+
**Owner:** Packages.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.save#save/0.1.0
```

### A game that already has its own `Assets/Shared/Save` copy

The package uses the same assembly name (`AmberScreen.Save`) and the same script GUIDs, so the two can't live side by side. Do both in the same step, before Unity recompiles:

1. Close Unity.
2. If you want to keep your `SaveSettings.asset`, move it into any `Resources` folder and rename it `AmberScreenSaveSettings` (the defaults are the same: 4 slots, folder `saves`, `.json`, format 3, so you can also just delete it).
3. Delete the game's `Assets/Shared/Save` folder (and its `.meta` file).
4. Add the install line above to `Packages/manifest.json`.
5. Open Unity. Game code keeps compiling because the namespace and type names are unchanged.

## What it does

- Slot files under `Application.persistentDataPath/<folderName>/slotN.json`. Slot 0 is the autosave by convention; with the default `slotCount = 4` the player slots are 1, 2 and 3.
- Safe write: write `slotN.json.tmp`, flush, swap it in and keep the old file as `slotN.json.bak`.
- If the main file can't be read, load the `.bak`.
- Stamps `gameVersion` (`Application.version`) and `saveFormat`.
- Providers register with a stable key; duplicates are rejected. Unknown keys in a file are reported in `LoadResult.skippedKeys` and kept when you re-save the same envelope.
- A different MAJOR version **warns and still loads** (`LoadResult.versionMismatch`, `LoadResult.message`). Versions are 0.MAJOR.MINOR during early access, so "0.3.4" and "0.4.0" differ, "0.3.1" and "0.3.9" don't. After 1.0 the first number is the MAJOR.
- `migrateLegacy` hook turns an old, pre-keyed file into an envelope once on load.
- `Peek` (read a slot for a menu summary), `NewestSlot`, `Delete` (also removes `.bak` and `.tmp`).
- Bad slots log a warning and return false / a failed `LoadResult`. Nothing throws.

## Plugging a game in

1. Reference `AmberScreen.Save` from the game asmdef.
2. Optional: Tools > AmberScreen > Save > Create Save Settings Asset (makes `Assets/Resources/AmberScreenSaveSettings.asset`) and change slot count or folder.
3. Implement `ISaveStateProvider` for each slice of state. `Capture()` returns a `[Serializable]` object; `Restore(object)` applies it. Add `hasX` flags so fields missing from older saves keep the game's defaults (never treat missing as 0 or false).
4. At boot: `SaveRegistry.Register(provider, typeof(YourState))`.
5. Save: `SaveStore.Save(slot, new SaveEnvelope(), out error)`. Load: `SaveStore.Load(slot, migrateLegacy)` (pass `null` if you have no old format).

## Public API

| Type | Role |
| --- | --- |
| `SaveStore` | `Save`, `Load`, `Peek`, `Delete`, `Exists`, `NewestSlot`, `PathFor`, `SlotCount`, `Folder`, `AutosaveSlot`, `MajorOf`, `IsMajorMismatch` |
| `SaveRegistry` | `Register`, `Unregister`, `Clear`, `TryGet`, `Keys` |
| `SaveEnvelope` | On-disk file: `gameVersion`, `saveFormat`, keyed JSON entries (`Find`, `Set`) |
| `ISaveStateProvider` | `Key`, `Capture()`, `Restore(object)` |
| `LoadResult` | `ok`, `versionMismatch`, `savedVersion`, `skippedKeys`, `message` |
| `SaveSettings` | ScriptableObject: slot count, folder, extension, temp and bak suffixes, format. `Resources/AmberScreenSaveSettings` |

## Stays in games

What gets saved (the providers and their data), old-save migrations, slot summary text and any save/load menu. `com.amberscreen.gameshell` has a ready pause menu with save and load pages.
