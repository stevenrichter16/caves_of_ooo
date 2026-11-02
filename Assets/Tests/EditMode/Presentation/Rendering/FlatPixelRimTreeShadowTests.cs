using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Paint = CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelRimTreeShadowTests
    {
        const int X = 10, Y = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Entity tree, floor;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = new Zone("Overworld.2.3.0"); manager.SetActiveZone(zone);
            foreach (var cell in zone.Cells) cell.Explored = cell.IsVisible = true;
            world = new FlatPixelGreyboxCatalog(); floor = Add("TepuiStone"); tree = Add("Tree");
        }
        [TearDown] public void Cleanup() { world?.Dispose(); content?.Dispose(); }
        Entity Add(string name) { var e = content.Factory.CreateEntity(name); Assert.True(zone.AddEntity(e, X, Y)); return e; }
        void On(Paint p)
        {
            p.Step(); var tile = p.Shade.GetTile<Tile>(Paint.Position(X, Y));
            Assert.NotNull(tile, "Ordinary Rim Forest tree must have bounded root contact shade");
            Assert.AreSame(Resources.Load<Sprite>("Sprites/Actors/actor_shadow"), tile.sprite);
            Assert.AreEqual(new Color(0, 0, 0, .18f), p.Shade.GetColor(Paint.Position(X, Y)));
        }
        void Off(Paint p) { p.Step(); Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y))); }
        [Test] public void ContactStaysWithinStoneCellAndReversibleWithoutGameplayMutation()
        {
            using (var p = new Paint(world, zone))
            {
                int version = zone.EntityVersion; string state = zone.TileState.ToSaveString(); On(p);
                var shade = p.ShadeBounds(X, Y); var cell = p.CellBounds(X, Y);
                Assert.GreaterOrEqual(shade.min.x, cell.min.x); Assert.LessOrEqual(shade.max.x, cell.max.x);
                Assert.GreaterOrEqual(shade.min.y, cell.min.y); Assert.LessOrEqual(shade.max.y, cell.max.y);
                Assert.AreEqual(0, p.Shade.GetComponent<TilemapRenderer>().sortingOrder);
                var body = p.At(X, Y); p.Environment.FlatTreeContactShadowEnabled = false; Off(p);
                Assert.AreSame(body, p.At(X, Y)); p.Environment.FlatTreeContactShadowEnabled = true; On(p);
                p.Renderer.FlatPixelModeEnabled = false; Off(p); p.Renderer.FlatPixelModeEnabled = true; On(p);
                Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(state, zone.TileState.ToSaveString());
            }
        }
        [TestCase("memory")] [TestCase("unexplored")] [TestCase("source-hidden")] [TestCase("source-variant")]
        [TestCase("dead")] [TestCase("source-tint")] [TestCase("floor-hidden")] [TestCase("floor-variant")]
        [TestCase("floor-removed")] [TestCase("floor-solid")] [TestCase("heat")] [TestCase("oil")]
        [TestCase("poi")] [TestCase("graph")] [TestCase("alias")] [TestCase("map")]
        public void ChangedMeaningDisclosureOrAuthorityWithdrawsContact(string change)
        {
            using (var p = new Paint(world, zone))
            {
                On(p);
                switch (change)
                {
                    case "memory": zone.GetCell(X, Y).IsVisible = false; break;
                    case "unexplored": zone.GetCell(X, Y).IsVisible = zone.GetCell(X, Y).Explored = false; break;
                    case "source-hidden": tree.GetPart<RenderPart>().Visible = false; break;
                    case "source-variant": tree.GetPart<RenderPart>().VisualVariant = "foreign"; break;
                    case "dead": tree.GetPart<DestructiblePart>().HP = 0; break;
                    case "source-tint": tree.GetPart<RenderPart>().ColorString = "&R"; break;
                    case "floor-hidden": floor.GetPart<RenderPart>().Visible = false; break;
                    case "floor-variant": floor.GetPart<RenderPart>().VisualVariant = "foreign"; break;
                    case "floor-removed": zone.RemoveEntity(floor); break;
                    case "floor-solid": floor.GetPart<PhysicsPart>().Solid = true; break;
                    case "heat": zone.TileState.AddHeat(X, Y, 1); break;
                    case "oil": zone.TileState.WriteCoating(X, Y, "oil", 3); break;
                    case "poi": manager.WorldMap.SetPOI(2, 3, new PointOfInterest(POIType.MerchantCamp, "camp")); break;
                    case "graph": manager.CachedZones[zone.ZoneID] = new Zone(zone.ZoneID); break;
                    case "alias": zone.ZoneID = "Overworld.02.3.0"; break;
                    case "map": manager.WorldMap.POIs = new PointOfInterest[1, 1]; break;
                }
                p.Dirty(X, Y); p.Renderer.MarkDirty("Rim shadow authority"); Off(p);
            }
        }
        [TestCase("Dagger")] [TestCase("Bush")] [TestCase("Player")] [TestCase("TepuiStone")]
        public void DisclosedCompetitorsWithdrawWhileHiddenOnesDoNotLeak(string name)
        {
            using (var p = new Paint(world, zone))
            {
                On(p); var e = Add(name); e.GetPart<RenderPart>().Visible = false; p.Dirty(X, Y); On(p);
                e.GetPart<RenderPart>().Visible = true; p.Dirty(X, Y); Off(p);
                zone.RemoveEntity(e); p.Dirty(X, Y); On(p);
                var tile = p.Shade.GetTile(Paint.Position(X, Y)); p.Renderer.MarkDirty("full parity"); On(p);
                Assert.AreSame(tile, p.Shade.GetTile(Paint.Position(X, Y)));
                zone.RemoveEntity(tree); p.Dirty(X, Y); Off(p); p.Renderer.MarkDirty("removed full parity"); Off(p);
            }
        }
        [Test] public void MissingStoneArtWithdrawsOnlyShadowAndRestores()
        {
            using (var p = new Paint(world, zone))
            {
                On(p); var body = p.At(X, Y);
                var catalog = typeof(FlatPixelGreyboxCatalog).GetField("_stumpMaterialsArt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(world);
                var field = catalog.GetType().GetField("materials", BindingFlags.Instance | BindingFlags.NonPublic); var tiles = field.GetValue(catalog);
                try { field.SetValue(catalog, null); p.Renderer.MarkDirty("missing stone"); Off(p); Assert.AreSame(body, p.At(X, Y)); }
                finally { field.SetValue(catalog, tiles); }
                p.Renderer.MarkDirty("restore stone"); On(p);
            }
        }
        [Test] public void OtherOrdinaryStumpChunksRemainOutsidePilot()
        {
            using (var p = new Paint(world, zone))
            {
                On(p); var other = new Zone("Overworld.2.5.0"); manager.SetActiveZone(other);
                foreach (var cell in other.Cells) cell.Explored = cell.IsVisible = true;
                zone.RemoveEntity(tree); zone.RemoveEntity(floor); other.AddEntity(tree, X, Y); other.AddEntity(floor, X, Y);
                p.Renderer.SetZone(other); Off(p);
            }
        }
    }
}
