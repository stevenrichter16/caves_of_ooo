using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.3.4 — base class for the three mineral-
    /// infusion tinker modifications (Pale-Salt, Choir-Iron, Glow-
    /// Quartz). Each concrete subclass declares the enhancement type
    /// name + tier; the base wires the rest through
    /// <see cref="ItemEnhancing.Apply"/>.
    ///
    /// <para><b>E.5.1 deep-audit Bug #1 fix:</b> the shim now sets a
    /// thread-static <see cref="CurrentCrafter"/> just before invoking
    /// <see cref="Apply"/>, and reads it back to pass as the
    /// <c>wielder</c> argument to <see cref="ItemEnhancing.Apply"/>.
    /// This lets a Tinker-applied enhancement on an already-equipped
    /// item fire OnEquipped immediately, so the gameplay-visible
    /// bonus (Lacquered AV, GlowQuartz radius, etc.) lands without
    /// requiring a re-equip cycle. <c>TinkeringService.TryApplyModification</c>
    /// sets the context; ITinkerModification.Apply reads it. The
    /// thread-static is reset to null on exit so the channel doesn't
    /// leak across calls.</para>
    ///
    /// <para><b>Two parallel modification systems — bridging the gap:</b>
    /// CoO has both <see cref="ITinkerModification"/> (older,
    /// directly tied to the Tinker recipe + bit-cost flow) and the
    /// E.1 <see cref="IItemEnhancement"/> system (newer, factory +
    /// dispatch + content). This shim lets the existing TinkerService
    /// + recipe UI surface invoke the new enhancement system without
    /// either side being rewritten. <c>EnhancementApply</c> is the
    /// integration point; the shim is the adapter.</para>
    ///
    /// <para><b>Tier-on-recipe (where does tier come from?):</b>
    /// Hardcoded per concrete shim, matching the mineral blueprint's
    /// declared Tier in Objects.json (PaleSalt=2, ChoirIron=3,
    /// GlowQuartz=2). If a future blueprint changes the Tier tag,
    /// the shim's constant must be updated in lockstep — drift risk
    /// is small (the two values sit ~30 lines apart in adjacent
    /// files). E.5+ polish could read Tier from the consumed
    /// ingredient's Tags before consumption.</para>
    ///
    /// <para><b>Slot-cap interaction:</b> If the item already has 2
    /// enhancements (Lockdown #6 from E.1.4), <see cref="Apply"/>
    /// returns false with the "Item enhancement slot cap" reason —
    /// the recipe machinery then refunds bits + restores ingredient.</para>
    /// </summary>
    public abstract class MineralInfusionTinkerModification : ITinkerModification
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }

        /// <summary>The <see cref="IItemEnhancement"/> class name to
        /// instantiate (e.g. <c>"EnhancementPaleSalt"</c>).</summary>
        protected abstract string EnhancementName { get; }

        /// <summary>The tier at which the enhancement is applied.
        /// Matches the mineral blueprint's Tier tag in Objects.json.</summary>
        protected abstract int Tier { get; }

        /// <summary>Adjective for the inventory display-name decoration
        /// (e.g. "pale-salt-edged", "glow-quartz-tipped"). Mirrors the
        /// existing SharpTinkerModification "sharp X" convention.</summary>
        protected abstract string DisplayAdjective { get; }

        /// <summary>E.5.1 deep-audit Bug #1 plumbing: TinkeringService
        /// sets this just before invoking <see cref="Apply"/> so the
        /// shim can pass the crafter as the <c>wielder</c> argument to
        /// <see cref="ItemEnhancing.Apply"/>. Reset to null on
        /// TryApplyModification exit. Thread-static so a future
        /// multi-threaded crafter pipeline doesn't cross-contaminate.</summary>
        [System.ThreadStatic]
        public static Entity CurrentCrafter;

        public bool CanApply(Entity item, out string reason)
        {
            reason = string.Empty;
            if (item == null)
            {
                reason = "Target item is missing.";
                return false;
            }

            // Use the Enhancement Part's Applicable filter as the source
            // of truth for "does this mineral fit on this item?" —
            // single contract, no drift between gates.
            if (!EnhancementFactory.TryGet(EnhancementName, out _))
            {
                reason = "Enhancement type '" + EnhancementName + "' is not registered.";
                return false;
            }
            var inst = EnhancementFactory.Create(EnhancementName, Tier);
            if (inst == null)
            {
                reason = "Failed to instantiate enhancement '" + EnhancementName + "'.";
                return false;
            }
            if (!inst.Applicable(item))
            {
                reason = "Target item is not compatible with this mineral.";
                return false;
            }

            // Slot-cap check (Lockdown #6).
            if (ItemEnhancing.CountEnhancements(item) >= ItemEnhancing.MAX_ENHANCEMENTS_PER_ITEM)
            {
                reason = "Item already has the maximum number of enhancements.";
                return false;
            }

            // Stack-split check: mirror SharpTinkerModification's
            // contract (can't tinker on a stacked item; the player
            // must split first).
            var stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                reason = "Split the stack before applying a modification.";
                return false;
            }

            return true;
        }

        public bool Apply(Entity item, out string reason)
        {
            if (!CanApply(item, out reason))
                return false;

            // E.5.1 deep-audit Bug #1: pass CurrentCrafter as the wielder
            // so that if the crafter is currently wielding this item
            // (e.g. tinker-on-equipped-armor), the OnEquipped hook fires
            // immediately and the bonus lands without a re-equip cycle.
            // CurrentCrafter is null in non-Tinker callers — ItemEnhancing.Apply
            // gracefully no-ops the wielder branch in that case.
            if (!ItemEnhancing.Apply(item, EnhancementName, Tier, wielder: CurrentCrafter))
            {
                // Apply emitted enhancement/ApplyFailed already with a
                // reason. Surface a generic recipe-level message here.
                reason = "Failed to apply " + DisplayName + " to target item.";
                return false;
            }

            // Visible name adornment matches the SharpTinkerModification
            // convention so the player sees what their weapon became.
            var render = item.GetPart<RenderPart>();
            if (render != null && !string.IsNullOrWhiteSpace(render.DisplayName))
            {
                string current = render.DisplayName.Trim();
                if (!current.StartsWith(DisplayAdjective + " ", StringComparison.OrdinalIgnoreCase))
                    render.DisplayName = DisplayAdjective + " " + current;
            }

            item.ModIntProperty("ModificationCount", 1);
            return true;
        }
    }

    public sealed class PaleSaltTinkerModification : MineralInfusionTinkerModification
    {
        public override string Id => "mod_palesalt";
        public override string DisplayName => "Pale-Salt Infusion";
        protected override string EnhancementName => nameof(EnhancementPaleSalt);
        protected override int Tier => 2;
        protected override string DisplayAdjective => "pale-salt-edged";
    }

    public sealed class ChoirIronTinkerModification : MineralInfusionTinkerModification
    {
        public override string Id => "mod_choiriron";
        public override string DisplayName => "Choir-Iron Infusion";
        protected override string EnhancementName => nameof(EnhancementChoirIron);
        protected override int Tier => 3;
        protected override string DisplayAdjective => "choir-iron-edged";
    }

    public sealed class GlowQuartzTinkerModification : MineralInfusionTinkerModification
    {
        public override string Id => "mod_glowquartz";
        public override string DisplayName => "Glow-Quartz Infusion";
        protected override string EnhancementName => nameof(EnhancementGlowQuartz);
        protected override int Tier => 2;
        protected override string DisplayAdjective => "glow-quartz-tipped";
    }
}
