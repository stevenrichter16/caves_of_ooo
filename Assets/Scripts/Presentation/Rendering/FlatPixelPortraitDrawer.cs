using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Draws borrowed flat-world artwork with one sRGB texture decode
    /// and normal target encoding. The legacy IMGUI texture material brightens
    /// this palette a second time on the Linear project’s game framebuffer.</summary>
    public sealed class FlatPixelPortraitDrawer : IDisposable
    {
        const string ShaderPath = "FlatPixelPortrait";
        static readonly int TintId = Shader.PropertyToID("_Tint");
        bool disposed;
        internal Material OwnedMaterial { get; private set; }

        /// <summary>Called only during popup repaint. No texture copy, capture,
        /// readback or global color-write state change occurs here.</summary>
        public bool Draw(Rect rect, ExaminePortrait portrait)
        {
            if (disposed || portrait.IsModel || portrait.Texture == null
                || !Valid(rect) || !Valid(portrait.Uv)) return false;
            if (OwnedMaterial == null)
            {
                var shader = Resources.Load<Shader>(ShaderPath);
                if (shader == null || !shader.isSupported) return false;
                OwnedMaterial = new Material(shader)
                    { name = "Flat pixel examine portrait", hideFlags = HideFlags.HideAndDontSave };
            }
            // SetVector avoids any implicit material color-property conversion.
            // The white native atlas tint remains white; procedural palette
            // tints enter the same linear space as the sampled sRGB texture.
            Color tint = QualitySettings.activeColorSpace == ColorSpace.Linear ? portrait.Tint.linear : portrait.Tint;
            OwnedMaterial.SetVector(TintId, new Vector4(tint.r, tint.g, tint.b, tint.a));
            // Graphics.DrawTexture's historical vertex-color scaling is not
            // used by this shader. Only the explicit tint uniform multiplies it.
            Graphics.DrawTexture(rect, portrait.Texture, portrait.Uv, 0, 0, 0, 0, Color.white, OwnedMaterial, 0);
            return true;
        }

        static bool Valid(Rect rect) => rect.width > 0 && rect.height > 0
            && Finite(rect.x) && Finite(rect.y) && Finite(rect.width) && Finite(rect.height);
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (OwnedMaterial != null)
            {
                if (Application.isPlaying) Object.Destroy(OwnedMaterial);
                else Object.DestroyImmediate(OwnedMaterial);
            }
            OwnedMaterial = null;
        }
    }
}
