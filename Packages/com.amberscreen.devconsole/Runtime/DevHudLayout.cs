#if AMBERSCREEN_DEVCONSOLE
using UnityEngine;

namespace AmberScreen.Dev
{
    /// <summary>Shared GameRect / HUD width maths (screen pixels and canvas units).</summary>
    static class DevHudLayout
    {
        const float SixteenNine = 16f / 9f;

        public static Rect LayoutSafeArea =>
            DevConsoleView.s_LayoutScreenWidth > 0
                ? new Rect(0f, 0f, DevConsoleView.s_LayoutScreenWidth, DevConsoleView.s_LayoutScreenHeight)
                : Screen.safeArea;

        public static int ScreenW => DevConsoleView.LayoutScreenWidth;
        public static int ScreenH => DevConsoleView.LayoutScreenHeight;

        public static bool IsPortrait => ScreenH > ScreenW;

        public static Rect ComputeGameRectPixels()
        {
            Rect safe = LayoutSafeArea;
            float h = Mathf.Max(safe.height, 1f);
            float aspect = safe.width / h;
            if (aspect <= SixteenNine + 0.001f)
                return safe;

            float gameW = h * SixteenNine;
            float x = safe.x + (safe.width - gameW) * 0.5f;
            return new Rect(x, safe.y, gameW, safe.height);
        }

        public static float HudWidthCanvasUnits(DevConsoleSettings settings, float scale)
        {
            scale = Mathf.Max(scale, 0.01f);
            Rect safe = LayoutSafeArea;
            float canvasW = safe.width / scale;
            float width = Mathf.Clamp(
                canvasW * settings.hudScreenFraction,
                settings.hudMinWidth,
                settings.hudMaxWidth);

            // Wider than 16:9: shrink into the left margin when it still fits hudMinWidth.
            Rect game = ComputeGameRectPixels();
            float aspect = safe.width / Mathf.Max(safe.height, 1f);
            if (aspect > SixteenNine + 0.001f)
            {
                float marginCu = (game.xMin - safe.xMin) / scale - settings.railEdgeMargin;
                if (marginCu >= settings.hudMinWidth)
                    width = Mathf.Min(width, marginCu);
            }

            return width;
        }

        public static float HudStripHeightCanvasUnits(DevConsoleSettings settings, float scale)
        {
            scale = Mathf.Max(scale, 0.01f);
            Rect safe = LayoutSafeArea;
            float canvasH = safe.height / scale;
            return canvasH * Mathf.Clamp(settings.hudPortraitHeightFraction, 0.08f, 0.4f);
        }

        /// <summary>
        /// Landscape left reserve for the HUD (width + margin). Zero in portrait or when hidden/empty.
        /// </summary>
        public static float HudLeftReserveCanvasUnits(DevConsoleSettings settings, float scale)
        {
            if (!DevHud.Visible || !HasHudContent() || IsPortrait)
                return 0f;
            return HudWidthCanvasUnits(settings, scale) + settings.railEdgeMargin;
        }

        /// <summary>
        /// Portrait top reserve for the HUD strip (height + margin). Zero in landscape or when hidden/empty.
        /// </summary>
        public static float HudTopReserveCanvasUnits(DevConsoleSettings settings, float scale)
        {
            if (!DevHud.Visible || !HasHudContent() || !IsPortrait)
                return 0f;
            return HudStripHeightCanvasUnits(settings, scale) + settings.railEdgeMargin;
        }

        static bool HasHudContent() =>
            DevHudRegistry.TotalLineCount() > 0 || DevHudKeyHints.TryGetActiveHints(out _);

        public static float SharedUiScaleFactor(DevConsoleSettings settings)
        {
            if (DevConsoleUiUtil.IsPortraitScaledLayout(settings))
                return DevConsoleUiUtil.PortraitScaleFactor(settings);
            if (DevConsoleView.s_LayoutScreenWidth > 0)
                return 1f;
            return DevConsoleUiUtil.LandscapeScaleFactor(settings);
        }
    }
}
#endif
