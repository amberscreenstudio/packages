using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>Pure, testable material report. Never writes assets or import settings.</summary>
    public static class ModelMaterialReport
    {
        public sealed class Entry
        {
            public string HierarchyPath;
            public int Slot;
            public string MaterialName;
            public string MaterialPath;
            public string ShaderName;
            public string BaseMapPath;
            public string Warning;
        }

        public sealed class Result
        {
            public int Renderers;
            public int Slots;
            public int Warnings;
            public readonly List<Entry> Entries = new();
            public readonly List<string> WarningMessages = new();

            public string SummaryLine =>
                "ModelReview report: " + Renderers + " renderers, " +
                Slots + " slots, " + Warnings + " warnings.";

            public string ToText()
            {
                var sb = new StringBuilder();
                sb.AppendLine(SummaryLine);
                foreach (var e in Entries)
                {
                    sb.Append(e.HierarchyPath).Append(" [").Append(e.Slot).Append("] ")
                      .Append(e.MaterialName ?? "(null)").Append(" | ")
                      .Append(e.MaterialPath ?? "?").Append(" | ")
                      .Append(e.ShaderName ?? "?").Append(" | tex=")
                      .Append(e.BaseMapPath ?? "none");
                    if (!string.IsNullOrEmpty(e.Warning))
                        sb.Append(" | WARN ").Append(e.Warning);
                    sb.AppendLine();
                }
                foreach (var w in WarningMessages)
                    sb.AppendLine("WARN: ").Append(w);
                return sb.ToString();
            }
        }

        public static Result Build(GameObject root, string sourceAssetPath = null)
        {
            var result = new Result();
            if (root == null) return result;

            string fbxPath = null;
            ModelImporter importer = null;
            if (!string.IsNullOrEmpty(sourceAssetPath))
            {
                fbxPath = sourceAssetPath;
                importer = AssetImporter.GetAtPath(sourceAssetPath) as ModelImporter;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            result.Renderers = renderers.Length;

            GameObject fbxRoot = null;
            if (importer != null && !string.IsNullOrEmpty(fbxPath))
                fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

            foreach (var renderer in renderers)
            {
                string hier = GetHierarchyPath(root.transform, renderer.transform);
                var shared = renderer.sharedMaterials;
                int submeshCount = GetSubMeshCount(renderer);
                int slotCount = Mathf.Max(shared.Length, submeshCount);
                if (shared.Length != submeshCount && submeshCount > 0)
                    result.WarningMessages.Add(hier + ": material count (" + shared.Length +
                                               ") != subMeshCount (" + submeshCount + ")");

                // Resolve source FBX for prefab instances (read-only).
                ModelImporter slotImporter = importer;
                GameObject slotFbxRoot = fbxRoot;
                string slotFbxPath = fbxPath;
                var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer);
                if (original != null)
                {
                    string p = AssetDatabase.GetAssetPath(original);
                    if (!string.IsNullOrEmpty(p))
                    {
                        var mi = AssetImporter.GetAtPath(p) as ModelImporter;
                        if (mi != null)
                        {
                            slotImporter = mi;
                            slotFbxPath = p;
                            slotFbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                        }
                    }
                }

                if (slotImporter == null && string.IsNullOrEmpty(sourceAssetPath))
                {
                    // logged once per build via summary note
                }

                for (int slot = 0; slot < slotCount; slot++)
                {
                    result.Slots++;
                    var entry = new Entry
                    {
                        HierarchyPath = hier,
                        Slot = slot,
                        MaterialName = "(null)",
                        MaterialPath = "none",
                        ShaderName = "none",
                        BaseMapPath = "none"
                    };

                    Material mat = slot < shared.Length ? shared[slot] : null;
                    Material reference = FindReferenceMaterial(slotImporter, slotFbxRoot, hier, slot, mat);

                        if (mat == null)
                    {
                        entry.Warning = "missing material";
                    }
                    else
                    {
                        entry.MaterialName = mat.name;
                        string matPath = AssetDatabase.GetAssetPath(mat);
                        entry.MaterialPath = string.IsNullOrEmpty(matPath)
                            ? "embedded in " + (slotFbxPath ?? "?")
                            : matPath;
                        var shader = mat.shader;
                        entry.ShaderName = shader != null ? shader.name : "null";
                        entry.BaseMapPath = ResolveBaseMapPath(mat) ?? "none";

                        var warns = new List<string>();
                        if (shader == null
                            || shader.name == "Hidden/InternalErrorShader"
                            || !shader.isSupported
                            || !IsUrpShader(shader))
                            warns.Add("non-URP shader (renders pink)");

                        if (reference != null && mat != reference)
                            warns.Add("not the import reference material");

                        if (reference != null
                            && ResolveBaseMap(reference) != null
                            && ResolveBaseMap(mat) == null)
                            warns.Add("base texture missing vs reference");

                        if (HasBrokenTextureReference(mat))
                            warns.Add("broken texture reference");

                        if (warns.Count > 0)
                            entry.Warning = string.Join("; ", warns);
                    }

                    if (!string.IsNullOrEmpty(entry.Warning))
                        result.Warnings++;
                    result.Entries.Add(entry);
                }
            }

            if (importer == null && string.IsNullOrEmpty(sourceAssetPath))
                result.WarningMessages.Add("no ModelImporter, import compare skipped");
            else if (importer != null)
            {
                var importMode = importer.materialImportMode;
                if (importMode == ModelImporterMaterialImportMode.None)
                {
                    result.WarningMessages.Add("materials not imported, model will look grey");
                    result.Warnings++;
                }
            }

            return result;
        }

        public static void LogAndShow(Result result)
        {
            if (result == null) return;
            string text = result.ToText();
            if (result.Warnings > 0)
                Debug.LogWarning(text);
            else
                Debug.Log(text);
            ModelReviewReportWindow.Show(text);
        }

        static int GetSubMeshCount(Renderer renderer)
        {
            if (renderer is MeshRenderer)
            {
                var mf = renderer.GetComponent<MeshFilter>();
                return mf != null && mf.sharedMesh != null ? mf.sharedMesh.subMeshCount : 0;
            }
            if (renderer is SkinnedMeshRenderer smr)
                return smr.sharedMesh != null ? smr.sharedMesh.subMeshCount : 0;
            return 0;
        }

        static string GetHierarchyPath(Transform root, Transform t)
        {
            var stack = new Stack<string>();
            while (t != null && t != root)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }

        static Material FindReferenceMaterial(
            ModelImporter importer,
            GameObject fbxRoot,
            string hierarchyPath,
            int slot,
            Material current)
        {
            if (importer == null) return null;

            // External object map by material name
            if (current != null)
            {
                try
                {
                    var map = importer.GetExternalObjectMap();
                    foreach (var kv in map)
                    {
                        if (kv.Key.type == typeof(Material)
                            && string.Equals(kv.Key.name, current.name, StringComparison.Ordinal)
                            && kv.Value is Material m)
                            return m;
                    }
                }
                catch
                {
                    // read-only best effort
                }
            }

            if (fbxRoot == null) return null;
            var fbxRenderer = FindRendererAtPath(fbxRoot.transform, hierarchyPath);
            if (fbxRenderer == null) return null;
            var mats = fbxRenderer.sharedMaterials;
            if (slot >= 0 && slot < mats.Length) return mats[slot];
            return null;
        }

        static Renderer FindRendererAtPath(Transform root, string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
                return root.GetComponent<Renderer>();
            var t = root;
            foreach (var part in hierarchyPath.Split('/'))
            {
                Transform next = null;
                for (int i = 0; i < t.childCount; i++)
                {
                    if (t.GetChild(i).name == part)
                    {
                        next = t.GetChild(i);
                        break;
                    }
                }
                if (next == null) return null;
                t = next;
            }
            return t.GetComponent<Renderer>();
        }

        static bool IsUrpShader(Shader shader)
        {
            if (shader == null) return false;
            string n = shader.name ?? "";
            if (n.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)
                || n.StartsWith("AmberScreen/", StringComparison.Ordinal))
                return true;
            // Read RenderPipeline tag from subshader 0 when available.
            var tagId = new ShaderTagId("RenderPipeline");
            try
            {
                var method = typeof(Shader).GetMethod(
                    "FindSubshaderTagValue",
                    new[] { typeof(int), typeof(ShaderTagId) });
                if (method != null)
                {
                    var tag = (ShaderTagId)method.Invoke(shader, new object[] { 0, tagId });
                    return tag == new ShaderTagId("UniversalPipeline");
                }
            }
            catch
            {
                // fall through
            }
            return false;
        }

        static Texture ResolveBaseMap(Material mat)
        {
            if (mat == null) return null;
            if (mat.HasProperty("_BaseMap")) return mat.GetTexture("_BaseMap");
            if (mat.HasProperty("_MainTex")) return mat.GetTexture("_MainTex");
            return null;
        }

        static string ResolveBaseMapPath(Material mat)
        {
            var tex = ResolveBaseMap(mat);
            if (tex == null) return null;
            string p = AssetDatabase.GetAssetPath(tex);
            return string.IsNullOrEmpty(p) ? tex.name : p;
        }

        static bool HasBrokenTextureReference(Material mat)
        {
            if (mat == null) return false;
            var so = new SerializedObject(mat);
            var texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (texEnvs == null || !texEnvs.isArray) return false;
            for (int i = 0; i < texEnvs.arraySize; i++)
            {
                var elem = texEnvs.GetArrayElementAtIndex(i);
                var val = elem.FindPropertyRelative("second.m_Texture");
                if (val == null) continue;
                if (val.objectReferenceValue != null) continue;
                // Non-zero EntityId with null resolve = broken reference
                if (val.objectReferenceEntityIdValue != EntityId.None)
                    return true;
            }
            return false;
        }
    }
}
