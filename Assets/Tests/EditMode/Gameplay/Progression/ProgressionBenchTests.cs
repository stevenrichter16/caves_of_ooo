using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P1 progression bench (Docs/PLACE-WORK-AND-PROGRESSION.md §3, "Measurement").
    /// A deterministic, self-auditing census of a fresh seed-64 world: which creature
    /// tiers each world tier actually spawns, and how much kill XP each tier holds
    /// against the XP a player needs to cross that tier's level band. One diag record
    /// per tier (category=progression, kind=TierAudit), stamped with a run id, and a
    /// plain-text report under Library/ProgressionBench. Explicit: it generates ~560
    /// zones and takes minutes, so it never runs in the ordinary suite.
    ///
    /// Honesty bounds: one seed at generation time. No respawns exist; events, trader
    /// stock and death drops are not counted. The duel table is melee only, with each
    /// build's starting kit, gear rolled from the tier's find tables and the level's
    /// hit points: a floor, not play feel. Only melee builds are asserted.
    /// </summary>
    [Explicit("Generates the whole world; run on demand.")]
    [Category("ProgressionBench")]
    public sealed class ProgressionBenchTests
    {
        const int Seed = 64;
        /// <summary>Planned share of each tier's XP that comes from kills (the rest from place work).</summary>
        const double KillShare = 0.6;

        sealed class TierCell
        {
            public int Zones, Hostiles, Violations, KillXP, ScaledKillXP;
            public readonly Dictionary<int, int> CreatureTiers = new Dictionary<int, int>();
            public readonly Dictionary<string, int> ViolationSamples = new Dictionary<string, int>();
        }

        static EntityFactory LoadWorldFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
            LoadoutPart.Factory = f; LootDropSystem.Factory = f; ContainerPlacementService.Factory = f; TraderPart.Factory = f;
            return f;
        }

        /// <summary>The tier a zone should play at: the authored cell tier on the surface,
        /// one deeper for the first three floors below (design rule, §3 rule 3).</summary>
        static int ZoneTier(int wx, int wy, int wz)
            => System.Math.Min(ProgressionCurve.MaxTier, WorldMapAuthoring.TierAt(wx, wy) + (wz > 0 ? 1 : 0));

        Dictionary<int, TierCell> Census(EntityFactory f, Entity player)
        {
            var m = OverworldZoneManager.CreateDetached(f, Seed);
            var tiers = new Dictionary<int, TierCell>();
            for (int wx = 0; wx < WorldMap.Width; wx++)
                for (int wy = 0; wy < WorldMap.Height; wy++)
                {
                    var depths = new List<int> { 0 };
                    if (m.WorldMap.GetPOI(wx, wy) != null || (wx % 4 == 0 && wy % 4 == 0)) depths.AddRange(new[] { 1, 2, 3 });
                    foreach (int wz in depths)
                    {
                        Zone zone; try { zone = m.GetZone(WorldMap.ToZoneID(wx, wy, wz)); } catch { continue; }
                        if (zone == null) continue;
                        m.SetActiveZone(zone);
                        int tier = ZoneTier(wx, wy, wz);
                        if (!tiers.TryGetValue(tier, out var cell)) tiers[tier] = cell = new TierCell();
                        cell.Zones++;
                        int playerLevel = ProgressionCurve.PlayerLevelBand(tier).Min;
                        foreach (var e in zone.GetAllEntities())
                        {
                            if (!e.HasTag("Creature") || e.HasTag("Player") || !FactionManager.IsHostile(e, player)) continue;
                            int ct = ProgressionCurve.CreatureTier(e);
                            cell.Hostiles++;
                            cell.CreatureTiers[ct] = cell.CreatureTiers.TryGetValue(ct, out var n) ? n + 1 : 1;
                            int xp = e.GetStatValue("XPValue", 0);
                            cell.KillXP += xp;
                            int victimLevel = e.GetStatValue("Level", ProgressionCurve.CreatureLevelBand(ct).Min);
                            cell.ScaledKillXP += ProgressionCurve.ScaleKillXP(xp, playerLevel, victimLevel);
                            if (ct > tier || ct < tier - 1)
                            {
                                cell.Violations++;
                                string k = e.BlueprintName + "(t" + ct + ")";
                                cell.ViolationSamples[k] = cell.ViolationSamples.TryGetValue(k, out var v) ? v + 1 : 1;
                            }
                        }
                    }
                }
            return tiers;
        }

        static string ReportDirectory()
        {
            string dir = Path.Combine(Application.dataPath, "..", "Library", "ProgressionBench");
            Directory.CreateDirectory(dir);
            return dir;
        }

        [Test]
        public void TierAudit_SpawnsAndKillXPMatchTheTierCurve()
        {
            var f = LoadWorldFactory();
            var player = f.CreateEntity("Player");
            string runId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var tiers = Census(f, player);

            var report = new StringBuilder();
            report.AppendLine("PROGRESSION BENCH run " + runId + " seed " + Seed);
            var failures = new List<string>();
            Diag.Record(category: "progression", kind: "MatrixAuditRun", payload: new { runId, seed = Seed });
            foreach (var pair in tiers.OrderBy(p => p.Key))
            {
                int tier = pair.Key; var c = pair.Value;
                var band = ProgressionCurve.PlayerLevelBand(tier);
                int need = ProgressionCurve.CumulativeXPToReach(band.Max) - ProgressionCurve.CumulativeXPToReach(band.Min);
                int killTarget = (int)(need * KillShare);
                string mix = string.Join(" ", c.CreatureTiers.OrderBy(k => k.Key).Select(k => "t" + k.Key + ":" + k.Value));
                string worst = string.Join(" ", c.ViolationSamples.OrderByDescending(k => k.Value).Take(6).Select(k => k.Key + "x" + k.Value));
                report.AppendLine($"TIER {tier} zones={c.Zones} hostiles={c.Hostiles} mix[{mix}] violations={c.Violations} [{worst}] killXP={c.KillXP} scaled@L{band.Min}={c.ScaledKillXP} bandNeed={need} killTarget={killTarget}");
                Diag.Record(category: "progression", kind: "TierAudit",
                    payload: new { runId, tier, zones = c.Zones, hostiles = c.Hostiles, violations = c.Violations, killXP = c.KillXP, scaledKillXP = c.ScaledKillXP, bandNeed = need, killTarget });
                if (c.Violations > 0) failures.Add($"tier {tier}: {c.Violations} hostiles outside tiers {tier - 1}-{tier} ({worst})");
                if (c.ScaledKillXP < killTarget) failures.Add($"tier {tier}: kill XP {c.ScaledKillXP} below the planned {killTarget} of {need}");
            }
            for (int t = 1; t <= ProgressionCurve.MaxTier; t++)
                if (!tiers.ContainsKey(t)) failures.Add($"tier {t}: no zones measured");
            File.WriteAllText(Path.Combine(ReportDirectory(), "tier-audit-" + runId + ".txt"), report.ToString());
            Debug.Log(report.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void DuelTable_EachBuildHoldsItsTierAndStrugglesAboveIt()
        {
            var f = LoadWorldFactory();
            StartingBuildRegistry.ResetForTests();
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Builds/StartingBuilds.json")));
            string runId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var report = new StringBuilder();
            report.AppendLine("DUEL TABLE run " + runId + " (melee only; starting kit plus tier find-table gear; level hit points; non-melee builds reported, not asserted)");
            Diag.Record(category: "progression", kind: "MatrixAuditRun", payload: new { runId, table = "duel" });

            // One representative per creature tier: every spawnable hostile of that tier.
            var byTier = new Dictionary<int, List<string>>();
            var probe = f.CreateEntity("Player");
            foreach (var name in f.Blueprints.Keys.OrderBy(n => n))
            {
                Entity e; try { e = f.CreateEntity(name); } catch { continue; }
                if (e == null || !e.HasTag("Creature") || e.HasTag("Player") || !FactionManager.IsHostile(e, probe)) continue;
                int t = ProgressionCurve.CreatureTier(e);
                if (!byTier.TryGetValue(t, out var list)) byTier[t] = list = new List<string>();
                list.Add(name);
            }

            var failures = new List<string>();
            foreach (var build in StartingBuildRegistry.All)
            {
                for (int tier = 1; tier <= ProgressionCurve.MaxTier; tier++)
                {
                    if (!byTier.TryGetValue(tier, out var foes)) { report.AppendLine($"{build.Id} t{tier}: no creatures of this tier exist"); continue; }
                    int level = ProgressionCurve.PlayerLevelBand(tier).Min;
                    int wins = 0, fights = 0, turns = 0, hpLost = 0;
                    foreach (var foe in foes)
                        for (int s = 0; s < 4; s++)
                        {
                            var r = Duel(f, build, level, foe, 1000 * tier + 17 * s + foe.Length);
                            if (r.skipped) continue;
                            fights++; if (r.won) wins++; turns += r.turns; hpLost += r.hpLost;
                        }
                    if (fights == 0) { report.AppendLine($"{build.Id} t{tier}: skipped"); continue; }
                    int winPct = 100 * wins / fights;
                    report.AppendLine($"{build.Id} L{level} vs t{tier} ({foes.Count} kinds): win {winPct}% turns~{turns / fights} hpLost~{hpLost / fights}  kit[{LastKit}]");
                    Diag.Record(category: "progression", kind: "DuelCell", payload: new { runId, build = build.Id, tier, level, winPct, fights });
                    if (MeleeBuilds.Contains(build.Id) && winPct < 60) failures.Add($"{build.Id} at L{level} wins only {winPct}% against tier {tier}");
                }
            }
            File.WriteAllText(Path.Combine(ReportDirectory(), "duel-table-" + runId + ".txt"), report.ToString());
            Debug.Log(report.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        static int TierOfLevel(int level)
        {
            for (int t = 1; t <= ProgressionCurve.MaxTier; t++) if (level < ProgressionCurve.PlayerLevelBand(t).Max) return t;
            return ProgressionCurve.MaxTier;
        }
        static string[] GearTables(int tier)
            => tier <= 1 ? new string[0]
                : tier <= 3 ? new[] { "FindWeaponT" + tier, "FindArmorT" + tier }
                : new[] { "FindWeaponT3", "FindArmorT3", "FindDeepEquipmentT4" };

        /// <summary>Builds whose plan is melee; the others are reported, not asserted,
        /// because this bench does not drive spells or throwing.</summary>
        static readonly HashSet<string> MeleeBuilds = new HashSet<string> { "duelist", "breaker", "warden", "classic" };

        /// <summary>One seeded melee duel in an empty zone. Energy accrues by Speed;
        /// whoever reaches 1000 acts. Returns skipped when either side cannot be set up.</summary>
        static string LastKit = "";
        static (bool skipped, bool won, int turns, int hpLost) Duel(EntityFactory f, StartingBuildDef build, int level, string foeName, int seed)
        {
            var zone = new Zone("ProgressionBench.Duel");
            var player = f.CreateEntity("Player"); var foe = f.CreateEntity(foeName);
            if (player == null || foe == null) return (true, false, 0, 0);
            if (!StartingBuildService.Apply(player, f, build).Success) return (true, false, 0, 0);
            var hp = player.GetStat("Hitpoints"); var levelStat = player.GetStat("Level");
            if (hp == null || levelStat == null) return (true, false, 0, 0);
            levelStat.BaseValue = level; hp.Max += 2 * (level - 1); hp.BaseValue = hp.Max;
            // Gear found at this tier, from the game's own find tables, so a level-9
            // fighter is not measured in a starting kit.
            var gearRng = new System.Random(seed ^ 0x5eed);
            foreach (string table in GearTables(TierOfLevel(level)))
                foreach (string bp in LootTableRegistry.Roll(table, gearRng))
                {
                    var item = f.CreateEntity(bp);
                    if (item == null || (!item.HasPart<MeleeWeaponPart>() && !item.HasPart<ArmorPart>())) continue;
                    var inventory = player.GetPart<InventoryPart>();
                    if (inventory == null || !inventory.AddObject(item)) continue;
                    // A found weapon replaces the starting one; auto-equip never swaps a held weapon.
                    // If the find cannot be held (a two-hander beside a buckler), keep the old weapon.
                    var heldWeapons = item.HasPart<MeleeWeaponPart>()
                        ? inventory.GetAllEquipped().Where(x => x.HasPart<MeleeWeaponPart>()).ToList() : new List<Entity>();
                    foreach (var held in heldWeapons) InventorySystem.UnequipItem(player, held);
                    if (!InventorySystem.AutoEquip(player, item))
                        foreach (var held in heldWeapons) InventorySystem.AutoEquip(player, held);
                }
            if (!zone.AddEntity(player, 10, 10) || !zone.AddEntity(foe, 11, 10)) return (true, false, 0, 0);
            LastKit = string.Join(",", player.GetPart<InventoryPart>().GetAllEquipped().Select(x => x.BlueprintName))
                + " AV" + CombatSystem.GetAV(player) + " DV" + CombatSystem.GetDV(player) + " HP" + hp.Max;
            var rng = new System.Random(seed);
            int startHp = hp.Value, pe = 0, fe = 0, rounds = 0;
            int ps = System.Math.Max(1, player.GetStatValue("Speed", 100)), fs = System.Math.Max(1, foe.GetStatValue("Speed", 100));
            while (rounds < 400 && player.GetStatValue("Hitpoints", 0) > 0 && foe.GetStatValue("Hitpoints", 0) > 0)
            {
                pe += ps; fe += fs;
                if (pe >= 1000) { pe -= 1000; rounds++; CombatSystem.PerformMeleeAttack(player, foe, zone, rng); }
                if (foe.GetStatValue("Hitpoints", 0) > 0 && fe >= 1000) { fe -= 1000; CombatSystem.PerformMeleeAttack(foe, player, zone, rng); }
            }
            bool won = foe.GetStatValue("Hitpoints", 0) <= 0 && player.GetStatValue("Hitpoints", 0) > 0;
            return (false, won, rounds, startHp - System.Math.Max(0, player.GetStatValue("Hitpoints", 0)));
        }
    }
}
