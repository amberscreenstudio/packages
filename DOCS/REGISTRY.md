# Shared systems registry

Last updated: 2026-10-19. Kept by Packages. Every registered module is a package with exactly ONE owner (Fabio, 2026-10-19).

## Rules

**Claim before building.** Before prompting ANY new game system, a director checks with Packages. The answer is one of three:

- **reuse:** it exists or is claimed, so use it and send requirements to its owner through Packages
- **free:** it gets registered to you
- **game-specific:** go ahead

**One owner.** Only the owner writes a package. Other games are users and never keep a second copy.

**Package-ready from day one.** Build in `Assets/Shared/<Module>` with its own asmdef and the namespace `AmberScreen.<Module>`:

- no game references; hook the game in through interfaces or adapters
- settings in a ScriptableObject
- the game references the package, never the reverse
- a README
- existing modules get a readiness pass, one prompt each, and the game must behave exactly the same afterwards

**Changing a package you don't own.**

- The user sends what it needs to Packages, and Packages passes it to the owner. The user never edits the package or builds its own version.
- The owner adds the change generically. A user's game-specific look or feel stays in that game and reads the package's values through its interfaces.
- If the owner can't get to it in time, the user may use a small temporary stand-in in its own game code, marked "replace with \<package\>". It never forks the package.
- If two users want conflicting behaviour, the owner adds an option that covers both. If they still disagree, Fabio decides.

**Versions.** Versions are `0.MAJOR.MINOR`:

- **MAJOR** when a change renames or removes something users already use, or breaks saves. Packages warns every user first.
- **MINOR** for additions.

Games pin a tag, never `main`. Packages moves settled modules into this repo, tags them, and records the versions in `DOCS/USAGE.md`. Directors never edit this repo.

**Ownership principle.** The game furthest along owns the module.

## Packages

Each entry lists: **Includes** (what is in the package), **Stays in games** (what is deliberately NOT in it), **Interfaces** (the contract users code against), **Depends on**.

### com.amberscreen.devconsole

- **Owner:** Packages | **State:** in library, `devconsole/0.3.3` | **Users:** all five
- **Includes:** right-edge rail (stays open without blocking), section windows (blocking), quick buttons, pinned readouts, buttons/toggles/dropdowns/confirms/readouts, filter, log window, saved state, portrait full-screen sheet with dpi scaling, open bindings (F1, Select+Start, 3-finger tap, corner hold), release-build stripping, Inter font
- **Stays in games:** every game's own commands and sections; HUD/debug overlays outside the console
- **Interfaces:** DevConsole static API, `[DevButton]`, `IDevConsoleSectionProvider`, `BlocksGame`, `IsPointerOverConsole`, `ConsumedCloseThisFrame`
- **Depends on:** TMP, uGUI, Input System
- **Queued for 0.4.x:** confine to a RectTransform (Twin Ticks), optional always-on HUD strip (S&I)

### com.amberscreen.versioning

- **Owner:** Sand & Ink | **State:** settled, extract after S&I week-2 check | **Users:** all five
- **Includes:** `0.MAJOR.MINOR` version asset, version label, save-version compare (warn-but-load)
- **Stays in games:** each game's version number and release notes
- **Interfaces:** version asset + compare helper
- **Depends on:** nothing
- **Note:** Watchmaker's GameVersion is retired in favour of it

### com.amberscreen.save

- **Owner:** Watchmaker | **State:** in progress (readiness pass first) | **Users:** all five
- **Includes:** save slots, temp-file swap writes, version stamp, `ISaveStateProvider` registry, per-field "known" flags, warn-but-load on version mismatch
- **Stays in games:** what each game saves (its providers and data classes)
- **Interfaces:** `ISaveStateProvider`
- **Depends on:** `com.amberscreen.versioning`
- **Note:** S&I's quick.json save is retired once it adopts; S&I requirements go in before Watchmaker's prompt

### com.amberscreen.timeofday

- **Owner:** Sand & Ink | **State:** in progress (S&I 0.1.7, moon in 0.1.9 prompt) | **Users:** all five
- **Includes:** core asmdef with `ITimeOfDay` (time, day, day length, events); optional driver asmdef (`TimeOfDay`, `DayCycleSettings`, curve-driven sun/light); moon: `MoonPhase` and `MoonIllumination` (0..1) on a 29.53-day cycle, moonrise/moonset shifted by phase
- **Stays in games:** sky/lighting look, how bright moonlight is, Enviro 3 hookup (S&S), shift rules (LWN), phone clock display (Twin Ticks)
- **Interfaces:** `ITimeOfDay`
- **Depends on:** nothing (driver: URP-agnostic lights only)

### com.amberscreen.vehicle

