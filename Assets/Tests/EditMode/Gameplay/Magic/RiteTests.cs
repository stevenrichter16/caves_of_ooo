using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SPELLCRAFT SM8 — the rites, now that resonance exists to feed them.
    ///
    /// <list type="bullet">
    /// <item><b>Hanging Bolt</b> — reads the SAME Electric table as Storm
    /// Anvil but converts marks into no-save Paralysis instead of damage.
    /// Two rites, one table, opposite purposes.</item>
    /// <item><b>Rendered Steam</b> — the only rite that wants two OPPOSED
    /// statuses on one body.</item>
    /// <item><b>Scalding Veil</b> — the only rite that spends a status on
    /// the CASTER.</item>
    /// </list>
    /// </summary>
    public class RiteTests
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

        private static Entity Creature(string name, int hp = 500)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["ElectricResistance"] = new Stat { Owner = e, Name = "ElectricResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new MutationsPart());
            return e;
        }

        /// <summary>A caster carrying an inked grimoire — the thing a
        /// rite actually spends.</summary>
        private Entity Caster(string name = "caster", int charges = 10)
        {
            var e = Creature(name);
            var inv = new InventoryPart { MaxWeight = 500 };
            e.AddPart(inv);
            var book = new Entity { ID = name + "-book", BlueprintName = "StormAnvilGrimoire" };
            book.AddPart(new RenderPart { DisplayName = "a rite" });
            book.AddPart(new GrimoireChargePart { Charges = charges, MaxCharges = 10 });
            inv.AddObject(book);
            return e;
        }

        private static GrimoireChargePart BookOf(Entity caster)
            => caster.GetPart<InventoryPart>().Objects[0].GetPart<GrimoireChargePart>();

        private static T Learn<T>(Entity caster) where T : BaseMutation, new()
        {
            var m = new T();
            caster.AddPart(m);
            m.Mutate(caster, 1);
            return m;
        }

        private static string Reasons()
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "RiteRejected", Limit = 20 }).Records;
            var sb = new System.Text.StringBuilder();
            foreach (var r in recs) sb.Append(r.PayloadJson).Append('|');
            return sb.ToString();
        }

        // ════════════════════════════════════════════════════════
        // Ink — the shared cost
        // ════════════════════════════════════════════════════════

        [Test]
        public void ARite_SpendsAnInkCharge()
        {
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(Creature("victim"), 11, 10);
            int before = BookOf(caster).Charges;

            Learn<StormAnvilMutation>(caster).Cast(zone, zone.GetEntityCell(caster));

            Assert.AreEqual(before - 1, BookOf(caster).Charges);
        }

        [Test]
        public void ARite_WithNoInk_RefusesAndSaysWhy()
        {
            var caster = Caster(charges: 0);
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            var victim = Creature("victim");
            zone.AddEntity(victim, 11, 10);
            int hp = victim.GetStatValue("Hitpoints");
            Diag.ResetAll();

            Learn<StormAnvilMutation>(caster).Cast(zone, zone.GetEntityCell(caster));

            Assert.AreEqual(hp, victim.GetStatValue("Hitpoints"), "a dry book casts nothing");
            StringAssert.Contains("no_ink", Reasons());
        }

        [Test]
        public void ARite_WithNoTarget_DoesNotWasteACharge()
        {
            // The most infuriating possible bug in a costed spell:
            // burning a charge on empty air.
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            int before = BookOf(caster).Charges;
            Diag.ResetAll();

            Learn<StormAnvilMutation>(caster).Cast(zone, zone.GetEntityCell(caster));

            Assert.AreEqual(before, BookOf(caster).Charges,
                "no target means no charge spent");
            StringAssert.Contains("no_target", Reasons());
        }

        [Test]
        public void Refill_ClampsAtMax_AndReportsWhatItAdded()
        {
            var book = new GrimoireChargePart { Charges = 8, MaxCharges = 10 };
            Assert.AreEqual(2, book.Refill(5), "only the two that fit");
            Assert.AreEqual(10, book.Charges);
            Assert.AreEqual(0, book.Refill(5),
                "a full book reports zero so a caller can decline to waste a vial");
        }

        // ════════════════════════════════════════════════════════
        // Hanging Bolt — same table, different purpose
        // ════════════════════════════════════════════════════════

        [Test]
        public void HangingBolt_ConvertsMarksIntoParalysis_NotDamage()
        {
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 5, 5);
            var victim = Creature("victim");
            zone.AddEntity(victim, 7, 5);
            victim.ApplyEffect(new WetEffect(1.0f), null, zone);

            Learn<HangingBoltMutation>(caster).Cast(zone, 1, 0);

            var par = victim.GetPart<StatusEffectsPart>().GetEffect<ParalyzedEffect>();
            Assert.IsNotNull(par, "a mark spent becomes paralysis");
            Assert.GreaterOrEqual(par.Duration, HangingBoltMutation.PARALYSIS_PER_MARK);
        }

        [Test]
        public void HangingBolt_CastCold_ParalysesNothing()
        {
            // Counter-check, and the same discipline Storm Anvil follows:
            // with nothing primed, a rite is nearly worthless.
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 5, 5);
            var victim = Creature("victim");
            zone.AddEntity(victim, 7, 5);

            Learn<HangingBoltMutation>(caster).Cast(zone, 1, 0);

            Assert.IsFalse(victim.GetPart<StatusEffectsPart>().HasEffect<ParalyzedEffect>(),
                "no marks, no paralysis");
        }

        [Test]
        public void HangingBolt_MoreMarks_MeansLongerParalysis()
        {
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 5, 5);
            var one = Creature("one");
            var two = Creature("two");
            zone.AddEntity(one, 7, 5);
            zone.AddEntity(two, 5, 7);
            one.ApplyEffect(new WetEffect(1.0f), null, zone);
            two.ApplyEffect(new WetEffect(1.0f), null, zone);
            two.ApplyEffect(new FrozenEffect(0.5f), null, zone);

            var bolt = Learn<HangingBoltMutation>(caster);
            bolt.Cast(zone, 1, 0);
            bolt.Cast(zone, 0, 1);

            int d1 = one.GetPart<StatusEffectsPart>().GetEffect<ParalyzedEffect>().Duration;
            int d2 = two.GetPart<StatusEffectsPart>().GetEffect<ParalyzedEffect>().Duration;
            Assert.Greater(d2, d1, "two marks hold them twice as long");
        }

        // ════════════════════════════════════════════════════════
        // Rendered Steam — two opposed statuses
        // ════════════════════════════════════════════════════════

        [Test]
        public void RenderedSteam_OnAWetAndBurningTarget_Detonates()
        {
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            var victim = Creature("victim");
            zone.AddEntity(victim, 11, 10);
            victim.ApplyEffect(new WetEffect(1.0f), null, zone);
            victim.ApplyEffect(new BurningEffect(1.0f, null, new Random(0)), null, zone);

            Learn<RenderedSteamMutation>(caster).Cast(zone, zone.GetEntityCell(caster));

            Assert.IsTrue(victim.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>(),
                "a real steam burst blinds");
        }

        [Test]
        public void RenderedSteam_WithOnlyOneOfThePair_DoesNotBlind()
        {
            // Counter-check that makes the PAIR mean something: either
            // status alone still resonates and still hurts, but the
            // crowd control is what you paid the pair for.
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            var wetOnly = Creature("wetOnly");
            zone.AddEntity(wetOnly, 11, 10);
            wetOnly.ApplyEffect(new WetEffect(1.0f), null, zone);
            int hp = wetOnly.GetStatValue("Hitpoints");

            Learn<RenderedSteamMutation>(caster).Cast(zone, zone.GetEntityCell(caster));

            Assert.Less(wetOnly.GetStatValue("Hitpoints"), hp, "it still hurts");
            Assert.IsFalse(wetOnly.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>(),
                "but one half of the pair does not render steam");
        }

        [Test]
        public void RenderedSteam_ThePairHitsHarderThanEitherAlone()
        {
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            var pair = Creature("pair");
            var single = Creature("single");
            zone.AddEntity(pair, 11, 10);
            zone.AddEntity(single, 9, 10);
            pair.ApplyEffect(new WetEffect(1.0f), null, zone);
            pair.ApplyEffect(new BurningEffect(1.0f, null, new Random(0)), null, zone);
            single.ApplyEffect(new WetEffect(1.0f), null, zone);

            int pairHp = pair.GetStatValue("Hitpoints");
            int singleHp = single.GetStatValue("Hitpoints");
            Learn<RenderedSteamMutation>(caster).Cast(zone, zone.GetEntityCell(caster));

            int pairLost = pairHp - pair.GetStatValue("Hitpoints");
            int singleLost = singleHp - single.GetStatValue("Hitpoints");
            Assert.Greater(pairLost, singleLost,
                "arranging two opposed statuses is the whole cost, and must pay");
        }

        // ════════════════════════════════════════════════════════
        // Scalding Veil — the rite that reads YOU
        // ════════════════════════════════════════════════════════

        [Test]
        public void ScaldingVeil_ConsumesTheCastersOwnWater()
        {
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            caster.ApplyEffect(new WetEffect(1.0f), null, zone);

            Learn<ScaldingVeilMutation>(caster).Cast(zone);

            Assert.IsFalse(caster.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "your own soaking is what it spends");
            Assert.IsTrue(caster.GetPart<StatusEffectsPart>().HasEffect<ScaldingVeilEffect>());
        }

        [Test]
        public void ScaldingVeil_WhenDry_RefusesWithoutSpendingInk()
        {
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            int before = BookOf(caster).Charges;
            Diag.ResetAll();

            Learn<ScaldingVeilMutation>(caster).Cast(zone);

            Assert.AreEqual(before, BookOf(caster).Charges,
                "a rite that can do nothing must not take your ink");
            Assert.IsFalse(caster.GetPart<StatusEffectsPart>().HasEffect<ScaldingVeilEffect>());
            StringAssert.Contains("caster_not_wet", Reasons());
        }

        [Test]
        public void ScaldingVeil_ScaldsWhoeverStrikesYou()
        {
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            var attacker = Creature("attacker");
            zone.AddEntity(attacker, 11, 10);
            caster.ApplyEffect(new WetEffect(1.0f), null, zone);
            Learn<ScaldingVeilMutation>(caster).Cast(zone);

            int attackerHp = attacker.GetStatValue("Hitpoints");
            var blow = new Damage(5);
            CombatSystem.ApplyDamage(caster, blow, attacker, zone);

            Assert.Less(attacker.GetStatValue("Hitpoints"), attackerHp,
                "the steam answers the blow");
            Assert.IsTrue(attacker.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>(),
                "and blinds them");
        }

        [Test]
        public void ScaldingVeil_DoesNotScaldOnSourcelessDamage()
        {
            // Counter-check: poison ticks, burning and environmental
            // damage have no attacker. Retaliating against nobody would
            // either crash or hit the caster.
            var caster = Caster();
            var zone = new Zone();
            zone.AddEntity(caster, 10, 10);
            caster.ApplyEffect(new WetEffect(1.0f), null, zone);
            Learn<ScaldingVeilMutation>(caster).Cast(zone);

            int hp = caster.GetStatValue("Hitpoints");
            Assert.DoesNotThrow(() =>
                CombatSystem.ApplyDamage(caster, new Damage(3), null, zone));
            Assert.Less(caster.GetStatValue("Hitpoints"), hp,
                "the damage still lands");
        }

        // ════════════════════════════════════════════════════════
        // Content reachability
        // ════════════════════════════════════════════════════════

        [Test]
        public void EveryRiteGrimoire_SpawnsInkedAndTeachesARealMutation()
        {
            // The trap this project keeps hitting: a rite nobody can
            // obtain does not exist.
            foreach (var bp in new[] { "StormAnvilGrimoire", "HangingBoltGrimoire",
                                       "RenderedSteamGrimoire", "ScaldingVeilGrimoire" })
            {
                var book = _factory.CreateEntity(bp);
                Assert.IsNotNull(book, bp + " must exist as a blueprint");

                var charge = book.GetPart<GrimoireChargePart>();
                Assert.IsNotNull(charge, bp + " must carry ink");
                Assert.Greater(charge.Charges, 0, bp + " must spawn inked");

                var grim = book.GetPart<GrimoirePart>();
                Assert.IsNotNull(grim, bp + " must be readable");
                // Migration: rite grimoires teach SKILLS now.
                Assert.IsFalse(string.IsNullOrEmpty(grim.SkillClassName));

                System.Type type = null;
                foreach (var t in typeof(CavesOfOoo.Skills.BaseSkillPart).Assembly.GetTypes())
                    if (!t.IsAbstract
                        && typeof(CavesOfOoo.Skills.BaseSkillPart).IsAssignableFrom(t)
                        && t.Name == grim.SkillClassName)
                    { type = t; break; }
                Assert.IsNotNull(type,
                    bp + " names " + grim.SkillClassName + ", which must resolve");
            }
        }

        [Test]
        public void RiteGrimoires_AreStockedSomewhere()
        {
            var json = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json"));
            foreach (var bp in new[] { "StormAnvilGrimoire", "HangingBoltGrimoire",
                                       "RenderedSteamGrimoire", "ScaldingVeilGrimoire" })
                StringAssert.Contains(bp, json,
                    bp + " is unobtainable — no shop or container stocks it");
        }
    }
}
