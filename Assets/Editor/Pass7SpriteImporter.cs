// Pass 7 — Editor utility to configure environment PNGs as proper
// Unity Sprites with point filtering, no compression, and atlas
// slicing where applicable. Run via menu after PNG export. Idempotent.
using UnityEditor;
using UnityEngine;

public static class Pass7SpriteImporter
{
    [MenuItem("Caves Of Ooo/Tools/Pass 7 — Configure Environment Sprites")]
    public static void Apply()
    {
        ConfigureSprite("Assets/Sprites/Environment/wall_atlas.png",
            multipleSlice: true, sliceWidth: 16, sliceHeight: 16);
        ConfigureSprite("Assets/Sprites/Environment/floor_atlas.png",
            multipleSlice: true, sliceWidth: 16, sliceHeight: 16);
        ConfigureSprite("Assets/Sprites/Environment/water_tile.png",
            multipleSlice: false, sliceWidth: 0, sliceHeight: 0);
        ConfigureSprite("Assets/Sprites/Environment/door_closed.png",
            multipleSlice: false, sliceWidth: 0, sliceHeight: 0);
        ConfigureSprite("Assets/Sprites/Environment/door_open.png",
            multipleSlice: false, sliceWidth: 0, sliceHeight: 0);
        AssetDatabase.Refresh();
        Debug.Log("[Pass7SpriteImporter] Done. 5 env sprites configured.");
    }

    private static void ConfigureSprite(string path,
        bool multipleSlice, int sliceWidth, int sliceHeight)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp == null)
        {
            Debug.LogWarning($"[Pass7SpriteImporter] Texture not found: {path}");
            return;
        }
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = 16f;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;

        if (multipleSlice)
        {
            imp.spriteImportMode = SpriteImportMode.Multiple;
            // Manually generate sprite-sheet metadata for the atlas.
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                imp.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            if (tex != null)
            {
                int cols = tex.width / sliceWidth;
                int rows = tex.height / sliceHeight;
                var rects = new System.Collections.Generic.List<SpriteMetaData>();
                int idx = 0;
                // y goes top-to-bottom in image; sprite sheet y is bottom-up.
                // Use top-row-first naming convention so atlas[0]=top-left.
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        var meta = new SpriteMetaData
                        {
                            name = System.IO.Path.GetFileNameWithoutExtension(path)
                                + "_" + idx.ToString("D2"),
                            rect = new Rect(
                                col * sliceWidth,
                                tex.height - (row + 1) * sliceHeight,
                                sliceWidth,
                                sliceHeight),
                            alignment = (int)SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f),
                        };
                        rects.Add(meta);
                        idx++;
                    }
                }
#pragma warning disable CS0618 // SpritesheetData deprecation; still functional
                imp.spritesheet = rects.ToArray();
#pragma warning restore CS0618
            }
        }
        else
        {
            imp.spriteImportMode = SpriteImportMode.Single;
        }

        imp.SaveAndReimport();
        Debug.Log($"[Pass7SpriteImporter] Configured: {path}");
    }
}
