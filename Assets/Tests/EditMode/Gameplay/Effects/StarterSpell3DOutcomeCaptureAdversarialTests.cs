using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using F = CavesOfOoo.Tests.StarterSpell3DCaptureFixture;

namespace CavesOfOoo.Tests
{
    /// <summary>Contact recording cannot fabricate an occupant, leak a nested cast,
    /// lose the original contact during displacement, or mutate a committed result.</summary>
    public sealed class StarterSpell3DOutcomeCaptureAdversarialTests
    {
        [SetUp] public void Setup() => F.Setup();
        [TearDown] public void Cleanup() => F.Cleanup();

        // Reflection keeps the RED gate executable before the new API exists.
        // This is an assertion failure, rather than a compile-only RED for every test.
        private static bool TargetAt(Zone zone, Entity target, Point contact)
        {
            var method = typeof(SpellFxCapture).GetMethod("TargetAt", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(Zone), typeof(Entity), typeof(Point) }, null);
            Assert.NotNull(method, "Missing validated physical-contact capture API.");
            Assert.AreEqual(typeof(bool), method.ReturnType);
            return (bool)method.Invoke(null, new object[] { zone, target, contact });
        }

        [TestCase(6, 10, true)]
        [TestCase(-1, 10, false)]
        [TestCase(80, 10, false)]
        [TestCase(6, 9, false)]
        [TestCase(7, 10, false)]
        public void Adversarial_OnlyAnActuallyOccupiedContactCanCreateAResult(int x, int y, bool valid)
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var target = F.Actor(zone, "target", 6, 9, "0,1");
            using (var capture = new SpellFxCapture("contact", zone, caster))
            {
                Assert.AreEqual(valid, TargetAt(zone, target, new Point(x, y)));
                capture.Commit();
            }
            var sequence = F.Single();
            Assert.AreEqual(valid ? 1 : 0, sequence.Targets.Count);
            if (valid) Assert.AreEqual(new Point(6, 10), sequence.Targets[0].Cell);
            else CollectionAssert.AreEqual(new[] { new Point(5, 10) }, sequence.AffectedCells,
                "An invalid contact must not paint its requested cell or the target's anchor.");
        }

        [TestCase("foreign-zone")]
        [TestCase("null-zone")]
        [TestCase("null-target")]
        [TestCase("absent-target")]
        public void Adversarial_InvalidContactContextCannotBorrowTheActiveCapture(string mode)
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var target = F.Actor(zone, "target", 6, 10);
            if (mode == "absent-target") zone.RemoveEntity(target);
            using (var capture = new SpellFxCapture("active", zone, caster))
            {
                var suppliedZone = mode == "foreign-zone" ? new Zone() : mode == "null-zone" ? null : zone;
                Assert.IsFalse(TargetAt(suppliedZone, mode == "null-target" ? null : target, new Point(6, 10)));
                capture.Commit();
            }
            Assert.IsEmpty(F.Single().Targets);
        }

        [Test] public void Adversarial_NoScopeOrDisposedScopeCannotAccumulateLaterHits()
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10); var target = F.Actor(zone, "target", 6, 10);
            Assert.IsFalse(TargetAt(zone, target, new Point(6, 10)));
            using (var abandoned = new SpellFxCapture("abandoned", zone, caster))
                Assert.IsTrue(TargetAt(zone, target, new Point(6, 10)));
            Assert.IsFalse(TargetAt(zone, target, new Point(6, 10)));
            Assert.IsEmpty(SpellFxBus.Drain());
            using (var clean = new SpellFxCapture("clean", zone, caster)) clean.Commit();
            Assert.IsEmpty(F.Single().Targets);
        }

        [Test] public void Adversarial_FirstContactIsStableAcrossRepeatedCellsAndDamageHooks()
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var target = F.Actor(zone, "target", 6, 9, "0,1;1,1");
            using (var capture = new SpellFxCapture("contact", zone, caster))
            {
                Assert.IsTrue(TargetAt(zone, target, new Point(6, 10)));
                Assert.IsTrue(TargetAt(zone, target, new Point(7, 10)));
                CombatSystem.ApplyDamage(target, 3, caster, zone);
                capture.Commit();
            }
            var sequence = F.Single(); Assert.AreEqual(1, sequence.Targets.Count);
            Assert.AreEqual(new Point(6, 10), sequence.Targets[0].Cell);
            Assert.AreEqual(3, sequence.Targets[0].Damage);
            CollectionAssert.AreEqual(new[] { new Point(6, 10) }, sequence.AffectedCells);
        }

        [TestCase(false)] [TestCase(true)]
        public void Adversarial_FinalContactTracksOnlyActualAnchorTranslation(bool moved)
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var target = F.Actor(zone, "target", 6, 9, "0,1");
            using (var capture = new SpellFxCapture("contact", zone, caster))
            {
                Assert.IsTrue(TargetAt(zone, target, new Point(6, 10)));
                if (moved) Assert.IsTrue(zone.MoveEntity(target, 9, 8));
                capture.Commit();
            }
            var result = F.Single().Targets.Single();
            Assert.AreEqual(new Point(6, 10), result.Cell);
            Assert.AreEqual(moved ? new Point(9, 9) : new Point(6, 10), result.FinalCell);
            Assert.AreEqual(moved, result.Moved);
        }

        [TestCase(false)] [TestCase(true)]
        public void Adversarial_RemovedOwnerRetainsContactButDoesNotInventAVisualDestination(bool died)
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var target = F.Actor(zone, "target", 6, 9, "0,1");
            using (var capture = new SpellFxCapture("contact", zone, caster))
            {
                Assert.IsTrue(TargetAt(zone, target, new Point(6, 10)));
                if (died) target.GetStat("Hitpoints").BaseValue = 0;
                zone.RemoveEntity(target);
                capture.Commit();
            }
            var result = F.Single().Targets.Single();
            Assert.AreEqual(new Point(6, 10), result.Cell);
            Assert.AreEqual(new Point(-1, -1), result.FinalCell);
            Assert.AreEqual(died, result.Died); Assert.IsFalse(result.Moved);
        }

        [Test] public void Adversarial_SameBlueprintOwnersKeepDistinctContactAndDamage()
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var first = F.Actor(zone, "first", 6, 9, "0,1"); var second = F.Actor(zone, "second", 8, 9, "0,1");
            Assert.AreEqual(first.BlueprintName, second.BlueprintName, "This is a same-blueprint collision probe.");
            using (var capture = new SpellFxCapture("two", zone, caster))
            {
                Assert.IsTrue(TargetAt(zone, first, new Point(6, 10)));
                Assert.IsTrue(TargetAt(zone, second, new Point(8, 10)));
                CombatSystem.ApplyDamage(first, 3, caster, zone); CombatSystem.ApplyDamage(second, 7, caster, zone);
                capture.Commit();
            }
            var sequence = F.Single(); Assert.AreEqual(2, sequence.Targets.Count);
            Assert.AreEqual("first", sequence.Targets[0].TargetId); Assert.AreEqual(3, sequence.Targets[0].Damage);
            Assert.AreEqual("second", sequence.Targets[1].TargetId); Assert.AreEqual(7, sequence.Targets[1].Damage);
            Assert.AreEqual(new Point(8, 10), sequence.Targets[1].Cell);
        }

        [Test] public void Adversarial_InnerCaptureContactDoesNotOverwriteOuterContact()
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var target = F.Actor(zone, "target", 6, 9, "0,1;1,1");
            using (var outer = new SpellFxCapture("outer", zone, caster))
            {
                Assert.IsTrue(TargetAt(zone, target, new Point(6, 10)));
                using (var inner = new SpellFxCapture("inner", zone, caster))
                { Assert.IsTrue(TargetAt(zone, target, new Point(7, 10))); inner.Commit(); }
                CombatSystem.ApplyDamage(target, 2, caster, zone);
                outer.Commit();
            }
            var sequences = SpellFxBus.Drain(); Assert.AreEqual(2, sequences.Count);
            Assert.AreEqual(new Point(7, 10), sequences[0].Targets.Single().Cell);
            Assert.AreEqual(0, sequences[0].Targets.Single().Damage);
            Assert.AreEqual(new Point(6, 10), sequences[1].Targets.Single().Cell);
            Assert.AreEqual(2, sequences[1].Targets.Single().Damage);
        }

        [Test] public void Adversarial_CommittedContactAndEffectsDoNotFollowFutureMutationOrSecondCommit()
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var target = F.Actor(zone, "target", 6, 9, "0,1");
            using (var capture = new SpellFxCapture("copied", zone, caster))
            {
                Assert.IsTrue(TargetAt(zone, target, new Point(6, 10)));
                target.ApplyEffect(new WetEffect(.8f), caster, zone);
                capture.Commit(); capture.Commit();
            }
            var sequence = F.Single(); var result = sequence.Targets.Single();
            Assert.IsTrue(zone.MoveEntity(target, 10, 12)); target.RemoveEffect<WetEffect>();
            Assert.AreEqual(new Point(6, 10), result.Cell); Assert.AreEqual(new Point(6, 10), result.FinalCell);
            Assert.Contains(nameof(WetEffect), result.AppliedEffects.ToArray()); Assert.IsFalse(result.Moved);
            Assert.IsEmpty(SpellFxBus.Drain());
        }

        [TestCase(true)] [TestCase(false)]
        public void Adversarial_PathClippingUsesOccupiedCellsAndPassesAnEmptyAnchorHole(bool body)
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10);
            var target = F.Actor(zone, "target", 6, 10, body ? "1,0;2,0" : "0,1");
            using (var capture = new SpellFxCapture("clip", zone, caster))
            {
                for (int x = 6; x <= 9; x++) SpellFxCapture.PathCell(zone, x, 10);
                SpellFxCapture.EndPathAt(zone, target);
                capture.Commit();
            }
            var sequence = F.Single();
            Assert.AreEqual(body ? new Point(7, 10) : new Point(9, 10), sequence.Path.Last());
            Assert.AreEqual(body ? 1 : 0, sequence.Targets.Count);
        }

        [Test] public void Adversarial_OrdinaryAnchorCaptureRemainsBackwardsCompatible()
        {
            var zone = new Zone(); var caster = F.Actor(zone, "caster", 5, 10); var target = F.Actor(zone, "target", 6, 10);
            using (var capture = new SpellFxCapture("ordinary", zone, caster))
            { SpellFxCapture.Target(zone, target); CombatSystem.ApplyDamage(target, 4, caster, zone); capture.Commit(); }
            var result = F.Single().Targets.Single();
            Assert.AreEqual(new Point(6, 10), result.Cell); Assert.AreEqual(result.Cell, result.FinalCell);
            Assert.AreEqual(4, result.Damage); Assert.IsFalse(result.Moved);
        }

    }
}
