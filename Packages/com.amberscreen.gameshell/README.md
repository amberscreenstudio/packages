# Amber Screen Game Shell

The frame around a game: boot, scene changes with fades, a pause menu with save/load slots, a version label, and one "is the game blocked" check. Assembly `AmberScreen.GameShell`, namespace `AmberScreen.GameShell`. uGUI built in code (legacy `Text`, no TextMeshPro), no render-pipeline code.

**Version:** 0.1.0  
**Unity:** 6000.6+  
**Depends on:** `com.amberscreen.save` 0.1.0, Input System, uGUI

## Install

Add both lines (Unity can't fetch a git dependency by itself, so `save` is listed too):

```
"com.amberscreen.save": "https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.save#save/0.1.0",
"com.amberscreen.gameshell": "https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.gameshell#gameshell/0.1.0"
```

(Pin the tags once published; until then use a commit hash or a local `file:` reference.)

## Quick start

```csharp
using AmberScreen.GameShell;

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void Boot()
{
    // register your ISaveStateProviders first (com.amberscreen.save)
    GameShell.Boot();
    GameShell.PauseSubtitle = () => "Day 3   $12.50";
    GameShell.TextScale = () => AmberScreen.Settings.GameSettings.TextScale;      // if you use com.amberscreen.settings
    GameShell.OpenSettings = () => settingsMenu.Open();
    GameShell.AddEscapeGuard(() => SettingsMenu.AnyOpen || SettingsMenu.ConsumedCloseThisFrame);
    GameShell.AddEscapeGuard(() => InspectViewer.AnyInspecting || InspectViewer.ConsumedCloseThisFrame);
    GameShell.AddEscapeGuard(() => DevConsole.IsOpen || DevConsole.ConsumedCloseThisFrame);
    GameShell.AddBlocker(() => DevConsole.BlocksGame);
    DevConsole.OpenBlocked = () => PauseFrame.IsOpen || SceneFlow.IsFading;
}

// gameplay
if (GameShell.BlocksGameplay) return;

// scenes
SceneFlow.Go("Workshop");            // fade, autosave slot 0 first
SceneFlow.Go("Workshop", false);     // no autosave
SceneFlow.FadeThrough(ResetLevel);   // fade out, run, fade in
```

The shell itself never references the settings, inspect or dev console packages; the game wires them with the hooks above.

## Settings

Tools > AmberScreen > Game Shell > Create Settings Asset makes `Assets/Resources/AmberScreenGameShellSettings.asset` (or pass an asset to `GameShell.Boot(settings)`).

| Field | Default | What it does |
| --- | --- | --- |
| `theme` | built-in | `GameShellTheme`: font, panel, text, accent, buttons, fade, version text colours |
| `autoBoot` | off | Boot before the first scene without game code |
| `knownScenes` | empty | Scenes `SceneFlow.Go` may load. Empty = any scene in Build Settings |
| `fadeSeconds` / `fadeSortingOrder` | 0.3 / 5000 | Fade time and canvas order |
| `autosaveOnSceneChange` | on | Write slot 0 before each scene change |
| `restoreSceneOnLoad` | off | After a load, go to the scene the slot was saved in |
| `pauseEnabled`, `pauseKey`, `gamepadPause` | on, Esc, on | Pause input (gamepad Start opens/closes, B steps back) |
| `pauseTitle`, `pauseSortingOrder` | "Paused", 1000 | Title and canvas order |
| `showSaveLoad`, `showQuit` | on, on | Main page buttons |
| `fixInputUpdateMode` | on | Switch the Input System to Dynamic update so the menu works at timeScale 0 |
| `showVersionLabel`, `versionSortingOrder`, `versionWarningSeconds` | on, 1100, 8 | Version label |

Draw order: pause 1000, settings menu 1010, dev console 1050, version label 1100, fade 5000.

## Pause menu

- Esc opens it; Esc again steps back one page, then closes. While any escape guard is true, Esc is left alone.
- Main page: Resume, Save, Load, your buttons (`PauseFrame.AddMainButton(caption, action, closesMenu)`), Settings (only when `GameShell.OpenSettings` is set), Quit.
- Save page: slots 1 to SlotCount-1 (slot 0 is the autosave). Overwriting asks first. Load page also lists the autosave.
- `PauseFrame.IsOpen`, `Opened`, `Closed`, `PauseFrame.OpenMenu()` / `CloseMenu()`, `ApplyTheme(theme)`.

## Saving

By default the pages save with every registered provider (`SaveStore.Save`) and load with `SaveStore.Load`, then warn if the save's MAJOR version differs (`0.MAJOR.MINOR`; the load still goes ahead). Replace any step:

```csharp
GameShell.SaveSlot = slot => MySave(slot);
GameShell.LoadSlot = slot => MyLoad(slot);          // return a LoadResult
GameShell.SlotSummary = slot => "Day 4, $12.00";     // shown on the slot buttons
GameShell.MigrateLegacy = json => ConvertOldSave(json);
```

`GameShellVersion.ShowWarning(text)` puts any message under the version label for 8 s.

## Stays in games

Menu content and art (theme asset), what is saved, the game's own keys, its scenes, and the wiring to other packages.

## Demo and tools

- Tools > AmberScreen > Game Shell > Create Game Shell Demo Scene: Play, then Esc. Save and load a counter, try the fade button, the text size slider, the version warning, and M (pretend another menu owns Esc).
- Tools > AmberScreen > Game Shell > Find Missing References: lists missing scripts and broken references in open scenes and all prefabs (use after moving code into packages).
