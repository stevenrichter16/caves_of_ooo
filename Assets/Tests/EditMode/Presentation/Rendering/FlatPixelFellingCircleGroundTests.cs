using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelFellingCircleGroundTests
    {
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        int x = 33, y = 13;
        Entity Marker => zone.GetCell(x, y).Objects.First(o => o.BlueprintName != "TepuiStone");
        Entity Floor => zone.GetCell(x, y).Objects.First(o => o.BlueprintName == "TepuiStone");
        [SetUp] public void Setup()
        {
            x = 33; y = 13;
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = manager.GetZone(FellingSiteBuilder.ZoneID); manager.SetActiveZone(zone);
            foreach (var c in zone.Cells) c.Explored = c.IsVisible = true;
            world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup() { world?.Dispose(); content?.Dispose(); }
        FlatPixelGreyboxCell View()
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var v)); return v; }
        void Native()
        {
            var v = View(); Assert.True(v.LocalGround, "ritual position must retain its real stone substrate");
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath), v.GroundTile.sprite.texture);
            Assert.That(v.GroundTile.sprite.rect.x / 16, Is.InRange(0, 3));
        }
        [Test] public void AllSevenPositionsKeepTheirExactGlyphAndReadersAboveActualStone()
        {
            using (var fallback = new FlatPixelFallbackCatalog())
            foreach (var l in FellingSceneDefinition.Load().landmarks)
            {
                x = l.x; y = l.y; var marker = Marker; var floor = Floor;
                int version = zone.EntityVersion; var state = zone.TileState.Get(x, y);
                Assert.True(fallback.TryResolve(zone, x, y, out var original)); Native(); var v = View();
                Assert.AreSame(marker, v.Owner); Assert.False(v.NativeForeground); Assert.False(v.BlocksMovement);
                Assert.AreEqual(original.ForegroundTile.sprite.rect, v.ForegroundTile.sprite.rect);
                Assert.AreEqual(original.ForegroundColor, v.ForegroundColor);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, marker, x, y, out var glyph, out _));
                Assert.AreNotSame(v.GroundTile.sprite.texture, glyph.texture);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, floor, x, y, out var stone, out _));
                Assert.AreSame(v.GroundTile.sprite, stone);
                Assert.AreEqual(version, zone.EntityVersion); Assert.AreSame(state, zone.TileState.Get(x, y));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void RemovingMarkerLeavesOnlyRealFloorAndNeverRecreatesRitual(bool seventh)
        {
            if (seventh) { x = 40; y = 8; }
            Native(); var marker = Marker; Assert.True(zone.RemoveEntity(marker)); int version = zone.EntityVersion;
            Native(); Assert.AreSame(Floor, View().Owner); Assert.IsNull(View().ForegroundTile);
            Assert.AreEqual(1, zone.GetCell(x, y).Objects.Count); Assert.AreEqual(version, zone.EntityVersion);
        }
        [TestCase("glyph")] [TestCase("color")] [TestCase("background")] [TestCase("detail")]
        [TestCase("tile-color")] [TestCase("variants")] [TestCase("layer")]
        [TestCase("barren")] [TestCase("underfoot")] [TestCase("solid")]
        [TestCase("foreign-part")] [TestCase("wrong-floor")]
        public void ChangedDisclosedMeaningRefusesStone(string change)
        {
            Native(); var marker = Marker; var render = marker.GetPart<RenderPart>();
            switch (change)
            {
                case "glyph": render.RenderString = "!"; break;
                case "color": render.ColorString = "&R"; break;
                case "background": render.BackgroundColor = "b"; break;
                case "detail": render.DetailColor = "R"; break;
                case "tile-color": render.TileColor = "&R"; break;
                case "variants": render.GlyphVariants = ".."; break;
                case "layer": render.RenderLayer = 3; break;
                case "barren": marker.Tags.Remove("Barren"); break;
                case "underfoot": marker.SetTag("UnderfootInteractable"); break;
                case "solid": marker.GetPart<PhysicsPart>().Solid = true; break;
                case "foreign-part": marker.AddPart(new SeventhPositionPart()); break;
                case "wrong-floor": Floor.ID = "foreign"; break;
            }
            Assert.False(View().LocalGround, change);
        }
        [TestCase(false)] [TestCase(true)]
        public void RolesCannotBeSwappedEvenWhenRenderIsUnchanged(bool seventh)
        {
            if (seventh) { x = 40; y = 8; }
            Native(); Assert.True(zone.RemoveEntity(Marker));
            Assert.True(zone.AddEntity(content.Factory.CreateEntity(seventh ? "FellingBarePosition" : "SeventhPosition"), x, y));
            Assert.False(View().LocalGround);
        }
        [Test] public void DuplicateOnlyVetoesWhileDisclosedAndPhysicallyPlaced()
        {
            Native(); var duplicate = content.Factory.CreateEntity("FellingBarePosition");
            Assert.True(zone.AddEntity(duplicate, x, y)); Assert.False(View().LocalGround);
            duplicate.GetPart<RenderPart>().Visible = false; Native();
            duplicate.GetPart<RenderPart>().Visible = true; Assert.False(View().LocalGround);
            duplicate.GetPart<PhysicsPart>().InInventory = content.Factory.CreateEntity("Player"); Native();
            duplicate.GetPart<PhysicsPart>().InInventory = null; Assert.False(View().LocalGround);
            Assert.True(zone.RemoveEntity(duplicate)); Native();
        }
        [TestCase("missing")] [TestCase("duplicate")] [TestCase("foreign-owned")]
        [TestCase("underfoot")] [TestCase("barren")]
        public void SeventhRequiresItsOwnUnambiguousMeaning(string change)
        {
            x = 40; y = 8; Native(); var marker = Marker; var part = marker.GetPart<SeventhPositionPart>();
            switch (change)
            {
                case "missing": Assert.True(marker.RemovePart(part)); break;
                case "duplicate": marker.AddPart(new SeventhPositionPart()); break;
                case "foreign-owned": part.ParentEntity = Floor; break;
                case "underfoot": marker.Tags.Remove("UnderfootInteractable"); break;
                case "barren": marker.SetTag("Barren"); break;
            }
            Assert.False(View().LocalGround, change);
        }
        [Test] public void StateAndAuthorityRevokeAndRestoreWithoutCachedPermission()
        {
            Native(); zone.TileState.WriteCoating(x, y, "oil", 5); Assert.False(View().LocalGround);
            zone.TileState.Get(x, y).Coatings.Clear(); Native();
            var poi = manager.WorldMap.GetPOI(3, 5);
            manager.WorldMap.SetPOI(3, 5, new PointOfInterest(POIType.MerchantCamp, "replacement"));
            Assert.False(View().LocalGround); manager.WorldMap.SetPOI(3, 5, poi); Native();
        }
        [Test] public void CoveredPositionKeepsActorAndRestoresGlyphWhenActorLeaves()
        {
            x = 40; y = 8; Native(); var marker = Marker; var actor = content.Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(actor, x, y)); Native(); Assert.AreSame(actor, View().Owner);
            Assert.True(zone.RemoveEntity(actor)); Native(); Assert.AreSame(marker, View().Owner);
            Assert.NotNull(marker.GetPart<SeventhPositionPart>());
        }
        [Test] public void MissingAtlasFogAndWrongCoordinatesNeverPromoteRitualGround()
        {
            Native(); using (var empty = new FlatPixelGreyboxCatalog((Texture2D)null))
            { Assert.True(empty.TryResolvePresentation(zone, x, y, out var v)); Assert.False(v.LocalGround); }
            zone.GetCell(x, y).IsVisible = false; Assert.False(View().LocalGround);
            zone.GetCell(x, y).Explored = false; Assert.False(world.TryResolvePresentation(zone, x, y, out _));
            zone.GetCell(x, y).IsVisible = zone.GetCell(x, y).Explored = true; Native();
            x = 34; Assert.True(zone.AddEntity(content.Factory.CreateEntity("FellingBarePosition"), x, y));
            Assert.False(View().LocalGround);
        }
    }
}
