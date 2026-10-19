# Amber Screen Packages

Shared library of small [UPM](https://docs.unity3d.com/Manual/upm-ui.html) packages used by Amber Screen Studio games. Games pull packages in by Git URL. **A package must never reference a specific game.**

Unity version target: **6000.6.6f1**.

## Install a package

In a Unity project’s `Packages/manifest.json`, add a Git dependency with `path` and a version tag (or branch):

```json
{
  "dependencies": {
    "com.amberscreen.devconsole": "https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.devconsole#devconsole/0.2.0"
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
| `com.amberscreen.devconsole` | 0.2.0 | Runtime debug console (`AmberScreen.Dev`) |

See [DOCS/USAGE.md](DOCS/USAGE.md) for which games use which package versions, [DOCS/REGISTRY.md](DOCS/REGISTRY.md) for shared-module claims, and [DOCS/PROJECT_STATUS_HANDOFF.md](DOCS/PROJECT_STATUS_HANDOFF.md) for status.

## Sandbox

`Sandbox/` is a minimal Unity 6000.6.6f1 URP project for exercising packages locally.

### Test note — Dev Console

Open the Sandbox in Unity 6000.6.6f1: the Console must show no red errors. Open `Assets/Scenes/DevConsoleSandbox.unity`.

- In `Resources/AmberScreenDevConsoleSettings`, turn the DEV button on. Press Play and tap or click DEV BEFORE pressing F1: the console opens. (This is the case that failed before.)
- Close it, then press F1: it opens, Esc closes it. Gamepad Select+Start: the first button is selected, the d-pad moves and the list scrolls, A presses and B closes.
- FPS readout ticks, spin toggle, colour dropdown + Go, reset (second press), DevButton note, note history scrolls.
- Layout DockRight: collapse selects the expand button; while collapsed, Esc and B do nothing.
- Layout MobilePortrait in the Device Simulator: big single-column buttons inside the safe area, the DEV button is finger-sized, and a 2-second corner hold opens the console.
- Stop and press Play again: the console starts closed.
- Optional: make a non-development Windows build. The Console window shows the strip and restore lines, and the console doesn't open in the build. What won't work here: there's no game, so only the label reacts to input blocking; the 3-finger tap can only be tested on a phone (in Twin Ticks).
