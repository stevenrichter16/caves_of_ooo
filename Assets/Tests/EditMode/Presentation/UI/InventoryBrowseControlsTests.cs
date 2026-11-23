using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        static void BrowseCategory(Rig r, string category) => Call(r.UI, "SetInventoryCategory", category);
        static void BrowseSort(Rig r, string sort) => Call(r.UI, "SetInventorySort", sort);
        static void BrowseSelect(Rig r, Entity item)
        { Call(r.UI, "SelectLedgerHit", r.FindHit("Item", r.InventoryRow(item))); Call(r.UI, "Render"); }

        [TestCase(ItemCategory.Armor, "LeatherArmor")]
        [TestCase(ItemCategory.MeleeWeapons, "Dagger")]
        [TestCase(ItemCategory.Food, "DriedMeat")]
        public void BrowseCategoryFiltersDisclosedCategoryAndAllRestoresExactOwner(string category, string blueprint)
        {
            using (var r = new Rig())
            {
                var blade = r.Carry("Dagger"); var armor = r.Carry("LeatherArmor"); var food = r.Carry("DriedMeat");
                r.Open(); r.Tab(1); BrowseSelect(r, blade); var before = r.Pack.Objects.ToArray();
                BrowseCategory(r, category);
                CollectionAssert.AreEqual(new[] { before.Single(x => x.BlueprintName == blueprint) }, DenseOwners(r));
                BrowseCategory(r, ""); AssertSelectedItemIsPainted(r, blade);
                CollectionAssert.AreEqual(before, r.Pack.Objects); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test]
        public void BrowseCategoryAndSearchIntersectAndClearIndependently()
        {
            using (var r = new Rig())
            {
                var a = r.Carry("Dagger", "needle"); r.Carry("DriedMeat", "needle meal");
                r.Open(); r.Tab(1); BrowseCategory(r, ItemCategory.MeleeWeapons);
                Call(r.UI,"BeginInventorySearch"); Call(r.UI,"SetInventoryQuery","needle");
                CollectionAssert.AreEqual(new[] { a }, DenseOwners(r));
                Call(r.UI,"SetInventoryQuery","meal"); Assert.IsEmpty(DenseOwners(r));
                Call(r.UI,"SetInventoryQuery",""); CollectionAssert.AreEqual(new[] { a }, DenseOwners(r));
                BrowseCategory(r, ""); Assert.AreEqual(2, DenseOwners(r).Length);
            }
        }
        [Test]
        public void BrowseExplicitFilteredSelectionReplacesReturnAnchor()
        {
            using (var r = new Rig())
            {
                var a = r.Carry("Dagger"); var b = r.Carry("LeatherArmor"); var c = r.Carry("LeatherBoots");
                r.Open(); r.Tab(1); BrowseSelect(r,a); BrowseCategory(r,ItemCategory.Armor); BrowseSelect(r,c);
                BrowseSort(r,"Name"); BrowseCategory(r,""); AssertSelectedItemIsPainted(r,c);
            }
        }
        [Test]
        public void BrowseMissingCategoryStaysEmptyAndRemovedReturnOwnerIsNeverSelected()
        {
            using (var r = new Rig())
            {
                var a = r.Carry("Dagger"); var b = r.Carry("DriedMeat"); r.Open(); r.Tab(1); BrowseSelect(r,a);
                BrowseCategory(r,"vanished trade"); Assert.IsEmpty(DenseOwners(r));
                Assert.IsNull(r.UI.SelectedDecisionPresentation(out _));
                Assert.True(r.Pack.RemoveObject(a)); Call(r.UI,"Rebuild"); Call(r.UI,"Render");
                Assert.IsEmpty(DenseOwners(r)); BrowseCategory(r,""); AssertSelectedItemIsPainted(r,b);
            }
        }
        [TestCase("Name")] [TestCase("Weight")] [TestCase("Category")]
        public void BrowseSortPreservesExactGroupedMemberAndEquippedState(string sort)
        {
            using (var r = new Rig())
            {
                var a=CountedCarry(r); var b=CountedCarry(r); var c=r.Carry("Dagger");
                Assert.True(InventorySystem.Equip(r.Player,c)); r.Open(); r.Tab(1); CountedToggle(r); BrowseSelect(r,b); CountedToggle(r);
                var before=r.Pack.Objects.ToArray(); BrowseSort(r,sort); AssertSelectedItemIsPainted(r,b);
                Assert.AreEqual(2,CountedUnits(CountedRows(r).Single(x=>CountedDisplay(x).Item==b)));
                Assert.True(InventorySystem.IsEquipped(r.Player,c)); CollectionAssert.AreEqual(before,r.Pack.Objects);
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test]
        public void BrowseNameOrderIsGlobalPublicAndDeterministic()
        {
            using (var r = new Rig())
            {
                var z=r.Carry("Dagger","zebra"); var a=r.Carry("LeatherArmor","apple"); var b=r.Carry("DriedMeat","Banana");
                r.Open(); r.Tab(1); BrowseSelect(r,z); BrowseSort(r,"Name");
                CollectionAssert.AreEqual(new[]{a,b,z},DenseOwners(r)); AssertSelectedItemIsPainted(r,z);
                for(int i=0;i<3;i++) { Call(r.UI,"Rebuild"); Call(r.UI,"Render"); CollectionAssert.AreEqual(new[]{a,b,z},DenseOwners(r)); }
            }
        }
        [Test]
        public void BrowseWeightOrdersDisplayedGroupTotalWithoutChangingPhysicalStacks()
        {
            using (var r = new Rig())
            {
                var a=CountedCarry(r,"DriedMeat"); var b=CountedCarry(r,"DriedMeat"); var c=r.Carry("Dagger");
                a.GetPart<StackerPart>().StackCount=3; b.GetPart<StackerPart>().StackCount=2;
                r.Open(); r.Tab(1); BrowseSort(r,"Weight");
                Assert.AreEqual(a, DenseOwners(r)[0]); Assert.Greater(CountedUnits(CountedRows(r)[0]),1);
                Assert.AreEqual(3,a.GetPart<StackerPart>().StackCount); Assert.AreEqual(2,b.GetPart<StackerPart>().StackCount);
                CountedToggle(r); BrowseSort(r,"Weight"); Assert.AreEqual(3,DenseOwners(r).Length);
            }
        }
        [TestCase(1280,800)] [TestCase(1920,1080)] [TestCase(640,480)] [TestCase(480,360)]
        public void BrowseControlsFitHeaderWithoutReducingRowCapacity(int width,int height)
        {
            using(var r=new Rig(width,height))
            {
                r.Carry("Dagger"); r.Open(); r.Tab(1);
                var layout=Get(r.UI,"_denseLayout"); var before=(int)Get(layout,"Capacity");
                var controls=r.Hits().Where(x=>Kind(x)=="PackCategory"||Kind(x)=="PackSort"||Kind(x)=="PackBrowse").ToArray();
                Assert.IsNotEmpty(controls);
                foreach(var hit in controls)
                {
                    var rect=Bounds(hit); Assert.Greater(rect.width,0); Assert.LessOrEqual(rect.xMax,width); Assert.GreaterOrEqual(rect.xMin,0);
                    foreach(var other in r.Hits().Where(x=>Kind(x)=="PackGrouping"||Kind(x)=="Details"))
                        Assert.False(rect.Overlaps(Bounds(other)),"Browse controls must not shadow grouping or full details.");
                }
                var headers=r.Hits().Where(x=>Kind(x)=="PackCategory"||Kind(x)=="PackSort"||Kind(x)=="PackBrowse"||Kind(x)=="PackGrouping"||Kind(x)=="Details").ToArray();
                for(int i=0;i<headers.Length;i++) for(int j=i+1;j<headers.Length;j++)
                    Assert.False(Bounds(headers[i]).Overlaps(Bounds(headers[j])),Kind(headers[i])+" overlaps "+Kind(headers[j]));
                BrowseCategory(r,ItemCategory.MeleeWeapons); BrowseSort(r,"Name");
                Assert.AreEqual(before,Get(Get(r.UI,"_denseLayout"),"Capacity"));
            }
        }
        [TestCase(Key.F3,"CATEGORIES")] [TestCase(Key.F4,"SORT ITEMS")]
        public void BrowseKeyboardOpensFreeModalAndEscapeCancelsWithoutAction(Key key,string title)
        {
            using(var input=new NativeNavigationScope()) using(var r=new Rig())
            {
                var a=r.Carry("Dagger"); r.Open(); r.Tab(1); input.Keys(key); r.UI.HandleInput();
                Assert.True((bool)Get(r.UI,"_ledgerCommandsOpen")); Assert.True(r.Labels().Any(x=>x.Content==title));
                Assert.False(r.Hits().Any(x=>Kind(x)=="Item")); input.Keys(Key.Escape); r.UI.HandleInput();
                Assert.False((bool)Get(r.UI,"_ledgerCommandsOpen")); AssertSelectedItemIsPainted(r,a);
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test]
        public void BrowseControllerCommandsOfferSameCategoryAndSortActions()
        {
            using(var input=new NativeNavigationScope()) using(var r=new Rig())
            {
                r.Carry("Dagger"); r.Open(); r.Tab(1); input.Button(GamepadButton.West); r.UI.HandleInput();
                var commands=r.Rows("_ledgerCommands").Cast<object>().Select(x=>(string)Get(x,"Label")).ToArray();
                CollectionAssert.Contains(commands,"Choose category"); CollectionAssert.Contains(commands,"Sort items");
            }
        }
        [Test]
        public void BrowseFullReopenResetsTemporaryFiltersAndSort()
        {
            using(var r=new Rig())
            {
                r.Carry("Dagger"); r.Carry("DriedMeat"); r.Open(); r.Tab(1);
                BrowseCategory(r,ItemCategory.Food); BrowseSort(r,"Name"); r.UI.Close(); r.Open(); r.Tab(1);
                Assert.AreEqual("",Get(r.UI,"_inventoryCategory")); Assert.AreEqual("Category",Get(r.UI,"_inventorySort"));
                Assert.AreEqual(2,DenseOwners(r).Length);
            }
        }
        [Test]
        public void BrowseDefaultOrderAndEqualNameTiesStayIndependentOfSelectedGroupRepresentative()
        {
            using(var r=new Rig())
            {
                var a=CountedCarry(r); var b=CountedCarry(r); var c=CountedCarry(r,volume:2,liquid:"water");
                foreach(var x in new[]{a,b,c}) x.GetPart<RenderPart>().DisplayName="same vessel";
                r.Open(); r.Tab(1); var defaultOwners=DenseOwners(r);
                BrowseSort(r,"Name"); var firstOrder=CountedRows(r).Select(x=>(InventoryItemGroup)Get(x,"Group")).Select(g=>g.Contains(c)).ToArray();
                CountedToggle(r); BrowseSelect(r,b); CountedToggle(r); BrowseSort(r,"Name");
                CollectionAssert.AreEqual(firstOrder,CountedRows(r).Select(x=>(InventoryItemGroup)Get(x,"Group")).Select(g=>g.Contains(c)));
                AssertSelectedItemIsPainted(r,b); BrowseSort(r,"Category");
                CollectionAssert.AreEqual(defaultOwners.Select(x=>x==a||x==b?"empty":"filled"),DenseOwners(r).Select(x=>x==a||x==b?"empty":"filled"));
            }
        }
        [Test]
        public void BrowseProjectionUsesRetainedPublicSnapshotWithoutActionOrStateProviderQueries()
        {
            using(var r=new Rig())
            {
                var a=r.Carry("Dagger"); var probe=new PublicStateProbe(); a.AddPart(probe);
                var secret=new PublicStateUnknownEnhancement(); a.AddPart(secret);
                r.Open(); r.Tab(1); var snapshot=Get(r.UI,"_state"); int reads=probe.Reads,actions=probe.Actions,descriptions=probe.Descriptions,secretReads=secret.DescriptionReads;
                BrowseCategory(r,ItemCategory.MeleeWeapons); BrowseSort(r,"Name"); BrowseSort(r,"Weight"); BrowseCategory(r,"");
                Assert.AreSame(snapshot,Get(r.UI,"_state")); Assert.AreEqual(reads,probe.Reads); Assert.AreEqual(actions,probe.Actions);
                Assert.AreEqual(descriptions,probe.Descriptions); Assert.AreEqual(secretReads,secret.DescriptionReads);
            }
        }
        [Test]
        public void BrowsePickerUsesFullSnapshotDespiteSearchAndLateCustomCategoryIsReachable()
        {
            using(var r=new Rig())
            {
                var a=r.Carry("Dagger","needle");
                for(int i=0;i<15;i++) r.Carry("DriedMeat","custom "+i).GetPart<PhysicsPart>().Category="Trade "+i;
                r.Open(); r.Tab(1); Call(r.UI,"BeginInventorySearch"); Call(r.UI,"SetInventoryQuery","needle");
                // Model-only setup ends typing before exercising the real menu.
                typeof(CavesOfOoo.Rendering.InventoryUI).GetField("_inventorySearchActive",Members).SetValue(r.UI,false);
                Call(r.UI,"OpenCategoryChooser");
                var choices=r.Rows("_ledgerCommands").Cast<object>().Select(x=>(string)Get(x,"Label")).ToArray();
                Assert.AreEqual(17,choices.Length); int index=choices.Length-1; Assert.Greater(index,9);
                string category=choices[index].Substring(4);
                Call(r.UI,"ExecuteLedgerCommand",index); Assert.IsEmpty(DenseOwners(r)); Call(r.UI,"SetInventoryQuery","");
                Assert.AreEqual(category,DenseSelected(r).GetPart<PhysicsPart>().Category);
                Call(r.UI,"OpenLedgerCommands"); Assert.True(r.Labels().Any(x=>x.Content=="COMMANDS"));
            }
        }
        [Test]
        public void BrowseEscapeClearsSearchThenCategoryThenClosesAndF2IsNotRebound()
        {
            using(var input=new NativeNavigationScope()) using(var r=new Rig())
            {
                r.Carry("Dagger"); r.Carry("DriedMeat"); r.Open(); r.Tab(1); BrowseCategory(r,ItemCategory.MeleeWeapons);
                Call(r.UI,"BeginInventorySearch"); Call(r.UI,"SetInventoryQuery","dagger"); input.Keys(Key.Enter); r.UI.HandleInput();
                input.Keys(Key.Escape); r.UI.HandleInput(); Assert.AreEqual("",Get(r.UI,"_inventoryQuery")); Assert.AreEqual(ItemCategory.MeleeWeapons,Get(r.UI,"_inventoryCategory"));
                input.Keys(); r.UI.HandleInput(); input.Keys(Key.Escape); r.UI.HandleInput(); Assert.True(r.UI.IsOpen); Assert.AreEqual("",Get(r.UI,"_inventoryCategory"));
                input.Keys(Key.F2); r.UI.HandleInput(); Assert.False((bool)Get(r.UI,"_ledgerCommandsOpen"));
                input.Keys(Key.Escape); r.UI.HandleInput(); Assert.False(r.UI.IsOpen); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void BrowseExplicitActionOnVisibleFallbackBecomesReturnOwner(bool pointer)
        {
            using(var input=new NativeNavigationScope()) using(var r=new Rig())
            {
                var blade=r.Carry("Dagger"); var food=r.Carry("DriedMeat"); r.Open(); r.Tab(1);
                BrowseSelect(r,blade); BrowseCategory(r,ItemCategory.Food); Assert.AreSame(food,DenseSelected(r));
                if(pointer) r.ActivatePainted(r.FindHit("Item",r.InventoryRow(food)));
                else { input.Keys(Key.Enter); r.UI.HandleInput(); }
                Assert.NotNull(Get(r.UI,"_itemActionPopup")); Call(r.UI,"CancelLedgerPopup");
                BrowseCategory(r,""); AssertSelectedItemIsPainted(r,food); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [TestCase(480,360,1f)] [TestCase(960,720,2f)]
        public void BrowseCompactHeaderKeepsTypedSearchVisibleAtSmallAndLargeText(int width,int height,float scale)
        {
            using(var r=new Rig(width,height))
            {
                r.Carry("DriedMeat"); r.Open(); r.Tab(1);
                typeof(CavesOfOoo.Rendering.InventoryUI).GetField("_denseTextScale",Members).SetValue(r.UI,scale); Call(r.UI,"Render");
                Call(r.UI,"BeginInventorySearch"); Call(r.UI,"SetInventoryQuery","dried");
                Assert.True(r.Labels().Any(x=>x.Content=="dried_"),"Retain visible query characters beside compact controls.");
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test]
        public void BrowseControllerCanEnterCategoryChooserAndApplyARealCategory()
        {
            using(var input=new NativeNavigationScope()) using(var r=new Rig())
            {
                var blade=r.Carry("Dagger"); r.Carry("DriedMeat"); r.Open(); r.Tab(1);
                input.Button(GamepadButton.West); r.UI.HandleInput();
                var commands=r.Rows("_ledgerCommands").Cast<object>().Select(x=>(string)Get(x,"Label")).ToArray();
                int index=Array.IndexOf(commands,"Choose category"); Assert.GreaterOrEqual(index,0);
                for(int i=0;i<index;i++){ input.Keys();r.UI.HandleInput();input.Button(GamepadButton.DpadDown);r.UI.HandleInput(); }
                input.Keys();r.UI.HandleInput();input.Button(GamepadButton.South);r.UI.HandleInput();
                Assert.True(r.Labels().Any(x=>x.Content=="CATEGORIES"));
                input.Keys();r.UI.HandleInput();input.Button(GamepadButton.DpadDown);r.UI.HandleInput();
                input.Keys();r.UI.HandleInput();input.Button(GamepadButton.South);r.UI.HandleInput();
                CollectionAssert.AreEqual(new[]{blade},DenseOwners(r)); Assert.False((bool)Get(r.UI,"_ledgerCommandsOpen"));
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
    }
}
