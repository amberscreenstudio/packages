using UnityEngine;

namespace AmberScreen.ModelReview
{
    /// <summary>Orbit / interior review camera. Owned by <see cref="ModelReviewRig"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class ReviewOrbitCamera : MonoBehaviour
    {
        public const float DefaultFov = 40f;
        public const float InteriorFov = 75f;

        [SerializeField] Camera targetCamera;
        [SerializeField] float orbitSensitivity = 0.2f;
        [SerializeField] float panSensitivity = 0.0025f;
        [SerializeField, Range(0.01f, 0.5f), Tooltip("Fraction of the current distance per scroll-wheel notch.")]
        float zoomSpeed = 0.15f;
        [SerializeField] float moveSpeed = 2.5f;

        float _yaw = 35f;
        float _pitch = 25f;
        float _distance = 5f;
        Vector3 _pivot;
        bool _interior;
        Bounds _lastBounds;

        public Camera Cam => targetCamera != null ? targetCamera : GetComponent<Camera>();
        public bool InteriorMode => _interior;
        public Vector3 Pivot => _pivot;
        public float Distance => _distance;

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
        public static float ZoomedDistance(float distance, float rawScroll, float zoomSpeed)
        {
            float notches = Mathf.Abs(rawScroll) >= 10f ? rawScroll / 120f : rawScroll;
            float speed = Mathf.Clamp(zoomSpeed, 0.01f, 0.5f);
            return Mathf.Clamp(distance * Mathf.Pow(1f - speed, notches), 0.15f, 250f);
        }

        public void Frame(Bounds bounds)
        {
            _lastBounds = bounds;
            _pivot = bounds.center;
            var cam = Cam;
            if (cam == null) return;
            float fov = cam.fieldOfView * Mathf.Deg2Rad;
            float radius = Mathf.Max(bounds.extents.magnitude, 0.05f);
            _distance = radius / Mathf.Sin(Mathf.Max(fov * 0.5f, 0.05f));
            _distance = Mathf.Clamp(_distance, 0.25f, 200f);
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

            float scroll = ReviewInput.ScrollY;
            if (Mathf.Abs(scroll) > 0.01f)
                _distance = ZoomedDistance(_distance, scroll, zoomSpeed);

            Vector2 delta = ReviewInput.MouseDelta;
            bool pan = ReviewInput.GetMouseButton(1)
                       || ReviewInput.GetMouseButton(2)
                       || (ReviewInput.GetMouseButton(0) && ReviewInput.ShiftHeld);
            bool orbit = ReviewInput.GetMouseButton(0) && !ReviewInput.ShiftHeld && !ReviewInput.GetMouseButton(1);

            if (pan)
            {
                Vector3 right = cam.transform.right;
                Vector3 up = cam.transform.up;
                float scale = _distance * panSensitivity;
                _pivot -= (right * delta.x + up * delta.y) * scale;
            }
            else if (orbit)
            {
                _yaw += delta.x * orbitSensitivity;
                _pitch = Mathf.Clamp(_pitch - delta.y * orbitSensitivity, -89f, 89f);
            }

            ApplyOrbitTransform();
        }

        void UpdateInterior()
        {
            var cam = Cam;
            if (cam == null) return;

            if (ReviewInput.GetMouseButton(0))
            {
                Vector2 delta = ReviewInput.MouseDelta;
                var e = cam.transform.eulerAngles;
                float yaw = e.y + delta.x * orbitSensitivity;
                float pitch = e.x - delta.y * orbitSensitivity;
                if (pitch > 180f) pitch -= 360f;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
                cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Vector3 move = Vector3.zero;
            if (ReviewInput.GetKey(KeyCode.W)) move += cam.transform.forward;
            if (ReviewInput.GetKey(KeyCode.S)) move -= cam.transform.forward;
            if (ReviewInput.GetKey(KeyCode.A)) move -= cam.transform.right;
            if (ReviewInput.GetKey(KeyCode.D)) move += cam.transform.right;
            if (ReviewInput.GetKey(KeyCode.E)) move += Vector3.up;
            if (ReviewInput.GetKey(KeyCode.Q)) move -= Vector3.up;
            float speed = moveSpeed * (ReviewInput.ShiftHeld ? 3f : 1f);
            cam.transform.position += move * (speed * Time.deltaTime);
            _pivot = cam.transform.position + cam.transform.forward * 2f;
        }

        void ApplyOrbitTransform()
        {
            var cam = Cam;
            if (cam == null) return;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            cam.transform.position = _pivot + rot * (Vector3.back * _distance);
            cam.transform.rotation = rot;
        }
    }
}
