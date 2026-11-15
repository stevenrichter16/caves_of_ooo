using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Paint = CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelSumpholdShadowTests
    {
        const int X=10,Y=10;DensityLootTestScope content;OverworldZoneManager manager;Zone zone;
        FlatPixelGreyboxCatalog world;Entity wall,floor;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);
            zone=new Zone(SumpholdCompositionPlan.ZoneID);manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            world=new FlatPixelGreyboxCatalog();wall=Add("SandstoneWall",X,Y);
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        Entity Add(string name,int x,int y){var e=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(e,x,y));return e;}
        void Floor(string name="Floor"){floor=Add(name,X,Y+1);}
        void On(Paint p)
        {
            p.Step();var t=p.Shade.GetTile<Tile>(Paint.Position(X,Y));Assert.NotNull(t,"Sumphold native masonry has no shade");
            Assert.AreSame(p.At(X,Y),t);Assert.IsNull(p.Shade.GetTile(Paint.Position(X,Y+1)));
            Assert.AreEqual(new Color(0,0,0,.16f),p.Shade.GetColor(Paint.Position(X,Y)));
        }
        void Off(Paint p){p.Step();Assert.IsNull(p.Shade.GetTile(Paint.Position(X,Y)));}
        [TestCase("Floor")] [TestCase("StoneFloor")]
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
        [TestCase("Player")] [TestCase("Dagger")] [TestCase("VillageDoor")] [TestCase("WaterPuddle")] [TestCase("Duckboard")] [TestCase("StoneFloor")]
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
        [TestCase("profile")] [TestCase("name")] [TestCase("graph")] [TestCase("map")] [TestCase("alias")] [TestCase("biome")] [TestCase("type")]
        public void WarmAuthorityRevocationClearsThePilot(string change)
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);switch(change)
                {
                    case "biome":manager.WorldMap.Tiles[15,6]=BiomeType.Sodden;break;
                    case "type":manager.WorldMap.SetPOI(15,6,new PointOfInterest(POIType.Lair,"Sumphold",profile:"Boatyard"));break;
                    case "profile":manager.WorldMap.GetPOI(15,6).Profile="another place";break;
                    case "name":manager.WorldMap.GetPOI(15,6).Name="another place";break;
                    case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                    case "map":manager.WorldMap.POIs=new PointOfInterest[1,1];break;
                    case "alias":zone.ZoneID="Overworld.015.6.0";break;
                }
                p.Renderer.MarkDirty("authority changed");Off(p);
            }
        }
        [Test] public void SavedSurfaceLairRevokesShade()
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);var poi=manager.WorldMap.GetPOI(15,6);
                manager.WorldMap.SetPOI(15,6,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));
                LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,zone.ZoneID));
                manager.WorldMap.SetPOI(15,6,poi);p.Renderer.MarkDirty("saved column");Off(p);
            }
        }
        [TestCase("Floor","yard")][TestCase("StoneFloor","floors")]
        public void MissingReceiverAtlasCannotBorrowTheValidWallAtlas(string material,string fieldName)
        {
            Floor(material);
            using(var p=new Paint(world,zone))
            {
                p.Step();
                var art=typeof(FlatPixelGreyboxCatalog).GetField("_settlementGroundArt",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
                Assert.NotNull(art);
                var field=art.GetType().GetField(fieldName,BindingFlags.Instance|BindingFlags.NonPublic);var saved=field.GetValue(art);
                try { On(p);field.SetValue(art,null);p.Renderer.MarkDirty("receiver views lost");p.Step();Assert.NotNull(p.At(X,Y));Off(p); }
                finally { field.SetValue(art,saved); }
            }
        }
        [TestCase("RoadStone")][TestCase("Grass")][TestCase("Sand")]
        public void OtherGroundMaterialsRemainOutsideThisBoundedPilot(string material)
        { Floor(material);using(var p=new Paint(world,zone)){p.Step();Assert.NotNull(p.At(X,Y));Off(p);} }
        [Test] public void RemovingSourceClearsOnlyItsShadow()
        { Floor();using(var p=new Paint(world,zone)){On(p);zone.RemoveEntity(wall);p.Dirty(X,Y);Off(p);Assert.NotNull(p.GroundAt(X,Y+1));} }
        [Test] public void AnotherBoatyardDoesNotAcquireThePilot()
        {
            zone=new Zone("Overworld.13.7.0");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            manager.WorldMap.GetPOI(13,7).Profile="Boatyard";wall=Add("SandstoneWall",X,Y);Floor();
            using(var p=new Paint(world,zone)){p.Step();Assert.NotNull(p.At(X,Y));Off(p);}
        }
        [TestCase("solid")][TestCase("material")][TestCase("health")][TestCase("thermal")]
        public void ChangedWallRefusesShadow(string change)
        {
            Floor();using(var p=new Paint(world,zone))
            {
                On(p);switch(change)
                {
                    case "solid":wall.GetPart<PhysicsPart>().Solid=false;break;
                    case "material":wall.GetPart<MaterialPart>().MaterialID="Wood";break;
                    case "health":wall.GetPart<DestructiblePart>().HP=0;break;
                    case "thermal":wall.GetPart<ThermalPart>().Temperature=10000;break;
                }
                p.Dirty(X,Y);Off(p);
            }
        }

    }
}
