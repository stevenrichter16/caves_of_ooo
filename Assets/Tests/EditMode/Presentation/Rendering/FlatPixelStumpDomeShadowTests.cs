using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Paint = CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelStumpDomeShadowTests
    {
        const int X = 10, Y = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Entity dome, floor;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = new Zone("Overworld.3.2.0"); manager.SetActiveZone(zone);
            foreach (var cell in zone.Cells) cell.Explored = cell.IsVisible = true;
            world = new FlatPixelGreyboxCatalog(); Add("TepuiStone", Y);
            dome = Add("StoneDome", Y); floor = Add("TepuiStone", Y + 1);
        }
        [TearDown] public void Cleanup() { world?.Dispose(); content?.Dispose(); }
        Entity Add(string name, int y) { var e = content.Factory.CreateEntity(name); Assert.True(zone.AddEntity(e, X, y)); return e; }
        void On(Paint p)
        {
            p.Step(); var tile = p.Shade.GetTile<Tile>(Paint.Position(X, Y));
            Assert.NotNull(tile, "Selected native Summit Scrub dome must cast bounded contact shade");
            Assert.AreSame(p.At(X, Y), tile);
            Assert.AreEqual(new Color(0, 0, 0, .16f), p.Shade.GetColor(Paint.Position(X, Y)));
            Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y + 1)));
        }
        void Off(Paint p) { p.Step(); Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y))); }
        [Test] public void TwoPixelSouthernStripIsReversibleAndDoesNotMutateGameplay()
        {
            using (var p = new Paint(world, zone))
            {
                int version = zone.EntityVersion; string state = zone.TileState.ToSaveString(); On(p);
                var body = p.BodyBounds(X, Y); var shade = p.ShadeBounds(X, Y);
                Assert.AreEqual(body.size, shade.size);
                Assert.That(Vector3.Distance(shade.center-body.center, new Vector3(0,-2f/16,0)), Is.LessThan(.0001f));
                Assert.That(shade.min.y, Is.EqualTo(p.CellBounds(X,Y).min.y-2f/16).Within(.0001f));
                Assert.AreEqual(0, p.Shade.GetComponent<TilemapRenderer>().sortingOrder);
                Assert.IsNull(p.Shade.GetComponent<TilemapCollider2D>());
                Assert.True(zone.GetCell(X,Y).BlocksMovement()); Assert.False(zone.GetCell(X,Y+1).BlocksMovement());
                var tile = p.At(X,Y); p.Environment.FlatWallShadowEnabled=false; Off(p); Assert.AreSame(tile,p.At(X,Y));
                p.Environment.FlatWallShadowEnabled=true; On(p);
                p.Renderer.FlatPixelModeEnabled=false; Off(p); p.Renderer.FlatPixelModeEnabled=true; On(p);
                Assert.AreEqual(version,zone.EntityVersion); Assert.AreEqual(state,zone.TileState.ToSaveString());
            }
        }
        [TestCase("memory")] [TestCase("unexplored")] [TestCase("source-hidden")] [TestCase("source-variant")]
        [TestCase("source-passable")] [TestCase("source-removed")] [TestCase("source-tint")]
        [TestCase("receiver-memory")] [TestCase("receiver-unexplored")] [TestCase("floor-hidden")]
        [TestCase("floor-variant")] [TestCase("floor-removed")] [TestCase("floor-solid")]
        [TestCase("source-heat")] [TestCase("receiver-oil")] [TestCase("poi")] [TestCase("graph")]
        [TestCase("alias")] [TestCase("map")]
        public void ChangedMeaningDisclosureOrAuthorityWithdrawsShade(string change)
        {
            using(var p=new Paint(world,zone))
            {
                On(p);
                switch(change)
                {
                    case "memory": zone.GetCell(X,Y).IsVisible=false; break;
                    case "unexplored": zone.GetCell(X,Y).IsVisible=zone.GetCell(X,Y).Explored=false; break;
                    case "source-hidden": dome.GetPart<RenderPart>().Visible=false; break;
                    case "source-variant": dome.GetPart<RenderPart>().VisualVariant="foreign"; break;
                    case "source-passable": dome.GetPart<PhysicsPart>().Solid=false; break;
                    case "source-removed": zone.RemoveEntity(dome); break;
                    case "source-tint": dome.GetPart<RenderPart>().ColorString="&R"; break;
                    case "receiver-memory": zone.GetCell(X,Y+1).IsVisible=false; break;
                    case "receiver-unexplored": zone.GetCell(X,Y+1).IsVisible=zone.GetCell(X,Y+1).Explored=false; break;
                    case "floor-hidden": floor.GetPart<RenderPart>().Visible=false; break;
                    case "floor-variant": floor.GetPart<RenderPart>().VisualVariant="foreign"; break;
                    case "floor-removed": zone.RemoveEntity(floor); break;
                    case "floor-solid": floor.GetPart<PhysicsPart>().Solid=true; break;
                    case "source-heat": zone.TileState.AddHeat(X,Y,1); break;
                    case "receiver-oil": zone.TileState.WriteCoating(X,Y+1,"oil",3); break;
                    case "poi": manager.WorldMap.SetPOI(3,2,new PointOfInterest(POIType.MerchantCamp,"camp")); break;
                    case "graph": manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID); break;
                    case "alias": zone.ZoneID="Overworld.03.2.0"; break;
                    case "map": manager.WorldMap.POIs=new PointOfInterest[1,1]; break;
                }
                p.Dirty(X,Y+1); p.Dirty(X,Y); p.Renderer.MarkDirty("Dome counter"); Off(p);
            }
        }
        [TestCase("Dagger")] [TestCase("Bush")] [TestCase("Player")] [TestCase("TepuiStone")] [TestCase("StoneDome")]
        public void ReceiverOnlyDirtyWithdrawsDisclosedOccupantsButDoesNotLeakHiddenOnes(string name)
        {
            using(var p=new Paint(world,zone))
            {
                On(p); var e=Add(name,Y+1);e.GetPart<RenderPart>().Visible=false;p.Dirty(X,Y+1);On(p);
                e.GetPart<RenderPart>().Visible=true;p.Dirty(X,Y+1);Assert.False(p.FullDirty,"Receiver change must exercise incremental paint");Off(p);
                zone.RemoveEntity(e);p.Dirty(X,Y+1);On(p);var tile=p.Shade.GetTile(Paint.Position(X,Y));
                p.Renderer.MarkDirty("full parity");On(p);Assert.AreSame(tile,p.Shade.GetTile(Paint.Position(X,Y)));
            }
        }
        [Test] public void MissingMaterialAtlasFailsClosedAndRestores()
        {
            using(var p=new Paint(world,zone))
            {
                On(p);var catalog=typeof(FlatPixelGreyboxCatalog).GetField("_stumpMaterialsArt",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
                var field=catalog.GetType().GetField("materials",BindingFlags.Instance|BindingFlags.NonPublic);var tiles=field.GetValue(catalog);
                try{field.SetValue(catalog,null);p.Renderer.MarkDirty("missing art");Off(p);}finally{field.SetValue(catalog,tiles);}
                p.Renderer.MarkDirty("restore art");On(p);
            }
        }
        [Test] public void OtherStumpChunkRemainsOutsidePilot()
        {
            using(var p=new Paint(world,zone))
            {
                On(p);var other=new Zone("Overworld.3.3.0");manager.SetActiveZone(other);
                foreach(var cell in other.Cells)cell.Explored=cell.IsVisible=true;
                zone.RemoveEntity(dome);zone.RemoveEntity(floor);other.AddEntity(dome,X,Y);other.AddEntity(floor,X,Y+1);
                p.Renderer.SetZone(other);Off(p);
            }
        }
    }
}
