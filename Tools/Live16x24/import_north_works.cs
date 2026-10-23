// Editor-only authoring snippet for Unity MCP code execute. Does not change scenes
// or simulation state. Renderer creates cached 16x24 sprites from this one texture.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("Stop Play before importing the north works atlas.");
string source = "ArtSource/Live16x24/north-works/north-works.png";
string path = "Assets/Resources/Sprites/Live16x24/north-works.png";
if (!System.IO.File.Exists(source)) throw new System.IO.FileNotFoundException(source);
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
System.IO.File.Copy(source, path, true);
UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
// The project's SpriteImportPostprocessor may enforce Sprite/16PPU/2048max
// during reimport. Runtime borrows the Texture2D and creates its own full-cell
// rects; validate actual size/filter below rather than relying on sub-sprites.
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
if (texture == null || texture.width != 144 || texture.height != 24
    || texture.filterMode != UnityEngine.FilterMode.Point || texture.mipmapCount != 1)
    throw new System.InvalidOperationException("Works atlas did not retain its native import contract.");
return new { path, width = texture.width, height = texture.height,
    filter = texture.filterMode.ToString(), mipmaps = texture.mipmapCount,
    compression = importer.textureCompression.ToString() };
