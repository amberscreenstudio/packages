using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.PhoneUI
{
    public static class UiFactory
    {
        static Sprite _whiteSprite;
        static TMP_FontAsset _font;
        static bool _fallbacksWired;
        static bool _circleClipCreatedAtRuntime;
        static bool _roundedIconClipCreatedAtRuntime;
        static Material _roundedIconClipMaterial;

        /// <summary>Clear cached font / fallback wiring so the next <see cref="DefaultFont"/> read picks up a new theme.</summary>
        public static void InvalidateFontCache()
        {
            _font = null;
            _fallbacksWired = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _font = null;
            _fallbacksWired = false;
            if (_circleClipMaterial != null && _circleClipCreatedAtRuntime)
                Object.DestroyImmediate(_circleClipMaterial);
            _circleClipMaterial = null;
            _circleClipCreatedAtRuntime = false;
            if (_roundedIconClipMaterial != null && _roundedIconClipCreatedAtRuntime)
                Object.DestroyImmediate(_roundedIconClipMaterial);
            _roundedIconClipMaterial = null;
            _roundedIconClipCreatedAtRuntime = false;
            _whiteSprite = null;
            _circleSprite = null;
            _roundedSquareSprite = null;
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null) return _whiteSprite;
                var tex = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                return _whiteSprite;
            }
        }

        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_font != null)
                {
                    EnsureFallbacks(_font);
                    return _font;
                }

                // Theme FontSettings first; Resources/PhoneUI/PhoneFontSettings only when theme has none.
                PhoneFontSettings settings = null;
                var theme = PhoneUITheme.Active;
                if (theme != null && theme.FontSettings != null)
                    settings = theme.FontSettings;
                if (settings == null)
                    settings = Resources.Load<PhoneFontSettings>("PhoneUI/PhoneFontSettings");

                if (settings != null && settings.Primary != null)
                    _font = settings.Primary;

                if (_font == null && TMP_Settings.instance != null)
                    _font = TMP_Settings.defaultFontAsset;
                if (_font == null)
                    _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (_font == null)
                    Debug.LogError("[UiFactory] TMP font missing — import TMP Essential Resources");

                EnsureFallbacks(_font, settings);
                return _font;
            }
        }

        /// <summary>Wire checkmark/emoji fallbacks into the primary font (idempotent).</summary>
        public static void EnsureFallbacks(TMP_FontAsset primary, PhoneFontSettings settings = null)
        {
            if (primary == null || _fallbacksWired) return;
            _fallbacksWired = true;

            if (settings == null)
                settings = Resources.Load<PhoneFontSettings>("PhoneUI/PhoneFontSettings");

            if (primary.fallbackFontAssetTable == null)
                primary.fallbackFontAssetTable = new List<TMP_FontAsset>();

            if (settings != null && settings.Fallbacks != null)
            {
                foreach (var fb in settings.Fallbacks)
                {
                    if (fb == null) continue;
                    if (!primary.fallbackFontAssetTable.Contains(fb))
                        primary.fallbackFontAssetTable.Add(fb);
                }
            }

            // Runtime dynamic fallbacks from TTF/OTF (Noto Sans Symbols 2 for ✓, Noto Emoji).
            TryAddRuntimeFallback(primary, "Fonts/NotoSansSymbols2-Regular");
            TryAddRuntimeFallback(primary, "Fonts/NotoEmoji-Regular");
            TryAddRuntimeFallback(primary, "PhoneUI/Fonts/NotoSansSymbols2-Regular");
            TryAddRuntimeFallback(primary, "PhoneUI/Fonts/NotoEmoji-Regular");

            if (settings != null && settings.RuntimeFallbackSourceFonts != null)
            {
                foreach (var f in settings.RuntimeFallbackSourceFonts)
                {
                    if (f == null) continue;
                    try
                    {
                        var dyn = TMP_FontAsset.CreateFontAsset(f);
                        if (dyn != null && !primary.fallbackFontAssetTable.Contains(dyn))
                            primary.fallbackFontAssetTable.Add(dyn);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("[UiFactory] fallback CreateFontAsset failed: " + ex.Message);
                    }
                }
            }

            // Built-in LiberationSans SDF Fallback (if present) — includes U+2026 ellipsis.
            var stock = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
            if (stock != null && !primary.fallbackFontAssetTable.Contains(stock))
                primary.fallbackFontAssetTable.Add(stock);

            EnsureEllipsisGlyph(primary);
            Debug.Log("[UiFactory] Font fallbacks wired count=" + primary.fallbackFontAssetTable.Count);
        }

        /// <summary>
        /// TMP TextOverflowModes.Ellipsis needs U+2026 in the primary or a fallback atlas.
        /// Force-try-add on primary + each fallback so "Ellipsis not available" stops.
        /// </summary>
        static void EnsureEllipsisGlyph(TMP_FontAsset primary)
        {
            const char ellipsis = '\u2026';
            TryAddChar(primary, ellipsis);
            if (primary.fallbackFontAssetTable == null) return;
            foreach (var fb in primary.fallbackFontAssetTable)
                TryAddChar(fb, ellipsis);
        }

        static void TryAddChar(TMP_FontAsset font, char ch)
        {
            if (font == null) return;
            try
            {
                if (font.HasCharacter(ch, searchFallbacks: false, tryAddCharacter: true))
                    return;
                font.TryAddCharacters(new string(ch, 1));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[UiFactory] ellipsis glyph add failed on " + font.name + ": " + ex.Message);
            }
        }

        static void TryAddRuntimeFallback(TMP_FontAsset primary, string resourcesPath)
        {
            var font = Resources.Load<Font>(resourcesPath);
            if (font == null) return;
            try
            {
                var dyn = TMP_FontAsset.CreateFontAsset(font);
                if (dyn != null && !primary.fallbackFontAssetTable.Contains(dyn))
                {
                    primary.fallbackFontAssetTable.Add(dyn);
                    TryAddChar(dyn, '\u2026');
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[UiFactory] runtime fallback '" + resourcesPath + "': " + ex.Message);
            }
        }

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            Stretch(rt);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = WhiteSprite;
            img.color = color;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            return rt;
        }

        public static UnityEngine.UI.Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = WhiteSprite;
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            var font = DefaultFont;
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = PhoneUITheme.Active.DefaultLabel;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        public static Button Button(string name, Transform parent, Color bg)
        {
            var img = MakeImage(name, parent, bg);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.1f);
            btn.colors = colors;
            return btn;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        static Sprite _circleSprite;
        static Sprite _roundedSquareSprite;
        static Material _circleClipMaterial;

        /// <summary>Legacy stencil-mask sprites; not used by RoundedIcon since 0.2.2.</summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null) return _circleSprite;
                _circleSprite = BuildSoftMaskSprite(64, roundness: 1f);
                return _circleSprite;
            }
        }

        /// <summary>Legacy stencil-mask sprites; not used by RoundedIcon since 0.2.2.</summary>
        public static Sprite RoundedSquareSprite
        {
            get
            {
                if (_roundedSquareSprite != null) return _roundedSquareSprite;
                _roundedSquareSprite = BuildSoftMaskSprite(64, roundness: 0.28f);
                return _roundedSquareSprite;
            }
        }

        /// <summary>
        /// Shared circle-clip material (one instance) so long threads batch; looks like a circular crop.
        /// Prefers the Resources asset (survives player builds); runtime fallbacks use HideAndDontSave.
        /// </summary>
        public static Material CircleClipMaterial
        {
            get
            {
                if (_circleClipMaterial != null) return _circleClipMaterial;

                var shared = Resources.Load<Material>("PhoneUI/CircleClip");
                if (shared != null)
                {
                    _circleClipMaterial = shared;
                    _circleClipCreatedAtRuntime = false;
                    return _circleClipMaterial;
                }

                var shader = Shader.Find("AmberScreen/UI/CircleClip");
                if (shader == null)
                {
                    Debug.LogWarning("[UiFactory] AmberScreen/UI/CircleClip missing — using UI/Default");
                    shader = Shader.Find("UI/Default");
                }
                _circleClipMaterial = new Material(shader)
                {
                    name = "PhoneUI_CircleClip_Shared",
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (_circleClipMaterial.HasProperty("_CornerRadius"))
                    _circleClipMaterial.SetFloat("_CornerRadius", 0.5f);
                _circleClipCreatedAtRuntime = true;
                return _circleClipMaterial;
            }
        }

        /// <summary>
        /// Circle-cropped avatar via shared circle-clip material (no per-avatar stencil Mask).
        /// Returns the Image root; use <see cref="SetCircleAvatarContent"/> to swap the sprite.
        /// </summary>
        public static RectTransform CircleAvatar(string name, Transform parent, Sprite avatar, float size, Color fallback)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.material = CircleClipMaterial;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            go.AddComponent<CircleClipMeshModifier>();
            SetCircleAvatarContent(rt, avatar, fallback);
            return rt;
        }

        /// <summary>Update an existing <see cref="CircleAvatar"/> Image (root has the Image).</summary>
        public static void SetCircleAvatarContent(RectTransform root, Sprite avatar, Color fallback)
        {
            if (root == null) return;
            var img = root.GetComponent<Image>();
            if (img == null) return;
            if (avatar != null)
            {
                img.sprite = avatar;
                img.color = Color.white;
            }
            else
            {
                img.sprite = WhiteSprite;
                img.color = fallback;
            }
        }

        /// <summary>
        /// Shared rounded-icon clip material (AA corners via CircleClip + <c>_CornerRadius</c> 0.14).
        /// Prefers the Resources asset; runtime fallbacks use HideAndDontSave.
        /// </summary>
        public static Material RoundedIconClipMaterial
        {
            get
            {
                if (_roundedIconClipMaterial != null) return _roundedIconClipMaterial;

                var shared = Resources.Load<Material>("PhoneUI/RoundedIconClip");
                if (shared != null)
                {
                    _roundedIconClipMaterial = shared;
                    _roundedIconClipCreatedAtRuntime = false;
                    return _roundedIconClipMaterial;
                }

                var shader = Shader.Find("AmberScreen/UI/CircleClip");
                if (shader == null)
                {
                    Debug.LogWarning("[UiFactory] AmberScreen/UI/CircleClip missing — using UI/Default");
                    shader = Shader.Find("UI/Default");
                }
                _roundedIconClipMaterial = new Material(shader)
                {
                    name = "PhoneUI_RoundedIconClip_Shared",
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (_roundedIconClipMaterial.HasProperty("_CornerRadius"))
                    _roundedIconClipMaterial.SetFloat("_CornerRadius", 0.14f);
                _roundedIconClipCreatedAtRuntime = true;
                return _roundedIconClipMaterial;
            }
        }

        /// <summary>Rounded-square icon tile via AA clip shader (no stencil Mask).</summary>
        public static RectTransform RoundedIcon(string name, Transform parent, Sprite icon, float size, Color fallback)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.material = RoundedIconClipMaterial;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            go.AddComponent<CircleClipMeshModifier>();
            if (icon != null)
            {
                img.sprite = icon;
                img.color = Color.white;
            }
            else
            {
                img.sprite = WhiteSprite;
                img.color = fallback;
            }
            return rt;
        }

        /// <summary>Legacy stencil-mask sprites; not used by RoundedIcon since 0.2.2.</summary>
        static RectTransform MaskedImage(
            string name, Transform parent, Sprite content, float w, float h, Sprite maskSprite, Color fallback)
        {
            var maskGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            var maskRt = maskGo.GetComponent<RectTransform>();
            maskRt.SetParent(parent, false);
            maskRt.sizeDelta = new Vector2(w, h);
            var maskImg = maskGo.GetComponent<Image>();
            maskImg.sprite = maskSprite;
            maskImg.color = Color.white;
            maskImg.raycastTarget = false;
            var mask = maskGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var inner = MakeImage("Img", maskRt, content != null ? Color.white : fallback);
            Stretch(inner.rectTransform);
            if (content != null)
            {
                inner.sprite = content;
                inner.preserveAspect = true;
                inner.type = Image.Type.Simple;
            }
            inner.raycastTarget = false;
            return maskRt;
        }

        static Sprite BuildSoftMaskSprite(int size, float roundness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float r = size * 0.5f;
            float corner = Mathf.Lerp(0f, r, Mathf.Clamp01(roundness));
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float a;
                if (roundness >= 0.99f)
                {
                    float dx = px - r;
                    float dy = py - r;
                    a = dx * dx + dy * dy <= (r - 0.5f) * (r - 0.5f) ? 1f : 0f;
                }
                else
                {
                    float cx = Mathf.Clamp(px, corner, size - corner);
                    float cy = Mathf.Clamp(py, corner, size - corner);
                    float dx = px - cx;
                    float dy = py - cy;
                    a = dx * dx + dy * dy <= corner * corner + 0.01f ? 1f : 0f;
                    if (corner < 0.5f) a = 1f;
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static void SetTopBar(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        public static void SetBottomBar(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Deactivate + detach before Destroy so RectMask2D / ClipperRegistry
        /// do not touch destroyed TMP/Graphics mid-frame.
        /// </summary>
        public static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.transform.SetParent(null, false);
            if (!Application.isPlaying)
                UnityEngine.Object.DestroyImmediate(go);
            else
                UnityEngine.Object.Destroy(go);
        }
    }
}
