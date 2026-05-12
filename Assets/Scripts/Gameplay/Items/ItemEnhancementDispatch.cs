using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.2.1 — central dispatcher for the content hooks
    /// declared on <see cref="IItemEnhancement"/>. Called from the few
    /// gameplay sites that the hooks care about
    /// (<c>CombatSystem.PerformSingleAttack</c>, <c>EquipCommand</c>,
    /// <c>UnequipCommand</c>), keeping each call site to a single line.
    ///
    /// <para><b>Why a dispatcher and not actor event subscription:</b>
    /// CoO events fire on the actor's Parts, but enhancement Parts live
    /// on the ITEM. The item's Parts never see actor events naturally.
    /// Mirroring Qud's IModification + IMeleeModification pattern, the
    /// item's enhancement Parts get called directly from the relevant
    /// gameplay path (combat, equip) with the item Entity as the lookup
    /// root. Tiny dispatcher class keeps the call sites trivial.</para>
    ///
    /// <para><b>Qud parity:</b> Qud's combat path (<c>Combat.MeleeAttack</c>)
    /// iterates <c>weapon.GetPartsDescendedFrom&lt;IMeleeModification&gt;()</c>
    /// and calls each Mod's hook directly. CoO mirrors this — we iterate
    /// <c>weapon.Parts</c> filtered to <see cref="IItemEnhancement"/>.</para>
    ///
    /// <para><b>Null-safety:</b> every method is fully null-safe — null
    /// item / null actor / null defender → no-op. Empty Parts list → no-op.
    /// No exception propagation from the hook is caught here — concrete
    /// enhancements are responsible for their own safety. (We'd rather
    /// fail loud during dev than silently swallow.)</para>
    /// </summary>
    public static class ItemEnhancementDispatch
    {
        /// <summary>
        /// Fired when <paramref name="weaponItem"/> was the weapon in a
        /// successful melee hit. Iterates the item's
        /// <see cref="IItemEnhancement"/> Parts and calls
        /// <see cref="IItemEnhancement.OnAttackerHit"/> on each.
        /// Called from <c>CombatSystem.PerformSingleAttack</c> inside the
        /// existing <c>if (hpAfter &gt; 0)</c> block, right after
        /// <c>OnHitWeaponEffects.Apply</c>.
        /// </summary>
        public static void DispatchOnHit(
            Entity weaponItem, Entity defender, Entity attacker,
            Damage damage, int actualDamage, Zone zone, Random rng)
        {
            if (weaponItem == null) return;
            // Cache count locally — enhancements should not mutate the
            // Parts list during dispatch, but iterating by index lets us
            // tolerate if one ever does (skips would be the worst case).
            var parts = weaponItem.Parts;
            if (parts == null) return;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] is IItemEnhancement enh)
                {
                    enh.OnAttackerHit(defender, attacker, damage, actualDamage, zone, rng);
                }
            }
        }

        /// <summary>
        /// Fired after the item is successfully equipped on the actor.
        /// Iterates the item's <see cref="IItemEnhancement"/> Parts and
        /// calls <see cref="IItemEnhancement.OnEquipped"/> on each.
        /// Called from <c>EquipCommand.Execute</c> after the
        /// <c>AfterEquip</c> event fires.
        /// </summary>
        public static void DispatchOnEquip(Entity actor, Entity item)
        {
            if (item == null) return;
            var parts = item.Parts;
            if (parts == null) return;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] is IItemEnhancement enh)
                {
                    enh.OnEquipped(actor, item);
                }
            }
        }

        /// <summary>
        /// Fired after the item is successfully unequipped from the actor.
        /// Iterates the item's <see cref="IItemEnhancement"/> Parts and
        /// calls <see cref="IItemEnhancement.OnUnequipped"/> on each.
        /// Called from <c>UnequipCommand.Execute</c> after the
        /// <c>AfterUnequip</c> event fires.
        /// </summary>
        public static void DispatchOnUnequip(Entity actor, Entity item)
        {
            if (item == null) return;
            var parts = item.Parts;
            if (parts == null) return;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] is IItemEnhancement enh)
                {
                    enh.OnUnequipped(actor, item);
                }
            }
        }
    }
}
