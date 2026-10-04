using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FieldMedicineVisualHookTests
    {
        FieldMedicineFixture f;
        Dictionary<PropertyInfo, object> hooks;
        [SetUp] public void Setup()
        {
            f = new FieldMedicineFixture();
            hooks = typeof(EntityVisualHooks).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p, p => p.GetValue(null));
        }
        [TearDown] public void Teardown()
        {
            foreach (var pair in hooks) pair.Key.SetValue(null, pair.Value);
            f.Dispose();
        }
        static PropertyInfo Hook()
        {
            var property = typeof(EntityVisualHooks).GetProperty("SelfUseCallback");
            Assert.NotNull(property, "Resolved self-use needs its own actor-only presentation hook."); return property;
        }
        static void Capture(Action<Entity, Zone> callback)
        {
            var hook = Hook(); hook.SetValue(null, Delegate.CreateDelegate(hook.PropertyType, callback.Target, callback.Method));
        }
        static void Emit(Entity actor, Zone zone)
        {
            var method = typeof(EntityVisualHooks).GetMethod("EmitSelfUse"); Assert.NotNull(method);
            method.Invoke(null, new object[] { actor, zone });
        }

        [Test] public void ResolvedSelfUsePreservesFacingWithoutFakeCastTargetOrAttack()
        {
            var actor = f.Actor(medicine: false); int poses = 0, attacks = 0, spells = 0, interactions = 0;
            actor.GetPart<RenderPart>().VisualFacing = EntityVisualFacing.West;
            Capture((a, z) => { Assert.AreSame(actor, a); Assert.AreSame(f.Zone, z); poses++; });
            EntityVisualHooks.AttackCallback = (a, b, z) => attacks++;
            EntityVisualHooks.CastCallback = (a, z, s, x, y, tx, ty, d) => spells++;
            EntityVisualHooks.InteractionCallback = (a, b, z) => interactions++;
            Emit(actor, f.Zone);
            Assert.AreEqual(1, poses); Assert.AreEqual(0, attacks + spells + interactions);
            Assert.AreEqual(EntityVisualFacing.West, actor.GetPart<RenderPart>().VisualFacing);
            Assert.AreEqual(7, actor.GetStatValue("Hitpoints"));
        }

        [TestCase("removed")] [TestCase("foreign-zone")] [TestCase("dead")]
        [TestCase("carried")] [TestCase("equipped")] [TestCase("death-handled")]
        public void NonCurrentActorCannotPublishSelfUse(string condition)
        {
            var actor = f.Actor(medicine: false); int poses = 0; Capture((a, z) => poses++);
            if (condition == "removed") f.Zone.RemoveEntity(actor);
            if (condition == "dead") actor.GetStat("Hitpoints").BaseValue = 0;
            if (condition == "carried") actor.GetPart<PhysicsPart>().InInventory = new Entity();
            if (condition == "equipped") actor.GetPart<PhysicsPart>().Equipped = new Entity();
            if (condition == "death-handled")
            {
                CombatSystem.HandleDeath(actor, null, f.Zone); actor.GetStat("Hitpoints").BaseValue = 7;
                Assert.IsTrue(f.Zone.AddEntity(actor, 10, 10));
            }
            Emit(actor, condition == "foreign-zone" ? new Zone("foreign") : f.Zone);
            Assert.AreEqual(0, poses);
        }

        [Test] public void MedicineGestureFiresAfterConsumptionAndHealingOnlyOnce()
        {
            var actor = f.Actor(); var target = f.Threat(actor); f.Supply(actor); int poses = 0;
            Capture((a, z) =>
            {
                Assert.AreSame(actor, a); Assert.AreSame(f.Zone, z);
                Assert.IsNull(FieldMedicineFixture.Available(actor));
                Assert.AreEqual(15, actor.GetStatValue("Hitpoints")); poses++;
            });
            Assert.IsTrue(f.Use(actor, target)); Assert.AreEqual(1, poses);
            actor.GetStat("Hitpoints").BaseValue = 7;
            Assert.IsFalse(f.Use(actor, target)); Assert.AreEqual(1, poses);
        }

        [Test] public void ResetRemovesSelfUseListenerAndHeadlessUseRemainsSafe()
        {
            int poses = 0; Capture((a, z) => poses++);
            EntityVisualHooks.Reset(); Assert.IsNull(Hook().GetValue(null));
            Emit(f.Actor(medicine: false), f.Zone); Assert.AreEqual(0, poses);
        }

        [Test] public void SpendingMedicineInvalidatesOnlyActualActorCellAndRefusalsStayQuiet()
        {
            var actor = f.Actor(); var target = f.Threat(actor); f.Supply(actor);
            var previous = ZoneRenderHooks.CellDirtyCallback;
            var previousFull = ZoneRenderHooks.FullDirtyCallback;
            int dirty = 0, fullDirty = 0;
            try
            {
                ZoneRenderHooks.CellDirtyCallback = (x, y, source) =>
                {
                    Assert.AreEqual(10, x); Assert.AreEqual(10, y);
                    Assert.AreEqual("FieldMedicine.Used", source);
                    Assert.IsNull(FieldMedicineFixture.Available(actor), "Refresh sees the paid inventory state.");
                    dirty++;
                };
                ZoneRenderHooks.FullDirtyCallback = source => fullDirty++;
                Assert.IsTrue(f.Use(actor, target)); Assert.AreEqual(1, dirty); Assert.AreEqual(0, fullDirty);
                actor.GetStat("Hitpoints").BaseValue = 7;
                Assert.IsFalse(f.Use(actor, target)); Assert.AreEqual(1, dirty); Assert.AreEqual(0, fullDirty);
            }
            finally { ZoneRenderHooks.CellDirtyCallback = previous; ZoneRenderHooks.FullDirtyCallback = previousFull; }
        }
    }
}
