using AmberScreen.Dev;
using UnityEngine;

namespace AmberScreen.Sandbox
{
    /// <summary>
    /// Registers Dev Console widgets for local playtesting (Rail + classic layouts).
    /// </summary>
    public sealed class SandboxCommands : MonoBehaviour
    {
        [SerializeField] Transform cube;
        [SerializeField] Renderer cubeRenderer;

        bool m_Spin;
        Color m_Color = Color.white;
        Vector3 m_StartPos;
        Quaternion m_StartRot;
        Vector3 m_StartScale;
        float m_FpsSmooth = 60f;
        int m_FrameCount;

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
            DevConsole.SetSectionOrder("Sandbox", "Readouts", "Danger", "Provider", "Density");

            DevConsole.RegisterToggle("Sandbox", "Spin Cube", () => m_Spin, v => m_Spin = v);

            DevConsole.RegisterDropdown(
                "Sandbox",
                "Cube Colour",
                () => new[] { "White", "Amber", "Teal", "Magenta" },
                ApplyColour);

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

            DevHud.SetSectionOrder("Sandbox");
            DevHud.SetLine("Sandbox", "FPS", () =>
            {
                float dt = Time.unscaledDeltaTime;
                if (dt > 0f)
                    m_FpsSmooth = Mathf.Lerp(m_FpsSmooth, 1f / dt, 0.15f);
                return m_FpsSmooth.ToString("0");
            });
            DevHud.SetLine("Sandbox", "Frames", () => m_FrameCount.ToString(), order: 1);
            DevHud.SetLine("Sandbox", "Input", () =>
                DevConsole.BlocksGame ? "blocked" : "live", order: 2);

            DevConsole.Note("Sandbox ready — press F1 (console) / F2 (HUD)");
        }

        void OnDestroy()
        {
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
