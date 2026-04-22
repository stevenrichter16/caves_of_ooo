namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that keeps an NPC dormant (doing nothing) until woken by a trigger.
    /// Mirrors Qud's Dormant goal handler.
    ///
    /// Use cases:
    /// - Ambush creatures in dungeons (trolls, mimics) that only activate on intruder proximity
    /// - Sleeping NPCs (paired with BedPart / dawn cycle)
    /// - Inactive constructs / golems activated by a quest event
    /// - Creatures under a sleep/hibernation status effect
    ///
    /// Wake triggers (configurable):
    /// - `WakeOnDamage` (default true) — detects HP decrease between ticks
    /// - `WakeOnHostileInSight` (default false) — scans for hostiles every tick
    /// - External: call <see cref="Wake"/> or <see cref="GoalHandler.Pop"/> directly
    ///
    /// While dormant:
    /// - CanFight() returns false (combat won't interrupt)
    /// - Emits a 'z' particle occasionally for visual feedback
    /// - Takes no action
    /// </summary>
    public class DormantGoal : GoalHandler
    {
        public bool WakeOnDamage;
        public bool WakeOnHostileInSight;

        /// <summary>Ticks between 'z' particle emissions. 0 disables the visual.</summary>
        public int SleepParticleInterval;

        private int _lastHp = -1;
        private bool _wakeRequested;

        public DormantGoal(bool wakeOnDamage = true, bool wakeOnHostileInSight = false, int sleepParticleInterval = 8)
        {
            WakeOnDamage = wakeOnDamage;
            WakeOnHostileInSight = wakeOnHostileInSight;
            SleepParticleInterval = sleepParticleInterval;
        }

        public override bool CanFight() => false;
        public override bool IsBusy() => false;

        public override bool Finished() => _wakeRequested;

        /// <summary>Explicitly wake this dormant goal. It will pop on next stack clean.</summary>
        public void Wake()
        {
            _wakeRequested = true;
        }

        public override void TakeAction()
        {
            if (ParentEntity == null || CurrentZone == null) { Pop(); return; }

            if (ParentBrain != null)
                ParentBrain.CurrentState = AIState.Idle;

            // Damage-wake check: HP dropped since last tick.
            // Baseline is seeded on the first tick rather than in OnPush so the
            // goal is robust to being constructed before the parent Brain is
            // fully wired to an entity (e.g., in test setup).
            if (WakeOnDamage)
            {
                int hp = ParentEntity.GetStatValue("Hitpoints", -1);
                if (_lastHp >= 0 && hp >= 0 && hp < _lastHp)
                {
                    _wakeRequested = true;
                    _lastHp = hp;
                    EmitWakeParticle();
                    return;
                }
                _lastHp = hp;
            }

            // Hostile-wake check: any hostile in sight radius.
            if (WakeOnHostileInSight && ParentBrain != null)
            {
                var hostile = AIHelpers.FindNearestHostile(
                    ParentEntity, CurrentZone, ParentBrain.SightRadius);
                if (hostile != null)
                {
                    ParentBrain.Target = hostile;
                    _wakeRequested = true;
                    EmitWakeParticle();
                    return;
                }
            }

            // Sleep visual (occasional 'z' particle)
            if (SleepParticleInterval > 0 && Age > 0 && Age % SleepParticleInterval == 0)
            {
                var pos = CurrentZone.GetEntityPosition(ParentEntity);
                if (pos.x >= 0)
                    AsciiFxBus.EmitParticle(CurrentZone, pos.x, pos.y - 1, 'z', "&c", 0.5f);
            }
        }

        private void EmitWakeParticle()
        {
            var pos = CurrentZone.GetEntityPosition(ParentEntity);
            if (pos.x >= 0)
                AsciiFxBus.EmitParticle(CurrentZone, pos.x, pos.y - 1, '!', "&Y", 0.4f);
        }
    }
}
