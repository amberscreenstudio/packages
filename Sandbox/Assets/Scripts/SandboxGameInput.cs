using AmberScreen.Dev;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.Sandbox
{
    /// <summary>
    /// Tiny Sandbox "game" so Rail blocking is visible: Space / cube click only when not blocked.
    /// </summary>
    public sealed class SandboxGameInput : MonoBehaviour
    {
        [SerializeField] Renderer cubeRenderer;
        TextMeshProUGUI m_Label;
        bool m_LastBlocked = true;

        void Awake()
        {
            if (cubeRenderer == null)
            {
                var cube = GameObject.Find("Cube");
                if (cube != null)
                    cubeRenderer = cube.GetComponent<Renderer>();
            }

            EnsureLabel();
            RefreshLabel(force: true);
        }

        void Update()
        {
            RefreshLabel(force: false);

            if (DevConsole.BlocksGame || DevConsole.IsPointerOverConsole)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                NudgeColour();

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                var cam = Camera.main;
                if (cam == null || cubeRenderer == null)
                    return;
                var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                if (Physics.Raycast(ray, out var hit) && hit.transform == cubeRenderer.transform)
                    NudgeColour();
            }
        }

        void NudgeColour()
        {
            if (cubeRenderer == null)
                return;
            var c = cubeRenderer.material.color;
            c.r = Mathf.Repeat(c.r + 0.17f, 1f);
            c.g = Mathf.Repeat(c.g + 0.11f, 1f);
            cubeRenderer.material.color = c;
        }

        void RefreshLabel(bool force)
        {
            bool blocked = DevConsole.BlocksGame;
            if (!force && blocked == m_LastBlocked)
                return;
            m_LastBlocked = blocked;
            if (m_Label != null)
                m_Label.text = blocked ? "Game input: blocked" : "Game input: live";
        }

        void EnsureLabel()
        {
            var canvasGo = new GameObject("SandboxGameLabelCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(canvasGo.transform, false);
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(16f, 16f);
            rt.sizeDelta = new Vector2(320f, 36f);
            m_Label = labelGo.AddComponent<TextMeshProUGUI>();
            m_Label.fontSize = 20f;
            m_Label.color = Color.white;
            m_Label.raycastTarget = false;
        }
    }
}
