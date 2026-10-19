# Amber Screen Packages

Shared library of small [UPM](https://docs.unity3d.com/Manual/upm-ui.html) packages used by Amber Screen Studio games. Games pull packages in by Git URL. **A package must never reference a specific game.**

Unity version target: **6000.6.6f1**.

## Install a package

In a Unity project’s `Packages/manifest.json`, add a Git dependency with `path` and a version tag (or branch):

```json
{
  "dependencies": {
    "com.amberscreen.devconsole": "https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.devconsole#devconsole/0.1.0"
  }
}
```

Or via **Window → Package Manager → + → Add package from git URL…** and paste the same URL.

Local development against this repo (Sandbox):

```json
"com.amberscreen.devconsole": "file:../../Packages/com.amberscreen.devconsole"
```

## Packages

| Package | Version | Description |
| --- | --- | --- |
| `com.amberscreen.devconsole` | 0.1.0 | Runtime debug console (buttons, toggles, readouts) |

See [DOCS/USAGE.md](DOCS/USAGE.md) for which games use which package versions, and [DOCS/PROJECT_STATUS_HANDOFF.md](DOCS/PROJECT_STATUS_HANDOFF.md) for status.

## Sandbox

`Sandbox/` is a minimal Unity 6000.6.6f1 URP project for exercising packages locally.

### Test note — Dev Console

Open the Sandbox project, open the scene `Assets/Scenes/DevConsoleSandbox.unity`, and press Play.

- F1 opens the console and the label shows IsOpen true. Esc closes it.
- On a gamepad, Select+Start opens it. The d-pad navigates, A presses and B closes, with no mouse.
- Try every widget: spin toggle, colour dropdown + Go, reset (needs a second press), FPS readout, DevButton note.
- In `Resources/AmberScreenDevConsoleSettings`, set Layout to DockRight and check the collapse button (BlocksGame turns false). Then set it to MobilePortrait with a 1080x1920 Game view and check the layout.
- Stop and press Play again: the console starts closed. What won't work in this scene: there's no real game, so the only thing that reacts to input blocking is the label. The 3-finger tap can't be tested in the Editor (the Device Simulator has one finger, so test the corner hold there); the 3-finger tap gets tested on a phone in Twin Ticks.
