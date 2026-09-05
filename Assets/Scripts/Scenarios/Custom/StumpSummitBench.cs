using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>CoO-original ecology audit. Staging is synthetic; all AI
    /// stimuli use the live energy scheduler and ordinary movement. Runtime
    /// profiling continues through keyboard waits in StumpSummitBenchPlayer.</summary>
    [Scenario(name: "Stump Summit and Sima Audit", category: "World",
        description: "Six deterministic ecology controls, then 75 seconds of native gameplay profiling.")]
    public sealed class StumpSummitBench : IScenario
    {
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public string RunId { get; private set; }
        public readonly List<string> Audit = new List<string>();

        public void Apply(ScenarioContext ctx)
        {
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            Diag.SetChannel("scenario", true);
            foreach (string bp in new[] { "BrocchiniaSentinel", "TankBrocchinia", "PricklebrowNest", "PrickleBrowGecko", "StoneFloor" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp))
                    throw new InvalidOperationException("Stump audit missing blueprint: " + bp);
            // This is a disposable scenario arena. Do not invoke on a saved
            // expedition: it intentionally clears its zone and old actors.
            foreach (var e in ctx.Zone.GetAllEntities().ToArray())
                if (e != ctx.PlayerEntity) { ctx.Turns.RemoveEntity(e); ctx.Zone.RemoveEntity(e); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 1; x < Zone.Width - 1; x++) for (int y = 1; y < Zone.Height - 1; y++)
                ctx.Zone.AddEntity(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            ctx.Zone.RemoveEntity(ctx.PlayerEntity); ctx.Zone.AddEntity(ctx.PlayerEntity, 20, 12);
            var hp = ctx.PlayerEntity.GetStat("Hitpoints"); hp.Max = hp.BaseValue = 1000000; hp.Penalty = 0;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn();

            Entity subject = Spawn(ctx, "BrocchiniaSentinel", 23, 12);
            Place(ctx, "TankBrocchinia", 24, 12);
            Entity quiet = Spawn(ctx, "BrocchiniaSentinel", 45, 12);
            Place(ctx, "TankBrocchinia", 46, 12);
            Entity blocked = Spawn(ctx, "BrocchiniaSentinel", 20, 9);
            Place(ctx, "TankBrocchinia", 20, 8);
            var obstruction = new Entity(); obstruction.AddPart(new PhysicsPart { Solid = true });
            ctx.Zone.AddEntity(obstruction, 20, 8);
            Wait(ctx, 2);
            Check("approached_retreat", ctx.Zone.GetEntityPosition(subject) == (24, 12));
            Check("quiet_control", ctx.Zone.GetEntityPosition(quiet) == (45, 12));
            Check("occupied_cover_control", ctx.Zone.GetEntityPosition(blocked) == (20, 9));
            Wait(ctx, 2);
            Check("sheltered_stays", ctx.Zone.GetEntityPosition(subject) == (24, 12));

            var previousFactory = PricklebrowNestPart.Factory;
            PricklebrowNestPart.Factory = ctx.Factory;
            try
            {
                var nest = Place(ctx, "PricklebrowNest", 21, 12);
                Require(MovementSystem.TryMove(ctx.PlayerEntity, ctx.Zone, 1, 0), "nest approach");
                var defenders = ctx.Zone.GetEntitiesWithTag("Creature").Where(e => e.BlueprintName == "PrickleBrowGecko").ToList();
                Check("nest_sixteen_scheduled", defenders.Count == 16
                    && defenders.Select(e => ctx.Zone.GetEntityPosition(e)).Distinct().Count() == 16
                    && defenders.All(e => ctx.Turns.IsRegistered(e) && e.GetPart<BrainPart>().IsPersonallyHostileTo(ctx.PlayerEntity)));
                var positions = defenders.ToDictionary(e => e.ID, e => ctx.Zone.GetEntityPosition(e));
                // Re-enter while the immediate ring is still open; failure is
                // a loud precondition failure, never a passing neutral result.
                bool reentered = MovementSystem.TryMove(ctx.PlayerEntity, ctx.Zone, -1, 0)
                    && MovementSystem.TryMove(ctx.PlayerEntity, ctx.Zone, 1, 0);
                Wait(ctx, 2);
                bool acted = defenders.Any(e => ctx.Zone.GetEntityPosition(e) != positions[e.ID]);
                Check("defenders_act_without_respawn", acted && reentered
                    && ctx.Zone.GetEntitiesWithTag("Creature").Count(e => e.BlueprintName == "PrickleBrowGecko") == 16
                    && nest.GetPart<PricklebrowNestPart>().Triggered);
            }
            finally { PricklebrowNestPart.Factory = previousFactory; }
            ZoneRenderHooks.MarkFullDirty("StumpSummitBench");
            if (Application.isPlaying)
                new GameObject("Stump Summit Audit").AddComponent<StumpSummitBenchPlayer>().Initialize(ctx, this);
        }

        private static Entity Place(ScenarioContext ctx, string bp, int x, int y)
        {
            var e = ctx.Factory.CreateEntity(bp);
            Require(e != null, bp); ctx.Zone.AddEntity(e, x, y); return e;
        }
        private static Entity Spawn(ScenarioContext ctx, string bp, int x, int y)
        {
            var e = Place(ctx, bp, x, y); var brain = e.GetPart<BrainPart>();
            Require(brain != null, bp + " brain");
            brain.CurrentZone = ctx.Zone; brain.Rng = new System.Random(63);
            ctx.Turns.AddEntity(e); return e;
        }
        private static void Wait(ScenarioContext ctx, int count)
        {
            for (int i = 0; i < count; i++)
            { ctx.Turns.EndTurn(ctx.PlayerEntity, ctx.Zone); ctx.Turns.ProcessUntilPlayerTurn(); }
        }
        private void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++;
            string row = name + ":" + (passed ? "PASS" : "FAIL"); Audit.Add(row);
            Diag.Record("scenario", "StumpSummitAudit", payload: new { runId = RunId, name, passed });
        }
        private static void Require(bool value, string condition)
        { if (!value) throw new InvalidOperationException("Stump audit precondition failed: " + condition); }
    }
}
