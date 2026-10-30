using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Authored trinkets through ordinary trade, equipment, readers and save paths.</summary>
    public sealed class TrinketEquipmentTests : FiftyWorldFixture
    {
        const string Tooth = "QuarryToothTrinket";
        const string Bell = "TonguelessBellTrinket";

        Entity Wear(string blueprint)
        {
            var item = Carry(blueprint);
            Assert.True(InventorySystem.Equip(Actor, item), blueprint);
            return item;
        }

        static void AssertStats(Entity actor, int strength, int agility, int ego)
        {
            Assert.AreEqual(strength, actor.GetStatValue("Strength"), "Strength");
            Assert.AreEqual(agility, actor.GetStatValue("Agility"), "Agility");
            Assert.AreEqual(ego, actor.GetStatValue("Ego"), "Ego");
        }

        static void AssertSkills(Entity actor, int cudgel, int persuasion)
        {
            var skills = actor.GetPart<SkillsPart>();
            Assert.AreEqual(cudgel, skills?.GetRank("CudgelSkill") ?? 0);
            Assert.AreEqual(persuasion, skills?.GetRank("Persuasion_Recruit") ?? 0);
            Assert.AreEqual(persuasion, skills?.GetRank("Persuasion_Dismiss") ?? 0);
            Assert.AreEqual(0, skills?.GetLearnedRank("CudgelSkill") ?? 0);
            Assert.AreEqual(0, skills?.GetLearnedRank("Persuasion_Recruit") ?? 0);
        }

        static void AssertWorn(Entity actor, Entity item)
        {
            Assert.True(InventorySystem.IsEquipped(actor, item));
            Assert.AreSame(actor, item.GetPart<PhysicsPart>().Equipped);
            Assert.Null(item.GetPart<PhysicsPart>().InInventory);
            Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreEqual(1, actor.GetPart<Body>().GetPartsByType(TrinketSlots.SlotType)
                .Count(p => ReferenceEquals(p._Equipped, item)));
        }

        [Test]
        public void TwoTrinketsCombineAndRemoveIndependentlyOfBootsAndATonic()
        {
            int strength = Actor.GetStatValue("Strength"), agility = Actor.GetStatValue("Agility"),
                ego = Actor.GetStatValue("Ego"), speed = Actor.GetStatValue("Speed");
            Assert.AreEqual(TrinketSlots.DefaultCapacity, TrinketSlots.GetCapacity(Actor));
            var boots = Wear("IronshodBoots");
            Assert.AreEqual(speed - 5, Actor.GetStatValue("Speed"));
            Assert.True(Actor.ApplyEffect(new TonicStatSurgeEffect("Strength", 3, 20)));
            var tooth = Wear(Tooth);
            var bell = Wear(Bell);
            AssertWorn(Actor, tooth); AssertWorn(Actor, bell);
            AssertStats(Actor, strength + 3, agility, ego);
            AssertSkills(Actor, 1, 1);

            Assert.True(InventorySystem.UnequipItem(Actor, tooth));
            AssertStats(Actor, strength + 3, agility, ego);
            AssertSkills(Actor, 0, 1);
            Assert.True(Actor.RemoveEffect<TonicStatSurgeEffect>());
            AssertStats(Actor, strength, agility, ego);
            AssertSkills(Actor, 0, 1);
            AssertWorn(Actor, bell);
            Assert.True(InventorySystem.IsEquipped(Actor, boots));
            Assert.AreEqual(speed - 5, Actor.GetStatValue("Speed"));

            Assert.True(InventorySystem.UnequipItem(Actor, bell));
            AssertStats(Actor, strength, agility, ego);
            Assert.True(InventorySystem.UnequipItem(Actor, boots));
            Assert.AreEqual(speed, Actor.GetStatValue("Speed"));
        }

        [Test]
        public void RepeatedEquipAndSlotExpansionDoNotMultiplyContributions()
        {
            int strength = Actor.GetStatValue("Strength"), agility = Actor.GetStatValue("Agility"),
                ego = Actor.GetStatValue("Ego");
            var tooth = Wear(Tooth);
            for (int attempt = 0; attempt < 3; attempt++) InventorySystem.Equip(Actor, tooth);
            Assert.True(TrinketSlots.EnsureMinimum(Actor, 3));
            Assert.True(TrinketSlots.EnsureMinimum(Actor, 3));
            Assert.AreEqual(3, TrinketSlots.GetCapacity(Actor));
            AssertWorn(Actor, tooth);
            AssertStats(Actor, strength, agility, ego);
            AssertSkills(Actor, 1, 0);
            Assert.True(InventorySystem.UnequipItem(Actor, tooth));
            Assert.False(InventorySystem.UnequipItem(Actor, tooth));
            AssertStats(Actor, strength, agility, ego);
            AssertSkills(Actor, 0, 0);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SavedEquipmentRestoresOnceAndUnequipPreservesLaterBaseStatChanges(bool wholeSession)
        {
            int strength = Actor.GetStatValue("Strength"), agility = Actor.GetStatValue("Agility"),
                ego = Actor.GetStatValue("Ego");
            var tooth = Wear(Tooth); var bell = Wear(Bell);
            Assert.True(TrinketSlots.EnsureMinimum(Actor, 3));
            // Training while wearing a trinket must survive its eventual removal.
            Actor.GetStat("Strength").BaseValue += 3;
            Assert.True(Actor.ApplyEffect(new TonicStatSurgeEffect("Strength", 2, 20)));
            var loaded = wholeSession ? RoundTripSession().Player
                : PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded);
            Assert.AreEqual(3, TrinketSlots.GetCapacity(loaded));
            var equipment = loaded.GetPart<InventoryPart>().GetAllEquipped().ToArray();
            var savedTooth = equipment.Single(i => i.ID == tooth.ID);
            var savedBell = equipment.Single(i => i.ID == bell.ID);
            AssertWorn(loaded, savedTooth); AssertWorn(loaded, savedBell);
            AssertStats(loaded, strength + 5, agility, ego);
            AssertSkills(loaded, 1, 1);

            Assert.True(InventorySystem.UnequipItem(loaded, savedTooth));
            AssertStats(loaded, strength + 5, agility, ego);
            AssertSkills(loaded, 0, 1);
            Assert.True(InventorySystem.UnequipItem(loaded, savedBell));
            AssertStats(loaded, strength + 5, agility, ego);
            AssertSkills(loaded, 0, 0);
            Assert.True(loaded.RemoveEffect<TonicStatSurgeEffect>());
            AssertStats(loaded, strength + 3, agility, ego);
            Assert.AreSame(loaded, savedTooth.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(loaded, savedBell.GetPart<PhysicsPart>().InInventory);
        }

        GameSessionState RoundTripSession()
        {
            var manager = new OverworldZoneManager(null, 812);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } },
                Zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            Clock.RestoreSavedState(Clock.TickCount, true, Actor,
                new List<TurnManager.SavedTurnEntry> {
                    new TurnManager.SavedTurnEntry { Entity = Actor, Energy = 1000 }
                });
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture(
                Guid.NewGuid().ToString("N"), "trinket-equipment", manager, Clock, Actor));
        }

        [TestCase("BeforeEquip", false)]
        [TestCase("AfterEquip", true)]
        public void CancelledOrThrowingEquipKeepsOtherTrinketAndOriginalStats(string eventId, bool throws)
        {
            var tooth = Wear(Tooth); var bell = Carry(Bell);
            int strength = Actor.GetStatValue("Strength"), agility = Actor.GetStatValue("Agility"),
                ego = Actor.GetStatValue("Ego");
            var probe = new TrinketEquipmentFailureProbe { EventId = eventId, Throws = throws };
            Actor.AddPart(probe);
            Assert.False(InventorySystem.Equip(Actor, bell));
            Assert.AreEqual(1, probe.Calls);
            AssertStats(Actor, strength, agility, ego);
            AssertSkills(Actor, 1, 0);
            AssertWorn(Actor, tooth);
            Assert.True(Pack.Objects.Contains(bell));
            Assert.AreSame(Actor, bell.GetPart<PhysicsPart>().InInventory);
            Assert.Null(bell.GetPart<PhysicsPart>().Equipped);
            Actor.RemovePart(probe);
            Assert.True(InventorySystem.Equip(Actor, bell));
            AssertStats(Actor, strength, agility, ego);
            AssertSkills(Actor, 1, 1);
        }

        [TestCase("BeforeUnequip", false)]
        [TestCase("AfterUnequip", true)]
        public void CancelledOrThrowingUnequipRestoresOwnershipAndOnlyItsOwnContribution(string eventId, bool throws)
        {
            var tooth = Wear(Tooth); var bell = Wear(Bell);
            int strength = Actor.GetStatValue("Strength"), agility = Actor.GetStatValue("Agility"),
                ego = Actor.GetStatValue("Ego");
            var probe = new TrinketEquipmentFailureProbe { EventId = eventId, Throws = throws };
            Actor.AddPart(probe);
            Assert.False(InventorySystem.UnequipItem(Actor, tooth));
            Assert.AreEqual(1, probe.Calls);
            AssertWorn(Actor, tooth); AssertWorn(Actor, bell);
            AssertStats(Actor, strength, agility, ego);
            AssertSkills(Actor, 1, 1);
            Actor.RemovePart(probe);
            Assert.True(InventorySystem.UnequipItem(Actor, tooth));
            AssertStats(Actor, strength, agility, ego);
            AssertSkills(Actor, 0, 1);
            AssertWorn(Actor, bell);
        }

        [Test]
        public void InventoryRowsComparisonAndExamineDescribePositiveSkillsWithoutEquipping()
        {
            var tooth = Carry(Tooth); var bell = Carry(Bell);
            int strength = Actor.GetStatValue("Strength"), agility = Actor.GetStatValue("Agility"),
                ego = Actor.GetStatValue("Ego");
            var screen = InventoryScreenData.Build(Actor);
            var group = screen.Categories.Single(g => g.CategoryName == "Trinkets");
            Assert.AreEqual(2, group.Items.Count);
            Assert.AreEqual(2, screen.Equipment.Count(s => s.SlotName == TrinketSlots.SlotType));
            Assert.True(screen.Equipment.Where(s => s.SlotName == TrinketSlots.SlotType)
                .All(s => s.EquippedItem == null));
            Assert.True(EquipmentComparisonService.TryDescribe(Actor, tooth, out string comparison, out string reason), reason);
            StringAssert.Contains("Equip slots: Trinket", comparison);
            StringAssert.Contains("Cudgel +1", comparison);
            StringAssert.Contains("Cudgel rank: 0 -> 1", comparison);
            StringAssert.DoesNotContain("Agility -1", comparison);
            StringAssert.Contains("Auto equip", comparison);
            var document = ExamineDocumentBuilder.BuildInventory(bell, Actor);
            Assert.NotNull(document);
            StringAssert.Contains("Recruit +1", document.ToPlainText());
            StringAssert.Contains("Dismiss +1", document.ToPlainText());
            StringAssert.DoesNotContain("Strength -1", document.ToPlainText());
            AssertStats(Actor, strength, agility, ego);
            Assert.IsEmpty(Pack.GetAllEquipped());

            Assert.True(InventorySystem.Equip(Actor, tooth));
            Assert.True(InventorySystem.Equip(Actor, bell));
            var rows = InventoryScreenData.Build(Actor).Equipment
                .Where(s => s.SlotName == TrinketSlots.SlotType).ToArray();
            CollectionAssert.AreEquivalent(new[] { tooth, bell }, rows.Select(s => s.EquippedItem));
            Assert.AreEqual(2, rows.Select(s => s.BodyPartID).Distinct().Count());
            Assert.True(EquipmentComparisonService.TryDescribe(Actor, bell, out comparison, out reason), reason);
            StringAssert.Contains("Currently equipped:", comparison);
        }

        [Test]
        public void MorrowfastMenderSellsActualSingletonTrinketsThatCanBothBeWorn()
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var seller = MorrowfastContent.CreateResident("southwest-craftsperson", content.Factory);
                Assert.NotNull(seller);
                var shelf = seller.GetPart<InventoryPart>();
                TradeSystem.SetDrams(Actor, 1000);
                int startingMoney = TradeSystem.GetDrams(Actor), sellerMoney = TradeSystem.GetDrams(seller), paid = 0;
                int strength = Actor.GetStatValue("Strength"), agility = Actor.GetStatValue("Agility"),
                    ego = Actor.GetStatValue("Ego");
                foreach (string blueprint in new[] { Tooth, Bell })
                {
                    var item = shelf.Objects.Single(i => i.BlueprintName == blueprint);
                    Assert.True(item.HasTag("Trinket"));
                    Assert.AreEqual("Trinkets", ItemCategory.GetCategory(item));
                    Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
                    Assert.AreEqual(1, item.GetPart<StackerPart>().MaxStack);
                    int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(Actor), seller);
                    Assert.Greater(price, 0);
                    Assert.True(TradeSystem.BuyFromTrader(Actor, seller, item, out string failure), failure);
                    paid += price;
                    Assert.False(shelf.Objects.Contains(item));
                    Assert.True(Pack.Objects.Contains(item));
                    Assert.True(InventorySystem.Equip(Actor, item));
                    AssertWorn(Actor, item);
                }
                Assert.AreEqual(startingMoney - paid, TradeSystem.GetDrams(Actor));
                Assert.AreEqual(sellerMoney + paid, TradeSystem.GetDrams(seller));
                AssertStats(Actor, strength, agility, ego);
                AssertSkills(Actor, 1, 1);
            }
        }

        [Test]
        public void DuelistsKnotsRespectRankCapAndComparisonPreservesLearnedTraining()
        {
            var skills = Actor.GetPart<SkillsPart>();
            Assert.True(skills.AddSkill(nameof(LongBlades_Deflect)));
            skills.GetSkill(nameof(LongBlades_Deflect)).Rank = 2;
            var first = Wear("DuelistsKnotTrinket");
            var second = Carry("DuelistsKnotTrinket");
            Assert.AreEqual(3, skills.GetRank(nameof(LongBlades_Deflect)));
            Assert.True(EquipmentComparisonService.TryDescribe(Actor, second, out string text, out string reason), reason);
            StringAssert.Contains("Deflect rank: 3 -> 3 (change: +0; maximum 3)", text);
            Assert.AreEqual(2, skills.GetLearnedRank(nameof(LongBlades_Deflect)));
            Assert.True(InventorySystem.Equip(Actor, second));
            Assert.True(InventorySystem.UnequipItem(Actor, first));
            Assert.AreEqual(3, skills.GetRank(nameof(LongBlades_Deflect)));
            Assert.True(InventorySystem.UnequipItem(Actor, second));
            Assert.AreEqual(2, skills.GetRank(nameof(LongBlades_Deflect)));
        }
    }

    public sealed class TrinketEquipmentFailureProbe : Part
    {
        public string EventId;
        public bool Throws;
        public int Calls;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != EventId) return true;
            Calls++;
            if (Throws) throw new InvalidOperationException("trinket equipment rollback probe");
            return false;
        }
    }
}
