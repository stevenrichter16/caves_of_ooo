using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Paint=CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelGantryMemoryTests
    {
        DensityLootTestScope content;OverworldZoneManager manager;Zone zone;FlatPixelGreyboxCatalog world;Entity wall;
        const int X=10,Y=10;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=new Zone(GantryCompositionPlan.ZoneID);manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            wall=Add("GantryTimberWall",X,Y);world=new FlatPixelGreyboxCatalog();
        }
        Entity Add(string name,int x,int y){var e=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        bool Native(FlatPixelGreyboxCell v)=>v.ForegroundTile?.sprite?.texture==Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath);
        void Memory(){zone.GetCell(X,Y).IsVisible=false;}
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        [Test] public void ExistingRememberedTimberKeepsLiveFrameWithExactlyOneDimAndNoShadow()
        {
            var live=View();Assert.True(Native(live));Memory();var m=View();
            Assert.True(m.Remembered);Assert.True(Native(m),"Remembered timber still becomes ASCII");Assert.AreSame(wall,m.Owner);
            Assert.AreSame(live.ForegroundTile,m.ForegroundTile);Assert.True(m.NativeForeground);Assert.False(m.BlocksMovement);
            Assert.AreEqual(new Color(.35f,.35f,.35f,1),m.ForegroundColor);
            using(var p=new Paint(world,zone)){p.Step();Assert.AreSame(m.ForegroundTile,p.At(X,Y));Assert.IsNull(p.Shade.GetTile(Paint.Position(X,Y)));}
            zone.GetCell(X,Y).IsVisible=true;Assert.AreSame(live.ForegroundTile,View().ForegroundTile);
        }
        [Test] public void MemoryDoesNotReadHiddenDamageTemperatureSolidityOccupantsOrNeighbors()
        {
            Memory();var before=View();Assert.True(Native(before),"Memory baseline required");
            wall.GetPart<DestructiblePart>().HP=0;wall.GetPart<ThermalPart>().Temperature=1000;wall.GetPart<PhysicsPart>().Solid=false;
            zone.TileState.AddHeat(X,Y,4);zone.TileState.WriteCoating(X,Y,"oil",4);Add("Player",X,Y);Add("GantryTimberWall",X+1,Y);
            var after=View();Assert.AreSame(wall,after.Owner);Assert.AreSame(before.ForegroundTile,after.ForegroundTile);Assert.AreEqual(before.ForegroundColor,after.ForegroundColor);
        }
        [TestCase("unexplored")] [TestCase("hidden-render")] [TestCase("variant")] [TestCase("glyph")] [TestCase("crop")] [TestCase("takeable")]
        [TestCase("stale")] [TestCase("profile")] [TestCase("name")] [TestCase("alias")] [TestCase("other-zone")]
        public void UnsupportedIdentityOrAuthorityKeepsConservativeFallback(string fault)
        {
            Memory();Assert.True(Native(View()),"Memory baseline required");
            switch(fault)
            {
                case "unexplored":zone.GetCell(X,Y).Explored=false;break;
                case "hidden-render":wall.GetPart<RenderPart>().Visible=false;break;
                case "variant":wall.GetPart<RenderPart>().VisualVariant="foreign";break;
                case "glyph":wall.GetPart<RenderPart>().RenderString="!";break;
                case "crop":wall.AddPart(new CropPart());break;
                case "takeable":wall.GetPart<PhysicsPart>().Takeable=true;break;
                case "stale":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "profile":manager.WorldMap.GetPOI(7,8).Profile="other";break;
                case "name":manager.WorldMap.GetPOI(7,8).Name="other";break;
                case "alias":zone.ZoneID="Overworld.007.8.0";break;
                case "other-zone":zone.ZoneID="Overworld.7.9.0";break;
            }
            Assert.False(world.TryResolvePresentation(zone,X,Y,out var v)&&Native(v));
        }
        [TestCase("TentWall")] [TestCase("Crate")] [TestCase("VillageDoor")] [TestCase("Player")]
        public void ThisPilotDoesNotPromoteOtherRememberedProps(string name)
        {
            zone.RemoveEntity(wall);Add("Grass",X,Y);Add(name,X,Y);Memory();var v=View();Assert.False(Native(v));
            Assert.True(world.TryResolveFallback(zone,X,Y,out var fallback));Assert.AreSame(fallback.Owner,v.Owner);
        }
        [Test] public void MissingOptionalArtAndDisposedCatalogRetainFallbackAndBorrowedTexture()
        {
            Memory();Assert.True(Native(View()));var borrowed=Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath);
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Assert.False(Native(View()));world.Dispose();Assert.True(borrowed!=null);
        }
    }
}
