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

            // Wider than 16:9: shrink into the right margin when it still fits hudMinWidth.
            Rect game = ComputeGameRectPixels();
            float aspect = safe.width / Mathf.Max(safe.height, 1f);
            if (aspect > SixteenNine + 0.001f)
            {
                float marginCu = (safe.xMax - game.xMax) / scale - settings.railEdgeMargin;
                if (marginCu >= settings.hudMinWidth)
                    width = Mathf.Min(width, marginCu);
            }

            return width;
        }

        /// <summary>
        /// Canvas units to reserve on the right for the HUD when visible.
        /// Zero when HUD is hidden or has no lines.
        /// </summary>
        public static float HudReserveCanvasUnits(DevConsoleSettings settings, float scale)
        {
            if (!DevHud.Visible || DevHudRegistry.TotalLineCount() == 0)
                return 0f;
            return HudWidthCanvasUnits(settings, scale) + settings.railEdgeMargin;
        }

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
