using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.1.4 — public API for applying / removing
    /// enhancements on items. Wraps <see cref="EnhancementFactory"/>
    /// + slot-cap veto + <see cref="IItemEnhancement.Applicable"/>
    /// gate + diag emission.
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>XRL.World.Tinkering/ItemModding.ApplyModification(obj, name, tier)</c>.
    /// CoO simplifies away Qud's Bits economy (Lockdown — one mineral
    /// consumed = one enhancement applied, no per-Mod bit cost).</para>
    ///
    /// <para><b>Slot cap (Lockdown #6):</b> max
    /// <see cref="MAX_ENHANCEMENTS_PER_ITEM"/> enhancements per item.
    /// At cap, <see cref="Apply"/> vetoes — no destroy-the-item path
    /// in v1 (matches F.3.3 veto-mode philosophy; revisit after playtest).</para>
    /// </summary>
    public static class ItemEnhancing
    {
        /// <summary>Lockdown #6 — slot cap per item.</summary>
        public const int MAX_ENHANCEMENTS_PER_ITEM = 2;

        /// <summary>Diag category — registered as on-by-default in
        /// <see cref="Diag.DefaultOnCategories"/>.</summary>
        public const string DIAG_CATEGORY = "enhancement";

        /// <summary>
        /// Apply a named enhancement to an item at the given tier.
        /// Returns true on success. Emits <c>enhancement/Applied</c>
        /// on success, <c>enhancement/ApplyFailed</c> on rejection
        /// (with <c>reason</c> in the payload).
        ///
        /// <para><b>Rejection paths:</b></para>
        /// <list type="bullet">
        ///   <item><c>null_item</c> — item is null</item>
        ///   <item><c>null_name</c> — enhancement name is null/empty</item>
        ///   <item><c>unknown_enhancement</c> — name not registered</item>
        ///   <item><c>not_applicable</c> — <see cref="IItemEnhancement.Applicable"/> returned false</item>
        ///   <item><c>at_slot_cap</c> — item already has
        ///         <see cref="MAX_ENHANCEMENTS_PER_ITEM"/> enhancements</item>
        ///   <item><c>instantiation_failed</c> — <see cref="EnhancementFactory.Create"/> returned null</item>
        /// </list>
        /// </summary>
        /// <summary>
        /// Apply a named enhancement to an item at the given tier.
        /// <para><b>Deep-audit fix (Bug #1):</b> if <paramref name="wielder"/>
        /// is non-null AND the item is currently equipped on that wielder
        /// (per <c>InventoryPart.EquippedItems</c>), the enhancement's
        /// <see cref="IItemEnhancement.OnEquipped"/> hook fires
        /// immediately after attachment so the gameplay-visible bonus
        /// lands without a re-equip cycle. Callers that don't know who
        /// holds the item (e.g. world-gen content path) pass null.</para>
        /// </summary>
        public static bool Apply(Entity item, string enhancementName, int tier = 1,
            Entity wielder = null)
        {
            // E.3.4: auto-register all concrete IItemEnhancement subclasses
            // on first call. No-op in test contexts that called
            // ResetForTests (which suppresses auto-load to preserve
            // test isolation).
            EnhancementFactory.EnsureInitialized();

            if (item == null)
            {
                EmitApplyFailed(null, enhancementName, tier, "null_item");
                return false;
            }
            if (string.IsNullOrEmpty(enhancementName))
            {
                EmitApplyFailed(item, enhancementName, tier, "null_name");
                return false;
            }
            if (!EnhancementFactory.TryGet(enhancementName, out _))
            {
                EmitApplyFailed(item, enhancementName, tier, "unknown_enhancement");
                return false;
            }

            // Slot-cap veto BEFORE instantiation (cheap check first).
            int currentCount = CountEnhancements(item);
            if (currentCount >= MAX_ENHANCEMENTS_PER_ITEM)
            {
                EmitApplyFailed(item, enhancementName, tier, "at_slot_cap");
                return false;
            }

            var inst = EnhancementFactory.Create(enhancementName, tier);
            if (inst == null)
            {
                EmitApplyFailed(item, enhancementName, tier, "instantiation_failed");
                return false;
            }

            if (!inst.Applicable(item))
            {
                EmitApplyFailed(item, enhancementName, tier, "not_applicable");
                return false;
            }

            // Attach + fire the enhancement's Apply hook.
            item.AddPart(inst);
            inst.Apply(item);

            // Deep-audit Bug #1 fix: if the item is currently equipped on
            // the supplied wielder, fire OnEquipped immediately so the
            // gameplay-visible bonus lands without requiring a re-equip
            // cycle (player Tinker-applies Lacquered to a currently-worn
            // armor → AV bumps now, not after unequip/re-equip).
            if (wielder != null && IsItemEquippedOn(item, wielder))
            {
                inst.OnEquipped(wielder, item);
            }

            if (Diag.IsChannelEnabled(DIAG_CATEGORY))
            {
                Diag.Record(
                    category: DIAG_CATEGORY,
                    kind: "Applied",
                    target: item,
                    payload: new
                    {
                        enhancement = enhancementName,
                        displayName = inst.GetDisplayName(),
                        tier = tier
                    });
            }
            return true;
        }

        /// <summary>
        /// Remove a named enhancement from an item. Returns true if
        /// the enhancement was found and removed. Emits
        /// <c>enhancement/Removed</c> on success.
        /// </summary>
        /// <summary>
        /// Remove a named enhancement from an item. Returns true if
        /// the enhancement was found and removed. Emits
        /// <c>enhancement/Removed</c> on success.
        ///
        /// <para><b>Deep-audit fix (Bug #2):</b> if <paramref name="wielder"/>
        /// is non-null AND the item is currently equipped on that wielder,
        /// the enhancement's
        /// <see cref="IItemEnhancement.OnUnequipped"/> hook fires BEFORE
        /// the Part is detached — so the gameplay-visible bonus
        /// (Lacquered AV, Engraved rep, GlowQuartz radius) is correctly
        /// reversed. Without this hook fire, the bonus would stay
        /// applied forever after the Part is gone (AppliedBonus flag
        /// permanent, but Part destroyed).</para>
        /// </summary>
        public static bool Remove(Entity item, string enhancementName,
            Entity wielder = null)
        {
            if (item == null || string.IsNullOrEmpty(enhancementName)) return false;
            if (!EnhancementFactory.TryGet(enhancementName, out var type)) return false;

            // Find the Part by type. RemovePart returns true if found.
            for (int i = item.Parts.Count - 1; i >= 0; i--)
            {
                if (item.Parts[i] is IItemEnhancement enh && enh.GetType() == type)
                {
                    // Deep-audit Bug #2 fix: if currently equipped on the
                    // wielder, fire OnUnequipped FIRST so AppliedBonus
                    // mutations roll back. Then enh.Remove + detach.
                    if (wielder != null && IsItemEquippedOn(item, wielder))
                    {
                        enh.OnUnequipped(wielder, item);
                    }

                    enh.Remove(item);
                    item.Parts.RemoveAt(i);
                    if (Diag.IsChannelEnabled(DIAG_CATEGORY))
                    {
                        Diag.Record(
                            category: DIAG_CATEGORY,
                            kind: "Removed",
                            target: item,
                            payload: new { enhancement = enhancementName });
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>Check whether <paramref name="item"/> is currently
        /// equipped on <paramref name="wielder"/>. Walks
        /// <c>InventoryPart.EquippedItems</c> — the canonical state the
        /// equip system maintains. Returns false if wielder has no
        /// InventoryPart or item is not in EquippedItems values.</summary>
        private static bool IsItemEquippedOn(Entity item, Entity wielder)
        {
            if (item == null || wielder == null) return false;
            var inv = wielder.GetPart<InventoryPart>();
            if (inv == null || inv.EquippedItems == null) return false;
            foreach (var kvp in inv.EquippedItems)
                if (kvp.Value == item) return true;
            return false;
        }

        /// <summary>Count of <see cref="IItemEnhancement"/> Parts on the
        /// item. Used by the slot-cap check.</summary>
        public static int CountEnhancements(Entity item)
        {
            if (item == null) return 0;
            int n = 0;
            for (int i = 0; i < item.Parts.Count; i++)
                if (item.Parts[i] is IItemEnhancement) n++;
            return n;
        }

        // ── Diag helpers ─────────────────────────────────────────

        private static void EmitApplyFailed(Entity item, string name, int tier, string reason)
        {
            if (!Diag.IsChannelEnabled(DIAG_CATEGORY)) return;
            Diag.Record(
                category: DIAG_CATEGORY,
                kind: "ApplyFailed",
                target: item,
                payload: new
                {
                    enhancement = name ?? "",
                    tier = tier,
                    reason = reason
                });
        }
    }
}