- **Owner:** Sand & Ink | **State:** built | **Users:** S&I, LWN, S&S (adapter only, boats)
- **Includes:** `IVehicleAdapter`, EVP5 adapter, SimDrivetrain, tyres, fuel, engine heat; optional asmdefs: chase camera, instruments (compass, odometer), surface grid (surface types, grip, `ISurfaceProvider`)
- **Stays in games:** vehicle input, teleport, state binder, headlights (S&I); every game's vehicle tuning assets
- **Interfaces:** `IVehicleAdapter`, `ISurfaceProvider`
- **Depends on:** EVP5 (adapter asmdef only)

### com.amberscreen.vehicleextras

- **Owner:** LWN | **State:** built (extract after LWN Gate B) | **Users:** LWN, S&S (enter/exit for boats)
- **Includes:** cargo bay, enter/exit and seat handover, parking zones, bump shake, van camera rig
- **Stays in games:** LWN's van model, job/cargo rules
- **Interfaces:** seat/enter-exit events
- **Depends on:** `com.amberscreen.vehicle` (`IVehicleAdapter`)

### com.amberscreen.floatingorigin

- **Owner:** Snorkel & Sail | **State:** built in both games, merging | **Users:** S&S, S&I
- **Includes:** floating origin (Double3 positions, recentre events), one streaming core for tiles and island scenes, distances as settings
- **Stays in games:** S&I's 2000 m 3x3 tile source and S&S's island scenes (both feed the core as streaming sources)
- **Interfaces:** recentre event, streaming source interface
- **Depends on:** nothing
- **Note:** S&I stops extending its own copy

### com.amberscreen.water

- **Owner:** Snorkel & Sail | **State:** planned | **Users:** S&S, S&I later
- **Includes:** `IWaterSurface` only (tiny core asmdef): height, normal, flow at a point
- **Stays in games:** KWS (or other water) adapter, water visuals
- **Interfaces:** `IWaterSurface`
- **Depends on:** nothing

### com.amberscreen.buoyancy

- **Owner:** Snorkel & Sail | **State:** planned (built straight into Shared) | **Users:** S&S
- **Includes:** boat/object buoyancy points, drag, righting
- **Stays in games:** boat handling and sailing
- **Interfaces:** buoyancy settings asset
- **Depends on:** `com.amberscreen.water`

### com.amberscreen.swim

- **Owner:** Snorkel & Sail | **State:** in progress (S&S 0.0.8) | **Users:** S&S
- **Includes:** swim controller states (surface, underwater, wade), glide and drag, presets, handover to/from the walker
- **Stays in games:** diving/snorkel rules, breath vignette, forced surfacing
- **Interfaces:** swim state events, enable/disable handover
- **Depends on:** `com.amberscreen.water`, `com.amberscreen.meters`

### com.amberscreen.meters

- **Owner:** Snorkel & Sail | **State:** in library, `meters/0.1.0` | **Users:** S&S, S&I
- **Includes:** Meter (value, max/capacity, drain and regen/fill, named thresholds, warning/empty/full events, saveable state), CharacterMeters (one set for land and water)
- **Stays in games:** which meters exist and their numbers (stamina, breath, water, radiator), meter UI
- **Interfaces:** Meter, meter events, save state
- **Depends on:** nothing

### com.amberscreen.interaction

- **Owner:** Snorkel & Sail | **State:** planned (after buoyancy) | **Users:** S&S, LWN, Watchmaker
- **Includes:** `IInteractable` + interactor: FP ray and 3P proximity, per-object range, nearest target wins (optional priority), dynamic prompt label per state, `CanInteract` (unavailable but visible), focus gained/lost events, press, hold with progress, press and hold on one target, take control (event only), works while carrying, underwater and on moving decks, origin-recentre safe, settable interactor enabled state, global block predicate (dev console)
- **Stays in games:** prompt UI, what each interactable does
- **Interfaces:** `IInteractable`, interactor events
- **Depends on:** Input System (action supplied by the game); no UI or render pipeline

### com.amberscreen.props

- **Owner:** LWN | **State:** built | **Users:** LWN, S&S, Watchmaker
- **Includes:** hinged physics door, on-foot carrying (ParcelCarrier), item damage
- **Stays in games:** LWN's parcel rules and payouts, all prop art
- **Interfaces:** implements `IInteractable` once interaction exists
- **Depends on:** `com.amberscreen.interaction` (later)

### com.amberscreen.charactercontroller

- **Owner:** LWN | **State:** planned (LWN phase 5) | **Users:** LWN, S&S
- **Includes:** the only on-foot walker (KCC), moving-deck support, origin-recentre safe, enable/disable handover API (to swim, vehicles), park and release at the helm
- **Stays in games:** camera feel presets, game input mapping
- **Interfaces:** handover API, movement events
- **Depends on:** Input System, `com.amberscreen.meters` (sprint)

