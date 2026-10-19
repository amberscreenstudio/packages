#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    sealed class DevConsoleButtonWidget
    {
        readonly DevConsoleEntry m_Entry;
        readonly Button m_Button;
        readonly TextMeshProUGUI m_Label;

        public Selectable Selectable => m_Button;

        public DevConsoleButtonWidget(
            Transform parent,
            DevConsoleEntry entry,
            DevConsoleSettings settings,
            Canvas canvas)
        {
            m_Entry = entry;
            float h = DevConsoleUiUtil.MinTouch(settings, canvas);
            float font = DevConsoleUiUtil.FontSize(settings, canvas);

            var root = DevConsoleUiUtil.CreateRect("Button_" + entry.Label, parent);
            DevConsoleUiUtil.SetHeight(root, h);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleWidth = 1f;

            m_Button = DevConsoleUiUtil.AddButton(root, DevConsoleUiUtil.ButtonBg);
            m_Label = DevConsoleUiUtil.AddLabel(
                root, "Label", entry.Caption ?? entry.Label, font,
                TextAlignmentOptions.Midline);

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
            RefreshInteractable();
        }

        public bool WillBeInteractable() => DevConsoleRegistry.IsAvailable(m_Entry);

        public void RefreshInteractable()
        {
            m_Button.interactable = WillBeInteractable();
        }
    }
}
#endif
