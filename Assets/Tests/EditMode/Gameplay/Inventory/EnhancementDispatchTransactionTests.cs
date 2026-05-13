using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// E.4.2 cold-eye Finding #1 — pin the contract that
    /// <c>EquipCommand</c>'s <see cref="ItemEnhancementDispatch.DispatchOnEquip"/>
    /// and <c>UnequipCommand</c>'s <see cref="ItemEnhancementDispatch.DispatchOnUnequip"/>
    /// participate in the inventory <see cref="InventoryTransaction"/>'s
    /// rollback path.
    ///
    /// <para><b>Why this matters:</b> the audit caught that the
    /// pre-fix dispatcher calls fired OUTSIDE the transaction, so a
    /// caller wrapping EquipCommand in a larger orchestration and
    /// rolling back AFTER equip-success would silently leave the
    /// enhancement's mutation (Lacquered AV bump, Engraved rep flow,
    /// GlowQuartz radius bump) permanent.</para>
    ///
    /// <para>These tests use the dispatcher directly + a manual
    /// <see cref="InventoryTransaction"/> to demonstrate the
    /// undo path is wired correctly without going through the full
    /// EquipCommand machinery (which is exercised by its own test
    /// suite).</para>
    /// </summary>
    public class EnhancementDispatchTransactionTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            PlayerReputation.Reset();
            Diag.ResetAll();
            EnhancementFactory.ForceReinitialize();
            EnhancementFactory.EnsureInitialized();
        }

        private static Entity MakeArmor(int baseAV = 3)
        {
            var e = new Entity { ID = "leather", BlueprintName = "leather" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "leather" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new ArmorPart { AV = baseAV });
            // Engraved.Applicable requires EquippablePart; add it so the
            // Engraved-rep test gets a valid attachment.
            e.AddPart(new EquippablePart { Slot = "Body" });
            return e;
        }

        private static Entity MakePlayer()
        {
            var e = new Entity { ID = "hero", BlueprintName = "hero" };
            e.Tags["Player"] = "";
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "hero" });
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // Lacquered: transaction.Do(undo=DispatchOnUnequip) unwinds AV
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchOnEquip_WrappedInTransaction_RollbackReversesAvBonus()
        {
            // Build the transaction-wrapped dispatch pattern that
            // EquipCommand uses post-fix. Rollback must unwind the
            // Lacquered AV bonus.
            var armor = MakeArmor(baseAV: 3);
            ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 2);
            var player = MakePlayer();
            var tx = new InventoryTransaction();

            // Mirror EquipCommand's transaction wrap (post-fix).
            ItemEnhancementDispatch.DispatchOnEquip(player, armor);
            tx.Do(
                apply: null,
                undo: () => ItemEnhancementDispatch.DispatchOnUnequip(player, armor));

            Assert.AreEqual(5, armor.GetPart<ArmorPart>().AV,
                "Precondition: equip applied +2 AV.");

            tx.Rollback();

            Assert.AreEqual(3, armor.GetPart<ArmorPart>().AV,
                "Rollback unwinds the AV bonus via the registered undo. " +
                "Without the transaction wrap, AV would stay at 5 (latent leak).");
            Assert.IsTrue(tx.IsRolledBack);
        }

        // ════════════════════════════════════════════════════════════════
        // Engraved: transaction rollback reverses faction rep
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchOnEquip_WrappedInTransaction_RollbackReversesEngravedRep()
        {
            var armor = MakeArmor();
            ItemEnhancing.Apply(armor, nameof(EnhancementEngraved), tier: 2);
            armor.GetPart<EnhancementEngraved>().Faction = "Villagers";
            var player = MakePlayer();
            int repBefore = PlayerReputation.Get("Villagers");
            var tx = new InventoryTransaction();

            ItemEnhancementDispatch.DispatchOnEquip(player, armor);
            tx.Do(
                apply: null,
                undo: () => ItemEnhancementDispatch.DispatchOnUnequip(player, armor));

            Assert.AreEqual(repBefore + 10, PlayerReputation.Get("Villagers"),
                "Precondition: equip applied +10 rep.");

            tx.Rollback();

            Assert.AreEqual(repBefore, PlayerReputation.Get("Villagers"),
                "Rollback unwinds rep delta via the registered undo.");
        }

        // ════════════════════════════════════════════════════════════════
        // Commit path: undo NOT fired on commit (sanity check)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchOnEquip_TransactionCommit_DoesNotInvokeUndo()
        {
            // Counter-check: a normal commit (the happy path EquipCommand
            // hits) does NOT fire the registered undo. Pin against a
            // future change that accidentally invokes undo on commit.
            var armor = MakeArmor(baseAV: 3);
            ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 2);
            var tx = new InventoryTransaction();

            ItemEnhancementDispatch.DispatchOnEquip(MakePlayer(), armor);
            tx.Do(
                apply: null,
                undo: () => ItemEnhancementDispatch.DispatchOnUnequip(MakePlayer(), armor));

            Assert.AreEqual(5, armor.GetPart<ArmorPart>().AV);

            tx.Commit();

            Assert.AreEqual(5, armor.GetPart<ArmorPart>().AV,
                "Commit preserves the AV bump — undo NOT fired on commit.");
            Assert.IsTrue(tx.IsCommitted);
        }

        // ════════════════════════════════════════════════════════════════
        // UnequipCommand path: dispatcher's transaction.Do(undo=DispatchOnEquip)
        // re-applies the enhancement on rollback.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DispatchOnUnequip_WrappedInTransaction_RollbackRestoresAvBonus()
        {
            // Symmetric: unequip's transaction wrap. Rollback re-applies
            // OnEquipped, so the AV bonus comes back.
            var armor = MakeArmor(baseAV: 3);
            ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 2);
            var player = MakePlayer();

            // Get into the "equipped" state first.
            ItemEnhancementDispatch.DispatchOnEquip(player, armor);
            Assert.AreEqual(5, armor.GetPart<ArmorPart>().AV);

            // Now unequip + register the re-equip undo (mirrors
            // UnequipCommand's transaction wrap post-fix).
            var tx = new InventoryTransaction();
            ItemEnhancementDispatch.DispatchOnUnequip(player, armor);
            tx.Do(
                apply: null,
                undo: () => ItemEnhancementDispatch.DispatchOnEquip(player, armor));

            Assert.AreEqual(3, armor.GetPart<ArmorPart>().AV,
                "Precondition: unequip removed +2.");

            tx.Rollback();

            Assert.AreEqual(5, armor.GetPart<ArmorPart>().AV,
                "Rollback re-fires OnEquipped, restoring +2 AV.");
        }
    }
}
