using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ActivatedAbility + ActivatedAbilitiesPart contracts — slots,
    /// cooldowns, legacy-assignment migration, EndTurn ticking.
    ///
    /// <para>Extracted verbatim from <c>MutationSystemTests</c> during
    /// the mutations→skills migration (M4): the ability substrate
    /// outlives the mutation system that first hosted these tests.</para>
    /// </summary>
    public class ActivatedAbilitiesPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ========================
        // ActivatedAbility (Data Class)
        // ========================

        [Test]
        public void ActivatedAbility_IsUsable_WhenNoCooldown()
        {
            var ability = new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                DisplayName = "Test",
                Command = "CommandTest",
                CooldownRemaining = 0
            };
            Assert.IsTrue(ability.IsUsable);
        }

        [Test]
        public void ActivatedAbility_NotUsable_WhenOnCooldown()
        {
            var ability = new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                DisplayName = "Test",
                Command = "CommandTest",
                CooldownRemaining = 5
            };
            Assert.IsFalse(ability.IsUsable);
        }

        // ========================
        // ActivatedAbilitiesPart
        // ========================

        [Test]
        public void ActivatedAbilitiesPart_AddAbility_ReturnsGuid()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Flaming Hands", "CommandFlamingHands", "Physical Mutations");
            Assert.AreNotEqual(Guid.Empty, id);
            Assert.AreEqual(1, part.AbilityList.Count);
        }

        [Test]
        public void ActivatedAbilitiesPart_GetAbility_ReturnsByGuid()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "CommandTest", "Test");
            var ability = part.GetAbility(id);
            Assert.IsNotNull(ability);
            Assert.AreEqual("Test", ability.DisplayName);
            Assert.AreEqual("CommandTest", ability.Command);
        }

        [Test]
        public void ActivatedAbilitiesPart_GetAbilityBySlot_ReturnsCorrectOrder()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            part.AddAbility("First", "Cmd1", "Class");
            part.AddAbility("Second", "Cmd2", "Class");

            Assert.AreEqual("First", part.GetAbilityBySlot(0).DisplayName);
            Assert.AreEqual("Second", part.GetAbilityBySlot(1).DisplayName);
            Assert.IsNull(part.GetAbilityBySlot(2));
            Assert.IsNull(part.GetAbilityBySlot(-1));
        }

        [Test]
        public void ActivatedAbilitiesPart_RemoveAbility_RemovesFromBoth()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            Assert.IsTrue(part.RemoveAbility(id));
            Assert.AreEqual(0, part.AbilityList.Count);
            Assert.IsNull(part.GetAbility(id));
        }

        [Test]
        public void ActivatedAbilitiesPart_RemoveAbility_ReturnsFalseForUnknown()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            Assert.IsFalse(part.RemoveAbility(Guid.NewGuid()));
        }

        [Test]
        public void ActivatedAbilitiesPart_CooldownAbility_SetsCooldown()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            part.CooldownAbility(id, 10);

            var ability = part.GetAbility(id);
            Assert.AreEqual(10, ability.CooldownRemaining);
            Assert.AreEqual(10, ability.MaxCooldown);
            Assert.IsFalse(ability.IsUsable);
        }

        [Test]
        public void ActivatedAbilitiesPart_TickCooldowns_ReducesByOne()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            part.CooldownAbility(id, 3);

            part.TickCooldowns();
            Assert.AreEqual(2, part.GetAbility(id).CooldownRemaining);

            part.TickCooldowns();
            Assert.AreEqual(1, part.GetAbility(id).CooldownRemaining);

            part.TickCooldowns();
            Assert.AreEqual(0, part.GetAbility(id).CooldownRemaining);
            Assert.IsTrue(part.GetAbility(id).IsUsable);
        }

        [Test]
        public void ActivatedAbilitiesPart_TickCooldowns_DoesNotGoNegative()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            // Already at 0

            part.TickCooldowns();
            Assert.AreEqual(0, part.GetAbility(id).CooldownRemaining);
        }

        // ========================
        // Slot assignment (Phase-C grimoire slot binding)
        // ========================

        [Test]
        public void ActivatedAbilitiesPart_AddAbility_AutoBindsToFirstEmptySlot()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var a = part.AddAbility("A", "CmdA", "Class");
            var b = part.AddAbility("B", "CmdB", "Class");

            Assert.AreEqual(a, part.SlotAssignments[0]);
            Assert.AreEqual(b, part.SlotAssignments[1]);
            // Remaining slots stay empty.
            for (int i = 2; i < ActivatedAbilitiesPart.SlotCount; i++)
                Assert.AreEqual(Guid.Empty, part.SlotAssignments[i]);
        }

        [Test]
        public void ActivatedAbilitiesPart_AddAbility_OverflowStaysInListWithoutSlot()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var ids = new Guid[ActivatedAbilitiesPart.SlotCount + 1];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = part.AddAbility("A" + i, "Cmd" + i, "Class");

            // All 10 slots are filled with the first 10 IDs.
            for (int i = 0; i < ActivatedAbilitiesPart.SlotCount; i++)
                Assert.AreEqual(ids[i], part.SlotAssignments[i]);

            // The 11th ability still exists in AbilityList / AbilityByGuid.
            Assert.AreEqual(ActivatedAbilitiesPart.SlotCount + 1, part.AbilityList.Count);
            Assert.IsNotNull(part.GetAbility(ids[ActivatedAbilitiesPart.SlotCount]));

            // ...but it's not bound to any slot.
            Assert.AreEqual(-1, part.GetSlotForAbility(ids[ActivatedAbilitiesPart.SlotCount]));
        }

        [Test]
        public void ActivatedAbilitiesPart_AssignAbilityToSlot_MovesFromOtherSlot()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var a = part.AddAbility("A", "CmdA", "Class");
            var b = part.AddAbility("B", "CmdB", "Class");
            // Starting layout: [0]=A, [1]=B.

            part.AssignAbilityToSlot(a, 5);

            // A moved to slot 5; slot 0 is empty; slot 1 still holds B (no duplicates).
            Assert.AreEqual(Guid.Empty, part.SlotAssignments[0]);
            Assert.AreEqual(b, part.SlotAssignments[1]);
            Assert.AreEqual(a, part.SlotAssignments[5]);
            Assert.AreEqual(5, part.GetSlotForAbility(a));
        }

        [Test]
        public void ActivatedAbilitiesPart_AssignAbilityToSlot_EmptyGuidClearsSlot()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var a = part.AddAbility("A", "CmdA", "Class");
            Assert.AreEqual(a, part.SlotAssignments[0]);

            part.AssignAbilityToSlot(Guid.Empty, 0);
            Assert.AreEqual(Guid.Empty, part.SlotAssignments[0]);
            // Ability still exists in the dictionary, just unbound.
            Assert.IsNotNull(part.GetAbility(a));
        }

        [Test]
        public void ActivatedAbilitiesPart_AssignAbilityToSlot_OutOfRange_IsNoOp()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var a = part.AddAbility("A", "CmdA", "Class");
            part.AssignAbilityToSlot(a, 42);
            part.AssignAbilityToSlot(a, -1);

            // Original binding on slot 0 is untouched.
            Assert.AreEqual(a, part.SlotAssignments[0]);
        }

        [Test]
        public void ActivatedAbilitiesPart_AssignAbilityToSlot_UnknownIdIsNoOp()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var a = part.AddAbility("A", "CmdA", "Class");
            Guid stranger = Guid.NewGuid();
            part.AssignAbilityToSlot(stranger, 3);

            // Slot 3 stays empty; slot 0 still holds A.
            Assert.AreEqual(Guid.Empty, part.SlotAssignments[3]);
            Assert.AreEqual(a, part.SlotAssignments[0]);
        }

        [Test]
        public void ActivatedAbilitiesPart_RemoveAbility_ClearsSlot()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var a = part.AddAbility("A", "CmdA", "Class");
            Assert.AreEqual(a, part.SlotAssignments[0]);

            Assert.IsTrue(part.RemoveAbility(a));
            Assert.AreEqual(Guid.Empty, part.SlotAssignments[0]);
            Assert.IsNull(part.GetAbilityBySlot(0));
        }

        [Test]
        public void ActivatedAbilitiesPart_GetAbilityBySlot_ReadsSlotAssignments()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var a = part.AddAbility("A", "CmdA", "Class");
            var b = part.AddAbility("B", "CmdB", "Class");

            // Swap the two slots manually and verify GetAbilityBySlot follows.
            part.AssignAbilityToSlot(a, 7);
            part.AssignAbilityToSlot(b, 2);

            Assert.AreEqual("A", part.GetAbilityBySlot(7).DisplayName);
            Assert.AreEqual("B", part.GetAbilityBySlot(2).DisplayName);
            Assert.IsNull(part.GetAbilityBySlot(0));
            Assert.IsNull(part.GetAbilityBySlot(10));
        }

        [Test]
        public void ActivatedAbilitiesPart_MigrateLegacyAssignments_BackfillsFromAbilityList()
        {
            // Simulate a save made before slot bindings existed: AbilityList is
            // populated but SlotAssignments is empty.
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            // Bypass AddAbility to avoid auto-binding. Directly poke the dict + list.
            var a = new ActivatedAbility { ID = Guid.NewGuid(), DisplayName = "Legacy A", Command = "CmdA" };
            var b = new ActivatedAbility { ID = Guid.NewGuid(), DisplayName = "Legacy B", Command = "CmdB" };
            part.AbilityByGuid[a.ID] = a;
            part.AbilityByGuid[b.ID] = b;
            part.AbilityList.Add(a);
            part.AbilityList.Add(b);
            for (int i = 0; i < ActivatedAbilitiesPart.SlotCount; i++)
                part.SlotAssignments[i] = Guid.Empty;

            part.MigrateLegacyAssignments();

            Assert.AreEqual(a.ID, part.SlotAssignments[0]);
            Assert.AreEqual(b.ID, part.SlotAssignments[1]);
            for (int i = 2; i < ActivatedAbilitiesPart.SlotCount; i++)
                Assert.AreEqual(Guid.Empty, part.SlotAssignments[i]);
        }

        [Test]
        public void ActivatedAbilitiesPart_MigrateLegacyAssignments_LeavesExplicitBindingsAlone()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var a = part.AddAbility("A", "CmdA", "Class");
            var b = part.AddAbility("B", "CmdB", "Class");
            part.AssignAbilityToSlot(a, 4);
            part.AssignAbilityToSlot(b, 9);

            part.MigrateLegacyAssignments();

            Assert.AreEqual(a, part.SlotAssignments[4]);
            Assert.AreEqual(b, part.SlotAssignments[9]);
            // Slot 0 and 1 should NOT be backfilled because the array is already non-empty.
            Assert.AreEqual(Guid.Empty, part.SlotAssignments[0]);
            Assert.AreEqual(Guid.Empty, part.SlotAssignments[1]);
        }

        [Test]
        public void ActivatedAbilitiesPart_MigrateLegacyAssignments_NormalizesShortArray()
        {
            // A partial save where the array was shorter than SlotCount (shouldn't happen
            // with current code but future-proofs the migration path).
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            part.SlotAssignments = new Guid[3]; // too short
            var a = new ActivatedAbility { ID = Guid.NewGuid(), DisplayName = "A", Command = "C" };
            part.AbilityByGuid[a.ID] = a;
            part.AbilityList.Add(a);

            part.MigrateLegacyAssignments();

            Assert.AreEqual(ActivatedAbilitiesPart.SlotCount, part.SlotAssignments.Length);
            Assert.AreEqual(a.ID, part.SlotAssignments[0]);
        }

        [Test]
        public void ActivatedAbilitiesPart_EndTurnEvent_TicksCooldowns()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            part.CooldownAbility(id, 5);

            entity.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(4, part.GetAbility(id).CooldownRemaining);
        }

    }
}
