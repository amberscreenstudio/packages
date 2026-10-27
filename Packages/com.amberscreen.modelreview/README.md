# Amber Screen Model Review

**Version:** 0.1.0 · **Unity:** 6000.6 · **Owner:** Packages

Ideal-conditions review scene for 3D deliveries: tuned URP shadows, neutral studio, orbit/interior camera, clay/wireframe views, and a generic read-only material report. Never modifies the reviewed asset.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.modelreview#modelreview/0.1.0
```

URP 17.6 is required; the host project should be URP.

Local Sandbox: `"com.amberscreen.modelreview": "file:../../Packages/com.amberscreen.modelreview"`.

## Use

1. **Tools > Amber Screen > Model Review > Open Review Scene**
2. Right-click a prefab/model → **Amber Screen > Review in Model Review**, or drop it on the rig inspector / Scene view.
3. Keys (Play): `1` Delivered · `2` Clay · `3` Wireframe · `F` frame · `I` interior · `R` reset · `G` grid · `C` capsule · `T` tonemap · `H` legend
4. **Tools > Amber Screen > Model Review > Material Report** for a Console + window report.

## Read-only guarantee

The package never changes an asset's import settings, materials, textures, or prefabs. Pipeline switching uses an in-memory clone of `ModelReview_URP` and restores the host pipeline without writing ProjectSettings. The review scene itself is excluded from saves while switched.

## Known limits

- Wireframe needs geometry shaders (DX11/12, Vulkan; not Metal); otherwise it falls back to Clay.
- A Built-in host without URP global settings runs under the host pipeline (not tuned); the legend shows **HOST**.
- Reflections come from a realtime probe, not baked.

## Editor tips

Game view Scale **1x**, **Low Resolution Aspect Ratios** off.
