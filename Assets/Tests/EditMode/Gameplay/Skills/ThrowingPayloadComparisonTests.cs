using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Fixed controlled opportunities, not natural acquisition evidence or a claim
    // that the scripted policies are optimal. Every paid action advances real AI.
    public sealed class ThrowingPayloadComparisonTests : FiftyWorldFixture
    {
        readonly List<Action> restore = new List<Action>();
        ComparisonFactionScope factions;
        [SetUp] public void SetupComparisonRegistries()
        {
            factions = new ComparisonFactionScope();
            SnapshotDictionary(typeof(GasRegistry), "_byId"); SnapshotScalar(typeof(GasRegistry), "_initialized");
            SnapshotScalar(typeof(GasSystem), "_rng");
            foreach (string name in new[] { "_skillsByName", "_skillsByClass", "_powersByClass", "_entriesByClass" }) SnapshotDictionary(typeof(SkillRegistry), name);
            SnapshotScalar(typeof(SkillRegistry), "_initialized");
            var field = typeof(TileReactionSystem).GetField("_reactions", BindingFlags.NonPublic | BindingFlags.Static);
            var list = (List<TileReactionSystem.TileReaction>)field.GetValue(null); var old = list.ToArray();
            restore.Add(() => { list.Clear(); list.AddRange(old); }); SnapshotScalar(typeof(TileReactionSystem), "<IsInitialized>k__BackingField");
            SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized();
            GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath, "Resources/Content/Data/GasDefinitions"), "*.json").Select(File.ReadAllText));
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }
        void SnapshotDictionary(Type type, string name)
        {
            var dict = (IDictionary)type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var entries = new List<DictionaryEntry>(); foreach (DictionaryEntry pair in dict) entries.Add(pair);
            restore.Add(() => { dict.Clear(); foreach (var pair in entries) dict.Add(pair.Key, pair.Value); });
        }
        void SnapshotScalar(Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static); var old = field.GetValue(null);
            restore.Add(() => field.SetValue(null, old));
        }
        [TearDown] public void RestoreComparisonRegistries() { foreach (var action in restore) action(); restore.Clear(); factions.Dispose(); }
        static string[] Packages(int budget) => budget == 0 ? new[] { "bank" }
            : budget == 2 ? new[] { "bank", "focused", "throw-water", "jet", "tumble" }
            : budget == 4 ? new[] { "bank", "general4", "focus-tumble", "water-tumble" }
            : new[] { "bank", "all5-bank", "focus-water", "water-acrobatics" };
        static int HP(Entity e) => Math.Max(0, e.GetStatValue("Hitpoints"));
        Entity Ally(ThrowingComparisonWorld w, int x, int y, string id)
        {
            var ally = w.Spawn("Villager", x, y); ally.ID = id;
            var brain = ally.GetPart<BrainPart>(); brain.CurrentZone = w.Zone; brain.Rng = new System.Random(w.Receipt.seed + x * 101 + y);
            Assert.True(brain.SetPartyLeader(w.Player)); w.Clock.AddEntity(ally); return ally;
        }
        [TestCase(0)][TestCase(2)][TestCase(4)][TestCase(6)]
        public void W3_MixedHealingAndAlliedGroupUseRealFiniteSupplies(int budget)
        {
            var receipts = new List<PayloadComparisonReceipt>();
            foreach (string package in Packages(budget)) foreach (string layout in new[] { "mixed", "two-allies" })
            foreach (string policy in new[] { "broad", "focused-if-owned", "drink-retreat" }) for (int seed = 101; seed <= 120; seed++)
            {
                var w = new ThrowingComparisonWorld(Factory, seed, budget, package, "W3", layout, policy);
                var ally = Ally(w, 6, 5, "ally"); ally.GetStat("Hitpoints").BaseValue = 5;
                var second = layout == "two-allies" ? Ally(w, 6, 6, "ally2") : null;
                if (second != null) second.GetStat("Hitpoints").BaseValue = 5;
                var enemy = w.Enemy("MarlbackScrabbler", layout == "mixed" ? 7 : 10, 5);
                enemy.GetStat("Hitpoints").BaseValue = Math.Max(1, HP(enemy) / 2);
                w.Player.GetStat("Hitpoints").BaseValue = Math.Max(1, HP(w.Player) / 2);
                int beforeAlly = HP(ally) + (second == null ? 0 : HP(second)), beforeEnemy = HP(enemy), beforePlayer = HP(w.Player);
                var r = new PayloadComparisonReceipt(); w.Start();
                bool focus = policy == "focused-if-owned" && w.Has(ThrowingComparisonWorld.Focused);
                var tonic = w.Item("HealingTonic"); Assert.NotNull(tonic);
                bool acted = policy == "drink-retreat"
                    ? w.Do("drink:HealingTonic", () => InventorySystem.PerformAction(w.Player, tonic, "ApplyTonic", w.Zone))
                    : w.Throw(tonic, ally, focused: focus);
                Assert.True(acted, "real initial payload action must be legal");
                // Freeze the follow-up policy: withdraw once, then observe two genuine AI rounds.
                if (w.Alive) w.Move(-1, 0); for (int step = 0; step < 2 && w.Alive; step++) w.Wait();
                r.allyHpDelta = HP(ally) + (second == null ? 0 : HP(second)) - beforeAlly;
                r.enemyHpDelta = HP(enemy) - beforeEnemy; r.playerHpDelta = HP(w.Player) - beforePlayer;
                r.allySurvivors = (HP(ally) > 0 ? 1 : 0) + (second != null && HP(second) > 0 ? 1 : 0);
                r.hostileAlly = ally.GetPart<BrainPart>().IsPersonallyHostileTo(w.Player);
                r.actualFocused = focus; r.common = w.Finish(w.Alive ? "observed" : "death", w.Item("Dagger"));
                r.common.initialHp = beforePlayer;
                r.common.hpLoss = Math.Max(0, beforePlayer - HP(w.Player));
                receipts.Add(r);
            }
            Save("W3-budget-" + budget, receipts);
        }
        [TestCase(0)][TestCase(2)][TestCase(4)][TestCase(6)]
        public void W7_FiniteGroundControlHasBypassImmunityRangeAndSupplyCounterconditions(int budget)
        {
            var receipts = new List<PayloadComparisonReceipt>();
            foreach (string package in Packages(budget))
            foreach (string variant in new[] { "open", "choke", "bypass", "acid-immune", "fast", "ranged", "no-spare" })
            foreach (string policy in new[] { "direct", "broad-ground", "focused-if-owned", "available-control", "no-item-retreat" })
            for (int seed = 101; seed <= 120; seed++)
                receipts.Add(RunGroundControlTrial(budget, package, variant, policy, seed));
            Save("W7-budget-" + budget, receipts);
        }
        // Selected prospectively for the bounded 20-to-100 precision expansion.
        // Same worlds, supplies and policies as W7; not a new balance policy.
        [TestCase(2, "focused", "fast", "focused-if-owned")]
        [TestCase(2, "focused", "fast", "broad-ground")]
        [TestCase(2, "focused", "open", "focused-if-owned")]
        [TestCase(2, "focused", "open", "broad-ground")]
        [TestCase(2, "focused", "acid-immune", "focused-if-owned")]
        [TestCase(2, "focused", "acid-immune", "broad-ground")]
        [TestCase(2, "jet", "open", "available-control")]
        [TestCase(0, "bank", "open", "direct")]
        [TestCase(0, "bank", "open", "broad-ground")]
        public void W7_SelectedPrecisionExtensionKeepsTheFrozenPolicies(int budget, string package, string variant, string policy)
        {
            var receipts = new List<PayloadComparisonReceipt>();
            for (int seed = 101; seed <= 200; seed++)
                receipts.Add(RunGroundControlTrial(budget, package, variant, policy, seed));
            Save("W7-precision100-" + budget + "-" + package + "-" + variant + "-" + policy, receipts);
        }
        PayloadComparisonReceipt RunGroundControlTrial(int budget, string package, string variant, string policy, int seed)
        {
            var w = new ThrowingComparisonWorld(Factory, seed, budget, package, "W7", variant, policy);
            if (variant == "choke" || variant == "bypass")
                for (int y = 1; y < 10; y++) if (y != 5 && !(variant == "bypass" && y == 2)) w.Spawn("StoneWall", 6, y);
            var enemy = w.Enemy(variant == "fast" ? "Viper" : variant == "ranged" ? "MarlbackStormbinder" : "MarlbackScrabbler", 8, 5);
            if (variant == "acid-immune")
            {
                var resistance = enemy.GetStat("AcidResistance");
                if (resistance == null) enemy.Statistics["AcidResistance"] = resistance = new Stat { Owner = enemy, Name = "AcidResistance", Min = -100, Max = 100 };
                resistance.BaseValue = 100;
            }
            var acid = variant == "no-spare" ? null : w.AddSupply("AcidTonic");
            int beforeEnemy = HP(enemy); w.Start(); var r = new PayloadComparisonReceipt();
            bool focus = policy == "focused-if-owned" && w.Has(ThrowingComparisonWorld.Focused);
            if (policy == "no-item-retreat")
            {
                w.Move(-1, 0); r.common = w.Finish(w.Alive ? "early-retreat" : "death", w.Item("Dagger")); return r;
            }
            if (policy == "available-control") w.Cast("CommandDrenchLob", enemy);
            else if (acid == null) w.Move(-1, 0);
            else if (policy == "direct") w.Throw(acid, enemy);
            else w.ThrowAt(acid, ThrowIntent.Ground(6, 5), focused: focus);
            r.matterAfterAction = Matter(w.Zone); r.navigationPenalty = TerrainNavigationWeight.ForCell(w.Zone.GetCell(6, 5), enemy);
            // Observe choices instead of forcing an enemy across the film. A ranged
            // caster may stay away; a route around it can be the successful outcome.
            for (int step = 0; step < 8 && w.Alive; step++)
            {
                if (policy == "available-control" && TryControl(w, enemy)) { }
                else if (step == 1 && w.Zone.GetEntityCell(w.Player)?.X > 1) w.Move(-1, 0);
                else if (w.Distance(enemy) <= 1 && HP(enemy) > 0) w.Melee(enemy);
                else w.Wait();
                var pos = w.Zone.GetEntityCell(enemy);
                if (pos != null) { r.maxDetour = Math.Max(r.maxDetour, Math.Abs(pos.Y - 5)); if (pos.X <= 6) r.crossed = true; }
                if (enemy.GetEffect<LiquidCoveredEffect>()?.LiquidId == "acid") r.coated = true;
            }
            r.enemyHpDelta = HP(enemy) - beforeEnemy; r.remainingMatter = Matter(w.Zone); r.actualFocused = focus;
            r.common = w.Finish(!w.Alive ? "death" : r.crossed ? "lane-crossed" : "held-lane", w.Item("Dagger"));
            Assert.LessOrEqual(r.matterAfterAction, 9); Assert.LessOrEqual(r.remainingMatter, r.matterAfterAction);
            if (variant == "no-spare" || policy == "no-item-retreat") Assert.AreEqual(0, r.matterAfterAction);
            return r;
        }
        [TestCase(true)][TestCase(false)]
        public void ComparisonStunSeedWitnessObservesOnlyRealContactEffects(bool electrify)
        {
            var w = new ThrowingComparisonWorld(Factory, 104, 2, "jet", "rng-control", "open", "actual-contact");
            var enemy = w.Enemy("MarlbackScrabbler", 8, 5); w.Start();
            Assert.True(w.Cast("CommandDrenchLob", enemy));
            var lightning = w.Item("LightningTonic"); Assert.NotNull(lightning);
            if (electrify) Assert.True(w.Throw(lightning, enemy)); else Assert.True(w.Wait());
            var witness = enemy.GetPart<ComparisonStunRandomPart>(); Assert.NotNull(witness);
            Assert.AreEqual(electrify, witness.Applications > 0);
            if (electrify)
            {
                Assert.AreSame(witness.Rng, witness.LastStun.Rng);
                Assert.IsNull(w.Item("LightningTonic"), "real finite payload consumed");
            }
            else
            {
                Assert.IsNull(witness.LastStun);
                Assert.AreSame(lightning, w.Item("LightningTonic"), "control retains the untouched payload");
            }
        }
        [TestCase("world")][TestCase("own-brain")][TestCase("other-brain")][TestCase("source-brain")][TestCase("fallback")]
        public void ComparisonStunSeedPreservesExplicitGameplayStreams(string source)
        {
            var w = new ThrowingComparisonWorld(Factory, 104, 0, "bank", "rng-control", "open", source);
            var first = w.Enemy("MarlbackScrabbler", 8, 5);
            // A stream declared after the first witness was installed must also
            // be recognized, since another enemy can be the stun's attacker.
            var second = w.Enemy("MarlbackScrabbler", 9, 7);
            var ally = w.Spawn("Villager", 10, 7);
            ally.GetPart<BrainPart>().Rng = new System.Random(8123);
            // Allies and dogs have their actual Brain RNG seeded outside Enemy().
            var supplied = source == "world" ? w.Rng : source == "own-brain" ? first.GetPart<BrainPart>().Rng
                : source == "other-brain" ? second.GetPart<BrainPart>().Rng : source == "source-brain" ? ally.GetPart<BrainPart>().Rng : null;
            var stun = new StunnedEffect(duration: 2, saveTarget: 16, rng: supplied);
            Assert.True(first.ApplyEffect(stun, source == "source-brain" ? ally : w.Player, w.Zone));
            var witness = first.GetPart<ComparisonStunRandomPart>();
            Assert.AreSame(stun, first.GetEffect<StunnedEffect>());
            Assert.AreSame(source == "fallback" ? witness.Rng : supplied, stun.Rng);
            if (source != "fallback") Assert.AreNotSame(witness.Rng, stun.Rng, "already-owned gameplay stream is not replaced");
        }
        [TestCase("available-control")][TestCase("broad-ground")]
        public void W7_SeededControlPolicyReplaysAcrossFreshWorlds(string policy)
        {
            int lightningUses = 0;
            for (int seed = 101; seed <= 120; seed++)
            {
                var first = RunGroundControlTrial(2, "jet", "open", policy, seed);
                var second = RunGroundControlTrial(2, "jet", "open", policy, seed);
                Assert.AreEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(second), "full same-command receipt, seed " + seed);
                lightningUses += first.common.commands.Count(c => c.StartsWith("throw:LightningTonic:", StringComparison.Ordinal) && c.EndsWith(":paid", StringComparison.Ordinal));
            }
            Assert.AreEqual(policy == "available-control", lightningUses > 0, "real contact-stun policy versus unused lightning control");
        }
        static bool TryControl(ThrowingComparisonWorld w, Entity enemy)
        {
            if (HP(enemy) <= 0) return false;
            var origin = w.Zone.GetEntityCell(w.Player); var target = w.Zone.GetEntityCell(enemy);
            if (origin == null || target == null) return false;
            int dx = Math.Sign(target.X - origin.X), dy = Math.Sign(target.Y - origin.Y);
            if (w.Has("Hydromancy_JetBlast") && w.Distance(enemy) <= 2 && w.Cast("CommandJetBlast", enemy, dx, dy)) return true;
            if (w.Distance(enemy) <= 2 && w.Cast("CommandColdSnap", enemy)) return true;
            if (enemy.GetEffect<WetEffect>() != null && w.Item("LightningTonic") != null && w.Throw(w.Item("LightningTonic"), enemy)) return true;
            return false;
        }
        [TestCase(0)][TestCase(2)][TestCase(4)][TestCase(6)]
        public void W7_WaterAndAcidOrdersPayForTheirRealInterveningEnemyTurns(int budget)
        {
            var receipts = new List<PayloadComparisonReceipt>();
            foreach (string package in Packages(budget)) foreach (string pair in new[] { "water-charge", "water-push", "acid-push" })
            foreach (bool groundFirst in new[] { true, false }) for (int seed = 101; seed <= 120; seed++)
            {
                var w = new ThrowingComparisonWorld(Factory, seed, budget, package, "W7-order", pair, groundFirst ? "place-first" : "effect-first");
                var enemy = w.Enemy("MarlbackScrabbler", 8, 5);
                var liquid = w.AddSupply(pair == "acid-push" ? "AcidTonic" : "WaterTonic");
                var cudgel = pair.EndsWith("push", StringComparison.Ordinal) ? w.AddSupply("Cudgel") : null;
                int before = HP(enemy); w.Start(); var r = new PayloadComparisonReceipt();
                for (int action = 0; action < 2 && w.Alive; action++)
                {
                    bool place = action == 0 ? groundFirst : !groundFirst;
                    if (place) w.ThrowAt(liquid, ThrowIntent.Ground(6, 5), focused: w.Has(ThrowingComparisonWorld.Focused));
                    else if (cudgel != null) w.Throw(cudgel, enemy, w.Has(ThrowingComparisonWorld.Driving) ? ThrowMode.Driving : ThrowMode.Ordinary);
                    else w.ThrowAt(w.Item("LightningTonic"), ThrowIntent.Ground(6, 5));
                    r.matterAfterAction = Math.Max(r.matterAfterAction, Matter(w.Zone));
                }
                for (int step = 0; step < 4 && w.Alive; step++)
                {
                    if (w.Distance(enemy) <= 1 && HP(enemy) > 0) w.Melee(enemy); else w.Wait();
                    var pos = w.Zone.GetEntityCell(enemy); if (pos != null && pos.X <= 6) r.crossed = true;
                    if (enemy.GetEffect<LiquidCoveredEffect>() != null) r.coated = true;
                }
                r.enemyHpDelta = HP(enemy) - before; r.remainingMatter = Matter(w.Zone);
                r.common = w.Finish(!w.Alive ? "death" : r.crossed ? "lane-crossed" : "held-lane", cudgel ?? w.Item("Dagger"));
                receipts.Add(r); Assert.LessOrEqual(r.remainingMatter, 9);
            }
            Save("W7-order-budget-" + budget, receipts);
        }
        static int Matter(Zone zone) => zone.GetReadOnlyEntities().Where(e => e.HasPart<FiniteLiquidFilmPart>()).Sum(e => e.GetPart<LiquidPoolPart>()?.Volume ?? 0);
        static void Save(string name, List<PayloadComparisonReceipt> receipts)
        {
            string path = Path.Combine(Application.dataPath, "../Docs/Verification/Throwing/comparisons"); Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, name + ".json"), JsonUtility.ToJson(new PayloadComparisonBatch { trials = receipts }, true));
            TestContext.Out.WriteLine(name + ": " + receipts.Count + " finite supplied, real-purchase/AI trials. Controlled world; not acquisition proof.");
        }
    }
    [Serializable] internal sealed class PayloadComparisonBatch { public List<PayloadComparisonReceipt> trials; }
    [Serializable] internal sealed class PayloadComparisonReceipt
    {
        public ThrowComparisonReceipt common;
        public int allyHpDelta, enemyHpDelta, playerHpDelta, allySurvivors, matterAfterAction, remainingMatter, navigationPenalty, maxDetour;
        public bool actualFocused, hostileAlly, crossed, coated;
    }
}
