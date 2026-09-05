using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>CoO-original ecology audit. Staging is synthetic; all AI
    /// stimuli use the live energy scheduler and ordinary movement. Runtime
    /// profiling continues through keyboard waits in StumpSummitBenchPlayer.</summary>
    [Scenario(name: "Stump Summit and Sima Audit", category: "World",
        description: "Fourteen deterministic ecology/combat/travel controls, then 75 seconds of native gameplay profiling.")]
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

            // W6.7 close-out: exercise the authored first-wave attacks through
            // real body combat after maintenance, where humanoid fists hid them.
            bool damageChannel = Diag.IsChannelEnabled("damage"); Diag.SetChannel("damage", true);
            try
            {
                foreach (var row in new[] { ("SariSnake", "1d6", true), ("Wardline", "1d4", false) })
                {
                    var snake = Place(ctx, row.Item1, 5, 5); snake.GetPart<Body>().UpdateBodyParts();
                    snake.GetPart<MeleeWeaponPart>().HitBonus = 100; snake.GetPart<MeleeWeaponPart>().PenBonus = 20;
                    var target = new Entity(); target.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100000, Max = 100000 };
                    ctx.Zone.AddEntity(target, 6, 5);
                    for (int seed = 0; seed < 30; seed++) CombatSystem.PerformMeleeAttack(snake, target, ctx.Zone, new System.Random(seed));
                    var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "DamageRoll", Actor = snake.ID, Limit = 100 }).Records;
                    Check(row.Item1 + "_authored_strike", records.Count > 0
                        && records.All(r => r.PayloadJson.Contains("\"damageDice\":\"" + row.Item2 + "\""))
                        && target.HasEffect<BleedingEffect>() == row.Item3);
                    ctx.Zone.RemoveEntity(snake); ctx.Zone.RemoveEntity(target);
                }
            }
            finally { Diag.SetChannel("damage", damageChannel); }
            foreach (bool spray in new[] { true, false })
            {
                var arena = new Zone("CascadeHabitatAudit");
                arena.AddEntity(ctx.Factory.CreateEntity(spray ? "SprayPool" : "WaterPuddle"), 10, 10);
                var table = new PopulationTable { Name = "CascadeHabitatAudit" };
                table.Entries.Add(new PopulationEntry { BlueprintName = "CascadeFather", MinCount = 1, MaxCount = 1, Weight = 1 });
                new PopulationBuilder(table) { HabitatFilter = StumpFaunaHabitat.Allows }.BuildZone(arena, ctx.Factory, new System.Random(67));
                var frogs = arena.GetAllEntities().Where(e => e.BlueprintName == "CascadeFather").ToList();
                Check(spray ? "cascade_in_spray" : "standing_water_excluded", spray
                    ? frogs.Count == 1 && arena.GetEntityPosition(frogs[0]) == (10, 10) : frogs.Count == 0);
            }
            foreach (var site in new[] { (2, 4), (4, 6) })
            {
                var manager = new OverworldZoneManager(ctx.Factory, 67);
                var lower = manager.GetZone($"Overworld.{site.Item1}.{site.Item2}.3");
                var upper = manager.GetZone($"Overworld.{site.Item1}.{site.Item2}.2");
                // Isolate the authored route from randomly occupying creatures.
                foreach (var z in new[] { lower, upper })
                    foreach (var e in z.GetEntitiesWithTag("Creature").ToArray()) z.RemoveEntity(e);
                var up = lower.GetAllEntities().Single(e => e.HasPart<StairsUpPart>());
                var down = upper.GetAllEntities().Single(e => e.HasPart<StairsDownPart>());
                var pos = upper.GetEntityPosition(down); var visitor = new Entity(); upper.AddEntity(visitor, pos.x, pos.y);
                var descent = ZoneTransitionSystem.TransitionPlayerVertical(visitor, upper, true, pos.x, pos.y, manager);
                bool returned = false;
                if (descent.Success)
                    returned = ZoneTransitionSystem.TransitionPlayerVertical(visitor, lower, false,
                        descent.NewPlayerX, descent.NewPlayerY, manager).Success;
                Check("deep_first_round_trip_" + site.Item1, descent.Success && returned && upper.GetEntityCell(visitor) != null);
                lower.RemoveEntity(up); upper.RemoveEntity(visitor); upper.AddEntity(visitor, pos.x, pos.y);
                var refused = ZoneTransitionSystem.TransitionPlayerVertical(visitor, upper, true, pos.x, pos.y, manager);
                Check("removed_return_refused_" + site.Item1, !refused.Success
                    && upper.GetEntityCell(visitor) != null && lower.GetEntityCell(visitor) == null);
            }

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
