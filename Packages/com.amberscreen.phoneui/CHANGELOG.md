# Changelog

## [0.2.1] - 2026-10-26

### Changed

- Moved into amberscreenstudio/packages from Twin Ticks 0.0.38 (bdbb8e6); GUIDs unchanged.

### Fixed

- Circle-clip material lives in `Runtime/Resources/PhoneUI/CircleClip.mat` so the shader survives player builds (`Resources.Load` first; runtime `Shader.Find` fallback keeps `HideAndDontSave`).
- Atlas-safe circle: `CircleClipMeshModifier` writes normalised position into UV1; shader crops from TEXCOORD1 while sampling TEXCOORD0 (one shared material, batching intact).
- Soft anti-aliased circle edge via `fwidth` + `smoothstep` (`#pragma target 3.0`).
- Font cache: `InvalidateFontCache` from theme `SetOverride` / `ResetStatics`; UiFactory `ResetStatics` clears sprites/material/font on SubsystemRegistration.

### Added

- PackageInfo EditMode test (`com.amberscreen.phoneui` / `0.2.1` / Input System + uGUI deps).
- Font-cache invalidate test (theme override → DefaultFont → clear).

## [0.2.0] - 2026-10-24

### Added

- Image attachments (`ChatImageAttachment`) and in-phone `ImageMessageViewer`.
- `PhoneUITheme` ScriptableObject covering shell / home / messaging / status / banner colours + optional `PhoneFontSettings` ref (builtin default = prior hard-coded look). `Reset()` applies builtins so Create Asset menu themes are not all-zero.
- `UiFactory.DefaultFont` reads `PhoneUITheme.FontSettings` before `Resources/PhoneUI/PhoneFontSettings`.
- Shared circle-clip material for avatars (no per-avatar stencil Mask).
- Back/Escape closes the image viewer (after host DevConsole Esc via `PhoneInputGate.IgnoreBack`).
- `ImageViewerEscCloser.ListenForEscape` — false when MessagingApp owns Back (prevents one Esc closing viewer **and** leaving the thread).
- Packed-sprite UV helper `ChatImageAttachment.SpriteUvRect` (tight-pack safe).
- Circle / rounded-square masked image helpers on `UiFactory` for avatars and app icons.
- Package layout: `Assets/Shared/PhoneUI` with `Runtime/`, `Tests/`, `package.json` (depends on Input System + uGUI 2.0.0).

### Changed

- MessagingApp refreshes player receipt tick glyphs when the host upgrades Sent → Delivered → Read.
- MessagingApp shows contact / player avatars on the thread list, header, and bubbles when `ContactInfo.Avatar` is set.
- README and `MessageReceipt` docs are host-neutral (no game-specific cast rules).
- `PhoneUITheme.Active` caches Resources lookup; statics reset on domain reload.
