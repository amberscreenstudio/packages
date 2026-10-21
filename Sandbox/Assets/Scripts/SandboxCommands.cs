using System.Collections.Generic;
using AmberScreen.Dev;
using UnityEngine;

namespace AmberScreen.Sandbox
{
    /// <summary>
    /// Registers Dev Console / DevHud widgets for local playtesting (0.4.2 demo).
    /// </summary>
    public sealed class SandboxCommands : MonoBehaviour
    {
        [SerializeField] Transform cube;
        [SerializeField] Renderer cubeRenderer;

        bool m_Spin;
        bool m_ShowExtra = true;
        Color m_Color = Color.white;
        Vector3 m_StartPos;
        Quaternion m_StartRot;
        Vector3 m_StartScale;
        float m_FpsSmooth = 60f;
        int m_FrameCount;
        DevHudSectionHandle m_StatusSection;

        static readonly string[] s_WheelCols = { "Wheel", "Temp", "Wear", "Grip" };

        void Awake()
        {
            if (cube == null)
            {
                var found = GameObject.Find("Cube");
                if (found != null)
                    cube = found.transform;
            }

            if (cube != null)
            {
                m_StartPos = cube.position;
                m_StartRot = cube.rotation;
                m_StartScale = cube.localScale;
                if (cubeRenderer == null)
                    cubeRenderer = cube.GetComponent<Renderer>();
            }
        }

        void Start()
        {
            DevConsole.SetSectionOrder("Sandbox", "Tools", "Readouts", "Danger", "Provider", "Density");

            // Varied button lengths + sub-header (path form) + RegisterSubHeader
            DevConsole.RegisterSubHeader("Sandbox", "Cube");
            DevConsole.RegisterToggle("Sandbox", "Spin Cube", () => m_Spin, v => m_Spin = v);
            DevConsole.Register("Sandbox", "Go", () => DevConsole.Note("Go"));
            DevConsole.Register("Sandbox", "A longer label here", () => DevConsole.Note("Long label"));

            DevConsole.Register("Tools/Quick", "Ping", () => DevConsole.Note("Tools/Quick Ping"));
            DevConsole.Register("Tools", "Short", () => DevConsole.Note("Short"));
            DevConsole.Register("Tools", "Medium length", () => DevConsole.Note("Medium"));
            DevConsole.Register("Tools", "Very long command button label", () => DevConsole.Note("Very long"));

            // Dropdown argument (label on its own row — must not clip)
            DevConsole.RegisterDropdown(
                "Sandbox",
                "Cube Colour",
                () => new[] { "White", "Amber", "Teal", "Magenta — long option label" },
                ApplyColour);

            // Hide / show another entry
            DevConsole.Register("Sandbox", "Toggle Extra Button", () =>
            {
                m_ShowExtra = !m_ShowExtra;
                DevConsole.SetVisible("Sandbox/Extra Action", m_ShowExtra);
                DevConsole.Note(m_ShowExtra ? "Extra shown" : "Extra hidden");
            });
            DevConsole.Register(
                "Sandbox",
                "Extra Action",
                () => DevConsole.Note("Extra Action fired"),
                visible: true,
                available: null,
                visiblePredicate: () => m_ShowExtra);

            // Command that "starts gameplay" (locks cursor) — console must stay interactive
            DevConsole.Register("Sandbox", "Start Gameplay (lock cursor)", () =>
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                DevConsole.Note("Gameplay started — cursor was locked; console should stay usable");
            });

            DevConsole.RegisterReadout(
                "Readouts",
                "FPS",
                () =>
                {
                    float dt = Time.unscaledDeltaTime;
                    return dt > 0f ? (1f / dt).ToString("0") : "—";
                },
                0.25f);

            DevConsole.RegisterReadout(
                "Readouts",
                "Memory MB",
                () => (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f))
                    .ToString("0.0"),
                0.5f);

            DevConsole.RegisterReadout(
                "Readouts",
                "Cube angle",
                () => cube != null ? cube.eulerAngles.y.ToString("0.0") : "—",
                0.1f);

            DevConsole.PinReadout("Readouts", "Cube angle");
            DevConsole.PinReadout("Readouts", "FPS");

            DevConsole.RegisterConfirm("Danger", "Reset Cube", ResetCube);

