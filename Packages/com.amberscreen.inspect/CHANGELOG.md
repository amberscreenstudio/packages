# Changelog

All notable changes to `com.amberscreen.inspect` are documented here.

## [0.2.0] - 2026-11-15

### Added

- `fitToBounds` with `fitHoldMultiplier` (3), `fitMinMultiplier` (1) and `fitMaxMultiplier` (8): hold, closest and furthest distance sized from the object's rendered size (largest half-extent of its enabled renderers). Off by default, so nothing changes until you turn it on. `InspectMath.RenderedExtent` and `InspectMath.FitDistances` do the maths.
- `DistanceChanged` (code) and `onDistanceChanged` (Inspector) events with the new hold distance, raised when inspecting starts, on zoom and on spot focus, so a depth-of-field band can follow the object.
- `InspectViewer.ConsumedCloseThisFrame` (true in the frame the viewer's own Esc / right-click / B ends inspecting) and `InspectViewer.AnyInspecting`, so a pause menu doesn't also react to the same Esc.
- `CurrentMinDistance` / `CurrentMaxDistance`.
- Demo: "Fit distances to object size" button and a distance-event readout.

## [0.1.0] - 2026-11-14 (verified 2026-11-14)

Verified in Sandbox (Unity 6000.6.6f1): EditMode tests green, inspect demo works.

### Added

- `InspectViewer`: `Inspect(Transform)` / `EndInspect()`, animated move to a hold point in front of the camera and back, drag/stick rotation, clamped zoom, next/previous marked spot, exact restore of parent, sibling order, pose, layers, rigidbodies and colliders.
- `InspectHotspot` (label, order, view direction, focus distance) and optional `Inspectable` marker.
- Events for player controls on/off, background focus (dim/blur is the game's job) and spot labels.
- Input through the old Input Manager, or the Input System when installed and active.
- Demo scene menu: Tools > AmberScreen > Inspect > Create Inspect Demo Scene.
