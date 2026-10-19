# Amber Screen Dev Console

Runtime debug console. Namespace: `AmberScreen.Dev`. No render-pipeline code — works on URP, URP 2D, and HDRP.

**Version:** 0.2.3  
**Unity:** 6000.6+  
**Package / asmdef name:** `com.amberscreen.devconsole` / `AmberScreen.DevConsole`

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.devconsole#devconsole/0.2.3
```

(Use `main` or a local `file:` path until the tag exists.)

## Font

UI text uses **Inter** (Regular + SemiBold), bundled under `Runtime/Fonts/`, licensed under the [SIL Open Font License 1.1](Runtime/Fonts/OFL.txt). TMP font assets are created at runtime from those TrueType files (or from `fontOverride` / the TMP default if Inter is missing).

## Setup

1. Add the package (Git URL or local `file:` path).
2. An editor script adds the scripting define `AMBERSCREEN_DEVCONSOLE` to the active build target if missing. Opt out via Dev Console settings → **Auto Add Define**.
3. Optional: create **Assets → Create → Amber Screen → Dev Console Settings** at `Resources/AmberScreenDevConsoleSettings`. Existing assets should run Reset (or update `buttonHeight` / `fontSize` / `spacing` by hand) after upgrading.
4. TMP Essential Resources remain a fallback if bundled Inter is not assigned on the settings asset.
5. Register commands from gameplay code. The console is inactive unless `Debug.isDebugBuild` is true **and** the define is present.

When the define is stripped, Register calls are no-ops and `IsOpen` is always false.

### Release builds

The define strip runs in `OnPreprocessBuild`. A Unity 6 Build Profile with its own scripting defines overrides it. `Debug.isDebugBuild` keeps the console inactive in any non-development build regardless. The Console window logs `[DevConsole] AMBERSCREEN_DEVCONSOLE stripped for release build` at the start of a non-development player build (unless **Keep in release builds** is on) and `[DevConsole] define restored` afterwards.

### IL2CPP / Preserve

`[DevButton]` methods and `IDevConsoleSectionProvider` types must survive linker stripping. Put `[UnityEngine.Scripting.Preserve]` on those methods/types, or add a `link.xml`. The `DevButtonAttribute` class itself is preserved.

## Open bindings (defaults)

| Input | Action |
| --- | --- |
| F1 | Open / toggle |
| Esc | Close (only when open and not DockRight-collapsed) |
| Gamepad Select + Start (held) | Open / toggle |
| 3-finger tap (release) | Open / toggle |
| 2 s hold in top-left 15% of safe area | Open only |
| On-screen DEV button | Off by default |

Games can check `DevConsole.ConsumedCloseThisFrame` after Esc/B so pause menus do not also close.

## API

Namespace: `AmberScreen.Dev`

```csharp
using AmberScreen.Dev;

DevConsole.Register("Cheats", "Give Gold", () => AddGold(100));
// Re-registering the same section+label replaces the entry and resets caption to the label.
DevConsole.SetButtonCaption("Cheats", "Give Gold", "Give 100 Gold");

DevConsole.RegisterToggle("Debug", "Show Bounds", () => showBounds, v => showBounds = v);
DevConsole.RegisterDropdown("Level", "Jump To", () => names, i => Load(i));
DevConsole.RegisterConfirm("Danger", "Delete Save", DeleteSave);
DevConsole.RegisterReadout("Perf", "FPS", () => fps, 0.5f);
DevConsole.Note("Warped");

bool open = DevConsole.IsOpen;
bool blocks = DevConsole.BlocksGame; // open and not collapsed
DevConsole.OpenChanged += isOpen => { };
DevConsole.BlocksGameChanged += blocksGame => { };
```

`OpenChanged` fires only when open/closed changes (not on DockRight collapse).  
`BlocksGameChanged` fires whenever `BlocksGame` changes (open, close, collapse, expand).
