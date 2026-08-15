using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Hypothesis-driven deep audit of the rites (CLAUDE.md §Hypothesis-
    /// driven deep audit). Written AFTER the cold-eye pass, by asking
    /// what the PLAYER does that no existing test simulates — not by
    /// re-reading the code a fourth time.
    ///
    /// <para>The grimoire is a physical object the player carries,
    /// stacks, drops, sells and runs dry. Every existing test builds one
    /// caster with exactly one full book in a fresh Zone and casts once.
    /// That is a narrow slice of a long game.</para>
    ///
    /// <para>Each test states its hypothesis and is classified honestly
    /// in the commit body as CONFIRMED BUG or PINNED-AS-CORRECT. A
    /// pinned-correct test is not a wasted test — it is permanent
    /// regression infrastructure for a contract nothing else asserts.</para>
    /// </summary>
    public class ConsumingRiteHypothesisTests
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

        private T Rite<T>(Zone zone, int x, int y, out Entity caster, string bookBlueprint)
            where T : CavesOfOoo.Skills.ConsumingRiteSkillBase, new()
        {
            caster = Creature(zone, "caster", x, y);
            caster.AddPart(new ActivatedAbilitiesPart());
            caster.AddPart(new CavesOfOoo.Skills.SkillsPart());
            var inv = new InventoryPart { MaxWeight = 500 };
            caster.AddPart(inv);
            inv.AddObject(_factory.CreateEntity(bookBlueprint));

            var rite = new T();
            caster.GetPart<CavesOfOoo.Skills.SkillsPart>().AddSkill(rite);
            return rite;
        }

        // ════════════════════════════════════════════════════════
        // H1 — "What if my follower is standing next to me?"
        // ════════════════════════════════════════════════════════

        [Test]
        public void Hypothesis_RadiusRite_CaughtFollowerDoesNotBecomeAPermanentEnemy()
        {
            // A follower walks adjacent to the player by design. Two of
            // the six new rites are self-centred radius casts. If a
            // caught follower flips into the player's PersonalEnemies —
            // which FactionManager ranks ABOVE party alignment — then one
            // AoE permanently turns your companion hostile, with no
            // in-game way to notice why.
            var zone = new Zone();
            var rite = Rite<CavesOfOoo.Skills.Rites_SunderingWord>(zone, 10, 10, out var caster,
                "SunderingWordGrimoire");

            var follower = Creature(zone, "follower", 11, 10);
            var brain = new BrainPart();
            follower.AddPart(brain);

            rite.Cast(zone, 0, 0);

            Assert.IsFalse(brain.IsPersonallyHostileTo(caster),
                "an AoE that catches your own companion must not create a"
                + " permanent vendetta the player cannot see or undo");
        }

        // ════════════════════════════════════════════════════════
        // H2 — "What if I find a second copy of a book I already have?"
        // ════════════════════════════════════════════════════════

        [Test]
        public void Hypothesis_SecondCopyOfARiteBook_ItsInkIsNotLostOnPickup()
        {
            // Rite grimoires carry a Stacker part. If two books merge
            // into a stack of 2 and the merge keeps only one charge
            // counter, the fresh book's ten casts evaporate on pickup —
            // and the player's most likely reason for picking up a
            // duplicate is that their first one ran dry.
            var zone = new Zone();
            var rite = Rite<CavesOfOoo.Skills.Rites_HollowCoin>(zone, 5, 5, out var caster,
                "HollowCoinGrimoire");
            var inv = caster.GetPart<InventoryPart>();

            // Run the carried book dry.
            var first = inv.Objects[0].GetPart<GrimoireChargePart>();
            first.Charges = 0;

            int before = TotalInk(inv);
            inv.AddObject(_factory.CreateEntity("HollowCoinGrimoire"));
            int after = TotalInk(inv);

            Assert.AreEqual(before + 10, after,
                "picking up a fresh book must add its ten charges, not"
                + " merge them into a dry stack and lose them");
        }

        [Test]
        public void Hypothesis_SecondCopyOfARiteBook_MakesTheRiteCastableAgain()
        {
            // The player-visible version of the same question, which is
            // what actually matters: dry book + found book = can I cast?
            var zone = new Zone();
            var rite = Rite<CavesOfOoo.Skills.Rites_HollowCoin>(zone, 5, 5, out var caster,
                "HollowCoinGrimoire");
            var inv = caster.GetPart<InventoryPart>();
            inv.Objects[0].GetPart<GrimoireChargePart>().Charges = 0;
            Creature(zone, "target", 6, 5);

            Assert.IsFalse(rite.Cast(zone, 1, 0), "dry to start with");

            inv.AddObject(_factory.CreateEntity("HollowCoinGrimoire"));

            Assert.IsTrue(rite.Cast(zone, 1, 0),
                "a freshly-found copy must bring the rite back");
        }

        [Test]
        public void Hypothesis_SplittingAStackOfRiteBooks_DoesNotDuplicateInk()
        {
            // The inverse exploit. If charges live on the stack entity
            // and SplitStack clones it, splitting a stack of 2 could
            // hand the player two full books from one — free casts.
            var zone = new Zone();
            var rite = Rite<CavesOfOoo.Skills.Rites_HollowCoin>(zone, 5, 5, out var caster,
                "HollowCoinGrimoire");
            var inv = caster.GetPart<InventoryPart>();
            inv.AddObject(_factory.CreateEntity("HollowCoinGrimoire"));

            int before = TotalInk(inv);

            var stack = inv.Objects[0].GetPart<StackerPart>();
            if (stack == null || stack.StackCount < 2)
                Assert.Pass("these books do not stack; nothing to split");

            var split = stack.SplitStack(1);
            if (split != null) inv.AddObject(split);

            Assert.LessOrEqual(TotalInk(inv), before,
                "splitting a stack must never mint ink out of nothing");
        }

        private static int TotalInk(InventoryPart inv)
        {
            int n = 0;
            for (int i = 0; i < inv.Objects.Count; i++)
            {
                var c = inv.Objects[i].GetPart<GrimoireChargePart>();
                if (c == null) continue;
                var st = inv.Objects[i].GetPart<StackerPart>();
                n += c.Charges * (st != null && st.StackCount > 1 ? st.StackCount : 1);
            }
            return n;
        }

        // ════════════════════════════════════════════════════════
        // H3 — "My book is dry. Can I ever re-ink it?"
        // ════════════════════════════════════════════════════════

        [Test]
        public void Hypothesis_ADryGrimoireCanBeRefilled_ByTheApiThatExistsForIt()
        {
            // GrimoireChargePart ships Refill(int) and every rite
            // blueprint sets ChargesPerVial: 5. If nothing in the game
            // ever calls Refill, that config is decoration and every
            // rite book is a strictly finite ten casts — a very
            // different item from what the content implies.
            var book = _factory.CreateEntity("HollowCoinGrimoire");
            var charge = book.GetPart<GrimoireChargePart>();
            Assert.IsNotNull(charge);
            Assert.Greater(charge.ChargesPerVial, 0,
                "the blueprint advertises a per-vial refill amount");

            charge.Charges = 0;
            charge.Refill(charge.ChargesPerVial);

            Assert.AreEqual(charge.ChargesPerVial, charge.Charges,
                "the refill API itself works — whether anything CALLS it"
                + " is the separate question this test's name raises");
        }

        // ════════════════════════════════════════════════════════
        // H4 — "I carry two rite books. Which one pays?"
        // ════════════════════════════════════════════════════════

        [Test]
        public void Hypothesis_CastingOneRite_MayDrainAnotherRitesBook()
        {
            // GrimoireInk.FindInked returns the FIRST inked book in the
            // pack regardless of which rite is casting. Pinning the
            // actual behaviour either way: if ink is fungible that is a
            // deliberate design, and this test says so out loud; if it
            // is not, this is where it surfaces.
            var zone = new Zone();
            var rite = Rite<CavesOfOoo.Skills.Rites_HollowCoin>(zone, 5, 5, out var caster,
                "StormAnvilGrimoire");                       // WRONG book first
            var inv = caster.GetPart<InventoryPart>();
            inv.AddObject(_factory.CreateEntity("HollowCoinGrimoire"));
            Creature(zone, "target", 6, 5);

            rite.Cast(zone, 1, 0);

            int storm = inv.Objects[0].GetPart<GrimoireChargePart>().Charges;
            Assert.AreEqual(9, storm,
                "ink is fungible across grimoires — the first inked book"
                + " pays, whichever rite is being cast. Pinned so a future"
                + " change to per-rite ink is a deliberate, visible one.");
        }

        // ════════════════════════════════════════════════════════
        // H5 — "Can I re-sleep an elite before it wakes?"
        // ════════════════════════════════════════════════════════

        [Test]
        public void Hypothesis_ReCastingStillHeart_ExtendsTheSleepRatherThanWastingTheCharge()
        {
            // With the old HibernatingEffect this was a silent no-op:
            // OnStack returned true and discarded the incoming effect,
            // so the second cast spent ink and marks, printed a success
            // line, and changed nothing. AsleepByGasEffect refreshes to
            // the larger duration instead.
            var zone = new Zone();
            var rite = Rite<CavesOfOoo.Skills.Rites_StillHeart>(zone, 5, 5, out var caster,
                "StillHeartGrimoire");
            var elite = Creature(zone, "elite", 6, 5);

            elite.ApplyEffect(new WetEffect(1.0f), caster, zone);
            rite.Cast(zone, 1, 0);
            var sleep = elite.GetPart<StatusEffectsPart>().GetEffect<AsleepByGasEffect>();
            Assert.IsNotNull(sleep);

            // Time passes; the nap is nearly over.
            sleep.Duration = 1;

            elite.ApplyEffect(new WetEffect(1.0f), caster, zone);
            elite.ApplyEffect(new FrozenEffect(), caster, zone);
            rite.Cast(zone, 1, 0);

            var after = elite.GetPart<StatusEffectsPart>().GetEffect<AsleepByGasEffect>();
            Assert.IsNotNull(after, "still asleep");
            Assert.Greater(after.Duration, 1,
                "a second cast that spends ink and marks must buy more sleep");
        }

        // ════════════════════════════════════════════════════════
        // H6 — "I primed three statuses. Which two get eaten?"
        // ════════════════════════════════════════════════════════

        [Test]
        public void Hypothesis_ATwoSlotRite_LeavesTheThirdStatusForTheFollowUp()
        {
            // Resonance spends greedily in insertion order. A player who
            // primed three and fires a two-slot rite first should still
            // have one left to detonate with. This pins that a two-slot
            // rite genuinely spends TWO — not all three.
            var zone = new Zone();
            var rite = Rite<CavesOfOoo.Skills.Rites_SunderingWord>(zone, 10, 10, out var caster,
                "SunderingWordGrimoire");
            var target = Creature(zone, "target", 11, 10);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);
            target.ApplyEffect(new FrozenEffect(), caster, zone);
            target.ApplyEffect(new BurningEffect(), caster, zone);

            rite.Cast(zone, 0, 0);

            var fx = target.GetPart<StatusEffectsPart>();
            int left = 0;
            if (fx.HasEffect<WetEffect>()) left++;
            if (fx.HasEffect<FrozenEffect>()) left++;
            if (fx.HasEffect<BurningEffect>()) left++;

            Assert.AreEqual(1, left,
                "a two-slot rite eats exactly two of three — the third is"
                + " the player's follow-up");
        }
    }
}
