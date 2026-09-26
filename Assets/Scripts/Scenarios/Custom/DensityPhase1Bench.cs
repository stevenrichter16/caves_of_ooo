using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite numeric audit on detached generated zones and real factory
    /// creatures. Does not stage, teleport, or modify the live player's zone.
    /// The isolated editor launcher adds native travel and a lair screenshot.</summary>
    [Scenario(name: "Density Phase 1 Audit", category: "World",
        description: "Audit natural dodge, stun controls, Spread encounters and real Beating lairs.")]
    public sealed class DensityPhase1Bench : IScenario
    {
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();

        public void Apply(ScenarioContext ctx)
        {
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            bool oldScenario = Diag.IsChannelEnabled("scenario");
            var oldFactory = ContainerPlacementService.Factory;
            Diag.SetChannel("scenario", true);
            Diag.Record("scenario", "DensityPhase1Run", payload: new { runId = RunId });
            try
            {
                Require(ctx != null && ctx.Factory != null, "live context and factory");
                foreach (string blueprint in new[] { "Viper", "SunStriker", "DesertProwler", "AmbushBandit", "SpikeTrap" })
                    Require(ctx.Factory.Blueprints.ContainsKey(blueprint), "missing blueprint " + blueprint);
                ContainerPlacementService.Factory = ctx.Factory;
                AuditDodge(ctx.Factory, "Viper", 5);
                AuditDodge(ctx.Factory, "SunStriker", 6);
                AuditPopulation();
                AuditLairs(ctx.Factory);
            }
            finally
            {
                Diag.Record("scenario", "DensityPhase1Summary", payload: new
                    { runId = RunId, cases = Cases, failures = Failures, complete = Cases >= 18 && Failures == 0 });
                ContainerPlacementService.Factory = oldFactory;
                Diag.SetChannel("scenario", oldScenario);
            }
        }

        private void AuditDodge(EntityFactory factory, string blueprint, int expectedNatural)
        {
            var actor = factory.CreateEntity(blueprint);
            var control = factory.CreateEntity(blueprint);
            Require(actor != null && control != null && actor.HasPart<Body>() && actor.GetStat("DV") != null,
                blueprint + " real body and DV stat");
            var armor = actor.GetPart<ArmorPart>();
            Require(armor != null, blueprint + " natural armor");
            int natural = armor.DV;
            int before = CombatSystem.GetDV(actor);
            int controlBefore = CombatSystem.GetDV(control);
            int expected = 6 + StatUtils.GetModifier(actor, "Agility") + actor.GetStatValue("DV") + expectedNatural;
            Measure(blueprint + "_natural_dodge", before, expected, before == expected && natural == expectedNatural);
            try
            {
                armor.DV = 0;
                Measure(blueprint + "_natural_removed_control", CombatSystem.GetDV(actor), before - natural,
                    CombatSystem.GetDV(actor) == before - natural);
            }
            finally { armor.DV = natural; }
            var stun = new StunnedEffect(2);
            try
            {
                Require(actor.ApplyEffect(stun), blueprint + " stun applies through the normal lazy effect facade");
                int after = CombatSystem.GetDV(actor);
                Measure(blueprint + "_stun_delta", after - before, -4, after - before == -4);
                Measure(blueprint + "_untreated_control", CombatSystem.GetDV(control), controlBefore,
                    CombatSystem.GetDV(control) == controlBefore);
            }
            finally { actor.GetPart<StatusEffectsPart>()?.RemoveEffect(stun); }
            Measure(blueprint + "_restored", CombatSystem.GetDV(actor), before, CombatSystem.GetDV(actor) == before);
        }

        private void AuditPopulation()
        {
            var table = PopulationTable.SpreadTier1();
            var control = PopulationTable.SpreadTier1();
            control.Entries.RemoveAll(e => e.BlueprintName == "Viper" || e.BlueprintName == "Snapjaw");
            int valid = 0, controls = 0;
            var species = new HashSet<string>();
            for (int seed = 1; seed <= 64; seed++)
            {
                var hostiles = table.Roll(new Random(seed)).Where(IsSpreadEncounter).ToList();
                if (hostiles.Count >= 1 && hostiles.Count <= 2 && hostiles.Distinct().Count() == 1) valid++;
                foreach (string blueprint in hostiles) species.Add(blueprint);
                if (!control.Roll(new Random(seed)).Any(IsSpreadEncounter)) controls++;
            }
            Measure("spread_one_group_per_roll", valid, 64, valid == 64);
            Measure("spread_both_group_choices", species.Count, 2, species.Count == 2);
            Measure("spread_removed_rows_control", controls, 64, controls == 64);
        }
        private static bool IsSpreadEncounter(string blueprint) => blueprint == "Viper" || blueprint == "Snapjaw";

        private void AuditLairs(EntityFactory factory)
        {
            int generated = 0, bosses = 0, trapZones = 0, safeTrapZones = 0, ambushers = 0, withoutAmbusher = 0;
            for (int seed = 1; seed <= 100 && (ambushers == 0 || withoutAmbusher == 0); seed++)
            {
                var manager = OverworldZoneManager.CreateDetached(factory, seed);
                for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
                {
                    if (manager.WorldMap.GetBiome(x, y) != BiomeType.Beating
                        || manager.WorldMap.GetPOI(x, y)?.Type != POIType.Lair) continue;
                    var zone = manager.GetZone($"Overworld.{x}.{y}.0");
                    Require(zone != null, "native generated Beating lair");
                    generated++;
                    var entities = zone.GetAllEntities();
                    if (entities.Count(e => e.BlueprintName == "DesertProwler") == 1) bosses++;
                    int count = entities.Count(e => e.BlueprintName == "AmbushBandit");
                    if (count > 0) ambushers++; else withoutAmbusher++;
                    var traps = entities.Where(e => e.HasTag("Trap")).ToList();
                    if (traps.Count >= 1 && traps.Count <= 2) trapZones++;
                    if (traps.Count > 0 && traps.All(e =>
                    {
                        var cell = zone.GetEntityCell(e);
                        return cell != null && !zone.GenReservedCells.Contains((cell.X, cell.Y))
                            && !cell.Objects.Any(o => o != e && (o.HasTag("Creature") || o.HasTag("Trap") || o.HasPart<ContainerPart>()));
                    })) safeTrapZones++;
                }
            }
            Require(generated > 0, "bounded seed sweep finds Beating lairs");
            Measure("lair_desert_prowler", bosses, generated, bosses == generated);
            Measure("lair_sparse_traps", trapZones, generated, trapZones == generated);
            Measure("lair_unoccupied_traps", safeTrapZones, generated, safeTrapZones == generated);
            Measure("lair_ambusher_observed", ambushers, 1, ambushers >= 1);
            Measure("lair_no_ambusher_control", withoutAmbusher, 1, withoutAmbusher >= 1);
        }

        private void Measure(string name, int actual, int expected, bool passed)
        {
            Cases++; if (!passed) Failures++;
            Audit.Add((passed ? "PASS " : "FAIL ") + name + " actual=" + actual + " expected=" + expected);
            Diag.Record("scenario", "DensityPhase1Case", payload: new { runId = RunId, name, actual, expected, passed });
        }
        private void Require(bool condition, string reason)
        {
            if (condition) return;
            Failures++;
            Diag.Record("scenario", "DensityPhase1Skipped", payload: new { runId = RunId, reason, passed = false });
            throw new InvalidOperationException("Density audit precondition failed: " + reason);
        }
    }
}
