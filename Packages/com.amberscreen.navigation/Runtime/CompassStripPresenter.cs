using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation
{
    /// <summary>A projected strip item, also available to tests and game-owned UI.</summary>
    public readonly struct CompassItem
    {
        public readonly string Id, Text;
        public readonly float RelativeAngle;
        public readonly Sprite Sprite;
        public readonly Color Tint;
        public readonly int Priority;
        public CompassItem(string id, string text, float relativeAngle, Sprite sprite, Color tint, int priority)
        {
            Id = id; Text = text; RelativeAngle = relativeAngle;
            Sprite = sprite; Tint = tint; Priority = priority;
        }
    }

    /// <summary>uGUI only, no camera/controller dependency. Parent under a game-owned Canvas.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CompassStripPresenter : MonoBehaviour
    {
        public HeadingSource headingSource;
        public MarkerRegistry registry;
        public MarkerTypes markerTypes;
        [Tooltip("Assign a game font. This package performs no Resources loading.")]
        public Font font;
        [Range(1f, 359f)] public float fieldOfView = 120f;
        public bool showDistances = true;
        [Tooltip("Bypass the shared instrument spring and display exact raw heading.")]
        public bool useRawHeading;
        [Tooltip("Optional component implementing INavigationDistanceProvider.")]
        public MonoBehaviour distanceProviderComponent;
        public List<CustomBearingSource> customSources = new List<CustomBearingSource>();
        // Runtime injection overrides the Inspector component, when supplied.
        public INavigationDistanceProvider DistanceProvider { get; set; }

        public IReadOnlyList<CompassItem> VisibleItems => items;
        public float DisplayedHeading { get; private set; }
        public bool IsHudVisible => rootGroup != null && rootGroup.alpha > 0f &&
            root.gameObject.activeInHierarchy && isActiveAndEnabled;
        private readonly List<CompassItem> items = new List<CompassItem>();
        private readonly List<MarkerWidget> widgets = new List<MarkerWidget>();
        private RectTransform root;
        private CanvasGroup rootGroup;
        private readonly Text[] ticks = new Text[4];
        private static readonly string[] CardinalNames = { "N", "E", "S", "W" };

        private sealed class MarkerWidget
        {
            public RectTransform rect;
            public Image icon;
            public Text text;
        }

        private void OnEnable()
        {
            EnsureUi();
            root.gameObject.SetActive(true);
            RenderNow();
        }
        private void OnDisable() { if (root != null) root.gameObject.SetActive(false); }
        private void LateUpdate() => RenderNow();

        /// <summary>Refresh synchronously; normal gameplay calls this from LateUpdate.</summary>
        public void RenderNow()
        {
            EnsureUi();
            items.Clear();
            bool visible = NavigationHud.Visible && headingSource != null && isActiveAndEnabled;
            rootGroup.alpha = visible ? 1f : 0f;
            if (!visible) return;
            DisplayedHeading = NavigationMath.Wrap360(useRawHeading ?
                headingSource.TrueHeadingDeg : headingSource.HeadingDeg);
            float fov = Mathf.Clamp(fieldOfView, 1f, 359f);
            for (int i = 0; i < ticks.Length; i++)
            {
                float relative = NavigationMath.RelativeAngle(i * 90f, DisplayedHeading);
                ticks[i].gameObject.SetActive(Mathf.Abs(relative) <= fov * 0.5f);
                ticks[i].font = font;
                Place(ticks[i].rectTransform, relative, fov, -14f);
            }

            if (registry != null)
            {
                foreach (NavigationMarker marker in registry.Entries)
                {
                    if (!marker.TryGetPosition(out Vector3 position)) continue;
                    float relative = NavigationMath.RelativeAngle(headingSource.BearingTo(position), DisplayedHeading);
                    if (Mathf.Abs(relative) > fov * 0.5f) continue;
                    MarkerType type = markerTypes != null ? markerTypes.Find(marker.TypeId) : null;
                    string text = string.IsNullOrEmpty(marker.Label) ? marker.TypeId : marker.Label;
                    if (string.IsNullOrEmpty(text)) text = marker.Id;
                    if (showDistances && (marker.ShowDistance ?? (type == null || type.defaultShowDistance)))
                    {
                        INavigationDistanceProvider provider = DistanceProvider ??
                            distanceProviderComponent as INavigationDistanceProvider;
                        Vector3 from = headingSource.Position;
                        float distance = provider != null ? provider.GetDistance(from, marker) :
                            NavigationMath.HorizontalDistance(from.x, from.z, position.x, position.z);
                        if (!float.IsNaN(distance) && !float.IsInfinity(distance) && distance >= 0f)
                            text += "\n" + distance.ToString("0", CultureInfo.InvariantCulture) + " m";
                    }
                    items.Add(new CompassItem(marker.Id, text, relative,
                        type != null ? type.sprite : null, type != null ? type.tint : Color.white, marker.Priority));
                }
            }
            foreach (CustomBearingSource source in customSources)
            {
                if (source == null || !source.isActiveAndEnabled || !source.TryGetBearing(out float bearing)) continue;
                if (float.IsNaN(bearing) || float.IsInfinity(bearing)) continue;
                float relative = NavigationMath.RelativeAngle(bearing, DisplayedHeading);
                if (Mathf.Abs(relative) <= fov * 0.5f)
                    items.Add(new CompassItem("custom:" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(source).ToString(CultureInfo.InvariantCulture),
                        source.label, relative, source.sprite, source.tint, source.priority));
            }
            // High priority draws last (on top). Equal priorities have stable ordinal ids.
            items.Sort(CompareItems);
            for (int i = 0; i < items.Count; i++)
            {
                if (i == widgets.Count) widgets.Add(CreateMarkerWidget());
                MarkerWidget widget = widgets[i];
                CompassItem item = items[i];
                widget.rect.gameObject.SetActive(true);
                Place(widget.rect, item.RelativeAngle, fov, -67f);
                widget.text.font = font;
                widget.text.text = item.Text;
                widget.text.color = item.Tint;
                widget.icon.sprite = item.Sprite;
                widget.icon.color = item.Tint;
                widget.icon.enabled = item.Sprite != null;
            }
            for (int i = items.Count; i < widgets.Count; i++) widgets[i].rect.gameObject.SetActive(false);
        }

        private static int CompareItems(CompassItem a, CompassItem b)
        {
            int result = a.Priority.CompareTo(b.Priority);
            return result != 0 ? result : string.CompareOrdinal(a.Id, b.Id);
        }
        private static void Place(RectTransform rect, float angle, float fov, float y)
        {
            // Normalized anchors resize with the game's safe-area container.
            float x = 0.5f + angle / fov;
            rect.anchorMin = rect.anchorMax = new Vector2(x, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }
        private void EnsureUi()
        {
            if (root != null) return;
            root = NewRect("Compass content", transform, Vector2.zero);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            root.gameObject.AddComponent<RectMask2D>();
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();
            rootGroup.interactable = false; rootGroup.blocksRaycasts = false;
            for (int i = 0; i < ticks.Length; i++)
            {
                ticks[i] = NewText(CardinalNames[i], root, new Vector2(48f, 26f));
                ticks[i].text = CardinalNames[i];
                ticks[i].fontSize = 20;
            }
        }
        private MarkerWidget CreateMarkerWidget()
        {
            var widget = new MarkerWidget();
            widget.rect = NewRect("Marker", root, new Vector2(120f, 56f));
            RectTransform icon = NewRect("Icon", widget.rect, new Vector2(20f, 20f));
            icon.anchoredPosition = new Vector2(0f, 18f);
            widget.icon = icon.gameObject.AddComponent<Image>();
            widget.icon.raycastTarget = false;
            widget.text = NewText("Label and distance", widget.rect, new Vector2(120f, 38f));
            widget.text.rectTransform.anchoredPosition = new Vector2(0f, -12f);
            return widget;
        }
        private Text NewText(string name, Transform parent, Vector2 size)
        {
            RectTransform rect = NewRect(name, parent, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = 16; text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false; text.supportRichText = false;
            return text;
        }
        private static RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }
        private void OnDestroy()
        {
            if (root == null) return;
            if (Application.isPlaying) Destroy(root.gameObject);
            else DestroyImmediate(root.gameObject);
        }
    }
}
