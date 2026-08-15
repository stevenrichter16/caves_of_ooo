using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Status-effects study fix 2 — audit F18, generalized. Entity.FireEvent
    /// walks Parts by index; a handler that mutates the Parts list
    /// mid-dispatch shifts the live iterator. The shipped case: the FIRST
    /// ApplyEffect on an entity lazily creates StatusEffectsPart, whose
    /// Initialize front-inserts it at Parts[0] — every index shifts up by
    /// one and the part that triggered the effect is VISITED AGAIN
    /// (double-applied ApplyHeat doses). The mirror case: a part that
    /// removes itself slides its successor into the current slot, and the
    /// successor is SKIPPED.
    ///
    /// <para>The fix re-anchors the loop on the part it just ran. These
    /// tests probe all three mutation shapes plus the real F18
    /// ignition-path repro.</para>
    /// </summary>
    public class FireEventMutationSafetyTests
    {
        [SetUp]
        public void SetUp() => MessageLog.Clear();

        // ── Instrumented parts ───────────────────────────────────────

        private class CountingPart : Part
        {
            public int Calls;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Probe") Calls++;
                return true;
            }
        }

        /// <summary>Mimics StatusEffectsPart.Initialize's front-insert,
        /// from inside a live dispatch.</summary>
        private class FrontInserterPart : Part
        {
            public int Calls;
            public CountingPart Inserted;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "Probe") return true;
                Calls++;
                if (Inserted == null)
                {
                    Inserted = new CountingPart();
                    ParentEntity.Parts.Insert(0, Inserted);
                }
                return true;
            }
        }

        private class SelfRemoverPart : Part
        {
            public int Calls;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "Probe") return true;
                Calls++;
                ParentEntity.Parts.Remove(this);
                return true;
            }
        }

        private class AppenderPart : Part
        {
            public CountingPart Appended;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "Probe") return true;
                if (Appended == null)
                {
                    Appended = new CountingPart();
                    ParentEntity.Parts.Add(Appended);
                }
                return true;
            }
        }

        private static void Probe(Entity e)
        {
            var evt = GameEvent.New("Probe");
            e.FireEvent(evt);
            evt.Release();
        }

        // ── The three mutation shapes ────────────────────────────────

        [Test]
        public void FrontInsert_DoesNotRevisitTheRunningPart()
        {
            // The F18 shape in miniature: [A, Inserter]. The insert at 0
            // shifts Inserter from index 1 to 2; the naive walk then
            // visits index 2 — Inserter AGAIN.
            var entity = new Entity { BlueprintName = "probe" };
            var a = new CountingPart();
            var inserter = new FrontInserterPart();
            entity.AddPart(a);
            entity.AddPart(inserter);

            Probe(entity);

            Assert.AreEqual(1, a.Calls, "the untouched part runs once");
            Assert.AreEqual(1, inserter.Calls,
                "the part that triggered the insert must NOT be re-visited");
            Assert.AreEqual(0, inserter.Inserted.Calls,
                "a part added mid-dispatch does not see the in-flight event");
        }

        [Test]
        public void SelfRemoval_DoesNotSkipTheSuccessor()
        {
            // The mirror bug: [Remover, C]. Removing self slides C into
            // the current slot; the naive walk increments past it.
            var entity = new Entity { BlueprintName = "probe" };
            var remover = new SelfRemoverPart();
            var successor = new CountingPart();
            entity.AddPart(remover);
            entity.AddPart(successor);

            Probe(entity);

            Assert.AreEqual(1, remover.Calls);
            Assert.AreEqual(1, successor.Calls,
                "the part after a self-removing part must still be visited");
        }

        [Test]
        public void Append_MidDispatch_IsStillVisited()
        {
            // Pin of EXISTING behavior, unchanged by the fix: a part
            // appended past the cursor is visited for the in-flight
            // event (it sits ahead of the walk).
            var entity = new Entity { BlueprintName = "probe" };
            var appender = new AppenderPart();
            entity.AddPart(appender);

            Probe(entity);

            Assert.AreEqual(1, appender.Appended.Calls,
                "appended-ahead parts keep seeing the in-flight event");
        }

        // ── The real F18 repro ───────────────────────────────────────

        [Test]
        public void FirstIgnition_AppliesTheHeatDoseExactlyOnce()
        {
            // The shipped bug: an entity with NO StatusEffectsPart takes
            // ApplyHeat hot enough to ignite. TryIgnite → ApplyEffect
            // (BurningEffect) → lazy StatusEffectsPart → front-insert →
            // ThermalPart re-visited → the SAME joules applied twice.
            // The calibrated FireDose anchors (fix dc1f533f) depend on
            // doses landing exactly once.
            var zone = new Zone("Z");
            var tinder = new Entity { ID = "tinder", BlueprintName = "Tinder" };
            tinder.AddPart(new RenderPart { DisplayName = "tinder" });
            tinder.AddPart(new MaterialPart
            { MaterialTagsRaw = "Organic,Wood", Combustibility = 0.6f });
            tinder.AddPart(new ThermalPart
            { Temperature = 25f, FlameTemperature = 100f, HeatCapacity = 1f });
            zone.AddEntity(tinder, 5, 5);
            Assert.IsNull(tinder.GetPart<StatusEffectsPart>(),
                "setup: the lazy-creation path must be live for this repro");

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)200f);
            heat.SetParameter("Radiant", (object)false);
            heat.SetParameter("Source", (object)null);
            heat.SetParameter("Zone", (object)zone);
            tinder.FireEvent(heat);
            heat.Release();

            Assert.IsTrue(tinder.HasEffect<BurningEffect>(), "200J over a 100° flashpoint ignites");
            Assert.AreEqual(225f, tinder.GetPart<ThermalPart>().Temperature, 0.01f,
                "25° + 200J/1.0 capacity applied ONCE — the double-visit read 425°");
        }
    }
}
