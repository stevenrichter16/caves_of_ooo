using System;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Corpse spawner. Attached to living creatures that should drop a corpse
    /// entity at their death cell when they die. Mirrors Qud's
    /// <c>XRL.World.Parts.Corpse</c> part (spawner-on-creature pattern, not an
    /// entity type).
    ///
    /// Hooks the <c>"Died"</c> event fired by
    /// <see cref="CombatSystem.HandleDeath"/>. The event fires AFTER equipment
    /// and inventory have dropped and BEFORE <c>Zone.RemoveEntity(target)</c>,
    /// so the deceased's cell is still resolvable via the event's
    /// <c>"Zone"</c> parameter.
    ///
    /// <para><b>Factory injection.</b> Spawning an entity from a blueprint
    /// requires an <see cref="EntityFactory"/>. Following the established
    /// <see cref="MaterialReactionResolver.Factory"/> / <see cref="ConversationActions.Factory"/>
    /// pattern, the factory is stored on a static field that
    /// <see cref="Presentation.Bootstrap.GameBootstrap"/> wires at startup.
    /// Tests that don't need corpse drops can leave it null — the part no-ops
    /// gracefully.</para>
    ///
    /// <para><b>Scope of M5.1.</b> Single-variant drop only. Qud's
    /// burnt / vaporized variants (driven by
    /// <c>Physics.LastDamagedByType</c>) are deferred to M9 (damage-type
    /// system) — CoO has no last-damage-type tracking yet.</para>
    ///
    /// <para><b>Suppression.</b> If the parent has the
    /// <c>SuppressCorpseDrops</c> tag, the drop is skipped. Mirrors Qud's
    /// <c>GetIntProperty("SuppressCorpseDrops")</c> gate.</para>
    ///
    /// <para><b>Properties copied to the corpse</b> (mirrors Qud's
    /// <c>ProcessCorpseDrop</c> lines 138-163):</para>
    /// <list type="bullet">
    ///   <item><c>CreatureName</c> — the deceased's display name</item>
    ///   <item><c>SourceBlueprint</c> — the deceased's blueprint</item>
    ///   <item><c>SourceID</c> — the deceased's runtime ID</item>
    ///   <item><c>KillerID</c> — the killer's runtime ID (if killer is set and not self)</item>
    ///   <item><c>KillerBlueprint</c> — the killer's blueprint (if applicable)</item>
    /// </list>
    /// </summary>
    public class CorpsePart : Part
    {
        public override string Name => "Corpse";

        // ====================================================================
        // Global factory — set once by GameBootstrap, read at event-fire time.
        // Mirrors the MaterialReactionResolver.Factory and
        // ConversationActions.Factory conventions in the codebase. Tests that
        // leave this null trigger the graceful no-op path in HandleDied.
        // ====================================================================
        public static EntityFactory Factory;

        // ====================================================================
        // Blueprint-configurable fields (set via EntityFactory reflection).
        // ====================================================================

        /// <summary>
        /// Percent chance to drop a corpse on death (0-100). 0 = never drops.
        /// Mirrors Qud's <c>Corpse.CorpseChance</c>.
        /// </summary>
        public int CorpseChance = 0;

        /// <summary>
        /// Blueprint name of the corpse entity to spawn. Null / empty disables
        /// corpse drops. Mirrors Qud's <c>Corpse.CorpseBlueprint</c>.
        /// </summary>
        public string CorpseBlueprint = null;

        /// <summary>
        /// Meta-gate applied before <see cref="CorpseChance"/>. In Qud this
        /// gates drops when the inventory zone is not yet built (preventing
        /// world-gen NPCs from pre-seeding dungeons with corpses). CoO doesn't
        /// build-check — we keep the field and default to 100 so corpses drop
        /// unconditionally on the CorpseChance gate alone.
        /// Mirrors Qud's <c>Corpse.BuildCorpseChance</c>.
        /// </summary>
        public int BuildCorpseChance = 100;

        // ====================================================================
        // Test injection hooks.
        // ====================================================================

        /// <summary>
        /// Deterministic RNG override for tests. When non-null, used instead of
        /// the instance RNG so tests can force the CorpseChance gate to pass /
        /// fail predictably without relying on <see cref="BrainPart.Rng"/>
        /// (which this part doesn't own).
        /// </summary>
        public Random TestRng;

        private Random _rng;

        // ====================================================================
        // Event handling.
        // ====================================================================

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Died")
                HandleDied(e);
            // Always propagate — other "Died" listeners (StatusEffectsPart.RemoveAllEffects,
            // GivesRepPart rep-award, M2.3 witness broadcast) still need to fire.
            return true;
        }

        private void HandleDied(GameEvent e)
        {
            if (ParentEntity == null) return;

            // Suppression flag (Qud parity: SuppressCorpseDrops int property).
            // CoO uses tag-equivalent since we don't need the graded
            // increment/decrement semantics Qud does.
            if (ParentEntity.HasTag("SuppressCorpseDrops"))
                return;

            // No factory wired — test or uninitialised runtime. Graceful no-op.
            var factory = Factory;
            if (factory == null) return;

            // Unconfigured spawner — missing blueprint or zero chance.
            if (CorpseChance <= 0 || string.IsNullOrEmpty(CorpseBlueprint))
                return;

            var rng = TestRng ?? _rng ?? (_rng = new Random());

            // Two-stage gate matches Qud's ProcessCorpseDrop structure.
            // BuildCorpseChance is checked first (the "is this drop even
            // possible right now" meta-gate), then CorpseChance (the
            // per-creature probability).
            if (BuildCorpseChance < 100 && rng.Next(100) >= BuildCorpseChance)
                return;
            if (rng.Next(100) >= CorpseChance)
                return;

            // Resolve zone from the event — added by CombatSystem for M5.
            var zone = e.GetParameter<Zone>("Zone");
            if (zone == null)
                return;

            // Cell must still be resolvable — Died fires BEFORE
            // zone.RemoveEntity so GetEntityCell should return non-null.
            // Pinning this expectation with a regression test is mandatory.
            var cell = zone.GetEntityCell(ParentEntity);
            if (cell == null)
                return;

            var corpse = factory.CreateEntity(CorpseBlueprint);
            if (corpse == null)
                return;

            // Descriptor properties — mirrors Qud Corpse.cs lines 138-163.
            // We intentionally use Entity.Properties (string dict) rather than
            // string-typed fields on the corpse part itself: consumers
            // (Examinable, AIUndertaker) read via GetProperty, which is the
            // established CoO pattern for per-instance descriptor data.
            corpse.Properties["CreatureName"] = ParentEntity.GetDisplayName();
            if (!string.IsNullOrEmpty(ParentEntity.BlueprintName))
                corpse.Properties["SourceBlueprint"] = ParentEntity.BlueprintName;
            if (!string.IsNullOrEmpty(ParentEntity.ID))
                corpse.Properties["SourceID"] = ParentEntity.ID;

            var killer = e.GetParameter<Entity>("Killer");
            if (killer != null && killer != ParentEntity)
            {
                if (!string.IsNullOrEmpty(killer.ID))
                    corpse.Properties["KillerID"] = killer.ID;
                if (!string.IsNullOrEmpty(killer.BlueprintName))
                    corpse.Properties["KillerBlueprint"] = killer.BlueprintName;
            }

            zone.AddEntity(corpse, cell.X, cell.Y);
        }
    }
}
