using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Skills;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    // Throwaway self-auditing probe (NOT committed). Real TurnManager, real
    // enemy AI, real CombatSystem, real skills. Scripted PLAYER policy per
    // build. No healing is used. Gas clouds are NOT modeled (World tick is
    // not driven here), so grenades are left out of the policies.
    public class BuildFightSim
    {
        class Build
        {
            public string Name, Policy; public int Str, Agi, Tou, Ego;
            public string Weapon; public string[] Gear = new string[0]; public string[] Carry = new string[0];
            public string[] Skills = new string[0]; public bool StartKit;
        }

        static Build[] Builds()
        {
            var SB = new[] { "ShortBladesSkill", "ShortBlades_Puncture", "ShortBlades_Bloodletter", "ShortBlades_Rejoinder", "ShortBlades_Shank", "ShortBlades_Flurry" };
            var AX = new[] { "AxeSkill", "Axe_Expertise", "Axe_Cleave", "Axe_Whirlwind", "Axe_Berserk", "Axe_RendArmor" };
            var CU = new[] { "CudgelSkill", "Cudgel_Expertise", "Cudgel_Conk", "Cudgel_Slam", "Cudgel_GroundPound", "Cudgel_ShatteringBlows" };
            var CA = new[] { "Pyromancy_Kindle", "Galvanism_ArcBolt", "Hydromancy_JetBlast", "Galvanism_GroundSurge", "SpellcraftSkill", "Spellcraft_Empower" };
            var CB = new[] { "Pyromancy_Kindle", "Galvanism_ArcBolt", "Hydromancy_JetBlast", "Galvanism_GroundSurge", "Spellcraft_Calm", "SpellcraftSkill" };
            var CC = new[] { "Pyromancy_Kindle", "Galvanism_ArcBolt", "Hydromancy_JetBlast", "Galvanism_GroundSurge" };
            var BO = new[] { "Hydromancy_DrenchLob", "Pyromancy_Oilmark", "Cryomancy_ColdSnap", "Spellcraft_Calm" };
            string[] kit5 = { "PoisonGasGrenade", "StunGasGrenade", "LightningTonic", "FrostTonic", "FireTonic" };
            string[] kit3 = { "PoisonGasGrenade", "LightningTonic", "FrostTonic" };
            var CU2 = new[] { "CudgelSkill", "Cudgel_Expertise", "Cudgel_Conk", "Cudgel_Slam", "Cudgel_GroundPound", "Cudgel_ShatteringBlows" };
            return new[]
            {
                new Build { Name = "Today (dagger equipped)", Policy = "today", Str = 18, Agi = 18, Tou = 18, Ego = 16, Weapon = "Dagger", StartKit = true },
                new Build { Name = "A Duelist",    Policy = "duelist",  Str = 16, Agi = 22, Tou = 14, Ego = 18, Weapon = "Dagger", Gear = new[] { "Buckler", "Cloak" }, Skills = SB },
                new Build { Name = "B4 Cudgel Str20 buckler only", Policy = "cudgel", Str = 20, Agi = 16, Tou = 16, Ego = 18, Weapon = "Cudgel", Gear = new[] { "Buckler" }, Skills = CU2 },
                new Build { Name = "B5 Cudgel Str18 Agi18 buckler", Policy = "cudgel", Str = 18, Agi = 18, Tou = 14, Ego = 20, Weapon = "Cudgel", Gear = new[] { "Buckler" }, Skills = CU2 },
                new Build { Name = "C2 Storm +Calm+Spellcraft", Policy = "caster", Str = 16, Agi = 20, Tou = 16, Ego = 18, Weapon = "Dagger", Gear = new[] { "Cloak" }, Skills = CB },
                new Build { Name = "D2 Bombardier 3 throwables", Policy = "bomber", Str = 16, Agi = 18, Tou = 14, Ego = 22, Weapon = "Dagger", Gear = new[] { "LeatherCap", "Cloak" }, Carry = kit3, Skills = BO },
                new Build { Name = "D3 Bombardier 2 throwables (Poison+Frost)", Policy = "bomber", Str = 16, Agi = 18, Tou = 14, Ego = 22, Weapon = "Dagger", Gear = new[] { "LeatherCap", "Cloak" }, Carry = new[] { "PoisonGasGrenade", "FrostTonic" }, Skills = BO },
                new Build { Name = "D4 Bombardier tonics only (no gas)", Policy = "bomber", Str = 16, Agi = 18, Tou = 14, Ego = 22, Weapon = "Dagger", Gear = new[] { "LeatherCap", "Cloak" }, Carry = new[] { "LightningTonic", "FrostTonic", "FireTonic" }, Skills = BO },
            };
        }

        static readonly Dictionary<string, string[]> Scenarios = new Dictionary<string, string[]>
        {
            { "S1 glade: 2 Scrabbler + Gleaner", new[] { "MarlbackScrabbler", "MarlbackScrabbler", "MarlbackGleaner" } },
            { "S2 lair: 2 GiantSpider + JungleApe", new[] { "GiantSpider", "GiantSpider", "JungleApe" } },
            { "S3 one JungleApe", new[] { "JungleApe" } },
            { "S4 Sodden: MawToad + 2 Bandfrog", new[] { "MawToad", "Bandfrog", "Bandfrog" } },
            { "S5 ambush: 2 Scrabbler + Gleaner at 2 cells", new[] { "MarlbackScrabbler", "MarlbackScrabbler", "MarlbackGleaner" } },
            { "S6 ambush: 2 GiantSpider + JungleApe at 2 cells", new[] { "GiantSpider", "GiantSpider", "JungleApe" } },
        };

        static readonly (int dx, int dy)[] Dirs = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
        static ScenarioTestHarness _h; static string Last = "";
        [OneTimeSetUp] public void Up()
        {
            _h = new ScenarioTestHarness();
            var dir = Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Data/GasDefinitions");
            GasRegistry.InitializeFromJsonSources(Directory.GetFiles(dir, "*.json").Select(File.ReadAllText).ToArray());
        }
        [OneTimeTearDown] public void Down() { _h?.Dispose(); _h = null; }

        static int Hp(Entity e) => e.GetStatValue("Hitpoints", 0);
        static bool Alive(ScenarioContext c, Entity e) => Hp(e) > 0 && c.Zone.GetEntityCell(e) != null;
        static bool Wet(Entity f) => (f.GetPart<StatusEffectsPart>()?.GetAllEffects() ?? new List<Effect>()).Any(e => e.GetType().Name == "WetEffect");
        static bool Resolved(ScenarioContext c, List<Entity> foes)
        {
            var me = c.Zone.GetEntityCell(c.PlayerEntity);
            foreach (var f in foes)
            {
                if (!Alive(c, f)) continue;
                var s = f.GetStat("Hitpoints"); bool hurt = s != null && s.Max > 0 && Hp(f) <= 0.3 * s.Max;
                if (!(hurt && Cheb(me, c.Zone.GetEntityCell(f)) >= 4)) return false;
            }
            return true;
        }
        static int Cheb(Cell a, Cell b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        static bool Cast(ScenarioContext c, string command, int dx = 0, int dy = 0, Cell target = null, int range = 6)
        {
            var p = c.PlayerEntity; var src = c.Zone.GetEntityCell(p);
            var cmd = GameEvent.New(command);
            cmd.SetParameter("Zone", (object)c.Zone); cmd.SetParameter("RNG", (object)c.Rng);
            cmd.SetParameter("SourceCell", (object)src);
            cmd.SetParameter("DirectionX", dx); cmd.SetParameter("DirectionY", dy); cmd.SetParameter("Range", range);
            if (target != null) cmd.SetParameter("TargetCell", (object)target);
            p.FireEvent(cmd); bool handled = cmd.Handled; cmd.Release();
            if (handled) Last = command;
            return handled;
        }

        // first creature along a ray within range, if it is a living foe
        static bool RayFoe(ScenarioContext c, List<Entity> foes, int range, out int dx, out int dy, out Entity foe)
        {
            dx = dy = 0; foe = null; int best = 99;
            var src = c.Zone.GetEntityCell(c.PlayerEntity);
            foreach (var (ddx, ddy) in Dirs)
                for (int i = 1; i <= range; i++)
                {
                    var cell = c.Zone.GetCell(src.X + ddx * i, src.Y + ddy * i);
                    if (cell == null || cell.IsSolid()) break;
                    var hit = cell.Objects.FirstOrDefault(o => o != c.PlayerEntity && o.HasTag("Creature") && Hp(o) > 0);
                    if (hit != null) { if (foes.Contains(hit) && i < best) { best = i; dx = ddx; dy = ddy; foe = hit; } break; }
                }
            return foe != null;
        }

        static List<Entity> Adjacent(ScenarioContext c, List<Entity> foes)
        {
            var me = c.Zone.GetEntityCell(c.PlayerEntity);
            return foes.Where(f => Alive(c, f) && Cheb(me, c.Zone.GetEntityCell(f)) == 1).ToList();
        }

        static bool Melee(ScenarioContext c, List<Entity> foes)
        {
            var adj = Adjacent(c, foes); if (adj.Count == 0) return false;
            var t = adj.OrderBy(f => Hp(f)).First();
            CombatSystem.PerformMeleeAttack(c.PlayerEntity, t, c.Zone, c.Rng); Last = "melee"; return true;
        }

        static bool CastAdj(ScenarioContext c, List<Entity> foes, string command)
        {
            var adj = Adjacent(c, foes); if (adj.Count == 0) return false;
            var me = c.Zone.GetEntityCell(c.PlayerEntity); var tc = c.Zone.GetEntityCell(adj[0]);
            return Cast(c, command, Math.Sign(tc.X - me.X), Math.Sign(tc.Y - me.Y), tc, 1);
        }

        static bool CastLine(ScenarioContext c, List<Entity> foes, string command, int range)
        {
            if (!RayFoe(c, foes, range, out int dx, out int dy, out _)) return false;
            return Cast(c, command, dx, dy, null, range);
        }

        static bool Throw(ScenarioContext c, List<Entity> foes, string blueprint)
        {
            var inv = c.PlayerEntity.GetPart<InventoryPart>();
            var item = inv.Objects.FirstOrDefault(o => o.BlueprintName == blueprint); if (item == null) return false;
            var me = c.Zone.GetEntityCell(c.PlayerEntity);
            var live = foes.Where(f => Alive(c, f)).ToList(); if (live.Count == 0) return false;
            var t = live.OrderBy(f => Cheb(me, c.Zone.GetEntityCell(f))).First(); var tc = c.Zone.GetEntityCell(t);
            if (Cheb(me, tc) < 2) return false; // 3x3 burst at distance 1 would catch me
            var r = InventorySystem.ExecuteCommand(new ThrowItemCommand(item, tc.X, tc.Y), c.PlayerEntity, c.Zone);
            if (r.Success) Last = "throw " + blueprint;
            return r.Success;
        }

        static bool Chase(ScenarioContext c, List<Entity> foes)
        {
            var me = c.Zone.GetEntityCell(c.PlayerEntity);
            var live = foes.Where(f => Alive(c, f)).OrderBy(f => Cheb(me, c.Zone.GetEntityCell(f))).ToList(); if (live.Count == 0) return false;
            var tc = c.Zone.GetEntityCell(live[0]);
            if (MovementSystem.TryMove(c.PlayerEntity, c.Zone, Math.Sign(tc.X - me.X), Math.Sign(tc.Y - me.Y))) { Last = "chase"; return true; }
            Last = "stuck"; return false;
        }

        // chase only when the nearest foe is far enough that waiting would idle forever (>= 4 cells) or it is fleeing
        static bool ChaseIfFar(ScenarioContext c, List<Entity> foes)
        {
            var me = c.Zone.GetEntityCell(c.PlayerEntity);
            var live = foes.Where(f => Alive(c, f)).ToList(); if (live.Count == 0) return false;
            // only chase a foe that is hurt and running (or very far); otherwise let them come to us and swing first
            bool anyRunning = live.Any(f => { var st = f.GetStat("Hitpoints"); return st != null && st.Max > 0 && Hp(f) <= 0.4 * st.Max && Cheb(me, c.Zone.GetEntityCell(f)) >= 2; });
            int d = live.Min(f => Cheb(me, c.Zone.GetEntityCell(f)));
            return (anyRunning || d >= 9) && Chase(c, foes);
        }

        static bool Frozen(Entity f) => (f.GetPart<StatusEffectsPart>()?.GetAllEffects() ?? new List<Effect>()).Any(e => e.GetType().Name == "FrozenEffect");

        static bool Act(ScenarioContext c, Build b, List<Entity> foes)
        {
            switch (b.Policy)
            {
                case "today":
                    return CastLine(c, foes, "CommandRimeGrip", 5) || CastLine(c, foes, "CommandGroundSurge", 4)
                        || CastLine(c, foes, "CommandEmberSpit", 4) || CastAdj(c, foes, "CommandFlamingHands")
                        || CastLine(c, foes, "CommandJetBlast", 2) || Melee(c, foes) || ChaseIfFar(c, foes);
                case "duelist":
                    return CastAdj(c, foes, "CommandFlurry") || CastAdj(c, foes, "CommandShank") || Melee(c, foes) || ChaseIfFar(c, foes);
                case "breaker":
                    {
                        int live = foes.Count(f => Alive(c, f)); int adj = Adjacent(c, foes).Count;
                        if (live >= 2 && adj >= 1 && Cast(c, "CommandAxeBerserk")) return true;
                        if (adj >= 2 && Cast(c, "CommandWhirlwind")) return true;
                        return Melee(c, foes) || ChaseIfFar(c, foes);
                    }
                case "cudgel":
                    {
                        int adj = Adjacent(c, foes).Count;
                        if (adj >= 2 && Cast(c, "CommandGroundPound")) return true;
                        if (CastAdj(c, foes, "CommandConk")) return true;
                        if (CastAdj(c, foes, "CommandSlam")) return true;
                        return Melee(c, foes) || ChaseIfFar(c, foes);
                    }
                case "caster0":
                    return CastLine(c, foes, "CommandQuench", 5) || CastLine(c, foes, "CommandArcBolt", 5)
                        || CastLine(c, foes, "CommandGroundSurge", 4) || CastLine(c, foes, "CommandJetBlast", 2) || Melee(c, foes) || ChaseIfFar(c, foes);
                case "caster":
                    if (RayFoe(c, foes, 5, out int cdx, out int cdy, out var cf))
                    {
                        if (Wet(cf) && Cast(c, "CommandArcBolt", cdx, cdy, null, 5)) return true;
                        if (!Wet(cf) && Cast(c, "CommandKindle", cdx, cdy, null, 5)) return true;
                    }
                    return CastLine(c, foes, "CommandGroundSurge", 4) || CastLine(c, foes, "CommandJetBlast", 2)
                        || CastLine(c, foes, "CommandArcBolt", 5) || CastLine(c, foes, "CommandKindle", 5) || Melee(c, foes) || ChaseIfFar(c, foes);
                case "bomber":
                    {
                        if (Throw(c, foes, "PoisonGasGrenade") || Throw(c, foes, "LightningTonic") || Throw(c, foes, "FrostTonic") || Throw(c, foes, "StunGasGrenade") || Throw(c, foes, "FireTonic")) return true;
                        if (CastLine(c, foes, "CommandDrenchLob", 6)) return true;
                        var adj = Adjacent(c, foes).Where(f => !Frozen(f)).ToList();
                        if (adj.Count >= 1 && Cast(c, "CommandColdSnap")) return true;
                        if (adj.Count >= 1) return Melee(c, foes);
                        return ChaseIfFar(c, foes); // else wait: frozen foes burn/thaw on their own
                    }
            }
            return false;
        }

        static Entity Setup(ScenarioContext c, Build b)
        {
            var p = c.PlayerEntity;
            p.GetStat("Strength").BaseValue = b.Str; p.GetStat("Agility").BaseValue = b.Agi;
            p.GetStat("Toughness").BaseValue = b.Tou; p.GetStat("Ego").BaseValue = b.Ego;
            var inv = p.GetPart<InventoryPart>();
            if (b.Weapon != null) { var w = _h.Factory.CreateEntity(b.Weapon); inv.AddObject(w); Assert.IsTrue(InventorySystem.Equip(p, w), b.Weapon); }
            foreach (var g in b.Gear) { var it = _h.Factory.CreateEntity(g); inv.AddObject(it); Assert.IsTrue(InventorySystem.Equip(p, it), g); }
            foreach (var cb in b.Carry) inv.AddObject(_h.Factory.CreateEntity(cb));
            if (b.StartKit) StartingSpellKit.GrantAll(p);
            var sk = p.GetPart<SkillsPart>(); foreach (var s in b.Skills) Assert.IsTrue(sk.AddSkill(s, "probe"), s);
            c.Turns.AddEntity(p);
            return p;
        }

        [Test]
        public void SimulateStartingFights()
        {
            const int Runs = 60, MaxRounds = 120;
            var sb = new StringBuilder();
            sb.AppendLine("scenario|build|win%|death%|stalemate%|avg_rounds(win)|avg_hp_lost(win)|avg_hp_left_when_won");
            foreach (var sc in Scenarios)
                foreach (var b in Builds())
                {
                    int wins = 0, deaths = 0, stale = 0; double rounds = 0, lost = 0, left = 0;
                    for (int run = 0; run < Runs; run++)
                    {
                        var c = _h.CreateContext(rngSeed: 500 + run, playerBlueprint: "Player", zoneId: "Sim" + run);
                        var p = Setup(c, b);
                        var foes = new List<Entity>(); int[] ys = { 11, 13, 12 }; int[] xs = sc.Key.Contains("2 cells") ? new[] { 42, 42, 43 } : new[] { 46, 46, 47 };
                        for (int i = 0; i < sc.Value.Length; i++)
                            foes.Add(c.Spawn(sc.Value[i]).Hostile().At(sc.Value.Length == 1 ? 46 : xs[i], sc.Value.Length == 1 ? 12 : ys[i]));
                        int hp0 = Hp(p), r = 0, prev = Hp(p), taken = 0; string outcome = "stale"; bool trace = run == 0 && Environment.GetEnvironmentVariable("TRACE") == b.Name + "|" + sc.Key.Substring(0, 2);
                        for (; r < MaxRounds; r++)
                        {
                            var who = c.Turns.ProcessUntilPlayerTurn();
                            if (Hp(p) <= 0 || who == null) { outcome = "death"; break; }
                            if (Resolved(c, foes)) { outcome = "win"; break; }
                            Last = "wait"; Act(c, b, foes);
                            if (trace) File.AppendAllText("trace.txt", $"r{r} hp{Hp(p)} {Last} | " + string.Join(", ", foes.Select(f => f.BlueprintName + ":" + Hp(f) + "[" + string.Join("/", (f.GetPart<StatusEffectsPart>()?.GetAllEffects() ?? new List<Effect>()).Select(e => e.GetType().Name.Replace("Effect", ""))) + "]@" + (c.Zone.GetEntityCell(f) == null ? "x" : c.Zone.GetEntityCell(f).X + "," + c.Zone.GetEntityCell(f).Y))) + "\n");
                            if (Hp(p) <= 0) { outcome = "death"; break; }
                            if (Resolved(c, foes)) { outcome = "win"; break; }
                            c.Turns.EndTurn(p, c.Zone);
                            GasSystem.OnTickEnd(c.Zone);
                            { int cur = Hp(p); if (cur < prev) taken += prev - cur; prev = cur; }
                        }
                        if (outcome == "win") { wins++; rounds += r; lost += taken; left += hp0 - taken; }
                        else if (outcome == "death") deaths++; else stale++;
                    }
                    sb.AppendLine($"{sc.Key}|{b.Name}|{100.0 * wins / Runs:F0}|{100.0 * deaths / Runs:F0}|{100.0 * stale / Runs:F0}|{(wins > 0 ? rounds / wins : 0):F1}|{(wins > 0 ? lost / wins : 0):F1}|{(wins > 0 ? left / wins : 0):F1}");
                }
            File.WriteAllText(Environment.GetEnvironmentVariable("PROBE_OUT") ?? "sim.txt", sb.ToString());
        }
    }
}
