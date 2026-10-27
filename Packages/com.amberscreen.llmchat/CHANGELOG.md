# Changelog

## [0.2.1] - 2026-10-27

### Added

- `cross_repeat` validation (last 8 other-speaker lines, Dice ≥ 0.8 or containment, ≥ 3 words).
- `LLMChatRunner.LiveHistoryProvider` (validate and retry against live thread history; final cross-speaker check before Done → Error `cross_repeat`).

## [0.2.0] - 2026-10-26

### Changed

- Moved into amberscreenstudio/packages from Twin Ticks 0.0.38 (bdbb8e6); GUIDs unchanged.

### Notes (from Twin Ticks 0.0.38)

- Validation retry: hint trimmed via `FitPromptToBudget(..., reserveExtraTokens)` (inserted before `/no_think`, counted in the token budget).
- Streaming removed: `Chunk` fires once with the final accepted text; partial `onChunk` callbacks are ignored.
- Neutral test ids (no cast / game character names).

## [0.2.0] - 2026-10-24

### Added

- `LLMChatExamplePicker` + `LLMChatPersona.ExampleLineTags` (tension / tell weighting; no duplicate openers in one pick).
- `LLMChatRunner.KnowledgeRulesHint` so hosts can supply story-specific hard-rule wording (**replaces** the generic known-facts / secrets lines when set).
- Package `package.json` / CHANGELOG.
- `LLMUnity` asmdef `versionDefines` on `ai.undream.llm` → `AMBERSCREEN_LLMUNITY` so the backend compiles out without LLMUnity.

### Changed

- Validation retry: hint is inserted **before** `/no_think`, counted in the token budget, and the prompt is trimmed the same way when the hint would exceed the budget.
- **Streaming removed:** `Chunk` fires once with the final accepted text; partial `onChunk` callbacks are ignored (`heldChunk` removed).
- Hard-rules default wording is host-neutral ("known facts" / "secrets"); story-specific phrasing belongs on the host via `KnowledgeRulesHint`.
- `roleplay_markup` no longer matches paired `_..._` (snake_case false positives). Leading `*` / `_` / `#` and `*…*` / `**…**` still fail.
- Layout: sources under `Runtime/`; docs and tests are host-neutral (no cast ids).
