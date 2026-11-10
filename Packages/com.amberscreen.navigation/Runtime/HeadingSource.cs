using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>Port of SandAndInk.Map.CompassInstrument's heading/spring behaviour; no dial UI.</summary>
    [DefaultExecutionOrder(-20)]
    public sealed class HeadingSource : MonoBehaviour
    {
        [SerializeField] private Transform referenceTransform;
        [SerializeField] private Rigidbody optionalRigidbody;
        [SerializeField] private Vector3 northAxis = Vector3.forward;
        [SerializeField] private float stiffness = 26f;
        [SerializeField] private float damping = 6f;
        [SerializeField] private float bumpGain = 3.5f;
        [SerializeField] private float maxOffset = 16f;
        private readonly CompassSpring spring = new CompassSpring();

        public Transform ReferenceTransform
        {
            get => referenceTransform != null ? referenceTransform : transform;
            set => referenceTransform = value;
        }
        public Rigidbody OptionalRigidbody { get => optionalRigidbody; set => optionalRigidbody = value; }
        public Vector3 NorthAxis { get => northAxis; set => northAxis = value; }
        public float Stiffness { get => stiffness; set => stiffness = value; }
        public float Damping { get => damping; set => damping = value; }
        public float BumpGain { get => bumpGain; set => bumpGain = value; }
        public float MaxOffset { get => maxOffset; set => maxOffset = value; }
        public Vector3 Position => ReferenceTransform.position;
        public float HeadingDeg => spring.HeadingDeg;
        public float TrueHeadingDeg
        {
            get
            {
                // Keep the proven game's forward projection and Atan2 calculation.
                Vector3 fwd = ReferenceTransform.forward;
                fwd.y = 0f;
                fwd.Normalize();
                float heading = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
                // Optional north-axis adaptation; +Z returns the original signed Atan2 result.
                Vector3 north = northAxis;
                north.y = 0f;
                if (north.sqrMagnitude < 1e-12f) north = Vector3.forward;
                north.Normalize();
                float northHeading = Mathf.Atan2(north.x, north.z) * Mathf.Rad2Deg;
                return northHeading == 0f ? heading : NavigationMath.WrapSigned(heading - northHeading);
            }
        }
        public float RawHeading => NavigationMath.Wrap360(TrueHeadingDeg);

        private void OnEnable() => SetHeading(TrueHeadingDeg);
        private void Update() => Tick(Time.deltaTime);
        public void SetHeading(float degrees) => spring.SetHeading(degrees);
        public void Tick(float deltaTime) => spring.Step(TrueHeadingDeg, deltaTime,
            optionalRigidbody != null ? optionalRigidbody.linearVelocity.y : 0f,
            stiffness, damping, bumpGain, maxOffset);

        public float BearingTo(Vector3 position)
        {
            Vector3 from = Position;
            return NavigationMath.Bearing(from.x, from.z, position.x, position.z,
                northAxis.x, northAxis.z, RawHeading);
        }
    }
}
