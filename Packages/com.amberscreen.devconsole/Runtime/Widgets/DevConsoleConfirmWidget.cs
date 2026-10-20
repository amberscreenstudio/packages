#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    sealed class DevConsoleConfirmWidget
    {
        const float ConfirmWindow = 3f;

        readonly DevConsoleEntry m_Entry;
        readonly Button m_Button;
        readonly TextMeshProUGUI m_Label;
        float m_ConfirmUntil = -1f;

        public Selectable Selectable => m_Button;

        public DevConsoleConfirmWidget(
            Transform parent,
            DevConsoleEntry entry,
            DevConsoleSettings settings,
            Canvas canvas)
        {
            m_Entry = entry;
            float h = DevConsoleUiUtil.MinTouch(settings, canvas);
            float font = DevConsoleUiUtil.FontSize(settings, canvas);

            var root = DevConsoleUiUtil.CreateRect("Confirm_" + entry.Label, parent);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleWidth = 1f;

            m_Button = DevConsoleUiUtil.AddButton(root, DevConsoleHost.ActiveTheme.confirmButton);
            m_Label = DevConsoleUiUtil.AddLabel(
                root, "Label", entry.Label, font, TextAlignmentOptions.Midline);
            m_Button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            float now = Time.unscaledTime;
            if (m_ConfirmUntil > 0f && now <= m_ConfirmUntil)
            {
                m_ConfirmUntil = -1f;
                m_Label.text = m_Entry.Label;
                try { m_Entry.Action?.Invoke(); }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    DevConsole.Note(e.Message);
                }
                return;
            }

            m_ConfirmUntil = now + ConfirmWindow;
            m_Label.text = "Confirm?";
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (m_ConfirmUntil < 0f)
                return;
            if (Time.unscaledTime > m_ConfirmUntil)
            {
                m_ConfirmUntil = -1f;
                m_Label.text = m_Entry.Label;
            }
        }
    }
}
#endif