            DevConsole.RegisterQuick("Spin cube", () => m_Spin = !m_Spin);
            DevConsole.RegisterQuick("Reset cube", ResetCube);
            DevConsole.RegisterQuick("Colour", () => ApplyColour(Random.Range(0, 4)));

            DevConsole.Register("Sandbox", "Toggle HUD", () => DevHud.Toggle());

            for (int i = 1; i <= 40; i++)
            {
                int n = i;
                DevConsole.Register(
                    "Density",
                    $"Filler {n:00}",
                    () => DevConsole.Note($"Filler {n:00}"));
            }

            BuildDevHudDemo();

            DevConsole.Note("Sandbox 0.4.2 — F1 console / F2 HUD");
        }

        void BuildDevHudDemo()
        {
            // Pinned FPS cell
            var status = DevHud.AddSection("status", "Status");
            m_StatusSection = status;
            var fps = DevHudCell.Create("FPS", () =>
            {
                float dt = Time.unscaledDeltaTime;
                if (dt > 0f)
                    m_FpsSmooth = Mathf.Lerp(m_FpsSmooth, 1f / dt, 0.15f);
                return m_FpsSmooth.ToString("0");
            });
            fps.Pin();
            status.AddRow(fps);

            // 3-cell row
            status.AddRow(
                DevHudCell.Create("Frames", () => m_FrameCount.ToString()),
                DevHudCell.Create("Input", () => DevConsole.BlocksGame ? "blocked" : "live"),
                DevHudCell.Create("Spin", () => m_Spin ? "on" : "off"));

            // Per-wheel table (4 rows)
            var wheels = DevHud.AddSection("wheels", "Wheels");
            wheels.AddTable(s_WheelCols, () =>
            {
                var list = new List<string[]>(4);
                for (int i = 0; i < 4; i++)
                {
                    list.Add(new[]
                    {
                        "W" + (i + 1),
                        (70 + i * 3).ToString(),
                        (10 + i * 5) + "%",
                        (0.85f - i * 0.05f).ToString("0.00")
                    });
                }

                return list;
            });

            // Collapsed-by-default section
            DevHud.AddSection("debug", "Debug extras", startCollapsed: true)
                .AddRow(DevHudCell.Create("Note", () => "collapsed by default"));

            // Tinted section
            DevHud.AddSection(
                    "tint",
                    "Tinted",
                    tint: new Color(0.55f, 0.35f, 0.1f, 1f))
                .AddRow(DevHudCell.Create("Accent", () => "amber tint"));

            // Plain-string F1 key hint (mixable with InputAction later)
            DevHud.SetKeyHints("Sandbox", ("F1", "Console"), ("F2", "HUD"));
            DevHud.SetMode("Sandbox");
        }

        void OnDestroy()
        {
            DevHud.ClearSection("status");
            DevHud.ClearSection("wheels");
            DevHud.ClearSection("debug");
            DevHud.ClearSection("tint");
            DevHud.ClearSection("Sandbox");
        }

        void Update()
        {
            m_FrameCount++;
            if (m_Spin && cube != null)
                cube.Rotate(0f, 90f * Time.deltaTime, 0f, Space.Self);
        }

        void ApplyColour(int index)
        {
            m_Color = index switch
            {
                1 => new Color(0.95f, 0.75f, 0.25f),
                2 => new Color(0.2f, 0.75f, 0.7f),
                3 => new Color(0.85f, 0.25f, 0.7f),
                _ => Color.white
            };

            if (cubeRenderer != null)
                cubeRenderer.material.color = m_Color;

            DevConsole.Note($"Cube colour → index {index}");
        }

        void ResetCube()
        {
            if (cube == null)
                return;

            m_Spin = false;
            cube.SetPositionAndRotation(m_StartPos, m_StartRot);
            cube.localScale = m_StartScale;
            m_Color = Color.white;
            if (cubeRenderer != null)
                cubeRenderer.material.color = m_Color;
            DevConsole.Note("Cube reset");
        }

        [DevButton("Sandbox", "Write Note")]
        static void WriteNoteButton()
        {
            DevConsole.Note("DevButton fired at " + Time.frameCount);
        }
    }

    public sealed class SandboxProviderSection : IDevConsoleSectionProvider
    {
        public void Register()
        {
            DevConsole.Register("Provider", "Provider Ping", () => DevConsole.Note("IDevConsoleSectionProvider ok"));
        }
    }
}
