using System;
using AmberScreen.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.GameShell.Demo
{
    /// <summary>
    /// Boots the shell and saves a counter and the spinner's angle, so Save/Load, pause,
    /// fades, the block check, text size and the version warning can all be tried.
    /// </summary>
    public sealed class GameShellDemo : MonoBehaviour
    {
        public Transform spinner;

        int m_Counter;
        float m_TextScale = 1f;
        bool m_FakeMenuOpen;
        Func<bool> m_Guard;
        Func<bool> m_Blocker;

        [Serializable]
        public class DemoState
        {
            public int counter;
            public float angle;
        }

        sealed class Provider : ISaveStateProvider
        {
            readonly GameShellDemo m_Demo;
            public Provider(GameShellDemo demo) { m_Demo = demo; }
            public string Key => "demo.counter";

            public object Capture() => new DemoState
            {
                counter = m_Demo.m_Counter,
                angle = m_Demo.spinner != null ? m_Demo.spinner.eulerAngles.y : 0f
            };

            public void Restore(object state)
            {
                if (!(state is DemoState s)) return;
                m_Demo.m_Counter = s.counter;
                if (m_Demo.spinner != null) m_Demo.spinner.rotation = Quaternion.Euler(0f, s.angle, 0f);
            }
        }

        void OnEnable()
        {
            SaveRegistry.Unregister("demo.counter");
            SaveRegistry.Register(new Provider(this), typeof(DemoState));
            GameShell.Boot();
            GameShell.TextScale = () => m_TextScale;
            GameShell.PauseSubtitle = () => "Counter " + m_Counter;
            GameShell.OpenSettings = () => Debug.Log("[Demo] The game would open its settings menu here.");
            PauseFrame.AddMainButton("Add 1 to counter", () => m_Counter++);
            PauseFrame.AddMainButton("Fade and reset counter", () => SceneFlow.FadeThrough(() => m_Counter = 0), true);
            m_Guard = () => m_FakeMenuOpen;
            m_Blocker = () => m_FakeMenuOpen;
            GameShell.AddEscapeGuard(m_Guard);
            GameShell.AddBlocker(m_Blocker);
        }

        void OnDisable()
        {
            SaveRegistry.Unregister("demo.counter");
            GameShell.RemoveEscapeGuard(m_Guard);
            GameShell.RemoveBlocker(m_Blocker);
            PauseFrame.ClearMainButtons();
            GameShell.TextScale = null;
            GameShell.PauseSubtitle = null;
            GameShell.OpenSettings = null;
        }

        void Update()
        {
            // Gameplay stops while the shell says the game is blocked (pause, fade, the fake menu).
            if (spinner != null && !GameShell.BlocksGameplay)
                spinner.Rotate(0f, 60f * Time.deltaTime, 0f);
            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame) m_FakeMenuOpen = !m_FakeMenuOpen;
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 400, 260), GUI.skin.box);
            GUILayout.Label("Game Shell demo. Esc = pause.  M = pretend another menu is open.");
            GUILayout.Label($"Counter: {m_Counter}   Blocked: {GameShell.BlocksGameplay}   Paused: {PauseFrame.IsOpen}");
            GUILayout.Label($"Another menu open (Esc ignored): {m_FakeMenuOpen}");
            GUILayout.Label($"Pause text size: {Mathf.RoundToInt(m_TextScale * 100f)}%");
            m_TextScale = GUILayout.HorizontalSlider(m_TextScale, 0.75f, 1.6f);
            if (GUILayout.Button("Show a version warning"))
                GameShellVersion.NoteLoaded("0.99.0");
            GUILayout.EndArea();
        }
    }
}
