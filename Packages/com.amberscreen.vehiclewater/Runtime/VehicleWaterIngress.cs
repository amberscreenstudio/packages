using System;
using UnityEngine;
using UnityEngine.Events;

namespace AmberScreen.VehicleWater
{
    /// <summary>
    /// Put on a vehicle. Each physics step it measures the water over the intake and cabin points:
    /// too deep at the intake stalls the engine, water over the cabin floods it over time (0..1), and the
    /// vehicle is slowed to a stop. Out of the water the cabin drains slowly; the engine may restart only
    /// when drained and when the game allows it.
    /// </summary>
    public sealed class VehicleWaterIngress : MonoBehaviour
    {
        [Serializable] public sealed class FloatEvent : UnityEvent<float> { }

        public Rigidbody body;
        [Tooltip("Air intake (engine). Empty = this transform.")]
        public Transform intakePoint;
        [Tooltip("Cabin floor. Empty = this transform.")]
        public Transform cabinPoint;
        [Tooltip("A component implementing IWaterHeightProvider. Empty = first enabled FlatWaterPlane.")]
        public MonoBehaviour waterSource;
        [Tooltip("A component implementing IVehicleEngineControl (optional).")]
        public MonoBehaviour engineSource;
        public WaterIngressSettings settings = new WaterIngressSettings();
        [Tooltip("Restart the engine by itself once drained (if the game allows). Off = the player starts it.")]
        public bool autoRestartWhenDrained;

        public UnityEvent onStalled = new UnityEvent();
        public UnityEvent onFloodStarted = new UnityEvent();
        public UnityEvent onDrained = new UnityEvent();
        public UnityEvent onRestartAllowed = new UnityEvent();
        public UnityEvent onRestarted = new UnityEvent();
        public UnityEvent onStoppedByWater = new UnityEvent();
        public UnityEvent onReleasedByWater = new UnityEvent();
        public FloatEvent onFloodChanged = new FloatEvent();

        /// <summary>The game's say on restarting (for example "not during a cutscene"). Null = always allowed.</summary>
        public Func<bool> RestartAllowed;
        public event Action<WaterIngressEvents> Changed;

        readonly WaterIngressModel m_Model = new WaterIngressModel();
        IWaterHeightProvider m_Water;
        IVehicleEngineControl m_Engine;
        bool m_DampingApplied;
        float m_BaseLinearDamping, m_BaseAngularDamping;
        float m_LastReportedFlood = -1f;

        public WaterIngressModel Model => m_Model;
        public float IntakeDepth { get; private set; } = float.NegativeInfinity;
        public float CabinDepth { get; private set; } = float.NegativeInfinity;
        public float Flood => m_Model.Flood;
        public bool IsStalled => m_Model.IsStalled;
        public bool IsStoppedByWater => m_Model.IsStoppedByWater;
        public bool CanRestart => m_Model.CanRestart;
        public IVehicleEngineControl Engine => ResolveEngine();
        public IWaterHeightProvider Water => ResolveWater();

        public void SetWaterProvider(IWaterHeightProvider water) => m_Water = water;
        public void SetEngineControl(IVehicleEngineControl engine) => m_Engine = engine;

        void Reset()
        {
            body = GetComponentInParent<Rigidbody>();
        }

        void Awake()
        {
            if (body == null) body = GetComponentInParent<Rigidbody>();
        }

        void OnDisable() => RemoveDamping();

        void FixedUpdate() => Simulate(Time.fixedDeltaTime);

