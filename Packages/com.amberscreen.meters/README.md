# Amber Screen Meters

Game-free capacity meters (stamina, breath, coolant, radiator water, …). Assembly `AmberScreen.Meters`, namespace `AmberScreen.Meters`. No UI, no render pipeline, no swim/player/camera knowledge.

**Version:** 0.1.0  
**Unity:** 6000.6+  
**Owner:** Snorkel & Sail. Changes go through Packages.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.meters#meters/0.1.0
```

Definition assets (`MeterDefinition`) live in each game, not in the package.

## What a game supplies

1. One or more **MeterDefinition** assets (id, max, start, default drain/fill, thresholds).
2. A **CharacterMeters** component on a character/vehicle with those definitions assigned.
3. Controllers that call `Drain` / `Fill` / `Add` when gameplay says so (sprint, submerged, engine heat, …).

## Public API

### `Meter`

| Member | Role |
|--------|------|
| `Id`, `Max`, `Current`, `Normalized` | Identity and 0..1 value |
| `Drain(perSecond, dt)`, `Fill(perSecond, dt)`, `Add(amount)`, `Set(value)`, `SetMax` | Mutators |
| `PassiveDrainPerSecond` / `PassiveFillPerSecond` + `TickPassive(dt)` | Optional idle rates |
| `IsEmpty` / `IsFull` | Convenience |
| `Capture()` / `Apply(MeterState)` | Save snapshot |
| Events: `BecameEmpty`, `BecameFull`, `ThresholdCrossedDown(name)`, `ThresholdCrossedUp(name)` | Once per transition |

### `MeterDefinition` (ScriptableObject)

Authoring: id, max, start value, default drain/fill, list of `MeterThreshold` (`Name` + `Normalized`).

### `CharacterMeters` (MonoBehaviour)

| Member | Role |
|--------|------|
| `Get(id)` / `TryGet(id)` | Lookup |
| `All` | Ordered list |
| `CaptureAll()` / `ApplyAll(states)` | Multi-meter save |
| `Rebuild()` / `SetDefinitions(...)` | Rebuild from assets |
| `TickPassiveAll(dt)` | Optional passive tick for every meter |

### `MeterState`

`{ Id, Current, Max }` — plain serializable struct. Persist with your own save system.

## Events

Threshold and Empty/Full events fire **once** when the condition becomes true, not every frame. Crossing back the other way arms them again.
