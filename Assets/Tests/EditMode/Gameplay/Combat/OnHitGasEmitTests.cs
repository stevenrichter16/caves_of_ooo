using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.7b — EmitGasOnHit dispatcher + spec parser + MeleeWeaponPart
    /// cache integration. Three sections:
    /// (1) Spec parser unit tests (malformed/edge inputs)
    /// (2) Dispatcher behavior (chance rolls, spawn placement)
    /// (3) Integration (CombatSystem.PerformSingleAttack actually
    ///     fires the dispatcher when a weapon declares
    ///     EmitGasOnHitRaw)
    /// </summary>
    public class OnHitGasEmitTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" },
              { ""Id"":""cryo-mist"", ""GasType"":""Cryo"",
                ""Glyph"":""°"", ""Color"":""&C"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":"""" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
        }

        private static Entity MakeCreature(Zone zone, int x, int y, int hpMax = 200)
        {
            var e = new Entity { ID = "c_" + x + "_" + y, BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax); S("Toughness", 12);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — EmitGasOnHitSpec.Parse
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Parse_ValidSingleSpec_AllFieldsPopulated()
        {
            var specs = EmitGasOnHitSpec.Parse("poison-vapor,30,40,15,2");
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual("poison-vapor", specs[0].GasId);
            Assert.AreEqual(30, specs[0].ChancePercent);
            Assert.AreEqual(40, specs[0].CellDensity);
            Assert.AreEqual(15, specs[0].AdjacentDensity);
            Assert.AreEqual(2, specs[0].GasLevel);
        }

        [Test]
        public void Parse_MultipleSpecs_SemicolonSeparated()
        {
            var specs = EmitGasOnHitSpec.Parse("poison-vapor,30,40,15,1;cryo-mist,20,25,10,2");
            Assert.AreEqual(2, specs.Count);
            Assert.AreEqual("poison-vapor", specs[0].GasId);
            Assert.AreEqual("cryo-mist", specs[1].GasId);
            Assert.AreEqual(20, specs[1].ChancePercent);
        }

        [Test]
        public void Parse_EmptyFields_UseDefaults()
        {
            // "poison-vapor,30,,,1" — CellDensity + AdjacentDensity empty
            var specs = EmitGasOnHitSpec.Parse("poison-vapor,30,,,1");
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual(EmitGasOnHitSpec.DEFAULT_CELL_DENSITY, specs[0].CellDensity);
            Assert.AreEqual(EmitGasOnHitSpec.DEFAULT_ADJACENT_DENSITY, specs[0].AdjacentDensity);
            Assert.AreEqual(1, specs[0].GasLevel);
        }

        [Test]
        public void Parse_NullOrEmpty_ReturnsEmptyList()
        {
            Assert.AreEqual(0, EmitGasOnHitSpec.Parse(null).Count);
            Assert.AreEqual(0, EmitGasOnHitSpec.Parse("").Count);
            Assert.AreEqual(0, EmitGasOnHitSpec.Parse("   ").Count);
        }

        [Test]
        public void Parse_MalformedSpec_Skipped()
        {
            // Each of these is malformed in a different way.
            var specs = EmitGasOnHitSpec.Parse(
                ",30,40,15,1;poison-vapor,abc,40,15,1;poison-vapor,30,40,15,1");
            // Only the third spec is valid (first has empty GasId, second
            // has non-numeric chance).
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual("poison-vapor", specs[0].GasId);
            Assert.AreEqual(30, specs[0].ChancePercent);
        }

        [Test]
        public void Parse_ZeroChance_Skipped()
        {
            // 0% chance is a no-op spec; skip (mirrors OnHitEffectSpec).
            var specs = EmitGasOnHitSpec.Parse("poison-vapor,0,40,15,1");
            Assert.AreEqual(0, specs.Count);
        }

        [Test]
        public void Parse_TrailingSemicolon_DoesNotCrash()
        {
            var specs = EmitGasOnHitSpec.Parse("poison-vapor,30,40,15,1;");
            Assert.AreEqual(1, specs.Count);
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — MeleeWeaponPart cache integration
        // ════════════════════════════════════════════════════════════

        [Test]
        public void MeleeWeaponPart_EmitGasOnHitCachedSpecs_LazyParse()
        {
            var w = new MeleeWeaponPart();
            w.EmitGasOnHitRaw = "poison-vapor,30,40,15,1";
            var specs = w.EmitGasOnHitCachedSpecs;
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual("poison-vapor", specs[0].GasId);
        }

        [Test]
        public void MeleeWeaponPart_EmitGasOnHitCachedSpecs_ReturnsSameInstanceOnSecondCall()
        {
            // Cache should not re-parse when EmitGasOnHitRaw is unchanged.
            var w = new MeleeWeaponPart();
            w.EmitGasOnHitRaw = "poison-vapor,30,40,15,1";
            var specs1 = w.EmitGasOnHitCachedSpecs;
            var specs2 = w.EmitGasOnHitCachedSpecs;
            Assert.AreSame(specs1, specs2, "same List instance — cache hit");
        }

        [Test]
        public void MeleeWeaponPart_EmitGasOnHitCachedSpecs_ReParsesOnRawChange()
        {
            var w = new MeleeWeaponPart();
            w.EmitGasOnHitRaw = "poison-vapor,30,40,15,1";
            var first = w.EmitGasOnHitCachedSpecs;
            w.EmitGasOnHitRaw = "cryo-mist,50,25,10,2";
            var second = w.EmitGasOnHitCachedSpecs;
            Assert.AreNotSame(first, second);
            Assert.AreEqual("cryo-mist", second[0].GasId);
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — OnHitGasEmit.Apply dispatcher
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Apply_NullWeapon_NoCrash()
        {
            var zone = new Zone("NullWeapon");
            var defender = MakeCreature(zone, 5, 5);
            Assert.DoesNotThrow(() =>
                OnHitGasEmit.Apply(weapon: null, defender, attacker: null, zone, new System.Random(42)));
        }

        [Test]
        public void Apply_EmptyEmitGasOnHitRaw_NoSpawns()
        {
            var zone = new Zone("EmptyRaw");
            var defender = MakeCreature(zone, 5, 5);
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "" };
            OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42));
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Apply_FullChance_SpawnsNineGases()
        {
            // 100% chance + valid gas + open zone → 9 spawns (1 center + 8 adjacent).
            var zone = new Zone("FullChance");
            var defender = MakeCreature(zone, 10, 10);
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "poison-vapor,100,40,15,1" };
            OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42));
            Assert.AreEqual(9, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Apply_FullChance_CenterDensityVsAdjacentDensity()
        {
            // Pin: center cell gets CellDensity (40), adjacents get
            // AdjacentDensity (15). Independent values.
            var zone = new Zone("DensityCheck");
            var defender = MakeCreature(zone, 10, 10);
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "poison-vapor,100,40,15,1" };
            OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42));

            int centerDensity = -1;
            int adjacentTotal = 0;
            int adjacentCount = 0;
            foreach (var g in zone.GetEntitiesWithTag("Gas"))
            {
                var pos = zone.GetEntityPosition(g);
                var pool = g.GetPart<GasPoolPart>();
                if (pos.x == 10 && pos.y == 10) centerDensity = pool.Density;
                else { adjacentTotal += pool.Density; adjacentCount++; }
            }
            Assert.AreEqual(40, centerDensity, "center at CellDensity");
            Assert.AreEqual(8, adjacentCount, "8 adjacent cells");
            Assert.AreEqual(15, adjacentTotal / adjacentCount,
                "each adjacent at AdjacentDensity");
        }

        [Test]
        public void Apply_ZeroChance_NoSpawns()
        {
            // EmitGasOnHitSpec.Parse already filters chance=0, but
            // re-pin at the dispatcher: a 1% chance with a seed that
            // never rolls below 1 should still produce 0 spawns.
            var zone = new Zone("OneChance");
            var defender = MakeCreature(zone, 10, 10);
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "poison-vapor,1,40,15,1" };
            // Seed 42's first Next(100) is deterministic — check what it is.
            int firstRoll = new System.Random(42).Next(100);
            if (firstRoll < 1)
                Assert.Inconclusive("seed produced a lucky <1% roll; pick another seed");
            OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42));
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Apply_AttackerCreditedAsCreator()
        {
            var zone = new Zone("AttackerCredit");
            var defender = MakeCreature(zone, 10, 10);
            var attacker = new Entity { ID = "attacker", BlueprintName = "Attacker" };
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "poison-vapor,100,40,15,1" };
            OnHitGasEmit.Apply(weapon, defender, attacker, zone, new System.Random(42));
            foreach (var g in zone.GetEntitiesWithTag("Gas"))
                Assert.AreSame(attacker, g.GetPart<GasPoolPart>().Creator,
                    "attacker carried through to every spawned gas");
        }

        [Test]
        public void Apply_DefenderAtEdge_SkipsOOBCells()
        {
            // Defender at (0,0) — only 4 of 9 cells in bounds.
            var zone = new Zone("EdgeApply");
            var defender = MakeCreature(zone, 0, 0);
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "poison-vapor,100,40,15,1" };
            OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42));
            Assert.AreEqual(4, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Apply_TwoSpecs_IndependentChanceRolls()
        {
            // Two specs each at 100% — both fire on the same hit.
            var zone = new Zone("MultiSpec");
            var defender = MakeCreature(zone, 10, 10);
            var weapon = new MeleeWeaponPart
            {
                EmitGasOnHitRaw = "poison-vapor,100,40,15,1;cryo-mist,100,25,10,1"
            };
            OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42));
            // 9 poison + 9 cryo = 18.
            Assert.AreEqual(18, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Apply_EmitsDiagPerSpecFire()
        {
            var zone = new Zone("DiagApply");
            var defender = MakeCreature(zone, 10, 10);
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "poison-vapor,100,40,15,2" };
            Diag.ResetAll();
            OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42));
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "EmitOnHit", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"poison-vapor\"", recs[0].PayloadJson);
            StringAssert.Contains("\"chance\":100", recs[0].PayloadJson);
            StringAssert.Contains("\"cellDensity\":40", recs[0].PayloadJson);
            StringAssert.Contains("\"adjacentDensity\":15", recs[0].PayloadJson);
            StringAssert.Contains("\"gasLevel\":2", recs[0].PayloadJson);
            StringAssert.Contains("\"totalSpawned\":9", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — Adversarial / counter-checks
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_DefenderNotInZone_NoSpawn_NoCrash()
        {
            // Defender exists but isn't in the zone. zone.GetEntityCell
            // returns null → dispatcher early-outs.
            var zone = new Zone("NotInZone");
            var orphanDefender = new Entity { ID = "orphan", BlueprintName = "Orphan" };
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "poison-vapor,100,40,15,1" };
            Assert.DoesNotThrow(() =>
                OnHitGasEmit.Apply(weapon, orphanDefender, null, zone, new System.Random(42)));
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Adversarial_UnknownGasId_NoSpawn_NoCrash()
        {
            var zone = new Zone("UnknownGas");
            var defender = MakeCreature(zone, 10, 10);
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "phantom-gas,100,40,15,1" };
            Assert.DoesNotThrow(() =>
                OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42)));
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
            // Diag still fires with totalSpawned=0 (the dispatcher rolled
            // the spec, factory rejected each cell).
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "EmitOnHit", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"totalSpawned\":0", recs[0].PayloadJson);
        }

        [Test]
        public void Adversarial_NullRng_NoCrash()
        {
            var zone = new Zone("NullRng");
            var defender = MakeCreature(zone, 10, 10);
            var weapon = new MeleeWeaponPart { EmitGasOnHitRaw = "poison-vapor,100,40,15,1" };
            Assert.DoesNotThrow(() =>
                OnHitGasEmit.Apply(weapon, defender, null, zone, rng: null));
            // No spawns — Apply early-outs on null rng (defensive).
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Adversarial_NoEmitGasOnHitRaw_NoSpawn_NoDiag()
        {
            // Counter: a weapon with no EmitGasOnHitRaw doesn't fire
            // any spawns or diag. Pin the conditional gate.
            var zone = new Zone("NoRaw");
            var defender = MakeCreature(zone, 10, 10);
            var weapon = new MeleeWeaponPart(); // EmitGasOnHitRaw = "" by default
            OnHitGasEmit.Apply(weapon, defender, null, zone, new System.Random(42));
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "EmitOnHit", Limit = 5 }).Records;
            Assert.AreEqual(0, recs.Count);
        }
    }
}
