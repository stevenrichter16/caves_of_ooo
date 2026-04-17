namespace CavesOfOoo.Core
{
    /// <summary>
    /// Pacifist override goal. While this goal is on the stack, the NPC will not
    /// initiate combat — even if hostiles are in sight and even if HP is low.
    /// Mirrors Qud's NoFight goal handler.
    ///
    /// Contrast with BrainPart.Passive (a creature-level flag): NoFightGoal is a
    /// temporary, stack-based override. Use it for:
    /// - Truce / ceasefire quest states
    /// - "Charmed" or "Peaceful" status effects
    /// - NPCs acting as non-combatants during dialogue scenes
    /// - Conversation participants that shouldn't suddenly attack mid-sentence
    ///
    /// The goal sits at the top of the stack for Duration ticks (or indefinitely
    /// if Duration &lt;= 0). While active:
    /// - BoredGoal never runs (this goal is on top), so no KillGoal is pushed
    /// - CanFight() returns false, so combat-interrupt logic skips this NPC
    /// - The NPC optionally wanders randomly if Wander=true, otherwise idles in place
    ///
    /// Pop via Duration expiration or external RemoveGoal().
    ///
    /// ⚠️ Side-effect: while NoFightGoal is on top of the stack, <see cref="AIBoredEvent"/>
    /// does not fire, which means <b>all</b> <see cref="AIBehaviorPart"/> subclasses stop
    /// responding — including <see cref="AISelfPreservationPart"/>. A pacified creature
    /// at critical HP will <b>not</b> be retreated by self-preservation until the
    /// NoFightGoal expires or is removed. If you need "pacified EXCEPT for emergency
    /// retreat," call <see cref="BrainPart.RemoveGoal"/> on the NoFightGoal from a
    /// higher-priority listener (e.g., a TakeDamage handler) rather than relying on
    /// AIBored-driven behavior parts.
    /// </summary>
    public class NoFightGoal : GoalHandler
    {
        /// <summary>Turns to remain pacifist. &lt;= 0 means infinite (requires external removal).</summary>
        public int Duration;

        /// <summary>When true, wanders randomly while pacified. When false, idles in place.</summary>
        public bool Wander;

        public NoFightGoal(int duration = 0, bool wander = false)
        {
            Duration = duration;
            Wander = wander;
        }

        public override bool CanFight() => false;
        public override bool IsBusy() => false;

        public override bool Finished()
        {
            return Duration > 0 && Age >= Duration;
        }

        public override void TakeAction()
        {
            if (ParentBrain != null)
                ParentBrain.CurrentState = AIState.Idle;

            if (Wander)
                PushChildGoal(new WanderRandomlyGoal());
            // else: do nothing — the goal sits on top and blocks combat acquisition.
        }
    }
}
