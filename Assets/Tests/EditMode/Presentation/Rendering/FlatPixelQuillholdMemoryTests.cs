using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo;
using NUnit.Framework;
using UnityEngine;
using Paint=CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelQuillholdMemoryTests
    {
        DensityLootTestScope content;OverworldZoneManager manager;Zone zone;FlatPixelGreyboxCatalog world;Entity wall;
        const int X=10,Y=10;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=new Zone(QuillholdCompositionPlan.ZoneID);manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            wall=Add("QuillholdArchiveWall",X,Y);world=new FlatPixelGreyboxCatalog();
        }
        Entity Add(string name,int x,int y){var e=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        bool Native(FlatPixelGreyboxCell v)=>v.ForegroundTile?.sprite?.texture==Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath);
        void Memory(){zone.GetCell(X,Y).IsVisible=false;}
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        [Test] public void ExistingRememberedMarbleKeepsLiveFrameWithExactlyOneDimAndNoShadow()
        {
            var live=View();Assert.True(Native(live));Assert.AreEqual(new Rect(160,288,16,24),live.ForegroundTile.sprite.rect);Memory();var m=View();
            Assert.True(m.Remembered);Assert.True(Native(m),"Remembered marble still becomes ASCII");Assert.AreSame(wall,m.Owner);
            Assert.AreSame(live.ForegroundTile,m.ForegroundTile);Assert.True(m.NativeForeground);Assert.False(m.BlocksMovement);
            Assert.AreEqual(new Color(.35f,.35f,.35f,1),m.ForegroundColor);
            using(var p=new Paint(world,zone)){p.Step();Assert.AreSame(m.ForegroundTile,p.At(X,Y));Assert.IsNull(p.Shade.GetTile(Paint.Position(X,Y)));}
            zone.GetCell(X,Y).IsVisible=true;Assert.AreSame(live.ForegroundTile,View().ForegroundTile);
        }
        [Test] public void MemoryDoesNotReadHiddenDamageTemperatureSolidityOccupantsOrNeighbors()
        {
            Memory();var before=View();Assert.True(Native(before),"Memory baseline required");
            wall.GetPart<DestructiblePart>().HP=0;wall.GetPart<DestructiblePart>().Gone=true;wall.GetPart<ThermalPart>().Temperature=1000;wall.GetPart<PhysicsPart>().Solid=false;
            zone.TileState.AddHeat(X,Y,4);zone.TileState.WriteCoating(X,Y,"oil",4);Add("Player",X,Y);Add("QuillholdArchiveWall",X+1,Y);
            var after=View();Assert.AreSame(wall,after.Owner);Assert.AreSame(before.ForegroundTile,after.ForegroundTile);Assert.AreEqual(before.ForegroundColor,after.ForegroundColor);
        }
        [TestCase("unexplored")] [TestCase("hidden-render")] [TestCase("variant")] [TestCase("glyph")] [TestCase("crop")] [TestCase("takeable")]
        [TestCase("biome")] [TestCase("type")] [TestCase("lair")] [TestCase("depth")] [TestCase("footprint")]
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("foreign-material")] [TestCase("foreign-destructible")]
        [TestCase("material")] [TestCase("custody")] [TestCase("tile-color")] [TestCase("gas")] [TestCase("tile-source")] [TestCase("brain")] [TestCase("body")] [TestCase("step-trigger")] [TestCase("sealed")] [TestCase("door")] [TestCase("lock")] [TestCase("container")]
        [TestCase("stale")] [TestCase("profile")] [TestCase("name")] [TestCase("alias")] [TestCase("other-zone")]
        public void UnsupportedIdentityOrAuthorityKeepsConservativeFallback(string fault)
        {
            Memory();Assert.True(Native(View()),"Memory baseline required");
            switch(fault)
            {
                case "biome":manager.WorldMap.Tiles[14,9]=BiomeType.Grovelands;break;
                case "type":manager.WorldMap.GetPOI(14,9).Type=POIType.MerchantCamp;break;
                case "lair":var poi=manager.WorldMap.GetPOI(14,9);manager.WorldMap.SetPOI(14,9,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(14,9,poi);break;
                case "depth":zone.ZoneID="Overworld.14.9.1";break;
                case "footprint":wall.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});break;
                case "foreign-render":wall.GetPart<RenderPart>().ParentEntity=new Entity();break;
                case "foreign-physics":wall.GetPart<PhysicsPart>().ParentEntity=new Entity();break;
                case "foreign-material":wall.GetPart<MaterialPart>().ParentEntity=new Entity();break;
                case "foreign-destructible":wall.GetPart<DestructiblePart>().ParentEntity=new Entity();break;
                case "material":wall.GetPart<MaterialPart>().MaterialID="Wood";break;
                case "custody":wall.GetPart<PhysicsPart>().InInventory=new Entity();break;
                case "tile-color":wall.GetPart<RenderPart>().TileColor="&R";break;
                case "gas":wall.AddPart(new GasPoolPart());break;
                case "tile-source":wall.AddPart(new TileStateSourcePart());break;
                case "brain":wall.AddPart(new BrainPart());break;
                case "body":wall.AddPart(new Body());break;
                case "step-trigger":wall.AddPart(new RuneFlameTriggerPart());break;
                case "sealed":wall.AddPart(new SealedLibraryBarrierPart());break;
                case "door":wall.AddPart(new DoorPart());break;
                case "lock":wall.AddPart(new LockPart());break;
                case "container":wall.AddPart(new ContainerPart());break;
                case "unexplored":zone.GetCell(X,Y).Explored=false;break;
                case "hidden-render":wall.GetPart<RenderPart>().Visible=false;break;
                case "variant":wall.GetPart<RenderPart>().VisualVariant="foreign";break;
                case "glyph":wall.GetPart<RenderPart>().RenderString="!";break;
                case "crop":wall.AddPart(new CropPart());break;
                case "takeable":wall.GetPart<PhysicsPart>().Takeable=true;break;
                case "stale":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "profile":manager.WorldMap.GetPOI(14,9).Profile="other";break;
                case "name":manager.WorldMap.GetPOI(14,9).Name="other";break;
                case "alias":zone.ZoneID="Overworld.014.9.0";break;
                case "other-zone":zone.ZoneID="Overworld.13.9.0";break;
            }
            Assert.False(world.TryResolvePresentation(zone,X,Y,out var v)&&Native(v));
        }
        [TestCase("QuillholdArchiveShelf")] [TestCase("QuillholdCopyDesk")] [TestCase("LibraryMemoryMarbleWall")] [TestCase("TentWall")] [TestCase("Crate")] [TestCase("VillageDoor")] [TestCase("Player")]
        public void ThisPilotDoesNotPromoteOtherRememberedProps(string name)
        {
            zone.RemoveEntity(wall);Add("Grass",X,Y);Add(name,X,Y);Memory();var v=View();Assert.False(Native(v));
            Assert.True(world.TryResolveFallback(zone,X,Y,out var fallback));Assert.AreSame(fallback.Owner,v.Owner);
        }
        [Test] public void HiddenStatusValuesDoNotRecolorStaticMemory()
        {
            Memory();var before=View();Assert.True(Native(before));
            Assert.True(wall.ForceApplyEffect(new WetEffect(),null,zone));
            Assert.NotNull(wall.GetPart<StatusEffectsPart>().GetRenderColorOverride());
            var after=View();Assert.AreSame(before.ForegroundTile,after.ForegroundTile);
            Assert.AreEqual(before.ForegroundColor,after.ForegroundColor);
        }
        [Test] public void MemoryDoesNotAuthorizeAFormerlyVisibleReader()
        {
            var viewer=Add("Player",X,Y+1);
            var doc=ExamineDocumentBuilder.BuildWorld(wall,zone,zone.GetCell(X,Y),viewer);
            Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc));Memory();Assert.True(Native(View()));
            Assert.False(ExamineDocumentBuilder.IsCurrentSubject(doc));
        }
        [Test] public void RestoringSiteAuthorityRecoversTheSameMemoryFrame()
        {
            Memory();var before=View();Assert.True(Native(before));manager.WorldMap.Tiles[14,9]=BiomeType.Grovelands;
            Assert.False(Native(View()));manager.WorldMap.Tiles[14,9]=BiomeType.Spread;
            Assert.AreSame(before.ForegroundTile,View().ForegroundTile);
        }
        [Test] public void MissingOptionalArtAndDisposedCatalogRetainFallbackAndBorrowedTexture()
        {
            Memory();Assert.True(Native(View()));var borrowed=Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath);
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Assert.False(Native(View()));world.Dispose();Assert.True(borrowed!=null);
        }
    }
}
