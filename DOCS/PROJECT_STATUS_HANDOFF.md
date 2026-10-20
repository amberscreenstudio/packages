# Project status handoff

Short status per package. Newest notes win.

## Registry

- **Registry v3** is current as of 2026-10-19 (`DOCS/REGISTRY.md`): one entry per package (includes / stays-in-games / interfaces / depends on).

## com.amberscreen.meters

- **0.1.0** tagged (`meters/0.1.0`). Lifted from Snorkel & Sail `1a12b5b` with original script GUIDs.

## com.amberscreen.devconsole

- **0.4.0** pushed (not tagged; fix1–3). `DevHud` on left edge / portrait top strip; console rail on right; collapse shows "DEV" tab. Fabio playtests Sandbox (ultrawide + portrait), then tags `devconsole/0.4.0`.
- Namespace is `AmberScreen.Dev`. Gate gameplay on `BlocksGame` / `IsPointerOverConsole`. Keep game UI inside `DevHud.GameRect` clear of both rails.
