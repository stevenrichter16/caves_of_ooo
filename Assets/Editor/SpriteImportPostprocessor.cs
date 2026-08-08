using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.EditorTools
{
    /// <summary>
    /// PASS 15 R4 — pixel-perfect import settings enforced by CODE, not
    /// convention (adapted from the farming project's
    /// ArtImportPostprocessor, the ~60 lines that prevent the entire
    /// "why is my pixel art blurry/banded" bug class).
    ///
    /// <para>Before this, 24 of the 52 sprite metas (the May-era
    /// auto-generated ones — including the wall and floor atlases that
    /// cover the most screen area) carried per-platform DXT/ETC
    /// compression overrides: guaranteed color banding on 16×16 art in
    /// any build. This preprocessor runs BEFORE import, so every sprite
    /// under <see cref="SpriteRoot"/> is correct from its first import
    /// and can never regress.</para>
    ///
    /// <para>Deliberately NOT forced: <c>spriteImportMode</c> — the two
    /// atlases (wall_atlas, floor_atlas) are Multiple with authored
    /// sub-rects; forcing Single would destroy them.</para>
    /// </summary>
    public class SpriteImportPostprocessor : AssetPostprocessor
    {
        private const string SpriteRoot = "Assets/Resources/Sprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;

            // Kill the stale per-platform compression overrides.
            importer.ClearPlatformTextureSettings("Standalone");
            importer.ClearPlatformTextureSettings("WebGL");
            importer.ClearPlatformTextureSettings("Android");
            importer.ClearPlatformTextureSettings("iPhone");

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // FullRect prevents Unity's tight mesh from clipping edge
            // pixels; extrude 0 keeps pixel rects exact.
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}
