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

        void Awake()
        {
            EnsureUi();
            Refresh();
            DevConsole.OpenChanged += OnOpenChanged;
        }

        void OnDestroy()
        {
            DevConsole.OpenChanged -= OnOpenChanged;
        }

        void Update()
        {
            // BlocksGame can change on collapse without a full open/close.
            Refresh();
        }

        void OnOpenChanged(bool _)
        {
            Refresh();
        }

        void Refresh()
        {
            if (m_Label == null)
                return;
            m_Label.text = $"IsOpen: {DevConsole.IsOpen}\nBlocksGame: {DevConsole.BlocksGame}";
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
