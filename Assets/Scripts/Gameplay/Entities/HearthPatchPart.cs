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

        /// <summary>One step, not a graded tick: this is a war, and the
        /// village does not negotiate the first offence down.</summary>
        public int RepLoss = 40;

        private static readonly int TakeDamageID = GameEvent.GetID("TakeDamage");

        public override bool WantEvent(int eventID) => eventID == TakeDamageID;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "TakeDamage") return true;
            var source = e.GetParameter<Entity>("Source");
            // Only a person can commit a crime. A collapsing ceiling is
            // not an enemy of the village.
            if (source == null || !source.HasTag("Player")) return true;

            PlayerReputation.Modify("CatacombFolk", -RepLoss);
            MessageLog.Add(
                "The light shudders. Somewhere behind you, every voice in " +
                "the chamber stops at once.");
            if (Diag.IsChannelEnabled("faction"))
                Diag.Record("faction", "HearthThreatened", source, ParentEntity,
                    new { repLoss = RepLoss, reason = "patch_damaged" });
            return true;
        }
    }
}
