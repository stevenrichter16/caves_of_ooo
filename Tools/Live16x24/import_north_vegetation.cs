// Editor-only authoring snippet for Unity MCP code execute. Does not change scenes
// or simulation state. Renderer creates cached 16x24 sprites from this one texture.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("Stop Play before importing the north vegetation atlas.");
string source = "ArtSource/Live16x24/north-vegetation/north-vegetation.png";
string path = "Assets/Resources/Sprites/Live16x24/north-vegetation.png";
if (!System.IO.File.Exists(source)) throw new System.IO.FileNotFoundException(source);
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
System.IO.File.Copy(source, path, true);
UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType = UnityEditor.TextureImporterType.Default;
importer.filterMode = UnityEngine.FilterMode.Point;
importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
importer.mipmapEnabled = false;
importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
importer.maxTextureSize = 256;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.alphaIsTransparency = true;
importer.sRGBTexture = true;
importer.isReadable = false;
foreach (string platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
    importer.ClearPlatformTextureSettings(platform);
importer.SaveAndReimport();
var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
if (texture == null || texture.width != 64 || texture.height != 24
    || texture.filterMode != UnityEngine.FilterMode.Point || texture.mipmapCount != 1)
    throw new System.InvalidOperationException("Vegetation atlas did not retain its native import contract.");
return new { path, width = texture.width, height = texture.height,
    filter = texture.filterMode.ToString(), mipmaps = texture.mipmapCount,
    compression = importer.textureCompression.ToString() };