### com.amberscreen.phoneui

- **Owner:** Twin Ticks | **State:** in progress (Brief 08, 0.0.12) | **Users:** Twin Ticks, LWN
- **Includes:** phone frame, status bar, home screen, messaging, notifications, app host
- **Stays in games:** every game's apps' content and chats
- **Interfaces:** `IPhoneApp`, `IChatSource`
- **Depends on:** TMP, uGUI

### com.amberscreen.phonecamera

- **Owner:** LWN | **State:** built | **Users:** LWN, Twin Ticks
- **Includes:** photo camera as an `IPhoneApp` (capture, preview, gallery hook)
- **Stays in games:** LWN's proof-of-delivery photo rules
- **Interfaces:** `IPhoneApp`
- **Depends on:** `com.amberscreen.phoneui`

### com.amberscreen.dialogue

- **Owner:** Twin Ticks | **State:** planned (built straight into Shared) | **Users:** Twin Ticks, Watchmaker, LWN, S&I
- **Includes:** thin Pixel Crushers Dialogue System wrapper: start conversations, localisation, save bridge
- **Stays in games:** all conversations and story content
- **Interfaces:** start/end conversation API, save bridge
- **Depends on:** Pixel Crushers Dialogue System, `com.amberscreen.save` (save bridge)
- **Note:** scripted dialogue only; LLM chat is `com.amberscreen.llmchat`

### com.amberscreen.llmclassifier

- **Owner:** Twin Ticks | **State:** in progress (Twin Ticks 0.0.20) | **Users:** Twin Ticks
- **Includes:** LLMUnity reply classifier, LoRA fine-tune pipeline, `IReplyClassifier` (text in, `ReplyClassification`: state, target, confidence out)
- **Stays in games:** Twin Ticks' reply states and training data
- **Interfaces:** `IReplyClassifier`, `ReplyClassification`
- **Depends on:** LLMUnity

### com.amberscreen.llmchat

- **Owner:** Twin Ticks | **State:** in progress (Twin Ticks 0.0.20, 11b1018, `Assets/Shared/LLMChat`) | **Users:** Twin Ticks, LWN likely
- **Includes:** LLM character chat runner: persona assets, history window, streaming replies, seen/typing timing assets, runner events (seen, typing, chunk, done)
- **Stays in games:** conversation director, story beats, personas' content (Theo/Mara/Jonah), pacing, the PhoneUI adapter onto `IChatSource`
- **Interfaces:** `ILLMChatBackend`, runner events
- **Depends on:** nothing in core; LLMUnity only in a separate backend asmdef (`LLMUnityChatBackend`). No phoneui reference.

### com.amberscreen.audio

- **Owner:** Watchmaker | **State:** planned (when sound comes) | **Users:** Watchmaker, LWN
- **Includes:** audio setup on Universal Sound FX: mixer groups, volume settings, one-shot and loop helpers
- **Stays in games:** all sound choices and music
- **Interfaces:** audio settings asset, play helpers
- **Depends on:** Universal Sound FX

### com.amberscreen.gameshell

- **Owner:** Watchmaker | **State:** built (readiness pass after save) | **Users:** all five
- **Includes:** pause menu, scene changes with fades (SceneFlow), central key list, UI theme
- **Stays in games:** menu content and art
- **Interfaces:** SceneFlow API, theme asset, key list
- **Depends on:** TMP, uGUI, Input System

### com.amberscreen.encounters

- **Owner:** Sand & Ink | **State:** planned (M3-M4, built straight into Shared) | **Users:** open
- **Includes:** rule-based weighted encounter director
- **Stays in games:** the encounters themselves
- **Interfaces:** encounter rule assets, director events
- **Depends on:** nothing

## Game-specific (no check needed)

- **Sand & Ink:** paper map, dead reckoning, map HUD and landmarks; surface debug view; vehicle input, teleport and the state binder; claims and recording; headlights; top-left debug overlay stack
- **Snorkel & Sail:** diving and snorkelling rules, sailing and boat handling, the dive shop economy and customers, the walkable cabin layout, the bunk sleep trigger, breath vignette and forced surfacing
- **LWN:** the job loop and job generator, payout, the doorstep ledger, delivery and photo rules
- **The Watchmaker Shop:** bench and tools, calibre profiles, the fault library, spares and billing, cleaning, oiling, the demagnetiser, regulation, casing, the final test, the counter room, the school
- **Twin Ticks:** the game master, the live-chat conversation director, reply states, relationship meters, personas and story content, demo chat and reply timing
- Every game's own console commands
