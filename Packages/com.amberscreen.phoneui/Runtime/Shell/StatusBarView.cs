using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.PhoneUI
{
    public sealed class StatusBarView : MonoBehaviour
    {
        IDeviceStatusProvider _status;
        RectTransform _bar;
        TextMeshProUGUI _time;
        TextMeshProUGUI _battery;
        TextMeshProUGUI _signal;
        float _tick;

        public RectTransform Bar => _bar;

        public void Build(RectTransform parent, IDeviceStatusProvider status)
        {
            var theme = PhoneUITheme.Active;
            _status = status ?? new DefaultDeviceStatusProvider();
            _bar = UiFactory.Panel("StatusBar", parent, theme.StatusBarBg);
            UiFactory.SetTopBar(_bar, PhoneDp.StatusBar);

            _time = UiFactory.Label("Time", _bar, "12:00", PhoneDp.NameText, FontStyles.Bold);
            _time.color = theme.StatusBarText;
            _time.alignment = TextAlignmentOptions.MidlineLeft;
            var timeRt = _time.rectTransform;
            timeRt.anchorMin = new Vector2(0f, 0f);
            timeRt.anchorMax = new Vector2(0.4f, 1f);
            timeRt.offsetMin = new Vector2(10f, 0f);
            timeRt.offsetMax = Vector2.zero;

            _signal = UiFactory.Label("Signal", _bar, "||||", PhoneDp.TimestampText, FontStyles.Bold);
            _signal.color = theme.StatusBarText;
            _signal.alignment = TextAlignmentOptions.MidlineRight;
            var sigRt = _signal.rectTransform;
            sigRt.anchorMin = new Vector2(0.55f, 0f);
            sigRt.anchorMax = new Vector2(0.78f, 1f);
            sigRt.offsetMin = Vector2.zero;
            sigRt.offsetMax = Vector2.zero;

            _battery = UiFactory.Label("Battery", _bar, "82%", PhoneDp.TimestampText, FontStyles.Bold);
            _battery.color = theme.StatusBarText;
            _battery.alignment = TextAlignmentOptions.MidlineRight;
            var batRt = _battery.rectTransform;
            batRt.anchorMin = new Vector2(0.78f, 0f);
            batRt.anchorMax = new Vector2(1f, 1f);
            batRt.offsetMin = Vector2.zero;
            batRt.offsetMax = new Vector2(-8f, 0f);

            Refresh();
        }

        public void BringToFront()
        {
            if (_bar != null) _bar.SetAsLastSibling();
        }

        public string DescribeLayout()
        {
            if (_bar == null) return "status=null";
            return "statusRect=" + _bar.rect.width.ToString("F0") + "x" + _bar.rect.height.ToString("F0");
        }

        void Update()
        {
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            Refresh();
        }

        void Refresh()
        {
            if (_status == null || _time == null) return;
            _time.text = _status.NowLocal.ToString("HH:mm");
            _battery.text = Mathf.RoundToInt(_status.BatteryPercent * 100f) + "%";
            int bars = Mathf.Clamp(_status.SignalBars, 0, 4);
            _signal.text = new string('|', Math.Max(bars, 1)) + new string('.', 4 - bars);
        }
    }
}
