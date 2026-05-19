using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Liquid Spell Test Bench (manual QA) — v3 SELF-AUDITING.
    ///
    /// <para><b>Why v3.</b> Diag audits of the v1/v2 runs proved the
    /// MECHANICS are correct but player-aimed spells are an unreliable
    /// audit instrument: Conflagration is a radius blast (not a piercing
    /// line), so casts only ever landed on the nearest dummy (diag:
    /// 26 hits on the water dummy, 0 on the other five); and ArcBolt
    /// applies an ElectrifiedEffect that — on a turn-frozen
    /// (<c>NotRegisteredForTurns</c>) dummy — never expires, so
    /// divergence #6 makes the coat permanently *yield* Electric and the
    /// water→Electric ×2 amp becomes unobservable after the first
    /// ArcBolt. Net: oil/pitch/brine/ichor were never audited.</para>
    ///
    /// <para><b>v3 fix.</b> Stop depending on player aim. At the end of
    /// <see cref="Apply"/> the bench runs a deterministic synthetic
    /// matrix: each dummy (incl. the Dry control) is dealt a fixed
    /// <see cref="BASE"/>-amount hit of Heat / Electric / Cold / Acid
    /// via <see cref="CombatSystem.ApplyDamage"/> (the same pipeline a
    /// real spell uses, so <c>damage/PreDamageMutation</c> fires), HP is
    /// snapshotted and restored around each cell (immortal rigs), and
    /// any <see cref="ElectrifiedEffect"/> is stripped before the
    /// Electric cell so div #6 can never suppress the conductivity amp.
    /// Result: launching the scenario instantly produces a complete,
    /// machine-checkable matrix in the message log AND in the diag
    /// (<c>diag_query category=liquid kind=MatrixAudit</c> and
    /// <c>category=damage kind=PreDamageMutation</c>) — zero aiming,
    /// zero turn dependency, zero div-#6 interference. Relaunch to
    /// re-run.</para>
    ///
    /// <para><b>v3.1.</b> v3's live diag audit caught a scenario bug:
    /// the hardcoded spawn offsets landed on SampleScene decor so only
    /// 2 of 6 dummies actually placed, and the audit reported the
    /// unspawned four as phantom "×1.00" rows. Fixed two ways: (1) clear
    /// the corridor before spawning (the proven ElementalCreatureZoo
    /// pattern) on a single collision-free row; (2) the audit now
    /// verifies <see cref="Zone.GetEntityPosition"/> ≥ 0 per dummy and
    /// emits a loud <c>SPAWN-FAILED</c> line + <c>liquid/MatrixAuditSkipped</c>
    /// instead of a misleading ×1.00 — a spawn failure can never again
    /// masquerade as "no interaction".</para>
    ///
    /// The dummies are still real, stationary, permanently-coated and
    /// stat-bearing, so manual casting still works as a secondary check
    /// (the player keeps the ArcBolt/Conflagration/IceLance/AcidSpray
    /// kit) — but the audit no longer needs it.
    ///
    /// Expected end-to-end factors (measured = HP delta ÷ base, which
    /// captures BOTH the OnBeforeTakeDamage coat layer AND the LQ.6
    /// resistance layer):
    ///   - water  : Heat ≈0.60 (FireDampen 40) · Electric ≈2.0 (Cond 100)
    ///   - oil    : Heat ≈1.45 (Combust 90)
    ///   - pitch  : Heat ≈1.45 (Combust 90)  [+ −2 Agi/−3 DV stat coat]
    ///   - brine  : Electric >2.0 (Cond 100 ×2 AND −15 ElecRes compounds)
    ///              · Heat <1.0 (+15 HeatRes)
    ///   - ichor  : Cold >1.0 (−20 ColdRes)  [+4 AV vs physical]
    ///   - dry    : all ≈1.0 (control / baseline)
    /// Honesty bound: water/oil/pitch OnBeforeTakeDamage factors are
    /// exact; brine/ichor are resistance-compounded so the bench reports
    /// the measured end-to-end number and the expected *direction*, not
    /// a hard-asserted constant (resistance has its own curve).
    /// </summary>
    [Scenario(
        name: "Liquid Spell Test Bench",
        category: "Combat",
        description: "Self-auditing: launches → deterministically tests Heat/Electric/Cold/Acid on water/oil/pitch/brine/ichor + Dry control, logs the full matrix + diag (liquid/MatrixAudit, damage/PreDamageMutation). No aiming needed; manual spell kit still provided.")]
    public class LiquidSpellTestBench : IScenario
    {
        // Effectively permanent coat (dummy is turn-frozen, but huge
        // amount is belt-and-suspenders vs any passive sim tick).
        private const int PERMA_COAT = 100000;

        // Synthetic test-hit base. Large so ×0.60 / ×1.45 / ×2.0 produce
        // clearly distinct integers and integer rounding is negligible.
        private const int BASE = 100;

        private static readonly string[] Elements = { "Heat", "Electric", "Cold", "Acid" };

        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            ctx.Player
                .SetStatMax("Hitpoints", 999)
                .SetHp(999)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .AddMutation("ArcBoltMutation", level: 5)
                .AddMutation("ConflagrationMutation", level: 5)
                .AddMutation("IceLanceMutation", level: 5)
                .AddMutation("AcidSprayMutation", level: 5)
                .GiveItem("HealingTonic", 5);

            // v3.1: clear the corridor BEFORE spawning. v3's hardcoded
            // offsets landed on SampleScene decor/walls so most dummies
            // never placed (diag proved only 2 of 6 spawned) and the
            // audit reported phantom 100% rows. Mirror the proven
            // ElementalCreatureZoo pattern: clear the dummy row + the two
            // adjacent rows (covers every dummy cell AND its orthogonal
            // cosmetic-pool-ring cell) across the full span.
            for (int dx = 1; dx <= 14; dx++)
            {
                ctx.World.ClearCell(p.x + dx, p.y);
                ctx.World.ClearCell(p.x + dx, p.y - 1);
                ctx.World.ClearCell(p.x + dx, p.y + 1);
            }

            // Single clean row, 2 apart. Off-row positions are gone —
            // v3 is self-auditing so player-aim positioning is moot, and
            // one row is trivially clearable + collision-free.
            var rig = new List<(string coat, Entity npc)>
            {
                ("dry",            Dummy(ctx, null,             p.x + 2,  p.y)),
                ("water",          Dummy(ctx, "water",          p.x + 4,  p.y)),
                ("oil",            Dummy(ctx, "oil",            p.x + 6,  p.y)),
                ("pitch",          Dummy(ctx, "pitch",          p.x + 8,  p.y)),
                ("brine",          Dummy(ctx, "brine",          p.x + 10, p.y)),
                ("carapace-ichor", Dummy(ctx, "carapace-ichor", p.x + 12, p.y)),
            };

            RunMatrixAudit(ctx, rig);

            ctx.Log("=== Liquid Spell Test Bench (v3.1 — self-auditing) ===");
            ctx.Log("A full synthetic matrix just ran (see [MatrixAudit] lines).");
            ctx.Log("Confirm in diag: diag_query category=liquid kind=MatrixAudit");
            ctx.Log("  and diag_query category=damage kind=PreDamageMutation.");
            ctx.Log("Relaunch the scenario to re-run the audit.");
            ctx.Log("Manual spell kit (ArcBolt/Conflagration/IceLance/AcidSpray)");
            ctx.Log("is still provided for hands-on poking — not required.");
        }

        /// <summary>
        /// Deterministically deals one fixed-base hit of each element to
        /// every rig dummy, measures the end-to-end factor (HP delta ÷
        /// base — captures the coat layer AND the resistance layer),
        /// restores HP (immortal rigs), and emits a machine-checkable
        /// <c>liquid/MatrixAudit</c> diag record + a readable log line
        /// per cell. ElectrifiedEffect is stripped before the Electric
        /// cell so divergence #6 can never suppress the conductivity amp.
        /// </summary>
        private static void RunMatrixAudit(ScenarioContext ctx, List<(string coat, Entity npc)> rig)
        {
            MessageLog.Add("───── [MatrixAudit] synthetic element matrix (base=" + BASE + ") ─────");
            foreach (var (coat, npc) in rig)
            {
                // Robustness (v3.1): a dummy that failed to place into
                // the zone must NOT be audited — otherwise a spawn
                // failure masquerades as "100% no interaction" (exactly
                // the phantom-row bug v3 shipped). Report it loudly.
                if (npc == null || ctx.Zone.GetEntityPosition(npc).x < 0)
                {
                    MessageLog.Add($"[MatrixAudit] {coat,-15} SPAWN-FAILED — cell blocked, NOT audited");
                    Diag.Record("liquid", "MatrixAuditSkipped", actor: npc, target: null,
                        payload: new { liquid = coat, reason = "spawn_failed" });
                    continue;
                }
                foreach (var elem in Elements)
                {
                    // Div-#6 bypass: a synthetic raw hit must never be
                    // suppressed by a stale Electrified yield.
                    npc.GetPart<StatusEffectsPart>()?.RemoveEffect<ElectrifiedEffect>();

                    var hp = npc.GetStat("Hitpoints");
                    if (hp == null) continue;
                    int hp0 = hp.BaseValue;

                    var dmg = new Damage(BASE);
                    dmg.AddAttribute(elem);
                    CombatSystem.ApplyDamage(npc, dmg, source: null, zone: ctx.Zone);

                    int dealt = hp0 - npc.GetStat("Hitpoints").BaseValue;
                    npc.GetStat("Hitpoints").BaseValue = hp0; // restore — immortal rig

                    float factor = dealt / (float)BASE;
                    MessageLog.Add(string.Format(
                        "[MatrixAudit] {0,-15} {1,-9} base={2} dealt={3,4}  ×{4:0.00}  {5}",
                        coat, elem, BASE, dealt, factor, Hint(coat, elem)));

                    Diag.Record("liquid", "MatrixAudit", actor: npc, target: null,
                        payload: new
                        {
                            liquid = coat,
                            element = elem,
                            baseAmount = BASE,
                            dealt,
                            factorPctOfBase = (int)(factor * 100),
                        });
                }
            }
            MessageLog.Add("───── [MatrixAudit] complete ─────");
        }

        /// <summary>Human-readable expectation per (liquid,element) so
        /// the log line is self-checking at a glance. The MEASURED
        /// factor is authoritative; this is just orientation.</summary>
        private static string Hint(string coat, string elem)
        {
            switch (coat + "/" + elem)
            {
                case "water/Heat": return "expect ~0.60 (FireDampen 40)";
                case "water/Electric": return "expect ~2.00 (Conductivity 100)";
                case "oil/Heat": return "expect ~1.45 (Combust 90)";
                case "pitch/Heat": return "expect ~1.45 (Combust 90)";
                case "brine/Electric": return "expect >2.00 (Cond ×2 + −15 ElecRes)";
                case "brine/Heat": return "expect <1.00 (+15 HeatRes)";
                case "carapace-ichor/Cold": return "expect >1.00 (−20 ColdRes)";
                case "dry/Heat":
                case "dry/Electric":
                case "dry/Cold":
                case "dry/Acid": return "baseline ≈1.00 (control)";
                default: return "≈1.00 (no interaction expected)";
            }
        }

        /// <summary>
        /// Spawn a stationary, turn-unregistered, stat-bearing dummy,
        /// inject the combat/resistance stats Snapjaw's blueprint lacks,
        /// pre-apply a permanent coat (<paramref name="liquidId"/> null
        /// = Dry control), ring it with a cosmetic pool, attach the
        /// manual-cast probe, and return the entity for the audit.
        /// </summary>
        private static Entity Dummy(ScenarioContext ctx, string liquidId, int x, int y)
        {
            var npc = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 4000)
                .WithHpAbsolute(4000)
                .Passive()
                .NotRegisteredForTurns()
                .At(x, y);
            if (npc == null) return null;

            void Stat(string n, int v) => npc.Statistics[n] =
                new Stat { Owner = npc, Name = n, BaseValue = v, Min = -200, Max = 400 };
            Stat("HeatResistance", 0);
            Stat("ColdResistance", 0);
            Stat("ElectricResistance", 0);
            Stat("AcidResistance", 0);
            Stat("AV", 0);
            Stat("DV", 0);
            if (npc.GetStat("Agility") == null) Stat("Agility", 14);

            npc.AddPart(new SpellTestProbePart { CoatLabel = liquidId ?? "dry" });

            if (string.IsNullOrEmpty(liquidId))
                return npc; // Dry control: no coat, no pool.

            int[,] cells = { { 0, 0 }, { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                var pool = new Entity
                { ID = $"{liquidId}Pool_{x}_{y}_{i}", BlueprintName = liquidId + "Pool" };
                pool.AddPart(new RenderPart { DisplayName = liquidId + " pool", RenderString = "~" });
                pool.AddPart(new PhysicsPart { Solid = false });
                pool.AddPart(new LiquidPoolPart { LiquidId = liquidId, Volume = PERMA_COAT });
                ctx.Zone.AddEntity(pool, x + cells[i, 0], y + cells[i, 1]);
            }

            npc.ApplyEffect(new LiquidCoveredEffect(liquidId, PERMA_COAT), source: null, zone: ctx.Zone);
            return npc;
        }
    }

    /// <summary>
    /// Showcase-only probe for the optional manual-cast path. On every
    /// incoming hit logs the element, amount, live elemental resistances
    /// and coat label. Production combat does not emit these lines; the
    /// authoritative audit figures are the v3 [MatrixAudit] lines and
    /// the <c>damage/PreDamageMutation</c> / <c>liquid/MatrixAudit</c>
    /// diag records.
    /// </summary>
    public class SpellTestProbePart : Part
    {
        public override string Name => "SpellTestProbe";

        public string CoatLabel = "dry";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "BeforeTakeDamage" || !(e.GetParameter("Damage") is Damage d))
                return true;

            string who = ParentEntity?.GetDisplayName() ?? "?";
            string elem =
                d.IsHeatDamage() ? "HEAT" :
                d.IsElectricDamage() ? "ELECTRIC" :
                d.IsColdDamage() ? "COLD" :
                d.IsAcidDamage() ? "ACID" : "physical";
            int hr = ParentEntity?.GetStatValue("HeatResistance", 0) ?? 0;
            int er = ParentEntity?.GetStatValue("ElectricResistance", 0) ?? 0;
            int cr = ParentEntity?.GetStatValue("ColdResistance", 0) ?? 0;

            MessageLog.Add(
                $"[SpellTest] {who} [{CoatLabel}]: {elem} in={d.Amount} " +
                $"(HeatRes={hr} ElecRes={er} ColdRes={cr})");
            return true;
        }
    }
}
