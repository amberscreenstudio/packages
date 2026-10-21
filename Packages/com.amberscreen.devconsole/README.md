# Amber Screen Dev Console

Runtime debug console. Namespace: `AmberScreen.Dev`. No render-pipeline code — works on URP, URP 2D, and HDRP.

**Version:** 0.4.1  
**Unity:** 6000.6+  
**Package / asmdef name:** `com.amberscreen.devconsole` / `AmberScreen.DevConsole`

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.devconsole#devconsole/0.4.1
```

Requires TextMeshPro Essential Resources (Window > TextMeshPro > Import TMP Essential Resources). Without them the console logs an error and stays off.

## Theme

Shared UI must be skinnable so no game looks worse for hosting the console.

1. **Create** Assets → Create → Amber Screen → Dev Console Theme.
2. **Assign** the asset to `theme` on your `AmberScreenDevConsoleSettings` (or call `DevConsole.ApplyTheme(theme)` at runtime).
3. Leave `theme` null to keep the built-in look (same pixels as 0.4.0).

| Field group | What it skins |
| --- | --- |
| Surfaces (panel / rail / HUD / window / row) | Background colour, optional sprite, 9-slice |
| Border | Colour + width (drawn when you add chrome) |
| Fonts | TMP regular + bold (falls back to Inter / `fontOverride`) |
| Text colours | Labels, values, warning, error, accent, muted |
| Font sizes | Title, body, HUD |
| Layout | Padding, spacing, row height; corner radius only applies when a sprite is set |
| Buttons | Normal / hover / pressed / disabled / selected |

## Debug HUD rail

Shared always-on translucent HUD on the **left** edge in landscape (or a **full-width top strip** in portrait). Games only register lines — no OnGUI, no per-game HUD canvas. The console rail stays on the right; section windows open between the two rails (or under the strip in portrait).

```csharp
DevHud.SetLine("Vehicle", "Speed", () => speedKmh.ToString("0"));
DevHud.SetLine("Vehicle", "Gear", () => gearText, order: 1);
// in OnDestroy: DevHud.ClearSection("Vehicle");
```

- **Toggle:** F2 (`hudToggleKey`), also `DevHud.Toggle()` / `Visible`. No `#if` needed — API is a no-op when the define is stripped.
- **Empty lines:** a getter that returns `null` or `""` is not drawn; a section with no visible lines (including its header) is omitted.
- **Key hints:** `DevHud.SetKeyHints(mode, (action, label)…)`, `SetMode(mode)`, `ClearKeyHints(mode)`. Draws a **Keys** section last for the active mode (binding display + label).
- **Settings:** `hudShowOnStart`, `hudToggleKey`, `hudScreenFraction` / min / max width, `hudOpacity`, `hudFontSize`, `hudUpdateHz` (default 6), `hudPortraitHeightFraction` (default 0.18). `rememberState` restores HUD visibility.
- **16:9 rule:** at 16:9 or narrower the HUD overlays the game (full `DevHud.GameRect`). Wider than 16:9, `GameRect` is the centred 16:9 of the safe area and the HUD sits in the left margin when it fits (otherwise it overlaps by the difference). Console windows never paint over the HUD.
- **Game UI:** during development, keep minimaps / gauges inside `DevHud.GameRect` so they stay clear of both rails on wide screens. At 16:9 or narrower the rails may draw over game UI — accepted for development. Move UI back when shipping.
- **Performance:** providers run only at `hudUpdateHz`; reuse cheap/cached strings. The HUD rebuilds TMP text only when the content changes.
- **Migration:** move every per-game debug HUD onto `DevHud`, and delete OnGUI HUDs and custom HUD canvases (e.g. LWN sortingOrder 100 canvases). OnGUI always draws over the console.

## Command line (optional)

Off by default (`commandLine` on settings). When on, a TMP input appears at the **bottom of the rail** (or floating/dock panel). `DevConsole.RegisterCommand(name, help, args => …)` registers commands; Enter runs `name arg1 arg2` (quotes keep spaces); Up/Down walk history; built-in `help` lists commands; errors go through `DevConsole.Note`. While the field is focused, console hotkeys are ignored.

