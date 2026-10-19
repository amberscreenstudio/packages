# Amber Screen Dev Console

Runtime debug console (`AmberScreen.DevConsole`). No render-pipeline code — works on URP, URP 2D, and HDRP.

**Version:** 0.1.0  
**Unity:** 6000.6+

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.devconsole#devconsole/0.1.0
```

## Setup

1. Add the package (Git URL or local `file:` path).
2. An editor script adds the scripting define `AMBERSCREEN_DEVCONSOLE` to the active build target if missing. Opt out via the Dev Console settings asset (`Resources/AmberScreenDevConsoleSettings` → **Auto Add Define**).
3. Optional: create **Assets → Create → Amber Screen → Dev Console Settings** and place it at `Resources/AmberScreenDevConsoleSettings` (built-in defaults apply if missing).
4. Register commands from gameplay code (see API). The console is inactive unless `Debug.isDebugBuild` is true **and** the define is present.

When the define is stripped, Register calls are no-ops and `IsOpen` is always false.

## Open bindings (defaults)

| Input | Action |
| --- | --- |
| F1 | Open / toggle |
| Esc | Close |
| Gamepad Select + Start (held) | Open |
| 3-finger tap | Open (switchable) |
| 2 s hold in top-left 15% corner | Open (switchable) |
| On-screen DEV button | Off by default |

## API

Namespace: `AmberScreen.DevConsole`

```csharp
using AmberScreen.DevConsole;

// Button (same section+label replaces; visible:false = hidden placeholder)
DevConsole.Register("Cheats", "Give Gold", () => AddGold(100));
DevConsole.Register("Cheats", "Secret", () => {}, visible: false);
DevConsole.Register("Cheats", "Warp", Warp, available: () => CanWarp);

DevConsole.SetButtonCaption("Cheats", "Give Gold", "Give 100 Gold");

DevConsole.RegisterToggle("Debug", "Show Bounds", () => showBounds, v => showBounds = v);

DevConsole.RegisterDropdown("Level", "Jump To",
    () => new[] { "Hub", "Cave", "Boss" },
    index => LoadLevel(index));

DevConsole.RegisterConfirm("Danger", "Delete Save", DeleteSave);

DevConsole.RegisterReadout("Perf", "FPS", () => $"{1f / Time.unscaledDeltaTime:0}", 0.5f);

DevConsole.Note("Warped to cave");

DevConsole.SetSectionOrder("Perf", "Cheats", "Danger");
DevConsole.Unregister("Cheats", "Give Gold");
DevConsole.ClearSection("Cheats");

bool open = DevConsole.IsOpen;
bool blocks = DevConsole.BlocksGame; // open and not collapsed
DevConsole.OpenChanged += isOpen => { /* ... */ };
DevConsole.Open();
DevConsole.Close();
DevConsole.Toggle();
```

### `[DevButton]`

```csharp
[DevButton("Cheats", "Heal")]
static void Heal() { /* ... */ }
```

Discovered once the first time the console opens (can be disabled in settings).

### `IDevConsoleSectionProvider`

```csharp
public class MyCheats : IDevConsoleSectionProvider
{
    public void Register()
    {
        DevConsole.Register("My", "Ping", () => DevConsole.Note("pong"));
    }
}
```

Discovered and called once (first open).

## Layout & settings

- **Floating** (default): centred panel sized to content.
- **DockRight**: full height, 320 px wide; collapse leaves a 56 px header (BlocksGame false when collapsed).
- **MobilePortrait**: full screen inside `Screen.safeArea`, single column, ≥48 dp touch targets.

Scale: Small / Medium / Large + multiplier. CanvasScaler reference: 1920×1080, or 1080×1920 for MobilePortrait. Medium is readable at 1280×800 (Steam Deck).

## Domain Reload off

Statics reset on `RuntimeInitializeLoadType.SubsystemRegistration`. The console always starts closed. Host GameObject is created on demand with `DontDestroyOnLoad`.

## Sandbox test

Open the Sandbox project, open the scene `Assets/Scenes/DevConsoleSandbox.unity`, and press Play.

- F1 opens the console and the label shows IsOpen true. Esc closes it.
- On a gamepad, Select+Start opens it. The d-pad navigates, A presses and B closes, with no mouse.
- Try every widget: spin toggle, colour dropdown + Go, reset (needs a second press), FPS readout, DevButton note.
- In `Resources/AmberScreenDevConsoleSettings`, set Layout to DockRight and check the collapse button (BlocksGame turns false). Then set it to MobilePortrait with a 1080x1920 Game view and check the layout.
- Stop and press Play again: the console starts closed. What won't work in this scene: there's no real game, so the only thing that reacts to input blocking is the label. The 3-finger tap can't be tested in the Editor (the Device Simulator has one finger, so test the corner hold there); the 3-finger tap gets tested on a phone in Twin Ticks.
