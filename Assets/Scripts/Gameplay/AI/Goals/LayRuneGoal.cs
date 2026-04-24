using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Walk → Place state machine that has the NPC walk to a target cell
    /// and spawn a rune entity there. Mirrors Qud's
    /// <c>XRL.World.AI.GoalHandlers.LayMineGoal</c> (LayMineGoal.cs lines
    /// 45-82) — same two-phase shape, simplified for CoO:
    /// <list type="number">
    ///   <item><b>Walk</b> — push <see cref="MoveToGoal"/> toward Target.</item>
    ///   <item><b>Place</b> — when at Target, spawn the rune entity at the
    ///   NPC's current cell, stamp the layer's faction onto its
    ///   <see cref="TriggerOnStepPart.TriggerFaction"/>, and pop.</item>
    /// </list>
    ///
    /// <para><b>CoO simplifications vs Qud.</b> Qud's LayMineGoal lays the
    /// mine when <c>DistanceTo(Target) == 1</c> (adjacent) and drops it at
    /// <c>ParentObject.CurrentCell</c>, not at Target itself. CoO drops the
    /// rune AT the target cell — the NPC walks onto the cell and lays it
    /// there. This is safe because <see cref="TriggerOnStepPart"/> excludes
    /// the mover from the <c>EntityEnteredCell</c> dispatch (so the layer's
    /// arrival on the cell does not trigger the newly-laid rune), and the
    /// faction filter means later same-faction occupants also pass through
    /// safely. Also, Qud's bomb / timer mechanic is omitted — CoO runes
    /// are pure step triggers, no countdown.</para>
    ///
    /// <para><b>CanFight.</b> False, matching Qud (LayMineGoal.cs line 35).
    /// An NPC in the middle of laying a rune interrupts to fight when
    /// attacked — the goal's <see cref="Failed"/> propagation covers that
    /// naturally via BrainPart's combat-interrupt path.</para>
    ///
    /// <para><b>Factory injection.</b> Rune entities are created from
    /// blueprints via <see cref="Factory"/>, wired by
    /// <c>GameBootstrap</c> at startup. Follows the
    /// <see cref="CorpsePart.Factory"/> / <c>MaterialReactionResolver.Factory</c>
    /// convention. Tests that don't need spawning can leave it null — the
    /// goal no-ops and pops.</para>
    /// </summary>
    public class LayRuneGoal : GoalHandler
    {
        // ====================================================================
        // Factory — wired by GameBootstrap at startup. Tests override directly.
        // ====================================================================
        public static EntityFactory Factory;

        /// <summary>Maximum MoveToGoal child pushes before giving up.
        /// Matches the DisposeOfCorpseGoal.MaxMoveTries convention (M5.3).</summary>
        public const int MaxMoveTries = 10;

        /// <summary>Per-leg MaxTurns budget handed to child MoveToGoal.
        /// Matches DisposeOfCorpseGoal.ChildMoveMaxTurns.</summary>
        public const int ChildMoveMaxTurns = 20;

        /// <summary>Cell where the rune should be laid.</summary>
        public int TargetX;
        public int TargetY;

        /// <summary>Blueprint name of the rune entity to spawn
        /// (e.g. "RuneOfFlame").</summary>
        public string RuneBlueprint;

        /// <summary>Count of MoveToGoal pushes so far.</summary>
        public int MoveTries;

        private bool _done;

        public LayRuneGoal(int targetX, int targetY, string runeBlueprint)
        {
            TargetX = targetX;
            TargetY = targetY;
            RuneBlueprint = runeBlueprint;
        }

        public override bool Finished() => _done;

        // Mirrors Qud LayMineGoal.cs line 35 — NPCs interrupt rune-laying
        // to fight. BrainPart's combat-interrupt path handles the pop.
        public override bool CanFight() => false;

        public override string GetDetails()
            => $"blueprint={RuneBlueprint} target=({TargetX},{TargetY}) tries={MoveTries}/{MaxMoveTries}";

        public override void TakeAction()
        {
            var zone = CurrentZone;
            var actor = ParentEntity;
            if (zone == null || actor == null) { FailToParent(); return; }
            if (string.IsNullOrEmpty(RuneBlueprint)) { FailToParent(); return; }
            if (!zone.InBounds(TargetX, TargetY)) { FailToParent(); return; }

            var pos = zone.GetEntityPosition(actor);
            if (pos.x < 0) { FailToParent(); return; }

            // At the target cell — lay the rune.
            if (pos.x == TargetX && pos.y == TargetY)
            {
                LayRune(actor, zone, pos.x, pos.y);
                _done = true;
                return;
            }

            // Not at target — push MoveToGoal, up to MaxMoveTries times.
            if (++MoveTries > MaxMoveTries)
            {
                // Exhausted retries (matches DisposeOfCorpseGoal — pop quietly,
                // the behavior part that pushed us will pick a new target on
                // its next tick). Not FailToParent because there's no
                // alternative at the parent level.
                _done = true;
                return;
            }

            Think("laying rune");
            PushChildGoal(new MoveToGoal(TargetX, TargetY, ChildMoveMaxTurns));
        }

        /// <summary>
        /// Spawn the rune at (x, y), stamp the layer's faction onto its
        /// TriggerOnStepPart so it ignores the layer's allies, and add it
        /// to the zone.
        /// </summary>
        private void LayRune(Entity actor, Zone zone, int x, int y)
        {
            var factory = Factory;
            if (factory == null)
            {
                // No factory wired (test or uninitialised runtime).
                // Graceful no-op — don't crash.
                UnityEngine.Debug.LogWarning(
                    "[LayRuneGoal] Factory is null; cannot spawn rune. " +
                    "Wire LayRuneGoal.Factory = factory at bootstrap.");
                return;
            }

            var rune = factory.CreateEntity(RuneBlueprint);
            if (rune == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[LayRuneGoal] factory.CreateEntity('{RuneBlueprint}') " +
                    "returned null. Check blueprint name in Objects.json.");
                return;
            }

            // Faction stamp — the rune's TriggerOnStepPart uses this to
            // avoid detonating on allies of the layer.
            var trigger = rune.GetPart<TriggerOnStepPart>();
            if (trigger != null)
            {
                string layerFaction = FactionManager.GetFaction(actor);
                if (!string.IsNullOrEmpty(layerFaction))
                    trigger.TriggerFaction = layerFaction;
            }

            zone.AddEntity(rune, x, y);
            MessageLog.Add($"{actor.GetDisplayName()} lays a {rune.GetDisplayName()}.");
        }

        public override void Failed(GoalHandler child)
        {
            // A child MoveToGoal explicitly failed (unreachable target).
            // Propagate — retrying is futile and BrainPart's combat path
            // already handles hostile-interrupt separately.
            FailToParent();
        }

        public override void OnPop()
        {
            // Clear sticky thought, same pattern as DisposeOfCorpseGoal.OnPop
            // (M5.2 UX lesson: terminal thoughts that outlive the goal are
            // bad UX because nothing else overwrites them).
            Think(null);
        }
    }
}
