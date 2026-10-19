# Shared systems registry

RULE: claim before building (approved by Fabio 2026-10-19). Before writing a Cursor prompt for any system another game might use (input, save, UI, console, camera, vehicles, interaction, audio, streaming, time/weather and similar), message Packages first. Packages answers: free to build (registered to you), already exists / in progress (reuse it or add requirements), or game-specific (no check needed). The first game to claim a module builds it package-ready in its own folder and asmdef, with no game references. Nobody starts a second copy. Tell Packages when it settles, and Packages schedules extraction. Directors never edit this repo; Packages updates this file.

| Module | Owner (director/game) | State (planned / in progress / settled / in library) | Wanted by | Planned package | Notes |
| --- | --- | --- | --- | --- | --- |
| Dev console | Packages (shared) | in library 0.1.x | all five | com.amberscreen.devconsole | no game builds a new one; Watchmaker, S&I, S&S and LWN consoles fold in |
| Versioning (0.MAJOR.MINOR, label, save-version check) | Sand & Ink | settled | all five | com.amberscreen.versioning | extract after S&I week 2 check; Watchmaker's version check folds in |
| Save scaffolding (slots, safe write, version stamp) | Sand & Ink (+ Watchmaker JSON slots) | settled scheme | Snorkel & Sail, LWN | to be decided (Fabio) | game save data classes stay in games |
| Time-of-day interface (current time, day count, change event, saveable) | to be decided | planned | Sand & Ink, Snorkel & Sail | to be decided | S&I connects TimeOfDay, S&S connects Enviro 3 |
| Day/night cycle (TimeOfDay + DayCycleSettings) | Sand & Ink | in progress (0.1.7) | Sand & Ink | to be decided | S&S uses Enviro 3 instead |
| Fuel | Sand & Ink | settled | Sand & Ink, LWN? | com.amberscreen.vehicle | |
| Engine heat | Sand & Ink | settled (0.1.6) | Sand & Ink | com.amberscreen.vehicle | package-ready in Assets/Scripts/Shared |
| Drivetrain | Sand & Ink | in progress | Sand & Ink | com.amberscreen.vehicle | |
| Floating origin + tile streaming | Snorkel & Sail leads, with Sand & Ink | in progress | Sand & Ink, Snorkel & Sail | com.amberscreen.floatingorigin | S&I 2000 m + 3x3 tiles; S&S 1000 m + KWS + islands; distance must be a setting |
| Cargo bay, van enter/exit, parking zones, bump shake | LWN | in progress | LWN | com.amberscreen.vehicle | after LWN Gate B |
| On-foot carrying (ParcelCarrier) | LWN | in progress | LWN | to be decided (interaction) | |
| KCC on-foot walker | LWN | planned (phase 5) | LWN | to be decided | |
| Boat buoyancy (KWS) | Snorkel & Sail | planned | Snorkel & Sail | to be decided | generic behind an interface |
| LLM text classifier (LLMUnity + LoRA) | Twin Ticks | in progress | Twin Ticks | maybe later | Twin Ticks only for now |

## Game-specific (no check needed)

- LWN jobs/payout loop and job generator
- Snorkel & Sail bunk sleep trigger
- Watchmaker bench tools
- All game console commands
