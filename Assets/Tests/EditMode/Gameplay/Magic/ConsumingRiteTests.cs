using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The six world-found rites: grimoires you dig out of vaults and
    /// bookshelves rather than buy.
    ///
    /// <para>All six consume statuses and all six are powerful — but
    /// only when fed. The shared discipline these tests protect is that
    /// a rite cast COLD is nearly worthless, which is what keeps them
    /// finishers instead of openers.</para>
    /// </summary>
    public class ConsumingRiteTests
    {
        private EntityFactory _factory;

        private static readonly string[] RiteGrimoires =
        {
            "ShatteredRimeGrimoire", "StillHeartGrimoire", "VerdigrisBloomGrimoire",
            "HollowCoinGrimoire", "SunderingWordGrimoire", "BloodletterLedgerGrimoire",
        };

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

        private static T Caster<T>(Zone zone, int x, int y, out Entity caster)
            where T : CavesOfOoo.Skills.ConsumingRiteSkillBase, new()
        {
            caster = Creature(zone, "caster", x, y);
            caster.AddPart(new ActivatedAbilitiesPart());
            caster.AddPart(new CavesOfOoo.Skills.SkillsPart());
            var inv = new InventoryPart { MaxWeight = 500 };
            caster.AddPart(inv);
            var book = new Entity { ID = "book", BlueprintName = "AnyRite" };
            book.AddPart(new RenderPart { DisplayName = "a rite" });
            book.AddPart(new GrimoireChargePart { Charges = 10, MaxCharges = 10 });
            inv.AddObject(book);

            var rite = new T();
            caster.GetPart<CavesOfOoo.Skills.SkillsPart>().AddSkill(rite);
            return rite;
        }

        // ── The shared discipline ────────────────────────────────

        [Test]
        public void EveryRite_CastCold_IsNearlyWorthless()
        {
            // The rule that keeps six powerful rites from being six
            // better skills. With nothing to spend, resonance returns
            // x1.0 and every base number here is small.
            void Check<T>(int allowance) where T : CavesOfOoo.Skills.ConsumingRiteSkillBase, new()
            {
                var zone = new Zone();
                var rite = Caster<T>(zone, 5, 5, out var caster);
                var target = Creature(zone, "target", 6, 5);
                int hp = target.GetStatValue("Hitpoints");

                rite.Cast(zone, 1, 0);

                int lost = hp - target.GetStatValue("Hitpoints");
                Assert.LessOrEqual(lost, allowance,
                    typeof(T).Name + " hits far too hard with nothing consumed");
            }

            Check<CavesOfOoo.Skills.Rites_ShatteredRime>(10);
            Check<CavesOfOoo.Skills.Rites_StillHeart>(10);
            Check<CavesOfOoo.Skills.Rites_VerdigrisBloom>(10);
            Check<CavesOfOoo.Skills.Rites_HollowCoin>(10);
            Check<CavesOfOoo.Skills.Rites_SunderingWord>(10);
            Check<CavesOfOoo.Skills.Rites_BloodletterLedger>(10);
        }

        [Test]
        public void EveryRite_HitsHarderWhenFed()
        {
            // The counter-check to the above: if these were ALSO weak
            // when fed, the rites would simply be bad.
            void Check<T>() where T : CavesOfOoo.Skills.ConsumingRiteSkillBase, new()
            {
                var coldZone = new Zone();
                var coldRite = Caster<T>(coldZone, 5, 5, out _);
                var coldTarget = Creature(coldZone, "t", 6, 5);
                int coldHp = coldTarget.GetStatValue("Hitpoints");
                coldRite.Cast(coldZone, 1, 0);
                int coldLost = coldHp - coldTarget.GetStatValue("Hitpoints");

                var fedZone = new Zone();
                var fedRite = Caster<T>(fedZone, 5, 5, out var fedCaster);
                var fedTarget = Creature(fedZone, "t", 6, 5);
                fedTarget.ApplyEffect(new WetEffect(1.0f), fedCaster, fedZone);
                int fedHp = fedTarget.GetStatValue("Hitpoints");
                fedRite.Cast(fedZone, 1, 0);
                int fedLost = fedHp - fedTarget.GetStatValue("Hitpoints");

                Assert.Greater(fedLost, coldLost,
                    typeof(T).Name + " must pay more for a status than for nothing");
            }

            // Wet resonates with every element, so it feeds all of them.
            Check<CavesOfOoo.Skills.Rites_ShatteredRime>();
            Check<CavesOfOoo.Skills.Rites_VerdigrisBloom>();
            Check<CavesOfOoo.Skills.Rites_HollowCoin>();
            Check<CavesOfOoo.Skills.Rites_SunderingWord>();
        }

        [Test]
        public void EveryRite_SpendsInkOnACast_AndNoneOnAMiss()
        {
            void Check<T>() where T : CavesOfOoo.Skills.ConsumingRiteSkillBase, new()
            {
                var zone = new Zone();
                var rite = Caster<T>(zone, 5, 5, out var caster);
                var book = caster.GetPart<InventoryPart>().Objects[0].GetPart<GrimoireChargePart>();

                // No target anywhere.
                rite.Cast(zone, 1, 0);
                Assert.AreEqual(10, book.Charges,
                    typeof(T).Name + " wasted a charge on empty air");

                Creature(zone, "target", 6, 5);
                rite.Cast(zone, 1, 0);
                Assert.AreEqual(9, book.Charges,
                    typeof(T).Name + " did not spend its charge");
            }

            Check<CavesOfOoo.Skills.Rites_ShatteredRime>();
            Check<CavesOfOoo.Skills.Rites_StillHeart>();
            Check<CavesOfOoo.Skills.Rites_VerdigrisBloom>();
            Check<CavesOfOoo.Skills.Rites_HollowCoin>();
            Check<CavesOfOoo.Skills.Rites_SunderingWord>();
            Check<CavesOfOoo.Skills.Rites_BloodletterLedger>();
        }

        // ── What makes each one different ────────────────────────

        [Test]
        public void StillHeart_RemovesAnEliteInsteadOfKillingIt()
        {
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_StillHeart>(zone, 5, 5, out var caster);
            var elite = Creature(zone, "elite", 6, 5);
            elite.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 1, 0);

            Assert.IsTrue(elite.GetPart<StatusEffectsPart>().HasEffect<AsleepByGasEffect>(),
                "it takes them out of the fight");
            Assert.Greater(elite.GetStatValue("Hitpoints"), 0,
                "without killing them — that is the trade");
        }

        [Test]
        public void StillHeart_SleepsLongerForMoreMarks()
        {
            // The docstring claims duration scales per mark. With one
            // slot it could not, so this is the test that keeps the
            // claim honest.
            int DurationWith(params Effect[] primed)
            {
                var zone = new Zone();
                var rite = Caster<CavesOfOoo.Skills.Rites_StillHeart>(zone, 5, 5, out var caster);
                var target = Creature(zone, "target", 6, 5);
                foreach (var e in primed) target.ApplyEffect(e, caster, zone);

                rite.Cast(zone, 1, 0);

                var sleep = target.GetPart<StatusEffectsPart>().GetEffect<AsleepByGasEffect>();
                return sleep?.Duration ?? 0;
            }

            int one = DurationWith(new WetEffect(1.0f));
            int two = DurationWith(new WetEffect(1.0f), new FrozenEffect());

            Assert.Greater(one, 0, "one mark must put them under");
            Assert.Greater(two, one,
                "two marks (" + two + ") must sleep longer than one (" + one + ")");
        }

        [Test]
        public void SunderingWord_DebuffsTheWholeRadius()
        {
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_SunderingWord>(zone, 10, 10, out var caster);
            var a = Creature(zone, "a", 11, 10);
            var b = Creature(zone, "b", 9, 10);
            a.ApplyEffect(new WetEffect(1.0f), caster, zone);
            b.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 0, 0);

            foreach (var t in new[] { a, b })
            {
                Assert.IsTrue(t.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>());
                Assert.IsTrue(t.GetPart<StatusEffectsPart>().HasEffect<WeakenedEffect>());
            }
        }

        [Test]
        public void BloodletterLedger_HealsTheCaster_ButOnlyForMarksSpent()
        {
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_BloodletterLedger>(zone, 5, 5, out var caster);
            var target = Creature(zone, "target", 6, 5);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);

            var hp = caster.GetStat("Hitpoints");
            hp.BaseValue = 100;
            rite.Cast(zone, 1, 0);

            Assert.Greater(hp.BaseValue, 100, "the credit side of the ledger");
        }

        [Test]
        public void BloodletterLedger_CastCold_HealsNothing()
        {
            // Counter-check: otherwise it is a free top-up between
            // fights, which would be the most abusable thing here.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_BloodletterLedger>(zone, 5, 5, out var caster);
            Creature(zone, "target", 6, 5);

            var hp = caster.GetStat("Hitpoints");
            hp.BaseValue = 100;
            rite.Cast(zone, 1, 0);

            Assert.AreEqual(100, hp.BaseValue,
                "no marks spent, no healing — this must not be a rest button");
        }

        [Test]
        public void VerdigrisBloom_StripsArmourAcrossTheRadius()
        {
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_VerdigrisBloom>(zone, 10, 10, out var caster);
            var target = Creature(zone, "target", 11, 10);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 0, 0);

            Assert.IsTrue(target.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>());
        }

        [Test]
        public void ShatteredRime_IsACone_AndCatchesAClump()
        {
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_ShatteredRime>(zone, 10, 10, out var caster);
            var centre = Creature(zone, "centre", 12, 10);
            var offAxis = Creature(zone, "offAxis", 12, 11);
            int c = centre.GetStatValue("Hitpoints");
            int o = offAxis.GetStatValue("Hitpoints");

            rite.Cast(zone, 1, 0);

            Assert.Less(centre.GetStatValue("Hitpoints"), c);
            Assert.Less(offAxis.GetStatValue("Hitpoints"), o, "a cone catches the clump");
        }

        [Test]
        public void HollowCoin_SpendsStatusesNoOtherRiteCanReach()
        {
            // The plan's promise: "any three statuses of any kind".
            // Burning is on no Electric/Cold/Acid table — of the six
            // SM9 rites, only the wildcard reaches it. (Heat DOES
            // carry Burning at 0.75; no SM9 rite reads Heat.)
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_HollowCoin>(zone, 5, 5, out var caster);
            var target = Creature(zone, "target", 6, 5);
            target.ApplyEffect(new BurningEffect(), caster, zone);

            rite.Cast(zone, 1, 0);

            Assert.IsFalse(target.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "the coin pays out on anything");
        }

        [Test]
        public void WildcardTable_PaysLessPerMarkThanAMatchedRite()
        {
            // The balance rule that keeps a catch-all from obsoleting
            // the elemental rites. Same status, same slot count, same
            // base damage — only the table differs.
            var res = new[] { "Any", "Cold" };
            var mults = new float[2];
            for (int i = 0; i < 2; i++)
            {
                var zone = new Zone();
                var target = Creature(zone, "t", 6, 5);
                var caster = Creature(zone, "c", 5, 5);
                target.ApplyEffect(new WetEffect(1.0f), caster, zone);
                mults[i] = ResonanceSystem.Spend(target, res[i], 2, caster, zone).Multiplier;
            }

            Assert.Less(mults[0], mults[1],
                "wildcard " + mults[0] + " must pay less than matched " + mults[1]);
        }

        [Test]
        public void ShatteredRime_BreaksIceCreaturesToo()
        {
            // The fiction is "frozen flesh is brittle". If the hit were
            // tagged Cold, a cold-immune target would absorb its own
            // shattering — which is both wrong and the exact opposite of
            // what a player expects from this rite.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_ShatteredRime>(zone, 10, 10, out var caster);
            var iceThing = Creature(zone, "iceThing", 11, 10);
            iceThing.Statistics["ColdResistance"].BaseValue = 100;

            int hp = iceThing.GetStatValue("Hitpoints");
            rite.Cast(zone, 1, 0);

            Assert.Less(iceThing.GetStatValue("Hitpoints"), hp,
                "cold immunity must not make a creature unshatterable");
        }

        [Test]
        public void VerdigrisBloom_IsStillResistedByAcidImmunity()
        {
            // Counter-check to the above: untyped is a deliberate
            // exception, not the house style. A rite that corrodes
            // SHOULD do nothing to something that cannot corrode.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_VerdigrisBloom>(zone, 10, 10, out var caster);
            var inert = Creature(zone, "inert", 11, 10);
            inert.Statistics["AcidResistance"].BaseValue = 100;
            inert.ApplyEffect(new WetEffect(1.0f), caster, zone);

            int hp = inert.GetStatValue("Hitpoints");
            rite.Cast(zone, 0, 0);

            Assert.AreEqual(hp, inert.GetStatValue("Hitpoints"),
                "acid immunity must still mean something");
        }

        [Test]
        public void HollowCoin_IsUntyped_SoNothingResistsIt()
        {
            // The answer to an enemy you have no answer for.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_HollowCoin>(zone, 5, 5, out var caster);
            var warded = Creature(zone, "warded", 6, 5);
            foreach (var r in new[] { "ElectricResistance", "HeatResistance",
                                      "ColdResistance", "AcidResistance" })
                warded.Statistics[r].BaseValue = 100;
            warded.ApplyEffect(new WetEffect(1.0f), caster, zone);

            int hp = warded.GetStatValue("Hitpoints");
            rite.Cast(zone, 1, 0);

            Assert.Less(warded.GetStatValue("Hitpoints"), hp,
                "no elemental ward stops the coin");
        }

        // ── Cold-eye regressions (all RED before the fix) ───────

        [Test]
        public void StillHeart_DoesNotBuffTheEnemyItPutsToSleep()
        {
            // THE bug this rite shipped with. HibernatingEffect is a
            // SELF-buff — 5% max-HP regen per turn and Heat+Cold
            // resistance forced to 100 — so Still Heart healed the elite
            // it was meant to neutralise and made it immune to its own
            // Cold damage. AsleepByGasEffect is the hostile sleep.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_StillHeart>(zone, 5, 5, out var caster);
            var elite = Creature(zone, "elite", 6, 5);
            elite.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 1, 0);

            var fx = elite.GetPart<StatusEffectsPart>();
            Assert.IsTrue(fx.HasEffect<AsleepByGasEffect>(), "it sleeps");
            Assert.IsFalse(fx.HasEffect<HibernatingEffect>(),
                "and must NOT be handed the self-buff");
            Assert.AreEqual(0, elite.GetStatValue("ColdResistance"),
                "no resistance buff — this rite deals Cold");
            Assert.AreEqual(0, elite.GetStatValue("HeatResistance"));
        }

        [Test]
        public void StillHeart_SleeperWakesWhenStruck()
        {
            // The rite's docstring promised "breaks on damage" and the
            // shipped effect had no wake hook at all.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_StillHeart>(zone, 5, 5, out var caster);
            var elite = Creature(zone, "elite", 6, 5);
            elite.ApplyEffect(new WetEffect(1.0f), caster, zone);
            rite.Cast(zone, 1, 0);

            var sleep = elite.GetPart<StatusEffectsPart>().GetEffect<AsleepByGasEffect>();
            Assert.IsNotNull(sleep);
            Assert.Greater(sleep.Duration, 0);

            CombatSystem.ApplyDamage(elite, new Damage(1), caster, zone);

            Assert.AreEqual(0, sleep.Duration, "anyone touching them ends it");
        }

        [Test]
        public void BloodletterLedger_StillPays_WhenItsOwnDamageKills()
        {
            // The heal lived inside ApplyPayoff, which the base skips on
            // a dead target — so the better the setup, the more likely
            // the sustain silently vanished.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_BloodletterLedger>(zone, 5, 5, out var caster);
            var doomed = Creature(zone, "doomed", 6, 5, hp: 1);
            doomed.ApplyEffect(new WetEffect(1.0f), caster, zone);

            var hp = caster.GetStat("Hitpoints");
            hp.BaseValue = 100;
            rite.Cast(zone, 1, 0);

            Assert.LessOrEqual(doomed.GetStatValue("Hitpoints"), 0, "the rite killed it");
            Assert.Greater(hp.BaseValue, 100,
                "marks and ink were spent, so the ledger must still post");
        }

        [Test]
        public void SingleTargetRite_FilesItsDiagUnderTheVictim()
        {
            // "Which rites were cast at this creature?" must be
            // answerable by diag_query target=<id>. The two older
            // single-target rites already did this; the three new ones
            // filed everything under the caster.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_HollowCoin>(zone, 5, 5, out var caster);
            var victim = Creature(zone, "victim", 6, 5);
            Diag.ResetAll();

            rite.Cast(zone, 1, 0);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "RiteCast", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1);
            Assert.AreEqual(victim.ID, recs[0].TargetId);
        }

        [Test]
        public void RadiusRite_StillFilesItsDiagUnderTheCaster()
        {
            // Counter-check: a radius cast has no single victim, so the
            // caster remains the right filing. Without this, "always use
            // targets[0]" would pass the test above.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_SunderingWord>(zone, 10, 10, out var caster);
            Creature(zone, "victim", 11, 10);
            Diag.ResetAll();

            rite.Cast(zone, 0, 0);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "RiteCast", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1);
            Assert.AreEqual(caster.ID, recs[0].TargetId);
        }

        [Test]
        public void RiteGrimoires_AreNotScribeCopies()
        {
            // The six inherited the GrimoireCopy blueprint and with it
            // the GrimoireCopy TAG, which marks "a disposable copy the
            // scribe made". The baker's dialogue destroys the first item
            // carrying that tag, so finishing the oven quest while
            // holding a rite book ate the book — and CopyGrimoire
            // refuses to copy anything carrying it, so the scribe
            // offered a service it then declined to perform.
            foreach (var bp in RiteGrimoires)
            {
                var book = _factory.CreateEntity(bp);
                Assert.IsNotNull(book, bp);
                Assert.IsTrue(book.HasTag("Grimoire"), bp + " is still a grimoire");
                Assert.IsFalse(book.HasTag("GrimoireCopy"),
                    bp + " must not be marked as a scribe copy — the baker eats those");
            }
        }

        [Test]
        public void TheScribesOwnCopy_IsStillMarkedAsACopy()
        {
            // Counter-check: stripping the tag everywhere would let the
            // player copy a copy forever and break the baker quest.
            var copy = _factory.CreateEntity("GrimoireCopy");
            Assert.IsNotNull(copy);
            Assert.IsTrue(copy.HasTag("GrimoireCopy"),
                "the scribe's product is the thing that tag is for");
        }

        [Test]
        public void EveryRiteAppearsInTheGrimoirePicker()
        {
            // GrimoireTooltipData's own docstring: "a grimoire-taught
            // power missing from this table is INVISIBLE in the
            // picker". All eleven rites were missing once. This turns
            // that "must" into something enforced for the next one too.
            // Migration: the picker keys on the SKILL class names now.
            foreach (var t in typeof(CavesOfOoo.Skills.BaseSkillPart).Assembly.GetTypes())
            {
                if (t.IsAbstract
                    || !typeof(CavesOfOoo.Skills.ConsumingRiteSkillBase).IsAssignableFrom(t))
                    continue;
                Assert.IsTrue(GrimoireTooltipData.IsGrimoireMutation(t.Name),
                    t.Name + " has no GrimoireTooltipData row, so it cannot be bound"
                    + " from the grimoire picker");
            }
        }

        // ── Payoff pins (cold-eye Q3: these mutations survived) ──

        [Test]
        public void ShatteredRime_ShattersArmour_AndOnlyWhenFed()
        {
            // Mutation "delete ShatteredRime.ApplyPayoff body" survived
            // every original test — the rite's entire non-damage payoff
            // was unasserted.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_ShatteredRime>(zone, 10, 10, out var caster);
            var fed = Creature(zone, "fed", 11, 10);
            fed.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 1, 0);
            Assert.IsTrue(fed.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>());

            var coldZone = new Zone();
            var coldRite = Caster<CavesOfOoo.Skills.Rites_ShatteredRime>(coldZone, 10, 10, out _);
            var cold = Creature(coldZone, "cold", 11, 10);
            coldRite.Cast(coldZone, 1, 0);
            Assert.IsFalse(cold.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>(),
                "nothing spent, nothing shattered — the rider guard must hold");
        }

        [Test]
        public void VerdigrisBloom_ReSeedsAcid_AndOnlyWhenFed()
        {
            // The shipped answer to §7.4's "spread", never asserted.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_VerdigrisBloom>(zone, 10, 10, out var caster);
            var fed = Creature(zone, "fed", 11, 10);
            fed.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 0, 0);
            Assert.IsTrue(fed.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "the bloom re-seeds acid on what it caught");

            var coldZone = new Zone();
            var coldRite = Caster<CavesOfOoo.Skills.Rites_VerdigrisBloom>(coldZone, 10, 10, out _);
            var cold = Creature(coldZone, "cold", 11, 10);
            coldRite.Cast(coldZone, 0, 0);
            Assert.IsFalse(cold.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>());
        }

        [Test]
        public void SunderingWord_RiderGuard_HoldsOnAColdCast()
        {
            // "Cast cold is nearly worthless" was pinned for DAMAGE
            // only; the rider guards all survived deletion.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_SunderingWord>(zone, 10, 10, out _);
            var cold = Creature(zone, "cold", 11, 10);

            rite.Cast(zone, 0, 0);

            Assert.IsFalse(cold.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>());
            Assert.IsFalse(cold.GetPart<StatusEffectsPart>().HasEffect<WeakenedEffect>());
        }

        [Test]
        public void StillHeart_ColdCast_PutsNobodyToSleep()
        {
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_StillHeart>(zone, 5, 5, out _);
            var cold = Creature(zone, "cold", 6, 5);

            rite.Cast(zone, 1, 0);

            Assert.IsFalse(cold.GetPart<StatusEffectsPart>().HasEffect<AsleepByGasEffect>());
        }

        [Test]
        public void HollowCoin_SpendsThreeStatuses_NotTwo()
        {
            // Slots => 3 and the ">= 3" Broken branch were unreachable
            // in every original test: nothing ever primed three.
            var zone = new Zone();
            var rite = Caster<CavesOfOoo.Skills.Rites_HollowCoin>(zone, 5, 5, out var caster);
            var target = Creature(zone, "target", 6, 5);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);
            target.ApplyEffect(new FrozenEffect(), caster, zone);
            target.ApplyEffect(new BurningEffect(), caster, zone);

            rite.Cast(zone, 1, 0);

            var fx = target.GetPart<StatusEffectsPart>();
            Assert.IsFalse(fx.HasEffect<WetEffect>(), "all three spent");
            Assert.IsFalse(fx.HasEffect<FrozenEffect>());
            Assert.IsFalse(fx.HasEffect<BurningEffect>());
            Assert.IsTrue(fx.HasEffect<BrokenEffect>(),
                "three marks is the coin's Broken threshold");
        }

        [Test]
        public void BloodletterLedger_OpensBleeding_ScaledByMarks()
        {
            // The entire Bleeding payoff was untested.
            int SaveTargetWith(params Effect[] primed)
            {
                var zone = new Zone();
                var rite = Caster<CavesOfOoo.Skills.Rites_BloodletterLedger>(zone, 5, 5, out var caster);
                var target = Creature(zone, "target", 6, 5);
                foreach (var e in primed) target.ApplyEffect(e, caster, zone);
                rite.Cast(zone, 1, 0);
                return target.GetPart<StatusEffectsPart>()
                    .GetEffect<BleedingEffect>()?.SaveTarget ?? 0;
            }

            int one = SaveTargetWith(new WetEffect(1.0f));
            int two = SaveTargetWith(new WetEffect(1.0f), new FrozenEffect());

            Assert.Greater(one, 0, "one mark opens a bleed");
            Assert.Greater(two, one, "two marks bleed harder to staunch");
        }

        // ── Reachability: found in the world, not bought ────────

        [Test]
        public void EveryRiteGrimoire_SpawnsInkedAndTeachesARealRite()
        {
            foreach (var bp in RiteGrimoires)
            {
                var book = _factory.CreateEntity(bp);
                Assert.IsNotNull(book, bp + " must exist");
                Assert.Greater(book.GetPart<GrimoireChargePart>()?.Charges ?? 0, 0,
                    bp + " must spawn inked");

                var grim = book.GetPart<GrimoirePart>();
                Assert.IsNotNull(grim, bp + " must be readable");
                // Migration: rite grimoires teach SKILLS. Resolve the
                // way SkillsPart.AddSkill does — scan the assembly that
                // owns BaseSkillPart. A bare Type.GetType() from the
                // test assembly cannot see game types and returns null
                // for every name, correct or not.
                System.Type type = null;
                foreach (var t in typeof(CavesOfOoo.Skills.BaseSkillPart).Assembly.GetTypes())
                    if (!t.IsAbstract
                        && typeof(CavesOfOoo.Skills.BaseSkillPart).IsAssignableFrom(t)
                        && t.Name == grim.SkillClassName)
                    { type = t; break; }

                Assert.IsNotNull(type,
                    bp + " names " + grim.SkillClassName
                    + ", which the game could not instantiate");
            }
        }

        /// <summary>
        /// Containers the player breaks open out in the world — not
        /// shops. Every one of these is a chest, vault, shelf, urn or
        /// crate that already exists in the loot content.
        /// </summary>
        private static readonly string[] WorldContainers =
        {
            "BookshelfT1", "BookshelfT2", "BookshelfT3", "LibraryShelfT1",
            "ReliquaryT1", "ReliquaryT2", "ReliquaryT3",
            "StrongBoxT2", "StrongBoxT3", "SealedVaultT3", "TombVaultT2",
            "CultCacheT2", "ZigguratVaultT2", "WorkshopCacheT2", "BanditCacheT2",
            "UrnT2", "UrnT3", "CrateT2", "CrateT3", "AlchemyShelfT2",
            "LairLoot", "DeepSupplyT2",
        };

        private void LoadLoot()
        {
            LootTableRegistry.ResetForTests();
            var sources = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot"), "*.json"))
                sources.Add(File.ReadAllText(f));
            LootTableRegistry.InitializeFromJsonSources(sources);
        }

        private static int SourcesFor(string blueprint)
        {
            int n = 0;
            foreach (var name in WorldContainers)
            {
                var table = LootTableRegistry.Get(name);
                if (table?.Entries == null) continue;
                foreach (var e in table.Entries)
                    if (e.Blueprint == blueprint) { n++; break; }
            }
            return n;
        }

        [Test]
        public void EveryRiteGrimoire_HasAtLeastTwoWorldContainerSources()
        {
            // Found in the world, per the request — and not hostage to a
            // single table. One unlucky container type must not make a
            // rite unobtainable. Asserted through the game's own loader,
            // so a table the loader rejects does not count as a source.
            LoadLoot();

            foreach (var bp in RiteGrimoires)
                Assert.GreaterOrEqual(SourcesFor(bp), 2,
                    bp + " has " + SourcesFor(bp)
                    + " container source(s) — one bad roll and it does not exist");
        }

        [Test]
        public void ContainerNamesUsedByThoseAssertions_AreRealTables()
        {
            // Counter-check for the test above: if a name were
            // misspelled, Get() returns null, SourcesFor skips it, and
            // the coverage assertion could pass while pointing at
            // nothing. This is what stops that.
            LoadLoot();

            foreach (var name in WorldContainers)
                Assert.IsNotNull(LootTableRegistry.Get(name),
                    name + " is not a real loot table — the coverage test above is lying");
        }

        [Test]
        public void RiteEntriesInPickModeTables_CarryAnExplicitWeight()
        {
            // Regression pin. A pick-mode table IGNORES Chance and
            // selects on Weight, which defaults to 1. Six of the twenty
            // container tables are pick-mode, so entries added with only
            // a Chance landed at weight 1 among a handful of weight-1
            // staples — a T1 bookshelf was yielding a top-end rite one
            // time in five. Silent, and invisible to every other test
            // here, because the blueprint really was "in the table".
            LoadLoot();

            foreach (var name in WorldContainers)
            {
                var table = LootTableRegistry.Get(name);
                if (table == null || !table.PickOne) continue;

                foreach (var e in table.Entries)
                {
                    if (System.Array.IndexOf(RiteGrimoires, e.Blueprint) < 0) continue;

                    int others = 0;
                    foreach (var o in table.Entries)
                        if (System.Array.IndexOf(RiteGrimoires, o.Blueprint) < 0)
                            others += o.Weight;

                    Assert.Less(e.Weight * 10, others,
                        name + "/" + e.Blueprint + " has weight " + e.Weight
                        + " against " + others + " of everything else — a powerful"
                        + " rite must be a jackpot, not a staple");
                }
            }
        }

        [Test]
        public void RiteGrimoires_AreScatteredWidely_NotConcentratedInOnePlace()
        {
            // "randomly throughout the map in chests, containers,
            // barrels" — the shape of the request is BREADTH.
            LoadLoot();

            int containersCarryingARite = 0;
            foreach (var name in WorldContainers)
            {
                var table = LootTableRegistry.Get(name);
                if (table?.Entries == null) continue;
                foreach (var e in table.Entries)
                    if (System.Array.IndexOf(RiteGrimoires, e.Blueprint) >= 0)
                    { containersCarryingARite++; break; }
            }

            Assert.GreaterOrEqual(containersCarryingARite, 15,
                "only " + containersCarryingARite + " container types carry a rite");
        }
    }
}
