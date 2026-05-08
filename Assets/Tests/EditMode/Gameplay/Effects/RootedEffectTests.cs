using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — RootedEffect tests. Pins the "blocks movement but
    /// not action" semantic.
    /// </summary>
    public class RootedEffectTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); }

        private static Entity MakeBodied()
        {
            var e = new Entity { ID = "actor", BlueprintName = "actor" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        [Test]
        public void Rooted_AllowMovement_ReturnsFalse()
        {
            var rooted = new RootedEffect();
            var target = MakeBodied();
            Assert.IsFalse(rooted.AllowMovement(target),
                "Rooted must block movement — that's the defining mechanic.");
        }

        [Test]
        public void Rooted_AllowAction_ReturnsTrue()
        {
            var rooted = new RootedEffect();
            var target = MakeBodied();
            Assert.IsTrue(rooted.AllowAction(target),
                "Rooted leaves AllowAction at default true — actor can still attack/cast.");
        }

        [Test]
        public void Rooted_Stacks_ExtendsDuration()
        {
            var first = new RootedEffect(duration: 4);
            var second = new RootedEffect(duration: 3);
            bool stacked = first.OnStack(second);
            Assert.IsTrue(stacked);
            Assert.AreEqual(7, first.Duration);
        }

        [Test]
        public void Rooted_DefaultDuration_Is4()
        {
            Assert.AreEqual(4, new RootedEffect().Duration);
        }

        [Test]
        public void Rooted_GetEffectType_IsNegative()
        {
            var rooted = new RootedEffect();
            Assert.IsTrue(rooted.IsOfType(Effect.TYPE_NEGATIVE));
        }

        [Test]
        public void Rooted_BlocksBeforeMoveEvent_ViaStatusEffectsPart()
        {
            var actor = MakeBodied();
            actor.ApplyEffect(new RootedEffect(4), null, null);
            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)actor);
            bool allowed = actor.FireEvent(beforeMove);
            beforeMove.Release();
            Assert.IsFalse(allowed,
                "BeforeMove on a rooted entity must be rejected (Handled=true, FireEvent returns false).");
        }
    }
}
