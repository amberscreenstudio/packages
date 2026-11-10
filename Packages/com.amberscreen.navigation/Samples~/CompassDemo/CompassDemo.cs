using AmberScreen.Navigation;
using UnityEngine;

/// <summary>Optional scene-owned demo. Does not create a camera, canvas or persistent objects.</summary>
public sealed class CompassDemo : MonoBehaviour
{
    public HeadingSource heading;
    public MarkerRegistry registry;
    public Transform spinningReference;
    public float degreesPerSecond = 20f;
    public bool hudVisible = true;
    private bool registeredNorth, registeredEast;
    private Transform previousReference;
    private bool previousHud, initialized;

    private void OnEnable()
    {
        if (heading == null || registry == null)
        {
            Debug.LogError("Assign the demo's Heading and Registry before entering Play mode.", this);
            enabled = false; return;
        }
        previousReference = heading.ReferenceTransform;
        previousHud = NavigationHud.Visible;
        initialized = true;
        if (spinningReference == null) spinningReference = transform;
        heading.ReferenceTransform = spinningReference;
        Vector3 origin = spinningReference.position;
        registeredNorth = registry.Register(new NavigationMarker("demo/north", origin + Vector3.forward * 50,
            label: "Player note", showDistance: false));
        registeredEast = registry.Register(new NavigationMarker("demo/east", origin + new Vector3(25, 0, 25),
            label: "Camp", showDistance: true));
    }
    private void Update()
    {
        spinningReference.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
        NavigationHud.Visible = hudVisible;
    }
    private void OnDisable()
    {
        if (!initialized) return;
        if (registry != null)
        {
            if (registeredNorth) registry.Unregister("demo/north");
            if (registeredEast) registry.Unregister("demo/east");
        }
        if (heading != null && heading.ReferenceTransform == spinningReference)
            heading.ReferenceTransform = previousReference;
        NavigationHud.Visible = previousHud;
        initialized = false;
        registeredNorth = registeredEast = false;
    }
}
