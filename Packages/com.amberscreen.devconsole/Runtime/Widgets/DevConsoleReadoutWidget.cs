#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    sealed class DevConsoleReadoutWidget
    {
        readonly DevConsoleEntry m_Entry;
        readonly TextMeshProUGUI m_Label;
        readonly LayoutElement m_Layout;
        readonly RectTransform m_Root;
        readonly float m_MinHeight;
        readonly float m_LineHeight;
        int m_Lines = 1;
        float m_Accum;

        /// <summary>Line count shown by a multi-line readout (1 for single-line).</summary>
        internal int LineCount => m_Lines;
        internal float PreferredHeight => m_Layout != null ? m_Layout.preferredHeight : 0f;
        internal string Text => m_Label != null ? m_Label.text : null;

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
            m_Layout = le;
            m_Root = root;
            m_MinHeight = h;
            m_LineHeight = font * 0.9f * 1.3f;

            m_Label = DevConsoleUiUtil.AddLabel(
                root, "Label", entry.Label + ": -", font * 0.9f,
                entry.ReadoutMultiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft);
            var rt = m_Label.rectTransform;
            if (entry.ReadoutMultiline)
            {
                rt.offsetMin = new Vector2(8, 4);
                rt.offsetMax = new Vector2(-8, -4);
                m_Label.textWrappingMode = TextWrappingModes.Normal;
                m_Label.overflowMode = TextOverflowModes.Truncate;
            }
            else
            {
                rt.offsetMin = new Vector2(8, 0);
                rt.offsetMax = new Vector2(-8, 0);
            }

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
            if (!m_Entry.ReadoutMultiline)
            {
                m_Label.text = m_Entry.Label + ": " + value;
                return;
            }

            string text = ClampLines(m_Entry.Label + ":\n" + value, Mathf.Max(1, m_Entry.ReadoutMaxLines) + 1, out int lines);
            m_Label.text = text;
            if (lines == m_Lines && m_Layout.preferredHeight > 0f)
                return;
            m_Lines = lines;
            float height = Mathf.Max(m_MinHeight, lines * m_LineHeight + 8f);
            m_Layout.minHeight = height;
            m_Layout.preferredHeight = height;
            if (m_Root != null && m_Root.parent is RectTransform parent)
                LayoutRebuilder.MarkLayoutForRebuild(parent);
        }

        /// <summary>Keeps at most <paramref name="maxLines"/> lines; the last kept line ends with "...".</summary>
        internal static string ClampLines(string text, int maxLines, out int lines)
        {
            text = (text ?? string.Empty).Replace("\r\n", "\n");
            lines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '\n')
                    continue;
                if (lines == maxLines)
                    return text.Substring(0, i) + " ...";
                lines++;
            }
            return text;
        }
    }
}
#endif
