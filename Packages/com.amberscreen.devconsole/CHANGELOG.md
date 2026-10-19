# Changelog

All notable changes to `com.amberscreen.devconsole` are documented here.

## [0.1.1] - 2026-10-19

### Fixed

- F1 open key (settings asset had RightMeta / 58; now Key.F1 = 94).
- Null-safe TMP font lookup with Resources fallback and one-time warning; view still builds without a font.
- Readout widgets refresh while open (`m_Readouts` wiring).
- On-screen DEV button uses its own canvas (visible while console is closed).
- Registry dirty flag: at most one rebuild per frame while open; selection restored after rebuild.
- MobilePortrait sizes in real dp; safe-area updates while open; single column.
- Esc / gamepad B ignored while DockRight is collapsed; `ConsumedCloseThisFrame` when the console handles close.
- Corner hold: touch-only (optional Editor mouse); opens only; uses safe-area corner.
- 3-finger tap: begin/end windows + movement threshold; fires on release.
- Collapsed header selects expand button; expand restores first entry.
- Scroll follows selection changes.
- Floating width from settings (height fits content, max ~80% screen); DockRight width scales.
- Note history scroll shows all 50 lines (newest first).
- ASCII section glyphs (`v` / `>`) for LiberationSans.
- Default Medium sizes: font 22, button height 44.
- EventSystem created on open under the host when missing.
- `SetCollapsed` no longer fires `OpenChanged`; added `BlocksGameChanged`.
- Availability / toggle refresh while open (0.25 s) without rebuild.
- Release builds strip `AMBERSCREEN_DEVCONSOLE` unless Keep in release builds is on.
- Discovery skips system/Unity assemblies; `[Preserve]` on `DevButtonAttribute`.
- `DestroyImmediate` for runtime-default settings outside Play mode.

### Docs

- IL2CPP Preserve / link.xml notes; re-register resets caption to label; events documented.

## [0.1.0] - 2026-10-19

### Added

- Initial package: floating / dock-right / mobile-portrait layouts.
- Public API: Register, RegisterToggle, RegisterDropdown, RegisterConfirm, RegisterReadout, Note, SetButtonCaption, SetSectionOrder, Unregister, ClearSection, Open/Close/Toggle, IsOpen, BlocksGame, OpenChanged.
- `[DevButton]` attribute and `IDevConsoleSectionProvider` discovery.
- Compile switch `AMBERSCREEN_DEVCONSOLE` (editor auto-define) and `Debug.isDebugBuild` gate.
- Domain-reload-off static reset via `SubsystemRegistration`.
- Sandbox scene and editor tests.
