using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.Dev.Demo
{
    /// <summary>
    /// Shows the 0.5.0 additions: open blocking, a multi-line readout, a remembered choice,
    /// and per-session open key / sort order. Needs the AMBERSCREEN_DEVCONSOLE define.
    /// </summary>
    public sealed class DevConsoleParityDemo : MonoBehaviour
    {
        static readonly string[] k_Colours = { "Red", "Green", "Blue", "Yellow" };
        static readonly Key[] k_Keys = { Key.F1, Key.F3, Key.Backquote };

        bool m_Paused;
        int m_KeyIndex;
        bool m_HighSort;
        readonly StringBuilder m_Sb = new StringBuilder();

        void OnEnable()
        {
            DevConsole.OpenBlocked = () => m_Paused;
            DevConsole.RegisterMultilineReadout("Demo", "Scene objects", ListObjects, 1f, 8);
            DevConsole.RegisterChoice("Demo", "Colour", () => k_Colours,
                i => DevConsole.Note("Picked " + k_Colours[i]));
            DevConsole.Register("Demo", "Use picked colour", () =>
                DevConsole.Note("Using " + k_Colours[Mathf.Max(0, DevConsole.GetChoice("Demo", "Colour"))]));
        }

        void OnDisable()
        {
            DevConsole.OpenBlocked = null;
            DevConsole.ClearSection("Demo");
            DevConsole.ClearOverrides();
        }

        string ListObjects()
        {
            m_Sb.Clear();
            var roots = gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (i > 0) m_Sb.Append('\n');
                m_Sb.Append(roots[i].name).Append(" (").Append(roots[i].transform.childCount).Append(" children)");
            }
            m_Sb.Append("\nTime ").Append(Time.realtimeSinceStartup.ToString("0.0")).Append(" s");
            return m_Sb.ToString();
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 380, 260), GUI.skin.box);
            GUILayout.Label("Dev Console 0.5.0 demo");
            GUILayout.Label($"Open key: {DevConsole.OpenKey}   Sort order: {DevConsole.SortingOrder}");
            GUILayout.Label($"Console open: {DevConsole.IsOpen}   Opening blocked: {DevConsole.IsOpenBlocked}");
            GUILayout.Label($"Picked colour: {k_Colours[Mathf.Max(0, DevConsole.GetChoice("Demo", "Colour"))]}");

            bool paused = GUILayout.Toggle(m_Paused, " Pretend the game is paused (console must not open)");
            if (paused != m_Paused)
            {
                m_Paused = paused;
                Time.timeScale = paused ? 0f : 1f;
            }

            if (GUILayout.Button("Change open key"))
            {
                m_KeyIndex = (m_KeyIndex + 1) % k_Keys.Length;
                DevConsole.SetOpenKey(k_Keys[m_KeyIndex]);
            }

            if (GUILayout.Button(m_HighSort ? "Sort order back to settings" : "Sort order 1050"))
            {
                m_HighSort = !m_HighSort;
                if (m_HighSort) DevConsole.SetSortingOrder(1050);
                else DevConsole.ClearOverrides();
                m_KeyIndex = m_HighSort ? m_KeyIndex : 0;
            }

            GUILayout.EndArea();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
