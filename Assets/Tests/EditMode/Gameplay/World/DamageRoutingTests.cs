using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Damage finding the right hitpoint pool
    /// (<c>Docs/OBJECT-INTERACTION-PLAN.md</c> §4/§5).
    ///
    /// <para><c>CombatSystem.ApplyDamage</c> deliberately early-returns on a
    /// target with no <c>Hitpoints</c> stat — statues and props are not
    /// creatures. The consequence nobody had noticed is that every caller
    /// which could reach an object did nothing at all, and did it silently:
    /// a fire bolt aimed at a hedgerow was a no-op, and scenery set alight
    /// burned forever without being consumed.</para>
    ///
    /// <para><see cref="DestructionSystem.RouteDamage"/> is the fix, and
    /// these are the tests that stop it regressing to a single path.</para>
    /// </summary>
    public class DamageRoutingTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        private static Entity Hedge(int hp = 10)
        {
            var e = new Entity { ID = "hedge", BlueprintName = "Hedge" };
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new DestructiblePart { HP = hp, MaxHP = hp });
            e.AddPart(new MaterialPart { MaterialTagsRaw = "Organic,Plant,Wood" });
            return e;
        }

        private static Entity Creature(int hp = 20)
        {
            var e = new Entity { ID = "npc", BlueprintName = "Villager" };
            e.Tags["Creature"] = "";
            e.AddPart(new PhysicsPart());
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            return e;
        }

        private static Damage Fire(int amount)
        {
            var d = new Damage(amount);
            d.AddAttribute("Fire");
            return d;
        }

        [Test]
        public void DamageToSceneryLandsInItsStructuralPool()
        {
            var hedge = Hedge(hp: 10);
            var zone = new Zone("Z");
            zone.AddEntity(hedge, 3, 3);

            int landed = DestructionSystem.RouteDamage(hedge, Fire(4), null, zone);

            Assert.AreEqual(4, landed, "the caller must be told what actually happened");
            Assert.AreEqual(6, hedge.GetPart<DestructiblePart>().HP);
        }

        [Test]
        public void DamageToACreatureStillTakesTheCombatPath()
        {
            // Counter-check. If RouteDamage sent everything to
            // DestructionSystem, creatures would stop taking damage entirely
            // and no object test above would notice.
            var npc = Creature(hp: 20);
            var zone = new Zone("Z");
            zone.AddEntity(npc, 3, 3);

            int landed = DestructionSystem.RouteDamage(npc, Fire(5), null, zone);

            Assert.AreEqual(5, landed);
            Assert.AreEqual(15, npc.GetStatValue("Hitpoints", 0));
        }

        [Test]
        public void EnoughDamageDestroysTheScenery()
        {
            var hedge = Hedge(hp: 3);
            var zone = new Zone("Z");
            zone.AddEntity(hedge, 3, 3);

            DestructionSystem.RouteDamage(hedge, Fire(9), null, zone);

            Assert.Less(zone.GetEntityPosition(hedge).x, 0, "burned away");
        }

        [Test]
        public void APropWithNeitherPoolAbsorbsNothingAndDoesNotCrash()
        {
            // A statue: not a creature, not breakable. Both paths must
            // decline rather than throw, and the caller must learn nothing
            // landed rather than being told a lie.
            var statue = new Entity { ID = "statue", BlueprintName = "Statue" };
            statue.AddPart(new PhysicsPart { Solid = true });

            Assert.AreEqual(0, DestructionSystem.RouteDamage(statue, Fire(50), null, new Zone("Z")));
        }

        [Test]
        public void NullsAndZeroDamageAreRefused()
        {
            Assert.AreEqual(0, DestructionSystem.RouteDamage(null, Fire(5), null, null));
            Assert.AreEqual(0, DestructionSystem.RouteDamage(Hedge(), null, null, null));
            Assert.AreEqual(0, DestructionSystem.RouteDamage(Hedge(), Fire(0), null, null));
        }

        [Test]
        public void AnIndestructibleThingShrugsOffSpellDamage()
        {
            // Routing must not become a back door around Indestructible:
            // a fireball at a staircase has to be as futile as a fist.
            var stairs = Hedge(hp: 10);
            stairs.GetPart<DestructiblePart>().Indestructible = true;
            var zone = new Zone("Z");
            zone.AddEntity(stairs, 3, 3);

            Assert.AreEqual(0, DestructionSystem.RouteDamage(stairs, Fire(999), null, zone));
            Assert.AreEqual(10, stairs.GetPart<DestructiblePart>().HP);
            Assert.AreEqual((3, 3), zone.GetEntityPosition(stairs));
        }
    }
}
