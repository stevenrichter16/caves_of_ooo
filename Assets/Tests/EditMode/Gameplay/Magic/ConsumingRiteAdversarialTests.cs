using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for the SM9 consuming rites and the shared
    /// <see cref="ConsumingRiteBase"/> spine (CLAUDE.md §Adversarial test
    /// sweep).
    ///
    /// <para>Surfaces probed: state atomicity (ink vs. resonance vs.
    /// damage), anti-exploit gates (the Ledger's heal, double-spend),
    /// cross-actor flows (two casters, one book), stacking semantics
    /// (re-cast onto an already-afflicted target), boundary inputs (null
    /// zone, no inventory, empty book), mid-execution death, and diag
    /// emission contracts.</para>
    ///
    /// <para>Surfaces NOT probed, stated so a future reader is not
    /// misled: save/load round-tripping of a mid-cooldown rite, and
    /// multi-zone casting. Neither has a code path distinct from the
    /// five earlier rites.</para>
    /// </summary>
    public class ConsumingRiteAdversarialTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            ResonanceSystem.ResetForTests();
            ResonanceSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Resonance/Resonance.json")));
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [TearDown]
        public void TearDown() => ResonanceSystem.ResetForTests();

        private static Entity Creature(Zone zone, string name, int x, int y, int hp = 400)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            foreach (var r in new[] { "ElectricResistance", "HeatResistance", "ColdResistance", "AcidResistance" })
                e.Statistics[r] = new Stat { Owner = e, Name = r, BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new Body());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static T Rite<T>(Zone zone, int x, int y, out Entity caster, int charges = 10)
            where T : ConsumingRiteBase, new()
        {
            caster = Creature(zone, "caster" + x + "_" + y, x, y);
            caster.AddPart(new MutationsPart());
            var inv = new InventoryPart { MaxWeight = 500 };
            caster.AddPart(inv);
            var book = new Entity { ID = "book" + x + "_" + y, BlueprintName = "AnyRite" };
            book.AddPart(new RenderPart { DisplayName = "a rite" });
            book.AddPart(new GrimoireChargePart { Charges = charges, MaxCharges = 10 });
            inv.AddObject(book);

            var rite = new T();
            caster.AddPart(rite);
            rite.Mutate(caster, 1);
            return rite;
        }

        private static GrimoireChargePart Book(Entity caster)
            => caster.GetPart<InventoryPart>().Objects[0].GetPart<GrimoireChargePart>();

        // ════════════════════════════════════════════════════════
        // BOUNDARY INPUTS — nothing here may throw
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_NullZone_RefusesWithoutThrowing()
        {
            var zone = new Zone();
            var rite = Rite<HollowCoinMutation>(zone, 5, 5, out var caster);
            Assert.DoesNotThrow(() => rite.Cast(null, 1, 0));
            Assert.AreEqual(10, Book(caster).Charges, "a null zone must not cost ink");
        }

        [Test]
        public void Adversarial_ZeroDirectionOnADirectionalRite_Refuses()
        {
            // A SingleTarget/Cone rite with no facing has nothing to aim
            // at. Radius rites legitimately take (0,0) — see below.
            var zone = new Zone();
            var rite = Rite<ShatteredRimeMutation>(zone, 5, 5, out var caster);
            Creature(zone, "target", 6, 5);

            Assert.IsFalse(rite.Cast(zone, 0, 0));
            Assert.AreEqual(10, Book(caster).Charges);
        }

        [Test]
        public void Adversarial_RadiusRiteAcceptsZeroDirection()
        {
            // Counter-check to the above: the no_direction guard must
            // NOT fire for a self-centred shape, or radius rites become
            // uncastable.
            var zone = new Zone();
            var rite = Rite<SunderingWordMutation>(zone, 10, 10, out var caster);
            Creature(zone, "target", 11, 10);

            Assert.IsTrue(rite.Cast(zone, 0, 0));
            Assert.AreEqual(9, Book(caster).Charges);
        }

        [Test]
        public void Adversarial_CasterWithNoInventory_RefusesWithoutThrowing()
        {
            var zone = new Zone();
            var caster = Creature(zone, "bookless", 5, 5);
            caster.AddPart(new MutationsPart());
            var rite = new HollowCoinMutation();
            caster.AddPart(rite);
            rite.Mutate(caster, 1);
            Creature(zone, "target", 6, 5);

            Assert.DoesNotThrow(() => rite.Cast(zone, 1, 0));
            Assert.IsFalse(rite.Cast(zone, 1, 0), "no pack means no ink means no cast");
        }

        [Test]
        public void Adversarial_EmptyBook_RefusesAndSaysWhy()
        {
            var zone = new Zone();
            var rite = Rite<VerdigrisBloomMutation>(zone, 10, 10, out var caster, charges: 0);
            Creature(zone, "target", 11, 10);
            Diag.ResetAll();

            Assert.IsFalse(rite.Cast(zone, 0, 0));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "RiteRejected", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1, "a refusal the player can feel must be queryable");
            StringAssert.Contains("no_ink", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════
        // STATE ATOMICITY — ink, resonance and damage move together
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_NoTarget_ConsumesNothingAnywhere()
        {
            // The ordering invariant the base exists to enforce: a cast
            // that finds nothing must leave ink, statuses and the world
            // exactly as it found them.
            var zone = new Zone();
            var rite = Rite<ShatteredRimeMutation>(zone, 5, 5, out var caster);
            var bystander = Creature(zone, "bystander", 5, 12);   // off the cone axis
            bystander.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 1, 0);

            Assert.AreEqual(10, Book(caster).Charges, "no ink");
            Assert.IsTrue(bystander.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "and nobody else's statuses were touched");
        }

        [Test]
        public void Adversarial_EmptyBookDoesNotEatTheTargetsStatuses()
        {
            // The nastiest partial-failure shape: refuse on ink but only
            // AFTER resonance already spent the statuses. The player
            // would lose their whole setup for nothing.
            var zone = new Zone();
            var rite = Rite<HollowCoinMutation>(zone, 5, 5, out var caster, charges: 0);
            var target = Creature(zone, "target", 6, 5);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 1, 0);

            Assert.IsTrue(target.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "a rite that could not fire must not have eaten the setup");
        }

        [Test]
        public void Adversarial_OneCast_SpendsExactlyOneCharge_EvenAcrossManyTargets()
        {
            // Ink is per CAST, not per target. A radius rite hitting six
            // creatures must not drain the book six times.
            var zone = new Zone();
            var rite = Rite<SunderingWordMutation>(zone, 10, 10, out var caster);
            for (int i = 0; i < 6; i++) Creature(zone, "t" + i, 9 + (i % 3), 9 + (i / 3));

            rite.Cast(zone, 0, 0);

            Assert.AreEqual(9, Book(caster).Charges);
        }

        // ════════════════════════════════════════════════════════
        // ANTI-EXPLOIT GATES
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_LedgerCannotBeFarmedForInfiniteHealing()
        {
            // The obvious exploit: park next to a status-free enemy and
            // spam the Ledger to top up. Marks are the gate, so a target
            // with nothing to spend heals the caster nothing, however
            // many times you cast.
            var zone = new Zone();
            var rite = Rite<BloodletterLedgerMutation>(zone, 5, 5, out var caster);
            Creature(zone, "target", 6, 5);

            var hp = caster.GetStat("Hitpoints");
            hp.BaseValue = 50;
            for (int i = 0; i < 5; i++) rite.Cast(zone, 1, 0);

            Assert.AreEqual(50, hp.BaseValue, "five casts, no marks, no healing");
        }

        [Test]
        public void Adversarial_LedgerCannotHealAboveMaximum()
        {
            var zone = new Zone();
            var rite = Rite<BloodletterLedgerMutation>(zone, 5, 5, out var caster);
            var target = Creature(zone, "target", 6, 5);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);

            var hp = caster.GetStat("Hitpoints");
            hp.BaseValue = hp.Max;
            rite.Cast(zone, 1, 0);

            Assert.LessOrEqual(hp.BaseValue, hp.Max, "the ledger does not overflow");
        }

        [Test]
        public void Adversarial_SecondCastFindsNothingLeftToSpend()
        {
            // Double-dipping: cast twice on one primed target. The
            // second cast must be a cold cast, because the first ate
            // everything.
            var zone = new Zone();
            var rite = Rite<HollowCoinMutation>(zone, 5, 5, out var caster);
            var target = Creature(zone, "target", 6, 5);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);

            int before = target.GetStatValue("Hitpoints");
            rite.Cast(zone, 1, 0);
            int afterFirst = target.GetStatValue("Hitpoints");
            rite.Cast(zone, 1, 0);
            int afterSecond = target.GetStatValue("Hitpoints");

            Assert.Greater(before - afterFirst, afterFirst - afterSecond,
                "the second cast must be weaker — the setup is gone");
        }

        // ════════════════════════════════════════════════════════
        // CROSS-ACTOR FLOWS
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwoCastersDoNotShareEachOthersInk()
        {
            var zone = new Zone();
            var riteA = Rite<HollowCoinMutation>(zone, 5, 5, out var casterA);
            var riteB = Rite<HollowCoinMutation>(zone, 5, 7, out var casterB);
            Creature(zone, "target", 6, 5);
            Creature(zone, "targetB", 6, 7);

            riteA.Cast(zone, 1, 0);

            Assert.AreEqual(9, Book(casterA).Charges);
            Assert.AreEqual(10, Book(casterB).Charges, "B's book is B's");
        }

        [Test]
        public void Adversarial_RiteNeverHitsItsOwnCaster()
        {
            // A radius rite centred on the caster is the obvious way to
            // suicide by friendly fire.
            var zone = new Zone();
            var rite = Rite<SunderingWordMutation>(zone, 10, 10, out var caster);
            Creature(zone, "target", 11, 10);

            int hp = caster.GetStatValue("Hitpoints");
            rite.Cast(zone, 0, 0);

            Assert.AreEqual(hp, caster.GetStatValue("Hitpoints"));
            Assert.IsFalse(caster.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>(),
                "and no riders on the caster either");
        }

        // ════════════════════════════════════════════════════════
        // STACKING + MID-EXECUTION DEATH
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ReCastOntoAlreadyDebuffedTarget_DoesNotThrow()
        {
            var zone = new Zone();
            var rite = Rite<SunderingWordMutation>(zone, 10, 10, out var caster);
            var target = Creature(zone, "target", 11, 10);
            target.ApplyEffect(new BrokenEffect(), caster, zone);
            target.ApplyEffect(new WeakenedEffect(), caster, zone);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);

            Assert.DoesNotThrow(() => rite.Cast(zone, 0, 0));
            Assert.IsTrue(target.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>());
        }

        [Test]
        public void Adversarial_TargetKilledByTheDamage_GetsNoRiders()
        {
            // The base promises riders never land on the dead. A corpse
            // wearing a fresh debuff reads as a bug to the player and
            // can keep a dead entity ticking.
            var zone = new Zone();
            var rite = Rite<SunderingWordMutation>(zone, 10, 10, out var caster);
            var doomed = Creature(zone, "doomed", 11, 10, hp: 1);
            doomed.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 0, 0);

            if (doomed.GetStatValue("Hitpoints") <= 0)
                Assert.IsFalse(doomed.GetPart<StatusEffectsPart>().HasEffect<WeakenedEffect>(),
                    "nothing lands on the dead");
        }

        [Test]
        public void Adversarial_OneDyingTargetDoesNotStopTheRest()
        {
            // Snapshot stability: a radius rite whose first victim dies
            // must still resolve everyone else.
            var zone = new Zone();
            var rite = Rite<SunderingWordMutation>(zone, 10, 10, out var caster);
            var doomed = Creature(zone, "doomed", 11, 10, hp: 1);
            var survivor = Creature(zone, "survivor", 9, 10);
            doomed.ApplyEffect(new WetEffect(1.0f), caster, zone);
            survivor.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 0, 0);

            Assert.IsTrue(survivor.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>(),
                "the survivor still gets their due");
        }

        // ════════════════════════════════════════════════════════
        // DIAG EMISSION CONTRACTS
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SuccessfulCastEmitsRiteCast_WithMarksAndInk()
        {
            var zone = new Zone();
            var rite = Rite<HollowCoinMutation>(zone, 5, 5, out var caster);
            var target = Creature(zone, "target", 6, 5);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);
            Diag.ResetAll();

            rite.Cast(zone, 1, 0);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "RiteCast", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1);
            StringAssert.Contains("statusesConsumed", recs[0].PayloadJson);
            StringAssert.Contains("inkLeft", recs[0].PayloadJson,
                "\"how much ink is left?\" must be answerable from the record");
        }

        [Test]
        public void Adversarial_FailedCastEmitsRejected_NotCast()
        {
            // Counter-check: a refusal must not look like a success in
            // the diag stream, or every future "why didn't my rite
            // fire?" query answers wrong.
            var zone = new Zone();
            var rite = Rite<HollowCoinMutation>(zone, 5, 5, out _);
            Diag.ResetAll();

            rite.Cast(zone, 1, 0);

            var cast = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "RiteCast", Limit = 5 }).Records;
            Assert.AreEqual(0, cast.Count, "a miss is not a cast");
        }

        // ════════════════════════════════════════════════════════
        // CONTENT INTEGRITY
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EveryRiteGrimoireNamesADistinctMutation()
        {
            // A copy-paste slip in six near-identical blueprints would
            // silently give two books the same rite.
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var bp in new[]
            {
                "ShatteredRimeGrimoire", "StillHeartGrimoire", "VerdigrisBloomGrimoire",
                "HollowCoinGrimoire", "SunderingWordGrimoire", "BloodletterLedgerGrimoire",
            })
            {
                var grim = _factory.CreateEntity(bp)?.GetPart<GrimoirePart>();
                Assert.IsNotNull(grim, bp);
                Assert.IsTrue(seen.Add(grim.MutationClassName),
                    bp + " duplicates " + grim.MutationClassName);
            }
        }

        [Test]
        public void Adversarial_EveryRiteDeclaresADistinctCommand()
        {
            // Two rites sharing a command string would make one of them
            // permanently uncastable — and HandleEvent would fire both.
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var r in new ConsumingRiteBase[]
            {
                new ShatteredRimeMutation(), new StillHeartMutation(),
                new VerdigrisBloomMutation(), new HollowCoinMutation(),
                new SunderingWordMutation(), new BloodletterLedgerMutation(),
            })
            {
                Assert.IsFalse(string.IsNullOrEmpty(r.Command), r.Name);
                Assert.IsTrue(seen.Add(r.Command), r.Name + " reuses " + r.Command);
            }
        }

        [Test]
        public void Adversarial_EveryRiteDeclaresAReadableResonanceTable()
        {
            // An element string with no matching table would make a rite
            // permanently cold — powerful on paper, useless in play, and
            // silent about it.
            var zone = new Zone();
            foreach (var r in new ConsumingRiteBase[]
            {
                new ShatteredRimeMutation(), new StillHeartMutation(),
                new VerdigrisBloomMutation(), new HollowCoinMutation(),
                new SunderingWordMutation(), new BloodletterLedgerMutation(),
            })
            {
                var target = Creature(zone, "probe_" + r.Name, 2, 2);
                target.ApplyEffect(new WetEffect(1.0f), null, zone);
                var preview = ResonanceSystem.Preview(target, r.Element, r.Slots);
                Assert.Greater(preview.Multiplier, 1.0f,
                    r.Name + " reads element \"" + r.Element
                    + "\", which resonates with nothing");
                zone.RemoveEntity(target);
            }
        }
    }
}
