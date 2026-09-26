using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>One-shot developer capture of the already rendered frame. A
    /// color-only owned target avoids the file screenshot path's depth load/store
    /// errors on the tested Metal editor, without changing any gameplay camera.</summary>
    public static class DensityNativeScreenshot
    {
        /// <summary>Call after WaitForEndOfFrame. Writes a full-resolution PNG,
        /// restores the previously active render target, and releases owned
        /// textures even when capture or file writing fails.</summary>
        public static void CaptureToFile(string path) => CaptureToFile(path, Screen.width, Screen.height,
            ScreenCapture.CaptureScreenshotIntoRenderTexture);

        // The callback seam allows real GPU readback/lifetime tests without
        // invoking a Game-view-only API from Edit mode.
        internal static void CaptureToFile(string path, int width, int height, Action<RenderTexture> capture)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Screenshot path is required.", nameof(path));
            if (capture == null) throw new ArgumentNullException(nameof(capture));
            if (width <= 0 || width > SystemInfo.maxTextureSize) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0 || height > SystemInfo.maxTextureSize) throw new ArgumentOutOfRangeException(nameof(height));
            var previous = RenderTexture.active;
            RenderTexture target = null;
            Texture2D pixels = null;
            try
            {
                target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    name = "Density screenshot target", hideFlags = HideFlags.HideAndDontSave,
                    antiAliasing = 1, memorylessMode = RenderTextureMemoryless.None,
                    useMipMap = false, autoGenerateMips = false
                };
                if (!target.Create()) throw new InvalidOperationException("Density screenshot target allocation failed.");
                capture(target);
                RenderTexture.active = target;
                pixels = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                    { name = "Density screenshot readback", hideFlags = HideFlags.HideAndDontSave };
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RestoreScreenRowOrder(pixels, SystemInfo.graphicsUVStartsAtTop);
                pixels.Apply(false, false);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Release(pixels);
                if (target != null) { target.Release(); Release(target); }
            }
        }

        // ScreenCapture's render-target copy follows the graphics backend's
        // vertical origin. PNG encoding expects Texture2D's bottom-origin rows.
        // Keep the bottom-origin path unchanged; do not special-case a platform.
        internal static void RestoreScreenRowOrder(Texture2D pixels, bool graphicsUVStartsAtTop)
        {
            if (!graphicsUVStartsAtTop) return;
            var colors = pixels.GetPixels32();
            int width = pixels.width, height = pixels.height;
            for (int y = 0; y < height / 2; y++)
                for (int x = 0; x < width; x++)
                {
                    int bottom = y * width + x, top = (height - 1 - y) * width + x;
                    var color = colors[bottom];
                    colors[bottom] = colors[top];
                    colors[top] = color;
                }
            pixels.SetPixels32(colors);
        }

        private static void Release(Object owned)
        {
            if (owned == null) return;
            if (Application.isPlaying) Object.Destroy(owned);
            else Object.DestroyImmediate(owned);
        }
    }
}
