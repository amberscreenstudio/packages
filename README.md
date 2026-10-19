# Amber Screen Packages

Shared library of small [UPM](https://docs.unity3d.com/Manual/upm-ui.html) packages used by Amber Screen Studio games. Games pull packages in by Git URL. **A package must never reference a specific game.**

Unity version target: **6000.6.6f1**.

## Install a package

In a Unity project’s `Packages/manifest.json`, add a Git dependency with `path` and a version tag (or branch):

```json
{
  "dependencies": {
    "com.amberscreen.devconsole": "https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.devconsole#devconsole/0.1.1"
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
| `com.amberscreen.devconsole` | 0.1.1 | Runtime debug console (buttons, toggles, readouts) |

See [DOCS/USAGE.md](DOCS/USAGE.md) for which games use which package versions, [DOCS/REGISTRY.md](DOCS/REGISTRY.md) for shared-module claims, and [DOCS/PROJECT_STATUS_HANDOFF.md](DOCS/PROJECT_STATUS_HANDOFF.md) for status.

## Sandbox

`Sandbox/` is a minimal Unity 6000.6.6f1 URP project for exercising packages locally.

### Test note — Dev Console

Open the Sandbox project in Unity 6000.6.6f1. Packages should resolve with no errors. Open `Assets/Scenes/DevConsoleSandbox.unity` and press Play.

- F1 opens the console with visible buttons, and the label shows IsOpen true. Esc closes it.
- Gamepad: Select+Start opens it with the first button already selected. The d-pad moves and the list scrolls to follow; A presses and B closes. No mouse needed.
- The FPS readout updates about twice a second. Toggle spin, pick a colour + Go, reset (second press), DevButton note; the note history scrolls.
- In `Resources/AmberScreenDevConsoleSettings`, turn on the DEV button: it shows while the console is closed and opens it when tapped.
- Set Layout to DockRight: collapse leaves the header with its expand button selected; while collapsed, Esc and B do nothing to the console.
- Set Layout to MobilePortrait and use the Device Simulator (any phone, portrait): big single-column buttons inside the safe area; a 2-second hold in the top-left corner opens it.
- Stop and press Play again: the console starts closed. What won't work in this scene: there's no real game, so only the label reacts to input blocking. The 3-finger tap can't be tested in the Editor (it's tested on a phone in Twin Ticks), and the release-build strip only shows in a non-development player build.
