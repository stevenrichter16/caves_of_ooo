// Execute in the stopped Unity Editor after plot-product selector/resource RED.
// Import exact native source bytes; preserve all prior resources and importers.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("Stop Play before importing plot art.");
var sources = new[] { "ArtSource/Live16x24/plot-products/plot-products.png" };
var targets = new[] { "Assets/Resources/Sprites/Live16x24/plot-products.png" };
var widths = new[] { 128 };
var results = new System.Collections.Generic.List<object>();
for (int i = 0; i < sources.Length; i++)
{
    byte[] sourceBytes = System.IO.File.ReadAllBytes(sources[i]);
    System.IO.File.WriteAllBytes(targets[i], sourceBytes);
    UnityEditor.AssetDatabase.ImportAsset(targets[i], UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(targets[i]);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spritePixelsPerUnit = 16;
    importer.filterMode = UnityEngine.FilterMode.Point;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    importer.mipmapEnabled = false;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.alphaIsTransparency = true;
    importer.sRGBTexture = true;
    importer.isReadable = false;
    foreach (string platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
        importer.ClearPlatformTextureSettings(platform);
    importer.SaveAndReimport();
    // Existing SpriteImportPostprocessor owns sub-sprite settings; runtime
    // borrows Texture2D and makes exact full-cell, centered cached frames.
    var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(targets[i]);
    if (texture == null || texture.width != widths[i] || texture.height != 24
        || texture.filterMode != UnityEngine.FilterMode.Point || texture.mipmapCount != 1)
        throw new System.InvalidOperationException("Native import mismatch: " + targets[i]);
    using (var sha = System.Security.Cryptography.SHA256.Create())
    {
        string hash = System.BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(targets[i]))).Replace("-", "").ToLowerInvariant();
        string original = System.BitConverter.ToString(sha.ComputeHash(sourceBytes)).Replace("-", "").ToLowerInvariant();
        if (hash != original) throw new System.InvalidOperationException("Source bytes changed: " + targets[i]);
        results.Add(new { path = targets[i], width = texture.width, height = texture.height, sha256 = hash,
            filter = texture.filterMode.ToString(), mipmaps = texture.mipmapCount, compression = importer.textureCompression.ToString() });
    }
}
return results;
