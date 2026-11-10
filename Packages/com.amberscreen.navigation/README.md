# AmberScreen Navigation 0.1.0

Unity 6 (`6000.0`), render-pipeline independent, including URP. First slice only:
a heading/bearing core and one opt-in uGUI compass strip. No map, radar, quest
arrows, discovery, routing, game-specific navigation, or persistent hosts.

## Install and connect

Use the package folder as a local UPM package (select its `package.json` in
Package Manager's **Install package from disk** control), or have your integration
workflow embed the folder. The only runtime dependency is `com.unity.ugui` 2.0.0.
Text uses **UnityEngine.UI.Text**, not TMP. Supply a game-owned `Font` in the
presenter's Font field; without one, text cannot render. There is no Resources
loading or ProjectSettings change.

1. Add `HeadingSource` and `MarkerRegistry` to scene GameObjects.
2. Set Heading Source's Reference Transform to the body, vehicle or instrument.
   If empty, it uses its own transform, never the camera. North Axis defaults to +Z.
3. Under your existing Canvas/safe-area container, make a UI GameObject with a
   RectTransform, then add `CompassStripPresenter`. A 640 x 110 rectangle is a
   starting size. Assign Heading Source, Registry and Font. Do not attach it to
   your entire HUD root: it owns a clipped child for its strip only.
4. Optionally create a `MarkerTypes` asset (its CreateAssetMenu entry is declared
   by this package), fill its type list, and assign it to the presenter.
5. Register only markers the game explicitly chooses to display:

```csharp
registry.Register(new NavigationMarker("my-note", position, "cairn", "My note", false));
registry.UpdateEntry(new NavigationMarker("my-note", newPosition, "cairn", "My note", false));
registry.Unregister("my-note");
NavigationHud.Visible = false; // all package HUD strips hide on their next refresh
```

A duplicate Register returns false; UpdateEntry requires an existing id. Transform
entries follow live positions; destroyed targets are skipped, not moved to origin.
Discovery alone does nothing. The game owns registry lifetime and registrations.
Unknown types use white text and default distance-on. A null marker ShowDistance
inherits its type default; explicit true/false overrides that default. Global
Show Distances false wins over both. Distances are horizontal game units labelled
`m`; games should use metre-scaled worlds or supply an appropriate provider.

## Hooks and proven instrument feel

- `NavigationMath` has no Unity dependency: X/Z float coordinates, clockwise
  bearings, unsigned [0,360) and signed (-180,180] wraps, horizontal distances.
  Zero-length XZ direction returns a supplied fallback (default north/0).
  `HeadingSource.BearingTo` uses raw heading as that fallback, so an above/below
  or coincident target has a defined forward bearing and zero horizontal distance.
  As in the proven compass, a vertically pointing reference projects to zero
  and its Atan2 heading is 0 (relative to configured north).
  A north axis with no horizontal component falls back to +Z. Inputs must be finite.
- Heading/spring behaviour is ported from Sand & Ink's working
  `SandAndInk.Map.CompassInstrument` algorithm, not a new lag/wobble model.
  The chosen reference's forward is flattened and normalized, then Atan2 gives
  TrueHeadingDeg. The spring uses shortest-angle error, stiffness **26**, damping
  **6**, vertical-acceleration bump gain **3.5**, and maximum offset **16 degrees**.
  dt is clamped to at least 1e-4. These settings are serialized on HeadingSource;
  Optional Rigidbody supplies `linearVelocity.y` for bump response (none = no bump).
  `SetHeading(degrees)` resets angular velocity; automatic sampling runs at execution
  order -20. SetHeading is useful if replacing the chosen reference at runtime.
- `HeadingDeg` exposes the spring card, `TrueHeadingDeg` the raw signed Atan2 heading,
  and `RawHeading` its [0,360) form. The strip normally reads the shared spring card;
  enable **Use Raw Heading** to bypass it exactly. Dash/hand instruments can query
  the same source. No independent oscillator or exponential lag remains.
- Implement `INavigationDistanceProvider` on a MonoBehaviour and assign Distance
  Provider Component, or set `presenter.DistanceProvider` in code. Invalid/negative
  distance hides its text. It is never requested when distance is hidden.
- Derive from `CustomBearingSource`, return a bearing measured from the same north,
  and explicitly add the component to `customSources`. It has no distance API.
- `NavigationHud.Visible` affects HUD only; raw queries and registrations survive.
  No static host is created; the switch resets at Play startup, including when
  domain reload is disabled. Game code controls physical instruments separately.

The strip projects bearings inside its FOV, clips outside its rectangle, and draws
higher-priority items on top. It does not resolve overlapping labels or clamp
markers to screen edges. Keep labels short and registration counts deliberate.
The game supplies Canvas scaling, safe-area layout and any DevHud.GameRect adapter;
this package does not depend on or edit a game's layout system.

## Tests and limits

Install the Unity Test Framework in the **host test project**, not as a runtime
package dependency. Add `"com.amberscreen.navigation"` to that project's manifest
`testables` array if package tests are not discovered. Use
**Window > General > Test Runner**, EditMode, then run this package's tests.
The Editor asmdef uses explicit test-runner references, NUnit DLL override and
`UNITY_INCLUDE_TESTS` (the modern equivalent of legacy optionalUnityReferences).
Tests create temporary objects and destroy them in TearDown; no persistent hosts.

Acceptance checks 1–8 are represented by EditMode tests. The exact maths suite
also runs in a separate .NET verification project. Unity is not installed on the
build box: Unity import/compilation, EditMode execution, URP rendering, font/layout
at 1280x800/1920x1080/ultrawide, and human judgement of spring/bump feel still need Unity.
See the optional Compass Demo instructions. Future LWN/Snorkel data is not built.
