using System;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.EditorTools
{
    /// <summary>Preserves original PNG dimensions and RGB while fitting all top-down rows.</summary>
    public sealed class MorrowfastSceneArtImporter : AssetPostprocessor
    {
        private const string ArtRoot="Assets/Resources/SceneArt/Morrowfast/Art/";
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(ArtRoot,StringComparison.Ordinal)||!assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=40.96f;
            importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.alphaIsTransparency=false;importer.sRGBTexture=true;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.isReadable=true;
            foreach(string platform in new[]{"Standalone","WebGL","Android","iPhone"})importer.ClearPlatformTextureSettings(platform);
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteExtrude=0;
            settings.spriteGenerateFallbackPhysicsShape=false;settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=Vector2.zero;importer.SetTextureSettings(settings);
        }
    }
}
