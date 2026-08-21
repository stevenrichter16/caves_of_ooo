using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W3.4 (Docs/FELLING-W3-PLAN.md §3) — the Sodden's own bestiary.
    /// The bog stops borrowing the jungle's roster: its danger is
    /// patient (nothing chases far; the toads wait, the greatdew waits
    /// better), its forage is oil on legs, and its strangest resident
    /// explains nothing (gate 7: the Gin Frogs get zero explanatory
    /// text, pinned here permanently).
    ///
    /// <para>Verify-first results recorded: the on-being-hit reflect
    /// pattern SHIPS (ScaldingVeil's OnTakeDamage; CausticSkinPart is
    /// the part-shaped sibling, born with the reflect-recursion guard
    /// the effect lacks). The Greatdew's adjacency-grab does NOT ship —
    /// per R4 it rides the step-trigger substrate instead: non-solid,
    /// you walk into the dew itself.</para>
    /// </summary>
    public class SoddenBestiaryTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        [TearDown]
        public void TearDown()
        {
            CausticSkinPart.TestRng = null;
            GreatdewSnarePart.TestRng = null;
            SettlementRuntime.ActiveZone = null;
        }

        private static Entity Attacker(Zone zone, int x, int y, int hp = 30)
        {
            var e = new Entity { ID = "atk" + x + "_" + y, BlueprintName = "Snapjaw" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "attacker" });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void Strike(Entity victim, Entity attacker, Zone zone, int amount = 3)
        {
            var d = new Damage(amount);
            d.AddAttribute("Slashing");
            CombatSystem.ApplyDamage(victim, d, attacker, zone);
        }

        // ════════════════════════════════════════════════════════
        // The tables — the Sodden stops borrowing
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheSodden_HasItsOwnTables_AtEveryTier()
        {
            Assert.AreEqual("SoddenTier1", PopulationTable.GetBiomeTable(BiomeType.Sodden, 1).Name);
            Assert.AreEqual("SoddenTier2", PopulationTable.GetBiomeTable(BiomeType.Sodden, 2).Name);
            Assert.AreEqual("SoddenTier3", PopulationTable.GetBiomeTable(BiomeType.Sodden, 3).Name);
            Assert.AreEqual("SoddenLairGuards", PopulationTable.LairGuards(BiomeType.Sodden).Name);
        }

        [Test]
        public void TheUnmigratedBiomes_StillBorrow()
        {
            // Counter-check on the tier-switch edits: only the migrated
            // biomes changed. Re-baselined in W4.3 (the Grovelands grew
            // its own tables); the Stump borrows Cave until W6.
            Assert.AreEqual("GrovelandsTier1",
                PopulationTable.GetBiomeTable(BiomeType.Grovelands, 1).Name);
            Assert.AreEqual("CaveTier1",
                PopulationTable.GetBiomeTable(BiomeType.Stump, 1).Name);
        }

        [Test]
        public void EverySoddenTableEntry_IsARealBlueprint()
        {
            // The fail-loud content gate (the "Waterskin" lesson from
            // W2.2): a table naming a ghost blueprint fails HERE, not
            // silently at zone-population time.
            var tables = new[]
            {
                PopulationTable.GetBiomeTable(BiomeType.Sodden, 1),
                PopulationTable.GetBiomeTable(BiomeType.Sodden, 2),
                PopulationTable.GetBiomeTable(BiomeType.Sodden, 3),
                PopulationTable.LairGuards(BiomeType.Sodden),
            };
            foreach (var table in tables)
                foreach (var entry in table.Entries)
                    Assert.IsNotNull(_factory.CreateEntity(entry.BlueprintName),
                        $"{table.Name} names '{entry.BlueprintName}', which does not exist");
        }

        [Test]
        public void NoJungleLegacyCreature_HauntsTheSoddenTables()
        {
            // The retrofit the overhaul exists to undo: rotlings and
            // giant spiders in the peat.
            var legacy = new HashSet<string> { "Rotling", "GiantSpider", "JungleStalker" };
            foreach (int tier in new[] { 1, 2, 3 })
                foreach (var entry in PopulationTable.GetBiomeTable(BiomeType.Sodden, tier).Entries)
                    Assert.IsFalse(legacy.Contains(entry.BlueprintName),
                        $"tier {tier} still carries {entry.BlueprintName}");
        }

        // ════════════════════════════════════════════════════════
        // Gate 7 — the Gin Frogs explain nothing
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheGinFrog_ExplainsNothing()
        {
            // Canon gate 7: no shipped text may explain the Gin Frogs.
            // The blueprint carries NO examine copy at all — "You see a
            // gin frog." is the entire record. This pin makes adding
            // explanatory text a test failure, whatever feature tries.
            var frog = _factory.CreateEntity("GinFrog");
            var exam = frog.GetPart<ExaminablePart>();
            Assert.IsTrue(exam == null || string.IsNullOrEmpty(exam.Text),
                "the gin frog must not explain itself");
        }

        // ════════════════════════════════════════════════════════
        // The bandfrog's skin
        // ════════════════════════════════════════════════════════

        [Test]
        public void StrikingABandfrog_Burns()
        {
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var frog = _factory.CreateEntity("Bandfrog");
            zone.AddEntity(frog, 10, 10);
            var attacker = Attacker(zone, 11, 10);

            Strike(frog, attacker, zone);

            Assert.AreEqual(28, attacker.GetStatValue("Hitpoints", 0),
                "2 acid back — the lesson is the skin");
        }

        [Test]
        public void ASpellFromAcrossTheMarsh_IsNotContact()
        {
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var frog = _factory.CreateEntity("Bandfrog");
            zone.AddEntity(frog, 10, 10);
            var caster = Attacker(zone, 18, 10);

            Strike(frog, caster, zone);

            Assert.AreEqual(30, caster.GetStatValue("Hitpoints", 0),
                "no contact, no lesson");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "SkinContactRejected", Limit = 10 }).Records.Count,
                "the rejecting gate names its reason");
        }

        [Test]
        public void EnvironmentalDamage_HasNoOneToAnswer()
        {
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var frog = _factory.CreateEntity("Bandfrog");
            zone.AddEntity(frog, 10, 10);

            Assert.DoesNotThrow(() =>
            {
                var d = new Damage(3);
                d.AddAttribute("Fire");
                CombatSystem.ApplyDamage(frog, d, null, zone);
            });
        }

        [Test]
        public void TwoBandfrogs_DoNotPingPongToDeath()
        {
            // The recursion guard: the reflected damage carries the
            // SkinContact attribute and skin never answers skin. Without
            // the guard this test dies by stack overflow or mutual HP
            // drain; with it, exactly one reflect fires.
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var a = _factory.CreateEntity("Bandfrog");
            var b = _factory.CreateEntity("Bandfrog");
            zone.AddEntity(a, 10, 10);
            zone.AddEntity(b, 11, 10);
            int aHp = a.GetStatValue("Hitpoints", 0);
            int bHp = b.GetStatValue("Hitpoints", 0);

            Strike(b, a, zone);   // a bites b

            Assert.AreEqual(bHp - 3, b.GetStatValue("Hitpoints", 0), "the bite lands");
            Assert.AreEqual(aHp - 2, a.GetStatValue("Hitpoints", 0),
                "one reflect — b's skin answers a's bite and the chain STOPS");
        }

        [Test]
        public void PoisonChanceBoundaries_HoldAtZeroAndHundred()
        {
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            foreach (var (chance, expectPoison) in new[] { (0, false), (100, true) })
            {
                var frog = new Entity { ID = "bf" + chance, BlueprintName = "Bandfrog" };
                frog.Tags["Creature"] = "";
                frog.AddPart(new RenderPart { DisplayName = "bandfrog" });
                frog.Statistics["Hitpoints"] = new Stat
                { Owner = frog, Name = "Hitpoints", BaseValue = 9, Min = 0, Max = 9 };
                frog.AddPart(new CausticSkinPart { PoisonChance = chance });
                zone.AddEntity(frog, 5, 5 + chance / 50);
                var attacker = Attacker(zone, 6, 5 + chance / 50);

                Strike(frog, attacker, zone);

                Assert.AreEqual(expectPoison, attacker.HasEffect<PoisonedEffect>(),
                    $"chance {chance}: poison boundary broken");
            }
        }

        [Test]
        public void AnAdjacentArsonist_IsNotTouchingTheFrog()
        {
            // W3.7 hypothesis audit (H4, confirmed RED pre-fix): a
            // burning bandfrog's tick damage carries Source = the
            // arsonist; standing adjacent, every tick read as "contact"
            // and seared them. Fire is not touch: elemental damage never
            // triggers the skin, whoever lit it and wherever they stand.
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var frog = _factory.CreateEntity("Bandfrog");
            zone.AddEntity(frog, 10, 10);
            var arsonist = Attacker(zone, 11, 10);

            var d = new Damage(3);
            d.AddAttribute("Fire");
            CombatSystem.ApplyDamage(frog, d, arsonist, zone);

            Assert.AreEqual(30, arsonist.GetStatValue("Hitpoints", 0),
                "the fire is touching the frog; the arsonist is not");
        }

        [Test]
        public void Adversarial_LightningIsNotTouchEither()
        {
            // W3 re-review (RED pre-fix): the elemental gate was six
            // exact strings, but the codebase's own damage model treats
            // Lightning/Shock/Electricity as aliases of Electric
            // (DamageAttributeFlags). ElectrifiedEffect ticks carry ONLY
            // "Lightning" — the moment its planned source-threading
            // lands, an adjacent electrified attacker would reopen the
            // exact H4 bug for electricity. The gate now reads the
            // alias-collapsing flag helpers.
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var frog = _factory.CreateEntity("Bandfrog");
            zone.AddEntity(frog, 10, 10);
            var attacker = Attacker(zone, 11, 10);

            var d = new Damage(3);
            d.AddAttribute("Lightning");
            CombatSystem.ApplyDamage(frog, d, attacker, zone);

            Assert.AreEqual(30, attacker.GetStatValue("Hitpoints", 0),
                "lightning arrives as element, not as touch — whatever it is called");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "SkinContactRejected", Limit = 10 }).Records.Count,
                "and the rejecting gate names its reason, like its adjacency sibling");
        }

        [Test]
        public void AReflectThatKills_DoesNotPoisonTheCorpse()
        {
            // W3.7 hypothesis audit (H12, pinned-as-correct): the
            // reflect lands mid-dispatch; if it kills the attacker, the
            // poison roll must see the corpse and stand down.
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var frog = new Entity { ID = "bf", BlueprintName = "Bandfrog" };
            frog.Tags["Creature"] = "";
            frog.AddPart(new RenderPart { DisplayName = "bandfrog" });
            frog.Statistics["Hitpoints"] = new Stat
            { Owner = frog, Name = "Hitpoints", BaseValue = 9, Min = 0, Max = 9 };
            frog.AddPart(new CausticSkinPart { PoisonChance = 100 });
            zone.AddEntity(frog, 10, 10);
            var dying = Attacker(zone, 11, 10, hp: 1);

            Assert.DoesNotThrow(() => Strike(frog, dying, zone));
            Assert.LessOrEqual(dying.GetStatValue("Hitpoints", 0), 0, "the skin finished it");
            Assert.IsFalse(dying.HasEffect<PoisonedEffect>(),
                "no venom spent on the dead");
        }

        // ════════════════════════════════════════════════════════
        // The greatdew waits
        // ════════════════════════════════════════════════════════

        private (Zone zone, Entity dew, Entity walker) DewCrossing(int grabChance)
        {
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var dew = _factory.CreateEntity("Greatdew");
            dew.GetPart<GreatdewSnarePart>().GrabChance = grabChance;
            zone.AddEntity(dew, 11, 10);
            var walker = Attacker(zone, 10, 10);
            return (zone, dew, walker);
        }

        [Test]
        public void WalkingIntoTheDew_AtCertainGrab_HoldsAndBurns()
        {
            var (zone, dew, walker) = DewCrossing(grabChance: 100);

            Assert.IsTrue(MovementSystem.TryMove(walker, zone, 1, 0),
                "the dew is not solid; the step succeeds — that is the trap");
            Assert.IsTrue(walker.HasEffect<RootedEffect>(), "held");
            Assert.IsTrue(walker.HasEffect<AcidicEffect>(), "and burning, slowly");
            Assert.IsNotNull(zone.GetEntityCell(dew),
                "a plant is not a mine — it holds, releases, and waits again");
        }

        [Test]
        public void AtZeroGrab_TheWalkerBrushesThrough()
        {
            var (zone, _, walker) = DewCrossing(grabChance: 0);

            MovementSystem.TryMove(walker, zone, 1, 0);

            Assert.IsFalse(walker.HasEffect<RootedEffect>());
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = "SnareBrushed", Limit = 10 }).Records.Count);
        }

        [Test]
        public void StillnessPasses_TheHoldExpiresOnItsOwn()
        {
            // R4's design line: NOT attacking lets the hold lapse. The
            // hold is RootedEffect's ordinary duration — wait it out.
            var (zone, _, walker) = DewCrossing(grabChance: 100);
            MovementSystem.TryMove(walker, zone, 1, 0);
            Assert.IsFalse(MovementSystem.TryMove(walker, zone, 1, 0),
                "held things do not walk");

            for (int t = 0; t < 5; t++)
            {
                var ev = GameEvent.New("EndTurn");
                ev.SetParameter("Zone", (object)zone);
                walker.FireEvent(ev);
                ev.Release();
            }

            Assert.IsFalse(walker.HasEffect<RootedEffect>(), "stillness passed");
            Assert.IsTrue(MovementSystem.TryMove(walker, zone, 1, 0),
                "and the walker walks away — thinner");
        }

        // ════════════════════════════════════════════════════════
        // The copse is the den
        // ════════════════════════════════════════════════════════

        [Test]
        public void ADrownedCopse_AlwaysHousesItsToads()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                var zone = new Zone("Overworld.16.3.0");
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                    {
                        var g = _factory.CreateEntity("Grass");
                        if (g != null) zone.AddEntity(g, x, y);
                    }
                new SoddenFormationBuilder { Override = Formation.DrownedCopse }
                    .BuildZone(zone, _factory, new Random(seed));

                int toads = 0;
                foreach (var e in zone.GetAllEntities())
                    if (e.BlueprintName == "MawToad") toads++;
                Assert.That(toads, Is.InRange(1, 2),
                    $"seed {seed}: the copse guarantees its residents");
            }
        }

        [Test]
        public void TheReedfrog_IsOilOnLegs()
        {
            var frog = _factory.CreateEntity("Reedfrog");
            Assert.IsTrue(frog.GetPart<BrainPart>().Passive,
                "unbothered until you are close enough to matter");
            var corpse = frog.GetPart<CorpsePart>();
            Assert.AreEqual("FrogOil", corpse.HarvestBlueprint,
                "the peat-cutters render them for lamps");
            Assert.IsNotNull(_factory.CreateEntity("FrogOil"), "and the oil is real");
        }
    }
}
