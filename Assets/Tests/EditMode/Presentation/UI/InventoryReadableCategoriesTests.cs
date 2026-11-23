using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        [TestCase("LiquidFlask","Containers")] [TestCase("Waterskin","Containers")]
        [TestCase("DrawgourdShell","Containers")] [TestCase("GreybladderCup","Containers")]
        [TestCase("CandyCarrotSeed","Seeds")] [TestCase("PrismreedSeed","Seeds")]
        [TestCase("PrismreedPith","Reagents")] [TestCase("SourmantleFold","Reagents")]
        [TestCase("FireMoss","Reagents")] [TestCase("LampOil","Reagents")]
        [TestCase("FireClay","Materials")] [TestCase("SalvagedTimber","Materials")]
        [TestCase("KnotflaxCord","Materials")] [TestCase("RecoveredIronBlank","Materials")]
        [TestCase("GlowQuartz","Materials")] [TestCase("MarlrootClod","Materials")]
        [TestCase("SteelBladeComponent","Components")] [TestCase("OakHaftComponent","Components")]
        [TestCase("CampHearth","Camp Equipment")] [TestCase("CampAwning","Camp Equipment")]
        [TestCase("CampBox","Camp Equipment")] [TestCase("CampStill","Camp Equipment")]
        [TestCase("Scraper","Tools")] [TestCase("FlintStriker","Tools")]
        [TestCase("SpringTrap","Tools")] [TestCase("SoundingRod","Tools")]
        [TestCase("HushStrip","Tools")] [TestCase("StunGasGrenade","Grenades")]
        [TestCase("Torch","Light Sources")] [TestCase("ChoirwickBranch","Light Sources")]
        [TestCase("Codex04","Documents")] [TestCase("OvenBuildersGuide","Documents")]
        [TestCase("DitchkeepersFootwork","Books")] [TestCase("ClaspbeanPulp","Tonics")]
        public void ReadableCategoriesSeparateFormerMiscellaneousBySupportedPublicUse(string blueprint,string category)
        {
            using(var r=new Rig())
            {
                var item=CountedCarry(r,blueprint); var physics=item.GetPart<PhysicsPart>();string authored=physics.Category;
                r.Open(); r.Tab(1); var display=CountedDisplay(CountedRows(r).Single());
                Assert.AreEqual(category,display.Category); Assert.AreEqual(category,ItemCategory.GetCategory(item));
                Assert.AreEqual(authored,physics.Category,"Reading a category never rewrites saved item state.");
                Assert.AreSame(r.Player,physics.InInventory); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [TestCase("Unknown")] [TestCase("Artifacts")] [TestCase("Quest Items")]
        [TestCase("Family keepsakes")] [TestCase("Food")] [TestCase("All categories")]
        public void ReadableCategoriesPreserveExplicitProtectedAndCustomLabels(string category)
        {
            using(var r=new Rig())
            {
                var flask=CountedCarry(r,"LiquidFlask"); flask.GetPart<PhysicsPart>().Category=category;
                Assert.AreEqual(category,ItemCategory.GetCategory(flask)); r.Open();r.Tab(1);BrowseCategory(r,category);
                CollectionAssert.AreEqual(new[]{flask},DenseOwners(r));
            }
        }
        [TestCase("")] [TestCase("Miscellaneous")]
        public void ReadableCategoriesRefineExistingSavedFallbackWithoutPayloadChanges(string oldCategory)
        {
            using(var registry=new PublicStateRegistryScope()) using(var r=new Rig())
            {
                var flask=CountedCarry(r,volume:2,liquid:"water");flask.GetPart<PhysicsPart>().Category=oldCategory;
                r.Open();r.Tab(1);var before=CountedDisplay(CountedRows(r).Single());
                Assert.AreEqual("Containers",before.Category); Assert.AreEqual("2/12 water",before.State);
                BrowseCategory(r,"Containers");BrowseSort(r,"Name");AssertSelectedItemIsPainted(r,flask);
                Assert.AreEqual(oldCategory,flask.GetPart<PhysicsPart>().Category);Assert.AreEqual(2,flask.GetPart<LiquidVesselPart>().Volume);
            }
        }
        [Test]
        public void ReadableCategoriesUnknownEnhancementsAndCompositionDoNotRevealANewPurpose()
        {
            using(var r=new Rig())
            {
                var item=r.Carry("Item"); var hidden=new PublicStateUnknownEnhancement();item.AddPart(hidden);
                item.AddPart(new CompositionPart()); var probe=new PublicStateProbe{Throws=true};item.AddPart(probe);
                Assert.AreEqual(ItemCategory.Miscellaneous,ItemCategory.GetCategory(item));
                Assert.Zero(hidden.DescriptionReads);Assert.Zero(probe.Reads);Assert.Zero(probe.Actions);Assert.Zero(probe.Descriptions);
            }
        }
        [Test]
        public void ReadableCategoriesOrderAndChooserRemainReachableForMixedPracticalPack()
        {
            using(var r=new Rig())
            {
                foreach(string bp in new[]{"Dagger","LeatherArmor","DriedMeat","PrismreedSeed","FireMoss","FireClay","LiquidFlask","CampBox","Scraper","SteelBladeComponent","Codex04","Torch"})CountedCarry(r,bp);
                r.Open();r.Tab(1);var state=(InventoryScreenData.ScreenState)Get(r.UI,"_state");
                var labels=state.Categories.Select(x=>x.CategoryName).ToArray();Assert.AreEqual(12,labels.Length);
                CollectionAssert.Contains(labels,"Materials");CollectionAssert.Contains(labels,"Containers");CollectionAssert.Contains(labels,"Camp Equipment");
                Assert.Less(Array.IndexOf(labels,ItemCategory.MeleeWeapons),Array.IndexOf(labels,"Materials"));
                Call(r.UI,"OpenCategoryChooser");var commands=r.Rows("_ledgerCommands").Cast<object>().Select(x=>(string)Get(x,"Label")).ToArray();
                Assert.AreEqual(13,commands.Length);Assert.True(commands.Any(x=>x.EndsWith("Documents",StringComparison.Ordinal)));
            }
        }
        [TestCase("Dagger","Melee Weapons")] [TestCase("LeatherArmor","Armor")]
        [TestCase("Scraper","Tools")] [TestCase("LiquidFlask","Containers")]
        [TestCase("CampHearth","Camp Equipment")] [TestCase("SteelBladeComponent","Components")]
        [TestCase("PrismreedPith","Reagents")] [TestCase("OvenBuildersGuide","Documents")]
        [TestCase("CandyCarrotSeed","Seeds")]
        public void ReadableCategoriesPrimaryUseWinsOverIncidentalLight(string blueprint,string category)
        {
            using(var r=new Rig())
            {
                var item=CountedCarry(r,blueprint);item.GetPart<PhysicsPart>().Category="";
                if(item.GetPart<LightSourcePart>()==null)item.AddPart(new LightSourcePart());
                Assert.AreEqual(category,ItemCategory.GetCategory(item));
            }
        }
        [Test]
        public void ReadableCategoriesKeepAuditedGroupMembershipAndPhysicalQuantities()
        {
            using(var registry=new PublicStateRegistryScope()) using(var r=new Rig())
            {
                var a=CountedCarry(r,volume:2,liquid:"water");var b=CountedCarry(r,volume:2,liquid:"water");
                r.Open();r.Tab(1);BrowseCategory(r,"Containers");var row=CountedRows(r).Single();
                var group=(InventoryItemGroup)Get(row,"Group");CollectionAssert.AreEquivalent(new[]{a,b},group.Members.Select(x=>x.Item));
                Assert.AreEqual(2,group.TotalUnits);Assert.AreEqual(2,r.Pack.Objects.Count);
                Assert.AreEqual(2,a.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(2,b.GetPart<LiquidVesselPart>().Volume);
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
    }
}
