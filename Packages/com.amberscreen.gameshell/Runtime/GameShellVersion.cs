// Version label text and the "save is from another version" warning (warn, then load anyway).
using AmberScreen.Save;
using UnityEngine;

namespace AmberScreen.GameShell
{
    public static class GameShellVersion
    {
        /// <summary>Version saves without a stamp are treated as.</summary>
        public const string Legacy = "0.1.0";

        public static string Current => Application.version;
        public static string Label => "v" + Current;

        public static string WarningText { get; private set; } = "";
        static float s_WarningUntil;

        /// <summary>Call after a load. A different MAJOR (0.MAJOR.MINOR) shows a warning; returns true if it did.</summary>
        public static bool NoteLoaded(string saveVersion) => NoteLoaded(saveVersion, Current);

        public static bool NoteLoaded(string saveVersion, string currentVersion)
        {
            if (string.IsNullOrEmpty(saveVersion)) saveVersion = Legacy;
            if (!SaveStore.IsMajorMismatch(saveVersion, currentVersion))
            {
                WarningText = "";
                return false;
            }
            ShowWarning("Save is from v" + saveVersion + " and may be incompatible with v" + currentVersion + ".");
            return true;
        }

        /// <summary>Shows <paramref name="text"/> under the version label for the settings' warning time.</summary>
        public static void ShowWarning(string text) =>
            ShowWarning(text, GameShellSettings.LoadOrDefault().versionWarningSeconds);

        public static void ShowWarning(string text, float seconds)
        {
            if (string.IsNullOrEmpty(text)) return;
            WarningText = text;
            s_WarningUntil = Time.unscaledTime + seconds;
            Debug.LogWarning("[GameShell] " + text);
        }

        /// <summary>The warning while it is still showing, else "".</summary>
        public static string ActiveWarning()
        {
            if (string.IsNullOrEmpty(WarningText) || Time.unscaledTime > s_WarningUntil) return "";
            return WarningText;
        }

        /// <summary>Label line plus the active warning, as the version label shows it.</summary>
        public static string LabelLine()
        {
            string warn = ActiveWarning();
            return string.IsNullOrEmpty(warn) ? Label : Label + "\n" + warn;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics()
        {
            WarningText = "";
            s_WarningUntil = 0f;
        }
    }
}
