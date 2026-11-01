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
    /// P5 B9 (Docs/UPPER-TIER-BESTIARY.md §3.5): the upper-tier creatures that were still
    /// glyphs draw with their shared 16x16 environment bodies, the new species with their
    /// original bodies, elders with their kind's, and the Bloomed hosts with the Bloom's
    /// tint accepted. Outside opening zones, canonical owners only.
    /// </summary>
    public sealed class FlatPixelUpperBestiaryTests
    {
        static EntityFactory factory;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        Zone zone;

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

        bool Native(Entity e, out Texture2D texture)
        {
            texture = null;
            var (x, y) = zone.GetEntityPosition(e);
            if (!world.TryResolvePresentation(zone, x, y, out var v) || !ReferenceEquals(v.Owner, e) || !v.NativeForeground) return false;
            texture = v.ForegroundTile.sprite.texture;
            Assert.AreEqual(new Rect(0, 0, 16, 16), v.ForegroundTile.sprite.rect, e.BlueprintName + " uses the shared 16x16 format");
            return true;
        }

        [TestCase("AncientGuardian", "ancient_guardian")] [TestCase("CanopyStrangler", "canopy_strangler")]
        [TestCase("DuneLurker", "dune_lurker")] [TestCase("StoneGolem", "stone_golem")]
        [TestCase("VaultSentinel", "vault_sentinel")] [TestCase("PaleStalker", "pale_stalker")]
        [TestCase("PaleMatriarch", "pale_stalker")] [TestCase("ObsidianBrute", "obsidian_brute")]
        [TestCase("PrickleBrowGecko", "pricklebrow_gecko")] [TestCase("OldSkySari", "sky_sari")]
        [TestCase("Riverwarden", "riverwarden")] [TestCase("OldRiverwarden", "riverwarden")]
        [TestCase("SimaSteward", "sima_steward")] [TestCase("BloomedWarden", "bloomed_warden")]
        [TestCase("BloomedHound", "bloomed_hound")]
        public void UpperCreaturesDrawWithTheirBodies(string blueprint, string resource)
        {
            var e = Put(blueprint);
            Assert.True(FlatPixelWildFaunaCatalog.HasKnownRole(e));
            Assert.True(Native(e, out var texture), blueprint + " has a native body");
            Assert.AreSame(Resources.Load<Texture2D>("Sprites/Environment/" + resource), texture);
            var (x, y) = zone.GetEntityPosition(e);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, e, x, y, out var portrait, out _), "the reader shows the same body");
            Assert.AreSame(texture, portrait.texture);
        }

        [Test] public void ARecolouredOrDeadOneKeepsItsGlyph()
        {
            var recoloured = Put("PaleStalker"); recoloured.GetPart<RenderPart>().ColorString = "&R";
            Assert.False(Native(recoloured, out _), "counter-check: not the canonical owner");
            var dead = Put("Riverwarden"); dead.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(Native(dead, out _));
        }

        [Test] public void OnlyTheBloomedHostsAcceptTheBloomsTint()
        {
            var host = Put("BloomedHound");
            Assert.True(host.HasEffect<BloomedEffect>(), "precondition: worn");
            Assert.True(Native(host, out _));
            var stalker = Put("PaleStalker"); stalker.ApplyEffect(new BloomedEffect());
            Assert.False(Native(stalker, out _), "counter-check: a stalker the Bloom took is not the stalker's body");
        }
    }
}
