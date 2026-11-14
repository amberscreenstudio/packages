# Changelog

All notable changes to `com.amberscreen.inspect` are documented here.

## [0.1.0] - 2026-11-14 (verified 2026-11-14)

Verified in Sandbox (Unity 6000.6.6f1): EditMode tests green, inspect demo works.

### Added

- `InspectViewer`: `Inspect(Transform)` / `EndInspect()`, animated move to a hold point in front of the camera and back, drag/stick rotation, clamped zoom, next/previous marked spot, exact restore of parent, sibling order, pose, layers, rigidbodies and colliders.
- `InspectHotspot` (label, order, view direction, focus distance) and optional `Inspectable` marker.
- Events for player controls on/off, background focus (dim/blur is the game's job) and spot labels.
- Input through the old Input Manager, or the Input System when installed and active.
- Demo scene menu: Tools > AmberScreen > Inspect > Create Inspect Demo Scene.
