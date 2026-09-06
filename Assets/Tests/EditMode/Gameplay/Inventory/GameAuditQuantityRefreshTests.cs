using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A05: supported configured handling penalties track quantity, without changing live weight semantics.</summary>
    public class GameAuditQuantityRefreshTests
    {
        private EntityFactory _factory;
        private Entity _actor;
        private InventoryPart Inv => _actor.GetPart<InventoryPart>();
        [SetUp] public void Setup()
        {
            _factory = new EntityFactory(); _factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            FactionManager.Initialize(); PlayerReputation.Reset(); Diag.ResetAll(); MessageLog.Clear();
            BrewRuleRegistry.ResetForTests(); BrewRuleRegistry.EnsureInitialized();
            TinkerRecipeRegistry.ResetForTests(); TinkerRecipeRegistry.EnsureInitialized();
            EnhancementFactory.ForceReinitialize(); EnhancementFactory.EnsureInitialized(); SeedPart.Factory = _factory;
            _actor = Item("Player"); _actor.GetStat("Speed").BaseValue = 100; _actor.GetStat("Speed").Penalty = 7;
            if (_actor.GetPart<BitLockerPart>() == null) _actor.AddPart(new BitLockerPart());
            _actor.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse");
        }
        [TearDown] public void Cleanup()
        {
            SeedPart.Factory = null; FactionManager.Reset(); PlayerReputation.Reset(); Diag.ResetAll(); MessageLog.Clear();
            BrewRuleRegistry.ResetForTests(); TinkerRecipeRegistry.ResetForTests(); EnhancementFactory.ForceReinitialize();
        }
        private Entity Item(string bp) { var item = _factory.CreateEntity(bp); Assert.NotNull(item, bp); return item; }
        private Entity Give(string bp, int count = 1, int penalty = 0)
        {
            var item = Item(bp); item.GetPart<StackerPart>().StackCount = count; Configure(item, penalty);
            Assert.IsTrue(Inv.AddObject(item)); return item;
        }
        private static void Configure(Entity item, int penalty)
        {
            var handling = item.GetPart<HandlingPart>(); if (handling == null) { handling = new HandlingPart(); item.AddPart(handling); }
            handling.CarryMovePenalty = penalty;
        }
        private void Penalty(int expected)
        {
            Assert.AreEqual(expected, _actor.GetStat("Speed").Penalty, "derived penalty must already be current");
            Assert.AreEqual(100 - expected, _actor.GetStatValue("Speed"));
            Inv.RefreshHandlingCarryPenalty(); Assert.AreEqual(expected, _actor.GetStat("Speed").Penalty, "refresh is idempotent");
        }
        [TestCase("plant", 1, 0)]
        [TestCase("plant", 1, 4)]
        [TestCase("plant", 3, 0)]
        [TestCase("plant", 3, 4)]
        [TestCase("gift", 1, 0)]
        [TestCase("gift", 1, 4)]
        [TestCase("gift", 3, 0)]
        [TestCase("gift", 3, 4)]
        [TestCase("temper", 1, 0)]
        [TestCase("temper", 1, 4)]
        [TestCase("temper", 3, 0)]
        [TestCase("temper", 3, 4)]
        [TestCase("infuse", 1, 0)]
        [TestCase("infuse", 1, 4)]
        [TestCase("infuse", 3, 0)]
        [TestCase("infuse", 3, 4)]
        [TestCase("disassemble", 1, 0)]
        [TestCase("disassemble", 1, 4)]
        [TestCase("disassemble", 3, 0)]
        [TestCase("disassemble", 3, 4)]
        [TestCase("mishap", 1, 0)]
        [TestCase("mishap", 1, 4)]
        [TestCase("mishap", 3, 0)]
        [TestCase("mishap", 3, 4)]
        public void SuccessfulOneUnitWorkRefreshesOnlyItsConfiguredPenalty(string kind, int count, int carryPenalty)
        {
            string bp = kind == "plant" ? "CandyCarrotSeed" : kind == "gift" || kind == "infuse" ? "PaleSalt" : kind == "disassemble" ? "Dagger" : "FireMoss";
            Entity source;
            if (kind == "temper")
            {
                var brine = Give("GlimmerBrine");
                Assert.IsTrue(BrewingService.TryBrew(_actor, _factory, new[] { brine }, out source, out _, out var why), why);
                source.GetPart<StackerPart>().StackCount = count; Configure(source, carryPenalty); Inv.RefreshHandlingCarryPenalty();
            }
            else source = Give(bp, count, carryPenalty);
            Penalty(7 + count * carryPenalty);
            switch (kind)
            {
                case "plant":
                    var zone = new Zone("QuantityPlant"); zone.AddEntity(Item("Grass"), 10, 10); zone.AddEntity(_actor, 10, 10);
                    Assert.IsTrue(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(source, "PlantSeed"), _actor, zone).Success);
                    Assert.IsTrue(zone.GetCell(10, 10).HasObjectWithPart<CropPart>()); break;
                case "gift":
                    var npc = Item("SaltMaster"); int rep = PlayerReputation.Get("TentRight");
                    Assert.IsTrue(MineralTradeService.TryTrade(_actor, npc, "PaleSalt"));
                    Assert.AreEqual(rep + 5, PlayerReputation.Get("TentRight")); break;
                case "temper":
                    var blade = Give("Dagger"); Assert.IsTrue(WeaponTemperingService.TryTemper(_actor, blade, source, out var reason), reason);
                    Assert.AreEqual(1, blade.GetPart<WeaponTemperPart>().TemperCount);
                    StringAssert.Contains("Electrified", blade.GetPart<MeleeWeaponPart>().OnHitEffectsRaw); break;
                case "infuse":
                    var weapon = Give("Dagger"); Assert.IsTrue(TinkeringService.TryApplyModification(_actor, "mod_palesalt_infuse", weapon, out var modReason), modReason);
                    Assert.AreEqual(1, weapon.Parts.OfType<EnhancementPaleSalt>().Count()); break;
                case "disassemble":
                    Assert.IsTrue(TinkeringService.TryDisassemble(_actor, source, out var bits, out var disReason), disReason);
                    Assert.IsNotEmpty(bits); Assert.IsTrue(_actor.GetPart<BitLockerPart>().HasBits(bits)); break;
                case "mishap":
                    Assert.IsTrue(BrewingService.TryBrew(_actor, _factory, new[] { source }, out var made, out var result, out var brewReason), brewReason);
                    Assert.AreEqual(BrewOutcomeKind.Mishap, result.Kind); Assert.IsNull(made); break;
            }
            Assert.AreEqual(count > 1, Inv.Contains(source)); if (count > 1) Assert.AreEqual(count - 1, source.GetPart<StackerPart>().StackCount);
            Penalty(7 + (count - 1) * carryPenalty);
        }
        [TestCase(false, false, false)] [TestCase(false, false, true)] [TestCase(false, true, false)] [TestCase(false, true, true)]
        [TestCase(true, false, false)] [TestCase(true, false, true)] [TestCase(true, true, false)] [TestCase(true, true, true)]
        public void CraftRestorationRefreshesAfterEveryRestoredQuantity(bool forge, bool reverse, bool missingOutput)
        {
            Entity[] inputs = forge ? new[] { Give("SteelBladeComponent", reverse ? 2 : 1, 4), Give("OakHaftComponent", reverse ? 1 : 2, 4), Give("LeatherBindingComponent", 2, 4) }
                : new[] { Give("GlimmerBrine", reverse ? 2 : 1, 4), Give("SparkRoot", reverse ? 1 : 2, 4) };
            int before = inputs.Sum(i => i.GetPart<StackerPart>().StackCount); Penalty(7 + before * 4);
            int[] counts = inputs.Select(i => i.GetPart<StackerPart>().StackCount).ToArray();
            if (missingOutput)
            {
                string missing = forge ? "ForgedWeapon" : "BrewedTonic";
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "EntityFactory: unknown blueprint '" + missing + "'");
                _factory.Blueprints.Remove(missing);
            }
            bool ok;
            if (forge) ok = WeaponForgingService.TryForge(_actor, _factory, inputs[0], inputs[1], inputs[2], out _, out _);
            else ok = BrewingService.TryBrew(_actor, _factory, inputs, out _, out _, out _);
            Assert.AreEqual(!missingOutput, ok);
            if (missingOutput)
            {
                for (int i = 0; i < inputs.Length; i++)
                { Assert.IsTrue(Inv.Objects.Contains(inputs[i])); Assert.AreEqual(counts[i], inputs[i].GetPart<StackerPart>().StackCount); }
            }
            Penalty(7 + (before - (missingOutput ? 0 : inputs.Length)) * 4);
        }
        [TestCase(false, 0)] [TestCase(false, 4)] [TestCase(true, 0)] [TestCase(true, 4)]
        public void SplitAndRemoveOneRefreshSourceWithoutChargingDetachedClone(bool removeOne, int carryPenalty)
        {
            var source = Give("PaleSalt", 3, carryPenalty); Penalty(7 + 3 * carryPenalty);
            var clone = removeOne ? source.GetPart<StackerPart>().RemoveOne() : source.GetPart<StackerPart>().SplitStack(1);
            Assert.NotNull(clone); Assert.AreEqual(1, clone.GetPart<StackerPart>().StackCount); Assert.IsNull(clone.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(2, source.GetPart<StackerPart>().StackCount); Penalty(7 + 2 * carryPenalty);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void MergeRefreshesBothActualCarriersButSameOwnerConservesPenalty(bool sameOwner, bool partial)
        {
            var a = Give("PaleSalt", 2, 4); a.GetPart<StackerPart>().MaxStack = 2;
            Entity ownerB; Entity b;
            if (sameOwner) { ownerB = _actor; b = Give("PaleSalt", 3, 4); }
            else
            {
                ownerB = Item("Player"); ownerB.GetStat("Speed").BaseValue = 100; ownerB.GetStat("Speed").Penalty = 7;
                b = Item("PaleSalt"); b.GetPart<StackerPart>().StackCount = 3; Configure(b, 4);
                Assert.IsTrue(ownerB.GetPart<InventoryPart>().AddObject(b));
            }
            a.GetPart<StackerPart>().MaxStack = partial ? 3 : 99;
            Assert.IsTrue(a.GetPart<StackerPart>().CanStackWith(b));
            Assert.AreEqual(partial ? 1 : 3, a.GetPart<StackerPart>().MergeFrom(b));
            Assert.AreEqual(partial ? 3 : 5, a.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(partial ? 2 : 0, b.GetPart<StackerPart>().StackCount);
            Penalty(sameOwner ? 27 : partial ? 19 : 27);
            Assert.AreEqual(sameOwner ? 27 : partial ? 15 : 7, ownerB.GetStat("Speed").Penalty);
            ownerB.GetPart<InventoryPart>().RefreshHandlingCarryPenalty();
            Assert.AreEqual(sameOwner ? 27 : partial ? 15 : 7, ownerB.GetStat("Speed").Penalty);
        }
        [TestCase(false)] [TestCase(true)] public void RealPenaltyDeltaIsObservableAndRepeatedRefreshIsQuiet(bool enabled)
        {
            var source = Give("PaleSalt", 3, 4); Diag.ResetAll(); Diag.SetChannel("event", enabled);
            Assert.IsTrue(Inv.TryConsumeOne(source)); Penalty(15);
            var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = "CarryPenaltyRefreshed", Actor = _actor.ID }).Records;
            Assert.AreEqual(enabled ? 1 : 0, records.Count);
            if (enabled)
            {
                StringAssert.Contains("\"previousPenalty\":12", records.Single().PayloadJson);
                StringAssert.Contains("\"currentPenalty\":8", records.Single().PayloadJson);
                StringAssert.Contains("\"delta\":-4", records.Single().PayloadJson);
            }
        }
    }
}
