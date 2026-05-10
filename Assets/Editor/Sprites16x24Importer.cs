// Editor utility: configure every PNG under Assets/coo_16x24/ as a Unity
// Sprite with the right import settings for the 16×24 creature layer.
//
// What we want per PNG:
//   - TextureType = Sprite (Single)
//   - PixelsPerUnit = 16   → 16×24 pixels = 1.0w × 1.5h world units
//   - Pivot = (0.5, 0.0)   → bottom-center, so the sprite anchors at the
//     cell's bottom and overflows the cell above (Qud aesthetic)
//   - FilterMode = Point   → crisp pixels, no bilinear blur
//   - Compression = Uncompressed
//   - alphaIsTransparency  → on
//
// Idempotent — safe to re-run after adding new PNGs.
using UnityEditor;
using UnityEngine;

public static class Sprites16x24Importer
{
    private const string ROOT = "Assets/coo_16x24";

    [MenuItem("Caves Of Ooo/Tools/16x24 — Configure Creature Sprites")]
    public static void Apply()
    {
        if (!System.IO.Directory.Exists(ROOT))
        {
            Debug.LogWarning($"[Sprites16x24Importer] {ROOT} not found.");
            return;
        }

        int ok = 0, skipped = 0;
        foreach (var path in System.IO.Directory.EnumerateFiles(ROOT, "*.png",
            System.IO.SearchOption.AllDirectories))
        {
            // AssetDatabase paths are forward-slashed and project-relative.
            string assetPath = path.Replace('\\', '/');
            int idx = assetPath.IndexOf("Assets/");
            if (idx > 0) assetPath = assetPath.Substring(idx);

            if (Configure(assetPath)) ok++; else skipped++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Sprites16x24Importer] Done. configured={ok} skipped={skipped}");
    }

    private static bool Configure(string assetPath)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (imp == null)
        {
            Debug.LogWarning($"[Sprites16x24Importer] Texture not found: {assetPath}");
            return false;
        }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 16f;

        // Pivot lives on TextureImporterSettings, not directly on
        // TextureImporter. Round-trip through the settings struct.
        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        settings.spritePivot = new Vector2(0.5f, 0.0f);
        imp.SetTextureSettings(settings);

        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        return true;
    }
}
