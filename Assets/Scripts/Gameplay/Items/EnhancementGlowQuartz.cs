using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.3.3 — <b>Glow-Quartz</b>. The first
    /// mineral-derived enhancement. Adds (or extends) a
    /// <see cref="LightSourcePart"/> on the parent item so the wielder
    /// gets a tier-scaled light radius bonus while it's equipped.
    /// Source mineral: <c>GlowQuartz</c> blueprint (E.3.2). Applied via
    /// Tinker recipe (E.3.4).
    ///
    /// <para><b>Filter:</b> any item with an
    /// <see cref="EquippablePart"/>. Both weapons (lantern-tipped
    /// quartz-rods) and armor (quartz-tinged plate) make sense per
    /// IDEAS.md.</para>
    ///
    /// <para><b>Mechanic (LightMap Pass-2 integration):</b>
    /// <c>LightMap.cs:113-118</c> iterates the wielder's equipped items
    /// and reads each item's <c>LightSourcePart</c> directly, projecting
    /// light from the WIELDER's cell. So GlowQuartz mutates the ITEM's
    /// LightSourcePart, NOT the wielder's. <c>EquipmentChangeBus</c>
    /// handles cache invalidation automatically (LightMap.cs:71-75).</para>
    ///
    /// <para><b>Tier scaling:</b> <c>RADIUS_PER_TIER * Tier</c> additional
    /// tiles of light radius. Tier 1 → +1, Tier 4 → +4. Modest values
    /// — a single FlamingSword blueprint already gives Radius 4, so
    /// stacking Tier-4 GlowQuartz on top doubles it; sane upper bound.</para>
    ///
    /// <para><b>Atomicity (F.3.4 lesson — see EnhancementLacquered.cs):</b>
    /// the <see cref="AppliedBonus"/> flag is set EAGERLY before
    /// mutating <c>LightSourcePart.Radius</c>. Double-equip is a no-op;
    /// unequip-without-equip is a no-op.</para>
    ///
    /// <para><b>LightSourcePart auto-creation:</b> If the item didn't
    /// already have a LightSourcePart (e.g. a plain LongSword, not a
    /// FlamingSword), OnEquipped adds one. On unequip the radius is
    /// subtracted back; we leave the Part attached at Radius=0 (a
    /// zero-radius LightSourcePart is a no-op in LightMap). Tracking
    /// "did we create the Part?" complexity buys nothing observable.</para>
    ///
    /// <para><b>Save/load (SL.6):</b> <c>Tier</c>, <c>RadiusBonus</c>,
    /// <c>AppliedBonus</c> are all public simple-typed fields and
    /// round-trip via reflection.</para>
    ///
    /// <para><b>Qud parity:</b> CoO-original mechanic. Qud has glow-
    /// rocks (<c>RhinoxHorn</c>, <c>GlowfishOil</c>, etc.) as held-light
    /// items, but no enhancement-mod that grants light radius. Faithful
    /// to IDEAS.md's "quartz-tipped lantern-rods extend bio-light range
    /// substantially."</para>
    /// </summary>
    public class EnhancementGlowQuartz : IItemEnhancement
    {
        /// <summary>Radius bonus per tier. Tier 1 → +1 tile, Tier 4 → +4 tiles.</summary>
        public const int RADIUS_PER_TIER = 1;

        // --- Round-tripped state -----------------------------------

        /// <summary>Computed in <see cref="TierConfigure"/>. The exact
        /// delta applied to the item's LightSourcePart.Radius on equip
        /// and subtracted on unequip. Round-trips so a save's stored
        /// value is what we'll subtract — safe even if tuning shifts.</summary>
        public int RadiusBonus;

        /// <summary>Atomicity flag — set EAGERLY in <see cref="OnEquipped"/>
        /// before mutation; symmetric guard in
        /// <see cref="OnUnequipped"/>. Round-trips via reflection so an
        /// equipped GlowQuartz loaded from save still knows to
        /// subtract on unequip.</summary>
        public bool AppliedBonus;

        public override string Name => nameof(EnhancementGlowQuartz);

        public override string GetDisplayName() => "Glow-Quartz-tipped";

        // --- Lifecycle ---------------------------------------------

        public override void TierConfigure()
        {
            RadiusBonus = Tier * RADIUS_PER_TIER;
        }

        public override bool Applicable(Entity item)
        {
            if (!base.Applicable(item)) return false;
            // Glow-Quartz needs something equippable to attach to.
            // Per IDEAS.md, both weapons and armor make sense
            // ("quartz-tipped lantern-rods" + "quartz-tinged armor").
            return item.GetPart<EquippablePart>() != null;
        }

        // --- Equip hooks -------------------------------------------

        public override void OnEquipped(Entity actor, Entity item)
        {
            if (AppliedBonus) return;          // double-equip idempotent
            if (item == null) return;

            // Get-or-create the item's LightSourcePart. LightMap.cs:113-118
            // reads it directly from the equipped item, projecting from
            // the WIELDER's cell — so this Part lives on the item, not
            // the actor.
            var light = item.GetPart<LightSourcePart>();
            if (light == null)
            {
                light = new LightSourcePart { Radius = 0 };
                item.AddPart(light);
            }

            // Eager-flag pattern: set AppliedBonus FIRST so any
            // subsequent failure mode (light is non-null but immutable,
            // future Part-observer throws) can't double-apply on retry.
            AppliedBonus = true;
            light.Radius += RadiusBonus;

            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY))
            {
                Diag.Record(
                    category: ItemEnhancing.DIAG_CATEGORY,
                    kind: "BonusApplied",
                    actor: actor,
                    target: item,
                    payload: new
                    {
                        enhancement = nameof(EnhancementGlowQuartz),
                        tier = Tier,
                        radiusBonus = RadiusBonus,
                        radiusAfter = light.Radius
                    });
            }
        }

        public override void OnUnequipped(Entity actor, Entity item)
        {
            if (!AppliedBonus) return;         // symmetric guard
            if (item == null) return;
            var light = item.GetPart<LightSourcePart>();
            if (light == null) return;         // Part vanished — defensive

            light.Radius -= RadiusBonus;
            AppliedBonus = false;
            // Leave the LightSourcePart attached at whatever radius
            // remains. A zero-radius LightSourcePart is a no-op for
            // LightMap (the inner loops degenerate). Tracking "did we
            // create it" buys nothing observable.

            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY))
            {
                Diag.Record(
                    category: ItemEnhancing.DIAG_CATEGORY,
                    kind: "BonusRemoved",
                    actor: actor,
                    target: item,
                    payload: new
                    {
                        enhancement = nameof(EnhancementGlowQuartz),
                        tier = Tier,
                        radiusBonus = RadiusBonus,
                        radiusAfter = light.Radius
                    });
            }
        }
    }
}
