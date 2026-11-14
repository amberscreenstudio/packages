using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AmberScreen.Inspect
{
    /// <summary>
    /// Close-up inspection: <see cref="Inspect"/> brings an object from where it is to a hold point in front of
    /// the camera; the player rotates and zooms it and steps through <see cref="InspectHotspot"/>s;
    /// <see cref="EndInspect"/> puts it back with its original parent, pose, layers, physics and colliders.
    /// The game disables its own player controls from <see cref="onPlayerControlsEnabled"/> or
    /// <see cref="PlayerControlsCallback"/>, and may dim/blur the background from <see cref="onBackgroundFocus"/>.
    /// </summary>
    public sealed class InspectViewer : MonoBehaviour
    {
        [Serializable] public sealed class TransformEvent : UnityEvent<Transform> { }
        [Serializable] public sealed class BoolEvent : UnityEvent<bool> { }
        [Serializable] public sealed class StringEvent : UnityEvent<string> { }

        public enum Phase
        {
            Idle,
            Entering,
            Holding,
            Leaving
        }

        [Tooltip("Camera to hold objects in front of (empty = Camera.main).")]
        public Camera viewCamera;

        [Header("Distance")]
        public float holdDistance = 0.7f;
        public float minDistance = 0.3f;
        public float maxDistance = 1.5f;
        [Tooltip("Metres per mouse-wheel notch.")]
        public float scrollZoomStep = 0.08f;
        [Tooltip("Metres per second at full trigger.")]
        public float triggerZoomSpeed = 0.8f;

        [Header("Rotation")]
        public float dragDegreesPerPixel = 0.3f;
        public float stickDegreesPerSecond = 160f;
        public bool invertVertical;

        [Header("Motion (seconds, unscaled time)")]
        public float transitionSeconds = 0.35f;
        public float focusSeconds = 0.3f;

        [Header("While inspecting")]
        [Tooltip("Move the object (and its children) to this layer while inspecting. -1 = leave layers alone.")]
        public int inspectLayer = -1;
        public bool makeRigidbodiesKinematic = true;
        public bool disableColliders = true;
        [Tooltip("Read mouse/keyboard/gamepad here. Turn off to drive Rotate/Zoom/NextHotspot from your own input.")]
        public bool handleInput = true;

        [Header("Events")]
        public TransformEvent onInspectStarted = new TransformEvent();
        public TransformEvent onInspectEnded = new TransformEvent();
        [Tooltip("false when inspecting starts (turn your player controls off), true when it has ended.")]
        public BoolEvent onPlayerControlsEnabled = new BoolEvent();
        [Tooltip("true while inspecting: dim or blur the background here if you want to.")]
        public BoolEvent onBackgroundFocus = new BoolEvent();
        [Tooltip("Label of the focused spot, or empty when no spot is focused.")]
        public StringEvent onHotspotLabel = new StringEvent();

        /// <summary>Same as <see cref="onPlayerControlsEnabled"/>, for code.</summary>
        public Action<bool> PlayerControlsCallback;
        public event Action<Transform> InspectStarted;
        public event Action<Transform> InspectEnded;
        public event Action<InspectHotspot> HotspotFocused;

        sealed class SavedState
        {
            public Transform parent;
            public int siblingIndex;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public Vector3 pivotWorldPosition;
            public Quaternion pivotWorldRotation;
            public readonly List<KeyValuePair<GameObject, int>> layers = new List<KeyValuePair<GameObject, int>>();
            public readonly List<KeyValuePair<Rigidbody, bool>> bodies = new List<KeyValuePair<Rigidbody, bool>>();
            public readonly List<KeyValuePair<Collider, bool>> colliders = new List<KeyValuePair<Collider, bool>>();
        }

        Transform m_Target;
        Transform m_Pivot;
        SavedState m_Saved;
        Phase m_Phase;
        float m_T;
        Vector3 m_EnterFromLocal;
        Vector3 m_LeaveFromPosition;
        Quaternion m_LeaveFromRotation;
        float m_Distance;
        float m_Min, m_Max;
        bool m_Focusing;
        Quaternion m_FocusRotation;
        int m_StartedFrame = -1;
        readonly List<InspectHotspot> m_Hotspots = new List<InspectHotspot>();
        int m_HotspotIndex = -1;

        public bool IsInspecting => m_Phase != Phase.Idle;
        public Phase CurrentPhase => m_Phase;
        public Transform Current => m_Target;
        public float Distance => m_Distance;
        public IReadOnlyList<InspectHotspot> Hotspots => m_Hotspots;
        public InspectHotspot FocusedHotspot =>
            m_HotspotIndex >= 0 && m_HotspotIndex < m_Hotspots.Count ? m_Hotspots[m_HotspotIndex] : null;
        /// <summary>The holder the object is parented to while inspected (a child of the camera).</summary>
        public Transform Pivot => m_Pivot;

        // ---------------- public API ----------------

        /// <summary>Starts inspecting <paramref name="target"/>. Ends any current inspection at once first.</summary>
        public bool Inspect(Transform target)
        {
            if (target == null) return false;
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Inspect] No camera: assign InspectViewer.viewCamera or tag one MainCamera.", this);
                return false;
            }
            if (cam.transform.IsChildOf(target))
            {
                Debug.LogWarning("[Inspect] Can't inspect an object that holds the camera.", this);
                return false;
            }
            if (IsInspecting) EndInspect(true);

            m_Target = target;
            m_Saved = Capture(target);

            if (m_Pivot == null)
            {
                m_Pivot = new GameObject("InspectPivot").transform;
            }
            m_Pivot.SetParent(cam.transform, false);
            m_Pivot.localScale = Vector3.one;
            m_Pivot.SetPositionAndRotation(BoundsCentre(target), target.rotation);
            m_Saved.pivotWorldPosition = m_Pivot.position;
            m_Saved.pivotWorldRotation = m_Pivot.rotation;
            target.SetParent(m_Pivot, true);

            if (inspectLayer >= 0 && inspectLayer <= 31)
                foreach (var pair in m_Saved.layers) pair.Key.layer = inspectLayer;
            if (makeRigidbodiesKinematic)
                foreach (var pair in m_Saved.bodies) pair.Key.isKinematic = true;
            if (disableColliders)
                foreach (var pair in m_Saved.colliders) pair.Key.enabled = false;

            var inspectable = target.GetComponent<Inspectable>();
            m_Min = Mathf.Min(minDistance, maxDistance);
            m_Max = Mathf.Max(minDistance, maxDistance);
            float hold = inspectable != null && inspectable.holdDistance > 0f ? inspectable.holdDistance : holdDistance;
            m_Distance = InspectMath.ClampDistance(hold, m_Min, m_Max);

            m_Hotspots.Clear();
            target.GetComponentsInChildren(true, m_Hotspots);
            m_Hotspots.Sort((a, b) => a.order.CompareTo(b.order));
            m_HotspotIndex = -1;
            m_Focusing = false;

            m_EnterFromLocal = m_Pivot.localPosition;
            m_StartedFrame = Time.frameCount;
            m_T = 0f;
            m_Phase = Phase.Entering;
            if (transitionSeconds <= 0f) FinishEnter();

            PlayerControlsCallback?.Invoke(false);
            onPlayerControlsEnabled.Invoke(false);
            onBackgroundFocus.Invoke(true);
            InspectStarted?.Invoke(target);
            onInspectStarted.Invoke(target);
            return true;
        }

        /// <summary>Puts the object back (animated, or at once with <paramref name="immediate"/>).</summary>
        public void EndInspect(bool immediate = false)
        {
            if (!IsInspecting) return;
            if (immediate || transitionSeconds <= 0f || m_Target == null || m_Pivot == null)
            {
                Restore();
                return;
            }
            if (m_Phase == Phase.Leaving) return;
            m_LeaveFromPosition = m_Pivot.position;
            m_LeaveFromRotation = m_Pivot.rotation;
            m_T = 0f;
            m_Phase = Phase.Leaving;
            ClearHotspotFocus();
        }

        /// <summary>Turns the object by degrees in view space (x = left/right, y = up/down).</summary>
        public void Rotate(Vector2 degrees)
        {
            if (m_Pivot == null || !IsInspecting || m_Phase == Phase.Leaving) return;
            if (invertVertical) degrees.y = -degrees.y;
            m_Focusing = false;
            m_Pivot.localRotation = InspectMath.RotateInViewSpace(m_Pivot.localRotation, degrees);
        }

        /// <summary>Changes the hold distance by <paramref name="metres"/> (negative = closer), clamped.</summary>
        public void Zoom(float metres)
        {
            if (!IsInspecting) return;
            m_Distance = InspectMath.ClampDistance(m_Distance + metres, m_Min, m_Max);
        }

        public void SetDistance(float metres)
        {
            if (!IsInspecting) return;
            m_Distance = InspectMath.ClampDistance(metres, m_Min, m_Max);
        }

        public void NextHotspot() => FocusHotspot(InspectMath.Step(m_HotspotIndex, 1, m_Hotspots.Count));
        public void PreviousHotspot() => FocusHotspot(InspectMath.Step(m_HotspotIndex, -1, m_Hotspots.Count));

        /// <summary>Turns (and optionally zooms) so the spot faces the camera, and raises the label events.</summary>
        public void FocusHotspot(int index)
        {
            if (!IsInspecting || m_Pivot == null || index < 0 || index >= m_Hotspots.Count) return;
            var spot = m_Hotspots[index];
            if (spot == null) return;
            m_HotspotIndex = index;
            m_FocusRotation = FocusRotationFor(spot);
            m_Focusing = true;
            if (focusSeconds <= 0f) m_Pivot.localRotation = m_FocusRotation;
            if (spot.focusDistance > 0f) SetDistance(spot.focusDistance);
            if (m_Phase == Phase.Holding && focusSeconds <= 0f) m_Pivot.localPosition = HoldLocal;

            HotspotFocused?.Invoke(spot);
            onHotspotLabel.Invoke(spot.label ?? "");
        }

        public void ClearHotspotFocus()
        {
            bool had = m_HotspotIndex >= 0;
            m_HotspotIndex = -1;
            m_Focusing = false;
            if (had)
            {
                HotspotFocused?.Invoke(null);
                onHotspotLabel.Invoke("");
            }
        }

        // ---------------- frame update ----------------

        void Update() => Tick(Time.unscaledDeltaTime);

        void OnDisable()
        {
            if (IsInspecting) Restore();
        }

        void OnDestroy()
        {
            if (m_Pivot == null) return;
            if (Application.isPlaying) Destroy(m_Pivot.gameObject);
            else DestroyImmediate(m_Pivot.gameObject);
        }

        /// <summary>Advances animation and input by <paramref name="dt"/> seconds. Called from Update.</summary>
        public void Tick(float dt)
        {
            if (handleInput) InspectInput.Poll();
            if (!IsInspecting) return;
            if (m_Target == null || m_Pivot == null)
            {
                // The object was destroyed while held: give the controls back.
                m_Target = null;
                Restore();
                return;
            }

            switch (m_Phase)
            {
                case Phase.Entering:
                    m_T += dt / Mathf.Max(0.0001f, transitionSeconds);
                    m_Pivot.localPosition = Vector3.Lerp(m_EnterFromLocal, HoldLocal, Mathf.SmoothStep(0f, 1f, m_T));
                    if (m_T >= 1f) FinishEnter();
                    break;

                case Phase.Holding:
                    if (handleInput && Time.frameCount != m_StartedFrame) ReadInput(dt);
                    if (m_Phase != Phase.Holding) break;
                    float k = 1f - Mathf.Exp(-dt * 12f);
                    m_Pivot.localPosition = Vector3.Lerp(m_Pivot.localPosition, HoldLocal, k);
                    if (m_Focusing)
                    {
                        float f = focusSeconds <= 0f ? 1f : 1f - Mathf.Exp(-dt * 4f / focusSeconds);
                        m_Pivot.localRotation = Quaternion.Slerp(m_Pivot.localRotation, m_FocusRotation, f);
                    }
                    break;

                case Phase.Leaving:
                    m_T += dt / Mathf.Max(0.0001f, transitionSeconds);
                    float s = Mathf.SmoothStep(0f, 1f, m_T);
                    m_Pivot.SetPositionAndRotation(
                        Vector3.Lerp(m_LeaveFromPosition, m_Saved.pivotWorldPosition, s),
                        Quaternion.Slerp(m_LeaveFromRotation, m_Saved.pivotWorldRotation, s));
                    if (m_T >= 1f) Restore();
                    break;
            }
        }

        void ReadInput(float dt)
        {
            if (InspectInput.ExitPressed)
            {
                EndInspect();
                return;
            }
            if (InspectInput.NextPressed) NextHotspot();
            if (InspectInput.PreviousPressed) PreviousHotspot();

            Vector2 rotate = Vector2.zero;
            if (InspectInput.PrimaryHeld) rotate += InspectInput.PointerDelta * dragDegreesPerPixel;
            rotate += InspectInput.Stick * (stickDegreesPerSecond * dt);
            if (rotate.sqrMagnitude > 1e-6f) Rotate(rotate);

            float scroll = InspectInput.ScrollSteps;
            if (scroll != 0f) Zoom(-scroll * scrollZoomStep);
            float trig = InspectInput.ZoomAxis;
            if (Mathf.Abs(trig) > 0.05f) Zoom(-trig * triggerZoomSpeed * dt);
        }

        Vector3 HoldLocal => new Vector3(0f, 0f, m_Distance);

        void FinishEnter()
        {
            m_Pivot.localPosition = HoldLocal;
            m_Phase = Phase.Holding;
            m_T = 1f;
        }

        Quaternion FocusRotationFor(InspectHotspot spot)
        {
            Quaternion current = m_Pivot.localRotation;
            if (spot.viewFrom == InspectHotspot.ViewFrom.HotspotForward)
            {
                // Spot's forward should point at the camera (-Z in camera space) with its up kept up.
                Quaternion spotInPivot = Quaternion.Inverse(m_Pivot.rotation) * spot.transform.rotation;
                return Quaternion.LookRotation(Vector3.back, Vector3.up) * Quaternion.Inverse(spotInPivot);
            }

            Vector3 dir = m_Pivot.InverseTransformPoint(spot.transform.position);
            if (dir.sqrMagnitude < 1e-8f) dir = m_Pivot.InverseTransformDirection(spot.transform.forward);
            Vector3 dirInCamera = current * dir.normalized;
            return Quaternion.FromToRotation(dirInCamera, Vector3.back) * current;
        }

        // ---------------- capture / restore ----------------

        SavedState Capture(Transform target)
        {
            var s = new SavedState
            {
                parent = target.parent,
                siblingIndex = target.GetSiblingIndex(),
                localPosition = target.localPosition,
                localRotation = target.localRotation,
                localScale = target.localScale
            };
            foreach (var t in target.GetComponentsInChildren<Transform>(true))
                s.layers.Add(new KeyValuePair<GameObject, int>(t.gameObject, t.gameObject.layer));
            foreach (var rb in target.GetComponentsInChildren<Rigidbody>(true))
                s.bodies.Add(new KeyValuePair<Rigidbody, bool>(rb, rb.isKinematic));
            foreach (var c in target.GetComponentsInChildren<Collider>(true))
                s.colliders.Add(new KeyValuePair<Collider, bool>(c, c.enabled));
            return s;
        }

        void Restore()
        {
            var target = m_Target;
            var saved = m_Saved;
            m_Phase = Phase.Idle;
            m_Target = null;
            m_Saved = null;
            m_Focusing = false;
            bool hadSpot = m_HotspotIndex >= 0;
            m_HotspotIndex = -1;
            m_Hotspots.Clear();

            if (target != null && saved != null)
            {
                target.SetParent(saved.parent, false);
                if (saved.parent == null || saved.siblingIndex < saved.parent.childCount)
                    target.SetSiblingIndex(saved.siblingIndex);
                target.localPosition = saved.localPosition;
                target.localRotation = saved.localRotation;
                target.localScale = saved.localScale;
                foreach (var pair in saved.layers)
                    if (pair.Key != null) pair.Key.layer = pair.Value;
                foreach (var pair in saved.colliders)
                    if (pair.Key != null) pair.Key.enabled = pair.Value;
                foreach (var pair in saved.bodies)
                {
                    if (pair.Key == null) continue;
                    pair.Key.isKinematic = pair.Value;
                    if (!pair.Value)
                    {
                        pair.Key.linearVelocity = Vector3.zero;
                        pair.Key.angularVelocity = Vector3.zero;
                    }
                }
            }

            if (m_Pivot != null)
            {
                m_Pivot.localPosition = Vector3.zero;
                m_Pivot.localRotation = Quaternion.identity;
            }

            if (hadSpot) onHotspotLabel.Invoke("");
            InspectEnded?.Invoke(target);
            onInspectEnded.Invoke(target);
            onBackgroundFocus.Invoke(false);
            PlayerControlsCallback?.Invoke(true);
            onPlayerControlsEnabled.Invoke(true);
        }

        static Vector3 BoundsCentre(Transform target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            bool any = false;
            var bounds = new Bounds();
            foreach (var r in renderers)
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (!any)
                {
                    bounds = r.bounds;
                    any = true;
                }
                else bounds.Encapsulate(r.bounds);
            }
            return any ? bounds.center : target.position;
        }
    }
}
