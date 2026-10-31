using UnityEngine;

namespace AmberScreen.ModelReview
{
    /// <summary>Orbit / interior review camera. Owned by <see cref="ModelReviewRig"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class ReviewOrbitCamera : MonoBehaviour
    {
        public const float DefaultFov = 40f;
        public const float InteriorFov = 75f;
        public const float FramePadding = 1.35f;
        public const float DefaultMinDistance = 0.15f;
        public const float DefaultMaxDistance = 250f;
        public const float ExteriorPitchMin = -15f;
        public const float ExteriorPitchMax = 75f;

        [SerializeField] Camera targetCamera;
        [SerializeField] float orbitSensitivity = 0.2f;
        [SerializeField] float panSensitivity = 0.0025f;
        [SerializeField, Range(0.01f, 0.5f), Tooltip("Fraction of the current distance per scroll-wheel notch.")]
        float zoomSpeed = 0.15f;
        [SerializeField] float moveSpeed = 2.5f;

        float _yaw = 35f;
        float _pitch = 25f;
        float _distance = 5f;
        float _minDistance = DefaultMinDistance;
        float _maxDistance = DefaultMaxDistance;
        Vector3 _pivot;
        bool _interior;
        Bounds _lastBounds;

        public Camera Cam => targetCamera != null ? targetCamera : GetComponent<Camera>();
        public bool InteriorMode => _interior;
        public Vector3 Pivot => _pivot;
        public float Distance => _distance;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;

        public float ZoomSpeed
        {
            get => zoomSpeed;
            set => zoomSpeed = Mathf.Clamp(value, 0.01f, 0.5f);
        }

        public void BindCamera(Camera cam) => targetCamera = cam;

        /// <summary>
        /// Distance after one or more scroll notches. Scroll up (positive) zooms in.
        /// Values with |rawScroll| ≥ 10 are treated as legacy 120-units-per-notch.
        /// </summary>
        public static float ZoomedDistance(float distance, float rawScroll, float zoomSpeed) =>
            ZoomedDistance(distance, rawScroll, zoomSpeed, DefaultMinDistance, DefaultMaxDistance);

        public static float ZoomedDistance(
            float distance, float rawScroll, float zoomSpeed, float minDistance, float maxDistance)
        {
            float notches = Mathf.Abs(rawScroll) >= 10f ? rawScroll / 120f : rawScroll;
            float speed = Mathf.Clamp(zoomSpeed, 0.01f, 0.5f);
            float min = Mathf.Max(0.001f, minDistance);
            float max = Mathf.Max(min, maxDistance);
            return Mathf.Clamp(distance * Mathf.Pow(1f - speed, notches), min, max);
        }

        public static void LimitsForBounds(Bounds bounds, out float minDistance, out float maxDistance)
        {
            float size = Mathf.Max(bounds.size.magnitude, 0.05f);
            minDistance = Mathf.Max(0.01f, 0.05f * size);
            maxDistance = Mathf.Max(minDistance + 0.01f, 20f * size);
        }

        public void SetZoomLimits(float minDistance, float maxDistance)
        {
            _minDistance = Mathf.Max(0.01f, minDistance);
            _maxDistance = Mathf.Max(_minDistance + 0.01f, maxDistance);
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
        }

        public void Frame(Bounds bounds)
        {
            _lastBounds = bounds;
            LimitsForBounds(bounds, out _minDistance, out _maxDistance);
            _pivot = bounds.center;
            var cam = Cam;
            if (cam == null) return;
            float fov = cam.fieldOfView * Mathf.Deg2Rad;
            float radius = Mathf.Max(bounds.extents.magnitude, 0.05f);
            _distance = radius / Mathf.Sin(Mathf.Max(fov * 0.5f, 0.05f)) * FramePadding;
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            cam.nearClipPlane = Mathf.Max(0.01f * _distance, 0.01f);
            cam.farClipPlane = Mathf.Max(cam.nearClipPlane * 1000f, _distance * 20f);
            if (!_interior)
                ApplyOrbitTransform();
        }

        public void ResetExterior()
        {
            _interior = false;
            var cam = Cam;
            if (cam != null) cam.fieldOfView = DefaultFov;
            _yaw = 35f;
            _pitch = 25f;
            if (_lastBounds.size.sqrMagnitude > 0f)
                Frame(_lastBounds);
            else
                ApplyOrbitTransform();
        }

        public void SetInterior(bool on)
        {
            _interior = on;
            var cam = Cam;
            if (cam == null) return;
            if (on)
            {
                cam.nearClipPlane = 0.01f;
                cam.fieldOfView = InteriorFov;
            }
            else
            {
                cam.fieldOfView = DefaultFov;
                if (_lastBounds.size.sqrMagnitude > 0f)
                    Frame(_lastBounds);
                else
                    ApplyOrbitTransform();
            }
        }

        public void ToggleInterior() => SetInterior(!_interior);

        public void ApplyOrbitDelta(float yawDelta, float pitchDelta)
        {
            _yaw += yawDelta;
            _pitch = Mathf.Clamp(_pitch - pitchDelta, ExteriorPitchMin, ExteriorPitchMax);
            ApplyOrbitTransform();
        }

        public void ApplyPanDelta(Vector2 delta)
        {
            var cam = Cam;
            if (cam == null) return;
            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;
            float scale = _distance * panSensitivity;
            _pivot -= (right * delta.x + up * delta.y) * scale;
            ApplyOrbitTransform();
        }

        public void ApplyZoomScroll(float rawScroll)
        {
            if (Mathf.Abs(rawScroll) <= 0.01f) return;
            _distance = ZoomedDistance(_distance, rawScroll, zoomSpeed, _minDistance, _maxDistance);
            ApplyOrbitTransform();
        }

        public void ApplyInteriorLook(float yawDelta, float pitchDelta)
        {
            var cam = Cam;
            if (cam == null) return;
            var e = cam.transform.eulerAngles;
            float yaw = e.y + yawDelta;
            float pitch = e.x - pitchDelta;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        public void ApplyInteriorMove(Vector3 localMove, float dt, bool fast)
        {
            var cam = Cam;
            if (cam == null) return;
            float speed = moveSpeed * (fast ? 3f : 1f);
            cam.transform.position += localMove * (speed * dt);
            _pivot = cam.transform.position + cam.transform.forward * 2f;
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            if (_interior)
                UpdateInterior();
            else
                UpdateOrbit();
        }

        void UpdateOrbit()
        {
            var cam = Cam;
            if (cam == null) return;

            ApplyZoomScroll(ReviewInput.ScrollY);

            Vector2 delta = ReviewInput.MouseDelta;
            bool pan = ReviewInput.GetMouseButton(1)
                       || ReviewInput.GetMouseButton(2)
                       || (ReviewInput.GetMouseButton(0) && ReviewInput.ShiftHeld);
            bool orbit = ReviewInput.GetMouseButton(0) && !ReviewInput.ShiftHeld && !ReviewInput.GetMouseButton(1);

            if (pan)
                ApplyPanDelta(delta);
            else if (orbit)
                ApplyOrbitDelta(delta.x * orbitSensitivity, delta.y * orbitSensitivity);
            else
                ApplyOrbitTransform();
        }

        void UpdateInterior()
        {
            var cam = Cam;
            if (cam == null) return;

            if (ReviewInput.GetMouseButton(0))
            {
                Vector2 delta = ReviewInput.MouseDelta;
                ApplyInteriorLook(delta.x * orbitSensitivity, delta.y * orbitSensitivity);
            }

            Vector3 move = Vector3.zero;
            if (ReviewInput.GetKey(KeyCode.W)) move += cam.transform.forward;
            if (ReviewInput.GetKey(KeyCode.S)) move -= cam.transform.forward;
            if (ReviewInput.GetKey(KeyCode.A)) move -= cam.transform.right;
            if (ReviewInput.GetKey(KeyCode.D)) move += cam.transform.right;
            if (ReviewInput.GetKey(KeyCode.E)) move += Vector3.up;
            if (ReviewInput.GetKey(KeyCode.Q)) move -= Vector3.up;
            ApplyInteriorMove(move, Time.deltaTime, ReviewInput.ShiftHeld);
        }

        void ApplyOrbitTransform()
        {
            var cam = Cam;
            if (cam == null) return;
            _pitch = Mathf.Clamp(_pitch, ExteriorPitchMin, ExteriorPitchMax);
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            cam.transform.position = _pivot + rot * (Vector3.back * _distance);
            cam.transform.rotation = rot;
        }
    }
}
