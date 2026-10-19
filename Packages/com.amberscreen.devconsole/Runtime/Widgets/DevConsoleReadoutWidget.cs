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
        readonly RectTransform m_Root;
        float m_Accum;

        public RectTransform Root => m_Root;

        public DevConsoleReadoutWidget(Transform parent, DevConsoleEntry entry, DevConsoleSettings settings)
        {
            m_Entry = entry;
            float h = DevConsoleUiUtil.MinTouch(settings) * 0.85f;
            float font = DevConsoleUiUtil.FontSize(settings);

            m_Root = DevConsoleUiUtil.CreateRect("Readout_" + entry.Label, parent);
            var le = m_Root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleWidth = 1f;
            DevConsoleUiUtil.AddImage(m_Root, DevConsoleUiUtil.RowBg);

            m_Label = DevConsoleUiUtil.AddLabel(
                m_Root, "Label", entry.Label + ": —", font * 0.9f,
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
            string value = "—";
            try { value = m_Entry.ReadoutValue?.Invoke() ?? "—"; }
            catch { value = "err"; }
            m_Label.text = m_Entry.Label + ": " + value;
        }
    }
}
#endif
