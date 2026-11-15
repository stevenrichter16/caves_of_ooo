using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelWellmeetWellTests
    {
        const int X=40,Y=12;
        const BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; Entity well;
        RepairableSiteState site; FlatPixelGreyboxCatalog world;
        Texture2D Atlas=>Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath);
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope(); manager=OverworldZoneManager.CreateDetached(content.Factory,224401440,true);
            zone=new Zone(WellmeetCompositionPlan.ZoneID); manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.IsVisible=c.Explored=true;
            site=manager.SettlementManager.GetSite(zone.ZoneID,SettlementSiteDefinitions.MainWellSiteId);
            Assert.NotNull(site); well=content.Factory.CreateEntity("Well");
            well.Properties["SettlementId"]=zone.ZoneID; well.Properties["SettlementSiteId"]=site.SiteId;
            well.AddPart(new WellSitePart{SettlementId=zone.ZoneID,SiteId=site.SiteId}); zone.AddEntity(well,X,Y);
            Stage(RepairStage.Fouled); world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        void Stage(RepairStage stage){site.Stage=stage;SettlementSiteVisuals.ApplyToEntity(well,site);}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        void Native(int frame)
        {
            var v=View();Assert.AreSame(well,v.Owner);Assert.True(v.NativeForeground,"MainWell remains a glyph");
            Assert.AreSame(Atlas,v.ForegroundTile.sprite.texture);Assert.AreEqual(new Rect(frame*16,0,16,24),v.ForegroundTile.sprite.rect);
            Assert.AreEqual(QudColorParser.Parse(well.GetPart<RenderPart>().ColorString),v.ForegroundColor);
            Assert.True(v.PreserveRenderColorCue);Assert.True(v.BlocksMovement);Assert.AreEqual(Tile.ColliderType.None,v.ForegroundTile.colliderType);
            foreach(bool semantic in new[]{false,true})
            {Sprite s;Color tint;Assert.True(semantic?world.TryGetSubjectSprite(well,X,Y,out s,out tint):world.TryGetPresentationSubjectSprite(zone,well,X,Y,out s,out tint));Assert.AreSame(v.ForegroundTile.sprite,s);Assert.AreEqual(v.ForegroundColor,tint);}
        }
        void Refused()
        {
            if(world.TryResolvePresentation(zone,X,Y,out var v))Assert.AreNotSame(Atlas,v.ForegroundTile?.sprite?.texture);
            if(world.TryGetPresentationSubjectSprite(zone,well,X,Y,out var s,out _))Assert.AreNotSame(Atlas,s.texture);
            if(world.TryGetSubjectSprite(well,X,Y,out s,out _))Assert.AreNotSame(Atlas,s.texture);
        }
        static object Get(object o,string n)=>o.GetType().GetField(n,I).GetValue(o);
        [TestCase(RepairStage.Fouled,0)][TestCase(RepairStage.TemporarilyPurified,0)]
        [TestCase(RepairStage.StableRepair,1)][TestCase(RepairStage.ImprovedWithCaretaker,1)]
        public void FourStagesPreserveLiningAndNativeColorAcrossWorldAndReaders(RepairStage stage,int frame)
        {Stage(stage);Native(frame);var count=zone.EntityVersion;var state=zone.TileState.ToSaveString();var probe=new Probe();well.AddPart(probe);Native(frame);Assert.Zero(probe.Renders);Assert.AreEqual(count,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());Stage(RepairStage.Fouled);Native(0);}
        [TestCase("name")][TestCase("poi")][TestCase("profile")][TestCase("biome")][TestCase("cache")][TestCase("tiles")][TestCase("pois")]
        public void ChangedCurrentSiteAuthorityRevokesWarmWorldAndReaders(string fault)
        {Native(0);var poi=manager.WorldMap.GetPOI(8,16);switch(fault){case "name":poi.Name="elsewhere";break;case "poi":poi.Type=POIType.MerchantCamp;break;case "profile":poi.Profile="foreign";break;case "biome":manager.WorldMap.Tiles[8,16]=BiomeType.Sodden;break;case "cache":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;case "tiles":manager.WorldMap.Tiles=new BiomeType[1,1];break;case "pois":manager.WorldMap.POIs=new PointOfInterest[1,1];break;}Refused();}
        [TestCase("Overworld.8.16.1")][TestCase("Overworld.08.16.0")][TestCase("Overworld.9.16.0")]
        public void AnotherAddressDoesNotBorrowMainWell(string id){Native(0);zone.ZoneID=id;Refused();}
        [TestCase("entity-town")][TestCase("entity-site")][TestCase("part-town")][TestCase("part-site")][TestCase("stage")][TestCase("type")][TestCase("problem")]
        public void BindingsAndCurrentStageMustAgree(string fault)
        {Native(0);switch(fault){case "entity-town":well.Properties["SettlementId"]="foreign";break;case "entity-site":well.Properties["SettlementSiteId"]="other";break;case "part-town":well.GetPart<WellSitePart>().SettlementId="foreign";break;case "part-site":well.GetPart<WellSitePart>().SiteId="other";break;case "stage":site.Stage=RepairStage.StableRepair;break;case "type":site.SiteType=RepairableSiteType.Structure;break;case "problem":site.ProblemType=(RepairProblemType)999;break;}Refused();}
        [TestCase("missing")][TestCase("foreign")][TestCase("duplicate")]
        public void EssentialPartsMustRemainUniqueAndOwned(string fault)
        {foreach(var type in new[]{typeof(RenderPart),typeof(PhysicsPart),typeof(ExaminablePart),typeof(WellPart),typeof(WellSitePart)}){Native(0);var part=well.Parts.First(p=>p.GetType()==type);Part extra=null;if(fault=="missing")well.RemovePart(part);else if(fault=="foreign")part.ParentEntity=new Entity();else {extra=(Part)Activator.CreateInstance(type);well.AddPart(extra);}Refused();if(fault=="missing")well.AddPart(part);else if(fault=="foreign")part.ParentEntity=well;else well.RemovePart(extra);}}
        [TestCase("glyph")][TestCase("color")][TestCase("visual")][TestCase("variant")][TestCase("layer")][TestCase("background")][TestCase("variants")][TestCase("takeable")][TestCase("carried")][TestCase("equipped")][TestCase("passable")]
        public void CustomizedOrPortableOwnerDoesNotBorrowTheSiteBody(string fault)
        {Native(0);var r=well.GetPart<RenderPart>();var p=well.GetPart<PhysicsPart>();switch(fault){case "glyph":r.RenderString="?";break;case "color":r.ColorString="&R";break;case "visual":r.VisualID="foreign";break;case "variant":r.VisualVariant="foreign";break;case "variants":r.GlyphVariants="!?";break;case "layer":r.RenderLayer=2;break;case "background":r.BackgroundColor="^R";break;case "takeable":p.Takeable=true;break;case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;case "passable":p.Solid=false;break;}Refused();}
        [TestCase("registry")][TestCase("site")]
        public void MissingRecordsAreNotCreatedOrBackfilledByQueries(string loss)
        {Native(0);var registry=(Dictionary<string,SettlementState>)Get(manager.SettlementManager,"_settlements");var state=registry[zone.ZoneID];if(loss=="registry")registry.Clear();else state.RestoreCollections(null,null,null);var n=registry.Count;var m=state.Sites.Count;Refused();Assert.AreEqual(n,registry.Count);Assert.AreEqual(m,state.Sites.Count);}
        [Test] public void AnotherActiveRegistryDoesNotReplaceDisplayedGraphAuthority()
        {Native(0);var other=new SettlementManager();other.GetOrCreateSettlement(zone.ZoneID,manager.WorldMap.GetPOI(8,16)).GetSite(site.SiteId).Stage=RepairStage.StableRepair;Native(0);}
        [TestCase(typeof(RepairablePart))][TestCase(typeof(LiquidPoolPart))][TestCase(typeof(ContainerPart))][TestCase(typeof(TraderPart))][TestCase(typeof(DoorPart))][TestCase(typeof(StairsDownPart))]
        public void DifferentServiceMeaningIsNotMasked(Type type){Native(0);well.AddPart((Part)Activator.CreateInstance(type));Refused();}
        [TestCase("hidden")][TestCase("memory")][TestCase("unexplored")][TestCase("removed")]
        public void UndisclosedOrDetachedOwnerCannotClaimLiveSiteArt(string fault)
        {Native(0);if(fault=="hidden")well.GetPart<RenderPart>().Visible=false;if(fault=="memory")zone.GetCell(X,Y).IsVisible=false;if(fault=="unexplored"){zone.GetCell(X,Y).IsVisible=false;zone.GetCell(X,Y).Explored=false;}if(fault=="removed")zone.RemoveEntity(well);Refused();}
        [Test] public void OrdinaryWellAndCoveredSubjectKeepDistinctAuthority()
        {Native(0);var extra=content.Factory.CreateEntity("Well");zone.AddEntity(extra,X+3,Y);Assert.True(world.TryResolvePresentation(zone,X+3,Y,out var e));Assert.AreEqual("well",e.ForegroundTile.sprite.texture.name);var cover=content.Factory.CreateEntity("Player");zone.AddEntity(cover,X,Y);Assert.AreSame(cover,View().Owner);Assert.True(world.TryGetPresentationSubjectSprite(zone,well,X,Y,out var s,out _));Assert.AreSame(Atlas,s.texture);}
        [Test] public void SubmittedPaintKeepsBaseColorsAndExistingSingleEventCues()
        {
            // Native detached fixtures intentionally activate their matching registry for the existing WellSite Render handler.
            var probe=new Probe();well.AddPart(probe);
            using(var paint=new FlatPixelSharedCampPropsTests.Paint(world,zone))
            {typeof(SettlementManager).GetMethod("Activate",I).Invoke(manager.SettlementManager,null);foreach(var stage in new[]{RepairStage.Fouled,RepairStage.TemporarilyPurified,RepairStage.StableRepair,RepairStage.ImprovedWithCaretaker})
             {Stage(stage);typeof(WellSitePart).GetField("_renderFrameCounter",I).SetValue(well.GetPart<WellSitePart>(),0);int n=probe.Renders;paint.Dirty(X,Y);paint.Step();Assert.AreEqual(n+1,probe.Renders);Assert.AreEqual(QudColorParser.Parse(well.GetPart<RenderPart>().ColorString),paint.Tint(X,Y),stage+" native="+View().NativeForeground+" cue="+View().PreserveRenderColorCue+" authored="+View().PreserveAuthoredNativeColor);Native(stage>=RepairStage.StableRepair?1:0);Assert.AreEqual(n+1,probe.Renders);paint.Dirty(X+1,Y);paint.Step();Assert.AreEqual(n+1,probe.Renders);}
             Stage(RepairStage.Fouled);typeof(WellSitePart).GetField("_renderFrameCounter",I).SetValue(well.GetPart<WellSitePart>(),7);paint.Dirty(X,Y);paint.Step();Assert.AreEqual(QudColorParser.Parse("&r"),paint.Tint(X,Y));
             Stage(RepairStage.TemporarilyPurified);site.ResolvedAtTurn=0;site.RelapseAtTurn=100;manager.SettlementManager.SetCurrentTurnProvider(()=>60);paint.Dirty(X,Y);paint.Step();Assert.AreEqual(QudColorParser.Parse("&Y"),paint.Tint(X,Y));}
        }
        [Test] public void OptionalMissingAtlasCannotReloadAndDoesNotDestroyBorrowedArt()
        {var type=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelWellmeetWellCatalog");Assert.NotNull(type);foreach(var t in new Texture2D[]{null,new Texture2D(16,16)}){var a=(IDisposable)Activator.CreateInstance(type,new object[]{t});typeof(FlatPixelGreyboxCatalog).GetField("_wellmeetWellArt",I).SetValue(world,a);Refused();a.Dispose();a.Dispose();if(t!=null){Assert.True(t!=null);UnityEngine.Object.DestroyImmediate(t);}}}
        [TestCase("hidden")][TestCase("stale")][TestCase("carried")][TestCase("empty-stack")][TestCase("foreign-stack")]
        public void UndisclosedDuplicateDoesNotChangeVisibleMainWell(string fault)
        {Native(0);var other=content.Factory.CreateEntity("Well");other.AddPart(new WellSitePart());if(fault=="stale")zone.GetCell(X,Y).Objects.Add(other);else zone.AddEntity(other,X,Y);if(fault=="hidden")other.GetPart<RenderPart>().Visible=false;if(fault=="carried")other.GetPart<PhysicsPart>().InInventory=new Entity();if(fault=="empty-stack")other.AddPart(new StackerPart{StackCount=0});if(fault=="foreign-stack"){var stack=new StackerPart{StackCount=1};other.AddPart(stack);stack.ParentEntity=new Entity();}Native(0);}
        [TestCase("empty")][TestCase("foreign")][TestCase("duplicate")]
        public void InvalidStackCannotDivergeBetweenWorldAndReader(string fault)
        {var stack=new StackerPart{StackCount=1};well.AddPart(stack);Native(0);if(fault=="empty")stack.StackCount=0;if(fault=="foreign")stack.ParentEntity=new Entity();if(fault=="duplicate")well.AddPart(new StackerPart());Refused();}
        [TestCase(typeof(CampfirePart))][TestCase(typeof(ForgePart))][TestCase(typeof(RootFacePart))]
        [TestCase(typeof(SeventhPositionPart))][TestCase(typeof(FoundingPlumePart))][TestCase(typeof(HearthPatchPart))]
        public void AdditionalServicesAndEndingSemanticsCannotBorrowLining(Type type){Native(0);well.AddPart((Part)Activator.CreateInstance(type));Refused();}
        [Test] public void StockAndRenamingDoNotInventStructuralDamage()
        {foreach(var stage in new[]{RepairStage.Fouled,RepairStage.TemporarilyPurified,RepairStage.StableRepair,RepairStage.ImprovedWithCaretaker}){Stage(stage);well.GetPart<WellPart>().Stock=0;well.GetPart<RenderPart>().DisplayName="Custom known well";Native(stage>=RepairStage.StableRepair?1:0);Assert.Zero(well.GetPart<WellPart>().Stock);}}
        [Test] public void AVisibleCompetingSiteRefusesTheAmbiguousCell()
        {Native(0);var other=content.Factory.CreateEntity("Well");other.AddPart(new WellSitePart());zone.AddEntity(other,X,Y);Refused();}
        [Test] public void CurrentSingleCellFootprintMustMatchItsRegistration()
        {zone.RemoveEntity(well);var p=new SpatialFootprintPart{CellsRaw="0,0;1,0"};well.AddPart(p);Assert.True(zone.AddEntity(well,X,Y));Refused();p.CellsRaw="0,0";Refused();Assert.True(zone.TryChangeFootprint(well,"0,0"));Native(0);}
        [Test] public void SavedLairCannotUseVillagePresentation()
        {Native(0);var p=manager.WorldMap.GetPOI(8,16);manager.WorldMap.SetPOI(8,16,new PointOfInterest(POIType.Lair,"lair",null,1,"DesertProwler"));LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(8,16,p);Refused();}
        sealed class Probe:Part{public int Renders;public override bool HandleEvent(GameEvent e){if(e.ID=="Render")Renders++;return true;}}
    }
}
