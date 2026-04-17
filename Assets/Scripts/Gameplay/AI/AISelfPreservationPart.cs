namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI behavior part that pushes RetreatGoal when the NPC's HP drops below
    /// RetreatThreshold AND the NPC has a StartingCell to retreat to.
    /// Mirrors Qud's AISelfPreservation — "I'd rather flee to safety than fight to the death."
    ///
    /// Attach to NPCs that should fall back when wounded:
    ///   { "Name": "AISelfPreservation", "Params": [{ "Key": "RetreatThreshold", "Value": "0.4" }] }
    ///
    /// The threshold is an HP fraction (0..1). Default 0.4 = retreat at 40% HP.
    ///
    /// Relationship to BrainPart.FleeThreshold (default 0.25):
    /// - <see cref="BrainPart.FleeThreshold"/> triggers a raw FleeGoal via
    ///   BoredGoal's ShouldFlee() check — "run AWAY from the nearest threat"
    ///   regardless of location. Used when combat is IMMINENT.
    /// - RetreatThreshold triggers RetreatGoal via AIBoredEvent — "walk to my
    ///   safe waypoint and recover HP." Used when combat has PAUSED (no
    ///   hostile in sight) and the NPC is still wounded.
    ///
    /// Typical pairing: RetreatThreshold (higher, e.g., 0.4–0.8) is the
    /// graceful fallback; FleeThreshold (lower, e.g., 0.25) is the panic
    /// tripwire. Authors should keep RetreatThreshold &gt; FleeThreshold so the
    /// flows compose naturally. If RetreatThreshold &lt; FleeThreshold, the NPC
    /// would enter panic-flee BEFORE ever crossing the retreat threshold,
    /// making retreat unreachable in practice (a dead-zone bug).
    ///
    /// Also note: AISelfPreservation only fires via AIBoredEvent, which only
    /// fires when BoredGoal runs and finds NO hostile in sight. Consequence:
    /// a wounded NPC in active combat will NOT mid-fight retreat — they'll
    /// fight on, then retreat after combat breaks off (hostile moves out of
    /// sight, dies, or goes into a different zone). This matches the
    /// "disengage and heal" design rather than "panic and run mid-swing."
    ///
    /// Evaluation order in BoredGoal:
    /// 1. BoredGoal fires AIBoredEvent → this part runs
    /// 2. If HP &lt; RetreatThreshold AND has StartingCell AND !HasGoal("RetreatGoal"):
    ///    - Push RetreatGoal(StartingCell)
    ///    - Event consumed → BoredGoal returns
    /// 3. Otherwise: event unhandled → BoredGoal continues (wander, furniture, etc.)
    ///
    /// The `!HasGoal("RetreatGoal")` gate prevents re-pushing every tick while
    /// a retreat is already in progress. Exercises Phase 5's HasGoal(string) API.
    /// </summary>
    public class AISelfPreservationPart : AIBehaviorPart
    {
        public override string Name => "AISelfPreservation";

        /// <summary>Minimum gap between RetreatThreshold and SafeThreshold. Prevents push-pop thrashing.</summary>
        private const float MinThresholdGap = 0.1f;

        /// <summary>HP fraction (0..1) at or below which the NPC retreats. Default 40%.</summary>
        public float RetreatThreshold = 0.4f;

        /// <summary>HP fraction the NPC must climb back to before ending retreat. Default 75%.</summary>
        public float SafeThreshold = 0.75f;

        /// <summary>
        /// Returns a clamped SafeThreshold that is strictly greater than RetreatThreshold
        /// by at least <see cref="MinThresholdGap"/>. Without this guard, a blueprint author
        /// who sets RetreatThreshold >= SafeThreshold would cause RetreatGoal to finish
        /// immediately on entry (HP already >= safe) and AISelfPreservation to push it
        /// again next tick, thrashing forever.
        /// </summary>
        public float EffectiveSafeThreshold => System.Math.Max(SafeThreshold, RetreatThreshold + MinThresholdGap);

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == AIBoredEvent.ID)
            {
                bool result = HandleBored();
                if (!result) e.Handled = true;
                return result;
            }
            return true;
        }

        private bool HandleBored()
        {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain == null) return true;
            if (!brain.HasStartingCell) return true; // no safe waypoint
            if (brain.HasGoal("RetreatGoal")) return true; // already retreating

            // Check HP fraction
            int hp = ParentEntity.GetStatValue("Hitpoints", 0);
            int maxHp = ParentEntity.GetStat("Hitpoints")?.Max ?? 0;
            if (hp <= 0 || maxHp <= 0) return true;

            float fraction = (float)hp / maxHp;
            if (fraction > RetreatThreshold) return true; // HP fine, let default behavior proceed

            brain.PushGoal(new RetreatGoal(
                brain.StartingCellX,
                brain.StartingCellY,
                EffectiveSafeThreshold));
            return false; // consumed
        }
    }
}
