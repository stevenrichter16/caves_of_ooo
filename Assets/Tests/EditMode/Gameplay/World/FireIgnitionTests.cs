using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Fire actually setting flammable things alight, measured against the
    /// real blueprints.
    ///
    /// <para><b>Reported from play:</b> "flammable materials do not seem to
    /// light on fire when hit with fire, despite being hit multiple
    /// times."</para>
    ///
    /// <para><b>Root cause.</b> <c>ThermalPart.HandleApplyHeat</c> converts
    /// joules to degrees as <c>delta = joules / HeatCapacity</c>, so
    /// igniting something from ambient costs roughly
    /// <c>(FlameTemperature - 25) x HeatCapacity</c> — hundreds of joules.
    /// Every fire ability had a hand-picked figure that had never been
    /// checked against that: Flaming Hands delivered <c>damage * 5</c>
    /// (5-20 at level 1), which per-turn ambient decay ate faster than it
    /// accumulated. It could not ignite anything, ever.</para>
    ///
    /// <para><b>Why it survived so long.</b> The scale is asymmetric. Cold
    /// crosses 25 degrees (ambient 25 → freeze 0) so every cold dose works
    /// in one cast and looks fine; heat crosses 295-475. The doses were
    /// sized for the cold half and never re-checked against the hot
    /// half.</para>
    ///
    /// <para>These tests pin the calibration in <see cref="FireDose"/>
    /// against shipped content. A future re-tune that drops a dose below
    /// its flame point fails here rather than silently making the spell
    /// inert again.</para>
    /// </summary>
    [TestFixture]
    public class FireIgnitionTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp() => MessageLog.Clear();

        /// <summary>
        /// Apply <paramref name="joules"/> once per turn, with the
        /// end-of-turn ambient decay in between — the real cadence, which
        /// is what makes an under-dosed source never arrive.
        /// </summary>
        /// <returns>Turns until it caught, or -1 if it never did.</returns>
        private static int TurnsToIgnite(Entity target, Zone zone, float joules, int limit = 40)
        {
            for (int turn = 1; turn <= limit; turn++)
            {
                var heat = GameEvent.New("ApplyHeat");
                heat.SetParameter("Joules", (object)joules);
                heat.SetParameter("Radiant", (object)false);
                heat.SetParameter("Zone", (object)zone);
                target.FireEventAndRelease(heat);

                if (target.HasEffect<BurningEffect>()) return turn;

                var endTurn = GameEvent.New("EndTurn");
                endTurn.SetParameter("Zone", (object)zone);
                target.FireEventAndRelease(endTurn);
            }
            return -1;
        }

        private static Entity Spawn(string blueprint, Zone zone, int x = 5, int y = 5)
        {
            var e = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(e, blueprint);
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════
        // The reported bug
        // ════════════════════════════════════════════════════════

        [Test]
        public void AFireAttackLightsABushInOneCast()
        {
            var zone = new Zone("Z");
            Assert.AreEqual(1, TurnsToIgnite(Spawn("Bush", zone), zone, FireDose.Attack),
                "dry tinder is what the anchor dose is calibrated against");
        }

        [Test]
        public void AFireAttackLightsAHedgerowAndATree()
        {
            var zone = new Zone("Z");
            int hedge = TurnsToIgnite(Spawn("Hedge", zone), zone, FireDose.Attack);
            int tree = TurnsToIgnite(Spawn("Tree", zone, 7, 7), zone, FireDose.Attack);

            Assert.Greater(hedge, 0, "a hedgerow must catch");
            Assert.LessOrEqual(hedge, 3, "and within a few casts, not dozens");
            Assert.Greater(tree, 0, "a tree must catch");
            Assert.LessOrEqual(tree, 5, "a tree is heavier fuel, but not hopeless");
        }

        [Test]
        public void EvenTheWeakestFireEventuallyLightsTinder()
        {
            // "Weakest" has to mean slow, not inert. The old figure (50)
            // could not light a bush inside 30 casts.
            var zone = new Zone("Z");
            int turns = TurnsToIgnite(Spawn("Bush", zone), zone, FireDose.Cantrip);
            Assert.Greater(turns, 0, "a cantrip must still work");
            Assert.LessOrEqual(turns, 5);
        }

        [Test]
        public void TheOldDoseCouldNotLightAnything_Regression()
        {
            // The actual shipped figure for Flaming Hands at level 1:
            // 1d4 averaged 2.5, times 5. This is the number the player was
            // hitting the tree with. Pinned so the failure mode is legible
            // to whoever reads this next.
            var zone = new Zone("Z");
            Assert.AreEqual(-1, TurnsToIgnite(Spawn("Bush", zone), zone, 12.5f),
                "12.5 joules per cast never ignites dry tinder — decay outruns it");
        }

        // ════════════════════════════════════════════════════════
        // Counter-checks — fire must still respect materials
        // ════════════════════════════════════════════════════════

        [Test]
        public void NoDoseOfFireLightsAStoneWall()
        {
            // Masonry is authored Combustibility 0, so MaterialPart vetoes
            // ignition whatever the temperature reaches. If raising the
            // doses had made stone burn, the material rules would have
            // become decorative.
            var zone = new Zone("Z");
            Assert.AreEqual(-1, TurnsToIgnite(Spawn("StoneWall", zone), zone, FireDose.Blast),
                "stone does not burn at any dose");
        }

        [Test]
        public void ASoakedThingRefusesToLightUntilItDriesOut()
        {
            // Water is fire's counter — the grammar the whole element
            // system teaches. Raising the doses must not have bought past
            // it. ThermalPart.TryIgnite boils off moisture instead.
            var zone = new Zone("Z");
            var bush = Spawn("Bush", zone);
            bush.ApplyEffect(new WetEffect(moisture: 1.0f), null, zone);

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)FireDose.Blast);
            heat.SetParameter("Radiant", (object)false);
            heat.SetParameter("Zone", (object)zone);
            bush.FireEventAndRelease(heat);

            Assert.IsFalse(bush.HasEffect<BurningEffect>(), "soaked wood does not catch");
            Assert.Less(bush.GetEffect<WetEffect>().Moisture, 1.0f,
                "but the fire boils some of the water off, so it is a delay not an immunity");
        }

        // ════════════════════════════════════════════════════════
        // Spread
        // ════════════════════════════════════════════════════════

        [Test]
        public void ABurningBushLightsTheOneNextToIt()
        {
            // EmitHeatToAdjacent divides its figure across eight
            // directions, so the old Intensity * 30 gave each neighbour
            // 3.75 joules — an equilibrium of 100 degrees against a flame
            // point of 320. Fire could not spread at all.
            var zone = new Zone("Z");
            var lit = Spawn("Bush", zone, 5, 5);
            var neighbour = Spawn("Bush", zone, 6, 5);

            lit.ApplyEffect(new BurningEffect(intensity: 1.0f), null, zone);
            Assert.IsTrue(lit.HasEffect<BurningEffect>(), "precondition: the first one is alight");

            int caught = -1;
            for (int turn = 1; turn <= 15; turn++)
            {
                MaterialSimSystem.EmitHeatToAdjacent(
                    lit, zone, 1.0f * FireDose.SpreadTotal);
                if (neighbour.HasEffect<BurningEffect>()) { caught = turn; break; }

                var endTurn = GameEvent.New("EndTurn");
                endTurn.SetParameter("Zone", (object)zone);
                neighbour.FireEventAndRelease(endTurn);
            }

            Assert.Greater(caught, 0, "the neighbouring bush must catch");
            Assert.GreaterOrEqual(caught, 2,
                "but not instantly — the player has to be able to walk away from it");
        }

        [Test]
        public void FireDoesNotSpreadToStone()
        {
            var zone = new Zone("Z");
            var lit = Spawn("Bush", zone, 5, 5);
            var wall = Spawn("StoneWall", zone, 6, 5);
            lit.ApplyEffect(new BurningEffect(intensity: 1.0f), null, zone);

            for (int turn = 0; turn < 15; turn++)
                MaterialSimSystem.EmitHeatToAdjacent(lit, zone, 1.0f * FireDose.SpreadTotal);

            Assert.IsFalse(wall.HasEffect<BurningEffect>());
        }

        // ════════════════════════════════════════════════════════
        // The dose table itself
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheDosesStayOrderedAndAnchored()
        {
            // The tiers are multiples of one anchor. If someone edits an
            // individual constant instead of the anchor, the ordering that
            // gives each spell its identity can invert silently.
            Assert.Less(FireDose.Cantrip, FireDose.Attack);
            Assert.Less(FireDose.Attack, FireDose.Ignition);
            Assert.Less(FireDose.Ignition, FireDose.Blast);
            Assert.AreEqual(FireDose.TinderIgnition, FireDose.Attack,
                "Attack IS the anchor — one cast lights dry tinder");
        }

        [Test]
        public void SelfSustainCoversWhatDecayTakes()
        {
            // A burning tree lost about 7 degrees a turn under the old flat
            // figure and would put itself out. Sustain must at least break
            // even, or nothing ever finishes burning down.
            const float capacity = 2.5f, temperature = 410f, ambient = 25f, decay = 0.04f;
            float lost = (temperature - ambient) * decay * capacity;
            float given = FireDose.SelfSustain(1.0f, capacity, temperature, ambient, decay);

            Assert.GreaterOrEqual(given, lost, "a fire must not cool itself out");
        }
    }
}
