using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class PlayerLoadoutAdversarialTests : FiftyWorldFixture
    {
        [SetUp] public void InitializeSkills()
        {
            SkillRegistry.ResetForTests();
            SkillRegistry.EnsureInitialized();
        }

        [TearDown] public void ResetSkills() => SkillRegistry.ResetForTests();

        SavedEquipmentLoadout Save(string name)
        {
            Assert.True(PlayerLoadoutService.SaveCurrent(Actor, name, out var saved, out string reason), reason);
            return saved;
        }

        Entity Wear(string blueprint, BodyPart slot = null)
        {
            var item = Carry(blueprint);
            Assert.True(InventorySystem.Equip(Actor, item, slot), blueprint);
            return item;
        }

        void RemoveAll()
        {
            foreach (var item in Pack.GetAllEquipped().ToArray())
                Assert.True(InventorySystem.UnequipItem(Actor, item));
        }

        [Test]
        public void SavingCopiesAndDeletingArchivesCannotReplaceTheTwoPreparedSnapshots()
        {
            var empty = Save("Empty");
            var tooth = Wear("QuarryToothTrinket");
            var first = Save("Tooth");
            RemoveAll();
            var bell = Wear("TonguelessBellTrinket");
            var archived = Save("Tooth"); // A reused name must not overwrite a prepared ID.

            Assert.AreNotEqual(first.ID, archived.ID);
            Assert.AreEqual(empty.ID, PlayerLoadoutService.GetPrepared(Actor, 0).ID);
            Assert.AreEqual(first.ID, PlayerLoadoutService.GetPrepared(Actor, 1).ID);
            Assert.True(first.Items.Any(i => i.ItemID == tooth.ID));
            Assert.False(first.Items.Any(i => i.ItemID == bell.ID));
            Assert.False(PlayerLoadoutService.Prepare(Actor, archived.ID, 0, null, out _));
            Assert.False(PlayerLoadoutService.Delete(Actor, first.ID, null, out _));
            Assert.True(PlayerLoadoutService.Delete(Actor, archived.ID, null, out string reason), reason);
            var another = Save("Another archive");
            Assert.AreEqual(empty.ID, PlayerLoadoutService.GetPrepared(Actor, 0).ID);
            Assert.AreEqual(first.ID, PlayerLoadoutService.GetPrepared(Actor, 1).ID);
            Assert.AreNotEqual(another.ID, PlayerLoadoutService.GetPrepared(Actor, 0).ID);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Assert.AreEqual(empty.ID, PlayerLoadoutService.GetPrepared(loaded, 0).ID);
            Assert.AreEqual(first.ID, PlayerLoadoutService.GetPrepared(loaded, 1).ID);
            Assert.False(PlayerLoadoutService.Prepare(loaded, another.ID, 1, null, out _));
        }

        [Test]
        public void SoldEquipmentIsNotRetainedBySaveGraphOrReplacedByMatchingBlueprint()
        {
            var tooth = Wear("QuarryToothTrinket");
            var saved = Save("Lost tooth");
            RemoveAll();
            Save("Empty");
            Assert.True(Pack.RemoveObject(tooth));
            var seller = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "LoadoutForeignOwner" };
            seller.AddPart(new InventoryPart());
            Assert.True(seller.GetPart<InventoryPart>().AddObject(tooth));
            var replacement = Carry("QuarryToothTrinket");
            Assert.AreNotEqual(tooth.ID, replacement.ID);

            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            writer.WriteEntityReference(Actor);
            writer.WriteQueuedEntityBodies();
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            var loaded = reader.ReadEntityReference();
            reader.ReadEntityBodies();
            var graph = (IEnumerable<Entity>)typeof(SaveReader)
                .GetProperty("LoadedEntities", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
            Assert.False(graph.Any(e => e.ID == tooth.ID || e.ID == seller.ID),
                "An archived item ID must not retain a sold item or its new owner as entity references.");
            Assert.AreEqual(saved.ID, PlayerLoadoutService.GetPrepared(loaded, 0).ID);
            Assert.False(PlayerLoadoutService.CanApplyPrepared(loaded, 0, out _));
            var result = PlayerLoadoutService.ApplyPrepared(loaded, 0);
            Assert.False(result.Success);
            Assert.False(result.ActionCommitted);
            Assert.IsEmpty(loaded.GetPart<InventoryPart>().GetAllEquipped());
            Assert.True(loaded.GetPart<InventoryPart>().Objects.Any(i => i.ID == replacement.ID));
        }

        [Test]
        public void FailureAfterSecondEquipRestoresOriginalGearSkillCooldownAndBindings()
        {
            Wear("LeatherCap"); Wear("QuarryToothTrinket");
            Save("Cudgel kit");
            RemoveAll();
            var boots = Wear("IronshodBoots");
            var bell = Wear("TonguelessBellTrinket");
            Save("Speaker kit");
            var skills = Actor.GetPart<SkillsPart>();
            var abilities = Actor.GetPart<ActivatedAbilitiesPart>();
            var recruit = skills.GetSkill(nameof(Persuasion_Recruit));
            Guid abilityID = recruit.ActivatedAbilityID;
            abilities.AssignAbilityToSlot(abilityID, 7);
            abilities.GetAbility(abilityID).CooldownRemaining = 17;
            var stats = Actor.Statistics.ToDictionary(kv => kv.Key, kv => kv.Value.Value);
            var probe = new LoadoutSecondEquipFailureProbe();
            Actor.AddPart(probe);

            var result = PlayerLoadoutService.ApplyPrepared(Actor, 0, Zone);
            Assert.False(result.Success);
            Assert.False(result.ActionCommitted);
            Assert.AreEqual(2, probe.Calls, "Exercise failure after an earlier item has already been applied.");
            CollectionAssert.AreEquivalent(new[] { boots, bell }, Pack.GetAllEquipped());
            Assert.AreEqual(abilityID, skills.GetSkill(nameof(Persuasion_Recruit)).ActivatedAbilityID);
            Assert.AreEqual(abilityID, abilities.GetAbilityBySlot(7)?.ID);
            Assert.AreEqual(17, abilities.GetAbility(abilityID).CooldownRemaining);
            Assert.AreEqual(0, skills.GetRank(nameof(CudgelSkill)));
            foreach (var stat in stats) Assert.AreEqual(stat.Value, Actor.GetStatValue(stat.Key), stat.Key);
            Actor.RemovePart(probe);
            var success = PlayerLoadoutService.ApplyPrepared(Actor, 0, Zone);
            Assert.True(success.Success, success.ErrorMessage);
            Assert.True(success.ActionCommitted);
            var noChange = PlayerLoadoutService.ApplyPrepared(Actor, 0, Zone);
            Assert.True(noChange.Success, noChange.ErrorMessage);
            Assert.False(noChange.ActionCommitted, "A matching loadout is inspection, not another paid equipment change.");
        }

        [Test]
        public void ThreeHandedSavedWeaponRestoresExactSupportAndRefusesMissingLimb()
        {
            var body = Actor.GetPart<Body>();
            var originalHands = body.GetPartsByType("Hand").ToArray();
            var extra = body.AddPartByManager("loadout-extra-hand", body.GetBody(), "Hand", name: "third hand");
            var blocker = Wear("Dagger", originalHands[0]);
            var sword = Wear("Greatsword", extra);
            Assert.True(InventorySystem.UnequipItem(Actor, blocker));
            Assert.Null(originalHands[0]._Equipped);
            Assert.AreSame(sword, originalHands[1]._Equipped);
            Assert.True(extra.FirstSlotForEquipped);
            Save("Third hand sword");
            RemoveAll(); Save("Empty hands");

            var result = PlayerLoadoutService.ApplyPrepared(Actor, 0, Zone);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Null(originalHands[0]._Equipped, "Do not greedily take the first free support hand.");
            Assert.AreSame(sword, originalHands[1]._Equipped);
            Assert.AreSame(sword, extra._Equipped);
            Assert.True(extra.FirstSlotForEquipped);
            Assert.False(originalHands[1].FirstSlotForEquipped);
            Assert.True(PlayerLoadoutService.ApplyPrepared(Actor, 1, Zone).Success);
            Assert.AreEqual(1, body.RemovePartsByManager("loadout-extra-hand"));
            var refused = PlayerLoadoutService.ApplyPrepared(Actor, 0, Zone);
            Assert.False(refused.Success);
            Assert.False(refused.ActionCommitted);
            Assert.IsEmpty(Pack.GetAllEquipped());
            Assert.True(Pack.Objects.Contains(sword));
        }

        [Test]
        public void StunnedPlayerCannotSwitchOrPayForARefusedLoadout()
        {
            Wear("QuarryToothTrinket"); Save("Cudgel");
            RemoveAll(); Save("Empty");
            Assert.True(Actor.ApplyEffect(new StunnedEffect(5)));
            Assert.True(Actor.GetPart<StatusEffectsPart>().IsActionBlocked());
            Assert.False(PlayerLoadoutService.CanApplyPrepared(Actor, 0, out _));
            var refused = PlayerLoadoutService.ApplyPrepared(Actor, 0, Zone);
            Assert.False(refused.Success);
            Assert.False(refused.ActionCommitted);
            Assert.IsEmpty(Pack.GetAllEquipped());
            Assert.AreEqual(0, Actor.GetPart<SkillsPart>().GetRank(nameof(CudgelSkill)));
            var unchanged = PlayerLoadoutService.ApplyPrepared(Actor, 1, Zone);
            Assert.True(unchanged.Success, unchanged.ErrorMessage);
            Assert.False(unchanged.ActionCommitted);
        }
    }

    public sealed class LoadoutSecondEquipFailureProbe : Part
    {
        public int Calls;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "AfterEquip" && ++Calls == 2)
                throw new InvalidOperationException("Loadout second equipment failure probe");
            return true;
        }
    }
}
