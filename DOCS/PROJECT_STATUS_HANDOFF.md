# Project status handoff

Short status per package. Newest notes win.

## Registry

- **Registry v3** is current as of 2026-10-19 (`DOCS/REGISTRY.md`): one entry per package (includes / stays-in-games / interfaces / depends on).

## com.amberscreen.meters

- **0.1.0** tagged (`meters/0.1.0`). Lifted from Snorkel & Sail `1a12b5b` with original script GUIDs.

## com.amberscreen.devconsole

- **0.3.4** pushed (not tagged). TMP missing guard + CS0104/CS0122 compile fixes; Fabio playtests Sandbox F1, then tags `devconsole/0.3.4`.
- Namespace is `AmberScreen.Dev` (package/asmdef names unchanged). Gate gameplay on `BlocksGame` / `IsPointerOverConsole`.
