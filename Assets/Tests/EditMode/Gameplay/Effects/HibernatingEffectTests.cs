using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — HibernatingEffect tests. Pins the "block action,
    /// heal per turn, +100% Heat/Cold resistance" semantic.
    /// </summary>
    public class HibernatingEffectTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); }

        private static Entity MakeBodied(int hp = 100, int maxHp = 100)
        {
            var e = new Entity { ID = "actor", BlueprintName = "actor" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = maxHp };
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 25, Min = -100, Max = 100 };
            e.Statistics["ColdResistance"] = new Stat { Owner = e, Name = "ColdResistance", BaseValue = 50, Min = -100, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        [Test]
        public void Hibernating_AllowAction_ReturnsFalse()
        {
            var hib = new HibernatingEffect();
            Assert.IsFalse(hib.AllowAction(MakeBodied()));
        }

        [Test]
        public void Hibernating_OnApply_BumpsResistancesTo100()
        {
            var actor = MakeBodied();
            actor.ApplyEffect(new HibernatingEffect(10), actor, null);
            Assert.AreEqual(100, actor.GetStatValue("HeatResistance"));
            Assert.AreEqual(100, actor.GetStatValue("ColdResistance"));
        }

        [Test]
        public void Hibernating_OnRemove_RestoresPriorResistances()
        {
            var actor = MakeBodied();
            actor.ApplyEffect(new HibernatingEffect(10), actor, null);
            Assert.AreEqual(100, actor.GetStatValue("HeatResistance"));
            // Manually remove.
            actor.GetPart<StatusEffectsPart>().RemoveEffect<HibernatingEffect>();
            Assert.AreEqual(25, actor.GetStatValue("HeatResistance"),
                "OnRemove must restore prior HeatResistance.");
            Assert.AreEqual(50, actor.GetStatValue("ColdResistance"));
        }

        [Test]
        public void Hibernating_OnTurnStart_HealsPercentOfMaxHp()
        {
            var actor = MakeBodied(hp: 50, maxHp: 100);
            var hib = new HibernatingEffect(10);
            actor.ApplyEffect(hib, actor, null);
            int hpBefore = actor.GetStatValue("Hitpoints");
            hib.OnTurnStart(actor, null);
            int hpAfter = actor.GetStatValue("Hitpoints");
            Assert.Greater(hpAfter, hpBefore, "OnTurnStart must heal.");
            // 5% of 100 = 5
            Assert.AreEqual(hpBefore + 5, hpAfter);
        }

        [Test]
        public void Hibernating_OnTurnStart_DoesNotExceedMaxHp()
        {
            var actor = MakeBodied(hp: 99, maxHp: 100);
            var hib = new HibernatingEffect(10);
            actor.ApplyEffect(hib, actor, null);
            hib.OnTurnStart(actor, null);
            Assert.AreEqual(100, actor.GetStatValue("Hitpoints"),
                "Healing must clamp at max HP.");
        }

        [Test]
        public void Hibernating_NonStacking()
        {
            var first = new HibernatingEffect(10);
            var second = new HibernatingEffect(10);
            bool stacked = first.OnStack(second);
            Assert.IsTrue(stacked, "OnStack returns true to suppress the duplicate.");
            Assert.AreEqual(10, first.Duration, "Re-applying must NOT extend duration.");
        }
    }
}
