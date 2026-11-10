# Compass Demo (optional, no scene asset)

Use a disposable Unity 6 URP scene after installing the package. Import this sample
from the package's Samples entry, or copy this folder under your project's Assets.
You need a Font asset your game owns: drag a `.ttf`/`.otf` file into Assets first.
No font is downloaded or loaded by the package.

1. In the Hierarchy, create an empty GameObject named `NavDemo`. In its Inspector,
   use Add Component to add `HeadingSource`, `MarkerRegistry` and `CompassDemo`.
2. Create an empty GameObject named `Body`, at position (0,0,0), rotation (0,0,0).
3. On `NavDemo`, assign Heading Source's Reference Transform to `Body`. On
   Compass Demo assign Heading to NavDemo's HeadingSource, Registry to its
   MarkerRegistry, and Spinning Reference to `Body`. Leave HUD Visible checked.
4. Create a Canvas (the standard GameObject > UI > Canvas command). Set its
   Render Mode to Screen Space - Overlay. A camera is not needed for this UI.
   On its Canvas Scaler, choose Scale With Screen Size, Reference Resolution
   1280 x 800, and Match 0.5.
5. Under the Canvas create a UI Image and name it `CompassStrip`. Remove its
   Image component in the Inspector, leaving its RectTransform. Set anchors min and max to
   (0.5,1), pivot (0.5,1), anchored position (0,-20), width 640, height 110,
   local scale (1,1,1). Add `CompassStripPresenter`.
6. Assign the presenter's Heading Source and Registry from `NavDemo`. Drag your
   Font asset to Font. Leave Marker Types empty for text-only markers. Set Field
   Of View to 120. Leave Use Raw Heading off. HeadingSource uses the proven
   spring defaults: stiffness 26, damping 6, bump gain 3.5 and max offset 16.
7. Press Play. `Body` spins clockwise. N/E/S/W ticks pass through the strip.
   The script explicitly registers `Player note` 50 units north without distance,
   and `Camp` at (25,0,25) with distance. Both come into view as their bearings pass
   through the FOV; they are not world arrows and are not automatically discovered.
8. In Play mode, uncheck Compass Demo's HUD Visible. The strip disappears while
   the heading still updates. Recheck to restore it. Uncheck the presenter's
   Show Distances: Camp loses its distance but remains registered.
9. Set Degrees Per Second to 0; rotate Body's Y value manually from 359 to 1.
   The spring crosses north the short way. Adjust HeadingSource Stiffness and
   Damping to judge instrument feel; restore 26 and 6 afterwards. Enable Use Raw
   Heading on the presenter to compare exact heading without the spring. Optional
   Rigidbody on HeadingSource can supply vertical velocity for physical bump response.
10. Disable CompassDemo during Play: its two owned registrations are removed.
    Disable the presenter alone to check it hides only its own strip.
11. In the Game view use the resolution/aspect selector to test 1280x800,
    1920x1080 and an ultrawide size. Check the font and your game's safe-area
    integration. Dense markers/long labels can overlap or be clipped; this small
    first iteration does not provide label decluttering.
12. Stop Play. To run automated tests, use Window > General > Test Runner,
    select EditMode and run AmberScreen.Navigation.Tests.Editor.

This sample never creates a persistent host and does not modify ProjectSettings.
A font, Canvas layout, marker types and scene wiring belong to the integrating game.
