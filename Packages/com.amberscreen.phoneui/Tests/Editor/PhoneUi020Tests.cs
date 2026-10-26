using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AmberScreen.PhoneUI.Tests
{
    public sealed class PhoneUi020Tests
    {
        [Test]
        public void Theme_Builtin_MatchesPriorHardCodedPlayerBubble()
        {
            var t = PhoneUITheme.Builtin;
            Assert.That(t.PlayerBubble.r, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(t.PlayerBubble.g, Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(t.PlayerBubble.b, Is.EqualTo(0.95f).Within(0.001f));
            Assert.That(t.ImageViewerDim.a, Is.EqualTo(0.96f).Within(0.001f));
            Assert.That(t.MessagingRoot.r, Is.EqualTo(0.97f).Within(0.001f));
            Assert.That(t.HomeBg.r, Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void Theme_CreateInstance_Reset_FillsNonZeroColours()
        {
            var t = ScriptableObject.CreateInstance<PhoneUITheme>();
            // Zero out to simulate a Create-menu SO before Reset (Unity 6 may also call Reset on CreateInstance).
            t.PlayerBubble = default;
            t.HomeBg = default;
            t.MessagingRoot = default;
            t.ImageViewerDim = default;
            t.StatusBarBg = default;
            t.NavBar = default;
            Assert.That(t.PlayerBubble.a, Is.EqualTo(0f).Within(0.001f));

            var reset = typeof(PhoneUITheme).GetMethod(
                "Reset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(reset, Is.Not.Null, "PhoneUITheme.Reset must exist for Create Asset menu");
            reset.Invoke(t, null);

            Assert.That(t.PlayerBubble.a, Is.GreaterThan(0.5f));
            Assert.That(t.PlayerBubble.r + t.PlayerBubble.g + t.PlayerBubble.b, Is.GreaterThan(0.1f));
            Assert.That(t.HomeBg.a, Is.GreaterThan(0.5f));
            Assert.That(t.MessagingRoot.a, Is.GreaterThan(0.5f));
            Assert.That(t.ImageViewerDim.a, Is.GreaterThan(0.5f));
            Assert.That(t.StatusBarBg.a, Is.GreaterThan(0.5f));
            Assert.That(t.NavBar.a, Is.GreaterThan(0.5f));
            Object.DestroyImmediate(t);
        }

        [Test]
        public void SpriteUvRect_FullTexture_IsUnitRect()
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            var uv = ChatImageAttachment.SpriteUvRect(sprite);
            Assert.That(uv.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(uv.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(uv.width, Is.EqualTo(1f).Within(0.001f));
            Assert.That(uv.height, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void ImageViewer_Show_CreatesRoot_WithEscCloser()
        {
            var canvasGo = new GameObject("C", typeof(RectTransform), typeof(Canvas));
            var host = canvasGo.GetComponent<RectTransform>();
            var tex = new Texture2D(32, 48, TextureFormat.RGBA32, false);
            tex.Apply();
            var att = new ChatImageAttachment { FileName = "t.png", Texture = tex, Aspect = 32f / 48f };
            var viewer = ImageMessageViewer.Show(host, att, "t.png");
            Assert.That(viewer, Is.Not.Null);
            var closer = viewer.GetComponent<ImageViewerEscCloser>();
            Assert.That(closer, Is.Not.Null);
            Assert.That(closer.ListenForEscape, Is.True);
            Object.DestroyImmediate(viewer);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void ImageViewer_WhenMessagingOwnsBack_ListenForEscapeIsFalse()
        {
            var canvasGo = new GameObject("C", typeof(RectTransform), typeof(Canvas));
            var host = canvasGo.GetComponent<RectTransform>();
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.Apply();
            var att = new ChatImageAttachment { FileName = "x.png", Texture = tex, Aspect = 1f };
            var viewer = ImageMessageViewer.Show(host, att, "x.png", null, listenForEscape: false);
            Assert.That(viewer, Is.Not.Null);
            var closer = viewer.GetComponent<ImageViewerEscCloser>();
            Assert.That(closer.ListenForEscape, Is.False);
            Object.DestroyImmediate(viewer);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void ImageViewerEscCloser_Close_RecordsFrame_ForTryHandleBack()
        {
            var go = new GameObject("V", typeof(ImageViewerEscCloser));
            var closer = go.GetComponent<ImageViewerEscCloser>();
            closer.ListenForEscape = false;
            int before = ImageViewerEscCloser.LastClosedFrame;
            closer.Close();
            Assert.That(ImageViewerEscCloser.LastClosedFrame, Is.GreaterThanOrEqualTo(before));
            Assert.That(ImageViewerEscCloser.LastClosedFrame, Is.EqualTo(Time.frameCount));
        }

        [Test]
        public void PackageInfo_NameVersionDependencies()
        {
            var info = PackageInfo.FindForAssembly(typeof(PhoneUITheme).Assembly);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.name, Is.EqualTo("com.amberscreen.phoneui"));
            Assert.That(info.version, Is.EqualTo("0.2.1"));
            Assert.That(info.dependencies, Has.Some.Matches<UnityEditor.PackageManager.DependencyInfo>(
                d => d.name == "com.unity.inputsystem"));
            Assert.That(info.dependencies, Has.Some.Matches<UnityEditor.PackageManager.DependencyInfo>(
                d => d.name == "com.unity.ugui"));
        }

        [Test]
        public void FontCache_Invalidates_OnThemeOverride()
        {
            var fontA = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            Assert.That(fontA, Is.Not.Null, "TMP Essential Resources (LiberationSans SDF) required");

            var settings = ScriptableObject.CreateInstance<PhoneFontSettings>();
            settings.Primary = fontA;
            var theme = ScriptableObject.CreateInstance<PhoneUITheme>();
            theme.FontSettings = settings;

            PhoneUITheme.SetOverride(theme);
            Assert.That(UiFactory.DefaultFont, Is.SameAs(fontA));

            PhoneUITheme.SetOverride(null);
            var fontField = typeof(UiFactory).GetField("_font", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(fontField, Is.Not.Null);
            Assert.That(fontField.GetValue(null), Is.Null, "SetOverride(null) must clear the font cache");

            Object.DestroyImmediate(theme);
            Object.DestroyImmediate(settings);
        }
    }
}
