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
        readonly LayoutElement m_Layout;

        public Selectable Selectable => m_Button;
        public RectTransform Root { get; }

        public DevConsoleButtonWidget(
            Transform parent,
            DevConsoleEntry entry,
            DevConsoleSettings settings,
            Canvas canvas,
            bool flowSized = false)
        {
            m_Entry = entry;
            float h = DevConsoleUiUtil.MinTouch(settings, canvas);
            float font = DevConsoleUiUtil.FontSize(settings, canvas);

            Root = DevConsoleUiUtil.CreateRect("Button_" + entry.Label, parent);
            DevConsoleUiUtil.SetHeight(Root, h);
            m_Layout = Root.gameObject.AddComponent<LayoutElement>();
            m_Layout.minHeight = h;
            m_Layout.preferredHeight = h;

            m_Button = DevConsoleUiUtil.AddButton(Root, DevConsoleUiUtil.ButtonBg);
            m_Label = DevConsoleUiUtil.AddLabel(
                Root, "Label", entry.Caption ?? entry.Label, font,
                TextAlignmentOptions.Midline);
            m_Label.textWrappingMode = TextWrappingModes.NoWrap;
            m_Label.overflowMode = TextOverflowModes.Overflow;

            if (flowSized)
            {
                m_Layout.flexibleWidth = 0f;
                ApplyPreferredWidth(font, h, settings);
            }
            else
            {
                m_Layout.flexibleWidth = 1f;
            }

            m_Button.onClick.AddListener(OnClick);
            Refresh();
        }

        void ApplyPreferredWidth(float font, float h, DevConsoleSettings settings)
        {
            string text = string.IsNullOrEmpty(m_Entry.Caption) ? m_Entry.Label : m_Entry.Caption;
            float pad = Mathf.Max(16f, settings.spacing * 4f);
            float w = m_Label.GetPreferredValues(text ?? string.Empty, 4096f, h).x + pad;
            w = Mathf.Max(h, w);
            m_Layout.minWidth = w;
            m_Layout.preferredWidth = w;
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
            finally
            {
                DevConsoleHost.KeepInteractiveWhileOpen();
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
