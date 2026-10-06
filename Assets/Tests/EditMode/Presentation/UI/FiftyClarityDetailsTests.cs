using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Reflection permits all teams' test-first additions to compile together.
    // Every presence guard is followed by a player-visible behavioral assertion.
    public abstract class FiftyClarityFixture
    {
        protected const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        HotbarSaveFixture scope;
        protected EntityFactory factory;
        protected Entity actor;
        protected Zone zone;
        Dictionary<string, LiquidDefinition> liquidRows;
        bool liquidInitialized;
        protected InventoryPart Pack => actor.GetPart<InventoryPart>();

        [SetUp] public void Setup()
        {
            var liquidMap = (Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            liquidRows = new Dictionary<string, LiquidDefinition>(liquidMap); liquidInitialized = LiquidRegistry.IsInitialized;
            scope = new HotbarSaveFixture(false, false);
            factory = new EntityFactory();
            factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
            actor = factory.CreateEntity("Player");
            actor.GetStat("Strength").BaseValue = 16;
            zone = new Zone("Overworld.2.6.0");
            Assert.True(zone.AddEntity(actor, 10, 10));
            Visible(10, 10); Visible(11, 10);
            MessageLog.Clear();
        }
        [TearDown] public void TearDown()
        {
            var liquidMap = (Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            liquidMap.Clear(); if (liquidRows != null) foreach (var row in liquidRows) liquidMap.Add(row.Key, row.Value);
            typeof(LiquidRegistry).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, liquidInitialized);
            scope?.Dispose();
        }
        protected void Visible(int x, int y) { zone.GetCell(x, y).Explored = true; zone.GetCell(x, y).IsVisible = true; }
        protected Entity Carry(string bp)
        {
            var item = factory.CreateEntity(bp); Assert.NotNull(item, bp);
            Assert.True(Pack.AddObject(item)); return item;
        }
        protected Entity Loose(string name, int weight = 2, int count = 1)
        {
            var item = new Entity { ID = Guid.NewGuid().ToString(), BlueprintName = name };
            item.Tags.Add("Item", "");
            item.AddPart(new RenderPart { DisplayName = name, Visible = true });
            item.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            item.AddPart(new HandlingPart { Weight = weight, Carryable = true, Throwable = true });
            item.AddPart(new StackerPart { StackCount = count, MaxStack = 100 });
            return item;
        }
        protected static object Query(string typeName, string method, params object[] args)
        {
            var type = typeof(InputHandler).Assembly.GetType("CavesOfOoo.Rendering." + typeName);
            Assert.NotNull(type, "Missing clarity query " + typeName);
            var fn = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(fn, "Missing clarity query " + method);
            return fn.Invoke(null, args);
        }
        protected static string Detail(string method, params object[] args) => (string)Query("InventoryDecisionDetails", method, args);

    }

    public sealed class FiftyClarityDetailsTests : FiftyClarityFixture
    {
        [Test] public void EquipmentComparisonShowsContributionTradeoffsForTheActualSwap()
        {
            var current = Carry("LeatherArmor");
            current.GetPart<ArmorPart>().AV = 1; current.GetPart<ArmorPart>().DV = 2;
            current.GetPart<ArmorPart>().SpeedPenalty = 0;
            current.GetPart<EquippablePart>().EquipBonuses = "";
            Assert.True(InventorySystem.Equip(actor, current));
            var next = factory.CreateEntity("LeatherArmor");
            next.GetPart<ArmorPart>().AV = 4; next.GetPart<ArmorPart>().DV = -1;
            next.GetPart<ArmorPart>().SpeedPenalty = 2;
            next.GetPart<EquippablePart>().EquipBonuses = "";
            Assert.True(Pack.AddObject(next));
            Assert.True(EquipmentComparisonService.TryDescribe(actor, next, out var text, out var reason), reason);
            StringAssert.Contains("AV change: +3", text);
            StringAssert.Contains("DV change: -3", text);
            StringAssert.Contains("Speed change: -2", text);
            Assert.True(InventorySystem.IsEquipped(actor, current));
            StringAssert.DoesNotContain("DPS", text);
        }

        [Test] public void ComparisonDeduplicatesOneDisplacedTwoSlotItem()
        {
            var current = Carry("DissolutionMaul"); current.AddPart(new ArmorPart { AV = 3 });
            Assert.True(InventorySystem.Equip(actor, current));
            var next = Carry("Dagger"); next.AddPart(new ArmorPart { AV = 5 });
            Assert.True(EquipmentComparisonService.TryDescribe(actor, next, out var text, out var reason), reason);
            StringAssert.Contains("AV change: +2", text);
            StringAssert.DoesNotContain("AV change: -1", text);
        }

        [Test] public void InventorySearchMatchesDisplayedNamesAndPreservesInputOrdering()
        {
            var hiddenId = Loose("not-a-match"); hiddenId.BlueprintName = "mendleaf";
            var a = Loose("Mendleaf sprig"); var b = Loose("Dried mendleaf");
            var source = new List<Entity> { hiddenId, b, a };
            var found = ((IEnumerable)Query("InventoryBrowseQuery", "Filter", source, "MENDLEAF")).Cast<Entity>().ToArray();
            CollectionAssert.AreEqual(new[] { b, a }, found);
            CollectionAssert.AreEqual(new[] { hiddenId, b, a }, source);
        }


        [Test] public void GroundLootReaderShowsExistingItemFactsAndWholeStackWeightWithoutTaking()
        {
            var item = factory.CreateEntity("Dagger"); item.GetPart<StackerPart>().StackCount = 3;
            Assert.True(zone.AddEntity(item, 10, 10));
            string text = Detail("Loot", actor, zone, item, null);
            StringAssert.Contains("Damage:", text); StringAssert.Contains("3 units", text);
            StringAssert.Contains("Total weight: " + InventoryPart.GetItemWeight(item), text);
            Assert.AreSame(zone.GetCell(10, 10), zone.GetEntityCell(item)); Assert.False(Pack.Contains(item));
        }


        [Test] public void PartialTakeAllKeepsFailedRowsAndActualReasonInExistingPopup()
        {
            var easy = Loose("light loot", 1); var heavy = Loose("heavy loot", 500);
            zone.AddEntity(easy, 10, 10); zone.AddEntity(heavy, 10, 10);
            var host = new GameObject("Partial pickup test"); host.SetActive(false);
            try
            {
                var ui = host.AddComponent<PickupUI>(); ui.PlayerEntity = actor; ui.CurrentZone = zone;
                ui.Open(new List<Entity> { easy, heavy });
                typeof(PickupUI).GetMethod("TakeAll", Private).Invoke(ui, null);
                Assert.True(Pack.Contains(easy)); Assert.False(Pack.Contains(heavy));
                Assert.True(ui.IsOpen, "Remaining refused loot must remain reviewable in the existing popup.");
                var rows = (List<Entity>)typeof(PickupUI).GetField("_items", Private).GetValue(ui);
                CollectionAssert.AreEqual(new[] { heavy }, rows);
                string status = (string)typeof(PickupUI).GetField("_statusMessage", Private).GetValue(ui);
                StringAssert.Contains("1", status); StringAssert.Contains("Strength", status);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [TestCase(false)] [TestCase(true)]
        public void ContainerFitDistinguishesFullMergeFromRequiredNewEntry(bool merge)
        {
            var box = Loose("box"); box.GetPart<PhysicsPart>().Takeable = false;
            box.AddPart(new ContainerPart { MaxItems = 1 }); zone.AddEntity(box, 11, 10);
            var stored = Loose("timber", 2, 3); box.GetPart<ContainerPart>().AddItem(stored);
            var input = Loose(merge ? "timber" : "rock", 2, 2); Pack.AddObject(input);
            string text = Detail("Container", actor, zone, box, input);
            StringAssert.Contains("1/1", text);
            StringAssert.Contains(merge ? "merge" : "full", text.ToLowerInvariant());
            Assert.AreEqual(3, stored.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(2, input.GetPart<StackerPart>().StackCount);
        }

        [TestCase(true)] [TestCase(false)]
        public void TradeQuoteNamesWholeStackAndActualPurseAfter(bool buying)
        {
            var trader = factory.CreateEntity("Player");
            var item = Loose("quoted goods", 2, 3); item.AddPart(new CommercePart { Value = 7 });
            if (buying) trader.GetPart<InventoryPart>().AddObject(item); else Pack.AddObject(item);
            TradeSystem.SetDrams(actor, 1000);
            int price = buying ? TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(actor), trader)
                : TradeSystem.GetSellPrice(item, TradeSystem.GetTradePerformance(actor), trader);
            string text = Detail("Trade", actor, trader, item, buying);
            StringAssert.Contains("3 units", text); StringAssert.Contains("Total weight: 6", text);
            StringAssert.Contains("Drams after: " + (1000 + (buying ? -price : price)), text);
            Assert.AreEqual(1000, TradeSystem.GetDrams(actor)); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
        }

        [Test] public void HandlingShowsActorGatesAndRangeWithoutConfusingStackWeightWithLift()
        {
            var item = Loose("small stones", 2, 20); Pack.AddObject(item);
            string text = Detail("Handling", actor, item);
            StringAssert.Contains("Total weight: 40", text);
            StringAssert.Contains("Lift Strength: " + HandlingService.GetLiftStrengthRequirement(item), text);
            StringAssert.Contains("Throw range: " + HandlingService.GetThrowRange(actor, item), text);
            item.GetPart<HandlingPart>().Throwable = false;
            StringAssert.DoesNotContain("Throw range:", Detail("Handling", actor, item));
        }
        [Test] public void HeavyNoncarryableBeamExplainsHaulingWithoutInventingPickup()
        {
            var beam = Loose("beam", 60); beam.GetPart<HandlingPart>().Carryable = false;
            beam.GetPart<HandlingPart>().Throwable = false; zone.AddEntity(beam, 11, 10);
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(actor, beam));
            string text = Detail("Handling", actor, beam);
            StringAssert.Contains("haul", text.ToLowerInvariant()); StringAssert.Contains("cannot be carried", text.ToLowerInvariant());
        }



        [Test] public void StatusReaderUsesLiveEffectAmountsAndOwnerTurnCounts()
        {
            var effects = actor.GetPart<StatusEffectsPart>() ?? new StatusEffectsPart();
            if (effects.ParentEntity == null) actor.AddPart(effects);
            var heart = new HeartFlameEffect { Duration = 9, ChargesRemaining = 2 }; actor.ApplyEffect(heart);
            string text = Detail("Status", actor);
            StringAssert.Contains(EffectDescriber.Describe(heart), text); StringAssert.DoesNotContain("Afflicted:", text);
            heart.ChargesRemaining = 1; heart.Duration = 8;
            StringAssert.Contains(EffectDescriber.Describe(heart), Detail("Status", actor));
            Assert.AreEqual(1, heart.ChargesRemaining); Assert.AreEqual(8, heart.Duration);
        }



        [TestCase(true)] [TestCase(false)]
        public void SurfaceDetailsExplainVisibleLiquidPropertiesWithoutResolvingThem(bool visible)
        {
            LiquidRegistry.Initialize("{\"Liquids\":[{\"Id\":\"clarity_ice\",\"DisplayName\":\"test ice\",\"Slippery\":true,\"SlipChance\":73}]}");
            var cell = zone.GetCell(11, 10); cell.IsVisible = visible;
            zone.TileState.WriteCoating(11, 10, "clarity_ice", 4);
            string text = Detail("Surface", zone, cell) ?? "";
            if (visible) { StringAssert.Contains("73%", text); StringAssert.Contains("slip", text.ToLowerInvariant()); }
            else StringAssert.DoesNotContain("test ice", text);
            Assert.AreEqual(4, zone.TileState.Get(11, 10).Coatings[0].Turns);
        }

        [Test] public void ForgeDetailsPreserveWholeEffectDescriptionsInsteadOfRawClippedText()
        {
            var preview = new ForgePreview { IsComplete = true, DisplayName = "test blade", BaseDamage = "2d3+1", PenBonus = 2,
                HitBonus = -1, MaxStrengthBonus = 3, FamilyDisplayName = "Short blades", Attributes = "Piercing" };
            string text = Detail("Forge", preview);
            StringAssert.Contains("2d3+1", text); StringAssert.Contains("Penetration: +2", text); StringAssert.Contains("Hit: -1", text);
            preview.IsComplete = false; preview.Missing = "a haft";
            StringAssert.Contains("a haft", Detail("Forge", preview));
        }
        [Test] public void BrewDetailsExplainEffectMeaningAndPreserveInvalidReason()
        {
            var invalid = new BrewPreview { IsValid = false, Reason = "This exact mixture refuses to bind." };
            StringAssert.Contains(invalid.Reason, Detail("Brew", invalid));
            var valid = BrewingService.PreviewBrew(new[] { factory.CreateEntity("MendleafSprig") });
            Assert.True(valid.IsValid, valid.Reason);
            string text = Detail("Brew", valid);
            StringAssert.Contains("1d4", text); StringAssert.Contains("heal", text.ToLowerInvariant());
        }

        [Test] public void SharpPreviewReportsActualBeforeAfterAndNeverAppliesTheMod()
        {
            var item = Carry("Dagger"); item.GetPart<MeleeWeaponPart>().PenBonus = 4;
            var recipe = new TinkerRecipe { ID = "clarity_sharp", Blueprint = "mod_sharp", Type = "Mod", Cost = "R", Description = "Sharpen a blade." };
            string text = Detail("Modification", item, recipe);
            StringAssert.Contains("4 -> 5", text); StringAssert.Contains("R", text);
            Assert.AreEqual(4, item.GetPart<MeleeWeaponPart>().PenBonus); Assert.False(item.HasTag("ModSharp"));
        }


        [TestCase(true)] [TestCase(false)]
        public void DisassemblyShowsExactOneUnitYieldWithoutGrantingTinkering(bool authorized)
        {
            var item = Loose("salvage", 2, 3); item.AddPart(new TinkerItemPart { BuildCost = "RRRR", NumberMade = 2 }); Pack.AddObject(item);
            var locker = actor.GetPart<BitLockerPart>(); if (locker != null) actor.RemovePart(locker);
            if (authorized) { locker = new BitLockerPart(); actor.AddPart(locker); }
            string text = Detail("Disassembly", actor, item);
            if (authorized) { StringAssert.Contains("one unit", text.ToLowerInvariant()); StringAssert.Contains("R", text); StringAssert.DoesNotContain("RRRR", text); }
            else StringAssert.Contains("unavailable", text.ToLowerInvariant());
            Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            if (authorized) Assert.Zero(locker.GetBitCount('R')); else Assert.IsNull(actor.GetPart<BitLockerPart>());
        }

        [Test] public void FoodDetailsExposeActualHealingWithoutInventingCookedBenefit()
        {
            var item = Loose("raw snack"); item.AddPart(new FoodPart { Healing = "1d3+2" }); Pack.AddObject(item);
            string text = Detail("Food", actor, item);
            StringAssert.Contains("1d3+2", text); StringAssert.Contains("one unit", text.ToLowerInvariant());
            StringAssert.DoesNotContain("100", text); StringAssert.DoesNotContain("replaces", text.ToLowerInvariant());
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
        }
        [Test] public void PreparedFoodDetailsUseRootsSharedMealDescription()
        {
            var item = Loose("prepared snack"); var food = new FoodPart { Healing = "1d2" }; item.AddPart(food); Pack.AddObject(item);
            var stat = typeof(FoodPart).GetField("MealStat"); Assert.NotNull(stat, "Root meal fields not implemented yet."); stat.SetValue(food, "HeatResistance");
            typeof(FoodPart).GetField("MealBonus").SetValue(food, 20); typeof(FoodPart).GetField("MealDuration").SetValue(food, 100);
            string text = Detail("Food", actor, item);
            StringAssert.Contains("20", text); StringAssert.Contains("100", text); StringAssert.Contains(FoodPart.DescribeMeal(food), text);
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
        }




        [TestCase(true)] [TestCase(false)]
        public void MealDetailsExplainRefreshVersusReplacementWithoutChangingCurrentMeal(bool same)
        {
            var item = Carry("ToastedEmberwheat"); var food = item.GetPart<FoodPart>();
            var current = new PreparedMealEffect(same ? food.MealStat : "DV", same ? food.MealBonus : 1, 23);
            Assert.True(actor.ApplyEffect(current));
            string text = Detail("Food", actor, item);
            StringAssert.Contains(same ? "refreshes" : "replaces", text);
            Assert.AreSame(current, actor.GetEffect<PreparedMealEffect>()); Assert.AreEqual(23, current.Duration);
        }
        [Test] public void SidebarHaulCueDisappearsWhenLinkIsReleased()
        {
            var load = Loose("beam", 60); zone.AddEntity(load, 11, 10);
            actor.AddPart(new DragPart { Dragged = load, AppliedPenalty = 7 }); load.AddPart(new DraggedPart { Dragger = actor });
            StringAssert.Contains("HAUL -7", string.Join(" ", SidebarStateBuilder.Build(actor, zone, null).VitalLines));
            actor.GetPart<DragPart>().Dragged = null;
            StringAssert.DoesNotContain("HAUL", string.Join(" ", SidebarStateBuilder.Build(actor, zone, null).VitalLines));
        }

        [Test] public void ControlsReaderRetainsAllExistingBindingsWithoutLogging()
        {
            int tick = WorldClock.CurrentTick;
            var method = typeof(ControlsReference).GetMethod("BuildReaderText", BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(method, "Controls need a free paginated reader text source.");
            string text = (string)method.Invoke(null, null);
            foreach (var row in ControlsReference.Bindings) { StringAssert.Contains(row.Key, text); StringAssert.Contains(row.What, text); }
            Assert.AreEqual(tick, WorldClock.CurrentTick); Assert.False(MessageLog.HasPendingAnnouncement);
        }
    }
}
