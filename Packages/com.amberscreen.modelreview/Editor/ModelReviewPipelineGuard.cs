using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>
    /// Switches Graphics/Quality pipeline to a runtime clone of ModelReview_URP while the review
    /// scene is active. Restores originals in memory only — never writes ProjectSettings.
    /// </summary>
    [InitializeOnLoad]
    public static class ModelReviewPipelineGuard
    {
        const string SessionSwitched = "AmberScreen.ModelReview.PipelineSwitched";
        const string SessionDefaultGuid = "AmberScreen.ModelReview.DefaultRPGuid";
        const string SessionQualityCount = "AmberScreen.ModelReview.QualityCount";
        const string SessionQualityPrefix = "AmberScreen.ModelReview.QualityRPGuid.";
        const string ReviewSceneName = "ModelReview";

        static UniversalRenderPipelineAsset s_Clone;
        static bool s_Registered;

        static ModelReviewPipelineGuard()
        {
            EnsureHooks();
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionSwitched, false) && !IsReviewSceneActive())
                    Restore();
                else if (IsReviewSceneActive())
                    TrySwitch();
            };
        }

        static void EnsureHooks()
        {
            if (s_Registered) return;
            s_Registered = true;
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => OnSceneContextChanged();
            EditorSceneManager.sceneClosing += (scene, _) =>
            {
                if (IsReviewScene(scene)) Restore();
            };
            EditorApplication.playModeStateChanged += _ =>
                EditorApplication.delayCall += OnSceneContextChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Restore;
            EditorApplication.wantsToQuit += () =>
            {
                Restore();
                return true;
            };
            EditorApplication.quitting += Restore;
        }

        static void OnSceneContextChanged()
        {
            if (IsReviewSceneActive())
                TrySwitch();
            else
                Restore();
        }

        public static bool IsReviewSceneActive()
        {
            var scene = SceneManager.GetActiveScene();
            return IsReviewScene(scene);
        }

        public static bool IsReviewScene(Scene scene)
        {
            if (!scene.IsValid()) return false;
            string path = scene.path ?? "";
            return path.Replace('\\', '/').IndexOf(
                       "com.amberscreen.modelreview/Scenes/ModelReview",
                       StringComparison.OrdinalIgnoreCase) >= 0
                   || string.Equals(scene.name, ReviewSceneName, StringComparison.Ordinal);
        }

        public static bool TrySwitch()
        {
            EnsureHooks();
            if (!CanSwitch(out string reason))
            {
                ApplyLegendFlag(false);
                if (!string.IsNullOrEmpty(reason))
                    Debug.LogWarning("[ModelReview] Pipeline switch refused: " + reason);
                return false;
            }

            var source = LoadReviewUrp();
            if (source == null)
            {
                ApplyLegendFlag(false);
                Debug.LogWarning("[ModelReview] ModelReview_URP.asset missing — rebuild package assets.");
                return false;
            }

            if (!SessionState.GetBool(SessionSwitched, false))
                StoreOriginals();

            if (s_Clone == null)
            {
                s_Clone = UnityEngine.Object.Instantiate(source);
                s_Clone.hideFlags = HideFlags.HideAndDontSave;
                s_Clone.name = source.name + " (Review Clone)";
            }

            GraphicsSettings.defaultRenderPipeline = s_Clone;
            QualitySettings.renderPipeline = s_Clone;
            SessionState.SetBool(SessionSwitched, true);
            ApplyLegendFlag(true);

            var rig = UnityEngine.Object.FindAnyObjectByType<ModelReviewRig>();
            if (rig != null)
            {
                rig.SetRuntimePipelineClone(s_Clone);
                rig.UsingReviewPipeline = true;
            }
            return true;
        }

        public static void Restore()
        {
            if (!SessionState.GetBool(SessionSwitched, false))
            {
                DestroyClone();
                return;
            }

            GraphicsSettings.defaultRenderPipeline = LoadByGuid(SessionState.GetString(SessionDefaultGuid, ""));
            int count = SessionState.GetInt(SessionQualityCount, 0);
            int previous = QualitySettings.GetQualityLevel();
            for (int i = 0; i < count && i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = LoadByGuid(SessionState.GetString(SessionQualityPrefix + i, ""));
            }
            QualitySettings.SetQualityLevel(previous, true);

            SessionState.SetBool(SessionSwitched, false);
            DestroyClone();
            ApplyLegendFlag(false);
        }

        public static UniversalRenderPipelineAsset ActiveClone => s_Clone;

        public static bool CanSwitch(out string reason)
        {
            reason = null;
            var current = GraphicsSettings.defaultRenderPipeline;
            if (current == null)
            {
                // Built-in: only switch if URP global settings already exist (avoid auto-create write).
                var settings = GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>();
                if (settings == null)
                {
                    reason = "Built-in host without URP global settings";
                    return false;
                }
                return true;
            }

            if (current is UniversalRenderPipelineAsset)
                return true;

            reason = "Host uses a non-URP SRP";
            return false;
        }

        static void StoreOriginals()
        {
            SessionState.SetString(SessionDefaultGuid, GuidOf(GraphicsSettings.defaultRenderPipeline));
            string[] names = QualitySettings.names;
            SessionState.SetInt(SessionQualityCount, names.Length);
            int previous = QualitySettings.GetQualityLevel();
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                SessionState.SetString(SessionQualityPrefix + i, GuidOf(QualitySettings.renderPipeline));
            }
            QualitySettings.SetQualityLevel(previous, true);
        }

        static string GuidOf(RenderPipelineAsset asset)
        {
            if (asset == null) return "";
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? "" : AssetDatabase.AssetPathToGUID(path);
        }

        static RenderPipelineAsset LoadByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
        }

        static UniversalRenderPipelineAsset LoadReviewUrp()
        {
            string[] guids = AssetDatabase.FindAssets("ModelReview_URP t:UniversalRenderPipelineAsset");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (path.Replace('\\', '/').IndexOf("com.amberscreen.modelreview/", StringComparison.Ordinal) >= 0)
                    return AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            }
            return null;
        }

        static void DestroyClone()
        {
            if (s_Clone != null)
            {
                UnityEngine.Object.DestroyImmediate(s_Clone);
                s_Clone = null;
            }
        }

        static void ApplyLegendFlag(bool review)
        {
            var rig = UnityEngine.Object.FindAnyObjectByType<ModelReviewRig>();
            if (rig != null) rig.UsingReviewPipeline = review;
        }

        class SaveInterceptor : AssetModificationProcessor
        {
            static string[] OnWillSaveAssets(string[] paths)
            {
                if (!SessionState.GetBool(SessionSwitched, false))
                    return paths;

                Restore();
                EditorApplication.delayCall += () =>
                {
                    if (IsReviewSceneActive())
                        TrySwitch();
                };

                var kept = new List<string>(paths.Length);
                foreach (var p in paths)
                {
                    if (p != null && p.Replace('\\', '/').IndexOf(
                            "com.amberscreen.modelreview/Scenes/ModelReview",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.Log("[ModelReview] Model Review scene is read-only — excluded from save.");
                        continue;
                    }
                    kept.Add(p);
                }
                return kept.ToArray();
            }
        }
    }
}
