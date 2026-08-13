using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// "How much before it breaks", for both kinds of health.
    ///
    /// <para>Creatures keep hitpoints in a stat and scenery keeps them on a
    /// <see cref="DestructiblePart"/>. Every surface that shows health had
    /// to know about only the first, so pointing at a barrel reported
    /// nothing — you could hit it repeatedly with no idea how close it was
    /// to going.</para>
    /// </summary>
    public class HealthReadoutTests
    {
        private static Entity Creature(int hp, int max)
        {
            var e = new Entity { ID = "npc", BlueprintName = "Villager" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = max };
            return e;
        }

        private static Entity Breakable(int hp, int max)
        {
            var e = new Entity { ID = "barrel", BlueprintName = "WoodenBarrel" };
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new DestructiblePart { HP = hp, MaxHP = max });
            return e;
        }

        [Test]
        public void ACreatureReportsItsHitpoints()
        {
            Assert.AreEqual("HP 6/10", HealthReadout.Describe(Creature(6, 10)));
        }

        [Test]
        public void AnObjectReportsItsStructuralHitpoints()
        {
            // The whole point: this used to be the empty string, so a
            // half-smashed barrel looked identical to a fresh one.
            Assert.AreEqual("HP 3/8", HealthReadout.Describe(Breakable(3, 8)));
        }

        [Test]
        public void AnUnbreakableThingSaysSoInsteadOfShowingANumber()
        {
            // "HP 40/40" on a staircase is a lie of omission — the player
            // would keep hitting it waiting for the number to move.
            var stairs = Breakable(40, 40);
            stairs.GetPart<DestructiblePart>().Indestructible = true;

            Assert.AreEqual(HealthReadout.UnbreakableLabel, HealthReadout.Describe(stairs));
        }

        [Test]
        public void SomethingWithNeitherKindOfHealthReportsNothing()
        {
            // Counter-check: a coin and a puddle must not sprout an
            // "HP 0/0" that implies they can be attacked.
            var coin = new Entity { ID = "coin", BlueprintName = "GoldCoin" };
            coin.AddPart(new PhysicsPart { Takeable = true });

            Assert.AreEqual(string.Empty, HealthReadout.Describe(coin));
            Assert.IsFalse(HealthReadout.Has(coin));
            Assert.AreEqual(string.Empty, HealthReadout.Describe(null));
        }

        [Test]
        public void ACreatureWithBothKindsReportsTheLivingOne()
        {
            // A mimic has the Creature tag and could plausibly acquire a
            // DestructiblePart from something generic. Its real health is
            // the stat, and that is the one the player must be shown —
            // otherwise the number would not move as they fought it.
            var mimic = Creature(4, 12);
            mimic.AddPart(new DestructiblePart { HP = 99, MaxHP = 99 });

            Assert.AreEqual("HP 4/12", HealthReadout.Describe(mimic));
        }

        [Test]
        public void ACreatureWithNoHitpointsStatReportsNothing()
        {
            var husk = new Entity { ID = "x", BlueprintName = "Odd" };
            husk.Tags["Creature"] = "";

            Assert.AreEqual(string.Empty, HealthReadout.Describe(husk));
        }
    }
}
