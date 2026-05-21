using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8d.1 — multi-stage fungal infection. CoO-simplified port of
    /// Qud's <c>FungalSporeInfection</c> (qud FungalSporeInfection.cs).
    /// Qud's version literally grows armor/weapon items on body parts
    /// as the infection matures — out of scope for CoO (no body-part-
    /// grown-equipment infrastructure). The portable Qud-parity feature
    /// is the <b>multi-stage state machine</b>:
    ///
    /// <list type="table">
    ///   <listheader><term>Stage</term><description>Turn range / Behavior</description></listheader>
    ///   <item><term>Incubation</term><description>turns 0-9: silent; transition messages only</description></item>
    ///   <item><term>Symptomatic</term><description>turns 10-19: 1 dmg/turn, -1 Toughness</description></item>
    ///   <item><term>Blooming</term><description>turns 20-29: 2 dmg/turn, -2 Toughness; (G.8d.3) spawns spore gas at host's cell (contagion)</description></item>
    ///   <item><term>Terminal</term><description>turns 30-39: 3 dmg/turn, -3 Toughness; (G.8d.3) bigger contagion</description></item>
    ///   <item><term>Expired</term><description>turn 40+: self-removes via Duration=0</description></item>
    /// </list>
    ///
    /// <para><b>Stat-shift Apply/Remove pattern.</b> Mirrors
    /// <see cref="HibernatingEffect"/>'s convention: capture
    /// PriorToughness on <see cref="OnApply"/>, restore on
    /// <see cref="OnRemove"/>. -1 sentinel for "OnApply hasn't run yet"
    /// — important for save round-trip resilience (an entity that saves
    /// mid-infection then loads must have the sentinel survive — Public
    /// field, not private).</para>
    ///
    /// <para><b>Refresh-on-reapply is INTENTIONALLY non-standard.</b>
    /// Other gas effects (Poison/Stun/Confusion/Sleep) refresh Duration
    /// on reapply. FungalInfection does NOT — once you're infected, the
    /// infection progresses regardless of fresh exposure. Walking
    /// through another spore cloud while in Stage 2 doesn't reset you
    /// to Stage 0. Pinned by test.</para>
    /// </summary>
    public class FungalInfectionEffect : Effect
    {
        public override string DisplayName => "fungal infection";

        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        /// <summary>Stage progression. Public for save round-trip.</summary>
        public enum InfectionStage
        {
            Incubation,
            Symptomatic,
            Blooming,
            Terminal,
            Expired,
        }

        // Stage thresholds (turn counts at which stage transitions occur).
        public const int STAGE_INCUBATION_END = 10;
        public const int STAGE_SYMPTOMATIC_END = 20;
        public const int STAGE_BLOOMING_END = 30;
        public const int STAGE_TERMINAL_END = 40;

        // Damage per turn at each stage.
        public const int DAMAGE_INCUBATION = 0;
        public const int DAMAGE_SYMPTOMATIC = 1;
        public const int DAMAGE_BLOOMING = 2;
        public const int DAMAGE_TERMINAL = 3;

        // Toughness penalty at each stage.
        public const int PENALTY_INCUBATION = 0;
        public const int PENALTY_SYMPTOMATIC = 1;
        public const int PENALTY_BLOOMING = 2;
        public const int PENALTY_TERMINAL = 3;

        /// <summary>Turns since infection start. Drives stage transitions.
        /// Public for save round-trip.</summary>
        public int TurnsInfected;

        /// <summary>Pre-infection Toughness, restored on OnRemove.
        /// -1 sentinel = "OnApply hasn't run yet". Public for save
        /// round-trip (HibernatingEffect SL.6.4 convention).</summary>
        public int PriorToughness = -1;

        /// <summary>Current stage based on <see cref="TurnsInfected"/>.</summary>
        public InfectionStage CurrentStage
        {
            get
            {
                if (TurnsInfected < STAGE_INCUBATION_END) return InfectionStage.Incubation;
                if (TurnsInfected < STAGE_SYMPTOMATIC_END) return InfectionStage.Symptomatic;
                if (TurnsInfected < STAGE_BLOOMING_END) return InfectionStage.Blooming;
                if (TurnsInfected < STAGE_TERMINAL_END) return InfectionStage.Terminal;
                return InfectionStage.Expired;
            }
        }

        public FungalInfectionEffect()
        {
            Duration = DURATION_INDEFINITE;
        }

        public override void OnApply(Entity target)
        {
            if (target == null) return;
            PriorToughness = target.GetStatValue("Toughness", 0);
            MessageLog.Add(target.GetDisplayName() + "'s skin itches strangely.");
        }

        public override void OnRemove(Entity target)
        {
            // -1 sentinel: OnApply didn't run (defensive — would be
            // an order-violation bug). Skip restore.
            if (target == null) return;
            if (PriorToughness < 0) return;
            var tough = target.GetStat("Toughness");
            if (tough != null) tough.BaseValue = PriorToughness;
            MessageLog.Add(target.GetDisplayName() + " is cured of the fungal infection.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            if (target == null) return;
            var before = CurrentStage;
            TurnsInfected++;
            var after = CurrentStage;

            // Stage transition: apply the new stage's stat shift + log.
            if (after != before)
            {
                ApplyStageStatShift(target, after);
                EmitStageTransitionMessage(target, after);
            }

            // Per-turn damage scaling by stage.
            int dmg = StageDamage(after);
            if (dmg > 0)
            {
                var d = new Damage(dmg);
                d.AddAttribute("Fungal");
                var zone = context?.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
                CombatSystem.ApplyDamage(target, d, source: null, zone: zone);
            }

            // G.8d.3 — contagion: at Blooming+ stages, the host
            // periodically releases spore gas at their cell. RED stub
            // — implementation in next step.
            TrySpawnContagion(target, after, context);

            // Self-expire on Expired stage.
            if (after == InfectionStage.Expired)
                Duration = 0;
        }

        // G.8d.3 — contagion tunings.
        public const int BLOOMING_CONTAGION_CADENCE = 3;
        public const int BLOOMING_CONTAGION_DENSITY = 30;
        public const int TERMINAL_CONTAGION_CADENCE = 2;
        public const int TERMINAL_CONTAGION_DENSITY = 50;
        public const string CONTAGION_GAS_ID = "fungal-spores";
        public const int CONTAGION_GAS_LEVEL = 1;

        /// <summary>G.8d.3 contagion mechanic. At Blooming + Terminal
        /// stages, the host periodically releases spore gas at their
        /// cell. The spores can infect ADJACENT creatures (the
        /// gas-dispersal + GasFungalSporesPart.ApplyGas path handles
        /// the spread). Self-immunity is enforced downstream — the
        /// host's `already infected` check in GasFungalSporesPart
        /// short-circuits re-infection by their own spores.
        ///
        /// <para>Mirrors Qud's `SporePuffer` mechanic where the host
        /// becomes a periodic-gas-emitter once the infection blooms.
        /// CoO simplification: instead of a separate Part attached on
        /// stage transition, the same FungalInfectionEffect handles
        /// the periodic spawn from inside its OnTurnStart.</para>
        /// </summary>
        protected virtual void TrySpawnContagion(Entity host, InfectionStage stage, GameEvent context)
        {
            int cadence, density, stageStart;
            switch (stage)
            {
                case InfectionStage.Blooming:
                    cadence = BLOOMING_CONTAGION_CADENCE;
                    density = BLOOMING_CONTAGION_DENSITY;
                    stageStart = STAGE_SYMPTOMATIC_END; // Blooming starts here
                    break;
                case InfectionStage.Terminal:
                    cadence = TERMINAL_CONTAGION_CADENCE;
                    density = TERMINAL_CONTAGION_DENSITY;
                    stageStart = STAGE_BLOOMING_END;
                    break;
                default:
                    return; // Incubation / Symptomatic / Expired: no contagion
            }

            int turnsInStage = TurnsInfected - stageStart;
            if (turnsInStage < 0) return; // defensive
            if (turnsInStage % cadence != 0) return;

            // Zone resolution: context first, then ActiveZone fallback
            // (mirrors PoisonedByGasEffect / damage-tick path).
            var zone = context?.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (zone == null) return;

            var pos = zone.GetEntityPosition(host);
            if (pos.x < 0) return; // host not in zone

            // Spawn fungal-spores gas at host's cell. Creator = host
            // (provenance: downstream infections trace back here).
            GasFactory.SpawnGas(zone, pos.x, pos.y, CONTAGION_GAS_ID,
                density: density, level: CONTAGION_GAS_LEVEL, creator: host);

            Diag.Record("gas", "Contagion", host, null,
                new
                {
                    stage = stage.ToString(),
                    turnsInfected = TurnsInfected,
                    cadence,
                    spawnDensity = density,
                    spawnX = pos.x,
                    spawnY = pos.y,
                });
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is FungalInfectionEffect)
            {
                // CRITICAL invariant: re-exposure does NOT reset the
                // stage clock. The incoming fresh infection is consumed
                // (return true) but TurnsInfected is preserved.
                Diag.Record("gas", "InfectionAlreadyPresent", null, Owner,
                    new
                    {
                        currentStage = CurrentStage.ToString(),
                        turnsInfected = TurnsInfected,
                    });
                return true;
            }
            return false;
        }

        /// <summary>Apply the per-stage Toughness penalty as a stat
        /// shift relative to <see cref="PriorToughness"/>. Floor at 0
        /// so a low-Toughness target doesn't go negative.</summary>
        private void ApplyStageStatShift(Entity target, InfectionStage stage)
        {
            if (PriorToughness < 0) return; // OnApply didn't run
            var tough = target.GetStat("Toughness");
            if (tough == null) return;
            int penalty = StagePenalty(stage);
            int newValue = PriorToughness - penalty;
            if (newValue < 0) newValue = 0;
            tough.BaseValue = newValue;
        }

        private static void EmitStageTransitionMessage(Entity target, InfectionStage stage)
        {
            string name = target?.GetDisplayName() ?? "creature";
            switch (stage)
            {
                case InfectionStage.Symptomatic:
                    MessageLog.Add(name + "'s skin breaks out in fungal patches.");
                    break;
                case InfectionStage.Blooming:
                    MessageLog.Add(name + " sprouts visible fungal growths.");
                    break;
                case InfectionStage.Terminal:
                    MessageLog.Add(name + " is consumed by the fungal infection!");
                    break;
                case InfectionStage.Expired:
                    MessageLog.Add(name + " has run its course with the fungus.");
                    break;
            }
        }

        /// <summary>Per-stage damage. Pure function — no side effects.</summary>
        public static int StageDamage(InfectionStage stage) => stage switch
        {
            InfectionStage.Symptomatic => DAMAGE_SYMPTOMATIC,
            InfectionStage.Blooming    => DAMAGE_BLOOMING,
            InfectionStage.Terminal    => DAMAGE_TERMINAL,
            _ => 0, // Incubation, Expired
        };

        /// <summary>Per-stage Toughness penalty.</summary>
        public static int StagePenalty(InfectionStage stage) => stage switch
        {
            InfectionStage.Symptomatic => PENALTY_SYMPTOMATIC,
            InfectionStage.Blooming    => PENALTY_BLOOMING,
            InfectionStage.Terminal    => PENALTY_TERMINAL,
            _ => 0,
        };
    }
}
