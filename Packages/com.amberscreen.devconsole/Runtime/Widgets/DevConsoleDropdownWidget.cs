#if AMBERSCREEN_DEVCONSOLE
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    sealed class DevConsoleDropdownWidget
    {
        readonly DevConsoleEntry m_Entry;
        readonly TMP_Dropdown m_Dropdown;
        readonly Button m_Go;
        string[] m_Options = Array.Empty<string>();

        public Selectable Selectable => m_Dropdown;

        public bool IsDropdownOpen =>
            m_Dropdown != null && m_Dropdown.transform.Find("Dropdown List") != null;

        public DevConsoleDropdownWidget(
            Transform parent,
            DevConsoleEntry entry,
            DevConsoleSettings settings,
            Canvas canvas)
        {
            m_Entry = entry;
            float h = DevConsoleUiUtil.MinTouch(settings, canvas);
            float font = DevConsoleUiUtil.FontSize(settings, canvas);
            float itemH = h;
            if (settings.layout == DevConsoleLayout.MobilePortrait)
                itemH = Mathf.Max(h, 48f * DevConsoleUiUtil.DpToCanvas(canvas));

            // Vertical stack: command name on its own row, then dropdown + Go — never clips the label.
            var root = DevConsoleUiUtil.CreateRect("Dropdown_" + entry.Label, parent);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h * 2f + 4f;
            le.preferredHeight = h * 2f + 4f;
            le.flexibleWidth = 1f;
            DevConsoleUiUtil.AddImage(root, DevConsoleUiUtil.RowBg);

            var stack = root.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(4, 4, 2, 2);
            stack.spacing = 4f;
            stack.childAlignment = TextAnchor.UpperLeft;
            stack.childForceExpandHeight = false;
            stack.childForceExpandWidth = true;
            stack.childControlHeight = true;
            stack.childControlWidth = true;

            var labelRt = DevConsoleUiUtil.CreateRect("Label", root);
            var labelLe = labelRt.gameObject.AddComponent<LayoutElement>();
            labelLe.minHeight = h * 0.85f;
            labelLe.preferredHeight = h * 0.85f;
            labelLe.flexibleWidth = 1f;
            var label = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(label, false);
            label.text = entry.Label;
            label.fontSize = font * 0.9f;
            label.color = DevConsoleUiUtil.TextColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;

            var controls = DevConsoleUiUtil.CreateRect("Controls", root);
            var controlsLe = controls.gameObject.AddComponent<LayoutElement>();
            controlsLe.minHeight = h;
            controlsLe.preferredHeight = h;
            controlsLe.flexibleWidth = 1f;
            var row = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 6f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandHeight = true;
            row.childForceExpandWidth = false;
            row.childControlWidth = true;

            var dropRt = DevConsoleUiUtil.CreateRect("Dropdown", controls);
            var dropLe = dropRt.gameObject.AddComponent<LayoutElement>();
            dropLe.flexibleWidth = 1f;
            dropLe.minWidth = 120f;
            var dropBg = DevConsoleUiUtil.AddImage(dropRt, DevConsoleHost.ActiveTheme.dropdownBg);

            var captionRt = DevConsoleUiUtil.CreateRect("Label", dropRt);
            DevConsoleUiUtil.Stretch(captionRt);
            captionRt.offsetMin = new Vector2(8, 2);
            captionRt.offsetMax = new Vector2(-24, -2);
            var caption = captionRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(caption, false);
            caption.fontSize = font * 0.85f;
            caption.color = DevConsoleUiUtil.TextColor;
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.raycastTarget = false;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;

            var template = CreateDropdownTemplate(dropRt, font, itemH);
            m_Dropdown = dropRt.gameObject.AddComponent<TMP_Dropdown>();
            m_Dropdown.targetGraphic = dropBg;
            m_Dropdown.captionText = caption;
            m_Dropdown.template = template;
            m_Dropdown.itemText = template.Find("Viewport/Content/Item/Item Label")
                .GetComponent<TextMeshProUGUI>();
            m_Dropdown.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            var goRt = DevConsoleUiUtil.CreateRect("Go", controls);
            var goLe = goRt.gameObject.AddComponent<LayoutElement>();
            goLe.preferredWidth = Mathf.Max(72f, h * 1.5f);
            goLe.minHeight = h;
            m_Go = DevConsoleUiUtil.AddButton(goRt, DevConsoleUiUtil.ButtonBg);
            DevConsoleUiUtil.AddLabel(goRt, "Label", "Go", font, TextAlignmentOptions.Midline);
            m_Go.onClick.AddListener(OnGo);

            RefreshOptions();
        }

        static RectTransform CreateDropdownTemplate(Transform parent, float font, float itemH)
        {
            var template = DevConsoleUiUtil.CreateRect("Template", parent);
            template.gameObject.SetActive(false);
            template.anchorMin = new Vector2(0, 0);
            template.anchorMax = new Vector2(1, 0);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = Vector2.zero;
            template.sizeDelta = new Vector2(0, Mathf.Max(160f, itemH * 4f));
            DevConsoleUiUtil.AddImage(template, DevConsoleHost.ActiveTheme.dropdownTemplateBg);

            var scroll = template.gameObject.AddComponent<ScrollRect>();
            var viewport = DevConsoleUiUtil.CreateRect("Viewport", template);
            DevConsoleUiUtil.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImg = viewport.gameObject.AddComponent<Image>();
            // Near-invisible graphic so ScrollRect receives raycasts.
            viewportImg.color = new Color(1, 1, 1, 0.01f);

            var content = DevConsoleUiUtil.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            DevConsoleUiUtil.FitTopStretchContent(content, itemH);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var item = DevConsoleUiUtil.CreateRect("Item", content);
            var itemLe = item.gameObject.AddComponent<LayoutElement>();
            itemLe.minHeight = itemH;
            itemLe.preferredHeight = itemH;
            var toggle = item.gameObject.AddComponent<Toggle>();
            var itemBg = DevConsoleUiUtil.AddImage(item, DevConsoleHost.ActiveTheme.dropdownItemBg);
            toggle.targetGraphic = itemBg;

            var checkRt = DevConsoleUiUtil.CreateRect("Item Checkmark", item);
            DevConsoleUiUtil.Stretch(checkRt);
            var check = DevConsoleUiUtil.AddImage(checkRt, DevConsoleHost.ActiveTheme.dropdownCheck);
            toggle.graphic = check;

            var itemLabelRt = DevConsoleUiUtil.CreateRect("Item Label", item);
            DevConsoleUiUtil.Stretch(itemLabelRt);
            itemLabelRt.offsetMin = new Vector2(8, 0);
            itemLabelRt.offsetMax = new Vector2(-8, 0);
            var itemLabel = itemLabelRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(itemLabel, false);
            itemLabel.fontSize = font * 0.8f;
            itemLabel.color = DevConsoleUiUtil.TextColor;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.textWrappingMode = TextWrappingModes.NoWrap;
            itemLabel.overflowMode = TextOverflowModes.Overflow;

            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;

            return template;
        }

        void OnGo()
        {
            try { m_Entry.DropdownGo?.Invoke(m_Dropdown.value); }
            catch (Exception e)
            {
                Debug.LogException(e);
                DevConsole.Note(e.Message);
            }
            finally
            {
                DevConsoleHost.KeepInteractiveWhileOpen();
            }
        }

        public void RefreshOptions()
        {
            try { m_Options = m_Entry.DropdownOptions?.Invoke() ?? Array.Empty<string>(); }
            catch { m_Options = Array.Empty<string>(); }

            m_Dropdown.ClearOptions();
            var list = new System.Collections.Generic.List<TMP_Dropdown.OptionData>(m_Options.Length);
            for (int i = 0; i < m_Options.Length; i++)
                list.Add(new TMP_Dropdown.OptionData(m_Options[i] ?? string.Empty));
            m_Dropdown.AddOptions(list);
        }

        public bool TryCloseDropdown()
        {
            if (!IsDropdownOpen)
                return false;
            m_Dropdown.Hide();
            return true;
        }
    }
}
#endif
