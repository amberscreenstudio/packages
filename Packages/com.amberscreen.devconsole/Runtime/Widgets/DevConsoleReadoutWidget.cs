#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.DevConsole
{
    sealed class DevConsoleReadoutWidget
    {
        readonly DevConsoleEntry m_Entry;
        readonly TextMeshProUGUI m_Label;
        float m_Accum;

        public DevConsoleReadoutWidget(
            Transform parent,
            DevConsoleEntry entry,
            DevConsoleSettings settings,
            Canvas canvas)
        {
            m_Entry = entry;
            float h = DevConsoleUiUtil.MinTouch(settings, canvas) * 0.85f;
            float font = DevConsoleUiUtil.FontSize(settings, canvas);

            var root = DevConsoleUiUtil.CreateRect("Readout_" + entry.Label, parent);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleWidth = 1f;
            DevConsoleUiUtil.AddImage(root, DevConsoleUiUtil.RowBg);

            m_Label = DevConsoleUiUtil.AddLabel(
                root, "Label", entry.Label + ": -", font * 0.9f,
                TextAlignmentOptions.MidlineLeft);
            var rt = m_Label.rectTransform;
            rt.offsetMin = new Vector2(8, 0);
            rt.offsetMax = new Vector2(-8, 0);

            RefreshNow();
        }

        public void Tick(float unscaledDeltaTime)
        {
            m_Accum += unscaledDeltaTime;
            if (m_Accum < m_Entry.ReadoutRefreshSeconds)
                return;
            m_Accum = 0f;
            RefreshNow();
        }

        void RefreshNow()
        {
            string value = "-";
            try { value = m_Entry.ReadoutValue?.Invoke() ?? "-"; }
            catch { value = "err"; }
            m_Label.text = m_Entry.Label + ": " + value;
        }
    }
}
#endif
