# Changelog

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
