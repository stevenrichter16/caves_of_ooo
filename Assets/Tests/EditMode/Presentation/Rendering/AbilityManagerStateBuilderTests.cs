using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.0 — Tests for <see cref="AbilityManagerStateBuilder"/>.
    /// Covers the pure-data builder + the SlotToHotkey / HotkeyToSlot
    /// translation. Tests for the actual UI MonoBehaviour
    /// (AbilityManagerUI) live in PlayMode tests since they require
    /// a Tilemap.
    /// </summary>
    public class AbilityManagerStateBuilderTests
    {
        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeBareActor(string name = "actor")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.AddPart(new RenderPart { DisplayName = name });
            return e;
        }

        private static Entity MakeActorWithAbilities(int count = 3)
        {
            var e = MakeBareActor();
            var abilities = new ActivatedAbilitiesPart();
            e.AddPart(abilities);
            for (int i = 0; i < count; i++)
            {
                abilities.AddAbility(
                    displayName: "Ability " + i,
                    command: "CommandTest" + i,
                    abilityClass: "Skills");
            }
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // Build — null/empty cases
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Build_NullActor_ReturnsEmptySnapshot()
        {
            var snap = AbilityManagerStateBuilder.Build(null);
            Assert.AreEqual(0, snap.RowCount);
        }

        [Test]
        public void Build_ActorWithoutAbilitiesPart_ReturnsEmptySnapshot()
        {
            var snap = AbilityManagerStateBuilder.Build(MakeBareActor());
            Assert.AreEqual(0, snap.RowCount);
        }

        [Test]
        public void Build_ActorWithEmptyAbilitiesList_ReturnsEmptySnapshot()
        {
            var actor = MakeBareActor();
            actor.AddPart(new ActivatedAbilitiesPart());
            var snap = AbilityManagerStateBuilder.Build(actor);
            Assert.AreEqual(0, snap.RowCount);
        }

        // ════════════════════════════════════════════════════════════════
        // Build — populated case
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Build_ActorWith3Abilities_ReturnsThreeRows()
        {
            var snap = AbilityManagerStateBuilder.Build(MakeActorWithAbilities(3));
            Assert.AreEqual(3, snap.RowCount);
            Assert.AreEqual(3, snap.Rows.Count);
        }

        [Test]
        public void Build_RowsCarryAbilityIDAndDisplayName()
        {
            var actor = MakeActorWithAbilities(2);
            var snap = AbilityManagerStateBuilder.Build(actor);
            // Both rows should have non-empty Guid + display name from
            // the AddAbility calls.
            foreach (var row in snap.Rows)
            {
                Assert.AreNotEqual(System.Guid.Empty, row.AbilityID,
                    "Each row must carry a non-empty ability Guid.");
                Assert.IsTrue(row.DisplayName.StartsWith("Ability "),
                    "Each row's DisplayName must come from the ability's DisplayName field.");
                Assert.AreEqual("Skills", row.SourceClass,
                    "Source class must come from the ability's Class field.");
            }
        }

        [Test]
        public void Build_AutoBoundAbilities_HotkeyMatchesAutoSlot()
        {
            // AddAbility auto-binds to first empty slot. So 3 added
            // abilities should be in slots 0, 1, 2 — hotkeys '1', '2', '3'.
            var actor = MakeActorWithAbilities(3);
            var snap = AbilityManagerStateBuilder.Build(actor);

            // Snapshot is sorted by class+name, so order matches AddAbility
            // order (all "Skills" class, names "Ability 0", "1", "2").
            Assert.AreEqual('1', snap.Rows[0].Hotkey);
            Assert.AreEqual('2', snap.Rows[1].Hotkey);
            Assert.AreEqual('3', snap.Rows[2].Hotkey);
        }

        [Test]
        public void Build_UnboundAbility_HotkeyIsDash()
        {
            // Add an ability + manually unbind its slot. Snapshot should
            // show '-' for the unbound row.
            var actor = MakeActorWithAbilities(1);
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            abilities.SlotAssignments[0] = System.Guid.Empty;  // manual unbind

            var snap = AbilityManagerStateBuilder.Build(actor);
            Assert.AreEqual(1, snap.RowCount);
            Assert.AreEqual('-', snap.Rows[0].Hotkey,
                "Unbound ability must render hotkey as '-'.");
            Assert.AreEqual(-1, snap.Rows[0].SlotIndex,
                "Unbound ability's SlotIndex must be -1.");
        }

        [Test]
        public void Build_AbilityOnCooldown_RowReflectsCooldownAndIsUsableFalse()
        {
            var actor = MakeActorWithAbilities(1);
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            abilities.AbilityList[0].MaxCooldown = 50;
            abilities.AbilityList[0].CooldownRemaining = 5;

            var snap = AbilityManagerStateBuilder.Build(actor);
            Assert.AreEqual(5, snap.Rows[0].CooldownRemaining);
            Assert.AreEqual(50, snap.Rows[0].MaxCooldown);
            Assert.IsFalse(snap.Rows[0].IsUsable,
                "IsUsable must be false when CooldownRemaining > 0.");
        }

        // ════════════════════════════════════════════════════════════════
        // Sort order — by Class first, then DisplayName
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Build_SortsByClassThenName_ForDeterministicOrder()
        {
            var actor = MakeBareActor();
            var abilities = new ActivatedAbilitiesPart();
            actor.AddPart(abilities);

            // Add in jumbled order — different classes + non-alphabetical
            // names. Snapshot should sort to: (Mutations, "Bolt"),
            // (Mutations, "Lance"), (Skills, "Apple"), (Skills, "Slam").
            abilities.AddAbility("Slam", "CommandSlam", "Skills");
            abilities.AddAbility("Lance", "CommandLance", "Mutations");
            abilities.AddAbility("Apple", "CommandApple", "Skills");
            abilities.AddAbility("Bolt", "CommandBolt", "Mutations");

            var snap = AbilityManagerStateBuilder.Build(actor);
            Assert.AreEqual(4, snap.RowCount);

            // Mutations come first (alphabetically before "Skills").
            Assert.AreEqual("Mutations", snap.Rows[0].SourceClass);
            Assert.AreEqual("Bolt", snap.Rows[0].DisplayName);
            Assert.AreEqual("Mutations", snap.Rows[1].SourceClass);
            Assert.AreEqual("Lance", snap.Rows[1].DisplayName);
            Assert.AreEqual("Skills", snap.Rows[2].SourceClass);
            Assert.AreEqual("Apple", snap.Rows[2].DisplayName);
            Assert.AreEqual("Skills", snap.Rows[3].SourceClass);
            Assert.AreEqual("Slam", snap.Rows[3].DisplayName);
        }

        // ════════════════════════════════════════════════════════════════
        // SlotToHotkey / HotkeyToSlot — round-trip
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void SlotToHotkey_ReturnsExpectedCharacters()
        {
            for (int slot = 0; slot < 9; slot++)
                Assert.AreEqual((char)('1' + slot),
                    AbilityManagerStateBuilder.SlotToHotkey(slot),
                    $"Slot {slot} should map to '{(char)('1' + slot)}'.");
            Assert.AreEqual('0', AbilityManagerStateBuilder.SlotToHotkey(9),
                "Slot 9 should map to '0' (the 10th key).");
            Assert.AreEqual('-', AbilityManagerStateBuilder.SlotToHotkey(-1),
                "Slot -1 (unbound) should map to '-'.");
            Assert.AreEqual('-', AbilityManagerStateBuilder.SlotToHotkey(10),
                "Slot 10 (out of range) should map to '-'.");
        }

        [Test]
        public void HotkeyToSlot_RoundTripsSlotToHotkey()
        {
            // For every valid slot, SlotToHotkey → HotkeyToSlot must
            // round-trip. -1 doesn't round-trip because '-' isn't a
            // valid hotkey input.
            for (int slot = 0; slot < 10; slot++)
            {
                char hotkey = AbilityManagerStateBuilder.SlotToHotkey(slot);
                int recovered = AbilityManagerStateBuilder.HotkeyToSlot(hotkey);
                Assert.AreEqual(slot, recovered,
                    $"Slot {slot} → '{hotkey}' → slot {recovered} should round-trip.");
            }
        }

        [Test]
        public void HotkeyToSlot_InvalidChar_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, AbilityManagerStateBuilder.HotkeyToSlot('-'));
            Assert.AreEqual(-1, AbilityManagerStateBuilder.HotkeyToSlot('a'));
            Assert.AreEqual(-1, AbilityManagerStateBuilder.HotkeyToSlot(' '));
        }

        // ════════════════════════════════════════════════════════════════
        // Reorder semantics — verify the underlying ActivatedAbilitiesPart
        // contract that AbilityManagerUI relies on
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AssignAbilityToSlot_SwapOnConflict_PreservesContract()
        {
            // Pin the contract that AbilityManagerUI's BindSelectedToSlot
            // depends on: when ability A is in slot 0 and the user binds
            // B to slot 0, A is unbound (its slot becomes -1) and B takes
            // slot 0. Without this guarantee the manager UI would create
            // duplicate bindings.
            var actor = MakeActorWithAbilities(2);
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var idA = abilities.AbilityList[0].ID;
            var idB = abilities.AbilityList[1].ID;

            // Pre: A in slot 0, B in slot 1.
            Assert.AreEqual(0, abilities.GetSlotForAbility(idA));
            Assert.AreEqual(1, abilities.GetSlotForAbility(idB));

            // Move B to slot 0.
            abilities.AssignAbilityToSlot(idB, 0);

            Assert.AreEqual(0, abilities.GetSlotForAbility(idB),
                "B should now be in slot 0.");
            Assert.AreEqual(-1, abilities.GetSlotForAbility(idA),
                "A should be unbound after B took its slot (swap-on-conflict).");
        }

        [Test]
        public void AssignAbilityToSlot_EmptyGuid_ClearsSlot()
        {
            // Pin the contract for the Unbind path: passing Guid.Empty
            // clears the target slot.
            var actor = MakeActorWithAbilities(1);
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var idA = abilities.AbilityList[0].ID;

            abilities.AssignAbilityToSlot(System.Guid.Empty, 0);

            Assert.AreEqual(-1, abilities.GetSlotForAbility(idA),
                "Assigning Guid.Empty to slot 0 must unbind whatever was there.");
            Assert.IsNull(abilities.GetAbilityBySlot(0),
                "Slot 0 should now return null from GetAbilityBySlot.");
        }

        // ════════════════════════════════════════════════════════════════
        // BuildRowDescription — per-row 1-line synthesis
        // ════════════════════════════════════════════════════════════════
        //
        // The description row is part of the player-facing UX contract.
        // These tests pin the wording so a future contributor can't
        // silently reword the strings (which would inconsistently match
        // the player's mental model of what a key press does).

        [Test]
        public void BuildRowDescription_BoundAndReady_ReturnsBoundReadyText()
        {
            var row = new AbilityManagerRow(
                abilityID: System.Guid.NewGuid(),
                displayName: "Slam",
                sourceClass: "Skills",
                hotkey: '1',
                slotIndex: 0,
                cooldownRemaining: 0,
                maxCooldown: 50,
                isUsable: true);
            string desc = AbilityManagerStateBuilder.BuildRowDescription(row);
            Assert.AreEqual("Bound to [1] - ready to use.", desc);
        }

        [Test]
        public void BuildRowDescription_BoundAndOnCooldown_ShowsCooldownProgress()
        {
            var row = new AbilityManagerRow(
                abilityID: System.Guid.NewGuid(),
                displayName: "Slam",
                sourceClass: "Skills",
                hotkey: '1',
                slotIndex: 0,
                cooldownRemaining: 12,
                maxCooldown: 50,
                isUsable: false);
            string desc = AbilityManagerStateBuilder.BuildRowDescription(row);
            Assert.AreEqual("Bound to [1] - cooldown: 12T / 50T.", desc,
                "Cooldown row should show remaining/max so player can " +
                "estimate when to come back.");
        }

        [Test]
        public void BuildRowDescription_UnboundAndReady_HintsToAssignAndCast()
        {
            var row = new AbilityManagerRow(
                abilityID: System.Guid.NewGuid(),
                displayName: "Berserk",
                sourceClass: "Skills",
                hotkey: '-',
                slotIndex: -1,
                cooldownRemaining: 0,
                maxCooldown: 100,
                isUsable: true);
            string desc = AbilityManagerStateBuilder.BuildRowDescription(row);
            Assert.AreEqual("Unbound. Press 0-9 to assign a slot, Enter to cast.", desc,
                "Unbound + ready row should hint the two paths forward " +
                "(bind to a slot OR activate directly via Enter).");
        }

        [Test]
        public void BuildRowDescription_UnboundAndOnCooldown_ShowsCooldownProgress()
        {
            var row = new AbilityManagerRow(
                abilityID: System.Guid.NewGuid(),
                displayName: "Berserk",
                sourceClass: "Skills",
                hotkey: '-',
                slotIndex: -1,
                cooldownRemaining: 75,
                maxCooldown: 100,
                isUsable: false);
            string desc = AbilityManagerStateBuilder.BuildRowDescription(row);
            Assert.AreEqual("Unbound - cooldown: 75T / 100T.", desc);
        }

        [Test]
        public void BuildRowDescription_AllRowsLessThan60Chars_FitInPopupWidth()
        {
            // The popup is 60 chars wide. The description row reserves
            // 2 chars on each side for borders, so the description text
            // itself has 56 chars. Verify all 4 description shapes fit.
            const int maxLen = 56;

            string[] descs = new[]
            {
                AbilityManagerStateBuilder.BuildRowDescription(new AbilityManagerRow(
                    System.Guid.NewGuid(), "X", "Y", '1', 0, 0, 50, true)),
                AbilityManagerStateBuilder.BuildRowDescription(new AbilityManagerRow(
                    System.Guid.NewGuid(), "X", "Y", '0', 9, 999, 9999, false)),
                AbilityManagerStateBuilder.BuildRowDescription(new AbilityManagerRow(
                    System.Guid.NewGuid(), "X", "Y", '-', -1, 0, 100, true)),
                AbilityManagerStateBuilder.BuildRowDescription(new AbilityManagerRow(
                    System.Guid.NewGuid(), "X", "Y", '-', -1, 999, 9999, false)),
            };

            foreach (var desc in descs)
            {
                Assert.LessOrEqual(desc.Length, maxLen,
                    "Description '" + desc + "' (" + desc.Length + " chars) " +
                    "exceeds the popup width budget (" + maxLen + " chars). " +
                    "The renderer's truncation tilde would kick in and clip the text.");
            }
        }
    }
}
