using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditPositivePaymentAdversarialTests : PositivePaymentFixture
    {
        Entity FindConsumable(string blueprint)
        {
            var method = typeof(InventoryPart).GetMethod("FindConsumableByBlueprint", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method, "Internal selector must exist without expanding the gameplay API for tests.");
            return (Entity)method.Invoke(Inventory, new object[] { blueprint });
        }
        [TestCase(0)] [TestCase(-1)]
        public void ExplicitEmptySeedNeverBorrowsASeparatePositiveSelection(int count)
        {
            Place("Grass"); var pair = Mixed("CandyCarrotSeed", count); var before = Inventory.Objects.ToArray();
            Assert.IsFalse(Plant(pair.empty)); CollectionAssert.AreEqual(before, Inventory.Objects);
            Assert.AreEqual(count, Quantity(pair.empty)); Assert.AreEqual(2, Quantity(pair.positive)); Assert.IsEmpty(Crops()); Assert.AreEqual(0, After.Count);
            Assert.IsTrue(Plant(pair.positive)); Assert.AreEqual(count, Quantity(pair.empty)); Assert.AreEqual(1, Quantity(pair.positive)); Assert.AreEqual(1, Crops().Length);
        }
        [TestCase("owner", true)] [TestCase("missing_backref", true)] [TestCase("actorless", true)] [TestCase("wrong_actor", false)]
        [TestCase("actor_without_inventory", false)] [TestCase("stale_carrier", false)] [TestCase("ground", false)] [TestCase("equipped", false)]
        public void PlantMenuUsesActualCarriageOfTheApplicableActor(string state, bool offered)
        {
            Place("Grass"); var seed = Carry("CandyCarrotSeed", 2); var physics = seed.GetPart<PhysicsPart>(); Entity actor = Player;
            if (state == "missing_backref") physics.InInventory = null;
            if (state == "actorless") actor = null;
            if (state == "wrong_actor") actor = Actor();
            if (state == "actor_without_inventory") { actor = Actor(); actor.RemovePart(actor.GetPart<InventoryPart>()); }
            if (state == "stale_carrier") { Assert.IsTrue(Inventory.RemoveObject(seed)); physics.InInventory = Player; actor = null; }
            if (state == "ground") { Assert.IsTrue(Inventory.RemoveObject(seed)); Assert.IsTrue(Zone.AddEntity(seed, 10, 10)); }
            if (state == "equipped") { Assert.IsTrue(Inventory.RemoveObject(seed)); physics.Equipped = Player; }
            Assert.AreEqual(offered, PlantRow(seed, actor)); Assert.AreEqual(2, Quantity(seed)); Assert.IsEmpty(Crops());
            if (state == "missing_backref") { Assert.IsTrue(Plant(seed)); Assert.AreEqual(1, Quantity(seed)); Assert.AreEqual(1, Crops().Length); }
            if (state == "ground") { Assert.IsFalse(Plant(seed)); Assert.AreEqual(2, Quantity(seed)); Assert.IsTrue(Zone.GetAllEntities().Contains(seed)); }
        }
        [TestCase(false)] [TestCase(true)] public void AuthoredItemWithoutStackerRemainsOneConsumableUnit(bool mineral)
        {
            var item = Carry(mineral ? "PaleSalt" : "CandyCarrotSeed"); item.RemovePart(item.GetPart<StackerPart>());
            if (mineral) { var npc = Speaker("SaltMaster"); Assert.IsTrue(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "PaleSalt"))); Assert.AreEqual(5, PlayerReputation.Get("TentRight")); }
            else { Place("Grass"); Assert.IsTrue(PlantRow(item, Player)); Assert.IsTrue(Plant(item)); Assert.AreEqual(1, Crops().Length); }
            Assert.IsFalse(Inventory.Objects.Contains(item)); Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(false)] [TestCase(true)] public void MissingCropFactoryOrBlueprintRefusesTheCommand(bool blueprint)
        {
            Place("Grass"); var seed = Carry("CandyCarrotSeed", 2); var old = Factory.Blueprints["CandyCarrotCrop"];
            if (blueprint) Factory.Blueprints.Remove("CandyCarrotCrop"); else SeedPart.Factory = null;
            if (blueprint) UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "EntityFactory: unknown blueprint 'CandyCarrotCrop'");
            try { Assert.IsFalse(Plant(seed)); }
            finally { Factory.Blueprints["CandyCarrotCrop"] = old; SeedPart.Factory = Factory; }
            Assert.AreEqual(2, Quantity(seed)); Assert.IsTrue(Inventory.Objects.Contains(seed)); Assert.IsEmpty(Crops()); Assert.AreEqual(0, After.Count);
            StringAssert.Contains(blueprint ? "unknown_blueprint" : "no_factory", DiagQuery.Apply(new DiagQuery.Filter { Kind = "PlantRejected", Actor = Player.ID }).Records.Single().PayloadJson);
        }
        [TestCase(false)] [TestCase(true)] public void ExplicitZoneCannotBeReplacedByActiveZoneButAbsentContextCanFallBack(bool explicitWrong)
        {
            Place("Grass"); var seed = Carry("CandyCarrotSeed", 2); var e = GameEvent.New("InventoryAction");
            e.SetParameter("Actor", (object)Player); e.SetParameter("Command", "PlantSeed"); if (explicitWrong) e.SetParameter("Zone", (object)new Zone("Other"));
            bool handled = false, propagated;
            try { propagated = KeepTime(() => { bool result = seed.FireEvent(e); handled = e.Handled; return result; }); }
            finally { e.Release(); }
            Assert.AreEqual(explicitWrong, propagated); Assert.AreEqual(!explicitWrong, handled);
            Assert.AreEqual(explicitWrong ? 2 : 1, Quantity(seed)); Assert.AreEqual(explicitWrong ? 0 : 1, Crops().Length);
        }
        public class PlantVeto : Part
        {
            public bool Block;
            public override string Name => "PlantVeto";
            public override bool HandleEvent(GameEvent e) => !(Block && e.ID == "BeforeInventoryAction");
        }
        [TestCase(false)] [TestCase(true)] public void BeforeActionVetoStillPreventsAllPlantingConsequences(bool veto)
        {
            Place("Grass"); var seed = Carry("CandyCarrotSeed", 2); Player.AddPart(new PlantVeto { Block = veto });
            Assert.AreEqual(!veto, Plant(seed)); Assert.AreEqual(veto ? 2 : 1, Quantity(seed)); Assert.AreEqual(veto ? 0 : 1, Crops().Length);
            Assert.AreEqual(veto ? 0 : 1, After.Count); Assert.AreEqual(veto ? 0 : 1, Records("CropPlanted"));
        }
        [TestCase(1)] [TestCase(3)] public void MultipleEmptyAndPositiveEntriesPayOnlyTheFirstPositive(int firstCount)
        {
            var npc = Speaker("SaltMaster"); var entries = new List<Entity>();
            foreach (int ignored in new[] { 0, 1, 2, 3 }) { var item = Carry("PaleSalt"); item.GetPart<StackerPart>().MaxStack = 1; entries.Add(item); }
            int[] counts = { 0, -1, firstCount, 2 };
            for (int i = 0; i < entries.Count; i++) { entries[i].GetPart<StackerPart>().MaxStack = 99; entries[i].GetPart<StackerPart>().StackCount = counts[i]; }
            Assert.IsTrue(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "PaleSalt")));
            CollectionAssert.AreEqual(firstCount == 1 ? new[] { entries[0], entries[1], entries[3] } : entries.ToArray(), Inventory.Objects);
            Assert.AreEqual(0, Quantity(entries[0])); Assert.AreEqual(-1, Quantity(entries[1])); Assert.AreEqual(firstCount > 1 ? firstCount - 1 : 1, Quantity(entries[2])); Assert.AreEqual(2, Quantity(entries[3]));
            Assert.AreEqual(5, PlayerReputation.Get("TentRight")); Assert.AreEqual(1, Records("Traded"));
        }
        [Test] public void WantedMineralMatchingRemainsCaseInsensitive()
        { var npc = Speaker("SaltMaster"); var salt = Carry("PaleSalt", 2); Assert.IsTrue(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "palesalt"))); Assert.AreEqual(1, Quantity(salt)); Assert.AreEqual(5, PlayerReputation.Get("TentRight")); }
        [TestCase(false)] [TestCase(true)] public void RepeatedAutomaticSelectionIsPureAndIgnoresMalformedNullEntries(bool nullEntry)
        {
            var pair = Mixed("PaleSalt", 0); if (nullEntry) Inventory.Objects.Insert(0, null);
            var before = Inventory.Objects.ToArray(); int diagnostics = DiagQuery.Count(new DiagQuery.Filter()).Count, penalty = Player.GetStat("Speed").Penalty;
            for (int i = 0; i < 100; i++)
            {
                Assert.AreSame(pair.positive, FindConsumable("PALESALT"));
                Assert.IsNull(FindConsumable(null)); Assert.IsNull(FindConsumable("")); Assert.IsNull(FindConsumable("ChoirIron"));
            }
            CollectionAssert.AreEqual(before, Inventory.Objects); Assert.AreEqual(0, Quantity(pair.empty)); Assert.AreEqual(2, Quantity(pair.positive));
            Assert.AreEqual(diagnostics, DiagQuery.Count(new DiagQuery.Filter()).Count); Assert.AreEqual(penalty, Player.GetStat("Speed").Penalty); Assert.AreEqual(0, PlayerReputation.Get("TentRight"));
        }
        [TestCase(5, false)] [TestCase(5, true)] [TestCase(0, false)] [TestCase(0, true)] [TestCase(-3, false)] [TestCase(-3, true)]
        public void ConfiguredConsumeOnlyZeroAndNegativeRewardsRetainTheirContracts(int reward, bool faction)
        {
            var npc = Speaker("SaltMaster"); npc.GetPart<WantsMineralPart>().RepReward = reward; npc.GetPart<WantsMineralPart>().Faction = faction ? "TentRight" : "";
            var salt = Carry("PaleSalt", 2); Assert.IsTrue(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "PaleSalt")));
            Assert.AreEqual(1, Quantity(salt)); Assert.AreEqual(faction ? reward : 0, PlayerReputation.Get("TentRight")); Assert.AreEqual(1, Records("Traded"));
        }
        [TestCase(false)] [TestCase(true)] public void CachedFoundingChoiceStillRevalidatesReachAndStanding(bool standing)
        {
            var npc = Speaker("FoundingPlaqueTender"); var stone = Carry("Tepuibone", 2); int row = CachedChoice(npc, "OfferFoundingStone");
            if (standing) PlayerReputation.Set("CatacombFolk", -1); else { Assert.IsTrue(Zone.RemoveEntity(npc)); Assert.IsTrue(Zone.AddEntity(npc, 30, 10)); }
            Assert.IsTrue(KeepTime(() => ConversationManager.SelectChoice(row))); Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);
            Assert.AreEqual(2, Quantity(stone)); Assert.AreEqual(standing ? -1 : 0, PlayerReputation.Get("CatacombFolk")); Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(FoundingTrustService.OfferedFact));
            Assert.AreEqual(1, Records("ConversationActionRejected")); Assert.IsFalse(ConversationManager.VisibleChoices.Any(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferFoundingStone")));
        }
        [TestCase(false)] [TestCase(true)] public void LegacyActionExecutionAndOverridesKeepExistingRegistryContracts(bool founding)
        {
            var npc = Speaker(founding ? "FoundingPlaqueTender" : "SaltMaster"); string action = founding ? "OfferFoundingStone" : "SellMineral", arg = founding ? "" : "PaleSalt";
            var sequence = new List<ConversationParam> { new ConversationParam { Key = action, Value = arg }, new ConversationParam { Key = "AddMessage", Value = "legacy continuation" } };
            ConversationActions.ExecuteAll(sequence, npc, Player); Assert.IsTrue(MessageLog.GetMessages().Contains("legacy continuation")); Assert.AreEqual(1, Records("ConversationActionRejected"));
            var record = DiagQuery.Apply(new DiagQuery.Filter { Kind = "ConversationActionRejected", Actor = Player.ID, Target = npc.ID }).Records.Single();
            var payload = UnityEngine.JsonUtility.FromJson<RequiredPayload>(record.PayloadJson);
            Assert.AreEqual(action, payload.action); Assert.AreEqual(arg, payload.argument); Assert.AreEqual(founding ? "founding_offer_refused" : "mineral_trade_refused", payload.reason);
            bool called = false; ConversationActions.Register(action, (_, __, ___) => called = true);
            Assert.IsTrue(ConversationActions.TryExecute(action, npc, Player, arg)); Assert.IsTrue(called);
            ConversationActions.Reset(); Assert.IsFalse(ConversationActions.TryExecute(action, npc, Player, arg)); Assert.AreEqual(2, Records("ConversationActionRejected"));
        }
        [Serializable] private class RequiredPayload { public string action, argument, reason; }
        [Test] public void RefusedCachedSaleCanBeRetriedOnTheSameNodeWithFreshSalt()
        {
            var npc = Speaker("SaltMaster"); var old = Carry("PaleSalt"); int row = CachedChoice(npc, "SellMineral"); Assert.IsTrue(Inventory.RemoveObject(old));
            Assert.IsTrue(KeepTime(() => ConversationManager.SelectChoice(row))); Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);
            var fresh = Carry("PaleSalt", 2); ConversationManager.RefreshVisibleChoices();
            row = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Key == "SellMineral")); Assert.GreaterOrEqual(row, 0);
            Assert.IsTrue(KeepTime(() => ConversationManager.SelectChoice(row))); Assert.AreEqual("Weighed", ConversationManager.CurrentNode.ID); Assert.AreEqual(1, Quantity(fresh)); Assert.AreEqual(5, PlayerReputation.Get("TentRight"));
        }
        [TestCase(2)] [TestCase(-1)] public void AnyNonzeroFoundingFactPreventsRepeatPayment(int fact)
        {
            var npc = Speaker("FoundingPlaqueTender"); var stone = Carry("Tepuibone", 2); NarrativeStatePart.Current.SetFact(FoundingTrustService.OfferedFact, fact);
            for (int i = 0; i < 10; i++) Assert.IsFalse(FoundingTrustService.CanOffer(Player, npc));
            Assert.IsFalse(KeepTime(() => ConversationActions.TryExecute("OfferFoundingStone", npc, Player, "")));
            Assert.AreEqual(2, Quantity(stone)); Assert.AreEqual(fact, NarrativeStatePart.Current.GetFact(FoundingTrustService.OfferedFact)); Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk"));
        }
        [TestCase(false)] [TestCase(true)] public void MineralElsewhereCannotPayFromThisInventory(bool otherActor)
        {
            var npc = Speaker("SaltMaster"); var salt = Item("PaleSalt");
            if (otherActor) Assert.IsTrue(Actor().GetPart<InventoryPart>().AddObject(salt));
            else { Assert.IsTrue(Zone.AddEntity(salt, 10, 10)); salt.GetPart<PhysicsPart>().InInventory = Player; }
            Assert.IsFalse(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "PaleSalt"))); Assert.AreEqual(1, Quantity(salt)); Assert.AreEqual(0, PlayerReputation.Get("TentRight")); Assert.AreEqual(0, Records("Traded"));
        }
    }
}
