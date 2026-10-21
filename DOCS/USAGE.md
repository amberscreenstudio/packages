# Package usage by game

## Package versions per game

| Game | Repo | com.amberscreen.devconsole | com.amberscreen.meters | com.amberscreen.economy |
| --- | --- | --- | --- | --- |
| Twin Ticks | amberscreenstudio/Twin-Ticks | `devconsole/0.3.1` (Twin Ticks 0.0.18), bumping to 0.3.3 | none | none |
| Sand & Ink | amberscreenstudio/Sand-and-Ink | switching to `devconsole/0.3.3` (S&I 0.1.9) | adopting for drinking water | planned |
| Snorkel & Sail | (local) | not yet (own console) | switching from `Assets/Shared/Meters` | planned |
| LWN | (local) | not yet (own console) | none | switching from `Assets/Shared/Economy` |
| The Watchmaker Shop | local, `E:\Unity Projects\Time & Tension` | not yet (own console) | none | planned |

Install URL format: `https://github.com/amberscreenstudio/packages.git?path=/Packages/<package>#<short>/<version>`, for example `#devconsole/0.3.3`, `#meters/0.1.0`, or `#economy/0.1.0` (once tagged). Always pin a tag, never `main`.

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
var fps = DevHudCell.Create("FPS", () => fpsText);
fps.Pin(); // always-visible pinned strip
status.AddRow(fps);
status.AddRow(
    DevHudCell.Create("A", () => a),
    DevHudCell.Create("B", () => b),
    DevHudCell.Create("C", () => c));

DevHud.AddSection("wheels", "Wheels")
    .AddTable(new[] { "Wheel", "Temp" }, () => rows);

DevHud.AddSection("debug", "Debug", startCollapsed: true);
DevHud.SetKeyHints("Play", ("F1", "Console"), ("F2", "HUD"));
DevHud.SetMode("Play");

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
