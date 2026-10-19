using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AmberScreen.DevConsole.Editor
{
    [InitializeOnLoad]
    static class DevConsoleDefineSymbols
    {
        const string Define = "AMBERSCREEN_DEVCONSOLE";

        static DevConsoleDefineSymbols()
        {
            EditorApplication.delayCall += EnsureDefine;
        }

        static void EnsureDefine()
        {
            var settings = Resources.Load<DevConsoleSettings>(DevConsoleSettings.ResourcesName);
            if (settings != null && !settings.autoAddDefine)
                return;

            var named = NamedBuildTarget.FromBuildTargetGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            if (named == NamedBuildTarget.Unknown)
                return;

            string defines = PlayerSettings.GetScriptingDefineSymbols(named);
            var parts = defines.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Contains(Define))
                return;

            string next = parts.Length == 0 ? Define : string.Join(";", parts) + ";" + Define;
            PlayerSettings.SetScriptingDefineSymbols(named, next);
        }

        [MenuItem("Amber Screen/Dev Console/Add Scripting Define")]
        static void MenuAddDefine()
        {
            EnsureDefine();
            Debug.Log($"[{Define}] ensure requested for active build target.");
        }
    }
}
