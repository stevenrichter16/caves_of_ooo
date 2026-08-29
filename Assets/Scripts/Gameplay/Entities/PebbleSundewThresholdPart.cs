using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W5.4 — the Pebble-Sundew threshold: a tended sundew field at a
    /// catacomb village's entrance. Canon makes it the village's FIRST
    /// SENSE-ORGAN: "the faint sticky welcome of the Drosera underfoot
    /// IS part of the formal greeting — the village is recognizing the
    /// visitor by feel of their step" (catacomb_village_design.md:117).
    /// Their word for it is <c>dewstep</c>: welcome, greeting (:261).
    ///
    /// <para>Surface-dwellers find it disturbing the first time;
    /// villagers find a handshake equally so. Mechanically it is
    /// announcement, not a snare — the sibling
    /// <see cref="GreatdewSnarePart"/> on the same seam grabs; this one
    /// only says you are here.</para>
    ///
    /// <para><c>TriggerFaction</c> is set to CatacombFolk in the
    /// blueprint so the village does not trip its own doormat all
    /// day.</para>
    /// </summary>
    public sealed class PebbleSundewThresholdPart : TriggerOnStepPart
    {
        public override string Name => "PebbleSundewThreshold";

        public PebbleSundewThresholdPart()
        {
            // The threshold is permanent — it greets everyone who comes.
            ConsumeOnTrigger = false;
        }

        /// <summary>Entity property that latches the greeting. The
        /// mats are a 5×7 field of ~35 separate trigger entities, so
        /// without a latch one walk across the band printed the same
        /// second-person line up to five times (close-out hypothesis
        /// H4) — and any wandering NPC crossing the mats narrated
        /// "the weight of YOU" at the player from across the zone.
        /// The fiction resolves it: the village recognizes a step
        /// ONCE — "knows the weight of you now" is permanent — so the
        /// latch is per-actor, forever, and rides the save with the
        /// entity's other properties. The diag record still fires on
        /// every crossing; the village always feels the step, it just
        /// does not re-announce an old acquaintance.</summary>
        public const string GreetedProperty = "DewstepKnown";

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            if (actor == null) return;
            if (actor.HasTag("Player")
                && !actor.Properties.ContainsKey(GreetedProperty))
            {
                actor.Properties[GreetedProperty] = "";
                MessageLog.Add(
                    "The sundew gives underfoot, sticky and warm — a dewstep. " +
                    "Somewhere ahead, the village knows the weight of you now.");
            }
            if (Diag.IsChannelEnabled("faction"))
                Diag.Record("faction", "Dewstep", actor, ParentEntity,
                    new { blueprintName = actor.BlueprintName });
        }
    }
}
