# Project status handoff

Short status per package. Newest notes win.

## Registry

- **Registry v3** is current as of 2026-10-19 (`DOCS/REGISTRY.md`): one entry per package (includes / stays-in-games / interfaces / depends on).

## com.amberscreen.meters

- **0.1.0** tagged (`meters/0.1.0`). Lifted from Snorkel & Sail `1a12b5b` with original script GUIDs.

## com.amberscreen.devconsole

- **0.4.0** pushed (not tagged; fix1 on rail width / wide margin / HUD GC). Shared `DevHud` Debug HUD rail; Fabio playtests Sandbox F1/F2 + wide/portrait, then tags `devconsole/0.4.0`.
- Namespace is `AmberScreen.Dev`. Gate gameplay on `BlocksGame` / `IsPointerOverConsole`. Migrate per-game HUDs onto `DevHud`.
