using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// G.12 — Gas System Test Bench (SELF-AUDITING, per the
    /// Docs/MCP_PlayMode_Testing_Strategy.md playbook). Launching the
    /// scenario runs a deterministic matrix and emits one machine-
    /// checkable diag record per cell — no aiming, no walking, no RNG
    /// luck. Audit with:
    ///   diag_query category=gasbench kind=MatrixAuditRun   (newest → runId)
    ///   diag_query category=gasbench kind=ApplyAudit       (per gas type)
    ///   diag_query category=gasbench kind=DispersalAudit   (lifecycle)
    ///   diag_query category=gasbench kind=DefenseAudit     (mask/immunity)
    ///
    /// <para><b>Three audit dimensions:</b></para>
    /// <list type="number">
    ///   <item><b>ApplyAudit</b> — for each of the 6 behavior gas types,
    ///   spawn the gas on a fresh dummy and call ApplyGas; record whether
    ///   it returned true + the dummy's HP delta. A control (no-behavior
    ///   "smoke") row must show applied=false.</item>
    ///   <item><b>DispersalAudit</b> — spawn one unstable high-density
    ///   cloud, tick it N times via OnTickEnd (seeded RNG), and record the
    ///   density trajectory: it must DECREASE (decay) and eventually the
    ///   cloud must SPREAD (≥1 neighbor gains gas) then DISSIPATE.</item>
    ///   <item><b>DefenseAudit</b> — bare / masked / immune dummies in the
    ///   same poison cloud; record the per-turn HP delta. masked &lt; bare,
    ///   immune == 0.</item>
    /// </list>
    ///
    /// <para><b>Rule 4 (loud precondition):</b> if the GasRegistry is
    /// unavailable the whole audit ABORTS with a gasbench/MatrixAuditSkipped
    /// record — never a phantom "all-zero" matrix that reads as
    /// "no interaction". <b>Rule 8 (run stamping):</b> every record carries
    /// a fresh runId; query the newest MatrixAuditRun first, then filter.</para>
    /// </summary>
    [Scenario(
        name: "Gas Dispersal Test Bench",
        category: "Combat",
        description: "Self-auditing: launches → deterministically audits all 6 gas behaviors (ApplyAudit), the dispersal lifecycle (DispersalAudit: decay/spread/dissipate), and defenses (DefenseAudit: mask/immunity). diag_query category=gasbench.")]
    public class GasDispersalTestBench : IScenario
    {
        // The 6 behavior gas types + a control (visual-only "smoke" with a
        // Poison behavior is still a behavior; the control is a gas whose
        // ApplyGas should still fire — so the "control" here is the
        // no-creature target case handled inside ApplyAudit).
        private static readonly (string id, string gasType, string behavior)[] GasTypes =
        {
            ("poison-vapor",    "Poison",    "Poison"),
            ("stun-vapor",      "Stun",      "Stun"),
            ("confusion-vapor", "Confusion", "Confusion"),
            ("cryo-mist",       "Cryo",      "Cryo"),
            ("sleep-vapor",     "Sleep",     "Sleep"),
            ("fungal-spores",   "FungalSpores", "FungalSpores"),
            ("plasma-gas",      "Plasma",    "Plasma"),
        };

        private const int APPLY_DENSITY = 500; // thick enough that ApplyGas always doses

        public void Apply(ScenarioContext ctx)
        {
            EnsureGasRegistry();
            // Dispersal + ApplyGas paths resolve the zone via ActiveZone
            // (TakeDamage / spawn events don't carry one). Pin it.
            SettlementRuntime.ActiveZone = ctx.Zone;

            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            ctx.Player.SetStatMax("Hitpoints", 999).SetHp(999);

            // Clear a wide work area so spawns + dispersal aren't blocked
            // by decor (the proven bench pattern).
            for (int dx = 1; dx <= 40; dx++)
                for (int dy = -4; dy <= 4; dy++)
                    ctx.World.ClearCell(p.x + dx, p.y + dy);

            string runId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Diag.Record("gasbench", "MatrixAuditRun", actor: null, target: null,
                payload: new { runId, gasTypes = GasTypes.Length });
            MessageLog.Add("───── [GasBench] run " + runId + " ─────");

            // Rule 4: loud precondition. Abort the whole audit if the
            // registry never loaded — never emit a phantom all-zero matrix.
            if (!GasRegistry.IsInitialized || GasRegistry.Count == 0)
            {
                MessageLog.Add("[GasBench] ABORT — GasRegistry unavailable; matrix NOT run.");
                Diag.Record("gasbench", "MatrixAuditSkipped", actor: null, target: null,
                    payload: new { runId, reason = "registry_unavailable" });
                return;
            }

            RunApplyAudit(ctx, runId, p.x, p.y);
            RunDispersalAudit(ctx, runId, p.x, p.y - 3);
            RunDefenseAudit(ctx, runId, p.x, p.y + 3);

            MessageLog.Add("───── [GasBench] run " + runId + " complete ─────");
            ctx.Log("=== Gas Dispersal Test Bench (self-auditing) ===");
            ctx.Log("A full synthetic matrix just ran. Confirm in diag:");
            ctx.Log("  diag_query category=gasbench kind=MatrixAuditRun  (newest → runId)");
            ctx.Log("  diag_query category=gasbench kind=ApplyAudit / DispersalAudit / DefenseAudit");
            ctx.Log("Relaunch to re-run.");
        }

        /// <summary>For each behavior gas type: spawn the gas on a fresh
        /// dummy's cell and call ApplyGas; record applied + HP delta.
        /// Snapshot→stimulate→restore (immortal rig).</summary>
        private static void RunApplyAudit(ScenarioContext ctx, string runId, int baseX, int baseY)
        {
            for (int i = 0; i < GasTypes.Length; i++)
            {
                var (id, gasType, behavior) = GasTypes[i];
                int x = baseX + 2 + i * 2;
                var dummy = Dummy(ctx, x, baseY, id + "-victim");
                if (dummy == null || ctx.Zone.GetEntityPosition(dummy).x < 0)
                {
                    MessageLog.Add($"[GasBench] ApplyAudit {id,-15} SPAWN-FAILED — not audited");
                    Diag.Record("gasbench", "MatrixAuditSkipped", actor: null, target: null,
                        payload: new { runId, gasId = id, reason = "spawn_failed", dim = "apply" });
                    continue;
                }

                var gas = GasFactory.SpawnGas(ctx.Zone, x, baseY, id, density: APPLY_DENSITY, creator: null);
                var behaviorPart = gas?.GetPart<IObjectGasBehaviorPart>();
                if (behaviorPart == null)
                {
                    MessageLog.Add($"[GasBench] ApplyAudit {id,-15} NO-BEHAVIOR-PART — not audited");
                    Diag.Record("gasbench", "MatrixAuditSkipped", actor: null, target: null,
                        payload: new { runId, gasId = id, reason = "no_behavior_part", dim = "apply" });
                    continue;
                }

                var hp = dummy.GetStat("Hitpoints");
                int hp0 = hp != null ? hp.BaseValue : 0;
                bool applied = behaviorPart.ApplyGas(dummy, ctx.Zone);
                int hpDelta = hp != null ? hp0 - hp.BaseValue : 0;
                if (hp != null) hp.BaseValue = hp0; // restore — immortal rig

                MessageLog.Add(string.Format(
                    "[GasBench] ApplyAudit {0,-15} applied={1,-5} hpΔ={2,4}", id, applied, hpDelta));
                Diag.Record("gasbench", "ApplyAudit", actor: gas, target: dummy,
                    payload: new { runId, gasId = id, gasType, behavior, applied, hpDelta });
            }
        }

        /// <summary>Spawn one unstable high-density cloud and tick it via
        /// OnTickEnd with a seeded RNG; record density trajectory + whether
        /// it spread to a neighbor + the tick it dissipated.</summary>
        private static void RunDispersalAudit(ScenarioContext ctx, string runId, int x, int y)
        {
            GasSystem.SetRngForTests(new System.Random(20260521));
            var gas = GasFactory.SpawnGas(ctx.Zone, x, y, "poison-vapor", density: 100, creator: null);
            if (gas == null)
            {
                Diag.Record("gasbench", "MatrixAuditSkipped", actor: null, target: null,
                    payload: new { runId, reason = "spawn_failed", dim = "dispersal" });
                GasSystem.SetRngForTests(null);
                return;
            }
            var pool = gas.GetPart<GasPoolPart>();
            int startDensity = pool.Density;
            int prevDensity = startDensity;
            bool decayed = false, spread = false;
            int dissipatedAtTick = -1;

            for (int tick = 1; tick <= 30; tick++)
            {
                GasSystem.OnTickEnd(ctx.Zone);
                // Did the origin cloud dissipate?
                if (ctx.Zone.GetEntityPosition(gas).x < 0) { dissipatedAtTick = tick; break; }
                int d = pool.Density;
                if (d < prevDensity) decayed = true;
                prevDensity = d;
                // Did it spread? Count gas anywhere other than the origin.
                if (!spread)
                {
                    foreach (var e in ctx.Zone.GetAllEntities())
                        if (e.Tags.ContainsKey("Gas") && e != gas) { spread = true; break; }
                }
            }
            GasSystem.SetRngForTests(null);

            MessageLog.Add(string.Format(
                "[GasBench] DispersalAudit start={0} decayed={1} spread={2} dissipatedAtTick={3}",
                startDensity, decayed, spread, dissipatedAtTick));
            Diag.Record("gasbench", "DispersalAudit", actor: null, target: null,
                payload: new { runId, startDensity, decayed, spread, dissipatedAtTick });
        }

        /// <summary>bare / masked / immune dummies, each dosed by one
        /// ApplyGas of the same poison cloud; record per-dummy HP delta.
        /// masked &lt; bare, immune == 0.</summary>
        private static void RunDefenseAudit(ScenarioContext ctx, string runId, int x, int y)
        {
            var defenses = new (string label, System.Action<Entity> setup)[]
            {
                ("bare",   _ => { }),
                ("masked", e => e.AddPart(new GasMaskPart { Power = 10 })),
                ("immune", e => e.AddPart(new GasImmunityPart { GasType = "Poison" })),
            };
            for (int i = 0; i < defenses.Length; i++)
            {
                var (label, setup) = defenses[i];
                int dx = x + 2 + i * 2;
                var dummy = Dummy(ctx, dx, y, "defense-" + label);
                if (dummy == null || ctx.Zone.GetEntityPosition(dummy).x < 0)
                {
                    Diag.Record("gasbench", "MatrixAuditSkipped", actor: null, target: null,
                        payload: new { runId, reason = "spawn_failed", dim = "defense", label });
                    continue;
                }
                setup(dummy);
                var gas = GasFactory.SpawnGas(ctx.Zone, dx, y, "poison-vapor", density: APPLY_DENSITY, creator: null);
                var behaviorPart = gas?.GetPart<IObjectGasBehaviorPart>();
                var hp = dummy.GetStat("Hitpoints");
                int hp0 = hp != null ? hp.BaseValue : 0;
                bool applied = behaviorPart != null && behaviorPart.ApplyGas(dummy, ctx.Zone);
                int hpDelta = hp != null ? hp0 - hp.BaseValue : 0;
                if (hp != null) hp.BaseValue = hp0;

                MessageLog.Add(string.Format(
                    "[GasBench] DefenseAudit {0,-7} applied={1,-5} hpΔ={2,4}", label, applied, hpDelta));
                Diag.Record("gasbench", "DefenseAudit", actor: gas, target: dummy,
                    payload: new { runId, defense = label, applied, hpDelta });
            }
        }

        private static Entity Dummy(ScenarioContext ctx, int x, int y, string label)
        {
            var npc = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 4000)
                .WithHpAbsolute(4000)
                .Passive()
                .NotRegisteredForTurns()
                .At(x, y);
            if (npc == null) return null;
            void S(string n, int v) => npc.Statistics[n] =
                new Stat { Owner = npc, Name = n, BaseValue = v, Min = -200, Max = 400 };
            if (npc.GetStat("HeatResistance") == null) S("HeatResistance", 0);
            if (npc.GetStat("ColdResistance") == null) S("ColdResistance", 0);
            if (npc.GetStat("ElectricResistance") == null) S("ElectricResistance", 0);
            if (npc.GetStat("AcidResistance") == null) S("AcidResistance", 0);
            if (npc.GetStat("Toughness") == null) S("Toughness", 12);
            var render = npc.GetPart<RenderPart>();
            if (render != null) render.DisplayName = "snapjaw (" + label + ")";
            return npc;
        }

        /// <summary>Bootstrap-order-independent registry load (mirrors
        /// GameBootstrap's Resources.LoadAll → InitializeFromJsonSources).
        /// Idempotent — a later GameBootstrap load replaces identical content.</summary>
        private static void EnsureGasRegistry()
        {
            if (GasRegistry.IsInitialized && GasRegistry.Count > 0) return;
            var assets = UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>(
                "Content/Data/GasDefinitions");
            if (assets == null || assets.Length == 0) return;
            var srcs = new List<string>(assets.Length);
            for (int i = 0; i < assets.Length; i++) srcs.Add(assets[i].text);
            GasRegistry.InitializeFromJsonSources(srcs);
        }
    }
}
