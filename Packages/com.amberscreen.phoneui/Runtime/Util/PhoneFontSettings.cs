using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Configurable TMP fallback chain for PhoneUI (checkmark + emoji glyphs).
    /// Assign assets in Resources/PhoneUI/PhoneFontSettings or rely on runtime CreateFontAsset.
    /// </summary>
    [CreateAssetMenu(menuName = "Amber Screen/Phone UI/Font Settings", fileName = "PhoneFontSettings")]
    public sealed class PhoneFontSettings : ScriptableObject
    {
        [Tooltip("Primary UI font (defaults to TMP Settings / LiberationSans SDF).")]
        public TMP_FontAsset Primary;

        [Tooltip("Fallback fonts for ✓ and emoji (Noto Sans Symbols 2, Noto Emoji, etc.).")]
        public List<TMP_FontAsset> Fallbacks = new List<TMP_FontAsset>();

        [Tooltip("TrueType/OTF fonts to build dynamic TMP fallbacks from at runtime if Fallbacks is empty.")]
        public List<Font> RuntimeFallbackSourceFonts = new List<Font>();
    }
}
