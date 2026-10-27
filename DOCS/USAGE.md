# Package usage by game

## Package versions per game

| Game | Repo | com.amberscreen.devconsole | com.amberscreen.meters | com.amberscreen.economy | com.amberscreen.phoneui | com.amberscreen.llmchat | com.amberscreen.modelreview |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Twin Ticks | amberscreenstudio/Twin-Ticks | `devconsole/0.3.1` (Twin Ticks 0.0.18), bumping to 0.3.3 | none | none | `#phoneui/0.2.1` (0.0.39), 0.2.2 next | `#llmchat/0.2.0` (0.0.39), 0.2.1 next | none |
| Sand & Ink | amberscreenstudio/Sand-and-Ink | switching to `devconsole/0.3.3` (S&I 0.1.9) | adopting for drinking water | planned | none | none | planned |
| Snorkel & Sail | (local) | not yet (own console) | switching from `Assets/Shared/Meters` | planned | none | none | none |
| LWN | (local) | not yet (own console) | none | switching from `Assets/Shared/Economy` | planned | likely | planned |
| The Watchmaker Shop | local, `E:\Unity Projects\Time & Tension` | not yet (own console) | none | planned | none | none | none |

Install URL format: `https://github.com/amberscreenstudio/packages.git?path=/Packages/<package>#<short>/<version>`, for example `#devconsole/0.3.3`, `#meters/0.1.0`, `#economy/0.1.0`, `#phoneui/0.2.2`, `#llmchat/0.2.1`, or `#modelreview/0.1.1` (once tagged). Always pin a tag, never `main`.

## com.amberscreen.modelreview quick start

1. Add the package (local Sandbox: `"com.amberscreen.modelreview": "file:../../Packages/com.amberscreen.modelreview"`). Host should be URP 17.6.
2. **Tools > Amber Screen > Model Review > Open Review Scene**.
3. Right-click a prefab/model → **Amber Screen > Review in Model Review**.
4. Controls: Left-drag: orbit. Right-drag or middle-drag (press the scroll wheel): pan. Scroll: zoom. F: frame. 1/2/3: Delivered/Clay/Wire. I: fly inside (WASD/QE). R: reset. G: grid. C: 1.8 m capsule. T: neutral tonemap. H: hide legend.
5. **Tools > Amber Screen > Model Review > Material Report** for the read-only Console report.

## com.amberscreen.phoneui quick start

1. Add the package (local Sandbox: `"com.amberscreen.phoneui": "file:../../Packages/com.amberscreen.phoneui"`).
2. Implement `IChatSource` + `IContactDirectory`, then `phoneShell.Initialize(statusOrNull, chat, contacts)`.
3. Theme resolution: `SetOverride` → `Resources/PhoneUITheme` (game asset) → builtin defaults. Package never ships its own `Resources/PhoneUITheme`.
4. Circle avatars need UV1 on the canvas (`CircleClipMeshModifier` enables `TexCoord1` on the root canvas).

## com.amberscreen.llmchat quick start

1. Add the package (local Sandbox: `"com.amberscreen.llmchat": "file:../../Packages/com.amberscreen.llmchat"`).
2. `runner.Configure(backend, persona, timings)`; subscribe to `Seen` / `TypingStarted` / `Chunk` / `Done` / `Error`.
3. Optional: LLMUnity via `LLMUnityChatBackend` (install `ai.undream.llm`); otherwise supply any `ILLMChatBackend`.
4. Retry prompts reserve tokens with `FitPromptToBudget(..., reserveExtraTokens)` so the validation hint fits before `/no_think`.
5. Group threads: set `LiveHistoryProvider` so cross-speaker validation sees lines delivered after the request started.

## com.amberscreen.economy quick start

1. Add the package (local Sandbox: `"com.amberscreen.economy": "file:../../Packages/com.amberscreen.economy"`).
2. Create `Resources/EconomyCurrency` (`AmberScreen/Economy/Currency Settings`) or call `Money.Apply(settings)`.
3. `var wallet = new Wallet();` then `wallet.Add` / `TrySpend` / `TryReserve` / `Release` / `Commit` (amounts in minor units).
4. Optional: `var save = new WalletSaveState(wallet);` + `JsonSaveBackend` under `AmberScreen.Economy.TempSave` until `com.amberscreen.save`.

