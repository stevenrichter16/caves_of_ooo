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
            // v3.2 CRITICAL (Rule 4 + Rule 8 fallout): scenario Apply()
            // can run BEFORE GameBootstrap's Step-1b' LiquidDefinitions
            // load completes. If so, every coat's OnApply/OnBeforeTakeDamage
            // early-returns on !LiquidRegistry.IsInitialized and the WHOLE
            // matrix records a phantom ×1.00 — indistinguishable from "no
            // interaction" (the exact Rule-4 failure mode). This was the
            // LX.3 bug: the bench's RunMatrixAudit had NEVER produced valid
            // data; prior "good" tables were stale persisted-buffer reads
            // from manual-cast sessions. Make the bench self-sufficient:
            // ensure the registry is loaded BEFORE any dummy is coated, so
            // OnApply stat-mods (lava −HeatRes, ichor −ColdRes) and
            // OnBeforeTakeDamage element re-weight both see a live registry.
            EnsureLiquidRegistry();

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
            for (int dx = 1; dx <= 42; dx++)
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
                // LX — Qud-liquid expansion (JSON-only): auto-audited
                // by the same synthetic matrix, no new test code.
                ("lava",           Dummy(ctx, "lava",           p.x + 14, p.y)),
                ("gel",            Dummy(ctx, "gel",            p.x + 16, p.y)),
                ("sap",            Dummy(ctx, "sap",            p.x + 18, p.y)),
                ("honey",          Dummy(ctx, "honey",          p.x + 20, p.y)),
                // LL — lore-grounded liquids (tepui-thread canon),
                // JSON-only; auto-audited by the same synthetic matrix.
                ("iron-gall-ink",  Dummy(ctx, "iron-gall-ink",  p.x + 22, p.y)),
                ("sundew-mucilage",Dummy(ctx, "sundew-mucilage",p.x + 24, p.y)),
                ("choir-wort",     Dummy(ctx, "choir-wort",     p.x + 26, p.y)),
                ("lumen-slime",    Dummy(ctx, "lumen-slime",    p.x + 28, p.y)),
                ("bog-mire",       Dummy(ctx, "bog-mire",       p.x + 30, p.y)),
                // LB — buff coats (positive lore-liquids). Most are
                // stat/tick/light/anchor liquids whose mechanics the
                // damage matrix can't see by design — the new TickAudit/
                // LightAudit/DeathAnchorAudit probes show them.
                // Spacing 1 for the LB block — zone is 80 wide (the
                // wall is at x=79 ⇒ p.x+40), so spacing-2 would push
                // bower-resin onto the wall and fail. Pools are
                // non-solid so stacked pool-rings between adjacent
                // dummies are fine. Player at p.x=39 ⇒ these sit at
                // x=71..75, well inside bounds.
                ("tepuibone-slurry",     Dummy(ctx, "tepuibone-slurry",     p.x + 32, p.y)),
                ("convalessence",        Dummy(ctx, "convalessence",        p.x + 33, p.y)),
                ("lantern-beetle-ichor", Dummy(ctx, "lantern-beetle-ichor", p.x + 34, p.y)),
                ("memory-bath",          Dummy(ctx, "memory-bath",          p.x + 35, p.y)),
                ("bower-resin-amber",    Dummy(ctx, "bower-resin-amber",    p.x + 36, p.y)),
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
        /// <summary>
        /// Make the bench bootstrap-order-independent: if GameBootstrap
        /// hasn't loaded the liquid defs yet, load them here exactly the
        /// way it does (Step 1b': Resources.LoadAll the
        /// LiquidDefinitions folder → InitializeFromJsonSources).
        /// Idempotent — a later GameBootstrap load just replaces with
        /// identical content.
        /// </summary>
        private static void EnsureLiquidRegistry()
        {
            if (LiquidRegistry.IsInitialized && LiquidRegistry.Count > 0)
                return;
            var assets = UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>(
                "Content/Data/LiquidDefinitions");
            if (assets == null || assets.Length == 0) return;
            var srcs = new List<string>(assets.Length);
            for (int i = 0; i < assets.Length; i++) srcs.Add(assets[i].text);
            LiquidRegistry.InitializeFromJsonSources(srcs);
        }

        private static void RunMatrixAudit(ScenarioContext ctx, List<(string coat, Entity npc)> rig)
        {
            // v3.2 robustness: the Diag ring buffer can PERSIST across
            // Play sessions when the project has domain-reload-on-play
            // disabled (this one does). Without a per-run id, a stale
            // older-run record is indistinguishable from this run's, so
            // a reader that dedups by (liquid,element) can silently show
            // last-session's numbers. Stamp every cell with a fresh
            // runId and emit a MatrixAuditRun marker so the audit query
            // is `kind=MatrixAuditRun` (newest) → its runId →
            // `MatrixAudit` filtered to that runId. (Self-auditing
            // playbook Rule 8.)
            string runId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Diag.Record("liquid", "MatrixAuditRun", actor: null, target: null,
                payload: new { runId, baseAmount = BASE, rigSize = rig.Count });
            MessageLog.Add("───── [MatrixAudit] run " + runId + " (base=" + BASE + ") ─────");

            // Rule 4 (loud precondition): if the registry is STILL
            // unavailable (Resources missing), abort the WHOLE audit
            // loudly — never emit phantom ×1.00 rows that read as
            // "no interaction". This is the guard whose absence made
            // the LX.3 pre-registry-init bug invisible.
            if (!LiquidRegistry.IsInitialized || LiquidRegistry.Count == 0)
            {
                MessageLog.Add("[MatrixAudit] ABORT — LiquidRegistry unavailable; "
                    + "matrix NOT run (would be phantom ×1.00).");
                Diag.Record("liquid", "MatrixAuditSkipped", actor: null, target: null,
                    payload: new { runId, reason = "registry_unavailable", scope = "all" });
                return;
            }

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
                        payload: new { runId, liquid = coat, reason = "spawn_failed" });
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
                            runId,
                            liquid = coat,
                            element = elem,
                            baseAmount = BASE,
                            dealt,
                            // Round, don't truncate: (int)(1.9f*100) is
                            // 189 not 190 (float). Mechanic is exact; this
                            // keeps the readout exact too.
                            factorPctOfBase = (int)System.Math.Round(factor * 100.0),
                        });
                }

                // ──── LB.7 audit dimensions (Rule-8 corollary) ────
                // The single-hit matrix can't see tick/light/anchor
                // mechanics. Three per-dummy probes follow it; each is
                // a synthetic-stimulus-then-restore (Rule 2) so the
                // dummy is left in its pre-probe state.
                RunTickAudit(ctx, runId, coat, npc);
                RunLightAudit(runId, coat, npc);
                RunDeathAnchorAudit(ctx, runId, coat, npc);
            }
            MessageLog.Add("───── [MatrixAudit] run " + runId + " complete ─────");
        }

        /// <summary>
        /// TickAudit — synthetic OnTurnStart per coat. Snapshot HP, set
        /// to ~half-Max (so heal has room AND damage doesn't kill),
        /// fire OnTurnStart, measure signed delta (positive = heal,
        /// negative = damage taken), restore HP. Records
        /// <c>liquid/TickAudit</c> per cell. Skips dry control.
        /// </summary>
        private static void RunTickAudit(ScenarioContext ctx, string runId, string coat, Entity npc)
        {
            var fx = npc.GetPart<StatusEffectsPart>();
            var lc = fx?.GetEffect<LiquidCoveredEffect>();
            if (lc == null) return; // dry control
            var hp = npc.GetStat("Hitpoints");
            if (hp == null) return;
            int original = hp.BaseValue;
            int testStart = hp.Max / 2;
            hp.BaseValue = testStart;
            lc.OnTurnStart(npc, GameEvent.New("BeginTakeAction"));
            int delta = hp.BaseValue - testStart; // + heal, − damage
            hp.BaseValue = original; // restore — immortal rig
            string label = delta > 0 ? "heal" : delta < 0 ? "damage" : "none";
            string sign = delta > 0 ? "+" : delta < 0 ? "" : " ";
            MessageLog.Add(string.Format(
                "[TickAudit]   {0,-21} Δ={1}{2,3}  ({3})", coat, sign, delta, label));
            Diag.Record("liquid", "TickAudit", actor: npc, target: null,
                payload: new { runId, liquid = coat, delta, kind = label });
        }

        /// <summary>
        /// LightAudit — read the live <c>LightSourcePart</c> on each
        /// coated dummy. Records radius/color (or 0 if absent). Records
        /// <c>liquid/LightAudit</c>. Skips dry control.
        /// </summary>
        private static void RunLightAudit(string runId, string coat, Entity npc)
        {
            if (npc.GetPart<SpellTestProbePart>()?.CoatLabel == "dry") return;
            var light = npc.GetPart<LightSourcePart>();
            int radius = light?.Radius ?? 0;
            string color = light?.LightColor ?? "";
            MessageLog.Add(string.Format(
                "[LightAudit]  {0,-21} radius={1}  color={2}", coat, radius, color));
            Diag.Record("liquid", "LightAudit", actor: npc, target: null,
                payload: new { runId, liquid = coat, radius, color, present = light != null });
        }

        /// <summary>
        /// DeathAnchorAudit — only when the coat declares
        /// <c>DeathAnchorPercent &gt; 0</c>. Snapshot HP, drop to 1,
        /// apply a massive fatal Damage, observe whether the anchor
        /// fired (HP &gt; 0 post-hit + coat's AnchorConsumed=true).
        /// Re-apply the same coat afterward (the probe consumes the
        /// original, so the bench's other consumers see a coated
        /// dummy). Records <c>liquid/DeathAnchorAudit</c>.
        /// </summary>
        private static void RunDeathAnchorAudit(ScenarioContext ctx, string runId, string coat, Entity npc)
        {
            var fx = npc.GetPart<StatusEffectsPart>();
            var lc = fx?.GetEffect<LiquidCoveredEffect>();
            if (lc == null) return;
            if (!LiquidRegistry.IsInitialized) return;
            var def = LiquidRegistry.Get(lc.LiquidId);
            if (def == null || def.DeathAnchorPercent <= 0) return; // non-anchor liquids: skip
            var hp = npc.GetStat("Hitpoints");
            if (hp == null) return;

            int original = hp.BaseValue;
            string liquidId = lc.LiquidId;
            int amount = lc.Amount;
            hp.BaseValue = 1; // make the next hit lethal
            var dmg = new Damage(999999);
            CombatSystem.ApplyDamage(npc, dmg, source: null, zone: ctx.Zone);
            bool triggered = hp.BaseValue > 1;
            int restoredTo = hp.BaseValue;
            // Re-arm the dummy for downstream visitors: restore HP,
            // REMOVE the consumed coat (otherwise OnStack on the
            // dead-flagged instance would just merge our fresh one into
            // it — AnchorConsumed=true would persist), then re-apply a
            // truly fresh coat. NotRegisteredForTurns dummies never get
            // an EndTurn cleanup so we must do it ourselves.
            hp.BaseValue = original;
            fx.RemoveEffect<LiquidCoveredEffect>();
            npc.ApplyEffect(new LiquidCoveredEffect(liquidId, amount), source: null, zone: ctx.Zone);

            MessageLog.Add(string.Format(
                "[DeathAnchorAudit] {0,-15} triggered={1}  restoredTo={2}",
                coat, triggered, restoredTo));
            Diag.Record("liquid", "DeathAnchorAudit", actor: npc, target: null,
                payload: new { runId, liquid = coat, triggered, restoredTo,
                               percent = def.DeathAnchorPercent });
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
                case "lava/Electric": return "expect ~1.90 (Conductivity 90)";
                case "lava/Heat": return "expect ~1.25 (−25 HeatRes); +8/turn tick is SEPARATE (not in this single-hit cell)";
                case "gel/Electric": return "expect ~2.00 (Conductivity 100)";
                case "sap/Heat": return "expect ~1.35 (Combust 70)";
                case "honey/Heat": return "expect ~1.30 (Combust 60)";
                // LL — only ink/Electric + bog/Heat re-weight a single
                // hit. sundew/choir-wort/lumen are stat/tick liquids:
                // their effect is in liquid/StatModApplied + OnTurnStart,
                // NOT this damage matrix — a 1.00 row there is CORRECT,
                // not "broken" (the all-cells-100 honesty caveat).
                case "iron-gall-ink/Electric": return "expect ~1.60 (Conductivity 60)";
                case "bog-mire/Heat": return "expect ~0.50 (FireDampen 50)";
                case "sundew-mucilage/Heat": return "1.00 by design — slow is −Agi/−DV (StatMod), not a hit re-weight";
                case "choir-wort/Heat": return "1.00 by design — effect is −Tough + Acid tick (StatMod/OnTurnStart)";
                case "lumen-slime/Electric": return "1.00 by design — effect is −DV glow-beacon (StatMod)";
                // LB — buff coats: most have NO single-hit re-weight
                // (tick/light/anchor — see Tick/Light/DeathAnchorAudit).
                case "tepuibone-slurry/Heat":
                case "tepuibone-slurry/Cold":
                case "tepuibone-slurry/Electric":
                case "tepuibone-slurry/Acid":
                    return "expect ~0.75 (+25 to each elemental Resistance)";
                case "convalessence/Heat":
                case "convalessence/Electric":
                case "convalessence/Cold":
                case "convalessence/Acid":
                    return "1.00 by design — heal is OnTurnStart (see TickAudit)";
                case "lantern-beetle-ichor/Heat":
                case "lantern-beetle-ichor/Electric":
                case "lantern-beetle-ichor/Cold":
                case "lantern-beetle-ichor/Acid":
                    return "1.00 by design — effect is attached LightSourcePart (see LightAudit)";
                case "memory-bath/Heat":
                case "memory-bath/Electric":
                case "memory-bath/Cold":
                case "memory-bath/Acid":
                    return "1.00 by design — death-anchor triggers only on lethal hits (see DeathAnchorAudit)";
                case "bower-resin-amber/Heat":
                case "bower-resin-amber/Electric":
                case "bower-resin-amber/Cold":
                case "bower-resin-amber/Acid":
                    return "1.00 in matrix — +DV/+AV apply upstream of synthetic ApplyDamage (LX.3 AV-blind caveat)";
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
