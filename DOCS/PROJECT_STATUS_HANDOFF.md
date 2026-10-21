# Project status handoff

Short status per package. Newest notes win.

## Registry

- **Registry v3** is current as of 2026-10-19 (`DOCS/REGISTRY.md`): one entry per package (includes / stays-in-games / interfaces / depends on).

## com.amberscreen.meters

- **0.1.0** tagged (`meters/0.1.0`). Lifted from Snorkel & Sail `1a12b5b` with original script GUIDs.

## com.amberscreen.economy

- **0.1.0** pushed (not tagged). Lifted from LWN `Assets/Shared/Economy` (`f5a5eeb`). Temp save under `AmberScreen.Economy.TempSave`. Sandbox scene: `Assets/Scenes/EconomySandbox.unity`.

## com.amberscreen.devconsole

- **0.4.2** pushed (not tagged). DevHud `AddRow(rowId)` upsert + `Clear()`; flowing buttons rebuild after layout; Sandbox scene tracked. Prior tag: `devconsole/0.4.1` → 01de3af.
- Namespace is `AmberScreen.Dev`. Gate gameplay on `BlocksGame` / `IsPointerOverConsole`. Keep game UI inside `DevHud.GameRect` clear of both rails.
