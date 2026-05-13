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
        public static bool Apply(Entity item, string enhancementName, int tier = 1)
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
        public static bool Remove(Entity item, string enhancementName)
        {
            if (item == null || string.IsNullOrEmpty(enhancementName)) return false;
            if (!EnhancementFactory.TryGet(enhancementName, out var type)) return false;

            // Find the Part by type. RemovePart returns true if found.
            for (int i = item.Parts.Count - 1; i >= 0; i--)
            {
                if (item.Parts[i] is IItemEnhancement enh && enh.GetType() == type)
                {
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
