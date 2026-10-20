# Changelog

All notable changes to `com.amberscreen.devconsole` are documented here.

## [0.4.0] - 2026-10-20

### Added

- **Debug HUD rail** (`DevHud`): shared right-edge always-on HUD. Games register lines via `SetLine` / `ClearSection`; F2 toggles visibility. Own canvas under the console sorting order; throttled updates (`hudUpdateHz`); `GameRect` / `RailScreenRect` for layout.
- Settings: `hudShowOnStart`, `hudToggleKey`, `hudScreenFraction`, `hudMinWidth`, `hudMaxWidth`, `hudOpacity`, `hudFontSize`, `hudUpdateHz`, `hudPortraitMaxHeightFraction`. `rememberState` also stores HUD visibility.

### Changed

- Console windows and rails keep clear of the HUD rail (landscape and portrait). Non-Rail layouts clamp their right edge the same way.
- At 16:9 or narrower the HUD overlays the game; wider than 16:9 it prefers the right margin outside a centred 16:9 `DevHud.GameRect`.
- Fix: console rail keeps full width when the HUD is visible; wide layouts shrink the HUD into the margin when it fits; HUD refresh avoids per-tick sort/`ToString` GC.
- Fix: use `TMP_Text.textWrappingMode` instead of obsolete `enableWordWrapping` (CS0619).

## [0.3.4] - 2026-10-19

### Fixed

- When TextMeshPro Essential Resources are missing, log a clear error and keep the host alive instead of failing silently on F1 (NRE in `CreateFontAsset`).
- Qualify `UnityEngine.Object` after adding `using System;` (CS0104 ambiguous `Object`).
- `InternalsVisibleTo` for `AmberScreen.DevConsole.Editor` so the TMP-missing prompt can reach `DevConsoleHost` (CS0122).

## [0.3.3] - 2026-10-19

### Fixed

- Section window now draws above the rail in portrait.

## [0.3.2] - 2026-10-19

### Fixed

- Added missing `DevConsoleRailLayoutTests.cs.meta`.
- README version / install tag / `BlocksGame` comment.
- Portrait and MobilePortrait use dpi-based `ConstantPixelSize` scaling (with `portraitUiScale`) so phone UI stays readable.

## [0.3.1] - 2026-10-19

### Fixed

- Rail content width, section window sized on open, layout containment tests.

## [0.3.0] - 2026-10-19

### Added

- **Rail** layout: right-edge column that stays open without blocking the game; section buttons open a large blocking window.
- API: `RegisterQuick`, `UnregisterQuick`, `PinReadout`, `OpenSection`, `CloseSection`, `OpenSectionName`, `IsRailOpen`, `IsPointerOverConsole`.
- `[DevButton(..., Quick = true)]` also appears in the rail quick grid.
- Settings: `railScreenFraction`, `railMinWidth`, `railMaxWidth`, `railEdgeMargin`, `windowMargin`, `rememberState` (PlayerPrefs).
- Sandbox: Rail defaults, quick/pin demos, Density fillers, tiny game-input label for blocking.

### Changed

- Default layout for new settings / Reset is **Rail**. Existing game assets keep their layout.
- Gate gameplay on `BlocksGame` (not `IsOpen`); skip world clicks when `IsPointerOverConsole` is true.
- Esc closes the section window first, then the rail. `pauseTimescaleWhileOpen` applies only while a window is open (Rail).

## [0.2.5] - 2026-10-19

### Fixed

- Test asmdef references TextMeshPro (and UnityEngine.UI for layout tests).

## [0.2.4] - 2026-10-19

### Fixed

- Title bar fixed height; chrome buttons are square and subtle (not tall blue action buttons).
- History toggle removed; history strip shows only when there are notes (max 4 lines).
- Inter loaded from package `Runtime/Resources/AmberScreenDevConsole/` via `Resources.Load` (no settings-asset refs).
- Logs `[DevConsole] Font: …` once when the console first opens.

## [0.2.3] - 2026-10-19

### Changed

- Compact defaults (`buttonHeight` 30, `fontSize` 16, `spacing` 4).
- Bundled Inter font (SIL OFL 1.1); SemiBold for title/section headers, Regular elsewhere.
- Floating / DockRight use screen-fraction panel sizing; landscape canvases use ConstantPixelSize scaled from 1080p height.
- Auto columns via `autoColumns` / `minEntryWidth`; MobilePortrait 2 columns when safe width ≥ 360 dp.
- Close moved to the title bar as **X**; history capped at 4 lines with a collapsible chevron.

### Added

- Settings: `floatingScreenFraction`, `dockRightScreenFraction`, `autoColumns`, `minEntryWidth`, `bundledRegular`, `bundledSemiBold`.

## [0.2.2] - 2026-10-19

### Fixed

- NullReferenceException building the collapsed header left an empty, unclosable panel on screen at startup.
- `BuildCollapsedHeader`: removed duplicate `AddImage` on the same GameObject as `AddButton` (Unity allows one Graphic per object).
- `DevConsoleUiUtil.AddImage` / `AddButton`: reuse existing Image; error and return null if another Graphic is present; null-check before use.
- `DevConsoleHost.Bootstrap`: catch UI build failures, log, destroy half-built canvases, leave console closed.

## [0.2.1] - 2026-10-19

### Fixed

- AssignDefaultActions on runtime-created InputSystemUIInputModule; warn on non-Input-System scene modules.

## [0.2.0] - 2026-10-19

### Changed

- Namespace renamed from `AmberScreen.DevConsole` to `AmberScreen.Dev` (MAJOR: compile fix for `AmberScreen.*` consumers; package/asmdef names unchanged).
- Replaced obsolete `FindFirstObjectByType` with `FindAnyObjectByType`.

### Fixed

- EventSystem ensured when the console or on-screen DEV button is shown; owned EventSystem cleaned up when neither needs it; reconcile via `FindObjectsByType`, not per frame.
- DEV button canvas uses matching CanvasScaler and dp sizing (min 48 dp) inside safe area.
- No per-frame UI work while closed; settings cached on the host.
- Selection: move off buttons about to become non-interactable; restore Close/Collapse after rebuild; scroll velocity cleared after follow-scroll.
- Header / padding / status sizes applied on first open after CanvasScaler updates.

### Docs

- Release-build define strip notes (Build Profiles, `Debug.isDebugBuild`, strip/restore log lines).

## [0.1.2] - 2026-10-19

### Fixed

- Fix duplicate TestRunner references in the test asmdef.

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
