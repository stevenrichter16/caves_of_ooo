using NUnit.Framework;
using CavesOfOoo.Core;
using System.Collections.Generic;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ApplyDamage owns one complete, owner-attributed damage receipt per real
    /// HP delta. The former particle-per-digit representation made neighboring
    /// hits concatenate; grouping changes presentation, not damage semantics.
    /// </summary>
    public class ApplyDamageFloatingNumberTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            AsciiFxBus.Clear();
        }

        [Test]
        public void ApplyDamage_EmitsFloatingNumber_AtTargetCell()
        {
            var zone = VisibleZone();
            var target = MakeTarget(50);
            Assert.True(zone.AddEntity(target, 7, 5));
            CombatSystem.ApplyDamage(target, new Damage(8), source: null, zone: zone);
            var requests = AsciiFxBus.Drain();
            try
            {
                var number = OnlyNumber(requests);
                Assert.AreEqual(8, number.Number);
                Assert.AreSame(target, number.Anchor);
                Assert.AreSame(zone.GetCell(7, 5), number.OriginCell);
                Assert.False(number.AllowOriginOnly);
                Assert.AreEqual(42, target.GetStatValue("Hitpoints"));
                Assert.Zero(CountByType(requests, AsciiFxRequestType.Particle),
                    "A whole number is no longer split into independent decoration particles.");
            }
            finally { Release(requests); }
        }

        [Test]
        public void ApplyDamage_FullyResisted_DoesNotEmitNumber()
        {
            var zone = VisibleZone();
            var target = MakeTarget(50);
            target.Statistics["HeatResistance"] = new Stat
            {
                Owner = target, Name = "HeatResistance", BaseValue = 100, Min = -100, Max = 200
            };
            Assert.True(zone.AddEntity(target, 7, 5));
            var damage = new Damage(20);
            damage.AddAttribute("Fire");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            var requests = AsciiFxBus.Drain();
            try
            {
                Assert.Zero(CountByType(requests, AsciiFxRequestType.FloatingNumber));
                Assert.AreEqual(50, target.GetStatValue("Hitpoints"));
            }
            finally { Release(requests); }
        }

        [Test]
        public void ApplyDamage_OverKillDamage_DisplaysHpBeforeNotRawAmount()
        {
            var zone = VisibleZone();
            var target = MakeTarget(3);
            Assert.True(zone.AddEntity(target, 7, 5));
            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);
            var requests = AsciiFxBus.Drain();
            try
            {
                var number = OnlyNumber(requests);
                Assert.AreEqual(3, number.Number, "Killing blows display actual HP lost, not raw overkill.");
                Assert.AreSame(target, number.Anchor);
                Assert.AreSame(zone.GetCell(7, 5), number.OriginCell);
                Assert.True(number.AllowOriginOnly,
                    "The visible lethal receipt must survive actual owner removal before presentation.");
                Assert.Null(zone.GetEntityCell(target));
                Assert.AreEqual(0, target.GetStatValue("Hitpoints"));
            }
            finally { Release(requests); }
        }

        [Test]
        public void ApplyDamage_TwoDigitDamage_EmitsOneCompleteReceipt()
        {
            var zone = VisibleZone();
            var target = MakeTarget(50);
            Assert.True(zone.AddEntity(target, 7, 5));
            CombatSystem.ApplyDamage(target, new Damage(12), source: null, zone: zone);
            var requests = AsciiFxBus.Drain();
            try
            {
                var number = OnlyNumber(requests);
                Assert.AreEqual(12, number.Number);
                Assert.AreSame(target, number.Anchor);
                Assert.AreEqual(38, target.GetStatValue("Hitpoints"));
                Assert.Zero(CountByType(requests, AsciiFxRequestType.Particle));
            }
            finally { Release(requests); }
        }

        [Test]
        public void ApplyDamage_ZeroAmount_DoesNotEmitNumber()
        {
            var zone = VisibleZone();
            var target = MakeTarget(50);
            Assert.True(zone.AddEntity(target, 7, 5));
            CombatSystem.ApplyDamage(target, new Damage(0), source: null, zone: zone);
            var requests = AsciiFxBus.Drain();
            try
            {
                Assert.Zero(CountByType(requests, AsciiFxRequestType.FloatingNumber));
                Assert.AreEqual(50, target.GetStatValue("Hitpoints"));
            }
            finally { Release(requests); }
        }

        // This fixture exercises actual ApplyDamage, including the lethal path.
        // It does not claim a seeded PerformSingleAttack duplication test.
        static Zone VisibleZone()
        {
            var zone = new Zone("TestZone");
            foreach (var cell in zone.Cells) { cell.IsVisible = true; cell.Explored = true; }
            return zone;
        }
        static AsciiFxRequest OnlyNumber(IList<AsciiFxRequest> requests)
        {
            Assert.AreEqual(1, CountByType(requests, AsciiFxRequestType.FloatingNumber));
            foreach (var request in requests)
                if (request.Type == AsciiFxRequestType.FloatingNumber) return request;
            return null;
        }
        static int CountByType(IList<AsciiFxRequest> requests, AsciiFxRequestType type)
        {
            int count = 0;
            foreach (var request in requests) if (request.Type == type) count++;
            return count;
        }
        static void Release(IList<AsciiFxRequest> requests)
        {
            foreach (var request in requests) AsciiFxBus.Release(request);
        }
        static Entity MakeTarget(int hp)
        {
            var entity = new Entity { ID = "tgt", BlueprintName = "TestTarget" };
            entity.AddPart(new RenderPart { DisplayName = "target", Visible = true });
            entity.AddPart(new PhysicsPart { Solid = false });
            entity.Statistics["Hitpoints"] = new Stat
            {
                Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp
            };
            return entity;
        }
    }
}
