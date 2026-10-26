# AmberScreen.LLMChat (`com.amberscreen.llmchat`)

Shared module for in-character chat replies with seen / typing timing. Namespace `AmberScreen.LLMChat`. Package-ready — no host game, PhoneUI, singletons, or tags.

Layout: `Runtime/` (asmdef `AmberScreen.LLMChat`), `LLMUnity/` (`AmberScreen.LLMChat.LLMUnity`), `Tests/Editor/`.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.llmchat#llmchat/0.2.0
```

LLMUnity (`ai.undream.llm`) is optional and only needed for `LLMUnityChatBackend`. Without it, the LLMUnity asmdef skips silently (`versionDefines` / `defineConstraints`).

Owner: Twin Ticks. Changes go through Packages.

## Purpose

- Drive several character personas from one thin LLM backend.
- Own a rolling history window and optional director instruction string.
- Emit UI-agnostic events: **Seen**, **TypingStarted**, **Chunk**, **Done**, **Error**.
  (`Chunk` fires once with the final accepted text; token streaming was removed.)

## Setup

1. Add asmdefs (already present):
   - `AmberScreen.LLMChat` — core types, no LLMUnity reference.
   - `AmberScreen.LLMChat.LLMUnity` — only `LLMUnityChatBackend` (references `undream.llmunity.Runtime`).
     Compiles only when package `ai.undream.llm` is present (`versionDefines` → `AMBERSCREEN_LLMUNITY` + `defineConstraints`). Without LLMUnity the core assembly still builds; hosts supply another `ILLMChatBackend`.
2. Create `LLMChatPersona` and `LLMChatTimingSettings` assets (Create → Amber Screen → LLM Chat).
3. Scene / runtime:
   - One `LLM` + `LLMAgent` (chat model) when using the LLMUnity backend.
   - `LLMUnityChatBackend` bound to that agent (or a custom backend).
   - One `LLMChatRunner` per character, `Configure(backend, persona, timings)`.

## Interfaces

| Type | Role |
| --- | --- |
| `ILLMChatBackend` | `GenerateAsync(prompt, options, onChunk, ct)` |
| `LLMChatRunner` | Prompt build + timing events |
| `LLMChatPersona` | Display name, system prompt, style, examples, max length |
| `LLMChatTimingSettings` | Seen delay, typing from length, jitter |
| `LLMChatHistoryTurn` | Shared history line |

Only `LLMUnity/LLMUnityChatBackend.cs` may reference LLMUnity.

## Example

```csharp
backend.Bind(chatAgent);
runner.Configure(backend, personaAsset, timings);
runner.Seen += id => /* show seen */;
runner.TypingStarted += id => /* show typing */;
runner.Done += (id, text) => /* deliver bubble */;

await runner.RunReplyAsync(history, "React casually to the last message.", ct);
```

## Model ladder

`LLMChatBackendSettings` holds an ordered `ModelLadder` (preferred first) and `ReplyBudgetSeconds` (default 10).

`ModelLadder` (pure C#, no LLMUnity) decides when to step down:

1. **Warmup** — host runs a short generation on the preferred model at startup. If `generateMs` exceeds the budget, step to the next entry and log `[LLMChat] model step-down A -> B (ms=…)`.
2. **First 3 replies** — if the median wall time exceeds the budget, step down the same way.

The host owns loading/swapping weights from the model store. Steam Deck / CPU-only builds are expected to land on a smaller rung.

EditMode coverage: `Tests/Editor/ModelLadderTests.cs` (fake timer via injected ms values).

## Prompt shape

- History lines are `Name: text`. The player uses their in-game name (never `You`).
- System prompt leads with: `You are <Friend>. Write ONE message as <Friend> only. Never write your own name or other people's lines.`
- Qwen3 thinking is disabled with `/no_think`; `<think>` blocks are stripped.
- Validation retry hints are inserted **before** `/no_think` and counted in the token budget via `FitPromptToBudget(..., reserveExtraTokens)`.
- Post-process strips a leading `<Name>:` and cuts at the first `\n<Name>:`.
- Dev toggle `LLMChatRunner.LogPrompts` logs the full final prompt once per friend.

## Reply validation

`LLMChatReplyValidator` runs after sanitize, before Done:

- Empty / emoji-only → Error reason `empty_reply` / `emoji_only`
- Roleplay / markdown (`*…*`, `**…**`, leading `*` `_` `#`) → `roleplay_markup` (paired `_…_` is **not** matched — snake_case is allowed)
- Near-identical to one of that speaker's last **6** lines (normalised Dice ≥ **0.85**) → `self_repeat`
- First word matches ≥ **2** of that friend's last **3** openers → `repeat_opener`

On validation fail the runner **retries once** with a hint naming the reason; `Chunk` is only raised for an accepted reply. After two failures the host gets `Error` and should fall back.

## Example picker

`LLMChatExamplePicker.Pick(lines, tags, beatTension)` chooses **6–8** samples for the prompt:

- `ExampleLineTags` (parallel to `ExampleLines`): empty, `tell`, `low`, `mid`, `high`
- At mid/high tension, prefer `tell` (and matching tension) tags
- Never two samples with the same normalised opener in one pick

Set `LLMChatRunner.BeatTension` from the host beat. Optional `KnowledgeRulesHint` **replaces** the generic known-facts / secrets hard-rule wording when set.

## Sampling defaults

Logged once at first generate: `temp=0.8 top_p=0.9 repeatPenalty=1.15 repeatLastN=64`.

## Package readiness

- Namespace `AmberScreen.LLMChat`. Version **0.2.0** (`package.json`).
- No game story, PhoneUI, or classifier references.
- Weights / `.gguf` stay outside git; the host project configures models.
