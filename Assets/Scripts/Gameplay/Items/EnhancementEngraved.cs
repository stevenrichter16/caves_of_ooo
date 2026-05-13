using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.2.4 — <b>Engraved</b>. The first concrete
    /// utility enhancement: while equipped by the player, grants
    /// faction reputation. On unequip, withdraws the same rep.
    ///
    /// <para><b>Filter:</b> requires the item to be equippable
    /// (<see cref="EquippablePart"/>). Both melee weapons (which carry
    /// an EquippablePart) and armor satisfy this — Engraved is
    /// agnostic to weapon-vs-armor since the engraving's meaning is
    /// the faction symbol, not what it's etched on. Rejects items
    /// without EquippablePart (tonics, throwables, etc.).</para>
    ///
    /// <para><b>Effect:</b> on <see cref="OnEquipped"/> (player only)
    /// calls <see cref="PlayerReputation.Modify"/> for
    /// <see cref="Faction"/> with <see cref="RepDelta"/> as the amount.
    /// On <see cref="OnUnequipped"/> applies the negative delta to
    /// undo it. Net change is zero across a full cycle.</para>
    ///
    /// <para><b>Atomicity (F.3.4 GrantsRepAsFollowerPart lesson):</b>
    /// the <see cref="AppliedBonus"/> flag is set EAGERLY before the
    /// rep mutation in <see cref="OnEquipped"/> — so a re-entered
    /// dispatch can't double-apply. Symmetric guard in
    /// <see cref="OnUnequipped"/> prevents subtract-without-prior-equip
    /// (which would corrupt rep on a stale-loaded equipped state).</para>
    ///
    /// <para><b>Player-only gate:</b> Engraved fires the rep delta only
    /// when <paramref name="actor"/> carries the <c>Player</c> tag.
    /// NPC equipping doesn't move the player's rep with the faction —
    /// because the player's rep is a player-scoped concept, the
    /// engravings on enemy weapons shouldn't move it. Mirrors F.3.4's
    /// <c>CheckApplyBonus</c> player-only conditional.</para>
    ///
    /// <para><b>Tier scaling:</b></para>
    /// <list type="table">
    ///   <listheader><term>Tier</term><description>Rep Delta</description></listheader>
    ///   <item><term>1</term><description>+5</description></item>
    ///   <item><term>2</term><description>+10</description></item>
    ///   <item><term>3</term><description>+15</description></item>
    ///   <item><term>4</term><description>+20</description></item>
    /// </list>
    ///
    /// <para><b>Qud parity:</b> Qud has no direct "engraved with faction X"
    /// modifier — the closest analog is item-tagged faction sigils
    /// (<c>religious item</c> / <c>FactionItem</c> properties) which
    /// influence faction-encounter outcomes rather than directly
    /// modifying rep. CoO-original mechanic. Documented in
    /// <c>Docs/ITEM-ENHANCEMENTS.md</c>.</para>
    /// </summary>
    public class EnhancementEngraved : IItemEnhancement
    {
        /// <summary>Rep delta per tier. Tier 1 → +5, Tier 4 → +20.</summary>
        public const int REP_DELTA_PER_TIER = 5;

        // --- Round-tripped state (public for SL.6 reflection) -----

        /// <summary>Faction whose rep is modified on equip. Content
        /// sets this when applying the enhancement (via the
        /// <c>EnhancementFactory</c> tier-aware overload or a direct
        /// factory caller). Empty string = no-op.</summary>
        public string Faction = "";

        /// <summary>Rep delta computed by <see cref="TierConfigure"/>.
        /// Round-trips independently so a save's stored RepDelta is
        /// the value we'll subtract on unequip even if tuning shifts.</summary>
        public int RepDelta;

        /// <summary>Atomicity flag (F.3.4 lesson). See class doc.
        /// Round-trips via reflection so an equipped Engraved item
        /// loaded from save still knows to subtract on unequip.</summary>
        public bool AppliedBonus;

        public override string Name => nameof(EnhancementEngraved);

        public override string GetDisplayName() =>
            string.IsNullOrEmpty(Faction)
                ? "Engraved"
                : $"Engraved with the symbol of {Faction}";

        public override string GetEffectDescription()
        {
            // Player-facing — RepDelta is set by TierConfigure (Tier * 5).
            // If Faction is unset, the effect is a no-op; surface honestly.
            if (string.IsNullOrEmpty(Faction))
                return "Engraved: (no faction set — no rep effect)";
            return $"Engraved: +{RepDelta} reputation with {Faction} while equipped";
        }

        // --- Lifecycle ---------------------------------------------

        public override void TierConfigure()
        {
            RepDelta = Tier * REP_DELTA_PER_TIER;
        }

        public override bool Applicable(Entity item)
        {
            if (!base.Applicable(item)) return false;
            // Engraving is meaningful only on something you wear/wield.
            return item.GetPart<EquippablePart>() != null;
        }

        // --- Equip hooks -------------------------------------------

        public override void OnEquipped(Entity actor, Entity item)
        {
            // Atomicity guard FIRST. Double-equip is a no-op.
            if (AppliedBonus) return;
            // Player-only gate. NPCs equipping must not move player rep
            // (the engraving's symbolic meaning is player-scoped).
            if (actor == null || !actor.Tags.ContainsKey("Player")) return;
            // No faction set → no-op (Engraved with what?).
            if (string.IsNullOrEmpty(Faction)) return;

            // Set flag eagerly BEFORE the rep mutation. If a future
            // PlayerReputation.Modify path throws (e.g. on an unknown
            // faction), the flag is already true so a retry won't
            // double-apply on success.
            AppliedBonus = true;
            PlayerReputation.Modify(Faction, RepDelta, silent: false);

            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY))
            {
                Diag.Record(
                    category: ItemEnhancing.DIAG_CATEGORY,
                    kind: "BonusApplied",
                    actor: actor,
                    target: item,
                    payload: new
                    {
                        enhancement = nameof(EnhancementEngraved),
                        tier = Tier,
                        faction = Faction,
                        repDelta = RepDelta
                    });
            }
        }

        public override void OnUnequipped(Entity actor, Entity item)
        {
            // Symmetric guard: only withdraw if we actually applied.
            if (!AppliedBonus) return;
            // Mirror the player-only gate so an NPC unequipping a
            // stale-loaded Engraved item can't drain player rep.
            if (actor == null || !actor.Tags.ContainsKey("Player")) return;
            if (string.IsNullOrEmpty(Faction)) return;

            PlayerReputation.Modify(Faction, -RepDelta, silent: false);
            AppliedBonus = false;

            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY))
            {
                Diag.Record(
                    category: ItemEnhancing.DIAG_CATEGORY,
                    kind: "BonusRemoved",
                    actor: actor,
                    target: item,
                    payload: new
                    {
                        enhancement = nameof(EnhancementEngraved),
                        tier = Tier,
                        faction = Faction,
                        repDelta = RepDelta
                    });
            }
        }
    }
}
