using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        static Entity CountedCarry(Rig r, string blueprint = "LiquidFlask", int volume = 0, string liquid = "")
        {
            var item = r.Factory.CreateEntity(blueprint); Assert.NotNull(item);
            var stack = item.GetPart<StackerPart>(); if (stack != null) { stack.MaxStack = 1; stack.StackCount = 1; }
            var vessel = item.GetPart<LiquidVesselPart>(); if (vessel != null) { vessel.Volume = volume; vessel.LiquidId = liquid; }
            Assert.True(r.Pack.AddObject(item)); Assert.True(r.Pack.Contains(item)); return item;
        }
        static object[] CountedRows(Rig r) => r.Rows("_rows").Cast<object>().Where(x => Get(x, "Item") != null).ToArray();
        static long CountedUnits(object row) => (long)Get(Get(row, "Group"), "TotalUnits");
        static InventoryScreenData.ItemDisplay CountedDisplay(object row) => (InventoryScreenData.ItemDisplay)Get(row, "Item");
        static void CountedRefresh(Rig r) { Call(r.UI, "Rebuild"); Call(r.UI, "Render"); }
        static void CountedToggle(Rig r) { Call(r.UI, "ToggleInventoryGrouping"); }

        [TestCase(5, 0, "")] [TestCase(3, 2, "water")] [TestCase(3, 2, "bog-mire")]
        public void CountedGroupsRequestedFlasksBecomeOneRowWithoutMergingOwners(int count, int volume, string liquid)
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                var items = Enumerable.Range(0, count).Select(_ => CountedCarry(r, volume:volume, liquid:liquid)).ToArray();
                r.Open(); r.Tab(1); var rows = CountedRows(r); Assert.AreEqual(1, rows.Length);
                Assert.AreEqual(count, CountedUnits(rows[0])); Assert.AreEqual(1, CountedDisplay(rows[0]).StackCount);
                CollectionAssert.AreEqual(items, r.Pack.Objects); Assert.True(items.All(e => e.GetPart<LiquidVesselPart>().Volume == volume));
                Assert.AreEqual(count, r.Pack.GetCarriedWeight()); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [TestCase("volume")] [TestCase("liquid")] [TestCase("capacity")] [TestCase("name")]
        [TestCase("no-trade")] [TestCase("unknown-part")] [TestCase("hidden")] [TestCase("foreign")] [TestCase("malformed")]
        public void CountedGroupsRefuseIncompatibleOrUnavailableFlasks(string change)
        {
            using (var registry = new PublicStateRegistryScope()) using (var r = new Rig())
            {
                CountedCarry(r, volume:2, liquid:"water"); var b = CountedCarry(r, volume:2, liquid:"water");
                if (change == "volume") b.GetPart<LiquidVesselPart>().Volume = 3;
                if (change == "liquid") b.GetPart<LiquidVesselPart>().LiquidId = "bog-mire";
                if (change == "capacity") b.GetPart<LiquidVesselPart>().Capacity = 6;
                if (change == "name") b.GetPart<RenderPart>().DisplayName = "my flask";
                if (change == "no-trade") b.SetTag("NoTrade");
                if (change == "unknown-part") b.AddPart(new PublicStateUnknownEnhancement());
                if (change == "hidden") b.GetPart<RenderPart>().Visible = false;
                if (change == "foreign") b.GetPart<PhysicsPart>().InInventory = r.Factory.CreateEntity("Player");
                if (change == "malformed") b.GetPart<LiquidVesselPart>().Volume = -1;
                r.Open(); r.Tab(1); Assert.AreEqual(2, CountedRows(r).Length);
            }
        }
        [TestCase("Waterskin")] [TestCase("FlintStriker")] [TestCase("Dagger")]
        [TestCase("LeatherArmor")] [TestCase("DriedMeat")] [TestCase("HealingTonic")]
        public void CountedGroupsOtherAuditedMatchingKindsAlsoShareRows(string blueprint)
        {
            using (var r = new Rig())
            {
                CountedCarry(r, blueprint); CountedCarry(r, blueprint); r.Open(); r.Tab(1);
                var rows = CountedRows(r); Assert.AreEqual(1, rows.Length, blueprint); Assert.AreEqual(2, CountedUnits(rows[0]));
                Assert.AreEqual(2, r.Pack.Objects.Count, "View grouping never grants physical merge permission.");
            }
        }
        [TestCase("skin")] [TestCase("striker")] [TestCase("condition")] [TestCase("treatment")]
        public void CountedGroupsPreserveChargesConditionAndTreatmentState(string kind)
        {
            using (var r = new Rig())
            {
                string bp = kind == "skin" ? "Waterskin" : kind == "striker" ? "FlintStriker" : kind == "treatment" ? "LeatherArmor" : "Dagger";
                var a = CountedCarry(r, bp); var b = CountedCarry(r, bp);
                if (kind == "skin") b.GetPart<WaterskinPart>().Charges = 1;
                if (kind == "striker") b.GetPart<FlintStrikerPart>().Strikes--;
                if (kind == "condition") { a.Statistics["Hitpoints"] = new Stat { Owner=a, Name="Hitpoints", BaseValue=10, Max=10 }; b.Statistics["Hitpoints"] = new Stat { Owner=b, Name="Hitpoints", BaseValue=9, Max=10 }; }
                if (kind == "treatment") b.AddPart(new EnhancementLacquered());
                r.Open(); r.Tab(1); Assert.AreEqual(2, CountedRows(r).Length);
            }
        }
        [Test] public void CountedGroupsKeepEquippedOwnerSeparateAndSinglesCanSelectAnyMember()
        {
            using (var r = new Rig())
            {
                var items = Enumerable.Range(0,3).Select(_=>CountedCarry(r, "Dagger")).ToArray();
                Assert.True(InventorySystem.Equip(r.Player, items[0])); r.Open(); r.Tab(1);
                Assert.AreEqual(2, CountedRows(r).Length); CountedToggle(r); Assert.AreEqual(3, CountedRows(r).Length);
                var selected = items[2]; Call(r.UI,"SelectLedgerHit",r.FindHit("Item",r.InventoryRow(selected))); Call(r.UI,"Render");
                CountedToggle(r); AssertSelectedItemIsPainted(r,selected); CountedToggle(r); AssertSelectedItemIsPainted(r,selected);
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test] public void CountedGroupsDoNotOverwritePhysicalStackCountAndDropOnlySelectedStack()
        {
            using (var r = new Rig())
            {
                var a=CountedCarry(r,"DriedMeat"); var b=CountedCarry(r,"DriedMeat");
                a.GetPart<StackerPart>().MaxStack=b.GetPart<StackerPart>().MaxStack=99;
                a.GetPart<StackerPart>().StackCount=2; b.GetPart<StackerPart>().StackCount=3;
                r.Open(); r.Tab(1); var row=CountedRows(r).Single(); Assert.AreEqual(5,CountedUnits(row));
                var selected=CountedDisplay(row); Assert.AreEqual(selected.Item.GetPart<StackerPart>().StackCount,selected.StackCount);
                Call(r.UI,"OpenItemActionPopup",selected); var popup=Get(r.UI,"_itemActionPopup");
                var actions=((IEnumerable)Get(popup,"Actions")).Cast<object>().ToArray();
                int drop=Array.FindIndex(actions,x=>(string)Get(x,"Command")=="drop"); Assert.GreaterOrEqual(drop,0);
                Call(r.UI,"ExecuteItemAction",drop);
                Assert.AreEqual(1,r.Pack.Objects.Count); Assert.False(r.Pack.Contains(selected.Item));
                Assert.AreEqual(5-selected.StackCount,r.Pack.Objects.Single().GetPart<StackerPart>().StackCount);
            }
        }
        [Test] public void CountedGroupsOneFlaskActionChangesOnlyItsOwnerAndRegroups()
        {
            using (var registry=new PublicStateRegistryScope()) using(var r=new Rig())
            {
                var items=Enumerable.Range(0,3).Select(_=>CountedCarry(r,volume:2,liquid:"water")).ToArray();
                var ground=r.Factory.CreateEntity("PouredLiquidPool"); ground.GetPart<LiquidPoolPart>().LiquidId="water";
                Assert.True(r.UI.CurrentZone.AddEntity(ground,11,10));
                r.Open(); r.Tab(1); var row=CountedRows(r).Single(); var chosen=CountedDisplay(row);
                Call(r.UI,"OpenItemActionPopup",chosen); var popup=Get(r.UI,"_itemActionPopup");
                var actions=((IEnumerable)Get(popup,"Actions")).Cast<object>().ToArray();
                int pour=Array.FindIndex(actions,x=>((string)Get(x,"Command")).StartsWith("PourOneLiquidVessel|")&&((string)Get(x,"Command")).EndsWith("|11|10"));
                Assert.GreaterOrEqual(pour,0); Call(r.UI,"ExecuteItemAction",pour);
                Assert.AreEqual(1,chosen.Item.GetPart<LiquidVesselPart>().Volume);
                Assert.True(items.Where(e=>e!=chosen.Item).All(e=>e.GetPart<LiquidVesselPart>().Volume==2));
                Assert.True(r.UI.ConsumePendingEverydayTurn()); Assert.False(r.UI.ConsumePendingEverydayTurn());
                r.Open(); r.Tab(1); var groups=CountedRows(r); Assert.AreEqual(2,groups.Length);
                CollectionAssert.AreEquivalent(new long[]{1,2},groups.Select(CountedUnits).ToArray());
                AssertSelectedItemIsPainted(r,chosen.Item);
            }
        }
        [Test] public void CountedGroupsQueryAndPaintReuseCapturedIdentityWithoutProviderReads()
        {
            using(var registry=new PublicStateRegistryScope()) using(var r=new Rig())
            {
                CountedCarry(r,volume:2,liquid:"water"); CountedCarry(r,volume:2,liquid:"water");
                var probeItem=CountedCarry(r,"Dagger"); var probe=new PublicStateProbe(); probeItem.AddPart(probe);
                r.Open(); r.Tab(1); int reads=probe.Reads, actions=probe.Actions;
                Call(r.UI,"SetInventoryQuery","water"); Assert.AreEqual(1,CountedRows(r).Length);
                Assert.AreEqual(2,CountedUnits(CountedRows(r)[0]));
                CountedToggle(r); Assert.AreEqual(2,CountedRows(r).Length); CountedToggle(r);
                for(int i=0;i<3;i++)Call(r.UI,"Render");
                Assert.AreEqual(reads,probe.Reads); Assert.AreEqual(actions,probe.Actions);
                Assert.AreEqual(3,r.Pack.Objects.Count); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test] public void CountedGroupsPopupAdvertisesEscapeThenGroupingWithoutChangingItsOwners()
        {
            using(var input=new NativeNavigationScope()) using(var registry=new PublicStateRegistryScope()) using(var r=new Rig())
            {
                var a=CountedCarry(r,volume:2,liquid:"water"); var b=CountedCarry(r,volume:2,liquid:"water");
                r.Open(); r.Tab(1); Call(r.UI,"OpenItemActionPopup",CountedDisplay(CountedRows(r).Single()));
                Assert.True(r.Labels().Any(t=>t.Content.Contains("Esc, then G")), "G is already a modal action hotkey; never advertise it alone as a modal grouping command.");
                input.Keys(UnityEngine.InputSystem.Key.Escape); r.UI.HandleInput(); Assert.IsNull(Get(r.UI,"_itemActionPopup"));
                input.Keys(); r.UI.HandleInput(); input.Keys(UnityEngine.InputSystem.Key.G); r.UI.HandleInput();
                Assert.AreEqual(2,CountedRows(r).Length); Assert.AreEqual(2,a.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual(2,b.GetPart<LiquidVesselPart>().Volume);
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test] public void CountedGroupsDistinctAdjacentFiniteThermalValuesNeverCollapseToOneKey()
        {
            using(var r=new Rig())
            {
                var a=CountedCarry(r,"Dagger"); var b=CountedCarry(r,"Dagger");
                a.GetPart<ThermalPart>().Temperature=b.GetPart<ThermalPart>().Temperature=25f;
                r.Open(); r.Tab(1); Assert.AreEqual(1,CountedRows(r).Length,"Fixture must first qualify for grouping.");
                b.GetPart<ThermalPart>().Temperature=BitConverter.ToSingle(BitConverter.GetBytes(BitConverter.ToInt32(BitConverter.GetBytes(25f),0)+1),0);
                Assert.AreNotEqual(a.GetPart<ThermalPart>().Temperature,b.GetPart<ThermalPart>().Temperature);
                CountedRefresh(r); Assert.AreEqual(2,CountedRows(r).Length);
            }
        }
        [Test] public void CountedGroupsLongNamesNeverHidePopupActionScope()
        {
            using(var registry=new PublicStateRegistryScope()) using(var r=new Rig())
            {
                var a=CountedCarry(r); var b=CountedCarry(r);
                a.GetPart<RenderPart>().DisplayName=b.GetPart<RenderPart>().DisplayName=new string('x',220);
                r.Open(); r.Tab(1); Call(r.UI,"OpenItemActionPopup",CountedDisplay(CountedRows(r).Single()));
                Assert.True(r.Labels().Any(t=>t.Content.Contains("Selected 1 of 2")), "Action scope needs its own fitted line before arbitrarily long names.");
            }
        }
        [Test] public void CountedGroupsControllerCommandsExposeIndividualOwnersWithoutPaidWork()
        {
            using(var input=new NativeNavigationScope()) using(var registry=new PublicStateRegistryScope()) using(var r=new Rig())
            {
                CountedCarry(r); CountedCarry(r); r.Open(); r.Tab(1);
                input.Button(UnityEngine.InputSystem.LowLevel.GamepadButton.West); r.UI.HandleInput();
                Assert.True((bool)Get(r.UI,"_ledgerCommandsOpen"));
                var commands=((IEnumerable)Get(r.UI,"_ledgerCommands")).Cast<object>().ToArray();
                int index=Array.FindIndex(commands,x=>(string)Get(x,"Label")=="Show individual items"); Assert.GreaterOrEqual(index,0);
                Call(r.UI,"ExecuteLedgerCommand",index); Assert.AreEqual(2,CountedRows(r).Length);
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
    }
}
