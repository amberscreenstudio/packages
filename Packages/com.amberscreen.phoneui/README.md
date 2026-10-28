# AmberScreen.PhoneUI (`com.amberscreen.phoneui`)

**Version: 0.2.3**

Shared phone chrome for portrait messaging games. Portrait stage, status bar, home, apps, nav. Namespace `AmberScreen.PhoneUI`.

Layout: `Runtime/` (asmdef `AmberScreen.PhoneUI`), `Tests/Editor/`, `package.json`.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.phoneui#phoneui/0.2.3
```

Owner: Twin Ticks. Changes go through Packages.

## Sharpness

PhoneAspectFitter/PhoneShell keep the 360 dp stage at Constant Pixel Size × uniform localScale. There is no RenderTexture or world-space canvas, so nothing renders at low resolution and then gets upscaled. TMP fallbacks use runtime `CreateFontAsset` defaults (90 pt sampling, 1024 atlas).

- Host art should be imported with mipmaps and a max size of at least about 2× the largest on-screen dp × stage scale.
- Runtime textures shown in RawImage thumbnails should have mips.
- A stage scale below 1 (small Game view) makes 11–13 dp text soft. Check it with `PhoneShell.ScaleOverlayEnabled`.

## Theme resolution order

1. `PhoneUITheme.SetOverride(theme)` if set (tests/host).
2. Otherwise `Resources.Load<PhoneUITheme>("PhoneUITheme")`: the game's own asset at any `Resources/PhoneUITheme.asset` (e.g. Twin Ticks `Assets/Game/Resources/PhoneUITheme.asset`). It is loaded once and cached; the cache resets on SubsystemRegistration.
3. Otherwise the built-in defaults (`ApplyBuiltinDefaults`, generic app ids `messages`, `app_a`, `app_b`, `app_c`).

Notes:

- The package never ships a `Resources/PhoneUITheme` of its own (that would clash with games), and a menu-created theme starts from the built-ins via `Reset()`.
- Fonts: theme `FontSettings`, then `Resources/PhoneUI/PhoneFontSettings`, then TMP default.
- The optional Noto emoji/symbol fallbacks are game-side Resources.
- A theme change affects UI built **after** the change; already-built screens keep their font and colours until rebuilt. Call `UiFactory.InvalidateFontCache()` (also done from `SetOverride` / theme `ResetStatics`) so the next `DefaultFont` read picks up the new theme.
- The avatar circle needs UV1 on the canvas (`CircleClipMeshModifier` sets `AdditionalCanvasShaderChannels.TexCoord1` on the root canvas).

## Screen content root

`PhoneShell.ScreenContentRoot` is the rect **inside the phone frame**, below the status bar and above the nav bar — the same host apps use when opened.

Use it to parent fullscreen overlays (e.g. a login screen) so they scale and clip with the phone. Do not parent overlays to the outer canvas.

```csharp
shell.BeginContentOverlay();
var login = YourLogin.Show(shell.ScreenContentRoot, onLoggedIn);
// …
shell.EndContentOverlay(); // returns to home
```

Draw order inside the phone screen: home → registered screen overlays (e.g. loading) → `ScreenContentRoot` (apps / login) → status / nav. Register mid-screen chips with `RegisterScreenOverlay` so content overlays stay above them.

`PhoneShell.PhoneScreenRoot` is the full safe-area screen (bezel inset) for layout tests.

## Image attachments (0.2.0)

`ChatMessage.Image` is an optional `ChatImageAttachment` (`Texture2D` / `Sprite`, file name, aspect). MessagingApp draws a framed thumbnail bubble with the file name; tap opens `ImageMessageViewer` full-screen under `ScreenContentRoot` with `listenForEscape: false` so MessagingApp owns Back (one Esc closes the viewer only; a second Esc returns to the thread list). Standalone viewers can leave `listenForEscape` true. If the host DevConsole consumes Esc first, `PhoneInputGate.IgnoreBack` keeps the viewer open for that frame.

**Packed sprites:** set `Sprite` on the attachment; thumbnails and the viewer apply `ChatImageAttachment.SpriteUvRect` so atlas sub-rects display correctly. Full-texture sprites and raw `Texture2D` use UV (0,0)–(1,1). The viewer letterboxes the image at `ResolvedAspect` (0.2.3).

No Gallery save yet.

## Receipt ticks

Player bubbles show Sent / Delivered / Read glyphs. The host owns upgrade rules; MessagingApp refreshes glyphs when `ThreadsChanged` fires after the host mutates `ChatMessage.Receipt`.
