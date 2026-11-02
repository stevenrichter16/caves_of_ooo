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

        /// <summary>P5 (Docs/UPPER-TIER-BESTIARY.md §3.5): the upper-tier creatures that were
        /// still glyphs, drawn with their shared 16x16 environment bodies; the new species use
        /// original editable sources (ArtSource/Live16x24/upper-bestiary). Elders wear their
        /// kind's body. Bloomed hosts' bodies already show the Bloom, so its tint is accepted.</summary>
        static readonly (string Blueprint, string Resource, string Glyph, string Color, bool Bloom)[] SharedBodies =
        {
            ("AncientGuardian", "ancient_guardian", "H", "&w", false), ("CanopyStrangler", "canopy_strangler", "C", "&G", false),
            ("DuneLurker", "dune_lurker", "d", "&y", false), ("StoneGolem", "stone_golem", "G", "&y", false),
            ("VaultSentinel", "vault_sentinel", "V", "&W", false), ("PaleStalker", "pale_stalker", "p", "&C", false),
            ("PaleMatriarch", "pale_stalker", "p", "&C", false), ("ObsidianBrute", "obsidian_brute", "O", "&m", false),
            ("PalimpsestEcho", "palimpsest_echo", "@", "&B", false), ("PrickleBrowGecko", "pricklebrow_gecko", "l", "&B", false),
            ("OldSkySari", "sky_sari", "E", "&W", false), ("Riverwarden", "riverwarden", "R", "&G", false),
            ("OldRiverwarden", "riverwarden", "R", "&G", false), ("SimaSteward", "sima_steward", "v", "&w", false),
            ("BloomedWarden", "bloomed_warden", "@", "&m", true), ("BloomedHound", "bloomed_hound", "b", "&m", true),
            // Glyph → pixel slice 1 (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md, bucket A): creatures and
            // people whose shared 16x16 bodies existed but were never admitted to the flat view.
            ("AmbushBandit", "ambush_bandit", "b", "&r", false), ("BrassHusk", "brass_husk", "H", "&y", false),
            ("CaveBat", "cave_bat", "b", "&K", false), ("CaveBear", "cave_bear", "B", "&y", false),
            ("CaveSlime", "cave_slime", "j", "&g", false), ("CharredHusk", "charred_husk", "H", "&r", false),
            ("DesertBandit", "desert_bandit", "h", "&y", false), ("DesertProwler", "desert_prowler", "D", "&W", false),
            ("GiantSpider", "giant_spider", "S", "&G", false), ("Glowmaw", "glowmaw", "O", "&Y", false),
            ("GroveLanternMoth", "grove_lantern_moth", "m", "&Y", false), ("HelmwoodFrog", "helmwood_frog", "f", "&y", false),
            ("IceWight", "ice_wight", "W", "&C", false), ("JungleApe", "jungle_ape", "A", "&G", false),
            ("JungleStalker", "jungle_stalker", "J", "&G", false), ("MarlbackBreacher", "marlback_breacher", "S", "&M", false),
            ("MarlbackTunnelguard", "marlback_tunnelguard", "s", "&R", false), ("MarlbackWallkeeper", "marlback_wallkeeper", "S", "&R", false),
            ("RuinScavenger", "ruin_scavenger", "r", "&K", false), ("SandWurm", "sand_wurm", "W", "&W", false),
            ("SkeletalSentry", "skeletal_sentry", "Z", "&w", false), ("SleepingTroll", "sleeping_troll", "T", "&g", false),
            ("SporeShambler", "spore_shambler", "f", "&g", false), ("Apothecary", "apothecary", "@", "&G", false),
            ("Arcanist", "arcanist", "@", "&M", false), ("Armorer", "armorer", "@", "&w", false),
            ("CatacombWarden", "catacomb_warden", "@", "&c", false), ("CaveHermit", "cave_hermit", "@", "&c", false),
            ("DesertHermit", "desert_hermit", "@", "&Y", false), ("EncasedElder", "encased_elder", "@", "&Y", false),
            ("FoundingListener", "founding_listener", "@", "&y", false), ("FoundingPlaqueTender", "founding_plaque_tender", "@", "&W", false),
            ("Grib", "grib", "G", "&G", false), ("JungleHermit", "jungle_hermit", "@", "&G", false),
            ("Marceline", "marceline", "M", "&K", false), ("Mogu", "mogu", "M", "&g", false),
            ("Nam", "nam", "N", "&y", false), ("PaleCurator", "pale_curator", "@", "&w", false),
            ("PlaqueTender", "plaque_tender", "@", "&W", false), ("Provisioner", "provisioner", "@", "&Y", false),
            ("RuinsHermit", "ruins_hermit", "@", "&W", false), ("SaccharineEnvoy", "saccharine_envoy", "@", "&Y", false),
            ("Sien", "sien", "S", "&W", false), ("Sopp", "sopp", "s", "&K", false),
            ("StillleafIndexer", "stillleaf_indexer", "@", "&W", false), ("StillleafSearcher", "stillleaf_searcher", "@", "&C", false),
            ("Undertaker", "undertaker", "U", "&K", false),
            // Slice 2 (bucket C): original bodies, ArtSource/Live16x24/glyph-slice2.
            ("CurationHalfSet", "curation_half_set", "h", "&g", false),
            ("DirtGnome", "dirt_gnome", "g", "&y", false),
            ("Furrowstalker", "furrowstalker", "f", "&y", false),
            ("MarlbackSoursprayer", "marlback_soursprayer", "g", "&G", false),
            ("MarlbackStormbinder", "marlback_stormbinder", "s", "&C", false),
            ("SoddenPassageGuard", "sodden_passage_guard", "@", "&w", false),
            ("SootGremlin", "soot_gremlin", "s", "&K", false),
            ("SpreadDitchMate", "spread_ditch_mate", "s", "&w", false),
            ("SpreadHurdleCutter", "spread_hurdle_cutter", "s", "&w", false),
            ("SpreadLatchcoil", "spread_latchcoil", "~", "&g", false),
            ("SunStriker", "sun_striker", "l", "&Y", false),
            ("Tatterjay", "tatterjay", "j", "&c", false),
            // Slice 3 (bucket C): original people, ArtSource/Live16x24/glyph-slice3.
            ("ConcordFactor", "concord_factor", "@", "&W", false),
            ("CurationIntakeFiler", "curation_intake_filer", "@", "&W", false),
            ("CurationJuniorIndexer", "curation_junior_indexer", "@", "&w", false),
            ("CurationSorter", "curation_sorter", "@", "&C", false),
            ("FilerClerk", "filer_clerk", "@", "&C", false),
            ("GantryRegistrar", "gantry_registrar", "@", "&w", false),
            ("PeatCutter", "peat_cutter", "@", "&w", false),
            ("RecensionScribe", "recension_scribe", "@", "&c", false),
            ("SaltMaster", "salt_master", "@", "&W", false),
            ("TentRightHost", "tent_right_host", "@", "&y", false),
            // Rank 1, sub-milestone 4: Morrowfast's eight residents (ArtSource/Live16x24/morrowfast-people).
            ("MorrowfastNemm", "morrowfast_nemm", "@", "&y", false),
            ("MorrowfastHesta", "morrowfast_hesta", "@", "&y", false),
            ("MorrowfastVennit", "morrowfast_vennit", "@", "&C", false),
            ("MorrowfastPell", "morrowfast_pell", "@", "&g", false),
            ("MorrowfastOrrit", "morrowfast_orrit", "@", "&g", false),
            ("MorrowfastSella", "morrowfast_sella", "@", "&g", false),
            ("MorrowfastFarra", "morrowfast_farra", "@", "&g", false),
            ("MorrowfastEdden", "morrowfast_edden", "@", "&g", false),
            // Slice 4 (bucket A): the Rooted, and fixed props whose 16x16 bodies existed unwired.
            ("TheRooted", "the_rooted", "&", "&W", false),
            ("HollowStump", "hollow_stump", "u", "&y", false),
            ("IceStalactite", "ice_stalactite", "|", "&C", false),
            ("LibraryChoirIronWall", "library_choir_iron_wall", "#", "&g", false),
            ("LibraryMemoryMarbleWall", "library_memory_marble_wall", "#", "&c", false),
            ("LibraryTepuiboneWall", "library_tepuibone_wall", "#", "&Y", false),
            ("PricklebrowNest", "pricklebrow_nest", ":", "&y", false),
            ("RopeAnchor", "rope_anchor", "|", "&y", false),
            ("SealedArchiveShelf", "sealed_archive_shelf", "H", "&Y", false),
            ("SealedLibraryDoor", "sealed_library_door", "+", "&y", false),
            ("Shrine", "shrine", "_", "&Y", false),
        };

        /// <summary>Residents whose runtime gives them an authored visual identity (MorrowfastContent.AdditionalResidents):
        /// that exact VisualID is their canon; any other still refuses.</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> AuthoredVisual = new System.Collections.Generic.Dictionary<string, string>
        {
            { "MorrowfastFarra", "actor.morrowfast_farra" }, { "MorrowfastEdden", "actor.morrowfast_edden" },
        };
        static bool CanonicalVisual(Entity owner, RenderPart r)
            => string.IsNullOrEmpty(r.VisualID) || AuthoredVisual.TryGetValue(owner.BlueprintName, out var v) && v == r.VisualID;

        /// <summary>Slice 4: rows that are fixed props, not creatures: admitted when placed,
        /// not carried and not a creature (the cactus rule), instead of the living-creature rule.</summary>
        static readonly System.Collections.Generic.HashSet<string> SharedProps = new System.Collections.Generic.HashSet<string>
        {
            "TheRooted", "HollowStump", "IceStalactite", "LibraryChoirIronWall", "LibraryMemoryMarbleWall", "LibraryTepuiboneWall",
            "PricklebrowNest", "RopeAnchor", "SealedArchiveShelf", "SealedLibraryDoor", "Shrine",
        };
        readonly System.Collections.Generic.Dictionary<string, Tile> upper = new System.Collections.Generic.Dictionary<string, Tile>();
        readonly bool loadUpper;

        // Items on the ground and fixed props (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md): generated atlases
        // and their indexes (ArtSource/Live16x24/item-icons, prop-icons). Items are 16x16 frames;
        // props are 16x24 cells with one row per state (crop stage, door open or closed).
        public const string ItemAtlasResourcePath = "Sprites/Live16x24/item-icons";
        public const string ItemIndexResourcePath = "Content/Data/ItemIcons";
        internal const string GantryTimberBlueprint = "GantryTimberWall";
        public const string PropAtlasResourcePath = "Sprites/Live16x24/prop-icons";
        public const string PropIndexResourcePath = "Content/Data/PropIcons";
        [System.Serializable] sealed class IconRow { public string Blueprint; public string State; public int Frame; public int Variants; public string Glyph; public string Color; public string Background; }
        [System.Serializable] sealed class IconFile { public int Columns = 16; public int FrameHeight = 16; public IconRow[] Icons; }

        /// <summary>One generated atlas index: rows keyed by blueprint, then state ("" when stateless).
        /// Lookups allocate nothing: the per-cell render path calls them.</summary>
        sealed class IconIndex
        {
            readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, IconRow>> rows =
                new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, IconRow>>();
            public readonly System.Collections.Generic.Dictionary<string, IconRow> AnyRow = new System.Collections.Generic.Dictionary<string, IconRow>();
            public readonly int Columns = 16, FrameHeight = 16;
            public IconIndex(string resourcePath)
            {
                var asset = Resources.Load<TextAsset>(resourcePath);
                var file = asset != null ? JsonUtility.FromJson<IconFile>(asset.text) : null;
                if (file?.Icons == null) return;
                Columns = Mathf.Max(1, file.Columns); FrameHeight = file.FrameHeight > 0 ? file.FrameHeight : 16;
                foreach (var r in file.Icons)
                {
                    if (r == null || string.IsNullOrEmpty(r.Blueprint)) continue;
                    if (!rows.TryGetValue(r.Blueprint, out var states)) rows[r.Blueprint] = states = new System.Collections.Generic.Dictionary<string, IconRow>();
                    states[r.State ?? ""] = r;
                    if (!AnyRow.ContainsKey(r.Blueprint)) AnyRow[r.Blueprint] = r;
                }
            }
            public bool TryGet(string blueprint, string state, out IconRow row)
            {
                row = null;
                return blueprint != null && state != null && rows.TryGetValue(blueprint, out var states) && states.TryGetValue(state, out row);
            }
        }
        static IconIndex itemIndex, propIndex;
        static IconIndex Items => itemIndex ?? (itemIndex = new IconIndex(ItemIndexResourcePath));
        static IconIndex Props => propIndex ?? (propIndex = new IconIndex(PropIndexResourcePath));
        readonly System.Collections.Generic.Dictionary<int, Tile> itemTiles = new System.Collections.Generic.Dictionary<int, Tile>();
        readonly System.Collections.Generic.Dictionary<int, Tile> propTiles = new System.Collections.Generic.Dictionary<int, Tile>();
        Texture2D itemAtlas, propAtlas;

        static bool ItemRole(string blueprint, out IconRow row)
        {
            row = null;
            return !string.IsNullOrEmpty(blueprint) && Items.TryGet(blueprint, "", out row);
        }
        static bool PropRole(string blueprint) => !string.IsNullOrEmpty(blueprint) && Props.AnyRow.ContainsKey(blueprint);

        /// <summary>Has a generated item icon (and no shared body that would take precedence).</summary>
        public static bool HasItemIcon(Entity owner) => owner != null && ItemRole(owner.BlueprintName, out _) && !UpperRole(owner.BlueprintName, out _);

        /// <summary>An item in a pack or worn: the same icon it shows on the ground, for readers (the
        /// inventory ledger portrait, examine). Disclosure stays the caller's. An item lying in a zone
        /// takes the world route instead, so its reader can never disagree with its cell.</summary>
        public bool TryResolvePortableItem(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasItemIcon(owner) || !ItemRole(owner.BlueprintName, out var row) || owner.SpatialZone != null) return false;
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            if (r == null || p == null || !p.Takeable || (p.InInventory == null && p.Equipped == null)
                || r.RenderString != row.Glyph || r.ColorString != row.Color
                || !string.IsNullOrEmpty(r.BackgroundColor) || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                || owner.HasTag("Creature") || owner.HasTag("Player")) return false;
            if (owner.HasPart<StatusEffectsPart>())
            { var fx = Owned<StatusEffectsPart>(owner); if (fx == null || fx.GetRenderColorOverride() != null) return false; }
            tile = ItemTile(row); return tile != null;
        }

        static readonly string[] CropStages = { "stage0", "stage1", "stage2" };
        // Painted-place doors key "componentId|open" and "componentId|closed"; built once per component.
        static readonly System.Collections.Generic.Dictionary<string, string[]> DoorKeys = new System.Collections.Generic.Dictionary<string, string[]>();
        static string DoorKey(string componentId, bool open)
        {
            if (!DoorKeys.TryGetValue(componentId, out var keys)) DoorKeys[componentId] = keys = new[] { componentId + "|closed", componentId + "|open" };
            return keys[open ? 1 : 0];
        }

        /// <summary>The prop's state as its index keys it: a crop's growth stage, a door's open or closed, a
        /// painted-place scene owner's component (Morrowfast doors with their state), otherwise "". A doubled or
        /// foreign state part has no state.</summary>
        static string PropState(Entity owner)
        {
            if (owner.BlueprintName == "MorrowfastSceneProp")
            {
                var prop = Owned<MorrowfastPropPart>(owner);
                if (prop == null || string.IsNullOrEmpty(prop.ComponentId)) return null;
                if (!owner.HasPart<MorrowfastDoorPart>()) return prop.ComponentId;
                var door = Owned<MorrowfastDoorPart>(owner);
                return door == null ? null : DoorKey(prop.ComponentId, door.IsOpen);
            }
            if (owner.BlueprintName == "FellingSceneProp")
            { var layer = Owned<FellingScenePropPart>(owner); return layer == null || string.IsNullOrEmpty(layer.ComponentId) ? null : layer.ComponentId; }
            if (owner.HasPart<CropPart>())
            { var crop = Owned<CropPart>(owner); return crop != null && crop.GrowthStage >= 0 && crop.GrowthStage < CropStages.Length ? CropStages[crop.GrowthStage] : null; }
            if (owner.HasPart<DoorPart>()) { var door = Owned<DoorPart>(owner); return door != null ? (door.IsClosed ? "closed" : "open") : null; }
            return "";
        }

        Tile IconTile(IconIndex index, string atlasPath, ref Texture2D atlas, System.Collections.Generic.Dictionary<int, Tile> cache, IconRow row, string label, int variant = 0)
        {
            int frame = row.Frame + variant;
            if (cache.TryGetValue(frame, out var tile)) return tile;
            if (loadUpper && atlas == null) atlas = Resources.Load<Texture2D>(atlasPath);
            tile = null;
            int h = index.FrameHeight;
            if (atlas != null)
            {
                int cx = frame % index.Columns, cy = frame / index.Columns, rows = atlas.height / h;
                if (cy < rows && (cx + 1) * 16 <= atlas.width)
                {
                    var sprite = Sprite.Create(atlas, new Rect(cx * 16, (rows - 1 - cy) * h, 16, h), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
                    sprite.name = "Live16x24 " + label + " " + row.Blueprint + (string.IsNullOrEmpty(row.State) ? "" : " " + row.State); sprite.hideFlags = HideFlags.HideAndDontSave;
                    tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                    tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
                }
            }
            cache[frame] = tile;
            return tile;
        }
        Tile ItemTile(IconRow row) => IconTile(Items, ItemAtlasResourcePath, ref itemAtlas, itemTiles, row, "item");
        Tile PropTile(IconRow row, int variant) => IconTile(Props, PropAtlasResourcePath, ref propAtlas, propTiles, row, "prop", variant);

        /// <summary>A stable per-cell choice among a family's drawn variants (plank walls, rails), so a run
        /// of the same prop does not repeat one frame. The same cell always shows the same variant.</summary>
        static int VariantAt(Zone zone, Entity owner, int variants)
        {
            if (variants <= 1) return 0;
            var cell = zone.GetEntityCell(owner);
            if (cell == null) return 0;
            uint h = (uint)(cell.X * 73856093) ^ (uint)(cell.Y * 19349663);
            return (int)(h % (uint)variants);
        }

        static bool UpperRole(string blueprint, out int index)
        {
            for (index = 0; index < SharedBodies.Length; index++) if (SharedBodies[index].Blueprint == blueprint) return true;
            index = -1; return false;
        }

        Tile UpperTile(int index)
        {
            var row = SharedBodies[index];
            if (upper.TryGetValue(row.Resource, out var tile)) return tile;
            tile = loadUpper ? Build(Resources.Load<Texture2D>("Sprites/Environment/" + row.Resource), 16, 16, 0, row.Resource) : null;
            upper[row.Resource] = tile;
            return tile;
        }

        public FlatPixelWildFaunaCatalog()
            : this(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath),
                Resources.Load<Texture2D>("Sprites/Environment/glass_scorpion"), Resources.Load<Texture2D>("Sprites/Environment/rune_cultist"),
                Resources.Load<Texture2D>("Sprites/Environment/rotling"), Resources.Load<Texture2D>("Sprites/Environment/cactus"),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.StatesResourcePath), Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath))
        { loadUpper = true; }

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
            if (UpperRole(role, out int u)) { glyph = SharedBodies[u].Glyph; color = SharedBodies[u].Color; return true; }
            if (ItemRole(role, out var item)) { glyph = item.Glyph; color = item.Color; return true; }
            if (PropRole(role)) { var any = Props.AnyRow[role]; glyph = any.Glyph; color = any.Color; return true; }
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

        // The caller supplies the existing conservative static-memory selection.
        // Never invoke live prop state resolution here: hidden HP, temperature,
        // solidity, status, tile state and neighbors must not alter remembered art.
        internal bool TryResolveRememberedGantryWall(Zone zone, int x, int y,
            in FlatPixelGreyboxCell selected, out Tile tile)
        {
            tile = null;
            var owner = selected.Owner;
            if (disposed || !selected.Remembered || owner?.BlueprintName != GantryTimberBlueprint
                || zone?.ZoneID != GantryCompositionPlan.ZoneID
                || !FlatPixelSettlementGroundCatalog.IsSupportedZone(zone)
                || !AreaCompositionScope.IsGantrySite(WorldLocationContext.For(zone).WorldMap.GetPOI(7, 8))) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null || !cell.Explored || cell.IsVisible || cell.ParentZone != zone
                || cell.X != x || cell.Y != y || owner.SpatialZone != zone
                || !ReferenceEquals(zone.GetEntityCell(owner), cell) || !owner.HasTag("Wall")) return false;
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            if (r == null || !r.Visible || r.RenderLayer != 1 || p == null || p.Takeable
                || p.InInventory != null || p.Equipped != null || Owned<DestructiblePart>(owner) == null
                || Owned<MaterialPart>(owner)?.MaterialID != "Wood"
                || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                || !string.IsNullOrEmpty(r.GlyphVariants) || !string.IsNullOrEmpty(r.TileColor)
                || !string.IsNullOrEmpty(r.DetailColor)) return false;
            if (owner.HasPart<SpatialFootprintPart>())
            { var f = Owned<SpatialFootprintPart>(owner); if (f == null || f.Offsets.Length != 1 || f.Offsets[0] != Vector2Int.zero) return false; }
            if (!Props.TryGet(owner.BlueprintName, "", out var row) || r.RenderString != row.Glyph
                || r.ColorString != row.Color || (r.BackgroundColor ?? "") != (row.Background ?? "")) return false;
            tile = PropTile(row, VariantAt(zone, owner, row.Variants));
            return tile != null;
        }

        public bool TryResolve(Zone zone, Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !AppliesTo(zone) || owner == null || owner.SpatialZone != zone || !Expected(owner.BlueprintName, out var glyph, out var color)) return false;
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            if (ItemRole(owner.BlueprintName, out var itemRow) && !UpperRole(owner.BlueprintName, out _))
            {
                // An item lying where it fell: takeable, not carried or worn, canonical, untinted.
                if (r == null || !r.Visible || r.RenderString != glyph || r.ColorString != color
                    || !string.IsNullOrEmpty(r.BackgroundColor) || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                    || p == null || !p.Takeable || p.InInventory != null || p.Equipped != null || owner.HasTag("Creature") || owner.HasTag("Player")
                    || Owned<StatusEffectsPart>(owner)?.GetRenderColorOverride() != null) return false;
                tile = ItemTile(itemRow); return tile != null;
            }
            if (PropRole(owner.BlueprintName) && !UpperRole(owner.BlueprintName, out _))
            {
                // A fixed prop where it stands: not takeable, carried or living, canonical for its
                // current state (crop stage, door open or closed), untinted, one cell, not wrecked.
                if (r == null || !r.Visible || p == null || p.Takeable || p.InInventory != null || p.Equipped != null
                    || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")
                    || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant) || !string.IsNullOrEmpty(r.GlyphVariants)) return false;
                if (owner.HasPart<StatusEffectsPart>())
                { var fx = Owned<StatusEffectsPart>(owner); if (fx == null || fx.GetRenderColorOverride() != null) return false; }
                if (owner.HasPart<SpatialFootprintPart>())
                { var f = Owned<SpatialFootprintPart>(owner); if (f == null || f.Offsets.Length != 1 || f.Offsets[0] != Vector2Int.zero) return false; }
                if (owner.HasPart<DestructiblePart>())
                { var d = Owned<DestructiblePart>(owner); if (d == null || d.Gone || d.HP <= 0) return false; }
                string state = PropState(owner);
                if (state == null || !Props.TryGet(owner.BlueprintName, state, out var propRow)
                    || r.RenderString != propRow.Glyph || r.ColorString != propRow.Color || (r.BackgroundColor ?? "") != (propRow.Background ?? "")) return false;
                tile = PropTile(propRow, VariantAt(zone, owner, propRow.Variants)); return tile != null;
            }
            if (r == null || !r.Visible || r.RenderString != glyph || r.ColorString != color
                || !string.IsNullOrEmpty(r.BackgroundColor) || !CanonicalVisual(owner, r) || !string.IsNullOrEmpty(r.VisualVariant)
                || !string.IsNullOrEmpty(r.GlyphVariants) || p == null || p.Takeable || p.InInventory != null || p.Equipped != null
                || owner.HasTag("Player") || owner.HasTag("Item") || owner.HasTag("Corpse")) return false;
            bool upperRole = UpperRole(owner.BlueprintName, out int upperIndex);
            if (owner.HasPart<StatusEffectsPart>())
            {
                var e = Owned<StatusEffectsPart>(owner); var tint = e?.GetRenderColorOverride();
                if (e == null || tint != null && !(upperRole && SharedBodies[upperIndex].Bloom && tint == SharedBodies[upperIndex].Color)) return false;
            }
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
            if (upperRole && SharedProps.Contains(owner.BlueprintName))
            {
                if (owner.HasTag("Creature")) return false;
                tile = UpperTile(upperIndex); return tile != null;
            }
            var hp = owner.GetStat("Hitpoints");
            if (!owner.HasTag("Creature") || Owned<Body>(owner) == null || Owned<BrainPart>(owner) == null
                || hp == null || hp.Owner != owner || hp.BaseValue <= 0) return false;
            if (upperRole) { tile = UpperTile(upperIndex); return tile != null; }
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
            foreach (var t in upper.Values) { if (t == null) continue; DestroyOwned(t.sprite); DestroyOwned(t); }
            upper.Clear();
            foreach (var t in itemTiles.Values) { if (t == null) continue; DestroyOwned(t.sprite); DestroyOwned(t); }
            itemTiles.Clear();
            foreach (var t in propTiles.Values) { if (t == null) continue; DestroyOwned(t.sprite); DestroyOwned(t); }
            propTiles.Clear();
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
