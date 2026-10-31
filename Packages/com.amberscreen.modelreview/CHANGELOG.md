# Changelog

## [0.1.3] - 2026-10-31

### Added

- Asset browser: gather prefabs/models under Assets (folder or whole project), one target at a time with wrap-around Next/Previous, multi-selection Review, on-screen list with search, Left/Right / `[` `]` / shoulders.
- Tone cycle Off → Neutral → ACES (T), via an in-memory Volume profile clone (package volume asset untouched). ACES adds a light contrast/exposure bump so it reads clearly in the grey studio.
- Legend readouts: size in metres, triangles (including skinned meshes), up-axis and scale warnings; plain-word controls.
- Package-owned gamepad actions (orbit, pan, zoom, reset, frame, view mode, interior, legend, previous/next).
- Waterline plane (W outside interior mode) with draft readout.
- Material Report WARN when `ModelImporter.materialImportMode` is None.
- Camera exterior pitch clamp (-15..75), zoom limits scaled to bounds, frame padding 1.35.

### Changed

- Opening a browse list always lands on one `ModelReview_Target`; swaps restore materials before destroying the previous preview.

## [0.1.2] - 2026-10-28

### Fixed

- Open Review Scene works from git-URL installs. It builds the studio (camera rig, lights, volume, probe, floor, grid, capsule, render settings) in a new untitled scene from package assets instead of opening the package scene, which Unity can't open from an immutable package. The scene is detected by `ModelReviewSceneMarker`. Close returns to the previous scene without a save prompt. Nothing is saved or written to Assets.
- T (neutral tonemap) uses `ModelReview_Volume` (the 0.1.1 scene's Volume had no profile).

### Changed

- Scroll zoom moves a fixed fraction of the current distance per notch (`zoomSpeed`, default 0.15).

### Removed

- `Scenes/ModelReview.unity` (studio is built in code).

## [0.1.1] - 2026-10-28

### Fixed

- Only one preview instance exists at a time; it lives in the review scene and is destroyed on Close Review Scene, opening another scene, play-mode enter/exit, assembly/domain reload and quit (0.1.0 stacked DontSave copies, so Clay/Wire left legacy-shader parts magenta and the preview leaked into the next scene).
- Clay and Wireframe cover every renderer and slot (legacy/error shaders, null slots, multi-material, skinned) and restore the exact original references.
- Grid shader compiles under URP (reserved identifier `line`), grid lines show on the floor. ClayWire shadow pass no longer depends on LitInput/`ShadowCasterPass.hlsl`.

### Changed

- Right-drag pans (middle-drag and Shift+left-drag still pan). The legend spells the controls out in plain words.

## [0.1.0] - 2026-10-27

### Added

- Ideal-conditions Model Review scene with tuned URP profile (switched in memory only).
- Studio key/fill/rim lights, orbit and interior camera, scale capsule and world grid.
- Delivered / Clay / Wireframe view modes (sharedMaterials only; originals restored exactly).
- Read-only material report (import compare, missing materials, non-URP shaders, broken textures).
- Menus: Open/Close Review Scene, Review in Model Review, Material Report, maintainer asset builder.
