using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Paint=CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelTreeContactShadowTests
    {
        const int X=10,Y=10;
        DensityLootTestScope content;OverworldZoneManager manager;Zone zone;FlatPixelGreyboxCatalog world;Entity tree,grass;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=new Zone("Overworld.10.15.0");manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            world=new FlatPixelGreyboxCatalog();grass=Add("Grass");tree=Add("Tree");
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        Entity Add(string name){var e=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(e,X,Y));return e;}
        void On(Paint p)
        {
            p.Step();var pos=Paint.Position(X,Y);var tile=p.Shade.GetTile<Tile>(pos);Assert.NotNull(tile,"Tree has no bounded root contact shade");
            Assert.AreSame(Resources.Load<Sprite>("Sprites/Actors/actor_shadow"),tile.sprite);
            Assert.AreEqual(new Color(0,0,0,.18f),p.Shade.GetColor(pos));Assert.AreEqual(0,p.Shade.GetComponent<TilemapRenderer>().sortingOrder);
        }
        void Off(Paint p){p.Step();Assert.IsNull(p.Shade.GetTile(Paint.Position(X,Y)));}
        void Toggle(Paint p,bool enabled)
        {
            var property=typeof(EnvironmentSpriteRenderer).GetProperty("FlatTreeContactShadowEnabled");Assert.NotNull(property);property.SetValue(p.Environment,enabled);
        }
        [Test] public void RootContactStaysInsideTheSourceCellAndBelowItsApprovedBody()
        {
            using(var p=new Paint(world,zone))
            {
                int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();On(p);
                var shade=p.ShadeBounds(X,Y);var cell=p.CellBounds(X,Y);
                Assert.GreaterOrEqual(shade.min.x,cell.min.x-.0001f);Assert.LessOrEqual(shade.max.x,cell.max.x+.0001f);
                Assert.GreaterOrEqual(shade.min.y,cell.min.y-.0001f);Assert.LessOrEqual(shade.max.y,cell.max.y+.0001f);
                Assert.That(Vector3.Distance(shade.center-p.BodyBounds(X,Y).center,new Vector3(1f/16,-10f/16,0)),Is.LessThan(.0001f));
                Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
                var body=p.At(X,Y);Toggle(p,false);Off(p);Assert.AreSame(body,p.At(X,Y));Toggle(p,true);On(p);
                p.Environment.FlatWallShadowEnabled=false;On(p);
            }
        }
        [TestCase("memory")] [TestCase("unexplored")] [TestCase("source-hidden")] [TestCase("source-variant")]
        [TestCase("removed")] [TestCase("dead")] [TestCase("floor-hidden")] [TestCase("floor-variant")]
        [TestCase("floor-removed")] [TestCase("oil")] [TestCase("heat")] [TestCase("grass-solid")]
        [TestCase("poi")] [TestCase("graph")] [TestCase("alias")] [TestCase("map")]
        public void ChangedMeaningDisclosureAndAuthorityClearWarmContact(string change)
        {
            using(var p=new Paint(world,zone))
            {
                On(p);switch(change)
                {
                    case "memory":zone.GetCell(X,Y).IsVisible=false;break;
                    case "unexplored":zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=false;break;
                    case "source-hidden":tree.GetPart<RenderPart>().Visible=false;break;
                    case "source-variant":tree.GetPart<RenderPart>().VisualVariant="foreign";break;
                    case "removed":zone.RemoveEntity(tree);break;
                    case "dead":tree.GetPart<DestructiblePart>().HP=0;break;
                    case "floor-hidden":grass.GetPart<RenderPart>().Visible=false;break;
                    case "floor-variant":grass.GetPart<RenderPart>().VisualVariant="foreign";break;
                    case "floor-removed":zone.RemoveEntity(grass);break;
                    case "oil":zone.TileState.WriteCoating(X,Y,"oil",3);break;
                    case "heat":zone.TileState.AddHeat(X,Y,1);break;
                    case "grass-solid":grass.GetPart<PhysicsPart>().Solid=true;break;
                    case "poi":manager.WorldMap.SetPOI(10,15,new PointOfInterest(POIType.MerchantCamp,"camp"));break;
                    case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                    case "alias":zone.ZoneID="Overworld.010.15.0";break;
                    case "map":manager.WorldMap.POIs=new PointOfInterest[1,1];break;
                }
                p.Dirty(X,Y);p.Renderer.MarkDirty("authority/control");Off(p);
            }
        }
        [TestCase("Dagger")] [TestCase("Bush")] [TestCase("Player")] [TestCase("Grass")]
        public void VisibleCompetitorsWithdrawContactButHiddenOnesCannotRevealThemselves(string name)
        {
            using(var p=new Paint(world,zone))
            {
                On(p);var e=Add(name);e.GetPart<RenderPart>().Visible=false;p.Dirty(X,Y);On(p);
                e.GetPart<RenderPart>().Visible=true;p.Dirty(X,Y);Off(p);zone.RemoveEntity(e);p.Dirty(X,Y);On(p);
                var tile=p.Shade.GetTile(Paint.Position(X,Y));p.Renderer.MarkDirty("full parity");p.Step();Assert.AreSame(tile,p.Shade.GetTile(Paint.Position(X,Y)));
            }
        }
        [TestCase(false)] [TestCase(true)] public void RealWaterCoatingDeclinesEvenWhenItsOwnerIsHidden(bool visible)
        {
            using(var p=new Paint(world,zone))
            {
                On(p);var e=Add("WaterPuddle");e.GetPart<RenderPart>().Visible=visible;
                Assert.False(zone.TileState.Get(X,Y).IsEmpty);p.Dirty(X,Y);Off(p);
            }
        }
        [Test] public void MissingNativeGrassDeclinesWhileTheNativeTreeBodyRemains()
        {
            using(var p=new Paint(world,zone))
            {
                On(p);var body=p.At(X,Y);
                var catalog=typeof(FlatPixelGreyboxCatalog).GetField("_spreadEnvironmentArt",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
                var field=catalog.GetType().GetField("turf",BindingFlags.Instance|BindingFlags.NonPublic);var tiles=field.GetValue(catalog);
                try{field.SetValue(catalog,null);p.Renderer.MarkDirty("missing turf only");Off(p);Assert.AreSame(body,p.At(X,Y));}
                finally{field.SetValue(catalog,tiles);}
                p.Renderer.MarkDirty("restore turf");On(p);
            }
        }
        [Test] public void DamagedTreeRetainsContactButThePilotDoesNotExpandAcrossZones()
        {
            tree.GetPart<DestructiblePart>().HP=28;using(var p=new Paint(world,zone))
            {
                On(p);var other=new Zone("Overworld.10.14.0");manager.SetActiveZone(other);foreach(var c in other.Cells)c.Explored=c.IsVisible=true;
                zone.RemoveEntity(tree);zone.RemoveEntity(grass);other.AddEntity(tree,X,Y);other.AddEntity(grass,X,Y);p.Renderer.SetZone(other);Off(p);
            }
        }
        [Test] public void SavedLairClaimStillExcludesRestoredWildernessLabel()
        {
            using(var p=new Paint(world,zone))
            {
                On(p);manager.WorldMap.SetPOI(10,15,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));
                LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,zone.ZoneID));manager.WorldMap.SetPOI(10,15,null);
                p.Renderer.MarkDirty("saved column");Off(p);
            }
        }
        [Test] public void OptionalMaskAndNativeGroundAreRequiredAndBorrowedMaskSurvivesDisposal()
        {
            var borrowed=Resources.Load<Sprite>("Sprites/Actors/actor_shadow");Tile owned=null;
            using(var p=new Paint(world,zone))
            {
                On(p);var f=typeof(EnvironmentSpriteRenderer).GetField("_flatTreeContactTile",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(f);
                owned=(Tile)f.GetValue(p.Environment);f.SetValue(p.Environment,null);p.Renderer.MarkDirty("missing mask");Off(p);f.SetValue(p.Environment,owned);
                p.Renderer.MarkDirty("restore mask");On(p);using(var empty=new FlatPixelGreyboxCatalog((Texture2D)null))
                {p.Catalog(empty);p.Renderer.MarkDirty("missing native ground/body");Off(p);p.Catalog(world);}
            }
            Assert.True(owned==null,"owned Tile must be destroyed");Assert.True(borrowed!=null&&borrowed.texture!=null,"borrowed source must survive");
        }
        [Test] public void TreeRemovalIncrementalMatchesFullAndLegacyClearsOnlyOwnedShade()
        {
            using(var p=new Paint(world,zone))
            {
                On(p);zone.RemoveEntity(tree);p.Dirty(X,Y);Off(p);p.Renderer.MarkDirty("full parity");Off(p);
                zone.AddEntity(tree,X,Y);p.Dirty(X,Y);On(p);p.Renderer.FlatPixelModeEnabled=false;Off(p);p.Renderer.FlatPixelModeEnabled=true;On(p);
            }
        }
    }
}
