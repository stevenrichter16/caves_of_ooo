using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Skills;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    // Throwaway measurement probe (NOT committed): real CombatSystem, real
    // blueprints, per-build melee exchange rates against first-hour enemies.
    public class BuildProbeTests
    {
        class Build
        {
            public string Name; public int Str, Agi, Tou, Ego;
            public string Weapon; public string[] Gear; public string[] Skills;
        }

        static readonly Build[] Builds =
        {
            new Build { Name = "Today fist", Str = 18, Agi = 18, Tou = 18, Ego = 16, Weapon = null, Gear = new string[0], Skills = new string[0] },
            new Build { Name = "Today dagger-equipped", Str = 18, Agi = 18, Tou = 18, Ego = 16, Weapon = "Dagger", Gear = new string[0], Skills = new string[0] },
            new Build { Name = "A Duelist dagger Str16 Agi22", Str = 16, Agi = 22, Tou = 14, Ego = 18, Weapon = "Dagger", Gear = new[] { "Buckler", "Cloak" }, Skills = new[] { "ShortBladesSkill", "ShortBlades_Bloodletter", "ShortBlades_Puncture", "ShortBlades_Rejoinder", "ShortBlades_Shank", "ShortBlades_Flurry" } },
            new Build { Name = "A Duelist spear Str16 Agi22", Str = 16, Agi = 22, Tou = 14, Ego = 18, Weapon = "Spear", Gear = new[] { "Buckler", "Cloak" }, Skills = new[] { "ShortBladesSkill", "ShortBlades_Bloodletter", "ShortBlades_Puncture", "ShortBlades_Rejoinder", "ShortBlades_Shank", "ShortBlades_Flurry" } },
            new Build { Name = "B cudgel Str22 (armored)", Str = 22, Agi = 16, Tou = 16, Ego = 16, Weapon = "Cudgel", Gear = new[] { "LeatherArmor", "Buckler" }, Skills = new[] { "CudgelSkill", "Cudgel_Expertise", "Cudgel_Conk", "Cudgel_Slam", "Cudgel_GroundPound", "Cudgel_ShatteringBlows" } },
            new Build { Name = "B cudgel Str20 (armored)", Str = 20, Agi = 16, Tou = 16, Ego = 18, Weapon = "Cudgel", Gear = new[] { "LeatherArmor", "Buckler" }, Skills = new[] { "CudgelSkill", "Cudgel_Expertise", "Cudgel_Conk", "Cudgel_Slam", "Cudgel_GroundPound", "Cudgel_ShatteringBlows" } },
            new Build { Name = "B cudgel Str20 cloak+buckler", Str = 20, Agi = 16, Tou = 16, Ego = 18, Weapon = "Cudgel", Gear = new[] { "Cloak", "Buckler" }, Skills = new[] { "CudgelSkill", "Cudgel_Expertise", "Cudgel_Conk", "Cudgel_Slam", "Cudgel_GroundPound", "Cudgel_ShatteringBlows" } },
            new Build { Name = "B hatchet Str20 cloak+buckler", Str = 20, Agi = 16, Tou = 16, Ego = 18, Weapon = "Hatchet", Gear = new[] { "Cloak", "Buckler" }, Skills = new[] { "AxeSkill", "Axe_Expertise", "Axe_Cleave", "Axe_Whirlwind", "Axe_RendArmor", "Axe_Berserk" } },
            new Build { Name = "B hatchet Str22 cloak+buckler", Str = 22, Agi = 16, Tou = 16, Ego = 16, Weapon = "Hatchet", Gear = new[] { "Cloak", "Buckler" }, Skills = new[] { "AxeSkill", "Axe_Expertise", "Axe_Cleave", "Axe_Whirlwind", "Axe_RendArmor", "Axe_Berserk" } },
            new Build { Name = "B mace Str20 cloak+buckler", Str = 20, Agi = 16, Tou = 16, Ego = 18, Weapon = "Mace", Gear = new[] { "Cloak", "Buckler" }, Skills = new[] { "CudgelSkill", "Cudgel_Expertise", "Cudgel_Conk", "Cudgel_Slam", "Cudgel_GroundPound", "Cudgel_ShatteringBlows" } },
            new Build { Name = "C caster dagger Str16 Agi20", Str = 16, Agi = 20, Tou = 16, Ego = 18, Weapon = "Dagger", Gear = new[] { "Cloak" }, Skills = new string[0] },
            new Build { Name = "D bomber dagger Str16 Agi18", Str = 16, Agi = 18, Tou = 14, Ego = 22, Weapon = "Dagger", Gear = new[] { "LeatherCap", "Cloak" }, Skills = new string[0] },
        };

        static readonly string[] Enemies = { "MarlbackScrabbler", "MarlbackGleaner", "Viper", "GiantSpider", "JungleApe" };

        static ScenarioTestHarness _h;
        [OneTimeSetUp] public void Up() => _h = new ScenarioTestHarness();
        [OneTimeTearDown] public void Down() { _h?.Dispose(); _h = null; }

        static void SetStat(Entity e, string n, int v) { var s = e.GetStat(n); s.BaseValue = v; }

        static Entity MakePlayer(ScenarioContext ctx, Build b)
        {
            var p = ctx.PlayerEntity;
            SetStat(p, "Strength", b.Str); SetStat(p, "Agility", b.Agi); SetStat(p, "Toughness", b.Tou); SetStat(p, "Ego", b.Ego);
            var inv = p.GetPart<InventoryPart>();
            if (b.Weapon != null)
            {
                var w = _h.Factory.CreateEntity(b.Weapon); inv.AddObject(w); Assert.IsTrue(InventorySystem.Equip(p, w), "equip " + b.Weapon);
            }
            foreach (var g in b.Gear)
            {
                var it = _h.Factory.CreateEntity(g); inv.AddObject(it); Assert.IsTrue(InventorySystem.Equip(p, it), "equip " + g);
            }
            var sk = p.GetPart<SkillsPart>();
            foreach (var s in b.Skills) sk.AddSkill(s, "probe");
            return p;
        }

        static void Fat(Entity e) { var s = e.GetStat("Hitpoints"); s.Max = 100000; s.BaseValue = 100000; }

        [Test]
        public void MeasureBuilds()
        {
            const int Reps = 40, Swings = 25;
            var sb = new StringBuilder();
            sb.AppendLine("build|dv|av(body)|enemy|player_hit%|player_dmg/swing|enemy_hit%|enemy_dmg/swing");
            foreach (var b in Builds)
            {
                foreach (var en in Enemies)
                {
                    int pHits = 0, pSw = 0, eHits = 0, eSw = 0; long pDmg = 0, eDmg = 0; int dv = 0;
                    for (int r = 0; r < Reps; r++)
                    {
                        var ctx = _h.CreateContext(rngSeed: 1000 + r, playerBlueprint: "Player");
                        var p = MakePlayer(ctx, b); Fat(p);
                        dv = CombatSystem.GetDV(p);
                        var foe = ctx.Spawn(en).NotRegisteredForTurns().At(41, 12); Fat(foe);
                        var rng = new Random(7000 + r);
                        for (int i = 0; i < Swings; i++)
                        {
                            int before = foe.GetStatValue("Hitpoints", 0);
                            CombatSystem.PerformMeleeAttack(p, foe, ctx.Zone, rng);
                            int d = before - foe.GetStatValue("Hitpoints", 0); pSw++; if (d > 0) pHits++; pDmg += d; Fat(foe);
                            before = p.GetStatValue("Hitpoints", 0);
                            CombatSystem.PerformMeleeAttack(foe, p, ctx.Zone, rng);
                            d = before - p.GetStatValue("Hitpoints", 0); eSw++; if (d > 0) eHits++; eDmg += d; Fat(p);
                        }
                    }
                    sb.AppendLine($"{b.Name}|{dv}|-|{en}|{100.0 * pHits / pSw:F0}|{(double)pDmg / pSw:F2}|{100.0 * eHits / eSw:F0}|{(double)eDmg / eSw:F2}");
                }
            }
            File.WriteAllText(Environment.GetEnvironmentVariable("PROBE_OUT") ?? "probe.txt", sb.ToString());
        }
    }
}
