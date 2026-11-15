// One entry point for the shell: boot, save hooks, and the "is the game blocked" check.
using System;
using System.Collections.Generic;
using System.IO;
using AmberScreen.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmberScreen.GameShell
{
    public static class GameShell
    {
        static readonly List<Func<bool>> s_Blockers = new List<Func<bool>>();
        static readonly List<Func<bool>> s_EscapeGuards = new List<Func<bool>>();
        static GameObject s_Root;
        static GameShellSettings s_Settings;

        /// <summary>Raised once after <see cref="Boot"/> has made the shell objects.</summary>
        public static event Action Booted;

        public static bool IsBooted => s_Root != null;
        public static GameObject Root => s_Root;
        public static GameShellSettings Settings => s_Settings != null ? s_Settings : GameShellSettings.LoadOrDefault();

        // ---------- hooks the game fills in (all optional) ----------

        /// <summary>Writes a slot. Default: <see cref="SaveStore.Save"/> with every registered provider.</summary>
        public static Func<int, bool> SaveSlot;

        /// <summary>Loads a slot. Default: <see cref="SaveStore.Load"/>, version warning, then scene restore.</summary>
        public static Func<int, LoadResult> LoadSlot;

        /// <summary>One-line slot description for the Save/Load pages. Default: save time and scene.</summary>
        public static Func<int, string> SlotSummary;

        /// <summary>Turns an old-format save file into an envelope (passed to SaveStore.Load/Peek).</summary>
        public static Func<string, SaveEnvelope> MigrateLegacy;

        /// <summary>Text size multiplier for the pause frame (e.g. () => GameSettings.TextScale). Default 1.</summary>
        public static Func<float> TextScale;

        /// <summary>Second line under the pause title (day, money...). Null or "" hides it.</summary>
        public static Func<string> PauseSubtitle;

        /// <summary>Shows a "Settings" button on the pause frame that calls this (e.g. open SettingsMenu).</summary>
        public static Action OpenSettings;

        /// <summary>Scene name read from the last loaded slot ("" if none).</summary>
        public static string LastSavedScene { get; internal set; } = "";

        // ---------- boot ----------

        /// <summary>
        /// Makes the "GameShell" object (kept across scenes) with <see cref="SceneFlow"/> and <see cref="PauseFrame"/>.
        /// Safe to call more than once. Call it from the game's own boot, before the first scene if you can.
        /// </summary>
        public static GameObject Boot(GameShellSettings settings = null)
        {
            if (s_Root != null) return s_Root;
            if (settings != null) s_Settings = settings;
            GameShellSceneProvider.Register();

            s_Root = new GameObject("GameShell");
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(s_Root);
            s_Root.AddComponent<SceneFlow>();
            s_Root.AddComponent<PauseFrame>();
            Booted?.Invoke();
            return s_Root;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoBoot()
        {
            if (GameShellSettings.LoadOrDefault().autoBoot) Boot();
        }

        /// <summary>Removes the shell objects (tests, or a game that wants to rebuild it).</summary>
        public static void Shutdown()
        {
            if (s_Root != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(s_Root);
                else UnityEngine.Object.DestroyImmediate(s_Root);
            }
            s_Root = null;
        }

        // ---------- blocked checks ----------

        /// <summary>True while gameplay should stop reading input: pause open, a fade running, or any blocker true.</summary>
        public static bool BlocksGameplay => PauseFrame.IsOpen || SceneFlow.IsFading || AnyTrue(s_Blockers);

        /// <summary>Same as <see cref="BlocksGameplay"/>; kept for games that ask about input.</summary>
        public static bool BlocksInput => BlocksGameplay;

        /// <summary>Adds a "game is blocked" check (e.g. () => DevConsole.BlocksGame).</summary>
        public static void AddBlocker(Func<bool> blocker)
        {
            if (blocker != null && !s_Blockers.Contains(blocker)) s_Blockers.Add(blocker);
        }

        public static void RemoveBlocker(Func<bool> blocker) => s_Blockers.Remove(blocker);

        /// <summary>
        /// While any guard is true the pause key is ignored: another menu owns Esc
        /// (e.g. () => SettingsMenu.AnyOpen || SettingsMenu.ConsumedCloseThisFrame).
        /// </summary>
        public static void AddEscapeGuard(Func<bool> guard)
        {
            if (guard != null && !s_EscapeGuards.Contains(guard)) s_EscapeGuards.Add(guard);
        }

        public static void RemoveEscapeGuard(Func<bool> guard) => s_EscapeGuards.Remove(guard);

        public static bool EscapeGuarded => AnyTrue(s_EscapeGuards);

        static bool AnyTrue(List<Func<bool>> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    if (list[i]()) return true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return false;
        }

        // ---------- save / load through the hooks ----------

        public static bool Save(int slot)
        {
            if (SaveSlot != null) return SaveSlot(slot);
            return DefaultSave(slot);
        }

        public static LoadResult Load(int slot)
        {
            if (LoadSlot != null) return LoadSlot(slot) ?? new LoadResult { message = "Load returned nothing" };
            return DefaultLoad(slot);
        }

        public static string Summary(int slot)
        {
            if (SlotSummary != null)
            {
                try { return SlotSummary(slot) ?? ""; }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return "unreadable";
                }
            }
            return DefaultSummary(slot);
        }

        public static bool DefaultSave(int slot)
        {
            if (!SaveStore.Valid(slot))
            {
                Debug.LogWarning("[GameShell] Bad slot " + slot + " (valid 0.." + (SaveStore.SlotCount - 1) + ")");
                return false;
            }
            return SaveStore.Save(slot, new SaveEnvelope(), out _);
        }

        public static LoadResult DefaultLoad(int slot)
        {
            LastSavedScene = "";
            var result = SaveStore.Load(slot, MigrateLegacy);
            if (!result.ok) return result;
            GameShellVersion.NoteLoaded(result.savedVersion);
            if (Settings.restoreSceneOnLoad && !string.IsNullOrEmpty(LastSavedScene)
                && LastSavedScene != SceneManager.GetActiveScene().name)
            {
                if (SceneFlow.Known(LastSavedScene)) SceneFlow.Go(LastSavedScene, false);
                else Debug.LogWarning("[GameShell] Scene \"" + LastSavedScene + "\" is not known; staying here");
            }
            return result;
        }

        public static string DefaultSummary(int slot)
        {
            if (!SaveStore.Exists(slot)) return "empty";
            try
            {
                string when = File.GetLastWriteTime(SaveStore.PathFor(slot)).ToString("yyyy-MM-dd HH:mm");
                var env = SaveStore.Peek(slot, MigrateLegacy);
                if (env == null) return "unreadable";
                var entry = env.Find(GameShellSceneProvider.KeyId);
                if (entry != null && !string.IsNullOrEmpty(entry.json))
                {
                    var st = JsonUtility.FromJson<GameShellSceneState>(entry.json);
                    if (st != null && !string.IsNullOrEmpty(st.scene)) return when + ", " + st.scene;
                }
                return when;
            }
            catch
            {
                return "unreadable";
            }
        }

        // ---------- helpers ----------

        /// <summary>Current text size multiplier (1 when unset or invalid).</summary>
        internal static float CurrentTextScale()
        {
            float scale = 1f;
            if (TextScale != null)
            {
                try { scale = TextScale(); }
                catch (Exception e) { Debug.LogException(e); }
            }
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f) scale = 1f;
            return scale;
        }

        internal static int ScaledFont(int baseSize) =>
            Mathf.Clamp(Mathf.RoundToInt(baseSize * CurrentTextScale()), 8, 64);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics()
        {
            s_Blockers.Clear();
            s_EscapeGuards.Clear();
            s_Root = null;
            s_Settings = null;
            Booted = null;
            SaveSlot = null;
            LoadSlot = null;
            SlotSummary = null;
            MigrateLegacy = null;
            TextScale = null;
            PauseSubtitle = null;
            OpenSettings = null;
            LastSavedScene = "";
        }

        /// <summary>Tests: use these settings without booting.</summary>
        internal static void UseSettings(GameShellSettings settings) => s_Settings = settings;
    }
}
