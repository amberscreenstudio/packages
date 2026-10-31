# Amber Screen Model Review

**Version:** 0.1.3 · **Unity:** 6000.6 · **Owner:** Packages

Ideal-conditions review scene for 3D deliveries: tuned URP shadows, neutral studio, orbit/interior camera, clay/wireframe views, asset browser, tone cycle, and a generic read-only material report. Never modifies the reviewed asset.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.modelreview#modelreview/0.1.3
```

URP 17.6 is required; the host project should be URP.

Local Sandbox: `"com.amberscreen.modelreview": "file:../../Packages/com.amberscreen.modelreview"`.

## Use

1. **Tools > Amber Screen > Model Review > Open Review Scene** (creates a temporary untitled scene; works from git installs).
2. Browse models:
   - Right-click a folder → **Amber Screen > Review Folder in Model Review**
   - **Tools > Amber Screen > Model Review > Browse Folder…** or **Browse Whole Project**
   - Multi-select prefabs/models → **Amber Screen > Review in Model Review**
3. The right-hand list (when more than one item) filters by search; Previous/Next and Left/Right arrows wrap. Only one `ModelReview_Target` exists at a time.
4. Controls (plain words on the legend):
   - Drag with the left mouse button: orbit. Right button or scroll-wheel drag: pan. Scroll wheel: zoom.
   - F: frame. R: reset camera. 1 / 2 / 3: delivered / clay / wireframe.
   - I: fly inside (W A S D, Q E). G: grid. C: 1.8 m capsule. W: waterline (outside). T: tone Off/Neutral/ACES. H: hide legend.
   - Gamepad: right stick orbit, left stick pan, triggers zoom, shoulders previous/next.
5. **Tools > Amber Screen > Model Review > Material Report** for a Console + window report.

## Read-only guarantee

The package never changes an asset's import settings, materials, textures, or prefabs. Pipeline switching uses an in-memory clone of `ModelReview_URP` and restores the host pipeline without writing ProjectSettings. The review scene is a temporary untitled scene and is never saved. Tone uses a runtime Volume profile clone only.

## Known limits

- Wireframe needs geometry shaders (DX11/12, Vulkan; not Metal); otherwise it falls back to Clay.
- A Built-in host without URP global settings runs under the host pipeline (not tuned); the legend shows **HOST**.
- Reflections come from a realtime probe, not baked.
- The preview is a temporary copy; it never stays in other scenes.
- Opening the review scene replaces the open scenes (Single mode); Close returns to the scene you came from.
- While flying inside, W moves forward (waterline is unavailable).

## Editor tips

Game view Scale **1x**, **Low Resolution Aspect Ratios** off.
