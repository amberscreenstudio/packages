#if AMBERSCREEN_DEVCONSOLE
using UnityEditor;
using UnityEngine;

namespace AmberScreen.Dev.Editor
{
    [InitializeOnLoad]
    static class DevConsoleTmpMissingPrompt
    {
        static bool s_ShownThisSession;

        static DevConsoleTmpMissingPrompt()
        {
            DevConsoleHost.TmpEssentialsMissingEditorHook = ShowOnce;
        }

        static void ShowOnce()
        {
            if (s_ShownThisSession)
                return;
            s_ShownThisSession = true;

            bool importNow = EditorUtility.DisplayDialog(
                "Amber Screen Dev Console",
                DevConsoleHost.TmpEssentialsMissingMessage,
                "Import now",
                "Later");
            if (!importNow)
                return;

            if (!TryImportViaTmpApi())
            {
                EditorApplication.ExecuteMenuItem(
                    "Window/TextMeshPro/Import TMP Essential Resources");
            }
        }

        static bool TryImportViaTmpApi()
        {
            try
            {
                // Present in TMP Editor assemblies; may not be public in every version.
                var type = System.Type.GetType(
                    "TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro");
                if (type == null)
                {
                    foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = asm.GetType("TMPro.TMP_PackageResourceImporter");
                        if (type != null)
                            break;
                    }
                }

                if (type == null)
                    return false;

                var method = type.GetMethod(
                    "ImportResources",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static);
                if (method == null)
                    return false;

                method.Invoke(null, new object[] { true, false, false });
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    "[DevConsole] TMP ImportResources failed; opening menu instead.\n" + e.Message);
                return false;
            }
        }
    }
}
#endif
