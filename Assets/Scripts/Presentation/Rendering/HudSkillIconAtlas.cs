using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>Seven original static16px pictograms (six actual starter powers plus fallback).</summary>
    public static class HudSkillIconAtlas
    {
        static Texture2D atlas;
        static Tile[] tiles;
        static readonly string[] Pixels =
        {
            ".......................................ss.............sWWs...........sW..Ws.........sW.ss.Ws.......sW.sWWs.Ws......sW.sWWs.Ws.......sW.ss.Ws.........sW..Ws...........sWWs.............ss.......................................................................",
            ".........................................rr............roor..........rroYYor......rrrooYWWYor...rrrooooYWWYYor....rrrooYYYYYor.......rrooYYor..........rooor.............rr.....................................................................................",
            ".....r....r.........ror..ror........oYo..oYo........oYo..oYo.........r....r........pp......pp......pW..pp..Wp......pW..pp..Wp......pWp.pp.pWp......pWWppppWWp.......pWWppWWp.........pWWWWp...........pppp............pppp......................................",
            ".............................c.............cc.........bbbbcc.......bbbcCCCCc....bbbccCWWWWWcc.....bbbcCCCCCCcc......bbbcWWWWCCc...bbbcCCCCCCcc..bbbccCWWWWWcc......bbbcCCCCc..........bbbbcc...............cc................c..................................",
            "........YY.............YY.............YY.............YY.............YYYYYY.............YY.............YY.............YY.............YY.............YY.............YYYYYYYYYYYY...ssssYYssssssss.....YY.............YY...........................................",
            ".......C............C..C..C..........C.C.C........C...CCC...C......C...C...C........CCCCCCC..........CCWCC.......CCCCWWWWCCCC........CCWCC..........CCCCCCC........C...C...C......C...CCC...C........C.C.C..........C..C..C............C........................",
            ".....................vvvvvv........vvVVVVVVvv.....vVV......VVv...vV..pppppp..Vv..vV.pPPPPPPp.Vv..vVpPPPPPPPPpVv..vVpPssPPssPpVv..vVpPPPPPPPPpVv..vV.pPPssPPp.Vv...vV.pPPPPp.Vv.....vV.pppp.Vv.......vvVVVVvv..........vvvv......................................"
        };
        public static Tile GetTile(SkillHudIcon icon)
        {
            int index = (int)icon;
            if (index < 0 || index >= Pixels.Length) index = 0;
            if (atlas == null || tiles == null || tiles[index] == null || tiles[index].sprite == null) Build();
            return tiles[index];
        }
        static Color32 ColorFor(char c)
        {
            switch (c)
            {
                case 'r': return new Color32(177, 54, 32, 255);
                case 'o': return new Color32(247, 119, 35, 255);
                case 'Y': return new Color32(255, 225, 91, 255);
                case 'W': return new Color32(241, 253, 255, 255);
                case 'b': return new Color32(28, 99, 173, 255);
                case 'c': return new Color32(55, 184, 228, 255);
                case 'C': return new Color32(166, 238, 250, 255);
                case 'p': return new Color32(152, 118, 132, 255);
                case 'P': return new Color32(209, 179, 198, 255);
                case 'v': return new Color32(93, 61, 145, 255);
                case 'V': return new Color32(177, 129, 234, 255);
                case 's': return new Color32(94, 111, 118, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }
        static void Build()
        {
            Release();
            atlas = new Texture2D(Pixels.Length * 16, 16, TextureFormat.RGBA32, false)
                { name = "HudSkillIcons", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            tiles = new Tile[Pixels.Length];
            for (int i = 0; i < Pixels.Length; i++)
            {
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                    atlas.SetPixel(i * 16 + x, 15 - y, ColorFor(Pixels[i][y * 16 + x]));
                var sprite = Sprite.Create(atlas, new Rect(i * 16, 0, 16, 16), new Vector2(.5f, .5f), 16);
                sprite.name = "HudSkill_" + (SkillHudIcon)i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite; tile.color = Color.white;
                tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave; tiles[i] = tile;
            }
            atlas.Apply(false, false);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Release()
        {
            if (tiles != null) foreach (var tile in tiles) if (tile != null) { Destroy(tile.sprite); Destroy(tile); }
            Destroy(atlas); atlas = null; tiles = null;
        }
        static void Destroy(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
