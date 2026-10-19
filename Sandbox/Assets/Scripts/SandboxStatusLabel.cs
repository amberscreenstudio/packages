using AmberScreen.DevConsole;
using TMPro;
using UnityEngine;

namespace AmberScreen.Sandbox
{
    /// <summary>
    /// Live on-screen readout of <see cref="DevConsole.IsOpen"/> and <see cref="DevConsole.BlocksGame"/>.
    /// </summary>
    public sealed class SandboxStatusLabel : MonoBehaviour
    {
        TextMeshProUGUI m_Label;
        bool m_LastOpen;
        bool m_LastBlocks;

        void Awake()
        {
            EnsureUi();
            m_LastOpen = !DevConsole.IsOpen;
            m_LastBlocks = !DevConsole.BlocksGame;
            RefreshIfChanged();
            DevConsole.OpenChanged += OnFlagChanged;
            DevConsole.BlocksGameChanged += OnFlagChanged;
        }

        void OnDestroy()
        {
            DevConsole.OpenChanged -= OnFlagChanged;
            DevConsole.BlocksGameChanged -= OnFlagChanged;
        }

        void OnFlagChanged(bool _)
        {
            RefreshIfChanged();
        }

        void RefreshIfChanged()
        {
            if (m_Label == null)
                return;

            bool open = DevConsole.IsOpen;
            bool blocks = DevConsole.BlocksGame;
            if (open == m_LastOpen && blocks == m_LastBlocks)
                return;

            m_LastOpen = open;
            m_LastBlocks = blocks;
            m_Label.text = $"IsOpen: {open}\nBlocksGame: {blocks}";
        }

        void EnsureUi()
        {
            var canvasGo = new GameObject("SandboxStatusCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(canvasGo.transform, false);
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -16f);
            rt.sizeDelta = new Vector2(420f, 80f);

            m_Label = labelGo.AddComponent<TextMeshProUGUI>();
            m_Label.fontSize = 22f;
            m_Label.color = Color.white;
            m_Label.alignment = TextAlignmentOptions.TopLeft;
            m_Label.raycastTarget = false;
        }
    }
}
