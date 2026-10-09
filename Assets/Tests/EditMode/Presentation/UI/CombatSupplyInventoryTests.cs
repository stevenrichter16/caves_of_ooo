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
    public sealed class CombatSupplyInventoryTests : FiftyWorldFixture
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        static object Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, Flags).Invoke(target, args);

        [TestCase("KnotflaxCord", "LayCordSnare|")]
        [TestCase("FireClay", "SmotherFire|")]
        [TestCase("Waterskin", "DrenchCreature|")]
        public void RealSupplyActionClosesInventoryAndQueuesExactlyOneTurn(string blueprint, string prefix)
        { InventoryAction(blueprint, prefix, false); }

        [TestCase("KnotflaxCord", "LayCordSnare|")]
        [TestCase("FireClay", "SmotherFire|")]
        [TestCase("Waterskin", "DrenchCreature|")]
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
                    if (blueprint == "Waterskin") item.GetPart<WaterskinPart>().Charges = 2;
                    if (blueprint == "FireClay") Actor.ApplyEffect(new BurningEffect(1));
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
                    Assert.AreEqual(stale || blueprint == "Waterskin", Pack.Objects.Contains(item));
                    if (blueprint == "Waterskin") Assert.AreEqual(stale ? 2 : 1, item.GetPart<WaterskinPart>().Charges);
                    if (stale) Assert.NotNull(Field(ui, "_itemActionPopup"));
                    ui.Close();
                }
                finally { Object.DestroyImmediate(host); }
            }
        }

        [TestCase("KnotflaxCord", "snare")]
        [TestCase("FireClay", "smother")]
        [TestCase("Waterskin", "drench")]
        public void CarriedInspectionExplainsTheNewUseAndItsTradeoff(string blueprint, string word)
        {
            var item = Carry(blueprint);
            Assert.True(MaterialUseDescription.TryDescribe(Actor, item, Factory, out string description));
            StringAssert.Contains(word, description.ToLowerInvariant());
            StringAssert.Contains(blueprint == "KnotflaxCord" ? "you" : "wet", description.ToLowerInvariant());
        }
    }
}