## Rail layout (default for new settings)

Modelled on the Lines of Mercy console: a slim **right-edge rail** that can stay open during gameplay without blocking. Section buttons open a large **section window** that does block.

| State | Blocks game? |
| --- | --- |
| Rail open (landscape) | No |
| Section window open | Yes (`BlocksGame == true`) |
| Rail collapsed to icon strip | No (unless a section window is open) |
| Portrait rail expanded | Yes — full-width sheet |
| Portrait collapsed strip | No (unless a section window is open) |

**−** shrinks the rail to a thin icon strip (`railCollapsedWidth`, default 36): one square button per section (sprite via `DevConsole.SetSectionIcon`, or the section’s first letter). Click an icon to open that section’s window beside the strip. **⟨** / **∨** expands back to the full rail. **×** closes the console. The floating **DEV** reopen button (`showReopenButton`, default on for mobile/touch, off on desktop) sits on the **console rail’s edge** (right / top of rail in landscape, bottom-right in portrait), themed from `DevConsoleTheme`, and never overlaps the DevHud.

**Gate gameplay on `BlocksGame`, not `IsOpen`.** Skip world clicks / raycasts when `IsPointerOverConsole` is true (pointer over rail, strip, or window).

```csharp
DevConsole.RegisterQuick("God mode", () => ToggleGod());
DevConsole.PinReadout("Perf", "FPS");
DevConsole.SetSectionIcon("Economy", economySprite);
DevConsole.OpenSection("Economy"); // opens rail if needed + window
// [DevButton("Bench", "Reset", Quick = true)]
```

Esc closes the window first, then the rail. `ConsumedCloseThisFrame` is set both times. `rememberState` restores rail collapsed/expanded via PlayerPrefs.

### Migration

Set `layout` to **Rail** in your `AmberScreenDevConsoleSettings` asset (or run Reset). Optionally mark 2–4 `[DevButton]` methods with `Quick = true`. Floating / DockRight / MobilePortrait still work unchanged.

## Font

UI text uses **Inter** (Regular + SemiBold), loaded from `Runtime/Resources/AmberScreenDevConsole/`, licensed under the [SIL Open Font License 1.1](Runtime/Resources/AmberScreenDevConsole/OFL.txt). TMP font assets are created at runtime from those TrueType files (or from `fontOverride` / the TMP default if Inter is missing). The Resources fonts add about **0.8 MB** to player builds.

## Setup

1. Add the package (Git URL or local `file:` path).
2. An editor script adds the scripting define `AMBERSCREEN_DEVCONSOLE` to the active build target if missing. Opt out via Dev Console settings → **Auto Add Define**.
3. Optional: create **Assets → Create → Amber Screen → Dev Console Settings** at `Resources/AmberScreenDevConsoleSettings`. Existing assets should run Reset (or update `buttonHeight` / `fontSize` / `spacing` by hand) after upgrading.
4. TMP Essential Resources remain a fallback if Inter fails to load from the package Resources folder.
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
| Esc | Rail: close window, then rail. Other layouts: close when blocking |
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
bool blocks = DevConsole.BlocksGame; // true while a section/log window is open, or the portrait sheet
DevConsole.OpenChanged += isOpen => { };
DevConsole.BlocksGameChanged += blocksGame => { };
```

`OpenChanged` fires only when open/closed changes (not on DockRight collapse).  
`BlocksGameChanged` fires whenever `BlocksGame` changes (open, close, collapse, expand).

## Portrait / phone scaling

In **portrait Rail** (`Screen.height > Screen.width`) and **MobilePortrait**, the canvas uses `ConstantPixelSize` with a dpi-based scale factor:

`max(dpi/160, height/960 fallback, 1.5) × ResolvedScaleFactor × portraitUiScale`, clamped to **1.5…4**.

That keeps text and touch targets readable on high-density phones (Device Simulator / device). Landscape scaling is unchanged. Tune with settings field `portraitUiScale` (default `1`, range `0.75…2`). Minimum touch height follows the same factor (at least **44 dp**).
