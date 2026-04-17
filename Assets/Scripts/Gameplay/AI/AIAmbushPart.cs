namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI behavior part that pushes <see cref="DormantGoal"/> onto the brain's
    /// goal stack on the first turn the creature takes. The creature then
    /// sleeps until a wake trigger (damage taken, hostile entering sight, or
    /// an explicit <see cref="DormantGoal.Wake"/> call) fires.
    ///
    /// Mirrors the "ambush creature" archetype from roguelikes: ambushing
    /// bandits in tall grass, sleeping trolls in dungeons, mimics disguised
    /// as chests, undead resting on slabs, etc.
    ///
    /// Attach to blueprints via:
    ///   { "Name": "AIAmbush", "Params": [
    ///       { "Key": "WakeOnDamage", "Value": "true" },
    ///       { "Key": "WakeOnHostileInSight", "Value": "true" },
    ///       { "Key": "SleepParticleInterval", "Value": "8" }
    ///   ]}
    ///
    /// Pattern rationale: the push happens on the first <c>TakeTurn</c> event
    /// rather than in <c>Initialize()</c>. This makes the part robust to
    /// blueprint part-declaration order — by the time the first TakeTurn
    /// fires, all parts (including <see cref="BrainPart"/>) are guaranteed
    /// to exist on the entity, and the zone context is fully wired.
    ///
    /// Uses a <c>_dormantPushed</c> flag so the goal is pushed exactly once
    /// even if TakeTurn fires repeatedly. After the creature wakes, a
    /// subsequent AIAmbushPart instance (e.g., from a fresh spawn or
    /// re-initialization) would need to reset this flag to re-arm ambush
    /// mode.
    /// </summary>
    public class AIAmbushPart : AIBehaviorPart
    {
        public override string Name => "AIAmbush";

        /// <summary>Whether taking damage wakes the dormant creature.</summary>
        public bool WakeOnDamage = true;

        /// <summary>Whether a hostile entering sight radius wakes the dormant creature.</summary>
        public bool WakeOnHostileInSight = true;

        /// <summary>Ticks between 'z' sleep particles. 0 disables the visual.</summary>
        public int SleepParticleInterval = 8;

        /// <summary>Set to true once DormantGoal has been pushed. Prevents re-push on subsequent turns.</summary>
        private bool _dormantPushed;

        public override bool HandleEvent(GameEvent e)
        {
            if (!_dormantPushed && e.ID == "TakeTurn")
            {
                var brain = ParentEntity?.GetPart<BrainPart>();
                if (brain != null)
                {
                    brain.PushGoal(new DormantGoal(
                        wakeOnDamage: WakeOnDamage,
                        wakeOnHostileInSight: WakeOnHostileInSight,
                        sleepParticleInterval: SleepParticleInterval));
                    _dormantPushed = true;
                }
            }
            return true;
        }
    }
}
