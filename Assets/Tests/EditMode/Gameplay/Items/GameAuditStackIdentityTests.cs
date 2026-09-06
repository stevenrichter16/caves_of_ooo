using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A03: actual crafted payloads must survive the insertion that can merge them.</summary>
    public abstract class StackIdentityFixture
    {
        protected EntityFactory Factory;
        [OneTimeSetUp] public void Load()
        {
            Factory = new EntityFactory(); Factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            MessageLog.Clear(); Diag.ResetAll(); BrewRuleRegistry.ResetForTests(); BrewRuleRegistry.EnsureInitialized();
            TinkerRecipeRegistry.ResetForTests(); TinkerRecipeRegistry.EnsureInitialized();
            EnhancementFactory.ForceReinitialize(); EnhancementFactory.EnsureInitialized();
        }
        [TearDown] public void Cleanup()
        { MessageLog.Clear(); Diag.ResetAll(); BrewRuleRegistry.ResetForTests(); TinkerRecipeRegistry.ResetForTests(); EnhancementFactory.ForceReinitialize(); }
        protected Entity Item(string bp) { var item = Factory.CreateEntity(bp); Assert.NotNull(item, bp); return item; }
        protected Entity Actor()
        {
            var actor = Item("Player"); Assert.IsEmpty(actor.GetPart<InventoryPart>().Objects);
            if (actor.GetPart<BitLockerPart>() == null) actor.AddPart(new BitLockerPart());
            actor.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse"); return actor;
        }
        protected Entity Give(Entity actor, string bp)
        { var item = Item(bp); Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item)); return item; }
        protected Entity Brew(Entity actor, bool healing, bool strong, bool equivalent = false)
        {
            string[] bps = healing ? (strong ? (equivalent ? new[] { "CandyHeartRoot", "MendleafSprig" } : new[] { "CandyHeartRoot" })
                : new[] { "MendleafSprig", "EmberFruit" }) : (strong ? new[] { "GlimmerBrine", "SparkRoot" } : new[] { "GlimmerBrine" });
            var ingredients = bps.Select(bp => Give(actor, bp)).ToArray();
            Assert.IsTrue(BrewingService.TryBrew(actor, Factory, ingredients, out var made, out var result, out var reason), reason);
            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind); Assert.NotNull(made);
            foreach (var ingredient in ingredients) Assert.IsFalse(actor.GetPart<InventoryPart>().Contains(ingredient), "actual reagent paid");
            return made;
        }
        protected Entity Infused(int times)
        {
            var actor = Actor(); var blade = Give(actor, "Dagger");
            for (int i = 0; i < times; i++)
            {
                var salt = Give(actor, "PaleSalt");
                Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_palesalt_infuse", blade, out var reason), reason);
                Assert.IsFalse(actor.GetPart<InventoryPart>().Contains(salt), "actual mineral paid");
            }
            Assert.AreEqual(times, blade.Parts.OfType<EnhancementPaleSalt>().Count());
            Assert.IsTrue(actor.GetPart<InventoryPart>().RemoveObject(blade)); return blade;
        }
        protected Entity Tempered(bool strong)
        {
            var actor = Actor(); var blade = Give(actor, "Dagger"); var quench = Brew(actor, false, strong);
            Assert.IsTrue(WeaponTemperingService.TryTemper(actor, blade, quench, out var reason), reason);
            Assert.IsFalse(actor.GetPart<InventoryPart>().Contains(quench));
            Assert.AreEqual(1, blade.GetPart<WeaponTemperPart>().TemperCount);
            Assert.IsTrue(actor.GetPart<InventoryPart>().RemoveObject(blade)); return blade;
        }
        protected static string Name(Entity item) => item.GetPart<RenderPart>().DisplayName;
        protected static void Compatible(Entity a, Entity b, bool expected)
        { Assert.AreEqual(expected, a.GetPart<StackerPart>().CanStackWith(b)); Assert.AreEqual(expected, b.GetPart<StackerPart>().CanStackWith(a)); }

        protected sealed class MinRandom : Random { public override int Next(int min, int max) => min; public override int Next(int max) => 0; }
    }
    public class GameAuditStackIdentityTests : StackIdentityFixture
    {
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void DifferentActualBrewStrengthsRemainCarriedAndDeliverTheirOwnPayload(bool healing, bool strongFirst)
        {
            var actor = Actor(); var first = Brew(actor, healing, strongFirst); var second = Brew(actor, healing, !strongFirst);
            var strong = strongFirst ? first : second; var weak = strongFirst ? second : first;
            Assert.AreEqual(Name(strong), Name(weak), "collision is same-name");
            var inv = actor.GetPart<InventoryPart>(); Assert.IsTrue(inv.Contains(strong)); Assert.IsTrue(inv.Contains(weak));
            Compatible(strong, weak, false);
            if (healing)
            {
                Assert.IsNull(strong.GetPart<BrewItemPart>()); Assert.AreEqual("2d4", strong.GetPart<TonicPart>().Healing);
                Assert.AreEqual("1d4", weak.GetPart<TonicPart>().Healing);
                actor.GetStat("Hitpoints").Max = 100; actor.GetStat("Hitpoints").BaseValue = 10;
            }
            var carried = inv.Objects.Single(i => ReferenceEquals(i, strong));
            Assert.IsTrue(carried.GetPart<TonicPart>().ApplyTo(actor, actor, rng: new MinRandom(), consumeItem: true));
            Assert.IsFalse(inv.Contains(strong)); Assert.IsTrue(inv.Contains(weak)); Assert.AreEqual(1, weak.GetPart<StackerPart>().StackCount);
            if (healing) Assert.AreEqual(12, actor.GetStatValue("Hitpoints"));
            else Assert.AreEqual(3, actor.GetEffect<ElectrifiedEffect>().Charge);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void IdenticalActualRecipesStillMerge(bool healing, bool strong)
        {
            var actor = Actor(); var a = Brew(actor, healing, strong);
            var parts = a.Parts.ToArray(); string name = Name(a); string healingDice = a.GetPart<TonicPart>().Healing;
            var b = Brew(actor, healing, strong);
            Assert.AreSame(a, b); Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(b));
            Assert.AreEqual(2, a.GetPart<StackerPart>().StackCount); CollectionAssert.AreEqual(parts, a.Parts);
            Assert.AreEqual(name, Name(a)); Assert.AreEqual(healingDice, a.GetPart<TonicPart>().Healing);
        }
        [Test] public void DifferentRecipesWithSameResolvedHealingStillMerge()
        {
            var actor = Actor(); var a = Brew(actor, true, true); var payload = a.GetPart<TonicPart>();
            Assert.AreEqual("2d4", payload.Healing); var b = Brew(actor, true, true, equivalent: true);
            Assert.AreSame(a, b); Assert.AreSame(payload, b.GetPart<TonicPart>()); Assert.AreEqual("2d4", payload.Healing);
            Assert.AreEqual(2, a.GetPart<StackerPart>().StackCount); Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(b));
        }
        [TestCase(1, 1)] [TestCase(1, 2)] [TestCase(2, 1)] [TestCase(2, 2)]
        public void PaidMineralUpgradeOccurrencesSurviveTransfer(int firstCount, int secondCount)
        {
            var a = Infused(firstCount); var b = Infused(secondCount); Assert.AreEqual(Name(a), Name(b));
            var receiver = Actor(); var inv = receiver.GetPart<InventoryPart>(); Assert.IsTrue(inv.AddObject(a)); Assert.IsTrue(inv.AddObject(b));
            Compatible(a, b, firstCount == secondCount);
            Assert.AreEqual(firstCount == secondCount ? 1 : 2, inv.Objects.Count);
            foreach (var carried in inv.Objects)
            {
                var target = new Entity(); target.AddPart(new MaterialPart { MaterialTagsRaw = "Undead" });
                target.Statistics["Hitpoints"] = new Stat { Owner = target, Name = "Hitpoints", BaseValue = 100, Max = 100 };
                int expected = carried.Parts.OfType<EnhancementPaleSalt>().Count() * 4;
                ItemEnhancementDispatch.DispatchOnHit(carried, target, receiver, new Damage(1), 1, null, new Random(1));
                Assert.AreEqual(100 - expected, target.GetStatValue("Hitpoints"));
            }
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void TemperedWeaponPayloadSurvivesTransfer(bool firstStrong, bool secondStrong)
        {
            var a = Tempered(firstStrong); var b = Tempered(secondStrong); Assert.AreEqual(Name(a), Name(b));
            var inv = Actor().GetPart<InventoryPart>(); Assert.IsTrue(inv.AddObject(a)); Assert.IsTrue(inv.AddObject(b));
            Compatible(a, b, firstStrong == secondStrong); Assert.AreEqual(firstStrong == secondStrong ? 1 : 2, inv.Objects.Count);
            foreach (var blade in inv.Objects)
                StringAssert.Contains(ReferenceEquals(blade, a) ? (firstStrong ? "Electrified,50,,0,3" : "Electrified,40,,0,2")
                    : (secondStrong ? "Electrified,50,,0,3" : "Electrified,40,,0,2"), blade.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
        }
        [TestCase(false)] [TestCase(true)] public void LoadedBrewGraphRetainsBothStrengths(bool healing)
        {
            var actor = Actor(); Brew(actor, healing, false); Brew(actor, healing, true);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var inv = loaded.GetPart<InventoryPart>(); Assert.AreEqual(2, inv.Objects.Count);
            Compatible(inv.Objects[0], inv.Objects[1], false);
            foreach (var item in inv.Objects) Assert.AreSame(loaded, item.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(1)] [TestCase(2)] public void SplitAndTokenReloadPreserveEveryUpgradeAndCanRemerge(int count)
        {
            var blade = Infused(count); blade.GetPart<StackerPart>().StackCount = 3;
            var split = blade.GetPart<StackerPart>().SplitStack(1); Compatible(blade, split, true);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(split); Compatible(blade, loaded, true);
            Assert.AreEqual(count, loaded.Parts.OfType<EnhancementPaleSalt>().Count());
            var inv = Actor().GetPart<InventoryPart>(); inv.AddObject(blade); inv.AddObject(loaded);
            Assert.AreEqual(3, blade.GetPart<StackerPart>().StackCount); Assert.AreEqual(1, inv.Objects.Count);
        }
    }
}
