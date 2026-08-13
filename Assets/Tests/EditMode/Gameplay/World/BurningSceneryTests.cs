using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Fire against real scenery, built from the real blueprints
    /// (<c>Docs/OBJECT-INTERACTION-PLAN.md</c> §5).
    ///
    /// <para>Every link in this chain is unit-tested elsewhere. This file
    /// exists because the links were tested against <b>fixtures</b>, and
    /// the chain only actually works if the shipped content carries the
    /// Parts the chain reads: a <c>MaterialPart</c> whose tags satisfy
    /// <see cref="ObjectStatusMatrix"/>, a <c>DestructiblePart</c> to take
    /// the damage, and a <c>ThermalPart</c> for the ignition pipeline. A
    /// hedgerow missing any one of them fails silently while every unit
    /// test stays green.</para>
    /// </summary>
    [TestFixture]
    public class BurningSceneryTests
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
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        /// <summary>
        /// One turn, through the production path: <c>BeginTakeAction</c> is
        /// what <c>StatusEffectsPart</c> listens for, and it carries the
        /// zone the effects tick against.
        /// </summary>
        private static void TickTurn(Entity target, Zone zone)
        {
            var e = GameEvent.New("BeginTakeAction");
            e.SetParameter("Zone", (object)zone);
            target.FireEventAndRelease(e);
        }

        [Test]
        public void ARealHedgerowCarriesEveryPartTheFireChainReads()
        {
            // The chain is: matrix reads MaterialPart tags -> effect lands
            // -> BurningEffect ticks damage into DestructiblePart ->
            // ThermalPart carries heat to the neighbours. Miss one and the
            // failure is silent.
            foreach (var name in new[] { "Hedge", "Tree", "Bush", "VineWall" })
            {
                var e = _factory.CreateEntity(name);
                Assert.IsNotNull(e, name);
                Assert.IsNotNull(e.GetPart<MaterialPart>(), $"{name} MaterialPart");
                Assert.IsNotNull(e.GetPart<DestructiblePart>(), $"{name} DestructiblePart");
                Assert.IsNotNull(e.GetPart<ThermalPart>(), $"{name} ThermalPart");
                Assert.AreEqual(ObjectStatusVerdict.Applies,
                    ObjectStatusMatrix.Evaluate(new BurningEffect(), e), $"{name} is flammable");
            }
        }

        [Test]
        public void ARealStoneWallIsNotFlammable()
        {
            // Counter-check on the above: if the material tags were wrong
            // (or the gate absent) everything would read as flammable and
            // the first test would still pass.
            var wall = _factory.CreateEntity("StoneWall");
            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(new BurningEffect(), wall));
        }

        [Test]
        public void SettingARealHedgerowAlight_BurnsItDown()
        {
            // The end-to-end property the user asked for, on shipped
            // content: light a hedge and it is eventually consumed. Before
            // this work it would catch fire and then burn forever, because
            // BurningEffect's damage went through ApplyDamage, which
            // early-returns on anything without a Hitpoints stat.
            var hedge = _factory.CreateEntity("Hedge");
            var zone = new Zone("Z");
            zone.AddEntity(hedge, 5, 5);

            int startHp = hedge.GetPart<DestructiblePart>().HP;
            Assert.IsTrue(ObjectStatusMatrix.TryApply(
                new BurningEffect(intensity: 2.0f), hedge, null, zone), "it catches");

            // Burn until it is gone, with a generous bound so a stuck
            // fire fails as a timeout rather than hanging the suite.
            var effects = hedge.GetPart<StatusEffectsPart>();
            for (int turn = 0; turn < 60 && zone.GetEntityPosition(hedge).x >= 0; turn++)
            {
                // Keep it alight the way an ongoing fire would; the point
                // under test is that the damage LANDS, not how long fuel
                // lasts.
                if (effects.GetEffect<BurningEffect>() == null)
                    hedge.ApplyEffect(new BurningEffect(intensity: 2.0f), null, zone);
                TickTurn(hedge, zone);
            }

            Assert.Less(hedge.GetPart<DestructiblePart>().HP, startHp, "fire damaged it");
            Assert.Less(zone.GetEntityPosition(hedge).x, 0, "and eventually consumed it");
        }

        [Test]
        public void ARealStoneWallDoesNotBurnDown()
        {
            // Counter-check. A stone wall refuses the effect outright, so
            // no amount of ticking should move its HP.
            var wall = _factory.CreateEntity("StoneWall");
            var zone = new Zone("Z");
            zone.AddEntity(wall, 5, 5);
            int startHp = wall.GetPart<DestructiblePart>().HP;

            Assert.IsFalse(ObjectStatusMatrix.TryApply(new BurningEffect(), wall, null, zone));

            for (int turn = 0; turn < 10; turn++)
                TickTurn(wall, zone);

            Assert.AreEqual(startHp, wall.GetPart<DestructiblePart>().HP, "untouched");
            Assert.AreEqual((5, 5), zone.GetEntityPosition(wall));
        }

        [Test]
        public void ARealCopperPipeConductsWhereAHedgerowDoesNot()
        {
            // The other half of the matrix, also against shipped content:
            // lightning cares about a tag the flammables do not have.
            var pipe = _factory.CreateEntity("CopperPipe");
            Assert.IsNotNull(pipe);
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), pipe), "copper conducts");

            var hedge = _factory.CreateEntity("Hedge");
            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), hedge), "a hedge does not");
        }

        [Test]
        public void ARealBarrelCannotBeMadeToBleed()
        {
            // The fail-closed property, against shipped content rather than
            // a fixture.
            var barrel = _factory.CreateEntity("WoodenBarrel");
            Assert.AreEqual(ObjectStatusVerdict.Meaningless,
                ObjectStatusMatrix.Evaluate(new BleedingEffect(), barrel));
        }
    }
}
