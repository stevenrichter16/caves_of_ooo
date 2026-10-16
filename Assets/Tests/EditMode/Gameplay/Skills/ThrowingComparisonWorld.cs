using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Controlled comparison infrastructure only. Uses production purchases,
    // commands, scheduler and brains; never replaces combat with arithmetic.
    internal sealed class ThrowingComparisonWorld
    {
        internal const string Root = "ThrowingSkill", Measured = "Throwing_MeasuredThrow",
            Driving = "Throwing_DrivingThrow", Recovery = "Throwing_RecoveringStep", Focused = "Throwing_FocusedSplash";
        internal readonly EntityFactory Factory;
        internal readonly Zone Zone;
        internal readonly Entity Player;
        internal readonly TurnManager Clock;
        internal readonly System.Random Rng;
        internal readonly ThrowComparisonReceipt Receipt;
        internal readonly List<Entity> Enemies = new List<Entity>();
        internal InventoryPart Pack => Player.GetPart<InventoryPart>();
        internal SkillsPart Skills => Player.GetPart<SkillsPart>();
        internal bool Alive => Player.GetStatValue("Hitpoints") > 0 && Zone.GetEntityCell(Player) != null;
        int initialHp;
        readonly System.Random tonicRng;
        readonly HashSet<System.Random> seededEffectStreams = new HashSet<System.Random>();

        internal ThrowingComparisonWorld(EntityFactory factory, int seed, int budget, string package,
            string scenario, string variant, string policy, string cohort = "bombardier")
        {
            Factory = factory; Rng = new System.Random(seed);
            tonicRng = new System.Random(unchecked(seed + 7919));
            seededEffectStreams.Add(Rng); seededEffectStreams.Add(tonicRng);
            LoadoutPart.Factory = CorpsePart.Factory = factory;
            LoadoutPart.Rng = new System.Random(seed);
            Zone = new Zone("throw-comparison"); Clock = new TurnManager();
            SettlementRuntime.ActiveZone = Zone; MessageLog.TickProvider = () => Clock.TickCount;
            TurnManager.World = new Entity { ID = "comparison-world" };
            TurnManager.World.AddPart(new GasSystemPart());
            GasSystem.SetRngForTests(new System.Random(seed));
            GasPoisonPart.TestRng = new System.Random(unchecked(seed + 6151));
            FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Factions.json")));
            Receipt = new ThrowComparisonReceipt { seed = seed, budget = budget, package = package,
                scenario = scenario, variant = variant, policy = policy, cohort = cohort };
            for (int y = 0; y <= 10; y++) for (int x = 0; x <= 14; x++)
            {
                Zone.GetCell(x, y).Explored = Zone.GetCell(x, y).IsVisible = true;
                if (x == 0 || x == 14 || y == 0 || y == 10) Spawn("StoneWall", x, y);
            }
            Player = factory.CreateEntity("Player"); Player.ID = "player";
            var builds = JsonUtility.FromJson<StartingBuildFile>(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Builds/StartingBuilds.json")));
            var applied = StartingBuildService.Apply(Player, factory, builds.Builds.Single(b => b.Id == cohort));
            Assert.True(applied.Success, string.Join(";", applied.Errors));
            Assert.True(Zone.AddEntity(Player, 2, 5));
            // Controlled earned-level budget, applied identically to every arm.
            // This is not evidence of a natural acquisition route.
            int xp = 0; for (int level = 1; level <= budget; level++) xp += LevelingSystem.XPToNextLevel(level);
            Player.GetStat("Experience").BaseValue = xp; Player.GetStat("SP").BaseValue = 0;
            LevelingSystem.CheckLevelUp(Player, Zone);
            Assert.AreEqual(budget, Player.GetStatValue("SP"));
            initialHp = Player.GetStatValue("Hitpoints"); Receipt.initialHp = initialHp;
            int n = 0; foreach (var item in Pack.Objects.Concat(Pack.GetAllEquipped()).Distinct())
            { item.ID = "starting-item-" + n++; SeedTonicRandom(item); }
            foreach (string node in Nodes(package))
            {
                var bought = BuySkillAction.Execute(Player, node);
                Assert.True(bought.Succeeded, package + ": " + node + " " + bought.Reason + " " + bought.Detail);
                Receipt.spent += bought.CostPaid;
                Receipt.purchases.Add(node + ":" + bought.CostPaid);
            }
            Receipt.banked = Player.GetStatValue("SP");
            Assert.AreEqual(budget, Receipt.spent + Receipt.banked);
            Receipt.initialSupply = Supply();
            Clock.AddEntity(Player);
        }

        internal static string[] Packages(int budget)
        {
            if (budget == 0) return new[] { "bank" };
            if (budget == 2) return new[] { "bank", "measured", "driving", "recovery", "focused", "throw-water", "dodge", "tumble", "puncture", "jet", "recruit" };
            if (budget == 4) return new[] { "bank", "physical4", "general4", "focus-tumble", "measured-jet", "recovery-recruit", "blades4", "water-tumble", "measured" };
            return new[] { "bank", "all5-bank", "all5-water", "physical-dodge", "focus-water", "recovery-party", "water-acrobatics", "blades4" };
        }
        internal static string[] Nodes(string package)
        {
            switch (package)
            {
                case "bank": return Array.Empty<string>();
                case "root": return new[] { Root };
                case "measured": return new[] { Root, Measured };
                case "driving": return new[] { Root, Driving };
                case "recovery": return new[] { Root, Recovery };
                case "focused": return new[] { Root, Focused };
                case "throw-water": return new[] { Root, "HydromancySkill" };
                case "dodge": return new[] { "AcrobaticsSkill", "AcrobaticsDodgePower" };
                case "tumble": return new[] { "AcrobaticsSkill", "Acrobatics_Tumble" };
                case "puncture": return new[] { "ShortBladesSkill", "ShortBlades_Puncture" };
                case "jet": return new[] { "HydromancySkill", "Hydromancy_JetBlast" };
                case "recruit": return new[] { "PersuasionSkill", "Persuasion_Recruit" };
                case "physical4": return new[] { Root, Measured, Driving, Recovery };
                case "general4": return new[] { Root, Measured, Recovery, Focused };
                case "focus-tumble": return new[] { Root, Focused, "AcrobaticsSkill", "Acrobatics_Tumble" };
                case "measured-jet": return new[] { Root, Measured, "HydromancySkill", "Hydromancy_JetBlast" };
                case "recovery-recruit": return new[] { Root, Recovery, "PersuasionSkill", "Persuasion_Recruit" };
                case "blades4": return new[] { "ShortBladesSkill", "ShortBlades_Puncture", "ShortBlades_Flurry", "ShortBlades_Disengage" };
                case "water-tumble": return new[] { "HydromancySkill", "Hydromancy_JetBlast", "AcrobaticsSkill", "Acrobatics_Tumble" };
                case "all5-bank": return new[] { Root, Measured, Driving, Recovery, Focused };
                case "all5-water": return new[] { Root, Measured, Driving, Recovery, Focused, "HydromancySkill" };
                case "physical-dodge": return new[] { Root, Measured, Driving, "AcrobaticsSkill", "AcrobaticsDodgePower", "Acrobatics_Tumble" };
                case "focus-water": return new[] { Root, Focused, "HydromancySkill", "Hydromancy_JetBlast", "Hydromancy_Undertow", "Hydromancy_Quench" };
                case "recovery-party": return new[] { Root, Recovery, "PersuasionSkill", "Persuasion_Recruit", "AcrobaticsSkill", "Acrobatics_Tumble" };
                case "water-acrobatics": return new[] { "HydromancySkill", "Hydromancy_JetBlast", "Hydromancy_Undertow", "AcrobaticsSkill", "AcrobaticsDodgePower", "Acrobatics_Tumble" };
                default: throw new ArgumentException("Unfrozen purchase package " + package);
            }
        }
        internal Entity Spawn(string blueprint, int x, int y)
        {
            var entity = Factory.CreateEntity(blueprint); Assert.NotNull(entity, blueprint);
            entity.ID = blueprint + "-" + x + "-" + y;
            Assert.True(Zone.AddEntity(entity, x, y), blueprint + " placement"); return entity;
        }
        internal Entity Enemy(string blueprint, int x, int y, int? armor = null)
        {
            var entity = Spawn(blueprint, x, y);
            if (armor.HasValue)
            {
                // Exact total AV: retain authored equipment and adjust only the
                // explicit fixed-world natural-armor experiment variable.
                var natural = entity.GetPart<ArmorPart>();
                if (natural == null) { natural = new ArmorPart(); entity.AddPart(natural); }
                natural.AV += armor.Value - CombatSystem.GetAV(entity);
                Assert.AreEqual(armor.Value, CombatSystem.GetAV(entity));
            }
            var brain = entity.GetPart<BrainPart>(); Assert.NotNull(brain);
            brain.CurrentZone = Zone; brain.Rng = new System.Random(Receipt.seed + Enemies.Count * 1009);
            if (entity.GetPart<CorpsePart>() is CorpsePart corpse)
                corpse.TestRng = new System.Random(Receipt.seed + Enemies.Count * 1013);
            brain.SetPersonallyHostile(Player, false);
            seededEffectStreams.Add(brain.Rng);
            entity.AddPart(new ComparisonStunRandomPart { Rng = new System.Random(unchecked(Receipt.seed + 9151 + Enemies.Count * 1019)), KnownStreams = seededEffectStreams });
            Enemies.Add(entity); Clock.AddEntity(entity); entity.AddPart(new ComparisonTurnWitness());
            return entity;
        }
        internal void Start() => Assert.AreSame(Player, Clock.ProcessUntilPlayerTurn());
        internal bool Has(string node) => Skills.HasSkill(node);
        internal Entity Item(string blueprint) => Pack.Objects.Concat(Pack.GetAllEquipped()).FirstOrDefault(e => e.BlueprintName == blueprint);
        internal Entity AddSupply(string blueprint)
        {
            var item = Factory.CreateEntity(blueprint); Assert.NotNull(item);
            item.ID = "supplied-" + blueprint; Assert.True(Pack.AddObject(item));
            SeedTonicRandom(item); Receipt.supplied.Add(blueprint); return item;
        }
        void SeedTonicRandom(Entity item)
        {
            if(item.GetPart<TonicPart>()==null)return;
            // Controlled fixture seam only. InventoryAction already supports a
            // Random parameter but the normal command leaves it absent, causing
            // tonic self-use to construct a wall-clock-seeded RNG. Supply the
            // existing parameter without replacing commands, doses or payment.
            var witness=new ComparisonTonicRandomPart { Rng=tonicRng };
            item.AddPart(witness); item.Parts.Remove(witness); item.Parts.Insert(0,witness);
        }
        internal bool Do(string label, Func<bool> action)
        {
            if (!Alive || Receipt.actions >= 100) return false;
            Assert.AreSame(Player, Clock.CurrentActor);
            bool paid = action(); Receipt.commands.Add(label + (paid ? ":paid" : ":refused"));
            if (!paid) return false;
            Receipt.afterActions.Add(label + " player=" + Position(Player) + "/" + Player.GetStatValue("Hitpoints") + " " + string.Join(";", Enemies.Select(e =>
                e.BlueprintName + "=" + Position(e) + "/" + e.GetStatValue("Hitpoints"))));
            Receipt.actions++; Clock.EndTurn(Player, Zone);
            if (Alive) Clock.ProcessUntilPlayerTurn();
            // Same boundary/order as InputHandler.EndTurnAndProcess: all NPC
            // responses precede material simulation and one player terrain tick.
            MaterialSimSystem.TickMaterialEntities(Zone);
            ZoneTileStateSystem.OnPlayerTurnEnd(Zone);
            return true;
        }
        internal bool Throw(Entity item, Entity target, ThrowMode mode = ThrowMode.Ordinary, bool focused = false)
        {
            var cell = Zone.GetEntityCell(target); if (item == null || cell == null) return false;
            return ThrowAt(item, ThrowIntent.AtEntity(target, cell.X, cell.Y), mode, focused);
        }
        internal bool ThrowAt(Entity item, ThrowIntent intent, ThrowMode mode = ThrowMode.Ordinary, bool focused = false)
            => item != null && Do("throw:" + item.BlueprintName + ":" + mode + ":" + (focused ? "focused" : "broad"),
                () => InventorySystem.ExecuteCommand(new ThrowItemCommand(item, intent, Rng, mode, focused), Player, Zone).ActionCommitted);
        internal bool Prepare(Entity item, Entity target) => Do("prepare", () => ThrowingRules.Prepare(Player, item, Zone,
            target, Zone.GetEntityCell(target), out _));
        internal bool Recover(Entity item) => Do("recover", () => ThrowingRules.Recover(Player, item, Zone).Committed);
        internal bool Move(int dx, int dy) => Do("move:" + dx + "," + dy, () => MovementSystem.TryMove(Player, Zone, dx, dy));
        internal bool Pickup(Entity item) => item != null && Zone.GetEntityCell(item) == Zone.GetEntityCell(Player)
            && Do("pickup", () => InventorySystem.ExecuteCommand(new PickupCommand(item, false), Player, Zone).Success);
        internal bool Melee(Entity target) => Do("melee", () => CombatSystem.PerformMeleeAttack(Player, target, Zone, Rng));
        internal bool Cast(string command, Entity target = null, int dx = 1, int dy = 0)
            => Do(command, () => Skills.TryRouteSkillCommand(command, Zone, Rng, dx, dy,
                Zone.GetEntityCell(Player), target == null ? null : Zone.GetEntityCell(target), 6, out _));
        internal bool Wait() => Do("wait", () => true);
        internal int Distance(Entity target) => SpatialQuery.Distance(Zone, Player, target);
        internal string Position(Entity entity)
        { var cell = Zone.GetEntityCell(entity); return cell == null ? "gone" : cell.X + "," + cell.Y; }
        internal string Supply() => string.Join(";", Pack.Objects.Concat(Pack.GetAllEquipped()).Distinct().OrderBy(e => e.ID).Select(e =>
            e.BlueprintName + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1) + (e.GetPart<PhysicsPart>()?.Equipped == Player ? ":equipped" : ":carried")));
        internal ThrowComparisonReceipt Finish(string outcome, Entity trackedItem = null)
        {
            Receipt.outcome = outcome; Receipt.hp = Player.GetStatValue("Hitpoints");
            Receipt.hpLoss = Math.Max(0, initialHp - Receipt.hp); Receipt.survived = Alive;
            Receipt.tick = Clock.TickCount; Receipt.finalSupply = Supply();
            var at = Zone.GetEntityCell(Player); Receipt.x = at?.X ?? -1; Receipt.y = at?.Y ?? -1;
            foreach (var enemy in Enemies)
            {
                var cell = Zone.GetEntityCell(enemy); var witness = enemy.GetPart<ComparisonTurnWitness>();
                Receipt.enemyTurns += witness?.Turns ?? 0;
                Receipt.enemies.Add(enemy.BlueprintName + ":hp=" + enemy.GetStatValue("Hitpoints")
                    + ":pos=" + (cell == null ? "gone" : cell.X + "," + cell.Y) + ":turns=" + (witness?.Turns ?? 0));
            }
            if (trackedItem != null)
            {
                var p = trackedItem.GetPart<PhysicsPart>(); var cell = Zone.GetEntityCell(trackedItem);
                Receipt.custody = Pack.Contains(trackedItem) ? "player" : p?.InInventory != null ? p.InInventory.BlueprintName
                    : cell != null ? "floor:" + cell.X + "," + cell.Y : "lost";
                Receipt.equipped = p?.Equipped == Player;
            }
            Receipt.measuredCooldown = ThrowingRules.CooldownRemaining(Player, Measured);
            Receipt.drivingCooldown = ThrowingRules.CooldownRemaining(Player, Driving);
            Receipt.recoveryCooldown = ThrowingRules.CooldownRemaining(Player, Recovery);
            return Receipt;
        }
        internal static void SaveReceipts(string name, List<ThrowComparisonReceipt> receipts)
        {
            var path = Path.Combine(Application.dataPath, "../Docs/Verification/Throwing/comparisons");
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, name + ".json"), JsonUtility.ToJson(new ThrowComparisonBatch { trials = receipts }, true));
            TestContext.Out.WriteLine(name + ": " + receipts.Count + " actual-command trials; production RNG streams diverge after differing skills/actions.");
        }
    }
    public sealed class ComparisonTonicRandomPart : Part
    {
        public System.Random Rng;
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="InventoryAction" && e.GetStringParameter("Command")=="ApplyTonic")
                e.SetParameter("Random",(object)Rng);
            return true;
        }
    }
    // Fixture seam only: real Electrified contact creates its real StunnedEffect,
    // but that effect otherwise seeds its save RNG from wall time. Seed the
    // existing RNG before application/ticks, retaining effects, saves and AI.
    // Keep real combat/brain RNGs already supplied explicitly by gameplay.
    public sealed class ComparisonStunRandomPart : Part
    {
        public System.Random Rng;
        public HashSet<System.Random> KnownStreams;
        public int Applications;
        public StunnedEffect LastStun;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeApplyEffect" && e.GetParameter<Effect>("Effect") is StunnedEffect stun)
            {
                var sourceStream = e.GetParameter<Entity>("Source")?.GetPart<BrainPart>()?.Rng;
                if (stun.Rng != sourceStream && (KnownStreams == null || !KnownStreams.Contains(stun.Rng))) stun.Rng = Rng;
                LastStun = stun;
                Applications++;
            }
            return true;
        }
    }
    public sealed class ComparisonTurnWitness : Part
    {
        public int Turns;
        public override bool HandleEvent(GameEvent e) { if (e.ID == "EndTurn") Turns++; return true; }
    }
    internal sealed class ComparisonFactionScope : IDisposable
    {
        readonly List<(FieldInfo field, object original)> saved = new List<(FieldInfo, object)>();
        readonly EntityFactory oldLoadoutFactory = LoadoutPart.Factory, oldCorpseFactory = CorpsePart.Factory;
        readonly System.Random oldLoadoutRng = LoadoutPart.Rng;
        readonly System.Random oldGasPoisonRng = GasPoisonPart.TestRng;
        readonly StackTraceLogType oldLogStackTrace=Application.GetStackTraceLogType(LogType.Log);
        internal ComparisonFactionScope()
        {
            Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
            foreach (var type in new[] { typeof(FactionManager), typeof(PlayerReputation) })
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (field.IsInitOnly || !field.FieldType.IsGenericType) continue;
                var original = field.GetValue(null); if (original == null) continue;
                saved.Add((field, original)); field.SetValue(null, Activator.CreateInstance(field.FieldType));
            }
        }
        public void Dispose()
        {
            foreach (var value in saved) value.field.SetValue(null, value.original);
            LoadoutPart.Factory = oldLoadoutFactory; CorpsePart.Factory = oldCorpseFactory; LoadoutPart.Rng = oldLoadoutRng;
            GasPoisonPart.TestRng = oldGasPoisonRng;
            Application.SetStackTraceLogType(LogType.Log,oldLogStackTrace);
        }
    }
    [Serializable] internal sealed class ThrowComparisonBatch { public List<ThrowComparisonReceipt> trials; }
    [Serializable] internal sealed class ThrowComparisonReceipt
    {
        public int seed, budget, spent, banked, initialHp, hp, hpLoss, actions, enemyTurns, tick, x, y;
        public int measuredCooldown, drivingCooldown, recoveryCooldown;
        public string scenario, variant, policy, cohort, package, outcome, initialSupply, finalSupply, custody;
        public bool survived, equipped;
        public List<string> purchases = new List<string>(), supplied = new List<string>(), commands = new List<string>(), enemies = new List<string>(), afterActions = new List<string>();
    }
}
