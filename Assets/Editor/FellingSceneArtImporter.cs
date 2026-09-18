using System;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.EditorTools
{
    /// <summary>Native scene color assets retain exact dimensions/RGB and a readable
    /// alpha mask. This scope intentionally bypasses the ordinary 16 PPU sprite policy.</summary>
    public sealed class FellingSceneArtImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Resources/SceneArt/FellingSite/Art/";
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.Ordinal)
                || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.isReadable = true;
            importer.ClearPlatformTextureSettings("Standalone");
            importer.ClearPlatformTextureSettings("WebGL");
            importer.ClearPlatformTextureSettings("Android");
            importer.ClearPlatformTextureSettings("iPhone");
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = Vector2.zero;
            importer.SetTextureSettings(settings);
        }
    }
}
