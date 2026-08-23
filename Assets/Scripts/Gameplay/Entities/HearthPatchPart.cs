using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W5.4 — the hearth-patch: "warmth, food, politics and the god's
    /// dream in one object" (FELLING-WORLD-DESIGN.md:1112). Canon calls
    /// the rule out explicitly and it is NOT flavour — the patch is
    /// "the village's physical and political heart; threatening it is
    /// war" (:724).
    ///
    /// <para>The anthropology supplies the legal frame: a crime against
    /// the patch is "the most serious internal crime; punishment is
    /// exile or, very rarely, refusal-to-inscribe"
    /// (catacomb_village_design.md:143) — the same punishment the
    /// chiselled-smooth niche on the plaque-wall records.</para>
    ///
    /// <para>This listens on the prop-damage seam
    /// (<c>DestructionSystem.RouteDamage</c> fires TakeDamage on
    /// non-creature targets BEFORE the HP decrement), so burning the
    /// patch counts too — the fungus is combustible by inheritance, and
    /// a torch is a threat like any other.</para>
    /// </summary>
    public sealed class HearthPatchPart : Part
    {
        public override string Name => "HearthPatch";

        /// <summary>Set-to, not delta: canon says threatening the patch
        /// IS war, and war means the village actually fights you —
        /// which needs rep at or under HATED_THRESHOLD. The first cut
        /// of this rule was <c>RepLoss = 40</c>, and the verify pass
        /// traced the arithmetic: 0 − 40 = −40, which is not ≤
        /// DISLIKED(−50), so the one act canon calls war produced a
        /// "reputation worsens" line, no attitude change, and villagers
        /// who kept chatting. Text must be backed by mechanics.</summary>
        public int WarRep = PlayerReputation.HATED_THRESHOLD;

        private static readonly int TakeDamageID = GameEvent.GetID("TakeDamage");

        public override bool WantEvent(int eventID) => eventID == TakeDamageID;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "TakeDamage") return true;
            var source = e.GetParameter<Entity>("Source");
            // Only a person can commit a crime. A collapsing ceiling is
            // not an enemy of the village.
            if (source == null || !source.HasTag("Player")) return true;

            // The ledger IS the latch. The patch is ~51 separate
            // entities (one per ellipse cell), so a per-Part bool was
            // 51 independent latches — one Pyroclasm hit nine of them
            // in a single cast and billed −360. The reputation ledger
            // is village-scoped, shared by every tile, and round-trips
            // the save for free: once the village is at war, it stays
            // at war, and no further blow on any tile re-bills.
            int current = PlayerReputation.Get("CatacombFolk");
            if (current <= WarRep) return true;   // already at war

            PlayerReputation.Modify("CatacombFolk", WarRep - current);
            MessageLog.Add(
                "The light shudders. Somewhere behind you, every voice in " +
                "the chamber stops at once.");
            if (Diag.IsChannelEnabled("faction"))
                Diag.Record("faction", "HearthThreatened", source, ParentEntity,
                    new { warRep = WarRep, reason = "patch_damaged" });
            return true;
        }
    }
}
