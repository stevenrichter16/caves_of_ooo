using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Wild fauna, camp Wardens and ordinary lit hearths outside the opening (2026-10-30): Magpie and Viper use their approved north-works bodies
    /// (frames 5 and 6 of 9); GlassScorpion, RuneCultist, Rotling and the Cactus use the shared 16x16
    /// environment sprites (the accepted Merchant/Elder format). Only canonical, visible, living, untinted,
    /// single-cell owners; never inside opening-admitted zones, whose own routes stay unchanged.
    /// Names, behaviour and belongings remain simulation-owned.
    /// </summary>
    public sealed class FlatPixelWildFaunaCatalog : IDisposable
    {
        readonly Tile magpie, viper, scorpion, cultist, rotling, cactus, hearth, warden;
        bool disposed;

        public FlatPixelWildFaunaCatalog()
            : this(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath),
                Resources.Load<Texture2D>("Sprites/Environment/glass_scorpion"), Resources.Load<Texture2D>("Sprites/Environment/rune_cultist"),
                Resources.Load<Texture2D>("Sprites/Environment/rotling"), Resources.Load<Texture2D>("Sprites/Environment/cactus"),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.StatesResourcePath), Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath)) { }

        /// <summary>Each group is independent; a missing or malformed texture leaves its role on the glyph.</summary>
        public FlatPixelWildFaunaCatalog(Texture2D works9, Texture2D scorpion16, Texture2D cultist16, Texture2D rotling16, Texture2D cactus16)
            : this(works9, scorpion16, cultist16, rotling16, cactus16, null, null) { }

        /// <summary>Also the ordinary lit hearth (opening-states burning frame 5 of 12) and the camp Warden
        /// (opening-base Warden frame 9 of 10).</summary>
        public FlatPixelWildFaunaCatalog(Texture2D works9, Texture2D scorpion16, Texture2D cultist16, Texture2D rotling16, Texture2D cactus16,
            Texture2D states12, Texture2D base10)
        {
            magpie = Build(works9, 144, 24, 80, "magpie");
            viper = Build(works9, 144, 24, 96, "viper");
            scorpion = Build(scorpion16, 16, 16, 0, "glass scorpion");
            cultist = Build(cultist16, 16, 16, 0, "rune cultist");
            rotling = Build(rotling16, 16, 16, 0, "rotling");
            cactus = Build(cactus16, 16, 16, 0, "cactus");
            hearth = Build(states12, 192, 24, 80, "hearth");
            warden = Build(base10, 160, 24, 144, "camp warden");
        }

        public static bool AppliesTo(Zone zone) => zone?.ZoneID != null && !FlatPixelGreyboxCatalog.IsAdmitted(zone);

        public static bool HasKnownRole(Entity owner) => Expected(owner?.BlueprintName, out _, out _);
        static bool Expected(string role, out string glyph, out string color)
        {
            switch (role)
            {
                case "Magpie": glyph = "b"; color = "&K"; return true;
                case "Viper": glyph = "~"; color = "&g"; return true;
                case "GlassScorpion": glyph = "s"; color = "&W"; return true;
                case "RuneCultist": glyph = "c"; color = "&M"; return true;
                case "Rotling": glyph = "r"; color = "&g"; return true;
                case "Cactus": glyph = "t"; color = "&g"; return true;
                case "Campfire": glyph = "*"; color = "&R"; return true;
                case "Warden": glyph = "@"; color = "&r"; return true;
                default: glyph = color = null; return false;
            }
        }

        public bool TryResolve(Zone zone, Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !AppliesTo(zone) || owner == null || owner.SpatialZone != zone || !Expected(owner.BlueprintName, out var glyph, out var color)) return false;
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            if (r == null || !r.Visible || r.RenderString != glyph || r.ColorString != color
                || !string.IsNullOrEmpty(r.BackgroundColor) || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                || !string.IsNullOrEmpty(r.GlyphVariants) || p == null || p.Takeable || p.InInventory != null || p.Equipped != null
                || owner.HasTag("Player") || owner.HasTag("Item") || owner.HasTag("Corpse")) return false;
            if (owner.HasPart<StatusEffectsPart>())
            { var e = Owned<StatusEffectsPart>(owner); if (e == null || e.GetRenderColorOverride() != null) return false; }
            if (owner.HasPart<SpatialFootprintPart>())
            { var f = Owned<SpatialFootprintPart>(owner); if (f == null || f.Offsets.Length != 1 || f.Offsets[0] != Vector2Int.zero) return false; }
            if (owner.BlueprintName == "Campfire")
            { tile = FlatPixelSettlementGroundCatalog.IsOrdinaryHearth(owner) ? hearth : null; return tile != null; }
            if (owner.BlueprintName == "Cactus")
            {
                // A plain cactus prop: no harvest, container, state or creature meaning.
                if (owner.HasTag("Creature") || owner.HasPart<HarvestablePart>() || owner.HasPart<ContainerPart>()
                    || owner.HasPart<LiquidPoolPart>() || owner.HasPart<TileStateSourcePart>() || owner.HasPart<DoorPart>()) return false;
                tile = cactus; return tile != null;
            }
            var hp = owner.GetStat("Hitpoints");
            if (!owner.HasTag("Creature") || Owned<Body>(owner) == null || Owned<BrainPart>(owner) == null
                || hp == null || hp.Owner != owner || hp.BaseValue <= 0) return false;
            switch (owner.BlueprintName)
            {
                case "Magpie": tile = magpie; break;
                case "Viper": tile = viper; break;
                case "GlassScorpion": tile = scorpion; break;
                case "RuneCultist": tile = cultist; break;
                case "Rotling": tile = rotling; break;
                case "Warden":
                    // The same Warden blueprint and approved body as the opening's; conversation required.
                    tile = Owned<ConversationPart>(owner) != null ? warden : null; break;
            }
            return tile != null;
        }

        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part) { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static Tile Build(Texture2D texture, int width, int height, int x, string label)
        {
            if (texture == null || texture.width != width || texture.height != height) return null;
            var sprite = Sprite.Create(texture, new Rect(x, 0, 16, height), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 wild " + label; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
            tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (var t in new[] { magpie, viper, scorpion, cultist, rotling, cactus, hearth, warden })
            { if (t == null) continue; DestroyOwned(t.sprite); DestroyOwned(t); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
