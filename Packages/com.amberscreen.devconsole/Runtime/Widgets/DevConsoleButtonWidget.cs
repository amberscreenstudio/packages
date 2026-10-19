#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.DevConsole
{
    sealed class DevConsoleButtonWidget
    {
        readonly DevConsoleEntry m_Entry;
        readonly Button m_Button;
        readonly TextMeshProUGUI m_Label;
        readonly RectTransform m_Root;

        public RectTransform Root => m_Root;
        public Selectable Selectable => m_Button;

        public DevConsoleButtonWidget(Transform parent, DevConsoleEntry entry, DevConsoleSettings settings)
        {
            m_Entry = entry;
            float h = DevConsoleUiUtil.MinTouch(settings);
            float font = DevConsoleUiUtil.FontSize(settings);

            m_Root = DevConsoleUiUtil.CreateRect("Button_" + entry.Label, parent);
            DevConsoleUiUtil.SetHeight(m_Root, h);
            var le = m_Root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleWidth = 1f;

            m_Button = DevConsoleUiUtil.AddButton(m_Root, DevConsoleUiUtil.ButtonBg);
            m_Label = DevConsoleUiUtil.AddLabel(
                m_Root, "Label", entry.Caption ?? entry.Label, font,
                TextAlignmentOptions.Midline);

            var pad = m_Root.gameObject.AddComponent<LayoutElement>();
            _ = pad;

            m_Button.onClick.AddListener(OnClick);
            Refresh();
        }

        void OnClick()
        {
            if (!DevConsoleRegistry.IsAvailable(m_Entry))
                return;
            try { m_Entry.Action?.Invoke(); }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                DevConsole.Note(e.Message);
            }
        }

        public void Refresh()
        {
            m_Label.text = string.IsNullOrEmpty(m_Entry.Caption) ? m_Entry.Label : m_Entry.Caption;
            m_Button.interactable = DevConsoleRegistry.IsAvailable(m_Entry);
        }
    }
}
#endif
