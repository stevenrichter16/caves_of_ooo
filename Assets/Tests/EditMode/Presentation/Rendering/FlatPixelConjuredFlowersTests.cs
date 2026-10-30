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
    /// Conjured flowers (2026-10-30). FlowerField ("conjured flowers": petals hummed up for the season over
    /// a half-buried carved token, a 200-turn lifespan, a re-asserted petals residue) drew a magenta '*' on
    /// a black box across the Spread, including whole meadows. It is not CharmFlowers, so it gets its own
    /// original sprite, drawn by the Spread adapter over the real grass; its own petals residue does not
    /// hide the ground, while any other live state still does.
    /// </summary>
    public sealed class FlatPixelConjuredFlowersTests
    {
        const string Atlas = "Sprites/Live16x24/conjured-flowers";
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
        bool Native(Zone zone, int x, int y, out FlatPixelGreyboxCell v)
            => world.TryResolvePresentation(zone, x, y, out v) && v.NativeForeground && v.ForegroundTile?.sprite != null
                && ReferenceEquals(v.ForegroundTile.sprite.texture, Resources.Load<Texture2D>(Atlas)) && v.LocalGround;

        [TestCase("Overworld.6.13.0")] [TestCase("Overworld.5.8.0")]
        public void ConjuredFlowersDrawTheirOwnSpriteOverRealGrass(string id)
        {
            var zone = Visit(id);
            var flowers = zone.GetAllEntities().Where(e => e.BlueprintName == "FlowerField").ToList();
            Assert.GreaterOrEqual(flowers.Count, 5, "precondition: conjured flowers");
            int native = 0;
            foreach (var f in flowers)
            {
                var (x, y) = zone.GetEntityPosition(f);
                if (!Native(zone, x, y, out var v)) continue;
                native++;
                Assert.AreSame(f, v.Owner);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, f, x, y, out var portrait, out _)); Assert.AreSame(v.ForegroundTile.sprite.texture, portrait.texture);
            }
            Assert.GreaterOrEqual(native, flowers.Count * 95 / 100, id + ": " + native + " of " + flowers.Count);
        }

        [Test] public void TheirOwnPetalsResidueKeepsTheGroundButOtherStateDoesNot()
        {
            var zone = Visit("Overworld.6.13.0");
            var f = zone.GetAllEntities().First(e => e.BlueprintName == "FlowerField"); var (x, y) = zone.GetEntityPosition(f);
            zone.TileState.WriteResidue(x, y, "petals", 4);
            Assert.True(Native(zone, x, y, out _), "its own re-asserted petals are not foreign state");
            zone.TileState.WriteCoating(x, y, "water", 4);
            Assert.False(Native(zone, x, y, out _), "water on the flowers is real live state");
        }

        [Test] public void CharmFlowersAndAChangedFlowerKeepTheirOwnPresentation()
        {
            var zone = Visit("Overworld.6.13.0");
            var f = zone.GetAllEntities().First(e => e.BlueprintName == "FlowerField"); var (x, y) = zone.GetEntityPosition(f);
            f.GetPart<RenderPart>().ColorString = "&R";
            Assert.False(Native(zone, x, y, out _), "a recoloured flower is not the canonical conjured patch");
            f.GetPart<RenderPart>().ColorString = "&M"; f.RemovePart(f.GetPart<FlowerCharmPart>());
            Assert.False(Native(zone, x, y, out _), "without its charm it is not conjured flowers");
            var charm = factory.CreateEntity("CharmFlowers");
            Assert.False(FlatPixelSpreadEnvironmentCatalog.IsConjuredFlowers(charm, null), "CharmFlowers keep their own approved bells");
        }
    }
}
