using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Writes normalised 0..1 position (within the graphic's vertex AABB) into UV1 so
    /// <c>AmberScreen/UI/CircleClip</c> can crop atlased / preserveAspect sprites correctly.
    /// Sampling still uses UV0.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class CircleClipMeshModifier : BaseMeshEffect
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureUv1OnCanvas();
        }

        void EnsureUv1OnCanvas()
        {
            var g = graphic;
            if (g == null || g.canvas == null)
                return;
            var root = g.canvas.rootCanvas;
            if (root == null)
                return;
            root.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
                return;

            EnsureUv1OnCanvas();

            var v = new UIVertex();
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                min = Vector2.Min(min, v.position);
                max = Vector2.Max(max, v.position);
            }

            float w = Mathf.Max(1e-4f, max.x - min.x);
            float h = Mathf.Max(1e-4f, max.y - min.y);
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                float u = (v.position.x - min.x) / w;
                float vv = (v.position.y - min.y) / h;
                v.uv1 = new Vector4(u, vv, 0f, 0f);
                vh.SetUIVertex(v, i);
            }
        }
    }
}
