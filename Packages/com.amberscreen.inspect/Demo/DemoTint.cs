using UnityEngine;

namespace AmberScreen.Inspect.Demo
{
    /// <summary>Demo-only colour without creating material assets (works in any render pipeline).</summary>
    [ExecuteAlways]
    public sealed class DemoTint : MonoBehaviour
    {
        public Color color = Color.white;

        void OnEnable() => Apply();
        void OnValidate() => Apply();

        public void Apply()
        {
            var r = GetComponent<Renderer>();
            if (r == null) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color); // URP / HDRP Lit
            block.SetColor("_Color", color);     // Built-in Standard
            r.SetPropertyBlock(block);
        }
    }
}
