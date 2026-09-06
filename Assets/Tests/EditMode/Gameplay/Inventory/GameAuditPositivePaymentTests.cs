using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    /// <summary>A47: actual planting and mineral dialogue only succeed after a real positive payment.</summary>
    public abstract class PositivePaymentFixture : StackIdentityFixture
    {
        protected Entity Player;
        protected Zone Zone;
        protected TurnManager Turns;
        protected AfterPlantProbe After;
        private Zone _oldZone;
        private NarrativeStatePart _oldNarrative;
        private EntityFactory _oldSeeds, _oldConversations;
        private TurnManager _oldTurns;
        private Entity _oldWorld;
        protected InventoryPart Inventory => Player.GetPart<InventoryPart>();
        [SetUp] public void BeginPayments()
        {
            _oldZone = SettlementRuntime.ActiveZone; _oldNarrative = NarrativeStatePart.Current;
            _oldSeeds = SeedPart.Factory; _oldConversations = ConversationActions.Factory;
            _oldTurns = TurnManager.Active; _oldWorld = TurnManager.World; TurnManager.World = null;
            ConversationManager.EndConversation(); ConversationLoader.Reset(); ConversationActions.Reset(); ConversationPredicates.Reset();
            FactionManager.Initialize(); PlayerReputation.Reset();
            NarrativeStatePart.Current = new NarrativeStatePart();
            SeedPart.Factory = ConversationActions.Factory = Factory;
            foreach (var file in new[] { "FriendlyNPCs.json", "FoundingVillage.json" })
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Conversations", file)), file);
            Player = Actor(); Inventory.MaxWeight = -1; After = new AfterPlantProbe(); Player.AddPart(After);
            Zone = SettlementRuntime.ActiveZone = new Zone("PositivePayments"); Assert.IsTrue(Zone.AddEntity(Player, 10, 10));
            Turns = new TurnManager(); Turns.AddEntity(Player); Turns.ProcessUntilPlayerTurn();
            Diag.SetChannel("crop", true); Diag.SetChannel("event", true); Diag.SetChannel("mineral-trade", true); Diag.SetChannel("furniture", true);
            MessageLog.Clear();
        }
        [TearDown] public void EndPayments()
        {
            ConversationManager.EndConversation(); ConversationLoader.Reset(); ConversationActions.Reset(); ConversationPredicates.Reset();
            SeedPart.Factory = _oldSeeds; ConversationActions.Factory = _oldConversations;
            SettlementRuntime.ActiveZone = _oldZone; NarrativeStatePart.Current = _oldNarrative;
            TurnManager.World = _oldWorld;
            typeof(TurnManager).GetProperty(nameof(TurnManager.Active)).GetSetMethod(true).Invoke(null, new object[] { _oldTurns });
            PlayerReputation.Reset(); FactionManager.Reset();
        }
        protected Entity Carry(string bp, int count = 1)
        {
            var item = Item(bp); if (item.GetPart<StackerPart>() is StackerPart stack) stack.StackCount = count;
            Assert.IsTrue(Inventory.AddObject(item)); Assert.IsTrue(Inventory.Objects.Contains(item)); return item;
        }
        protected (Entity empty, Entity positive) Mixed(string bp, int emptyCount)
        {
            var first = Carry(bp); var stack = first.GetPart<StackerPart>(); int max = stack.MaxStack; stack.MaxStack = 1;
            var positive = Carry(bp, 2); stack.MaxStack = max; stack.StackCount = emptyCount;
            Assert.AreNotSame(first, positive); Assert.Less(Inventory.Objects.IndexOf(first), Inventory.Objects.IndexOf(positive)); return (first, positive);
        }
        protected Entity Place(string bp)
        { var item = Item(bp); Assert.IsTrue(Zone.AddEntity(item, 10, 10)); return item; }
        protected Entity Speaker(string bp)
        { var item = Item(bp); Assert.IsTrue(Zone.AddEntity(item, 11, 10)); return item; }
        protected bool KeepTime(Func<bool> action)
        { int ticks = Turns.TickCount, energy = Turns.GetEnergy(Player); bool ok = action(); Assert.AreEqual(ticks, Turns.TickCount); Assert.AreEqual(energy, Turns.GetEnergy(Player)); return ok; }
        protected bool Plant(Entity seed) => KeepTime(() => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(seed, "PlantSeed"), Player, Zone).Success);
        protected int Records(string kind) => DiagQuery.Count(new DiagQuery.Filter { Kind = kind, Actor = Player.ID }).Count;
        protected Entity[] Crops() => Zone.GetCell(10, 10).Objects.Where(i => i.HasPart<CropPart>()).ToArray();
        protected static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        protected static bool PlantRow(Entity item, Entity actor) => WorldInteractionSystem.GatherActions(item, actor).Any(a => a.Command == "PlantSeed");
        protected int CachedChoice(Entity npc, string action)
        {
            Assert.IsTrue(ConversationManager.StartConversation(npc, Player));
            int row = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Key == action));
            Assert.GreaterOrEqual(row, 0, "actual authored choice is initially available"); return row;
        }
        public class AfterPlantProbe : Part
        {
            public int Count;
            public override string Name => "AfterPlantProbe";
            public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction" && e.GetStringParameter("Command") == "PlantSeed") Count++; return true; }
        }
    }
    public class GameAuditPositivePaymentTests : PositivePaymentFixture
    {
        [TestCase("floor", "not_plantable")] [TestCase("occupied", "already_planted")] [TestCase("barren", "placement_refused")]
        public void RefusedPlantingReportsFailureAndNeverFiresAfterAction(string condition, string reason)
        {
            if (condition == "floor") Place("Floor");
            else if (condition == "barren") { var bare = Place("FellingBarePosition"); bare.SetTag("Plantable"); }
            else { Place("Grass"); Assert.IsTrue(Plant(Carry("EmberwheatSeed"))); }
            var original = Crops(); var seed = Carry("CandyCarrotSeed", 2); int after = After.Count, planted = Records("CropPlanted");
            Assert.IsFalse(Plant(seed)); CollectionAssert.AreEqual(original, Crops()); Assert.AreEqual(2, Quantity(seed));
            Assert.IsTrue(Inventory.Objects.Contains(seed)); Assert.AreSame(Player, seed.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(after, After.Count); Assert.AreEqual(planted, Records("CropPlanted"));
            StringAssert.Contains(reason, DiagQuery.Apply(new DiagQuery.Filter { Kind = "PlantRejected", Actor = Player.ID }).Records.Last().PayloadJson);
        }
        [TestCase(0)] [TestCase(-1)] public void EmptySeedOffersNoPlantActionAndCannotGrow(int count)
        {
            Place("Grass"); var seed = Carry("CandyCarrotSeed", count);
            Assert.IsFalse(PlantRow(seed, Player)); Assert.IsFalse(Plant(seed)); Assert.IsEmpty(Crops());
            Assert.AreEqual(count, Quantity(seed)); Assert.IsTrue(Inventory.Objects.Contains(seed)); Assert.AreEqual(0, After.Count); Assert.AreEqual(0, Records("CropPlanted"));
        }
        [Test] public void ExplicitMenuActorCannotBorrowAnotherActorsCarriedSeed()
        {
            var other = Actor(); var seed = Item("CandyCarrotSeed"); Assert.IsTrue(other.GetPart<InventoryPart>().AddObject(seed));
            Assert.IsTrue(PlantRow(seed, other)); Assert.IsTrue(PlantRow(seed, null)); Assert.IsFalse(PlantRow(seed, Player));
            Assert.AreSame(other, seed.GetPart<PhysicsPart>().InInventory); Assert.IsFalse(Inventory.Objects.Contains(seed));
        }
        [TestCase(1)] [TestCase(3)] public void PositiveSeedPaysOneAndReportsExactlyOnePlant(int count)
        {
            Place("Grass"); var seed = Carry("CandyCarrotSeed", count); Assert.IsTrue(PlantRow(seed, Player)); Assert.IsTrue(Plant(seed));
            Assert.AreEqual(1, Crops().Length); Assert.AreEqual("CandyCarrotCrop", Crops()[0].BlueprintName);
            Assert.AreEqual(count > 1, Inventory.Objects.Contains(seed)); Assert.AreEqual(count > 1 ? count - 1 : 1, Quantity(seed));
            Assert.AreSame(count > 1 ? Player : null, seed.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(1, After.Count); Assert.AreEqual(1, Records("CropPlanted"));
            Assert.IsTrue(MessageLog.GetMessages().Contains(Player.GetDisplayName() + " plants " + seed.GetPart<RenderPart>().DisplayName + "."), "planting prose must describe one unit, not the remaining stack");
        }
        [TestCase(0)] [TestCase(-1)] public void EmptyMineralCannotBuyReputation(int count)
        {
            var npc = Speaker("SaltMaster"); var mineral = Carry("PaleSalt", count);
            Assert.IsFalse(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "PaleSalt")));
            Assert.AreEqual(0, PlayerReputation.Get("TentRight")); Assert.AreEqual(count, Quantity(mineral)); Assert.IsTrue(Inventory.Objects.Contains(mineral)); Assert.AreEqual(0, Records("Traded"));
        }
        [TestCase(0)] [TestCase(-1)] public void MineralTradeSkipsEmptyFirstMatchAndPaysPositiveUnit(int count)
        {
            var npc = Speaker("SaltMaster"); var pair = Mixed("PaleSalt", count);
            Assert.IsTrue(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "PaleSalt")));
            Assert.AreEqual(count, Quantity(pair.empty)); Assert.IsTrue(Inventory.Objects.Contains(pair.empty)); Assert.AreEqual(1, Quantity(pair.positive));
            Assert.IsTrue(Inventory.Objects.Contains(pair.positive)); Assert.AreEqual(5, PlayerReputation.Get("TentRight")); Assert.AreEqual(1, Records("Traded"));
        }
        [TestCase(1)] [TestCase(3)] public void ActualSaltMasterPaysFiveStandingPerOneUnit(int count)
        {
            var npc = Speaker("SaltMaster"); var mineral = Carry("PaleSalt", count);
            Assert.IsTrue(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "PaleSalt")));
            Assert.AreEqual(count > 1, Inventory.Objects.Contains(mineral)); Assert.AreEqual(count > 1 ? count - 1 : 1, Quantity(mineral));
            Assert.AreEqual(5, PlayerReputation.Get("TentRight")); Assert.AreEqual(1, Records("Traded"));
        }
        [Test] public void UnwantedMineralNeverPaysOrDisappears()
        {
            var npc = Speaker("SaltMaster"); var mineral = Carry("ChoirIron"); Assert.IsFalse(KeepTime(() => MineralTradeService.TryTrade(Player, npc, "ChoirIron")));
            Assert.IsTrue(Inventory.Objects.Contains(mineral)); Assert.AreEqual(0, PlayerReputation.Get("TentRight")); Assert.AreEqual(0, Records("Traded"));
        }
        [TestCase(0)] [TestCase(-1)] public void FoundingFindsPositiveStoneAfterEmptyEntry(int count)
        {
            var npc = Speaker("FoundingPlaqueTender"); var pair = Mixed("Tepuibone", count);
            Assert.IsTrue(FoundingTrustService.CanOffer(Player, npc)); Assert.IsTrue(KeepTime(() => FoundingTrustService.TryOffer(Player, npc)));
            Assert.AreEqual(count, Quantity(pair.empty)); Assert.IsTrue(Inventory.Objects.Contains(pair.empty)); Assert.AreEqual(1, Quantity(pair.positive));
            Assert.AreEqual(50, PlayerReputation.Get("CatacombFolk")); Assert.AreEqual(1, NarrativeStatePart.Current.GetFact(FoundingTrustService.OfferedFact));
        }
        [TestCase(0)] [TestCase(-1)] public void EmptyFoundingOfferIsARequiredActionRefusal(int count)
        {
            var npc = Speaker("FoundingPlaqueTender"); var mineral = Carry("Tepuibone", count);
            Assert.IsFalse(FoundingTrustService.CanOffer(Player, npc)); Assert.IsFalse(KeepTime(() => ConversationActions.TryExecute("OfferFoundingStone", npc, Player, "")));
            Assert.AreEqual(1, Records("ConversationActionRejected")); Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk"));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(FoundingTrustService.OfferedFact)); Assert.IsTrue(Inventory.Objects.Contains(mineral));
        }
        [Test] public void FoundingPaysOnceAndRequiredRepeatRefuses()
        {
            var npc = Speaker("FoundingPlaqueTender"); var mineral = Carry("Tepuibone", 2);
            Assert.IsTrue(KeepTime(() => ConversationActions.TryExecute("OfferFoundingStone", npc, Player, "")));
            Assert.IsFalse(KeepTime(() => ConversationActions.TryExecute("OfferFoundingStone", npc, Player, "")));
            Assert.AreEqual(1, Quantity(mineral)); Assert.AreEqual(50, PlayerReputation.Get("CatacombFolk"));
            Assert.AreEqual(1, Records("ConversationActionApplied")); Assert.AreEqual(1, Records("ConversationActionRejected")); Assert.AreEqual(1, Records("Traded"));
        }
        [TestCase("SaltMaster", "remove")] [TestCase("SaltMaster", "empty")] [TestCase("SaltMaster", "keep")]
        [TestCase("FoundingPlaqueTender", "remove")]
        public void CachedAuthoredChoiceMustPayBeforeItCanAdvance(string speaker, string state)
        {
            bool founding = speaker == "FoundingPlaqueTender"; var npc = Speaker(speaker); var item = Carry(founding ? "Tepuibone" : "PaleSalt", 2);
            string action = founding ? "OfferFoundingStone" : "SellMineral"; int choice = CachedChoice(npc, action);
            if (state == "remove") Assert.IsTrue(Inventory.RemoveObject(item)); else if (state == "empty") item.GetPart<StackerPart>().StackCount = 0;
            Assert.IsTrue(KeepTime(() => ConversationManager.SelectChoice(choice)), "dialogue remains active; this bool is not action success");
            Assert.AreEqual(state == "keep" ? "Weighed" : "Start", ConversationManager.CurrentNode.ID);
            Assert.AreEqual(state == "keep" ? 5 : 0, PlayerReputation.Get(founding ? "CatacombFolk" : "TentRight"));
            Assert.AreEqual(state == "keep" ? 0 : 1, Records("ConversationActionRejected")); Assert.AreEqual(state == "keep" ? 1 : 0, Records("ConversationActionApplied"));
            if (state != "keep") Assert.IsFalse(ConversationManager.VisibleChoices.Any(c => c.Actions != null && c.Actions.Any(a => a.Key == action)));
            if (!founding && state != "keep") Assert.IsTrue(MessageLog.GetMessages().Contains("Nothing to weigh."));
            Assert.AreEqual(state == "empty" ? 0 : state == "keep" ? 1 : 2, Quantity(item));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(FoundingTrustService.OfferedFact));
        }
        [TestCase(false)] [TestCase(true)] public void RequiredSaleStopsFollowingActionOnlyWhenPaymentRefuses(bool paid)
        {
            var npc = Speaker("SaltMaster"); if (paid) Carry("PaleSalt");
            var actions = new List<ConversationParam> { new ConversationParam { Key = "SellMineral", Value = "PaleSalt" }, new ConversationParam { Key = "AddMessage", Value = "payment sequence sentinel" } };
            Assert.AreEqual(paid, KeepTime(() => ConversationActions.TryExecuteAll(actions, npc, Player)));
            Assert.AreEqual(paid, MessageLog.GetMessages().Contains("payment sequence sentinel")); Assert.AreEqual(paid ? 5 : 0, PlayerReputation.Get("TentRight"));
        }
        public class CropPaymentProbe : Part
        {
            public static Action<Entity> Callback;
            public override string Name => "CropPaymentProbe";
            public override void Initialize() { Callback?.Invoke(ParentEntity); }
        }
        [TestCase("remove")] [TestCase("empty")] [TestCase("keep")]
        public void CropCreationMustStillPayBeforeAPlacedCropCanRemain(string change)
        {
            Place("Grass"); var unrelated = Place("Torch"); var seed = Carry("CandyCarrotSeed", 2); Entity created = null;
            Factory.RegisterPartType<CropPaymentProbe>();
            var parts = Factory.Blueprints["CandyCarrotCrop"].Parts;
            Assert.IsFalse(parts.ContainsKey("CropPaymentProbe")); parts.Add("CropPaymentProbe", new Dictionary<string, string>());
            CropPaymentProbe.Callback = crop =>
            {
                created = crop;
                if (change == "remove") Assert.IsTrue(Inventory.RemoveObject(seed));
                else if (change == "empty") seed.GetPart<StackerPart>().StackCount = 0;
            };
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Actor", (object)Player); e.SetParameter("Zone", (object)Zone); e.SetParameter("Command", "PlantSeed");
            bool propagated = false, handled = false;
            try { propagated = seed.FireEvent(e); handled = e.Handled; }
            finally { e.Release(); CropPaymentProbe.Callback = null; parts.Remove("CropPaymentProbe"); }
            Assert.NotNull(created, "actual crop factory initializer ran"); bool success = change == "keep";
            Assert.AreEqual(!success, propagated); Assert.AreEqual(success, handled); Assert.AreEqual(success ? 1 : 0, Crops().Length);
            Assert.AreEqual(success, Zone.GetEntityCell(created) != null); Assert.AreEqual(success, Zone.GetEntitiesWithTag("Crop").Contains(created)); Assert.IsTrue(Zone.GetAllEntities().Contains(unrelated));
            Assert.AreEqual(success ? 1 : 0, Records("CropPlanted")); Assert.AreEqual(0, After.Count, "direct event avoids broader inventory-action snapshot contract");
            Assert.AreEqual(change != "remove", Inventory.Objects.Contains(seed)); Assert.AreEqual(change == "empty" ? 0 : success ? 1 : 2, Quantity(seed));
        }
    }
}
