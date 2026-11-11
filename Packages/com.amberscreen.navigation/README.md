# AmberScreen Navigation 0.3.0

Unity 6 (`6000.0`), render-pipeline independent, including URP.
A heading/bearing core, opt-in uGUI compass strip, ordered next-stop data,
co-driver text feed and an optional route ribbon. No minimap, radar, quest arrows,
glowing follow-dots, discovery, route finding, game-specific navigation, or
persistent hosts.

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
The box also compiles the new layer against minimal Unity API stubs and runs its
pure-logic tests. This is not a Unity compilation or rendering test. See the demos below.


## 0.2.0: next stops and co-driver text

`NavigationRoute` adapts the ordered stop snapshot/current-index/event pattern from
LWN's `JobManifest` / `IJobManifest`, reduced to id, label and position. Delivery
outcomes, parcels, shift timers and world assets stay in the game. Inspection of
LWN found `CoDriverLines` authored next-stop/praise/wrong-door strings, but no working
route-trigger feed or Mags-presence state to port. The trigger feed and optional
presenter here are new, small game-agnostic code, not a replacement LWN job system.

```csharp
// Keep these as game-owned fields, created once for the scene/route.
var route = new NavigationRoute();
route.SetStops(new[] {
    new RouteStop("stop-1", "Harbour Road", stopPosition),
    new RouteStop("stop-2", "Depot", depotPosition)
});
route.NextStopChanged += stop => { /* bind app/dash text; null = complete */ };
route.Reached += stop => { /* proximity only, NOT proof of delivery */ };
route.Advanced += previousStop => { /* game explicitly moved past this stop */ };
route.UpdatePosition(vehiclePosition, reachedRadius: 3f); // call each frame
// Call from the game's existing stop-resolved/stop-changed lifecycle, NOT proximity:
route.Advance();
```

`Stops` is a read-only snapshot, in authored order; ids must be unique and non-empty.
`NextStop` is null on empty/completed routes. `DistanceToNextStop` is horizontal
straight-line metres (positive infinity with no next stop), **not** road distance.
Call `UpdatePosition` before querying it. `Reached` fires once per current stop;
only `Advance` changes that stop. `SetStops` resets the snapshot and notifies even
when empty. LWN should mirror its authoritative manifest via a game-side adapter;
do not use this package to decide parcel outcomes or keep a second delivery ledger.

Nothing is registered automatically. For an explicit next-stop marker:

```csharp
var binding = new RouteMarkerBinding(route, registry, "lwn/next-stop");
// Dispose when the route/game binding ends (for example, in OnDisable):
binding.Dispose();
```

Only the current stop is registered, using type `next-stop`. The caller supplies
a registry-unique marker id. Existing registrations are never overwritten or
removed, even if the game replaces this binding's entry. Other stops are not shown.
No arrows, edge markers, minimap or device visual are created.

```csharp
var feed = new PaceNoteFeed { DisplaySeconds = 4f };
feed.SetNotes(new[] {
    new PaceNote("Right 3 into Harbour Road", 80f), // authored route progress in metres
    new PaceNote("Caution gate", gatePosition, 12f) // horizontal radius in metres
});
feed.NoteFired += note => { /* optional game-owned device/data binding */ };
paceNotePresenter.Feed = feed; // optional component
paceNotePresenter.label = existingHudText; // uGUI Text, font/layout owned by game
// Each frame, INCLUDING frames without a co-driver:
feed.CoDriverPresent = gameSaysCoDriverIsPresent; // default false; never inferred from player/camera
feed.Tick(metresAlongAuthoredRoute, vehiclePosition, deltaSeconds);
```

Each authored note is consumed once per `SetNotes`. Distance notes trigger at or
past their threshold, including a large progress step. The game supplies route
progress: the package does not infer it from travel, stops or GPS. World notes
trigger when sampled inside their radius; call every frame and use a suitable
radius (a teleport/large step across the entire radius will not trigger). Multiple
notes crossed in one tick fire in authored order; the last is the displayed note.
Call `SetNotes` for a new route or deliberate replay. Supply finite world coordinates.

Without a co-driver, no note event fires and no text is retained. Crossed triggers are
consumed silently so they do not suddenly play when he returns. Removing him
immediately clears the current note. Keep ticking while he is absent.
`CurrentNote` and `RemainingSeconds` expose state for game-owned bindings. The feed
uses caller-supplied time; pass zero to pause its display timer.

`PaceNotePresenter` only writes its assigned Text. It never creates a Canvas or
moves a game HUD. Put that Text under the game's existing safe-area/HUD root.
For LWN, keep it under the game-owned `DevHud.GameRect` content, below the console;
LWN's adapter retains control of layout and console/payout suppression. The shared
package has no dependency on `AmberScreen.Dev`, LWN, a controller, camera or input
backend. Plain text only, no rich-text interpretation. Do not bind the same Text to
another presenter at the same time.

The same **single** `NavigationHud.Visible` switch hides the compass and pace-note
HUD; it does not stop route/feed events, clear registrations or hide a physical
instrument/app. When alone, next-stop data is still available to the game's app or
dash device, but no pace-note HUD text appears. Device art and interactions remain
entirely game-side. No minimap is included in 0.2.0; the optional track ribbon arrived in 0.3.0.

