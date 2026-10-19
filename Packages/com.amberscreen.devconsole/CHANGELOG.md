# Changelog

All notable changes to `com.amberscreen.devconsole` are documented here.

## [0.1.0] - 2026-10-19

### Added

- Initial package: floating / dock-right / mobile-portrait layouts.
- Public API: Register, RegisterToggle, RegisterDropdown, RegisterConfirm, RegisterReadout, Note, SetButtonCaption, SetSectionOrder, Unregister, ClearSection, Open/Close/Toggle, IsOpen, BlocksGame, OpenChanged.
- `[DevButton]` attribute and `IDevConsoleSectionProvider` discovery.
- Compile switch `AMBERSCREEN_DEVCONSOLE` (editor auto-define) and `Debug.isDebugBuild` gate.
- Domain-reload-off static reset via `SubsystemRegistration`.
- Sandbox scene and editor tests.
