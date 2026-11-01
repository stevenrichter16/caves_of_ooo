using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Glyph → pixel, rank 2 (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md): berry bushes, wild hives and river
    /// shrines in ordinary Spread country reuse their approved opening art: north-vegetation frame 2,
    /// orchard-hive and river-shrine. Each is admitted exactly as the opening admits it, an unspent harvest of its own
    /// yield. A real harvest removes the source, and no spent frame is invented.
    /// </summary>
    public sealed class FlatPixelSpreadBerryHiveTests
    {
        const string Home = "Overworld.10.11.0", Vegetation = "Sprites/Live16x24/north-vegetation", Hive = "Sprites/Live16x24/orchard-hive";
        const int X = 10, Y = 10;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = new Zone(Home); manager.SetActiveZone(zone); foreach (var c in zone.Cells) c.IsVisible = c.Explored = true;
            Assume.That(manager.WorldMap.GetPOI(10, 11), Is.Null, "ordinary Spread country");
            world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup() { world?.Dispose(); content?.Dispose(); }

        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e, name); Assert.True(zone.AddEntity(e, x, y)); return e; }
        Entity OnGrass(string name) { Add("Grass"); return Add(name); }
        bool Native(Entity e, out FlatPixelGreyboxCell v)
            => world.TryResolvePresentation(zone, X, Y, out v) && ReferenceEquals(v.Owner, e) && v.NativeForeground;

        [Test] public void ABerryBushInOrdinarySpreadCountryDrawsItsApprovedOpeningFrame()
        {
            var bush = OnGrass("BerryBush");
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone), "not an opening zone");
            Assert.True(Native(bush, out var v), "the berry bush is no longer a ';' glyph");
            Assert.AreSame(Resources.Load<Texture2D>(Vegetation), v.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(32, 0, 16, 24), v.ForegroundTile.sprite.rect, "frame 2 of 4, the opening's berry bush");
            Assert.True(v.LocalGround, "it stands on the real grass");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, bush, X, Y, out var portrait, out _));
            Assert.AreSame(v.ForegroundTile.sprite, portrait, "the reader shows the same bush");
        }

        [TestCase("spent")] [TestCase("wrong-yield")] [TestCase("zero-min")] [TestCase("reversed-range")] [TestCase("zero-chance")]
        [TestCase("recoloured")] [TestCase("wrecked")]
        public void ABerryBushOnlyPromisesItsActualUnspentHarvest(string mutation)
        {
            var bush = OnGrass("BerryBush"); var harvest = bush.GetPart<HarvestablePart>();
            Assume.That(Native(bush, out _));
            switch (mutation)
            {
                case "spent": harvest.Harvested = true; break;
                case "wrong-yield": harvest.YieldBlueprint = "Mushroom"; break;
                case "zero-min": harvest.YieldMin = 0; break;
                case "reversed-range": harvest.YieldMin = 4; harvest.YieldMax = 3; break;
                case "zero-chance": harvest.YieldChance = 0; break;
                case "recoloured": bush.GetPart<RenderPart>().ColorString = "&G"; break;
                case "wrecked": bush.GetPart<DestructiblePart>().HP = 0; break;
            }
            Assert.False(Native(bush, out _), mutation + ": counter-check, not the canonical unspent bush");
        }

        [Test] public void AWildHiveInOrdinarySpreadCountryDrawsTheApprovedOrchardHive()
        {
            var hive = OnGrass("Beehive");
            Assert.True(Native(hive, out var v), "the hive is no longer a '6' glyph");
            Assert.AreSame(Resources.Load<Texture2D>(Hive), v.ForegroundTile.sprite.texture);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, hive, X, Y, out var portrait, out _));
            Assert.AreSame(v.ForegroundTile.sprite, portrait);
        }

        [Test] public void ARiverShrineOutsideTheOpeningDrawsItsApprovedImage()
        {
            var shrine = OnGrass("RiverShrine");
            Assert.True(Native(shrine, out var v), "the shrine is no longer a '_' glyph");
            Assert.AreSame(Resources.Load<Texture2D>("Sprites/Live16x24/river-shrine"), v.ForegroundTile.sprite.texture);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, shrine, X, Y, out var portrait, out _));
            Assert.AreSame(v.ForegroundTile.sprite, portrait);
        }

        [Test] public void AHiddenHiveIsNotDisclosedFromMemory()
        {
            var hive = OnGrass("Beehive"); Assume.That(Native(hive, out _));
            zone.GetCell(X, Y).IsVisible = false;
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var memory));
            Assert.True(memory.Remembered);
            Assert.False(memory.NativeForeground && ReferenceEquals(memory.Owner, hive), "counter-check: memory shows terrain, not the hive");
        }

        [Test] public void ASpentHiveKeepsItsGlyph()
        {
            var hive = OnGrass("Beehive"); Assume.That(Native(hive, out _));
            hive.GetPart<HarvestablePart>().Harvested = true;
            Assert.False(Native(hive, out _), "counter-check: no empty hive is invented");
        }
    }
}
