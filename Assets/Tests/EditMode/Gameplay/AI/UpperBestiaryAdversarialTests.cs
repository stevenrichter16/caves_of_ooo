using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P5 adversarial sweep (ADVERSARIAL_TESTING.md): null boundaries, mid-execution death,
    /// self-reference, probability boundaries, save reach, two actors on one quarry, the
    /// heavy-swing clamp, cured hosts staying cured, and diag for every behaviour.
    /// </summary>
    public sealed class UpperBestiaryAdversarialTests
    {
        DensityCombatFixture f;
        [SetUp] public void Setup() { f = new DensityCombatFixture(); Diag.ResetAll(); Diag.SetChannel("ai", true); }
        [TearDown] public void TearDown() { Diag.SetChannel("ai", false); f.Dispose(); }

        sealed class Rolls : System.Random
        {
            public override int Next(int maxValue) => maxValue - 1;
            public override int Next(int minValue, int maxValue) => maxValue == 11 ? 9 : maxValue - 1;
        }

        Entity Beast(string blueprint, int x, int y)
        {
            var e = f.Factory.CreateEntity(blueprint); e.ID = blueprint + "-" + System.Guid.NewGuid().ToString("N");
            f.Zone.AddEntity(e, x, y);
            var brain = e.GetPart<BrainPart>(); brain.CurrentZone = f.Zone; brain.Rng = new Rolls(); brain.FleeThreshold = 0;
            return e;
        }
        static bool Recorded(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "ai", Kind = kind, Limit = 5 }).Records.Any();

        // ════════════════ nulls ════════════════

        [Test] public void Adversarial_NullsAreHarmlessAtEverySeam()
        {
            var bird = Beast("SkySari", 10, 10);
            var stoop = bird.GetPart<StoopPart>();
            Assert.False(stoop.TryAct(null, f.Zone, null, true));
            Assert.False(stoop.TryAct(f.Target(14, 10), null, null, true));
            Assert.False(stoop.AdvancePending(null, null));
            Assert.False(new GrapplePart().TryAct(null, f.Zone, null, true));
            Assert.AreEqual(0, PackHunterPart.FlankHitBonus(null, null, null));
            Assert.AreEqual(0, new ColonyPart().Alarm(null));
            Assert.DoesNotThrow(() => Noise.Mark(null));
            Assert.False(Noise.Recent(null, 1));
            Assert.False(CreatureBehaviors.TryAct(null, null, null, null, true));
            Assert.False(CreatureBehaviors.AdvancePending(null, null, null));
            Assert.IsNull(CreatureBehaviors.Perceives(null, null, null));
            Assert.DoesNotThrow(() => UrquPressure.OnDeath(null, f.Zone));
            Assert.AreEqual(0, UrquPressure.OnZoneEntered(null, f.Zone));
            Assert.DoesNotThrow(() => UrquPressure.Manifest(-5, 999, "test"));
        }

        // ════════════════ mid-execution death ════════════════

        [Test] public void Adversarial_AQuarryThatDiesUnderTheShadowIsNotStruckAgain()
        {
            var bird = Beast("SkySari", 10, 10); var you = f.Target(14, 10); var stoop = bird.GetPart<StoopPart>();
            stoop.TryAct(you, f.Zone, new Rolls(), true);
            you.GetStat("Hitpoints").BaseValue = 0;
            stoop.AdvancePending(f.Zone, new Rolls());
            Assert.DoesNotThrow(() => stoop.AdvancePending(f.Zone, new Rolls()));
            Assert.True(Recorded("StoopMissed"), "a dead quarry is not a hit");
            Assert.False(stoop.IsAloft);
        }

        [Test] public void Adversarial_AGrapplerThatDiesLetsGo()
        {
            var warden = Beast("Riverwarden", 12, 10); var you = f.Target(13, 10);
            var grip = warden.GetPart<GrapplePart>(); grip.Chance = 100;
            grip.TryAct(you, f.Zone, new Rolls(), true);
            var held = you.GetEffect<GrappledEffect>(); Assume.That(held, Is.Not.Null);
            warden.GetStat("Hitpoints").BaseValue = 0;
            held.OnTurnStart(you);
            Assert.True(held.Broken);
            Assert.True(held.AllowMovement(you), "the dead hold nothing");
        }

        [Test] public void Adversarial_TheDeadDoNotAnswerTheColony()
        {
            var you = f.Target(20, 10);
            var a = Beast("PrickleBrowGecko", 21, 10); var dead = Beast("PrickleBrowGecko", 22, 10);
            dead.GetStat("Hitpoints").BaseValue = 0;
            Assert.AreEqual(1, a.GetPart<ColonyPart>().Alarm(you), "only the living rise");
        }

        // ════════════════ self-reference ════════════════

        [Test] public void Adversarial_NothingStoopsGrapplesOrRaisesAColonyAgainstItself()
        {
            var bird = Beast("SkySari", 10, 10);
            Assert.False(bird.GetPart<StoopPart>().TryAct(bird, f.Zone, new Rolls(), true));
            var warden = Beast("Riverwarden", 20, 10);
            Assert.False(warden.GetPart<GrapplePart>().TryAct(warden, f.Zone, new Rolls(), true));
            var gecko = Beast("PrickleBrowGecko", 30, 10);
            Assert.AreEqual(0, gecko.GetPart<ColonyPart>().Alarm(gecko));
        }

        // ════════════════ probability boundaries ════════════════

        [Test] public void Adversarial_ZeroPercentNeverGrabsAHundredAlwaysDoesWithBlood()
        {
            var warden = Beast("Riverwarden", 12, 10); var grip = warden.GetPart<GrapplePart>();
            var you = f.Target(13, 10);
            grip.Chance = 0;
            for (int i = 0; i < 5; i++) Assert.False(grip.TryAct(you, f.Zone, new Rolls(), true));
            Assert.False(you.HasEffect<GrappledEffect>());
            grip.Chance = 100;
            Assert.True(grip.TryAct(you, f.Zone, new Rolls(), true));
            Assert.True(you.HasEffect<GrappledEffect>());
        }

        [Test] public void Adversarial_ASecondGrapplerCannotTakeAQuarryAlreadyHeld()
        {
            var a = Beast("Riverwarden", 12, 10); var b = Beast("Riverwarden", 14, 10); var you = f.Target(13, 10);
            a.GetPart<GrapplePart>().Chance = 100; b.GetPart<GrapplePart>().Chance = 100;
            a.GetPart<GrapplePart>().TryAct(you, f.Zone, new Rolls(), true);
            b.GetPart<GrapplePart>().TryAct(you, f.Zone, new Rolls(), true);
            Assert.AreEqual(a.ID, you.GetEffect<GrappledEffect>().GrapplerId, "the first grip holds");
            Assert.AreEqual(1, you.GetPart<StatusEffectsPart>().GetAllEffects().Count(e => e is GrappledEffect));
        }

        // ════════════════ the heavy swing's clamp ════════════════

        [Test] public void Adversarial_AZeroOrNegativePercentNeverHeals()
        {
            var attacker = f.Actor(10, 10, skills: ""); var target = f.Target(11, 10);
            int hp = target.GetStatValue("Hitpoints");
            CombatSystem.PerformCommittedMeleeAttack(attacker, target, f.Zone, new Rolls(), -300);
            Assert.LessOrEqual(target.GetStatValue("Hitpoints"), hp);
        }

        // ════════════════ two birds, one quarry ════════════════

        [Test] public void Adversarial_TwoShadowsOnOneQuarryAreTwoDives()
        {
            var a = Beast("SkySari", 10, 10); var b = Beast("SkySari", 18, 10); var you = f.Target(14, 10);
            Assert.True(a.GetPart<StoopPart>().TryAct(you, f.Zone, new Rolls(), true));
            Assert.True(b.GetPart<StoopPart>().TryAct(you, f.Zone, new Rolls(), true));
            int hp = you.GetStatValue("Hitpoints");
            for (int i = 0; i < 2; i++) { a.GetPart<StoopPart>().AdvancePending(f.Zone, new Rolls()); b.GetPart<StoopPart>().AdvancePending(f.Zone, new Rolls()); }
            Assert.Less(you.GetStatValue("Hitpoints"), hp);
            Assert.AreEqual(2, DiagQuery.Apply(new DiagQuery.Filter { Category = "ai", Kind = "StoopHit", Limit = 5 }).Records.Count);
        }

        // ════════════════ save reach ════════════════

        [Test] public void Adversarial_EveryBehaviourStateSurvivesASave()
        {
            var warden = Beast("Riverwarden", 12, 10);
            var g = PartRoundTripHelper.RoundTripEntity(warden).GetPart<GrapplePart>();
            Assert.True(g.Drag); Assert.AreEqual(warden.GetPart<GrapplePart>().DrownDamage, g.DrownDamage);
            var gecko = Beast("PrickleBrowGecko", 20, 10); gecko.GetPart<ColonyPart>().LastAlarmTick = 77;
            Assert.AreEqual(77, PartRoundTripHelper.RoundTripEntity(gecko).GetPart<ColonyPart>().LastAlarmTick);
            var stalker = Beast("PaleStalker", 30, 10);
            Assert.NotNull(PartRoundTripHelper.RoundTripEntity(stalker).GetPart<HuntsBySoundPart>());
            var you = f.Target(31, 15); Noise.Mark(you);
            Assert.AreEqual(you.GetIntProperty(Noise.Property), PartRoundTripHelper.RoundTripEntity(you).GetIntProperty(Noise.Property));
        }

        [Test] public void Adversarial_ACuredHostStaysCured()
        {
            var host = Beast("BloomedWarden", 12, 10);
            Assume.That(host.HasEffect<BloomedEffect>());
            host.RemoveEffect(typeof(BloomedEffect));
            host.FireEventAndRelease(GameEvent.New("BeginTakeAction"));
            Assert.False(host.HasEffect<BloomedEffect>(), "the Choir's cure is not undone by the next turn");
            var loaded = PartRoundTripHelper.RoundTripEntity(host);
            loaded.FireEventAndRelease(GameEvent.New("BeginTakeAction"));
            Assert.False(loaded.HasEffect<BloomedEffect>(), "nor by a save");
        }

        // ════════════════ diag ════════════════

        [Test] public void Adversarial_EveryBehaviourIsRecorded()
        {
            var bird = Beast("SkySari", 10, 10); var you = f.Target(14, 10);
            bird.GetPart<StoopPart>().TryAct(you, f.Zone, new Rolls(), true);
            Assert.True(Recorded("StoopMarked"));
            var warden = Beast("Riverwarden", 30, 10); var other = f.Target(31, 10);
            warden.GetPart<GrapplePart>().Chance = 100; warden.GetPart<GrapplePart>().TryAct(other, f.Zone, new Rolls(), true);
            Assert.True(Recorded("Grappled"));
            var gecko = Beast("PrickleBrowGecko", 40, 10); Beast("PrickleBrowGecko", 41, 10);
            gecko.GetPart<ColonyPart>().Alarm(f.Target(42, 12));
            Assert.True(Recorded("ColonyAlarm"));
            var h1 = Beast("BrittleHound", 51, 10); var h2 = Beast("BrittleHound", 49, 10); var quarry = f.Target(50, 10);
            PackHunterPart.FlankHitBonus(h2, quarry, f.Zone);
            Assert.True(Recorded("Flanked"));
        }
    }
}
