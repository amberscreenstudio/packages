# Changelog

## 0.3.0 — 2026-11-12
- Added RoutePath, a game-supplied world-space polyline snapshot with cumulative length, SampleAtDistance and ClosestDistance. No route finding.
- Added RouteRibbonUI, a uGUI MaskableGraphic that projects the path into its RectTransform from game-supplied world-XZ bounds (optional yaw) and can trim to the remaining distance.
- Added optional RouteRibbonWorld LineRenderer presenter with a height offset. The game supplies the material; a default sprite material is used if none is set.
- Both presenters draw the path only (no player pip, arrows or follow-dots) and honour the existing NavigationHud.Visible switch.
- Added a route ribbon demo (Tools > AmberScreen > Navigation > Create Route Ribbon Demo Scene) with Hud Visible and Show Remaining Only toggles.
- Added EditMode tests for RoutePath maths and ribbon mesh vertex counts and visibility.
- Verified in Sandbox (Unity 6000.6.6f1): EditMode tests green, ribbon demo works (world line, panel line trims behind the car, Hud Visible hides both).

## 0.2.1 — 2026-11-11
- Route demo toggle renamed from Mags Present to Co Driver Present; package text no longer names a game character.

## 0.2.0 — 2026-11-11
- Adapted LWN JobManifest ordered-stop/current-index/event pattern into navigation-only data.
- Added explicit next-stop distance, reached/advance/change events and opt-in registry binding.
- Added authored distance/world-radius text notes, a game-set co-driver gate and caller-driven display timer.
- Added optional uGUI Text presenter using the existing single NavigationHud.Visible switch.
- Added a repeating route demo with Inspector toggles for Mags presence and HUD visibility.
- Added logic and Unity presentation/marker ownership tests. No persistent hosts or input dependency.
- No maps, arrows, route finding, player/camera coupling or game-side job-state changes.
- Verified in Sandbox (Unity 6000.6.6f1): compiles, EditMode tests green, route demo works (notes gate on Mags Present, labels hide with Hud Visible).

## 0.1.0 — 2026-11-10
- Ported heading and spring-card behaviour from Sand & Ink `SandAndInk.Map.CompassInstrument`
  using the supplied working-code algorithm: stiffness 26, damping 6, bump gain 3.5,
  maximum offset 16 degrees, minimum dt 1e-4, SetHeading velocity reset, execution order -20.
- Replaced vehicle adapter/MapSettings with an explicit reference Transform, optional Rigidbody
  and serialized settings. No DrawHud, procedural textures or HudCanvas code was ported.
- Added configurable north and pure bearing utilities; strip can bypass the spring for raw heading.
- Removed the first draft's independent exponential lag/sinusoidal wobble design.
- Scene-owned opt-in marker registry, marker type asset, distance and custom bearing hooks.
- Optional uGUI compass strip and one global HUD switch.
- EditMode acceptance tests and a small optional demo script.
- Menu Tools > AmberScreen > Navigation > Create Compass Demo Scene builds an unsaved demo scene.
- Verified in Sandbox (Unity 6000.6.6f1): compiles, EditMode tests green, compass visible.
