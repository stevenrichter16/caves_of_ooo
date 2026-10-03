using System.Collections;
using System.Linq;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class EquipmentDiscoveryForgeUiTests : CraftingFlowFixture
    {
        void Tiles()
        {
            var grid = new GameObject("Equipment preview grid"); grid.transform.SetParent(UI.transform); grid.AddComponent<Grid>();
            var tiles = new GameObject("Equipment preview tiles"); tiles.transform.SetParent(grid.transform); UI.Tilemap = tiles.AddComponent<Tilemap>();
        }
        string Text()
        {
            var result = new StringBuilder();
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    var tile = UI.Tilemap.GetTile(new Vector3Int(x, 44 - y, 0)); char value = ' ';
                    if (tile != null) for (int c = 33; c < 127; c++)
                        if (ReferenceEquals(tile, CP437TilesetGenerator.GetUiTile((char)c))) { value = (char)c; break; }
                    result.Append(value);
                }
                result.AppendLine();
            }
            return result.ToString();
        }

        [Test] public void PackAndStationUseHeadsAndBladesWithoutChangingInternalSlot()
        {
            var rows = (IList)Get("_craftRows");
            Assert.IsTrue(rows.Cast<object>().Any(r => (bool)Field(r, "IsHeader") && (string)Field(r, "Text") == "Heads / blades"));
            var actions = new InventoryActionList(); var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actor", (object)Player); e.SetParameter("Actions", (object)actions);
            try { Station.FireEvent(e); } finally { e.Release(); }
            Assert.IsTrue(actions.Actions.Any(a => a.Display.Contains("Heads / blades")));
            Assert.AreEqual("Blade", Steel.GetPart<WeaponComponentPart>().Slot);
        }

        [TestCase("SteelBladeComponent", "Long blades", "+1", "0")]
        [TestCase("PeatMalletHeadComponent", "Cudgel", "+1", "-1")]
        [TestCase("CinderhookAxeHeadComponent", "Axe", "-1", "0")]
        [TestCase("CounterweightLongBladeComponent", "Long blades", "+2", "-1")]
        public void ActualSelectedResultDisplaysFamilyAndSignedFinalTradeoffs(string blueprint, string family, string hit, string pen)
        {
            var head = blueprint == "SteelBladeComponent" ? Steel : Carry(blueprint); Call("Rebuild");
            Pick(head); Pick(Oak); Pick(Leather); Tiles(); Call("RenderForgeResult", 1, 2);
            string text = Text(); StringAssert.Contains("Family", text); StringAssert.Contains(family, text);
            StringAssert.Contains("Pen       " + pen, text); StringAssert.Contains("Hit       " + hit, text);
            Assert.AreEqual(2, head.GetPart<StackerPart>().StackCount, "Preview never spends the actual selected component.");
        }

        [Test] public void ChangingHeadReplacesVisibleFamilyRatherThanLeavingStaleLongBlade()
        {
            var mallet = Carry("PeatMalletHeadComponent"); Call("Rebuild");
            Pick(Steel); Pick(Oak); Pick(Leather); Tiles(); Call("RenderForgeResult", 1, 2);
            StringAssert.Contains("Long blades", Text());
            Pick(mallet); UI.Tilemap.ClearAllTiles(); Call("RenderForgeResult", 1, 2);
            StringAssert.Contains("Cudgel", Text()); StringAssert.DoesNotContain("Long blades", Text());
        }
    }
}
