// Loads scenes by name with a full-screen fade each way, writing the autosave slot first.
using System;
using System.Collections;
using System.Collections.Generic;
using AmberScreen.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AmberScreen.GameShell
{
    [DisallowMultipleComponent]
    public class SceneFlow : MonoBehaviour
    {
        static readonly List<string> s_Known = new List<string>();
        static bool s_KnownSet;

        public static SceneFlow Instance { get; private set; }
        public static bool IsFading { get; private set; }

        /// <summary>Before a scene change starts (target scene name).</summary>
        public static event Action<string> SceneChanging;

        /// <summary>After the new scene has loaded and faded in.</summary>
        public static event Action<string> SceneChanged;

        Image m_Shade;
        Canvas m_Canvas;

        internal Image Shade => m_Shade;
        internal Canvas FadeCanvas => m_Canvas;

        /// <summary>Replaces the known scene list (otherwise the settings list, otherwise Build Settings).</summary>
        public static void SetKnownScenes(params string[] names)
        {
            s_Known.Clear();
            s_KnownSet = names != null;
            if (names == null) return;
            for (int i = 0; i < names.Length; i++)
                if (!string.IsNullOrEmpty(names[i]) && !s_Known.Contains(names[i])) s_Known.Add(names[i]);
        }

        public static bool Known(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            if (s_KnownSet) return s_Known.Contains(sceneName);
            var list = GameShell.Settings.knownScenes;
            if (list != null && list.Length > 0) return Array.IndexOf(list, sceneName) >= 0;
            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public static void Go(string sceneName) => Go(sceneName, GameShell.Settings.autosaveOnSceneChange);

        public static void Go(string sceneName, bool autosave)
        {
            if (Instance == null)
            {
                Debug.LogError("[SceneFlow] Not ready. Call GameShell.Boot() first.");
                return;
            }
            Instance.Begin(sceneName, autosave);
        }

        /// <summary>Fade out, run <paramref name="midpoint"/> behind the black, fade back in.</summary>
        public static void FadeThrough(Action midpoint)
        {
            if (Instance == null)
            {
                midpoint?.Invoke();
                return;
            }
            if (IsFading) return;
            Instance.StartCoroutine(Instance.RunFadeThrough(midpoint));
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            EnsureBuilt();
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            IsFading = false;
        }

        internal void EnsureBuilt()
        {
            if (Instance == null) Instance = this;
            if (m_Shade == null) BuildFade();
        }

        void Begin(string sceneName, bool autosave)
        {
            if (IsFading) return;
            if (!Known(sceneName))
            {
                Debug.LogWarning("[SceneFlow] Not registered: " + sceneName);
                return;
            }
            if (SceneManager.GetActiveScene().name == sceneName) return;
            StartCoroutine(Run(sceneName, autosave));
        }

        IEnumerator Run(string sceneName, bool autosave)
        {
            IsFading = true;
            SceneChanging?.Invoke(sceneName);
            if (autosave) GameShell.Save(SaveStore.AutosaveSlot);
            yield return Fade(0f, 1f);
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError("[SceneFlow] Failed to load " + sceneName + ". It needs to be enabled in Build Settings.");
                yield return Fade(1f, 0f);
                IsFading = false;
                yield break;
            }
            while (!op.isDone) yield return null;
            yield return Fade(1f, 0f);
            IsFading = false;
            SceneChanged?.Invoke(sceneName);
        }

        IEnumerator RunFadeThrough(Action midpoint)
        {
            IsFading = true;
            yield return Fade(0f, 1f);
            try { midpoint?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
            yield return Fade(1f, 0f);
            IsFading = false;
        }

        IEnumerator Fade(float from, float to)
        {
            if (m_Shade != null) m_Shade.raycastTarget = true;
            float seconds = Mathf.Max(0f, GameShell.Settings.fadeSeconds);
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            SetAlpha(to);
            if (m_Shade != null) m_Shade.raycastTarget = to > 0.01f;
        }

        internal void SetAlpha(float a)
        {
            if (m_Shade == null) return;
            var c = GameShell.Settings.Theme.fade;
            c.a = a;
            m_Shade.color = c;
        }

        void BuildFade()
        {
            var settings = GameShell.Settings;
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform, false);
            m_Canvas = canvasGo.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = settings.fadeSortingOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(canvasGo.transform, false);
            var rt = fadeGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            m_Shade = fadeGo.AddComponent<Image>();
            var fade = settings.Theme.fade;
            m_Shade.color = new Color(fade.r, fade.g, fade.b, 0f);
            m_Shade.raycastTarget = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics()
        {
            s_Known.Clear();
            s_KnownSet = false;
            Instance = null;
            IsFading = false;
            SceneChanging = null;
            SceneChanged = null;
        }
    }
}
