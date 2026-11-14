using UnityEngine;

namespace AmberScreen.VehicleWater.Demo
{
    /// <summary>Demo-only "engine": pushes a box forward while running. Stands in for a real vehicle controller.</summary>
    public sealed class BoxCarEngine : MonoBehaviour, IVehicleEngineControl
    {
        public Rigidbody body;
        [Tooltip("-1 reverse, 0 coast, 1 forward.")]
        public float throttle = 1f;
        public float acceleration = 3f;
        public float maxSpeed = 6f;

        bool m_Running = true;
        PhysicsMaterial m_Slide;

        public bool IsEngineRunning => m_Running;
        public bool CanStart { get; set; } = true;

        public void Stall() => m_Running = false;

        public void Restart()
        {
            if (CanStart) m_Running = true;
        }

        void Awake()
        {
            // Low friction so the box slides like a car on wheels (made at runtime, no asset).
            var col = GetComponent<Collider>();
            if (col == null) return;
            m_Slide = new PhysicsMaterial("BoxCarSlide")
            {
                dynamicFriction = 0.15f,
                staticFriction = 0.15f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            col.sharedMaterial = m_Slide;
        }

        void OnDestroy()
        {
            if (m_Slide != null) Destroy(m_Slide);
        }

        void FixedUpdate()
        {
            if (!m_Running || body == null || Mathf.Approximately(throttle, 0f)) return;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float speed = Vector3.Dot(body.linearVelocity, forward);
            if (throttle > 0f && speed > maxSpeed) return;
            if (throttle < 0f && speed < -maxSpeed) return;
            body.AddForce(transform.forward * (throttle * acceleration), ForceMode.Acceleration);
        }
    }
}
