#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.DevConsole
{
    sealed class DevConsoleToggleWidget
    {
        readonly DevConsoleEntry m_Entry;
        readonly Toggle m_Toggle;
        readonly TextMeshProUGUI m_Label;
        bool m_Suppress;

        public Selectable Selectable => m_Toggle;

        public DevConsoleToggleWidget(
            Transform parent,
            DevConsoleEntry entry,
            DevConsoleSettings settings,
            Canvas canvas)
        {
            m_Entry = entry;
            float h = DevConsoleUiUtil.MinTouch(settings, canvas);
            float font = DevConsoleUiUtil.FontSize(settings, canvas);

            var root = DevConsoleUiUtil.CreateRect("Toggle_" + entry.Label, parent);
            DevConsoleUiUtil.SetHeight(root, h);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleWidth = 1f;
            DevConsoleUiUtil.AddImage(root, DevConsoleUiUtil.RowBg);

            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 8, 4, 4);
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var boxRt = DevConsoleUiUtil.CreateRect("Box", root);
            var boxLe = boxRt.gameObject.AddComponent<LayoutElement>();
            boxLe.minWidth = h * 0.7f;
            boxLe.minHeight = h * 0.7f;
            boxLe.preferredWidth = h * 0.7f;
            boxLe.preferredHeight = h * 0.7f;
            var bg = DevConsoleUiUtil.AddImage(boxRt, new Color(0.1f, 0.1f, 0.12f, 1f));

            var checkRt = DevConsoleUiUtil.CreateRect("Check", boxRt);
            DevConsoleUiUtil.Stretch(checkRt);
            checkRt.offsetMin = new Vector2(4, 4);
            checkRt.offsetMax = new Vector2(-4, -4);
            var check = DevConsoleUiUtil.AddImage(checkRt, DevConsoleUiUtil.Accent);

            m_Toggle = boxRt.gameObject.AddComponent<Toggle>();
            m_Toggle.targetGraphic = bg;
            m_Toggle.graphic = check;
            m_Toggle.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            m_Toggle.onValueChanged.AddListener(OnChanged);

            var labelRt = DevConsoleUiUtil.CreateRect("Label", root);
            var labelLe = labelRt.gameObject.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            m_Label = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            var fontAsset = DevConsoleUiUtil.Font;
            if (fontAsset != null)
                m_Label.font = fontAsset;
            m_Label.text = entry.Label;
            m_Label.fontSize = font;
            m_Label.color = DevConsoleUiUtil.TextColor;
            m_Label.alignment = TextAlignmentOptions.MidlineLeft;
            m_Label.raycastTarget = false;

            RefreshValueOnly();
        }

        void OnChanged(bool value)
        {
            if (m_Suppress)
                return;
            try { m_Entry.ToggleSet?.Invoke(value); }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                DevConsole.Note(e.Message);
            }
        }

        public void RefreshValueOnly()
        {
            bool value = false;
            try { value = m_Entry.ToggleGet != null && m_Entry.ToggleGet(); }
            catch { /* ignore */ }

            if (m_Toggle.isOn == value)
                return;

            m_Suppress = true;
            m_Toggle.isOn = value;
            m_Suppress = false;
        }
    }
}
#endif
