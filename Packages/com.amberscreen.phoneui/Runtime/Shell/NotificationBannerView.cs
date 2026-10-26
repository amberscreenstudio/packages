using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.PhoneUI
{
    public sealed class NotificationBannerView : MonoBehaviour
    {
        RectTransform _root;
        TextMeshProUGUI _title;
        TextMeshProUGUI _body;
        Action _onTap;
        float _hideAt;

        public RectTransform Root => _root;

        public void Build(RectTransform parent)
        {
            var img = UiFactory.MakeImage("Banner", parent, PhoneUITheme.Active.NotificationBanner);
            _root = img.rectTransform;
            _root.anchorMin = new Vector2(0.04f, 1f);
            _root.anchorMax = new Vector2(0.96f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.sizeDelta = new Vector2(0f, 48f);
            _root.anchoredPosition = new Vector2(0f, -PhoneDp.StatusBar - 4f);
            img.gameObject.SetActive(false);

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => _onTap?.Invoke());

            _title = UiFactory.Label("Title", _root, "", PhoneDp.NameText, FontStyles.Bold);
            var tr = _title.rectTransform;
            tr.anchorMin = new Vector2(0f, 0.5f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = new Vector2(10f, 0f);
            tr.offsetMax = new Vector2(-10f, -2f);

            _body = UiFactory.Label("Body", _root, "", PhoneDp.TimestampText);
            var br = _body.rectTransform;
            br.anchorMin = new Vector2(0f, 0f);
            br.anchorMax = new Vector2(1f, 0.55f);
            br.offsetMin = new Vector2(12f, 6f);
            br.offsetMax = new Vector2(-12f, 0f);
        }

        public void Show(string title, string body, Action onTap)
        {
            if (_root == null || _title == null || _body == null) return;
            _onTap = onTap;
            _title.text = title ?? "";
            _body.text = body ?? "";
            _root.gameObject.SetActive(true);
            _hideAt = Time.unscaledTime + 4.5f;
            _root.SetAsLastSibling();
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
            _onTap = null;
        }

        void OnDisable()
        {
            Hide();
        }

        void Update()
        {
            if (_root != null && _root.gameObject.activeSelf && Time.unscaledTime >= _hideAt)
                Hide();
        }
    }
}
