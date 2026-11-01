using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Glyph → pixel slice 1 (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md, bucket A): 47 creatures and
    /// people whose shared 16x16 bodies existed under Sprites/Environment but were never admitted
    /// to the flat view now draw with them, outside opening zones, canonical owners only, with the
    /// reader portrait from the same body.
    /// </summary>
    public sealed class FlatPixelSharedBodiesTests
    {
        static EntityFactory factory;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        Zone zone;

        static readonly object[] Bodies =
        {
            new object[] { "AmbushBandit", "ambush_bandit" },
            new object[] { "BrassHusk", "brass_husk" },
            new object[] { "CaveBat", "cave_bat" },
            new object[] { "CaveBear", "cave_bear" },
            new object[] { "CaveSlime", "cave_slime" },
            new object[] { "CharredHusk", "charred_husk" },
            new object[] { "DesertBandit", "desert_bandit" },
            new object[] { "DesertProwler", "desert_prowler" },
            new object[] { "GiantSpider", "giant_spider" },
            new object[] { "Glowmaw", "glowmaw" },
            new object[] { "GroveLanternMoth", "grove_lantern_moth" },
            new object[] { "HelmwoodFrog", "helmwood_frog" },
            new object[] { "IceWight", "ice_wight" },
            new object[] { "JungleApe", "jungle_ape" },
            new object[] { "JungleStalker", "jungle_stalker" },
            new object[] { "MarlbackBreacher", "marlback_breacher" },
            new object[] { "MarlbackTunnelguard", "marlback_tunnelguard" },
            new object[] { "MarlbackWallkeeper", "marlback_wallkeeper" },
            new object[] { "RuinScavenger", "ruin_scavenger" },
            new object[] { "SandWurm", "sand_wurm" },
            new object[] { "SkeletalSentry", "skeletal_sentry" },
            new object[] { "SleepingTroll", "sleeping_troll" },
            new object[] { "SporeShambler", "spore_shambler" },
            new object[] { "Apothecary", "apothecary" },
            new object[] { "Arcanist", "arcanist" },
            new object[] { "Armorer", "armorer" },
            new object[] { "CatacombWarden", "catacomb_warden" },
            new object[] { "CaveHermit", "cave_hermit" },
            new object[] { "DesertHermit", "desert_hermit" },
            new object[] { "EncasedElder", "encased_elder" },
            new object[] { "FoundingListener", "founding_listener" },
            new object[] { "FoundingPlaqueTender", "founding_plaque_tender" },
            new object[] { "Grib", "grib" },
            new object[] { "JungleHermit", "jungle_hermit" },
            new object[] { "Marceline", "marceline" },
            new object[] { "Mogu", "mogu" },
            new object[] { "Nam", "nam" },
            new object[] { "PaleCurator", "pale_curator" },
            new object[] { "PlaqueTender", "plaque_tender" },
            new object[] { "Provisioner", "provisioner" },
            new object[] { "RuinsHermit", "ruins_hermit" },
            new object[] { "SaccharineEnvoy", "saccharine_envoy" },
            new object[] { "Sien", "sien" },
            new object[] { "Sopp", "sopp" },
            new object[] { "StillleafIndexer", "stillleaf_indexer" },
            new object[] { "StillleafSearcher", "stillleaf_searcher" },
            new object[] { "Undertaker", "undertaker" },
            // Slice 2: original bodies drawn for twelve creatures that had none.
            new object[] { "CurationHalfSet", "curation_half_set" },
            new object[] { "DirtGnome", "dirt_gnome" },
            new object[] { "Furrowstalker", "furrowstalker" },
            new object[] { "MarlbackSoursprayer", "marlback_soursprayer" },
            new object[] { "MarlbackStormbinder", "marlback_stormbinder" },
            new object[] { "SoddenPassageGuard", "sodden_passage_guard" },
            new object[] { "SootGremlin", "soot_gremlin" },
            new object[] { "SpreadDitchMate", "spread_ditch_mate" },
            new object[] { "SpreadHurdleCutter", "spread_hurdle_cutter" },
            new object[] { "SpreadLatchcoil", "spread_latchcoil" },
            new object[] { "SunStriker", "sun_striker" },
            new object[] { "Tatterjay", "tatterjay" },
            // Slice 3: original bodies for ten people who had none.
            new object[] { "ConcordFactor", "concord_factor" },
            new object[] { "CurationIntakeFiler", "curation_intake_filer" },
            new object[] { "CurationJuniorIndexer", "curation_junior_indexer" },
            new object[] { "CurationSorter", "curation_sorter" },
            new object[] { "FilerClerk", "filer_clerk" },
            new object[] { "GantryRegistrar", "gantry_registrar" },
            new object[] { "PeatCutter", "peat_cutter" },
            new object[] { "RecensionScribe", "recension_scribe" },
            new object[] { "SaltMaster", "salt_master" },
            new object[] { "TentRightHost", "tent_right_host" },
            // Slice 4: the Rooted and fixed props with existing bodies.
            new object[] { "TheRooted", "the_rooted" },
            new object[] { "HollowStump", "hollow_stump" },
            new object[] { "IceStalactite", "ice_stalactite" },
            new object[] { "LibraryChoirIronWall", "library_choir_iron_wall" },
            new object[] { "LibraryMemoryMarbleWall", "library_memory_marble_wall" },
            new object[] { "LibraryTepuiboneWall", "library_tepuibone_wall" },
            new object[] { "PricklebrowNest", "pricklebrow_nest" },
            new object[] { "RopeAnchor", "rope_anchor" },
            new object[] { "SealedArchiveShelf", "sealed_archive_shelf" },
            new object[] { "SealedLibraryDoor", "sealed_library_door" },
            new object[] { "Shrine", "shrine" },
        };

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
        }
        [SetUp] public void Setup()
        {
            manager = OverworldZoneManager.CreateDetached(factory, 64); world = new FlatPixelGreyboxCatalog();
            zone = manager.GetZone("Overworld.11.6.0"); manager.SetActiveZone(zone);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) { var c = zone.GetCell(x, y); c.Explored = true; c.IsVisible = true; }
        }
        [TearDown] public void Cleanup() { world?.Dispose(); }

        Entity Put(string blueprint)
        {
            var e = factory.CreateEntity(blueprint);
            for (int y = 3; y < Zone.Height - 3; y++) for (int x = 3; x < Zone.Width - 3; x++)
            {
                var c = zone.GetCell(x, y);
                if (c.BlocksMovement() || c.Objects.Any(o => !o.HasTag("Terrain"))) continue;
                if (zone.AddEntity(e, x, y)) return e;
            }
            Assert.Fail("no room for " + blueprint); return null;
        }

        [TestCaseSource(nameof(Bodies))]
        public void TheyDrawWithTheirSharedBody(string blueprint, string resource)
        {
            var e = Put(blueprint);
            // A lurking glowmaw is invisible until it drops (GlowmawAmbushPart); drawn, it is revealed.
            if (e.GetPart<GlowmawAmbushPart>() != null) e.GetPart<RenderPart>().Visible = true;
            Assert.True(FlatPixelWildFaunaCatalog.HasKnownRole(e), blueprint);
            var (x, y) = zone.GetEntityPosition(e);
            Assert.True(world.TryResolvePresentation(zone, x, y, out var v) && ReferenceEquals(v.Owner, e) && v.NativeForeground, blueprint + " has a native body");
            Assert.AreSame(Resources.Load<Texture2D>("Sprites/Environment/" + resource), v.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 16), v.ForegroundTile.sprite.rect);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, e, x, y, out var portrait, out _));
            Assert.AreSame(v.ForegroundTile.sprite.texture, portrait.texture, "the reader shows the same body");
        }

        [Test] public void ALurkingGlowmawIsNotGivenAway()
        {
            var e = Put("Glowmaw");
            Assume.That(e.GetPart<RenderPart>().Visible, Is.False, "the blueprint lurks");
            var (x, y) = zone.GetEntityPosition(e);
            Assert.False(world.TryResolvePresentation(zone, x, y, out var v) && ReferenceEquals(v.Owner, e) && v.NativeForeground,
                "counter-check: an ambusher still hidden draws no body");
        }

        [Test] public void ARecolouredPersonKeepsTheGlyph()
        {
            var e = Put("Apothecary"); e.GetPart<RenderPart>().ColorString = "&R";
            var (x, y) = zone.GetEntityPosition(e);
            Assert.False(world.TryResolvePresentation(zone, x, y, out var v) && ReferenceEquals(v.Owner, e) && v.NativeForeground, "counter-check: not the canonical owner");
        }
    }
}
