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
    /// Wild fauna (2026-10-30). Outside the opening, wandering magpies, vipers, glass scorpions, rune
    /// cultists, rotlings and the camp cactus were still glyphs although approved bodies exist: Magpie and
    /// Viper in the opening's north-works atlas (frames 5, 6) and the others as the shared 16x16 environment
    /// sprites (the accepted Merchant/Elder format). They now draw outside opening zones only, so the
    /// opening's own routes are unchanged.
    /// </summary>
    public sealed class FlatPixelWildFaunaTests
    {
        static EntityFactory factory;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
        }
        [SetUp] public void Setup() { manager = OverworldZoneManager.CreateDetached(factory, 64); world = new FlatPixelGreyboxCatalog(); }
        [TearDown] public void Cleanup() { world?.Dispose(); }

        Zone Visit(string id)
        {
            var zone = manager.GetZone(id); Assert.NotNull(zone, id); manager.SetActiveZone(zone);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) { var c = zone.GetCell(x, y); c.Explored = true; c.IsVisible = true; }
            return zone;
        }

        [TestCase("Overworld.4.9.0", "Magpie", "Sprites/Live16x24/north-works", 80, 24)]
        [TestCase("Overworld.5.10.0", "Viper", "Sprites/Live16x24/north-works", 96, 24)]
        [TestCase("Overworld.0.17.0", "GlassScorpion", "Sprites/Environment/glass_scorpion", 0, 16)]
        [TestCase("Overworld.0.14.0", "RuneCultist", "Sprites/Environment/rune_cultist", 0, 16)]
        [TestCase("Overworld.0.0.0", "Rotling", "Sprites/Environment/rotling", 0, 16)]
        [TestCase("Overworld.15.18.0", "Cactus", "Sprites/Environment/cactus", 0, 16)]
        public void WildFaunaUseTheirApprovedBodies(string id, string blueprint, string resource, int rectX, int height)
        {
            var zone = Visit(id);
            var owners = zone.GetAllEntities().Where(e => e.BlueprintName == blueprint).ToList();
            Assert.IsNotEmpty(owners, "precondition: " + blueprint + " in " + id);
            int shown = 0;
            foreach (var owner in owners)
            {
                var (x, y) = zone.GetEntityPosition(owner);
                if (!world.TryResolvePresentation(zone, x, y, out var v) || !ReferenceEquals(v.Owner, owner)) continue;
                shown++;
                Assert.True(v.NativeForeground, blueprint + " has a native body");
                Assert.AreSame(Resources.Load<Texture2D>(resource), v.ForegroundTile.sprite.texture);
                Assert.AreEqual(new Rect(rectX, 0, 16, height), v.ForegroundTile.sprite.rect);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, x, y, out var portrait, out _)); Assert.AreSame(v.ForegroundTile.sprite.texture, portrait.texture);
            }
            Assert.Greater(shown, 0, "precondition: a selected " + blueprint);
        }

        // Camps and wild camps: the merchant camp's Warden (no guard duty) wears the approved opening Warden
        // body, and ordinary (permanently lit) campfires burn on the real biome ground beneath them.
        [TestCase("Overworld.8.2.0")] [TestCase("Overworld.0.16.0")]
        public void OrdinaryCampfiresBurnOnRealGround(string id)
        {
            var zone = Visit(id);
            var fires = zone.GetAllEntities().Where(e => e.BlueprintName == "Campfire").ToList();
            Assert.IsNotEmpty(fires, "precondition: a campfire");
            foreach (var fire in fires)
            {
                var (x, y) = zone.GetEntityPosition(fire);
                Assert.True(world.TryResolvePresentation(zone, x, y, out var v)); Assert.AreSame(fire, v.Owner);
                Assert.True(v.NativeForeground, "the hearth burns natively");
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.StatesResourcePath), v.ForegroundTile.sprite.texture);
                Assert.AreEqual(new Rect(80, 0, 16, 24), v.ForegroundTile.sprite.rect);
                Assert.True(v.LocalGround, "real ground beneath the fire");
            }
        }

        [Test] public void TheCampWardenWearsTheApprovedWardenBody()
        {
            var zone = Visit("Overworld.8.2.0");
            var warden = zone.GetAllEntities().First(e => e.BlueprintName == "Warden"); var (x, y) = zone.GetEntityPosition(warden);
            Assert.True(world.TryResolvePresentation(zone, x, y, out var v)); Assert.AreSame(warden, v.Owner);
            Assert.True(v.NativeForeground); Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath), v.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(144, 0, 16, 24), v.ForegroundTile.sprite.rect);
        }

        [Test] public void ARecolouredOrDeadCreatureKeepsItsGlyph()
        {
            var zone = Visit("Overworld.0.14.0");
            var cultists = zone.GetAllEntities().Where(e => e.BlueprintName == "RuneCultist").ToList();
            var a = cultists[0]; var (x, y) = zone.GetEntityPosition(a);
            a.GetPart<RenderPart>().ColorString = "&R";
            Assert.True(world.TryResolvePresentation(zone, x, y, out var red)); Assert.False(red.NativeForeground && ReferenceEquals(red.Owner, a));
            var b = cultists[1]; var (x2, y2) = zone.GetEntityPosition(b); b.GetStat("Hitpoints").BaseValue = 0;
            Assert.True(world.TryResolvePresentation(zone, x2, y2, out var dead)); Assert.False(dead.NativeForeground && ReferenceEquals(dead.Owner, b));
        }

        [Test] public void OpeningZonesKeepTheirOwnRoutes()
        {
            Assert.False(FlatPixelWildFaunaCatalog.AppliesTo(Visit("Overworld.11.9.0")), "the opening draws its own fauna");
            Assert.True(FlatPixelWildFaunaCatalog.AppliesTo(Visit("Overworld.4.9.0")));
        }
    }
}
