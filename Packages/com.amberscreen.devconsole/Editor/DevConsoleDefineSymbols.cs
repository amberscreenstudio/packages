using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AmberScreen.Dev.Editor
{
    [InitializeOnLoad]
    sealed class DevConsoleDefineSymbols : IActiveBuildTargetChanged
    {
        internal const string Define = "AMBERSCREEN_DEVCONSOLE";

        public int callbackOrder => 0;

        static DevConsoleDefineSymbols()
        {
            EditorApplication.delayCall += EnsureDefineForActiveTarget;
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            EnsureDefineForActiveTarget();
        }

        internal static NamedBuildTarget ActiveNamedTarget()
        {
            var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }

        internal static void EnsureDefineForActiveTarget()
        {
            var settings = Resources.Load<DevConsoleSettings>(DevConsoleSettings.ResourcesName);
            if (settings != null && !settings.autoAddDefine)
                return;

            var named = ActiveNamedTarget();
            if (named == NamedBuildTarget.Unknown)
                return;

            AddDefine(named);
        }

        internal static void AddDefine(NamedBuildTarget named)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(named);
            var parts = defines.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Contains(Define))
                return;

            string next = parts.Length == 0 ? Define : string.Join(";", parts) + ";" + Define;
            PlayerSettings.SetScriptingDefineSymbols(named, next);
        }

        internal static bool RemoveDefine(NamedBuildTarget named, out string previous)
        {
            previous = PlayerSettings.GetScriptingDefineSymbols(named);
            var parts = previous.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p != Define)
                .ToArray();
            if (parts.Length == previous.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries).Length)
                return false;

            PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", parts));
            return true;
        }

        [MenuItem("Amber Screen/Dev Console/Add Scripting Define")]
        static void MenuAddDefine()
        {
            EnsureDefineForActiveTarget();
            Debug.Log($"[{Define}] ensure requested for active build target.");
        }
    }

    sealed class DevConsoleReleaseBuildStrip : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static bool s_Removed;
        static string s_PreviousDefines;
        static NamedBuildTarget s_Target;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_Removed = false;
            s_PreviousDefines = null;

            var settings = Resources.Load<DevConsoleSettings>(DevConsoleSettings.ResourcesName);
            if (settings != null && settings.keepInReleaseBuilds)
                return;

            if ((report.summary.options & BuildOptions.Development) != 0)
                return;

            s_Target = NamedBuildTarget.FromBuildTargetGroup(
                BuildPipeline.GetBuildTargetGroup(report.summary.platform));
            if (s_Target == NamedBuildTarget.Unknown)
                return;

            s_Removed = DevConsoleDefineSymbols.RemoveDefine(s_Target, out s_PreviousDefines);
            if (s_Removed)
                Debug.Log("[DevConsole] AMBERSCREEN_DEVCONSOLE stripped for release build");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (!s_Removed)
                return;

            PlayerSettings.SetScriptingDefineSymbols(s_Target, s_PreviousDefines);
            s_Removed = false;
            Debug.Log("[DevConsole] define restored");
        }
    }
}
