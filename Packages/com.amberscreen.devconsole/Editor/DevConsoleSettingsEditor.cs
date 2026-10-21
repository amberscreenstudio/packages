using UnityEditor;
using UnityEngine;

namespace AmberScreen.Dev.Editor
{
    [CustomEditor(typeof(DevConsoleSettings))]
    sealed class DevConsoleSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            Draw("autoAddDefine", "keepInReleaseBuilds",
                "enableAttributeDiscovery", "enableProviderDiscovery",
                "showReopenButton",
                "showCursorWhileOpen", "pauseTimescaleWhileOpen",
                "openKey", "enableGamepadChord", "enableThreeFingerTap",
                "enableCornerHold", "cornerHoldWithMouseInEditor");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            Draw("layout", "scale", "scaleMultiplier", "columns", "autoColumns",
                "minEntryWidth", "buttonHeight", "fontSize", "spacing", "sortingOrder",
                "floatingScreenFraction", "floatingPanelWidth",
                "dockRightScreenFraction", "dockRightWidth", "portraitUiScale");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Rail", EditorStyles.boldLabel);
            Draw("railScreenFraction", "railMinWidth", "railMaxWidth", "railEdgeMargin",
                "railCollapsedWidth", "windowMargin", "rememberState");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Debug HUD", EditorStyles.boldLabel);
            Draw("hudShowOnStart", "hudToggleKey", "hudScreenFraction",
                "hudMinWidth", "hudMaxWidth", "hudOpacity", "hudFontSize",
                "hudUpdateHz", "hudPortraitHeightFraction");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Command line", EditorStyles.boldLabel);
            Draw("commandLine");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Theme", EditorStyles.boldLabel);
            Draw("theme", "fontOverride");

            // Any future serialized fields not listed above still appear.
            DrawRemaining();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        void Draw(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var prop = serializedObject.FindProperty(names[i]);
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, true);
            }
        }

        void DrawRemaining()
        {
            var drawn = new System.Collections.Generic.HashSet<string>
            {
                "m_Script",
                "autoAddDefine", "keepInReleaseBuilds",
                "enableAttributeDiscovery", "enableProviderDiscovery",
                "showReopenButton",
                "showCursorWhileOpen", "pauseTimescaleWhileOpen",
                "openKey", "enableGamepadChord", "enableThreeFingerTap",
                "enableCornerHold", "cornerHoldWithMouseInEditor",
                "layout", "scale", "scaleMultiplier", "columns", "autoColumns",
                "minEntryWidth", "buttonHeight", "fontSize", "spacing", "sortingOrder",
                "floatingScreenFraction", "floatingPanelWidth",
                "dockRightScreenFraction", "dockRightWidth", "portraitUiScale",
                "railScreenFraction", "railMinWidth", "railMaxWidth", "railEdgeMargin",
                "railCollapsedWidth", "windowMargin", "rememberState",
                "hudShowOnStart", "hudToggleKey", "hudScreenFraction",
                "hudMinWidth", "hudMaxWidth", "hudOpacity", "hudFontSize",
                "hudUpdateHz", "hudPortraitHeightFraction",
                "commandLine", "theme", "fontOverride"
            };

            var it = serializedObject.GetIterator();
            bool enterChildren = true;
            bool any = false;
            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (drawn.Contains(it.name))
                    continue;
                if (!any)
                {
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("Other", EditorStyles.boldLabel);
                    any = true;
                }

                EditorGUILayout.PropertyField(it, true);
            }
        }
    }
}
