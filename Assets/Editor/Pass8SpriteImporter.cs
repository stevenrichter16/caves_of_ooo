// Pass 8 — Editor utility to configure 15 NEW environment PNGs as
// proper Unity Sprites: point filtering, no compression, single
// sprite (no atlas slicing — each PNG is one tile). Idempotent.
using UnityEditor;
using UnityEngine;

public static class Pass8SpriteImporter
{
    private static readonly string[] Pass8Pngs = new[]
    {
        "Assets/Sprites/Environment/stalagmite.png",
        "Assets/Sprites/Environment/boulder.png",
        "Assets/Sprites/Environment/stalactite.png",
        "Assets/Sprites/Environment/bush.png",
        "Assets/Sprites/Environment/cactus.png",
        "Assets/Sprites/Environment/tree.png",
        "Assets/Sprites/Environment/campfire.png",
        "Assets/Sprites/Environment/shrine.png",
        "Assets/Sprites/Environment/stairs_down.png",
        "Assets/Sprites/Environment/stairs_up.png",
        "Assets/Sprites/Environment/bones.png",
        "Assets/Sprites/Environment/barrel.png",
        "Assets/Sprites/Environment/mushroom.png",
        "Assets/Sprites/Environment/gold_pile.png",
        "Assets/Sprites/Environment/chair.png",
        // Pass 10 — per-blueprint disambiguation sprites
        "Assets/Sprites/Environment/chest.png",
        "Assets/Sprites/Environment/lantern.png",
    };

    [MenuItem("Caves Of Ooo/Tools/Pass 8 — Configure Sprite Expansion")]
    public static void Apply()
    {
        int ok = 0, missing = 0;
        foreach (var path in Pass8Pngs)
        {
            if (Configure(path)) ok++; else missing++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Pass8SpriteImporter] Done. configured={ok} missing={missing}");
    }

    private static bool Configure(string path)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp == null)
        {
            Debug.LogWarning($"[Pass8SpriteImporter] Texture not found: {path}");
            return false;
        }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 16f;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        return true;
    }
}