## Demo scenes (no sample import required)

Before running any of these commands, stop Play mode and save any scene you want to keep.
Each command creates a new **unsaved** scene; they do not change the saved scene.

- **Tools > AmberScreen > Navigation > Create Compass Demo Scene** — press Play
  to see the original spinning compass.
- **Tools > AmberScreen > Navigation > Create Route Demo Scene** — press Play.
  The next-stop label counts down through Harbour Road, Old Gate and Depot. With
  the co-driver present, authored pace-note text appears for four seconds. The demo moves
  automatically and repeats about every 18 seconds; no driving/input setup is needed.
  Select **RouteDemo** in the Hierarchy and use the Inspector's **Co Driver Present**
  checkbox: off immediately removes pace-note text but leaves next-stop info.
  Re-enable it to see only future notes, or wait for the next loop. **Hud Visible**
  hides both navigation labels without stopping progress. The instructions remain
  visible because they are demo help, not navigation HUD. The route demo explicitly
  binds a next-stop marker to its local registry; it draws no map or arrows.

- **Tools > AmberScreen > Navigation > Create Route Ribbon Demo Scene** — press Play.
  A cube travels a curved sample path. A world line sits just above a ground plane,
  and the same path is drawn in a side panel. With **Show Remaining Only** on, both
  ribbons trim behind the cube. Select **RibbonDemo** in the Hierarchy to toggle
  **Hud Visible** (both package ribbons hide; the cube keeps moving) or
  **Show Remaining Only**. The instructions remain visible because they are demo
  help, not navigation HUD. The demo draws no player pip, arrows or follow-dots.

## 0.3.0: track ribbon

`RoutePath` is a world-space polyline the game supplies. It does not find a route,
read EasyRoads, or follow the player. `SetPoints` stores a snapshot, drops non-finite
and duplicate consecutive points, and raises `Changed`. `Length` and
`CumulativeLengths` are 3D distances along that polyline. `SampleAtDistance` returns
a position and segment direction, clamped to the path. `ClosestDistance` returns the
along-path distance of the nearest point to a world position, for progress.

```csharp
var path = new RoutePath();
path.SetPoints(worldPoints); // game-authored or adapter-built
path.Changed += () => { /* rebuild bindings if needed */ };
path.SampleAtDistance(metresAlong, out Vector3 position, out Vector3 direction);
float progress = path.ClosestDistance(vehiclePosition);
```

Optional presenters draw the path only. They share the existing
`NavigationHud.Visible` switch (events and the `RoutePath` data itself keep running).
A game-owned device can draw the same path itself if that screen should stay up
when the HUD is hidden.

`RouteRibbonUI` is a `MaskableGraphic`. Put it under the game's existing Canvas
(phone/app screen or a HUD panel). Set `WorldMin` / `WorldMax` as the world XZ
rectangle that maps onto the graphic's `RectTransform`, and optional `WorldYawDeg`
to rotate that mapping about the rectangle centre. `Width` is in local UI units.
`ShowRemainingOnly` plus `ProgressDistance` trim the stroke to the untravelled
part. No player pip, arrows or follow-dots are drawn. Assign `Path` in code;
the component does not create a Canvas.

```csharp
uiRibbon.Path = path;
uiRibbon.WorldMin = new Vector2(islandMinX, islandMinZ);
uiRibbon.WorldMax = new Vector2(islandMaxX, islandMaxZ);
uiRibbon.ShowRemainingOnly = true;
uiRibbon.ProgressDistance = progress;
```

`RouteRibbonWorld` is an optional thin world `LineRenderer`. The game supplies
`lineMaterial`; if that field is empty, a `Sprites/Default` material is used.
`HeightOffset` raises the line above the road. It uses the same remaining-distance
and HUD visibility rules.

LWN should keep EasyRoads and job routing on the game side. The package never
references EasyRoads. A thin adapter can read each `LWN_*` road's centreline and
stitch a job path, then hand the points to `RoutePath`:

```csharp
// Game-side only (EasyRoads3Dv3). Do not put this in the shared package.
var net = new ERRoadNetwork();
var points = new List<Vector3>();
foreach (ERRoad road in net.GetRoadObjects())
{
    if (road == null || !road.GetName().StartsWith("LWN_")) continue;
    Vector3[] centre = road.GetSplinePointsCenter();
    if (centre == null || centre.Length == 0) continue;
    // Stitch the roads for this job here. Junctions in LWN are snapped, not
    // joined with EasyRoads intersection prefabs, so the game owns the route.
    points.AddRange(centre);
}
path.SetPoints(points);
uiRibbon.Path = path;
worldRibbon.Path = path;
```

If the network is not in the scene, the same track CSVs the LWN road builder
already uses (`index,x,y,z,kind,bridge`) are a fallback. Delivery outcomes and
Plenty Drive chrome stay in the game.

New tests: `RouteAndPaceNoteTests` (logic), `RoutePresentationTests` (Unity marker
ownership and optional uGUI presenter), `RoutePathTests` (polyline maths),
`RouteRibbonTests` (mesh vertex counts and visibility). Run them in the Sandbox's
EditMode tab.
