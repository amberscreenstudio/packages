using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.PhoneUI
{
    public sealed class HomeScreenView : MonoBehaviour
    {
        RectTransform _root;
        RectTransform _grid;
        readonly List<GameObject> _icons = new();

        public RectTransform Root => _root;

        public void Build(RectTransform parent)
        {
            var theme = PhoneUITheme.Active;
            _root = UiFactory.Panel("HomeScreen", parent, theme.HomeBg);
            UiFactory.Stretch(_root);
            _root.offsetMin = new Vector2(0f, PhoneDp.NavBar);
            _root.offsetMax = new Vector2(0f, -PhoneDp.StatusBar);

            var title = UiFactory.Label("HomeTitle", _root, "Home", PhoneDp.TitleText, FontStyles.Bold);
            title.color = theme.HomeTitle;
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.sizeDelta = new Vector2(0f, 28f);
            tr.anchoredPosition = new Vector2(0f, -4f);
            title.alignment = TextAlignmentOptions.Center;

            var gridGo = new GameObject("AppGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            _grid = gridGo.GetComponent<RectTransform>();
            _grid.SetParent(_root, false);
            _grid.anchorMin = new Vector2(0f, 0f);
            _grid.anchorMax = new Vector2(1f, 1f);
            _grid.offsetMin = new Vector2(12f, 12f);
            _grid.offsetMax = new Vector2(-12f, -36f);

            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(PhoneDp.IconCellW, PhoneDp.IconCellH);
            grid.spacing = new Vector2(12f, 12f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
        }

        public void SetApps(IReadOnlyList<IPhoneApp> apps, Action<IPhoneApp> onOpen)
        {
            foreach (var go in _icons) UiFactory.SafeDestroy(go);
            _icons.Clear();
            if (apps == null || _grid == null) return;
            var theme = PhoneUITheme.Active;

            for (int i = 0; i < apps.Count; i++)
            {
                var app = apps[i];
                var btn = UiFactory.Button("App_" + app.Id, _grid, theme.HomeIconTile);
                btn.GetComponent<RectTransform>().localScale = Vector3.one;

                Color fallback = theme.AppIconColor(app.Id);
                RectTransform iconRt;
                if (app.Icon != null)
                {
                    iconRt = UiFactory.RoundedIcon("Icon", btn.transform, app.Icon, PhoneDp.IconSize, fallback);
                }
                else
                {
                    var icon = UiFactory.MakeImage("Icon", btn.transform, fallback);
                    iconRt = icon.rectTransform;
                    icon.raycastTarget = false;
                }
                iconRt.anchorMin = new Vector2(0.5f, 1f);
                iconRt.anchorMax = new Vector2(0.5f, 1f);
                iconRt.pivot = new Vector2(0.5f, 1f);
                iconRt.sizeDelta = new Vector2(PhoneDp.IconSize, PhoneDp.IconSize);
                iconRt.anchoredPosition = new Vector2(0f, -4f);

                var label = UiFactory.Label("Name", btn.transform, app.DisplayName, PhoneDp.IconLabel, FontStyles.Bold);
                label.color = theme.HomeIconLabel;
                label.alignment = TextAlignmentOptions.Center;
                var lr = label.rectTransform;
                lr.anchorMin = new Vector2(0f, 0f);
                lr.anchorMax = new Vector2(1f, 0f);
                lr.pivot = new Vector2(0.5f, 0f);
                lr.sizeDelta = new Vector2(0f, PhoneDp.IconLabel + 4f);
                lr.anchoredPosition = Vector2.zero;

                var captured = app;
                btn.onClick.AddListener(() => onOpen?.Invoke(captured));
                _icons.Add(btn.gameObject);
            }
        }

        public string DescribeLayout()
        {
            var sb = new StringBuilder();
            sb.Append("homeIcons=").Append(_icons.Count);
            if (_root != null)
                sb.Append(" homeRect=").Append(_root.rect.width.ToString("F0"))
                    .Append('x').Append(_root.rect.height.ToString("F0"));
            return sb.ToString();
        }

        public void Show(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
        }
    }
}
