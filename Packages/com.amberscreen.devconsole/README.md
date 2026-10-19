# Amber Screen Dev Console

Runtime debug console (`AmberScreen.DevConsole`). No render-pipeline code — works on URP, URP 2D, and HDRP.

**Version:** 0.1.1  
**Unity:** 6000.6+

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.devconsole#devconsole/0.1.1
```

(Tag may not exist yet — use `main` or a local `file:` path until tagged.)

## Setup

1. Add the package (Git URL or local `file:` path).
2. An editor script adds the scripting define `AMBERSCREEN_DEVCONSOLE` to the active build target if missing. Opt out via Dev Console settings → **Auto Add Define**.
3. Non-development player builds strip the define unless **Keep in release builds** is enabled (then restore after the build).
4. Optional: create **Assets → Create → Amber Screen → Dev Console Settings** at `Resources/AmberScreenDevConsoleSettings`.
5. Import **TMP Essential Resources** if the project does not already have them. The console falls back to Resources fonts and warns once if none are found.
6. Register commands from gameplay code. The console is inactive unless `Debug.isDebugBuild` is true **and** the define is present.

When the define is stripped, Register calls are no-ops and `IsOpen` is always false.

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

Namespace: `AmberScreen.DevConsole`

```csharp
using AmberScreen.DevConsole;

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

## Sandbox test

Open the Sandbox project in Unity 6000.6.6f1. Packages should resolve with no errors. Open `Assets/Scenes/DevConsoleSandbox.unity` and press Play.

- F1 opens the console with visible buttons, and the label shows IsOpen true. Esc closes it.
- Gamepad: Select+Start opens it with the first button already selected. The d-pad moves and the list scrolls to follow; A presses and B closes. No mouse needed.
- The FPS readout updates about twice a second. Toggle spin, pick a colour + Go, reset (second press), DevButton note; the note history scrolls.
- In `Resources/AmberScreenDevConsoleSettings`, turn on the DEV button: it shows while the console is closed and opens it when tapped.
- Set Layout to DockRight: collapse leaves the header with its expand button selected; while collapsed, Esc and B do nothing to the console.
- Set Layout to MobilePortrait and use the Device Simulator (any phone, portrait): big single-column buttons inside the safe area; a 2-second hold in the top-left corner opens it.
- Stop and press Play again: the console starts closed. What won't work in this scene: there's no real game, so only the label reacts to input blocking. The 3-finger tap can't be tested in the Editor (it's tested on a phone in Twin Ticks), and the release-build strip only shows in a non-development player build.
