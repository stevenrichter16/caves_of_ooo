namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI behavior part that pushes <see cref="DormantGoal"/> onto the brain's
    /// goal stack as soon as the part is attached. The creature then sleeps
    /// until a wake trigger (damage taken, hostile entering sight, or an
    /// explicit <see cref="DormantGoal.Wake"/> call) fires.
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
    /// Push timing rationale:
    /// The DormantGoal is pushed in <see cref="Initialize"/> rather than on the
    /// first TakeTurn event. This ensures the goal is on top of the stack
    /// BEFORE <see cref="BrainPart.HandleEvent"/> runs on the first TakeTurn,
    /// which would otherwise push a BoredGoal onto the empty stack and
    /// execute one tick of wandering / hostile scanning before the ambush
    /// takes effect.
    ///
    /// Part-declaration order matters: <see cref="BrainPart"/> must be attached
    /// BEFORE AIAmbushPart for the Initialize push to find it. In practice the
    /// Creature base blueprint declares Brain at index 5 and child blueprints
    /// append AIAmbush later, so this ordering is guaranteed for blueprint-loaded
    /// entities. For test/programmatic setup, add parts in the same order.
    ///
    /// Fallback safeguard: <see cref="HandleEvent"/> also attempts to push on
    /// TakeTurn if Initialize couldn't (e.g., BrainPart was attached AFTER this
    /// part). The <c>DormantPushed</c> flag ensures exactly-once push regardless
    /// of which path succeeds.
    ///
    /// The flag is never auto-cleared: once woken, AIAmbush does not re-arm on
    /// subsequent ticks. Call <see cref="Rearm"/> explicitly to re-enter ambush
    /// mode (used by "sleep" status effects, re-spawn logic, etc.).
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
        public bool DormantPushed;

        /// <summary>Presence marker for the additive lifecycle fields. Old saves
        /// omitted the private latch; restored brains conservatively count as
        /// already armed, never as a request to sleep again.</summary>
        public int AmbushSaveStateVersion;

        public override void OnBeforeSave(SaveWriter writer)
        {
            AmbushSaveStateVersion = 1;
        }

        public override void OnAfterLoad(SaveReader reader)
        {
            if (AmbushSaveStateVersion != 0) return;
            DormantPushed = ParentEntity?.GetPart<BrainPart>() != null;
            AmbushSaveStateVersion = 1;
        }

        public override void Initialize()
        {
            TryPushDormant();
        }

        public override bool HandleEvent(GameEvent e)
        {
            // Fallback: if Initialize couldn't push (e.g., BrainPart was attached
            // after this part, or part fields were modified post-construction),
            // retry on the first TakeTurn event.
            if (!DormantPushed && e.ID == "TakeTurn")
            {
                TryPushDormant();
            }
            return true;
        }

        /// <summary>
        /// Re-arm ambush mode so the next Initialize or TakeTurn re-pushes DormantGoal.
        /// Intended for sleep-status effects or programmatic re-dormancy. Callers are
        /// responsible for ensuring no DormantGoal is currently on the stack (the
        /// `DormantPushed` flag tracks state, not actual stack contents).
        /// </summary>
        public void Rearm()
        {
            DormantPushed = false;
        }

        private void TryPushDormant()
        {
            if (DormantPushed) return;
            var brain = ParentEntity?.GetPart<BrainPart>();
            if (brain == null) return;

            brain.PushGoal(new DormantGoal(
                wakeOnDamage: WakeOnDamage,
                wakeOnHostileInSight: WakeOnHostileInSight,
                sleepParticleInterval: SleepParticleInterval));
            DormantPushed = true;
        }
    }
}