## com.amberscreen.devconsole settings (0.2.3+)

Additive fields on `DevConsoleSettings` (`Resources/AmberScreenDevConsoleSettings`):

| Field | Default | Notes |
| --- | --- | --- |
| `buttonHeight` | 30 | Compact landscape row height |
| `fontSize` | 16 | Base label size |
| `spacing` | 4 | Grid / stack spacing |
| `floatingScreenFraction` | (0.9, 0.88) | Floating panel fills this fraction of the safe area |
| `floatingPanelWidth` | 560 | Ignored for Floating (kept for older assets) |
| `dockRightScreenFraction` | 0.28 | DockRight width as a fraction of safe-area width |
| `dockRightWidth` | 320 | Minimum DockRight width |
| `autoColumns` | true | Column count from content width / `minEntryWidth` |
| `minEntryWidth` | 220 | Used when `autoColumns` is on |
| `columns` | 1 | Used when `autoColumns` is off |
| `fontOverride` | null | Optional TMP font; otherwise Inter from package Resources |

Games with an existing settings asset should run **Amber Screen → Reset DevConsole Settings To Defaults** (Sandbox menu) or update `buttonHeight`, `fontSize`, and `spacing` by hand. Inter loads from the package Resources folder automatically.

**0.3.4:** TMP missing guard — if TextMeshPro Essential Resources are not imported, the console logs an error and stays off (no silent F1 failure).

**0.4.0:** `DevHud` shared Debug HUD (left rail / portrait top strip) — register lines with `DevHud.SetLine`; F2 toggles; console windows keep clear of the HUD; migrate off OnGUI / custom HUD canvases.

**0.4.1:** `DevConsoleTheme` skinning, optional command line (`RegisterCommand`), HUD empty-line skip + key hints (`SetKeyHints` / `SetMode`).

**0.4.2:** DevHud sections (`AddSection` / rows / tables / pin / collapse), plain-string key hints, flowing command buttons, `RegisterSubHeader` + `Section/Sub` paths, `SetVisible` / visibility predicates, `SetCommandLineEnabled`.

### DevHud 0.4.2 quick start

```csharp
var status = DevHud.AddSection("status", "Status");
status.Clear(); // wipe rows/tables if re-registering (keeps tint / collapse options)
var fps = DevHudCell.Create("FPS", () => fpsText);
fps.Pin(); // always-visible pinned strip
status.AddRow("fps", fps); // row id → replaces on OnEnable / scene reload (no duplicates)
status.AddRow(
    "stats",
    DevHudCell.Create("A", () => a),
    DevHudCell.Create("B", () => b),
    DevHudCell.Create("C", () => c));
// status.AddRow(cell); // no id → always appends

DevHud.AddSection("wheels", "Wheels")
    .AddTable(new[] { "Wheel", "Temp" }, () => rows);

DevHud.AddSection("debug", "Debug", startCollapsed: true);
DevHud.SetKeyHints("Play", ("F1", "Console"), ("F2", "HUD"));
DevHud.SetMode("Play");

// Ambiguous nulls — cast the null side:
// DevHud.SetKeyHints("Play", ((InputAction)null, "x"));  // or ((string)null, "x")
// DevHud.AddKeyHint("Play", (InputAction)null, "x");
// Prefer SetKeyHints("Play") to clear, or pass only real keys/actions.

// Legacy still works:
DevHud.SetLine("Legacy", "HP", () => hp.ToString());
```

### Console layout 0.4.2

```csharp
DevConsole.RegisterSubHeader("Combat", "Weapons");
DevConsole.Register("Combat/Weapons", "Fire", OnFire); // path auto-creates sub-header
DevConsole.SetVisible("Combat/Fire", false);
DevConsole.Register("UI", "Extra", OnExtra, visible: true, available: null, visiblePredicate: () => showExtra);
DevConsole.SetCommandLineEnabled(true);
```
