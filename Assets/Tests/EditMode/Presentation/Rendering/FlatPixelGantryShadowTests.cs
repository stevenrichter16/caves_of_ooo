using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Paint = CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelGantryShadowTests
    {
        const int X=10,Y=10;DensityLootTestScope content;OverworldZoneManager manager;Zone zone;
        FlatPixelGreyboxCatalog world;Entity wall,floor;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=new Zone(GantryCompositionPlan.ZoneID);manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            world=new FlatPixelGreyboxCatalog();wall=Add("GantryTimberWall",X,Y);
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        Entity Add(string name,int x,int y){var e=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(e,x,y));return e;}
        void Floor(string name="Floor"){floor=Add(name,X,Y+1);}
        void On(Paint p)
        {
            p.Step();var t=p.Shade.GetTile<Tile>(Paint.Position(X,Y));Assert.NotNull(t,"Gantry native timber has no shade");
            Assert.AreSame(p.At(X,Y),t);Assert.IsNull(p.Shade.GetTile(Paint.Position(X,Y+1)));
            Assert.AreEqual(new Color(0,0,0,.16f),p.Shade.GetColor(Paint.Position(X,Y)));
        }
        void Off(Paint p){p.Step();Assert.IsNull(p.Shade.GetTile(Paint.Position(X,Y)));}
        [TestCase("Floor")] [TestCase("StoneFloor")] [TestCase("RoadStone")] [TestCase("Grass")]
        public void CanonicalDryReceiversGetOnlyShortSouthShade(string material)
        {
            Floor(material);using(var p=new Paint(world,zone))
            {
                int version=zone.EntityVersion;var state=zone.TileState.ToSaveString();On(p);
                Assert.AreEqual(0,p.Shade.GetComponent<TilemapRenderer>().sortingOrder);
                Assert.That(Vector3.Distance(p.ShadeBounds(X,Y).center-p.BodyBounds(X,Y).center,new Vector3(0,-.125f,0)),Is.LessThan(.0001f));
                Assert.True(zone.GetCell(X,Y).BlocksMovement());Assert.False(zone.GetCell(X,Y+1).BlocksMovement());
                Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
                p.Environment.FlatWallShadowEnabled=false;Off(p);p.Environment.FlatWallShadowEnabled=true;On(p);
            }
        }
        [TestCase("Player")] [TestCase("Dagger")] [TestCase("VillageDoor")] [TestCase("WaterPuddle")]
        public void OccupancyWithdrawsAndRemovalRestoresWithFullParity(string name)
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);var e=Add(name,X,Y+1);p.Dirty(X,Y+1);Off(p);
                Assert.True(zone.RemoveEntity(e));p.Dirty(X,Y+1);On(p);var t=p.Shade.GetTile(Paint.Position(X,Y));
                p.Renderer.MarkDirty("full control");p.Step();Assert.AreSame(t,p.Shade.GetTile(Paint.Position(X,Y)));
            }
        }
        [TestCase("source-hidden")] [TestCase("source-memory")] [TestCase("receiver-hidden")] [TestCase("receiver-memory")]
        [TestCase("oil")] [TestCase("changed")] [TestCase("heat")]
        public void DisclosureAndLiveStateRemoveOnlyShade(string change)
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);var originalWall=p.At(X,Y);switch(change)
                {
                    case "source-hidden":wall.GetPart<RenderPart>().Visible=false;break;
                    case "source-memory":zone.GetCell(X,Y).IsVisible=false;break;
                    case "receiver-hidden":floor.GetPart<RenderPart>().Visible=false;break;
                    case "receiver-memory":zone.GetCell(X,Y+1).IsVisible=false;break;
                    case "oil":zone.TileState.WriteCoating(X,Y+1,"oil",5);break;
                    case "heat":zone.TileState.AddHeat(X,Y+1,1);break;
                    case "changed":floor.GetPart<RenderPart>().VisualVariant="changed";break;
                }
                p.Dirty(X,Y);p.Dirty(X,Y+1);Off(p);
                if(!change.StartsWith("source-"))Assert.AreSame(originalWall,p.At(X,Y));
            }
        }
        [Test] public void HiddenOccupantsCannotAffectTheDisclosedShadow()
        {
            Floor();using(var p=new Paint(world,zone))
            {On(p);var e=Add("Dagger",X,Y+1);e.GetPart<RenderPart>().Visible=false;p.Dirty(X,Y+1);On(p);}
        }
        [TestCase("profile")] [TestCase("name")] [TestCase("graph")] [TestCase("map")] [TestCase("alias")]
        public void WarmAuthorityRevocationClearsThePilot(string change)
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);switch(change)
                {
                    case "profile":manager.WorldMap.GetPOI(7,8).Profile="another place";break;
                    case "name":manager.WorldMap.GetPOI(7,8).Name="another place";break;
                    case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                    case "map":manager.WorldMap.POIs=new PointOfInterest[1,1];break;
                    case "alias":zone.ZoneID="Overworld.007.8.0";break;
                }
                p.Renderer.MarkDirty("authority changed");Off(p);
            }
        }
        [Test] public void SavedSurfaceLairRevokesShade()
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);var poi=manager.WorldMap.GetPOI(7,8);
                manager.WorldMap.SetPOI(7,8,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));
                LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,zone.ZoneID));
                manager.WorldMap.SetPOI(7,8,poi);p.Renderer.MarkDirty("saved column");Off(p);
            }
        }
        [Test] public void MissingReceiverArtLeavesTheApprovedTimberBody()
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);var body=p.At(X,Y);
                var art=typeof(FlatPixelGreyboxCatalog).GetField("_settlementGroundArt",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
                var field=art.GetType().GetField("yard",BindingFlags.Instance|BindingFlags.NonPublic);var original=field.GetValue(art);
                try{field.SetValue(art,null);p.Renderer.MarkDirty("missing receiver");Off(p);Assert.AreSame(body,p.At(X,Y));}
                finally{field.SetValue(art,original);}
                p.Renderer.MarkDirty("restore receiver");On(p);
            }
        }
        [TestCase("variant")] [TestCase("nonsolid")] [TestCase("dead")] [TestCase("material")] [TestCase("source-oil")]
        public void ChangedTimberMeaningClearsShade(string change)
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);switch(change)
                {
                    case "variant":wall.GetPart<RenderPart>().VisualVariant="foreign";break;
                    case "nonsolid":wall.GetPart<PhysicsPart>().Solid=false;break;
                    case "dead":wall.GetPart<DestructiblePart>().HP=0;break;
                    case "material":wall.GetPart<MaterialPart>().MaterialID="Stone";break;
                    case "source-oil":zone.TileState.WriteCoating(X,Y,"oil",3);break;
                }
                p.Dirty(X,Y);Off(p);
            }
        }
        [Test] public void LegacyAndMissingShaderClearShadeWithoutChangingTheBorrowedBody()
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);var body=p.At(X,Y);var f=typeof(EnvironmentSpriteRenderer).GetField("_flatGroundShadowMaterial",BindingFlags.Instance|BindingFlags.NonPublic);var original=f.GetValue(p.Environment);
                try{f.SetValue(p.Environment,null);p.Renderer.MarkDirty("missing shader");Off(p);Assert.AreSame(body,p.At(X,Y));}
                finally{f.SetValue(p.Environment,original);}
                p.Renderer.MarkDirty("shader restored");On(p);p.Renderer.FlatPixelModeEnabled=false;Off(p);p.Renderer.FlatPixelModeEnabled=true;On(p);
            }
        }
    }
}
