using AmberScreen.Dev;
using AmberScreen.Sandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AmberScreen.Sandbox.Editor
{
    /// <summary>
    /// Rebuilds Sandbox URP assets, scene, and project pipeline references through the Unity API.
    /// </summary>
    public static class SandboxSetup
    {
        const string UrpFolder = "Assets/Settings/URP";
        const string RendererPath = UrpFolder + "/SandboxRenderer.asset";
        const string PipelinePath = UrpFolder + "/SandboxURP.asset";
        const string ScenePath = "Assets/Scenes/DevConsoleSandbox.unity";
        const string MaterialPath = "Assets/Materials/SandboxCube.mat";
        const string SettingsPath = "Assets/Resources/AmberScreenDevConsoleSettings.asset";

        [MenuItem("AmberScreen/Rebuild Sandbox")]
        public static void RebuildSandbox()
        {
            EnsureFolders();
            var pipeline = CreateUrpAssets();
            AssignPipeline(pipeline);
            CreateScene();
            ResetDevConsoleSettingsToDefaults();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Sandbox] Rebuilt. Commit Sandbox/Assets and Sandbox/ProjectSettings.");
        }

        [MenuItem("AmberScreen/Reset DevConsole Settings To Defaults")]
        public static void ResetDevConsoleSettingsToDefaults()
        {
            EnsureFolder("Assets/Resources");
            var settings = AssetDatabase.LoadAssetAtPath<DevConsoleSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DevConsoleSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            settings.ApplyCompactDefaults();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sandbox] DevConsole settings reset to compact defaults.");
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Settings");
            EnsureFolder(UrpFolder);
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Editor");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static UniversalRenderPipelineAsset CreateUrpAssets()
        {
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.DeleteAsset(RendererPath);
            AssetDatabase.CreateAsset(rendererData, RendererPath);

            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.DeleteAsset(PipelinePath);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        static void AssignPipeline(UniversalRenderPipelineAsset asset)
        {
            GraphicsSettings.defaultRenderPipeline = asset;

            int previous = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = asset;
            }

            QualitySettings.SetQualityLevel(previous);
        }

        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 1f, -5f);
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();

            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            var material = new Material(shader != null ? shader : Shader.Find("Standard"));
            AssetDatabase.DeleteAsset(MaterialPath);
            AssetDatabase.CreateAsset(material, MaterialPath);
            var renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            var uiModule = esGo.AddComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();

            var root = new GameObject("SandboxRoot");
            var commands = root.AddComponent<SandboxCommands>();
            root.AddComponent<SandboxStatusLabel>();
            var gameInput = root.AddComponent<SandboxGameInput>();

            var so = new SerializedObject(commands);
            so.FindProperty("cube").objectReferenceValue = cube.transform;
            so.FindProperty("cubeRenderer").objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            var gi = new SerializedObject(gameInput);
            gi.FindProperty("cubeRenderer").objectReferenceValue = renderer;
            gi.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }
    }
}
