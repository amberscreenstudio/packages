# AmberScreen Vehicle Water

Vehicle water ingress: when the water gets deep enough the engine stalls, the cabin floods over time and the vehicle stops. Out of the water the cabin drains slowly, and the engine can restart only once drained and only if the game allows it. Assembly `AmberScreen.VehicleWater`, namespace `AmberScreen.VehicleWater`.

It does not float anything (keep your buoyancy asset for that) and has no dependency on any water or vehicle asset: water height and engine control come through two small interfaces.

**Version:** 0.1.0
**Unity:** 6000.0+
**Owner:** Packages.

## Install

```
https://github.com/amberscreenstudio/packages.git?path=/Packages/com.amberscreen.vehiclewater#vehiclewater/0.1.0
```

## Quick start

1. Add `VehicleWaterIngress` to the vehicle (next to its Rigidbody).
2. Make two empty children and assign them: **Intake point** (where the engine breathes, usually high in the engine bay) and **Cabin point** (the cabin floor).
3. Water: assign a component that implements `IWaterHeightProvider` to **Water Source**. With nothing assigned, the first enabled `FlatWaterPlane` in the scene is used.
4. Engine: assign a component that implements `IVehicleEngineControl` to **Engine Source** (or call `SetEngineControl(...)` from code). Without one, flooding and stopping still work and the events still fire.
5. Optional: set `RestartAllowed = () => ...` for the game's say on restarting; turn on **Auto Restart When Drained**, or show a "Start engine" prompt when `onRestartAllowed` fires and call `TryRestart()`.

## The rules (`WaterIngressSettings`)

| Setting | Default | Meaning |
|---------|---------|---------|
| `intakeStallDepth` | 0.05 m | water this far over the intake stalls the engine |
| `cabinFloodDepth` | 0.02 m | water this far over the cabin floor floods the cabin |
| `floodPerSecond` / `fullRateDepth` | 0.08 / 0.3 m | flood rate, reached at this depth over the threshold (shallower floods slower) |
| `drainPerSecond` | 0.02 | slow drain once out of the water (50 s from full) |
| `stallFloodLevel` | 0.6 | a cabin this full also stalls the engine |
| `stopFloodLevel` | 0.3 | a cabin this full keeps the vehicle stopped even out of deep water |
| `drainedLevel` | 0.001 | restart allowed at or below this |
| `extraLinearDamping`, `extraAngularDamping`, `brakeDeceleration` | 2, 1, 4 | how hard it is stopped (all removed again when released) |

Stopped by water = stalled with the intake or cabin under water, or flood at or above `stopFloodLevel`.

## Events and state

UnityEvents: `onStalled`, `onFloodStarted`, `onFloodChanged(0..1)`, `onDrained`, `onStoppedByWater`, `onReleasedByWater`, `onRestartAllowed`, `onRestarted`. C# event `Changed(WaterIngressEvents)`.
State: `IntakeDepth`, `CabinDepth` (positive = under water), `Flood`, `IsStalled`, `IsStoppedByWater`, `CanRestart`. `ResetWater()` for respawns and loads; `Model.SetFlood(x)` to restore a saved flood level.

`WaterIngressModel` holds the same rules with no Unity objects, if a game wants to drive it itself.

## Adapters (copy into your game, not compiled here)

These are examples only. They are deliberately not part of the package, because the package is public and the assets are paid. Check the names against the version you own.

KWS Water System, simple height:

```text
using AmberScreen.VehicleWater;
using UnityEngine;

public sealed class KwsWaterHeight : MonoBehaviour, IWaterHeightProvider
{
    public float GetWaterHeight(Vector3 worldPosition)
    {
        // Returns worldPosition.y when there is no water system, which reads as "not under water".
        return KWS.WaterSystem.FindWaterLevelAtLocation(worldPosition);
    }
}
```

KWS with waves (`WaterSurfaceRequestPoint`; the result can lag by a frame):

```text
readonly KWS.WaterSurfaceRequestPoint m_Request = new KWS.WaterSurfaceRequestPoint();

public float GetWaterHeight(Vector3 worldPosition)
{
    m_Request.SetNewPosition(worldPosition);
    KWS.WaterSystem.TryGetWaterSurfaceData(m_Request);
    return m_Request.IsDataReady ? m_Request.Result.Position.y : float.NegativeInfinity;
}
```

A vehicle controller (NWH Vehicle Physics 2 style; any controller works the same way: map four members):

```text
using AmberScreen.VehicleWater;
using UnityEngine;

public sealed class NwhEngineAdapter : MonoBehaviour, IVehicleEngineControl
{
    public NWH.VehiclePhysics2.VehicleController vehicle;
    bool m_CanStart = true;

    public bool IsEngineRunning => vehicle.powertrain.engine.IsRunning;
    public bool CanStart { get => m_CanStart; set => m_CanStart = value; }
    public void Stall() => vehicle.powertrain.engine.Stop();
    public void Restart() { if (m_CanStart) vehicle.powertrain.engine.Start(); }

    // Also block the player's own ignition input while CanStart is false.
}
```

## Stays in games

The water and vehicle adapters, sounds, screen effects, the player's ignition prompt, and what happens to a fully flooded vehicle (towing, damage, write-off).

## Demo

Tools > AmberScreen > Vehicle Water > Create Vehicle Water Demo Scene, then Play.
