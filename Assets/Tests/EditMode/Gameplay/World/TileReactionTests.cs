using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// PALIMPSEST P3 — two things written on a tile produce a third.
    ///
    /// <para>The POC question: can a handful of dumb rules produce
    /// situations players set up on purpose? These pin that the rules
    /// fire, that they are entirely data-driven (no ability contains
    /// combo logic), and — most importantly — that a chain reaction
    /// cannot run away.</para>
    /// </summary>
    public class TileReactionTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }

        [TearDown]
        public void TearDown() => TileReactionSystem.ResetForTests();

        private static Entity Creature(Zone zone, string name, int x, int y, int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["ElectricResistance"] = new Stat { Owner = e, Name = "ElectricResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static string Reasons(string kind)
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "tile", Kind = kind, Limit = 30 }).Records;
            var sb = new System.Text.StringBuilder();
            foreach (var r in recs) sb.Append(r.PayloadJson).Append('|');
            return sb.ToString();
        }

        // ── The table loads ──────────────────────────────────────

        [Test]
        public void TheShippedReactionTable_Loads()
        {
            Assert.IsTrue(TileReactionSystem.IsInitialized);
            Assert.GreaterOrEqual(TileReactionSystem.Count, 5);
        }

        // ── The canonical reaction ───────────────────────────────

        [Test]
        public void WaterPlusCharge_ElectrifiesWhoeverIsStandingInIt()
        {
            var zone = new Zone();
            var victim = Creature(zone, "victim", 5, 5);
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);

            int hp = victim.GetStatValue("Hitpoints");
            TileReactionSystem.ResolveZone(zone);

            Assert.Less(victim.GetStatValue("Hitpoints"), hp, "shocked");
            Assert.IsTrue(victim.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>());
        }

        [Test]
        public void ElectrifiedWater_SurvivesTheReaction()
        {
            // The puddle is TERRAIN, not a one-shot. It has to still be
            // there to be charged again — that is what makes preparing a
            // tile worth doing.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);

            TileReactionSystem.ResolveZone(zone);

            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "water"),
                "water is not consumed by being charged");
            Assert.AreEqual(0, zone.TileState.Charge(5, 5), "but the charge is spent");
        }

        [Test]
        public void WaterAlone_DoesNothing()
        {
            // Counter-check: a reaction needs BOTH inputs. If water alone
            // fired, every puddle in the world would be shocking people.
            var zone = new Zone();
            var bystander = Creature(zone, "bystander", 5, 5);
            zone.TileState.WriteCoating(5, 5, "water", 6);

            int hp = bystander.GetStatValue("Hitpoints");
            TileReactionSystem.ResolveZone(zone);

            Assert.AreEqual(hp, bystander.GetStatValue("Hitpoints"));
            Assert.IsFalse(bystander.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>());
        }

        // ── The rest of the table ────────────────────────────────

        [Test]
        public void WaterPlusHeat_BecomesSteam_AndTheWaterIsGone()
        {
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddHeat(5, 5, 1);

            TileReactionSystem.ResolveZone(zone);

            Assert.IsFalse(zone.TileState.HasCoating(5, 5, "water"), "boiled off");
            Assert.AreEqual("steam", zone.TileState.Cloud(5, 5));
        }

        [Test]
        public void WaterPlusCold_FreezesIntoIce()
        {
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddCold(5, 5, 1);

            TileReactionSystem.ResolveZone(zone);

            Assert.IsFalse(zone.TileState.HasCoating(5, 5, "water"));
            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "ice"));
        }

        [Test]
        public void IcePlusHeat_MeltsBackToWater()
        {
            // Reversibility is what makes experimenting safe.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "ice", 4);
            zone.TileState.AddHeat(5, 5, 1);

            TileReactionSystem.ResolveZone(zone);

            Assert.IsFalse(zone.TileState.HasCoating(5, 5, "ice"));
            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "water"));
        }

        [Test]
        public void OilPlusHeat_IgnitesHard()
        {
            var zone = new Zone();
            var victim = Creature(zone, "victim", 5, 5);
            zone.TileState.WriteCoating(5, 5, "oil", 8);
            zone.TileState.AddHeat(5, 5, 1);

            int hp = victim.GetStatValue("Hitpoints");
            TileReactionSystem.ResolveZone(zone);

            Assert.Less(victim.GetStatValue("Hitpoints"), hp - 4,
                "burning oil is the spectacular one");
            Assert.IsFalse(zone.TileState.HasCoating(5, 5, "oil"), "the fuel is spent");
            Assert.IsTrue(zone.TileState.HasResidue(5, 5, "embers"));
            Assert.AreEqual("smoke", zone.TileState.Cloud(5, 5));
        }

        [Test]
        public void OilPlusEmbers_IgnitesToo_WithoutNeedingHeat()
        {
            // Embers are a heat SOURCE, which is what makes Ember Spit's
            // residue tactically meaningful two turns later.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "oil", 8);
            zone.TileState.WriteResidue(5, 5, "embers", 4);

            TileReactionSystem.ResolveZone(zone);

            Assert.IsFalse(zone.TileState.HasCoating(5, 5, "oil"));
        }

        // ── The cross-ability payoff ─────────────────────────────

        [Test]
        public void OilThenEmbers_FromTwoSeparateAbilities_Ignites()
        {
            // THE POINT OF THE WHOLE PHASE. Oilmark knows nothing about
            // fire; Ember Spit knows nothing about oil. Neither contains
            // combo logic. The DATA decides what their combination means.
            var zone = new Zone();
            ZoneTileStateSystem.WriteCoating(zone, 5, 5, "oil", 8, null, "Oilmark");
            ZoneTileStateSystem.WriteResidue(zone, 5, 5, "embers", 4, null, "EmberSpit");

            int fired = TileReactionSystem.ResolveZone(zone);

            Assert.Greater(fired, 0, "two unrelated abilities produced a third thing");
            Assert.IsFalse(zone.TileState.HasCoating(5, 5, "oil"));
        }

        // ── Energy cancellation runs first ───────────────────────

        [Test]
        public void OpposedEnergyCancels_BeforeAnyReactionReadsIt()
        {
            // Design §12. Heat 1 + Cold 1 leaves neither, so water on
            // that tile neither boils nor freezes — the tile is simply
            // not hot and not cold.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddHeat(5, 5, 1);
            zone.TileState.AddCold(5, 5, 1);

            TileReactionSystem.ResolveZone(zone);

            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "water"),
                "cancelled energy reacts with nothing");
            Assert.IsNull(zone.TileState.Get(5, 5)?.Cloud is string c && c == "steam" ? "steam" : null);
        }

        // ── Loop guards: the CPU must not join in ────────────────

        [Test]
        public void TheSameReaction_CannotFireTwiceOnATileInOneAction()
        {
            // Without this, freeze and melt would oscillate forever on a
            // tile that has both water and heat available.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddCold(5, 5, 2);
            zone.TileState.AddHeat(5, 5, 2);
            Diag.ResetAll();

            Assert.DoesNotThrow(() => TileReactionSystem.ResolveZone(zone));
        }

        [Test]
        public void EnergyCancellation_MakesTheFreezeMeltLoopImpossible()
        {
            // The obvious adversarial case is ice+heat -> water,
            // water+cold -> ice, oscillating forever. It turns out
            // energy cancellation (design §12) already forecloses it:
            // a tile CANNOT hold both heat and cold, so the two halves
            // of the loop can never both be available.
            //
            // This test originally asserted the reaction CAP caught the
            // loop. It passed — but for the wrong reason, because
            // cancellation had already zeroed both channels and nothing
            // fired at all. Cancellation is a loop guard in its own
            // right, and that is worth stating rather than leaving as an
            // accident.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddCold(5, 5, 2);
            zone.TileState.AddHeat(5, 5, 2);

            int fired = 0;
            Assert.DoesNotThrow(() => fired = TileReactionSystem.ResolveZone(zone));

            Assert.AreEqual(0, zone.TileState.Heat(5, 5), "heat cancelled");
            Assert.AreEqual(0, zone.TileState.Cold(5, 5), "cold cancelled");
            Assert.AreEqual(0, fired,
                "with neither channel left there is nothing to react with");
            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "water"),
                "and the water is untouched");
        }

        [Test]
        public void ARunawayChain_IsCappedAndSaysSo()
        {
            // A silently-stopped chain is indistinguishable from one that
            // never started, so the cap has to announce itself.
            // Enough genuinely-reacting tiles to exceed
            // MaxReactionsPerAction. An earlier draft used heat+cold on
            // each tile and hit the cap never, because cancellation
            // zeroed both channels first — the test passed while
            // exercising nothing.
            var zone = new Zone();
            int tiles = TileReactionSystem.MaxReactionsPerAction + 8;
            for (int x = 0; x < tiles; x++)
            {
                zone.TileState.WriteCoating(x, 5, "water", 6);
                zone.TileState.AddCharge(x, 5, 1);
            }
            Diag.ResetAll();

            TileReactionSystem.ResolveZone(zone);

            StringAssert.Contains("cap", Reasons("ReactionSuppressed"),
                "hitting a guard is reported, not swallowed");
        }

        // ── Observability ────────────────────────────────────────

        [Test]
        public void EveryReaction_EmitsAFiredRecord()
        {
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);
            Diag.ResetAll();

            TileReactionSystem.ResolveZone(zone);

            StringAssert.Contains("electrify_water", Reasons("ReactionFired"));
        }

        // ── Guards ───────────────────────────────────────────────

        [Test]
        public void NullZoneAndUninitialisedTable_AreGraceful()
        {
            Assert.DoesNotThrow(() => TileReactionSystem.ResolveZone(null));

            TileReactionSystem.ResetForTests();
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            Assert.AreEqual(0, TileReactionSystem.ResolveZone(zone),
                "no table means no reactions, not a crash");
        }

        [Test]
        public void ABlankZone_ResolvesNothing()
        {
            Assert.AreEqual(0, TileReactionSystem.ResolveZone(new Zone()));
        }

        [Test]
        public void ADeadOccupant_IsNotAlsoAfflicted()
        {
            // Damage can kill mid-reaction; a corpse must not then be
            // handed an Electrified effect.
            var zone = new Zone();
            var frail = Creature(zone, "frail", 5, 5, hp: 1);
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);

            TileReactionSystem.ResolveZone(zone);

            Assert.LessOrEqual(frail.GetStatValue("Hitpoints"), 0, "precondition: lethal");
            Assert.IsFalse(frail.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "the dead are not electrified");
        }
    }
}
