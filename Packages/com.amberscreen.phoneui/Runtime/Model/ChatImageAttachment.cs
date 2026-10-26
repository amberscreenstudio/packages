using System;
using UnityEngine;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Optional image attachment on a <see cref="ChatMessage"/> (thumbnail in-thread, full viewer on tap).
    /// PhoneUI 0.2.0.
    /// </summary>
    [Serializable]
    public sealed class ChatImageAttachment
    {
        public string FileName;
        public Texture2D Texture;
        public Sprite Sprite;
        /// <summary>Width / height. ≤0 → derive from texture/sprite.</summary>
        public float Aspect = 0f;

        public float ResolvedAspect
        {
            get
            {
                if (Aspect > 0.01f) return Aspect;
                if (Texture != null && Texture.height > 0)
                    return Texture.width / (float)Texture.height;
                if (Sprite != null && Sprite.rect.height > 0.01f)
                    return Sprite.rect.width / Sprite.rect.height;
                return 9f / 16f;
            }
        }

        public Texture ResolvedTexture =>
            Texture != null ? Texture : (Sprite != null ? Sprite.texture : null);

        /// <summary>
        /// UV rect for a (possibly packed) sprite within its atlas texture.
        /// Full-texture sprites return (0,0,1,1). Hosts should set <see cref="RawImage.uvRect"/>
        /// when drawing packed sprites; using the whole atlas without this crops incorrectly.
        /// </summary>
        public static Rect SpriteUvRect(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return new Rect(0f, 0f, 1f, 1f);
            var tex = sprite.texture;
            if (tex.width <= 0 || tex.height <= 0) return new Rect(0f, 0f, 1f, 1f);

            // Tight packing: textureRect throws; use UV bounds / full rect instead.
            if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
            {
                var uvs = sprite.uv;
                if (uvs != null && uvs.Length > 0)
                {
                    float minU = uvs[0].x, maxU = uvs[0].x, minV = uvs[0].y, maxV = uvs[0].y;
                    for (int i = 1; i < uvs.Length; i++)
                    {
                        if (uvs[i].x < minU) minU = uvs[i].x;
                        if (uvs[i].x > maxU) maxU = uvs[i].x;
                        if (uvs[i].y < minV) minV = uvs[i].y;
                        if (uvs[i].y > maxV) maxV = uvs[i].y;
                    }
                    return new Rect(minU, minV, maxU - minU, maxV - minV);
                }
                return new Rect(0f, 0f, 1f, 1f);
            }

            var r = sprite.textureRect;
            return new Rect(
                r.x / tex.width,
                r.y / tex.height,
                r.width / tex.width,
                r.height / tex.height);
        }
    }
}

