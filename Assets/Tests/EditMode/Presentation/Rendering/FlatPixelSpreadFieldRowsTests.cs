using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Ordinary fields reuse the opening's real stubble/gleaning and old-field soil.
    /// This is presentation only; prepared beds and foreign states retain their own policy.</summary>
    public sealed class FlatPixelSpreadFieldRowsTests
    {
        const int X = 10, Y = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Entity row;
        EntityFactory oldHarvestFactory;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            oldHarvestFactory=HarvestablePart.Factory;HarvestablePart.Factory=content.Factory;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = new Zone("Overworld.10.11.0"); manager.SetActiveZone(zone);
            foreach (var cell in zone.Cells) cell.IsVisible = cell.Explored = true;
            world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelSpreadEnvironmentCatalog.IsSupportedZone(zone));
        }
        [TearDown] public void Cleanup() { world?.Dispose(); HarvestablePart.Factory=oldHarvestFactory;content?.Dispose(); }
        Entity Add(string name)
        { var owner = content.Factory.CreateEntity(name); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, X, Y)); return owner; }
        void Field(bool ripe = false) { Add("Grass"); row = Add(ripe ? "RipeCropRow" : "CropRow"); }
        bool Native(out FlatPixelGreyboxCell view)
            => world.TryResolvePresentation(zone, X, Y, out view) && view.Owner == row && view.NativeForeground;
        void Stubble()
        {
            row.GetPart<FieldHarvestPart>().Harvested = true;
            row.GetPart<RenderPart>().RenderString = "\"";
            row.GetPart<RenderPart>().ColorString = "&w";
        }

        [TestCase(false, false, 8)] [TestCase(true, false, 7)] [TestCase(true, true, 8)]
        public void OrdinaryRowsUseApprovedBodyAndOldFieldSoil(bool ripe, bool harvested, int frame)
        {
            Field(ripe); if (harvested) Stubble();
            Assert.True(Native(out var view), "ordinary field must not remain an ASCII row");
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath), view.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), view.ForegroundTile.sprite.rect);
            Assert.True(view.LocalGround);
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath), view.GroundTile.sprite.texture);
            Assert.AreEqual(new Rect(18 * 16, 0, 16, 24), view.GroundTile.sprite.rect);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, row, X, Y, out var portrait, out _));
            Assert.AreSame(view.ForegroundTile.sprite, portrait);
        }

        [Test] public void HarvestedStateChangesTheSameOwnerWithoutRecreatingTheCatalog()
        {
            Field(true); Assert.True(Native(out var ripe)); Stubble(); Assert.True(Native(out var cut));
            Assert.AreSame(ripe.Owner, cut.Owner);
            Assert.AreNotEqual(ripe.ForegroundTile.sprite.rect, cut.ForegroundTile.sprite.rect);
            Assert.AreEqual(2, row.GetPart<RenderPart>().RenderLayer, "real harvest retains ripe layer2");
            Assert.AreEqual(1, row.GetPart<FieldHarvestPart>().YieldCount);
        }

        [Test] public void UnderfootReaderStillDescribesTheCoveredRow()
        {
            Field(true); Assert.True(Native(out var before)); Add("Player");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, row, X, Y, out var portrait, out _));
            Assert.AreSame(before.ForegroundTile.sprite, portrait);
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var covered));
            Assert.AreEqual(before.GroundTile.sprite.rect, covered.GroundTile.sprite.rect);
        }

        [TestCase("hidden-cell")] [TestCase("unexplored")] [TestCase("hidden-owner")]
        [TestCase("recoloured")] [TestCase("solid")] [TestCase("foreign-yield")]
        [TestCase("prepared")] [TestCase("wrong-glyph")] [TestCase("wrong-layer")]
        public void UnsupportedMeaningDoesNotBorrowTheCanonicalField(string change)
        {
            Field(true);
            switch (change)
            {
                case "hidden-cell": zone.GetCell(X, Y).IsVisible = false; break;
                case "unexplored": zone.GetCell(X, Y).Explored = false; break;
                case "hidden-owner": row.GetPart<RenderPart>().Visible = false; break;
                case "recoloured": row.GetPart<RenderPart>().ColorString = "&R"; break;
                case "solid": row.GetPart<PhysicsPart>().Solid = true; break;
                case "foreign-yield": row.GetPart<FieldHarvestPart>().YieldBlueprint = "Mushroom"; break;
                case "prepared": row.AddPart(new CultivatedSoilPart()); break;
                case "wrong-glyph": row.GetPart<RenderPart>().RenderString = "?"; break;
                case "wrong-layer": row.GetPart<RenderPart>().RenderLayer = 10; break;
            }
            Assert.False(Native(out _), change);
            using(var selector=new FlatPixelFallbackCatalog())
            using(var catalog=new FlatPixelSpreadEnvironmentCatalog(selector))
                Assert.False(catalog.TryGetSubjectSprite(zone,row,X,Y,out _),change+" reader");
        }

        [Test] public void MissingBaseArtDoesNotBorrowAResourceBehindTheCallersBack()
        {
            Field(); using (var selector = new FlatPixelFallbackCatalog())
            using (var catalog = new FlatPixelSpreadEnvironmentCatalog(null, null, null, null, null, selector))
            {
                Assert.False(catalog.TryResolveForeground(zone, X, Y, row, out _));
                Assert.False(catalog.TryResolveGround(zone, X, Y, out _, out _));
            }
        }

        [Test] public void ValidSoilWithMissingBodyDoesNotEraseTheFallbackRow()
        {
            Field(true);
            using(var selector=new FlatPixelFallbackCatalog())
            using(var catalog=new FlatPixelSpreadEnvironmentCatalog(null,
                Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath),null,null,null,selector))
            {
                Assert.False(catalog.TryResolveForeground(zone,X,Y,row,out _));
                Assert.False(catalog.TryResolveGround(zone,X,Y,out _,out _),
                    "claiming row ground suppresses the world fallback glyph; never claim without body art");
            }
        }

        [Test] public void RowOnlyCellsDoNotRequireInventedGrassAndRemovalRestoresActualSubstrate()
        {
            row=Add("CropRow"); Assert.True(Native(out var before));
            Assert.AreEqual(new Rect(288,0,16,24),before.GroundTile.sprite.rect);
            zone.RemoveEntity(row); Assert.False(Native(out _));
            var grass=Add("Grass"); Assert.True(world.TryResolvePresentation(zone,X,Y,out var after));
            Assert.AreSame(grass,after.Owner);
            Assert.AreNotSame(before.GroundTile.sprite.texture,after.GroundTile.sprite.texture);
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedSoilKeepsTheAvailableBody(bool malformed)
        {
            Field(true); var invalid=malformed?new Texture2D(16,24):null;
            try
            {
                using(var selector=new FlatPixelFallbackCatalog())
                using(var catalog=new FlatPixelSpreadEnvironmentCatalog(null,invalid,null,null,
                    Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),selector))
                {
                    Assert.True(catalog.TryResolveForeground(zone,X,Y,row,out _));
                    Assert.False(catalog.TryResolveGround(zone,X,Y,out _,out _));
                    Assert.True(catalog.TryGetSubjectSprite(zone,row,X,Y,out _));
                }
            }
            finally { if(invalid!=null)Object.DestroyImmediate(invalid); }
        }

        [Test] public void HiddenCompetingOwnerDoesNotDiscloseItselfThroughTheField()
        {
            Field(true); Assert.True(Native(out var before));
            var other=Add("RoadStone");other.GetPart<RenderPart>().Visible=false;
            Assert.True(Native(out var after));
            Assert.AreSame(before.GroundTile,after.GroundTile);
            Assert.AreSame(before.ForegroundTile,after.ForegroundTile);
            other.GetPart<RenderPart>().Visible=true;Assert.False(Native(out _));
        }

        [TestCase("water")] [TestCase("oil")] [TestCase("mixed")]
        public void LiveCoatingsNeverReceiveDryFieldSoil(string state)
        {
            Field(true); Assert.True(Native(out _));
            zone.TileState.WriteCoating(X,Y,state=="mixed"?"water":state,4);
            if(state=="mixed")zone.TileState.WriteCoating(X,Y,"oil",4);
            Assert.False(Native(out _));
            using(var selector=new FlatPixelFallbackCatalog())
            using(var catalog=new FlatPixelSpreadEnvironmentCatalog(selector))
                Assert.False(catalog.TryResolveGround(zone,X,Y,out _,out _));
        }

        [TestCase("duplicate-row")] [TestCase("duplicate-harvest")] [TestCase("foreign-harvest")]
        [TestCase("crop")] [TestCase("road")] [TestCase("flowers")] [TestCase("carried")]
        public void AmbiguousOrForeignFieldStateKeepsFallback(string state)
        {
            Field(true); Assert.True(Native(out _));
            switch(state)
            {
                case "duplicate-row": Add("CropRow"); break;
                case "duplicate-harvest": row.Parts.Add(new FieldHarvestPart{ParentEntity=row}); break;
                case "foreign-harvest": row.GetPart<FieldHarvestPart>().ParentEntity=new Entity(); break;
                case "crop": row.AddPart(new CropPart()); break;
                case "road": Add("RoadStone"); break;
                case "flowers": Add("CharmFlowers"); break;
                case "carried": row.GetPart<PhysicsPart>().InInventory=new Entity(); break;
            }
            Assert.False(Native(out _),state);
        }

        [Test] public void RealHarvestProducesOneGrainAndTheSameCutRow()
        {
            Field(true); Assert.True(Native(out var before));
            var actor=content.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(actor,X+1,Y));
            var harvest=new GameEvent("InventoryAction");
            harvest.SetParameter("Actor",actor);harvest.SetParameter("Zone",zone);harvest.SetParameter("Command","Harvest");
            row.FireEvent(harvest);Assert.True(harvest.Handled);
            Assert.True(Native(out var after)); Assert.AreSame(before.Owner,after.Owner);
            Assert.AreEqual(new Rect(128,0,16,24),after.ForegroundTile.sprite.rect);
            int grain=0;
            foreach(var e in actor.GetPart<InventoryPart>().Objects)if(e.BlueprintName=="Emberwheat")grain++;
            foreach(var e in zone.GetAllEntities())if(e.BlueprintName=="Emberwheat")grain++;
            Assert.AreEqual(1,grain);
            var again=new GameEvent("InventoryAction");
            again.SetParameter("Actor",actor);again.SetParameter("Zone",zone);again.SetParameter("Command","Harvest");
            row.FireEvent(again);Assert.False(again.Handled);
        }

        [Test] public void OrdinaryFieldDoesNotAdmitAnOpeningOrForeignZone()
        {
            Field(); zone.ZoneID = "Overworld.11.10.0";
            using (var selector = new FlatPixelFallbackCatalog())
            using (var catalog = new FlatPixelSpreadEnvironmentCatalog(selector))
                Assert.False(catalog.TryResolveForeground(zone, X, Y, row, out _));
        }
    }
}
