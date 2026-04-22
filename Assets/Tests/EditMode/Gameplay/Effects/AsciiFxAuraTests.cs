using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class AsciiFxAuraTests
    {
        [SetUp]
        public void Setup()
        {
            AsciiFxBus.Clear();
            MessageLog.Clear();
        }

        [Test]
        public void ApplyBurning_StartsFireAura_AndRemovalStopsIt()
        {
            var zone = new Zone("AuraZone");
            var entity = CreateCreature("burning target");
            zone.AddEntity(entity, 5, 5);

            bool applied = entity.ApplyEffect(new BurningEffect(intensity: 1.0f), zone: zone);
            var applyRequests = AsciiFxBus.Drain();

            Assert.IsTrue(applied);
            Assert.AreEqual(1, applyRequests.Count);
            Assert.AreEqual(AsciiFxRequestType.AuraStart, applyRequests[0].Type);
            Assert.AreEqual(AsciiFxTheme.Fire, applyRequests[0].Theme);

            bool removed = entity.RemoveEffect<BurningEffect>();
            var removeRequests = AsciiFxBus.Drain();

            Assert.IsTrue(removed);
            Assert.AreEqual(1, removeRequests.Count);
            Assert.AreEqual(AsciiFxRequestType.AuraStop, removeRequests[0].Type);
            Assert.AreEqual(AsciiFxTheme.Fire, removeRequests[0].Theme);
        }

        [Test]
        public void ApplyPoison_StartsPoisonAura()
        {
            var zone = new Zone("AuraZone");
            var entity = CreateCreature("poisoned target");
            zone.AddEntity(entity, 5, 5);

            bool applied = entity.ApplyEffect(new PoisonedEffect(duration: 5), zone: zone);
            var requests = AsciiFxBus.Drain();

            Assert.IsTrue(applied);
            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(AsciiFxRequestType.AuraStart, requests[0].Type);
            Assert.AreEqual(AsciiFxTheme.Poison, requests[0].Theme);
        }

        private static Entity CreateCreature(string name)
        {
            var entity = new Entity { BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            return entity;
        }
    }
}
