using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class CombatInventoryPresentationTests : FiftyWorldFixture
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        static object Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, Flags).Invoke(target, args);

        [TestCase("FrogOil", "SpreadGrease|")]
        [TestCase("SilverSand", "ScatterGrit|")]
        public void RealSupplyActionClosesInventoryAndQueuesExactlyOneTurn(string blueprint, string prefix)
        { InventoryAction(blueprint, prefix, false); }

        [TestCase("FrogOil", "SpreadGrease|")]
        [TestCase("SilverSand", "ScatterGrit|")]
        public void StaleSupplyActionKeepsMenuAndQueuesNoTurn(string blueprint, string prefix)
        { InventoryAction(blueprint, prefix, true); }

        void InventoryAction(string blueprint, string prefix, bool stale)
        {
            var fixture = GetType().Assembly.GetType("CavesOfOoo.Tests.HotbarSaveFixture");
            using (var scope = (IDisposable)Activator.CreateInstance(fixture, Flags, null, new object[] { false, false }, null))
            {
                var host = new GameObject("combat utility inventory");
                try
                {
                    host.transform.SetParent(((GameObject)Field(scope, "Root")).transform, false);
                    var grid = new GameObject("Grid"); grid.transform.SetParent(host.transform, false); grid.AddComponent<Grid>();
                    var tiles = new GameObject("Tiles"); tiles.transform.SetParent(grid.transform, false);
                    var tilemap = tiles.AddComponent<Tilemap>(); tiles.AddComponent<TilemapRenderer>();
                    foreach (var cell in Zone.Cells) { cell.IsVisible = true; cell.Explored = true; }
                    var item = Carry(blueprint);
                    var ui = host.AddComponent<InventoryUI>(); ui.Tilemap = tilemap; ui.PlayerEntity = Actor; ui.CurrentZone = Zone; ui.Open();
                    Assert.True(ui.ReopenItemActionPopupFor(item));
                    var actions = (IList)Field(Field(ui, "_itemActionPopup"), "Actions");
                    int selected = -1;
                    for (int i = 0; i < actions.Count; i++)
                        if (((string)Field(actions[i], "Command")).StartsWith(prefix, StringComparison.Ordinal)) { selected = i; break; }
                    Assert.That(selected, Is.GreaterThanOrEqualTo(0), "the normal inventory must offer the real utility action");
                    if (stale) Assert.True(Zone.MoveEntity(Actor, 9, 10));
                    Call(ui, "ExecuteItemAction", selected);
                    Assert.AreEqual(stale, ui.IsOpen);
                    Assert.AreEqual(!stale, Call(ui, "ConsumePendingEverydayTurn"));
                    Assert.False((bool)Call(ui, "ConsumePendingEverydayTurn"));
                    Assert.AreEqual(stale, Pack.Objects.Contains(item));
                    if (stale) Assert.NotNull(Field(ui, "_itemActionPopup"));
                    ui.Close();
                }
                finally { Object.DestroyImmediate(host); }
            }
        }

        [Test] public void GritLookReadoutExplainsTractionAndExpiryWithoutHidingOil()
        {
            var cell = Zone.GetCell(10, 10); cell.IsVisible = cell.Explored = true;
            Zone.TileState.WriteCoating(10, 10, "oil", 5);
            StringAssert.Contains("slippery", CellStatusReadout.GroundLine(Zone, cell));
            Zone.TileState.WriteResidue(10, 10, "grit", 1);
            string line = CellStatusReadout.GroundLine(Zone, cell);
            StringAssert.Contains("oil", line); StringAssert.Contains("grit", line); StringAssert.Contains("firm footing", line);
            StringAssert.DoesNotContain("slippery", line);
            int dirty = 0; Zone.TileState.OnCellChanged += (x, y) => { if (x == 10 && y == 10) dirty++; };
            Zone.TileState.Tick();
            Assert.Greater(dirty, 0, "grit expiry must repaint a tile even when oil remains");
            StringAssert.Contains("slippery", CellStatusReadout.GroundLine(Zone, cell));
            cell.IsVisible = false; Assert.IsNull(CellStatusReadout.GroundLine(Zone, cell));
        }

        [Test] public void SilverSandInspectionIncludesCombatAndRepairUse()
        {
            var item = Carry("SilverSand");
            Assert.True(MaterialUseDescription.TryDescribe(Actor, item, Factory, out string description));
            StringAssert.Contains("grit", description.ToLowerInvariant());
            StringAssert.Contains("repair", description.ToLowerInvariant());
        }

        [Test] public void VeilReadoutNamesItsActualObscuringCloud()
        {
            var cell = Zone.GetCell(10, 10); cell.IsVisible = cell.Explored = true;
            Zone.TileState.WriteCloud(10, 10, "veil-mist", 4);
            StringAssert.Contains("sight", CellStatusReadout.GroundLine(Zone, cell));
        }

        [TestCase("grit", "residue:grit")]
        [TestCase("veil-mist", "veil-mist")]
        public void NewSurfaceHasRealNativeGeometryAndRelinquishesHiddenCells(string id, string kind)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                f.Zone.TileState.Clear(20, 10);
                if (id == "grit") f.Zone.TileState.WriteResidue(20, 10, id, 12);
                else f.Zone.TileState.WriteCloud(20, 10, id, 4);
                string saved = f.Zone.TileState.ToSaveString(); f.Refresh();
                var method = f.Presenter.GetType().GetMethod("TryGetElementVolume", Flags);
                object[] args = { 20, 10, null, null };
                Assert.True((bool)method.Invoke(f.Presenter, args));
                var view = (GameObject)args[2];
                Assert.AreEqual(kind, Field(args[3], "Kind"));
                Assert.Greater(view.GetComponent<MeshFilter>().sharedMesh.vertexCount, 0);
                Assert.True(view.GetComponent<MeshRenderer>().enabled);
                Assert.AreEqual(saved, f.Zone.TileState.ToSaveString(), "rendering cannot extend lifetime");
                f.Zone.GetCell(20, 10).IsVisible = false; f.Refresh();
                Assert.False((bool)method.Invoke(f.Presenter, new object[] { 20, 10, null, null }));
                SpawnRing3DIntegrationFixture.Hidden(view);
            }
        }
    }
}
