using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    [Category("ThrowingComparison")]
    public sealed class ThrowingOpportunityComparisonTests : FiftyWorldFixture
    {
        object oldGasRng;
        readonly FieldInfo gasRng = typeof(GasSystem).GetField("_rng", BindingFlags.Static | BindingFlags.NonPublic);
        Dictionary<string, GasDefinition> oldGases;
        bool oldGasInitialized;
        readonly FieldInfo gases = typeof(GasRegistry).GetField("_byId", BindingFlags.Static | BindingFlags.NonPublic);
        readonly FieldInfo gasInitialized = typeof(GasRegistry).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
        ComparisonFactionScope factions;
        static readonly FieldInfo Reactions = typeof(TileReactionSystem).GetField("_reactions", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo ReactionsInitialized = typeof(TileReactionSystem).GetField("<IsInitialized>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        List<TileReactionSystem.TileReaction> oldReactions;
        bool oldReactionsInitialized;

        [SetUp] public void ComparisonSetup()
        {
            oldGasRng = gasRng.GetValue(null);
            oldGases = new Dictionary<string, GasDefinition>((Dictionary<string, GasDefinition>)gases.GetValue(null));
            oldGasInitialized = (bool)gasInitialized.GetValue(null);
            GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,
                "Resources/Content/Data/GasDefinitions"), "*.json").Select(File.ReadAllText));
            factions = new ComparisonFactionScope();
            oldReactions = new List<TileReactionSystem.TileReaction>((List<TileReactionSystem.TileReaction>)Reactions.GetValue(null));
            oldReactionsInitialized = TileReactionSystem.IsInitialized;
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/TileReactions/Reactions.json")));
            SkillRegistry.ResetForTests();
        }
        [TearDown] public void ComparisonCleanup()
        {
            gasRng.SetValue(null, oldGasRng);
            var registry = (Dictionary<string, GasDefinition>)gases.GetValue(null); registry.Clear();
            foreach (var pair in oldGases) registry.Add(pair.Key, pair.Value);
            gasInitialized.SetValue(null, oldGasInitialized); SkillRegistry.ResetForTests();
            factions?.Dispose();
            TileReactionSystem.ResetForTests(); ((List<TileReactionSystem.TileReaction>)Reactions.GetValue(null)).AddRange(oldReactions);
            ReactionsInitialized.SetValue(null, oldReactionsInitialized);
        }

        int comparisonLastSeed = 120;
        string comparisonReceiptSuffix = "";
        string comparisonRevisionTag = "";
        Func<string,string,string,string,bool> comparisonFilter;
        bool IncludeComparison(string family,string variant,string package,string policy)
            => comparisonFilter == null || comparisonFilter(family,variant,package,policy);

        // One prospectively selected precision extension, not a balance revision or retry
        // until a favorable outcome. Reuses every existing command policy.
        [TestCase("W1")] [TestCase("W4")] [TestCase("W4-reversal")] [TestCase("W5")]
        public void A3D12SelectedPrimaryComparisonExtendsOnceTo100Seeds(string family)
        {
            comparisonLastSeed=200; comparisonReceiptSuffix="-precision100"+comparisonRevisionTag;
            comparisonFilter=(world,variant,package,policy)=>
            {
                if(world!=family)return false;
                if(world=="W1")return (variant=="Viper/AV0/daggers2"||variant=="MarlbackScrabbler/AV0/daggers2"
                    ||variant=="Viper/AV4/daggers1"||variant=="MarlbackScrabbler/AV4/daggers1")
                    &&(package=="bank"&&policy=="ordinary"||package=="root"||package=="measured"||package=="puncture");
                if(world=="W4")return (variant=="gas"||variant=="dry"||variant=="wall"||variant=="brace")
                    &&(package=="root"||package=="driving");
                if(world=="W4-reversal")return package=="root"||package=="driving";
                return policy=="secure"&&(package=="bank"||package=="recovery");
            };
            try
            {
                if(family=="W1")A3W1PursuitUsesRealEnemyTurnsScarcityAndArmor(2);
                else if(family=="W4")A3W4DrivingUsesRealEntryAndEnemyResponses(2);
                else if(family=="W4-reversal")A3W4DrivingCanRescueAnEnemyFromTheExistingGasLane(2);
                else A3W5RecoveryCompetesWithGroundReuseOrdinaryPickupAndRealDog(2);
            }
            finally{comparisonLastSeed=120;comparisonReceiptSuffix="";comparisonFilter=null;}
        }

        [TestCase(0)] [TestCase(2)] [TestCase(4)] [TestCase(6)]
        public void MeasuredRevisionOneReplaysAllFrozenW1Conditions(int budget)
        {
            comparisonReceiptSuffix="-measured-r1";
            try{A3W1PursuitUsesRealEnemyTurnsScarcityAndArmor(budget);}
            finally{comparisonReceiptSuffix="";}
        }
        [Test] public void MeasuredRevisionOneReplaysSelected100SeedConditions()
        {
            comparisonRevisionTag="-measured-r1";
            try{A3D12SelectedPrimaryComparisonExtendsOnceTo100Seeds("W1");}
            finally{comparisonRevisionTag="";}
        }

        [Test]
        public void A3InactiveThrowingPurchaseCannotChangeSameSeedMeleeAndFiniteHealing()
        {
            for(int seed=101;seed<=120;seed++)
            {
                ThrowComparisonReceipt baseline=null;
                foreach(string package in new[]{"bank","measured"})
                {
                    var w=new ThrowingComparisonWorld(Factory,seed,2,package,"rng-control","no-throws","melee-heal");
                    var enemy=w.Enemy("MarlbackScrabbler",5,5);
                    w.Enemy("Viper",8,6);w.Start();
                    for(int action=0;action<25&&w.Alive;action++)
                    {
                        var healing=w.Item("HealingTonic");
                        if(w.Player.GetStatValue("Hitpoints")<24&&healing!=null)
                            w.Do("drink-finite-healing",()=>InventorySystem.PerformAction(w.Player,healing,"ApplyTonic",w.Zone));
                        else
                        {
                            var current=w.Enemies.Where(e=>e.GetStatValue("Hitpoints")>0&&w.Zone.GetEntityCell(e)!=null)
                                .OrderBy(w.Distance).ThenBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
                            if(current==null)break;
                            if(w.Distance(current)<=1)w.Melee(current);else w.Wait();
                        }
                    }
                    var receipt=w.Finish("same-script");
                    Assert.True(receipt.commands.Any(c=>c.StartsWith("drink-finite-healing:paid")),"Control must exercise the old unseeded self-use path.");
                    if(baseline==null)baseline=receipt;
                    else
                    {
                        Assert.AreEqual(baseline.hp,receipt.hp,"Inactive throwing skills cannot alter melee/healing: seed="+seed);
                        CollectionAssert.AreEqual(baseline.commands,receipt.commands);
                        CollectionAssert.AreEqual(baseline.afterActions,receipt.afterActions);
                        CollectionAssert.AreEqual(baseline.enemies,receipt.enemies);
                        Assert.AreEqual(baseline.finalSupply,receipt.finalSupply);
                    }
                }
            }
        }

        [TestCase(0)] [TestCase(2)] [TestCase(4)] [TestCase(6)]
        public void A3PackagesUseActualPricesAndPreserveIdenticalStartingSupply(int budget)
        {
            var receipts = new List<ThrowComparisonReceipt>(); string baseline = null;
            foreach (var package in ThrowingComparisonWorld.Packages(budget))
            {
                var w = new ThrowingComparisonWorld(Factory, 101, budget, package, "packages", "prices", "purchase");
                if (baseline == null) baseline = w.Receipt.initialSupply;
                Assert.AreEqual(baseline, w.Receipt.initialSupply);
                Assert.True(w.Has("Hydromancy_DrenchLob")); Assert.True(w.Has("Cryomancy_ColdSnap"));
                Assert.True(w.Has("Spellcraft_Calm"));
                Assert.False(w.Receipt.purchases.Any(s => s.StartsWith("Hydromancy_DrenchLob:")), "Do not charge for starter grants.");
                Assert.AreEqual(budget, w.Receipt.spent + w.Receipt.banked);
                receipts.Add(w.Finish("purchased"));
            }
            ThrowingComparisonWorld.SaveReceipts("packages-" + budget, receipts);
        }

        [TestCase(0)] [TestCase(2)] [TestCase(4)] [TestCase(6)]
        public void A3W1PursuitUsesRealEnemyTurnsScarcityAndArmor(int budget)
        {
            var receipts = new List<ThrowComparisonReceipt>();
            string[] packages = budget == 0 ? new[] { "bank" }
                : budget == 2 ? new[] { "bank", "root", "measured", "dodge", "puncture", "jet" }
                : budget == 4 ? new[] { "bank", "physical4", "measured-jet", "blades4" }
                : new[] { "bank", "all5-water", "physical-dodge", "water-acrobatics", "blades4" };
            foreach (string creature in new[] { "MarlbackScrabbler", "Viper" })
            foreach (int armor in new[] { 0, 4, 8 })
            foreach (int supply in new[] { 1, 2 })
            foreach (string package in packages)
            foreach (string policy in package == "bank" ? new[] { "ordinary", "melee", "free-cold", "retreat" } : new[] { "owned-opportunity" })
            for (int seed = 101; seed <= comparisonLastSeed; seed++)
            {
                if(!IncludeComparison("W1",creature+"/AV"+armor+"/daggers"+supply,package,policy))continue;
                var w = new ThrowingComparisonWorld(Factory, seed, budget, package, "A3-W1",
                    creature + "/AV" + armor + "/daggers" + supply, policy);
                var dagger = w.Item("Dagger"); if (supply == 2) w.AddSupply("Dagger");
                var enemy = w.Enemy(creature, 7, 5, armor); w.Start();
                if (policy == "retreat") w.Move(-1, 0);
                else
                {
                    bool prepared = false, castCold = false;
                    while (w.Alive && enemy.GetStatValue("Hitpoints") > 0 && w.Receipt.actions < 30)
                    {
                        int distance = w.Distance(enemy); bool acted = false;
                        var missile = w.Item("Dagger") ?? w.Zone.GetReadOnlyEntities().FirstOrDefault(e => e.BlueprintName == "Dagger"
                            && e.GetPart<PhysicsPart>()?.InInventory == null && SpatialQuery.Distance(w.Zone, w.Player, e) <= 1);
                        if (policy == "owned-opportunity" && !prepared && w.Has(ThrowingComparisonWorld.Measured)
                            && missile != null && distance > 1)
                        { prepared = true; acted = w.Prepare(missile, enemy); }
                        else if (policy == "free-cold" && !castCold && distance <= 2)
                        { castCold = true; acted = w.Cast("CommandColdSnap"); }
                        else if (policy == "owned-opportunity" && w.Has("Hydromancy_JetBlast") && distance <= 2)
                            acted = w.Cast("CommandJetBlast", enemy);
                        if (!acted && policy == "owned-opportunity" && w.Has("ShortBlades_Flurry") && distance == 1)
                            acted = w.Cast("CommandFlurry", enemy);
                        // The melee/defense alternatives retain their actual
                        // equipped blade so their purchased passives/actives can
                        // contribute. Only the declared ordinary-throw baseline
                        // and packages buying Throwing choose this ranged policy.
                        if (!acted && distance > 1 && (policy == "ordinary"
                            || policy == "owned-opportunity" && w.Has(ThrowingComparisonWorld.Root)) && missile != null)
                            acted = w.Throw(missile, enemy);
                        if (!acted && distance <= 1) acted = w.Melee(enemy);
                        if (!acted) w.Wait();
                    }
                    // The objective includes carrying the actual weapon out.
                    // A living early retreat is reported separately from victory.
                    for (int n = 0; w.Alive && enemy.GetStatValue("Hitpoints") <= 0 && !w.Pack.Contains(dagger) && n < 12; n++)
                    {
                        var cell = w.Zone.GetEntityCell(dagger); if (cell == null) break;
                        if (w.Has(ThrowingComparisonWorld.Recovery) && w.Recover(dagger)) continue;
                        if (w.Pickup(dagger)) continue;
                        if (!Toward(w, cell.X, cell.Y)) break;
                    }
                    for (int n = 0; w.Alive && enemy.GetStatValue("Hitpoints") <= 0 && w.Pack.Contains(dagger)
                        && w.Position(w.Player) != "1,5" && n < 14; n++) if (!Toward(w, 1, 5)) break;
                }
                string outcome = !w.Alive ? "death" : enemy.GetStatValue("Hitpoints") <= 0 && w.Pack.Contains(dagger)
                    && w.Position(w.Player) == "1,5" ? "objective-and-weapon" : policy == "retreat" ? "early-retreat" : "partial-or-timeout";
                receipts.Add(w.Finish(outcome, dagger));
                if (w.Receipt.actions > 1 && enemy.GetStatValue("Hitpoints") > 0)
                    Assert.Greater(w.Receipt.enemyTurns, 0, "A surviving actual brain must act between the player's actions.");
            }
            Assert.Greater(receipts.Count,0,"Selected comparison cannot silently run zero trials.");
            ThrowingComparisonWorld.SaveReceipts("W1-" + budget + comparisonReceiptSuffix, receipts);
        }

        [TestCase(0)] [TestCase(2)] [TestCase(4)] [TestCase(6)]
        public void A3W2ClutterClearanceIsUniversalAndIntentRemainsExplicit(int budget)
        {
            var receipts = new List<ThrowComparisonReceipt>();
            foreach (string package in budget == 0 ? new[] { "bank" } : new[] { "bank", "measured" })
            foreach (string variant in new[] { "floor-body", "floor-low", "floor-ground", "crate", "closed-door", "open-door", "two-owners", "secondary-body" })
            for (int seed = 101; seed <= 120; seed++)
            {
                var w = new ThrowingComparisonWorld(Factory, seed, budget, package, "A3-W2", variant, "ordinary-throw");
                var floor = w.Spawn("Dagger", 4, 5); var enemy = w.Enemy("MarlbackScrabbler", 7, 5);
                Entity obstruction = null;
                if (variant == "crate") obstruction = w.Spawn("Crate", 5, 5);
                if (variant.Contains("door"))
                {
                    obstruction = new Entity { ID = "controlled-door" }; obstruction.AddPart(new PhysicsPart());
                    obstruction.AddPart(new DoorPart { IsOpen = variant == "open-door" });
                    Assert.True(w.Zone.AddEntity(obstruction, 5, 5));
                }
                if (variant == "two-owners") obstruction = w.Enemy("Viper", 6, 5);
                if (variant == "secondary-body")
                {
                    w.Zone.RemoveEntity(enemy); enemy.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;0,1" });
                    Assert.True(w.Zone.AddEntity(enemy, 7, 4));
                }
                var intent = variant == "floor-low" ? ThrowIntent.AtEntity(floor, 4, 5)
                    : variant == "floor-ground" ? ThrowIntent.Ground(4, 5) : ThrowIntent.AtEntity(enemy, 7, 5);
                var dagger = w.Item("Dagger");
                var trace = ThrowTrajectory.Trace(w.Zone, w.Player, dagger, intent, HandlingService.GetThrowRange(w.Player, dagger));
                Entity expected = variant == "floor-low" || variant == "floor-ground" ? floor
                    : obstruction != null && variant != "open-door" ? obstruction : enemy;
                Assert.AreSame(expected, trace.HitEntity, variant + " must not need a purchased geometry unlock.");
                w.Start(); Assert.True(w.ThrowAt(dagger, intent));
                receipts.Add(w.Finish("committed-" + expected.BlueprintName, dagger));
            }
            ThrowingComparisonWorld.SaveReceipts("W2-" + budget, receipts);
        }

        [TestCase(0)] [TestCase(2)] [TestCase(4)] [TestCase(6)]
        public void A3W4DrivingUsesRealEntryAndEnemyResponses(int budget)
        {
            var receipts = new List<ThrowComparisonReceipt>();
            string[] packages = budget == 0 ? new[] { "bank" } : budget == 2 ? new[] { "bank", "root", "driving", "jet" }
                : budget == 4 ? new[] { "bank", "root", "physical4", "water-tumble" } : new[] { "bank", "root", "all5-water", "water-acrobatics" };
            foreach (string package in packages)
            foreach (string variant in new[] { "dry", "water", "acid", "ice", "gas", "wall", "brace" })
            for (int seed = 101; seed <= comparisonLastSeed; seed++)
            {
                if(!IncludeComparison("W4",variant,package,"lane-control"))continue;
                var w = new ThrowingComparisonWorld(Factory, seed, budget, package, "A3-W4", variant, "lane-control");
                var cudgel = w.AddSupply("Cudgel"); Assert.AreEqual(8, HandlingService.GetWeight(cudgel));
                var enemy = w.Enemy("MarlbackScrabbler", 6, 5, 0);
                if (variant == "water") w.Spawn("WaterPuddle", 7, 5);
                if (variant == "acid") w.Spawn("AcidPool", 7, 5);
                if (variant == "ice") w.Zone.TileState.WriteCoating(7, 5, "ice", 12);
                if (variant == "gas") Assert.NotNull(GasFactory.SpawnGas(w.Zone, 7, 5, "poison-vapor", 20, 1));
                if (variant == "wall") w.Spawn("StoneWall", 7, 5);
                if (variant == "brace")
                {
                    w.Spawn("Grass", 6, 5); var boots = Factory.CreateEntity("IronshodBoots");
                    Assert.True(enemy.GetPart<InventoryPart>().AddObject(boots)); Assert.True(InventorySystem.Equip(enemy, boots));
                    var choice = InventorySystem.GetActions(enemy, boots).Single(a => a.Command.StartsWith("BraceBoots|"));
                    Assert.True(InventorySystem.PerformAction(enemy, boots, choice.Command, w.Zone));
                    Assert.NotNull(enemy.GetEffect<EquipmentBraceEffect>());
                }
                w.Start();
                if (w.Has("Hydromancy_JetBlast") && !w.Has(ThrowingComparisonWorld.Driving))
                {
                    // Its shorter cone pays real approach actions; no test-only
                    // range extension or teleport makes the comparison equal.
                    while (w.Alive && w.Distance(enemy) > 2 && w.Receipt.actions < 4) if (!w.Move(1, 0)) break;
                    w.Cast("CommandJetBlast", enemy);
                }
                else Assert.True(w.Throw(cudgel, enemy, w.Has(ThrowingComparisonWorld.Driving) ? ThrowMode.Driving : ThrowMode.Ordinary));
                if (w.Alive) w.Wait();
                if (w.Alive) w.Move(-1, 0);
                receipts.Add(w.Finish(!w.Alive ? "death" : enemy.GetStatValue("Hitpoints") <= 0
                    ? "enemy-defeated-then-withdraw" : "control-then-withdraw", cudgel));
                // A legal first-throw kill may remove the enemy before its first
                // turn. Do not fabricate a response to keep the metric positive.
                if (enemy.GetStatValue("Hitpoints") > 0)
                    Assert.Greater(w.Receipt.enemyTurns, 0, "A surviving enemy must receive real scheduled responses.");
            }
            Assert.Greater(receipts.Count,0,"Selected comparison cannot silently run zero trials.");
            ThrowingComparisonWorld.SaveReceipts("W4-" + budget + comparisonReceiptSuffix, receipts);
        }

        [TestCase(0)] [TestCase(2)] [TestCase(4)] [TestCase(6)]
        public void A3W5RecoveryCompetesWithGroundReuseOrdinaryPickupAndRealDog(int budget)
        {
            var receipts = new List<ThrowComparisonReceipt>();
            string[] packages = budget == 0 ? new[] { "bank" } : budget == 2 ? new[] { "bank", "recovery", "dodge" }
                : budget == 4 ? new[] { "bank", "physical4", "blades4" } : new[] { "bank", "all5-water", "water-acrobatics" };
            foreach (string package in packages)
            foreach (string variant in new[] { "quiet", "pursuit", "acid", "free-dog", "busy-dog" })
            foreach (string policy in new[] { "secure", "ground-rethrow", "abandon" })
            for (int seed = 101; seed <= comparisonLastSeed; seed++)
            {
                if(!IncludeComparison("W5",variant,package,policy))continue;
                var w = new ThrowingComparisonWorld(Factory, seed, budget, package, "A3-W5", variant, policy);
                Entity dog = null;
                if (variant.Contains("dog"))
                {
                    dog = w.Spawn("PetDog", 5, 4); var brain = dog.GetPart<BrainPart>();
                    brain.CurrentZone = w.Zone; brain.Rng = new System.Random(seed);
                    w.Clock.AddEntity(dog); Assert.True(FactionManager.IsAllied(dog, w.Player));
                }
                var enemy = variant == "pursuit" ? w.Enemy("MarlbackScrabbler", 9, 5) : null;
                if (variant == "acid") w.Spawn("AcidPool", 4, 5);
                w.Start();
                if (variant == "busy-dog")
                {
                    var bone = w.AddSupply("Bone");
                    var fetchIntent = ThrowIntent.Ground(8, 8);
                    var fetchTrace = ThrowTrajectory.Trace(w.Zone, w.Player, bone, fetchIntent,
                        HandlingService.GetThrowRange(w.Player, bone));
                    Assert.AreNotSame(dog, fetchTrace.HitEntity, "The setup throws past the dog, not at it.");
                    Assert.True(w.ThrowAt(bone, fetchIntent));
                    Assert.True(FactionManager.IsAllied(dog, w.Player), "The paid setup must preserve the real retriever relationship.");
                    Assert.NotNull(dog.GetPart<BrainPart>().FindGoal<GoFetchGoal>(), "A genuinely busy dog needs an actual pending fetch.");
                }
                var dagger = w.Item("Dagger"); Assert.True(w.ThrowAt(dagger, ThrowIntent.Ground(4, 5)));
                Assert.True(w.Move(1, 0));
                if (policy == "abandon") w.Move(-1, 0);
                else if (policy == "ground-rethrow")
                {
                    // Accessible floor throwing is universal and has a different
                    // objective from securing portable custody.
                    if (enemy != null) w.Throw(dagger, enemy);
                    else w.ThrowAt(dagger, ThrowIntent.Ground(6, 5));
                }
                else
                {
                    if (w.Has(ThrowingComparisonWorld.Recovery)) w.Recover(dagger);
                    for (int n = 0; w.Alive && !w.Pack.Contains(dagger) && n < 12; n++)
                    {
                        var cell = w.Zone.GetEntityCell(dagger);
                        if (cell == null) { w.Wait(); continue; } // a real dog may own it
                        if (w.Pickup(dagger)) continue;
                        if (!Toward(w, cell.X, cell.Y)) w.Wait();
                    }
                    for (int n = 0; w.Alive && w.Pack.Contains(dagger) && w.Position(w.Player) != "1,5" && n < 10; n++)
                        if (!Toward(w, 1, 5)) break;
                }
                receipts.Add(w.Finish(!w.Alive ? "death" : w.Pack.Contains(dagger) && w.Position(w.Player) == "1,5"
                    ? "portable-custody-and-extraction" : policy == "secure" ? "partial" : policy, dagger));
                Assert.False(dagger.GetPart<PhysicsPart>()?.Equipped == w.Player, "Neither recovery nor the comparison's explicit carry-only pickup equips.");
            }
            Assert.Greater(receipts.Count,0,"Selected comparison cannot silently run zero trials.");
            ThrowingComparisonWorld.SaveReceipts("W5-" + budget + comparisonReceiptSuffix, receipts);
        }

        // The same W4 gas lane can make force a mistake: rescuing a target
        // from its current cloud avoids the imminent real round dose. No new
        // enemy bonus, AI override or supply is created for this countercondition.
        [TestCase(2)] [TestCase(4)] [TestCase(6)]
        public void A3W4DrivingCanRescueAnEnemyFromTheExistingGasLane(int budget)
        {
            var receipts = new List<ThrowComparisonReceipt>();
            string candidate = budget == 2 ? "driving" : budget == 4 ? "physical4" : "all5-water";
            int harmfulPushes = 0;
            foreach (string variant in new[] { "dry-control", "gas-at-current-position" })
            for (int seed = 101; seed <= comparisonLastSeed; seed++)
            {
                int ordinaryHp = -1;
                foreach (string package in new[] { "root", candidate })
                {
                    if(!IncludeComparison("W4-reversal",variant,package,"existing-gas-lane"))continue;
                    var w = new ThrowingComparisonWorld(Factory, seed, budget, package,
                        "A3-W4-reversal", variant, "existing-gas-lane");
                    var cudgel = w.AddSupply("Cudgel");
                    Assert.AreEqual(8, HandlingService.GetWeight(cudgel));
                    var enemy = w.Enemy("MarlbackScrabbler", 6, 5, 0);
                    if (variant == "gas-at-current-position")
                        Assert.NotNull(GasFactory.SpawnGas(w.Zone, 6, 5, "poison-vapor", 20, 1));
                    w.Start();
                    bool driving = w.Has(ThrowingComparisonWorld.Driving);
                    Assert.True(w.Throw(cudgel, enemy, driving ? ThrowMode.Driving : ThrowMode.Ordinary));
                    // Capture after the paid throw and genuine first enemy response.
                    // Subsequent retreat cannot retroactively change this comparison.
                    int enemyHpAfterResponse = enemy.GetStatValue("Hitpoints");
                    if (!driving) ordinaryHp = enemyHpAfterResponse;
                    else if (variant == "dry-control")
                        Assert.AreEqual(ordinaryHp, enemyHpAfterResponse,
                            "Dry control: same root accuracy and damage, no invented penalty for pushing.");
                    else if (enemyHpAfterResponse > ordinaryHp)
                        harmfulPushes++;
                    if (w.Alive) w.Move(-1, 0);
                    receipts.Add(w.Finish(enemy.GetStatValue("Hitpoints") <= 0
                        ? "enemy-defeated-then-withdraw" : "enemy-survives-withdrawal", cudgel));
                    if (driving) Assert.Greater(w.Receipt.drivingCooldown, 0,
                        "Rescuing the enemy still spends the selected ability's real cooldown.");
                }
            }
            Assert.Greater(receipts.Count,0,"Selected comparison cannot silently run zero trials.");
            ThrowingComparisonWorld.SaveReceipts("W4-reversal-" + budget + comparisonReceiptSuffix, receipts);
            Assert.Greater(harmfulPushes, 0,
                "At least one ordinary same-world condition must leave the enemy better off after Driving.");
        }

        static bool Toward(ThrowingComparisonWorld w, int x, int y)
        {
            var cell = w.Zone.GetEntityCell(w.Player); if (cell == null) return false;
            int dx = Math.Sign(x - cell.X), dy = Math.Sign(y - cell.Y);
            if (dx == 0 && dy == 0) return false;
            if (w.Move(dx, dy)) return true;
            if (dy == 0 && w.Move(dx, 1)) return true;
            return dx != 0 && w.Move(0, dy == 0 ? -1 : dy);
        }
    }
}
