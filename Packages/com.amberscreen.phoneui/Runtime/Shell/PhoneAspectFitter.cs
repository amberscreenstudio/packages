using UnityEngine;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Locks the phone to a fixed 360 dp logical width.
    /// Computes the on-screen column (portrait 9:21–9:16), then sets
    /// sizeDelta = (360, heightDp) and localScale = columnWidthPx / 360
    /// so CanvasScaler does not double-scale the UI.
    /// Parent canvas should use Constant Pixel Size (scale factor 1).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PhoneAspectFitter : MonoBehaviour
    {
        public const float MinWidthOverHeight = 9f / 21f;
        public const float MaxWidthOverHeight = 9f / 16f;
        public const float ReferenceWidthDp = PhoneDp.ReferenceWidth;

        RectTransform _rt;
        Vector2 _lastParentSize = new Vector2(-1f, -1f);
        int _lastScreenW = -1;
        int _lastScreenH = -1;

        public float LastScale { get; private set; } = 1f;
        public float LastColumnWidthPx { get; private set; }
        public float LastLogicalHeightDp { get; private set; }

        void Awake() => _rt = (RectTransform)transform;

        void OnEnable()
        {
            if (_rt == null) _rt = (RectTransform)transform;
            Apply();
        }

        void OnRectTransformDimensionsChange() => Apply();

        void LateUpdate()
        {
            if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
                Apply();
            else if (_rt != null && _rt.parent is RectTransform parent)
            {
                var s = parent.rect.size;
                if (!Mathf.Approximately(s.x, _lastParentSize.x) ||
                    !Mathf.Approximately(s.y, _lastParentSize.y))
                    Apply();
            }
        }

        public void Apply()
        {
            if (_rt == null) _rt = (RectTransform)transform;
            if (_rt == null) return;
            if (!(_rt.parent is RectTransform parent)) return;

            float parentW = parent.rect.width;
            float parentH = parent.rect.height;
            if (parentW < 1f || parentH < 1f) return;

            _lastParentSize = new Vector2(parentW, parentH);
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;

            float parentAspect = parentW / parentH;
            float colW;
            float colH;

            if (parentAspect > MaxWidthOverHeight)
            {
                colH = parentH;
                colW = colH * MaxWidthOverHeight;
            }
            else if (parentAspect < MinWidthOverHeight)
            {
                colW = parentW;
                colH = colW / MinWidthOverHeight;
            }
            else
            {
                colW = parentW;
                colH = parentH;
            }

            float aspect = colW / colH;
            float logicalH = ReferenceWidthDp / aspect;
            float scale = colW / ReferenceWidthDp;

            LastColumnWidthPx = colW;
            LastLogicalHeightDp = logicalH;
            LastScale = scale;

            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.anchoredPosition = Vector2.zero;
            _rt.sizeDelta = new Vector2(ReferenceWidthDp, logicalH);
            _rt.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