        /// <summary>One step of the rules (FixedUpdate calls this; tests may call it directly).</summary>
        public WaterIngressEvents Simulate(float dt)
        {
            m_Model.Settings = settings;
            var water = ResolveWater();
            Vector3 intake = (intakePoint != null ? intakePoint : transform).position;
            Vector3 cabin = (cabinPoint != null ? cabinPoint : transform).position;
            IntakeDepth = water != null ? WaterIngressModel.Depth(water.GetWaterHeight(intake), intake.y) : float.NegativeInfinity;
            CabinDepth = water != null ? WaterIngressModel.Depth(water.GetWaterHeight(cabin), cabin.y) : float.NegativeInfinity;

            bool allowed = RestartAllowed == null || RestartAllowed();
            var events = m_Model.Step(IntakeDepth, CabinDepth, dt, allowed);
            var engine = ResolveEngine();

            if ((events & WaterIngressEvents.Stalled) != 0)
            {
                if (engine != null)
                {
                    engine.CanStart = false;
                    engine.Stall();
                }
                onStalled.Invoke();
            }
            else if (engine != null && m_Model.IsStalled && !m_Model.CanRestart && engine.IsEngineRunning)
            {
                // Something restarted it while still drowned: stall again.
                engine.CanStart = false;
                engine.Stall();
            }

            if ((events & WaterIngressEvents.FloodStarted) != 0) onFloodStarted.Invoke();
            if ((events & WaterIngressEvents.Drained) != 0) onDrained.Invoke();
            if ((events & WaterIngressEvents.StoppedByWater) != 0) onStoppedByWater.Invoke();
            if ((events & WaterIngressEvents.ReleasedByWater) != 0) onReleasedByWater.Invoke();
            if ((events & WaterIngressEvents.RestartAllowed) != 0)
            {
                if (engine != null) engine.CanStart = true;
                onRestartAllowed.Invoke();
                if (autoRestartWhenDrained) TryRestart();
            }

            if (Mathf.Abs(m_Model.Flood - m_LastReportedFlood) >= 0.001f)
            {
                m_LastReportedFlood = m_Model.Flood;
                onFloodChanged.Invoke(m_Model.Flood);
            }

            ApplyStopping(dt);
            if (events != WaterIngressEvents.None) Changed?.Invoke(events);
            return events;
        }

        /// <summary>Restarts the engine if drained and allowed. Returns true when it restarted.</summary>
        public bool TryRestart()
        {
            if (!m_Model.CanRestart) return false;
            var engine = ResolveEngine();
            if (engine != null)
            {
                engine.CanStart = true;
                engine.Restart();
            }
            m_Model.NotifyRestarted();
            onRestarted.Invoke();
            return true;
        }

        /// <summary>Back to dry and running-allowed (for respawns and loads).</summary>
        public void ResetWater()
        {
            m_Model.Reset();
            RemoveDamping();
            var engine = ResolveEngine();
            if (engine != null) engine.CanStart = true;
            m_LastReportedFlood = -1f;
        }

        void ApplyStopping(float dt)
        {
            if (body == null) return;
            if (m_Model.IsStoppedByWater)
            {
                if (!m_DampingApplied)
                {
                    m_BaseLinearDamping = body.linearDamping;
                    m_BaseAngularDamping = body.angularDamping;
                    body.linearDamping = m_BaseLinearDamping + settings.extraLinearDamping;
                    body.angularDamping = m_BaseAngularDamping + settings.extraAngularDamping;
                    m_DampingApplied = true;
                }

                if (!body.isKinematic && settings.brakeDeceleration > 0f && dt > 0f)
                {
                    Vector3 v = body.linearVelocity;
                    Vector3 flat = new Vector3(v.x, 0f, v.z);
                    float speed = flat.magnitude;
                    if (speed > 1e-4f)
                    {
                        float dv = Mathf.Min(speed, settings.brakeDeceleration * dt);
                        body.AddForce(-flat / speed * dv, ForceMode.VelocityChange);
                    }
                }
            }
            else
            {
                RemoveDamping();
            }
        }

        void RemoveDamping()
        {
            if (!m_DampingApplied || body == null) return;
            body.linearDamping = m_BaseLinearDamping;
            body.angularDamping = m_BaseAngularDamping;
            m_DampingApplied = false;
        }

        IWaterHeightProvider ResolveWater()
        {
            if (m_Water != null && !(m_Water is UnityEngine.Object o && o == null)) return m_Water;
            m_Water = waterSource as IWaterHeightProvider;
            if (m_Water == null && FlatWaterPlane.Active.Count > 0) m_Water = FlatWaterPlane.Active[0];
            return m_Water;
        }

        IVehicleEngineControl ResolveEngine()
        {
            if (m_Engine != null && !(m_Engine is UnityEngine.Object o && o == null)) return m_Engine;
            m_Engine = engineSource as IVehicleEngineControl;
            return m_Engine;
        }

        void OnValidate()
        {
            if (waterSource != null && !(waterSource is IWaterHeightProvider))
            {
                Debug.LogWarning($"[VehicleWater] {waterSource.GetType().Name} does not implement IWaterHeightProvider.", this);
                waterSource = null;
            }
            if (engineSource != null && !(engineSource is IVehicleEngineControl))
            {
                Debug.LogWarning($"[VehicleWater] {engineSource.GetType().Name} does not implement IVehicleEngineControl.", this);
                engineSource = null;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f);
            Gizmos.DrawWireSphere((intakePoint != null ? intakePoint : transform).position, 0.08f);
            Gizmos.color = new Color(0.2f, 0.6f, 1f);
            Gizmos.DrawWireSphere((cabinPoint != null ? cabinPoint : transform).position, 0.08f);
        }
    }
}
