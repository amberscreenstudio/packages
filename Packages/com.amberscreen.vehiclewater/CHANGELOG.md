# Changelog

All notable changes to `com.amberscreen.vehiclewater` are documented here.

## [0.1.0] - 2026-11-14 (verified 2026-11-14)

Verified in Sandbox (Unity 6000.6.6f1): EditMode tests green, vehicle water demo works.

### Added

- `VehicleWaterIngress`: water over the intake stalls the engine, water over the cabin floods it over time (0..1), the vehicle is slowed to a stop (extra damping and braking), the cabin drains slowly out of the water, and the engine may restart only when drained and the game allows it.
- `WaterIngressModel`: the same rules without Unity objects (tested).
- `IWaterHeightProvider` with a `FlatWaterPlane` default; `IVehicleEngineControl` for the game's vehicle.
- Demo scene menu: Tools > AmberScreen > Vehicle Water > Create Vehicle Water Demo Scene.
