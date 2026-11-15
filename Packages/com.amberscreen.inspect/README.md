# AmberScreen Inspect

Close-up inspection viewer: pick up an object, look at it in front of the camera, turn it, zoom, step through marked spots, then put it back exactly where it was. Assembly `AmberScreen.Inspect`, namespace `AmberScreen.Inspect`. No UI, no render-pipeline parts.

**Version:** 0.2.0
**Unity:** 6000.0+
**Owner:** Packages.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.inspect#inspect/0.2.0
```

## Quick start

1. Add `InspectViewer` to your camera (or anywhere, and assign `viewCamera`).
2. Hook `onPlayerControlsEnabled` to your player: `false` = turn movement/look off, `true` = back on. (Or set `PlayerControlsCallback` from code.)
3. From your interaction code call `viewer.Inspect(item.transform)`. The player then:
   - drags with the left mouse button or uses the right stick to turn it,
   - uses the mouse wheel or the triggers to zoom (between `minDistance` and `maxDistance`),
   - presses Tab / Shift+Tab or RB / LB to step through marked spots,
   - presses Esc, right-click or B to put it back (or you call `EndInspect()`).
4. Optional: add `InspectHotspot` children at interesting spots (label, order, focus distance). Optional `Inspectable` marker gives a per-object hold distance and display name.

Sizing by object: tick `fitToBounds` and the hold, closest and furthest distances become the object's largest half-size times `fitHoldMultiplier` (3), `fitMinMultiplier` (1) and `fitMaxMultiplier` (8). Good for parts of very different sizes. An `Inspectable.holdDistance` above 0 still sets the hold distance.

Depth of field: listen to `DistanceChanged` (or `onDistanceChanged`) and set your focus distance to the value. It fires on start, zoom and spot focus.

Esc and pause menus: in your pause menu, ignore Esc while `InspectViewer.AnyInspecting` or `InspectViewer.ConsumedCloseThisFrame` is true, so one Esc puts the object back without also opening the pause menu.

Keep the camera's near clip plane well below `minDistance` minus the object's size (for example 0.03), or close-up objects get cut.

## While an object is inspected

- It is parented to a pivot under the camera, centred on its renderers' bounds.
- Its rigidbodies are kinematic and its colliders off (both optional), and it can be moved to `inspectLayer` (-1 = leave layers).
- `onBackgroundFocus(true)` fires: dim or blur the background yourself if you want (for example a UI panel, or a second camera that renders only `inspectLayer`).
- On exit everything is restored: parent, sibling order, local position/rotation/scale, every layer, kinematic flags, collider states.
- Uses unscaled time, so it works with the game paused.

## Public API (`InspectViewer`)

| Member | Role |
|--------|------|
| `Inspect(Transform)`, `EndInspect(immediate)` | Start / finish |
| `Rotate(Vector2 degrees)`, `Zoom(metres)`, `SetDistance` | Drive it from your own input (`handleInput = false`) |
| `NextHotspot()`, `PreviousHotspot()`, `FocusHotspot(i)`, `ClearHotspotFocus()` | Marked spots |
| `IsInspecting`, `Current`, `Distance`, `Hotspots`, `FocusedHotspot`, `CurrentPhase` | State |
| UnityEvents: `onInspectStarted`, `onInspectEnded`, `onPlayerControlsEnabled(bool)`, `onBackgroundFocus(bool)`, `onHotspotLabel(string)` | Inspector wiring |
| C# events: `InspectStarted`, `InspectEnded`, `HotspotFocused(spot or null)` | Code wiring |

## Input

Uses the Input System when `com.unity.inputsystem` is installed and active in Player settings; otherwise the old Input Manager (mouse and keyboard only, no gamepad). Set `handleInput = false` to drive everything from your own bindings.

## Stays in games

What can be inspected, the prompt to pick it up, the text shown for a spot, any background dim/blur, and game-only actions on an inspected object.

## Demo

Tools > AmberScreen > Inspect > Create Inspect Demo Scene, then Play and click a shape on the table.
