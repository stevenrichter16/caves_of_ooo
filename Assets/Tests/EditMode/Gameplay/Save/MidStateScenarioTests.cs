using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.9 — Mid-state save scenarios. See
    /// <c>Docs/SAVE-LOAD-AUDIT.md §SL.9</c>.
    ///
    /// <para>SL.6 / SL.7 verified data-shape round-trips for each
    /// effect/Part with a canonical setup. SL.9 verifies the
    /// <b>matrix</b>: mid-flux states + multiple
    /// effects/abilities on the same actor at the same time. A bug
    /// that zeros out non-default fields on load would slip past
    /// the single-effect SL.6 tests but be caught here.</para>
    ///
    /// <para><b>Mid-state scenarios pinned:</b></para>
    /// <list type="bullet">
    ///   <item>Ability cooldown mid-tick (Remaining &lt; Max)</item>
    ///   <item>Effect mid-duration (lower than ctor default)</item>
    ///   <item>HookedEffect mid-drag (hooker set, partway through)</item>
    ///   <item>StackCount &gt; 1 (post-stack state)</item>
    ///   <item>Multi-effect actor (3 simultaneous effects, each at
    ///         different mid-flux durations)</item>
    /// </list>
    /// </summary>
    public class MidStateScenarioTests
    {
        // ── Mid-cooldown ──────────────────────────────────────────

        [Test]
        public void Ability_MidCooldown_Remaining_LessThan_Max_RoundTrips()
        {
            // Production-real: ability was just used → CooldownRemaining
            // jumps to MaxCooldown. Then ticks down each turn. Save
            // mid-tick (Remaining < Max). Pin both fields survive
            // independently.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var part = new ActivatedAbilitiesPart();
            actor.AddPart(part);
            var ability = new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                DisplayName = "Slam",
                Command = "CommandSlam",
                Class = "Cudgel",
                MaxCooldown = 6,
                CooldownRemaining = 4, // partway through cooldown
            };
            part.AbilityList.Add(ability);
            part.AbilityByGuid[ability.ID] = ability;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var la = loaded.GetPart<ActivatedAbilitiesPart>().AbilityList[0];
            Assert.AreEqual(4, la.CooldownRemaining,
                "Mid-cooldown Remaining=4 must survive — NOT reset to 0 "
                + "(off-cooldown) or to MaxCooldown (just-used). A bug "
                + "that picks either default would give the player a "
                + "free recast or stick them in eternal cooldown.");
            Assert.AreEqual(6, la.MaxCooldown,
                "MaxCooldown survives alongside Remaining.");
            Assert.IsFalse(la.IsUsable,
                "Derived IsUsable property correctly reads CooldownRemaining > 0.");
        }

        [Test]
        public void Ability_AtCooldownBoundary_OneTickFromUsable_RoundTrips()
        {
            // Boundary: Remaining=1 (one tick from usable). A buggy
            // load that off-by-ones the cooldown would surface here.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var part = new ActivatedAbilitiesPart();
            actor.AddPart(part);
            part.AbilityList.Add(new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                MaxCooldown = 6,
                CooldownRemaining = 1,
            });

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(1,
                loaded.GetPart<ActivatedAbilitiesPart>().AbilityList[0].CooldownRemaining,
                "Boundary: Remaining=1 stays 1.");
        }

        [Test]
        public void MultipleAbilities_EachAtDifferentCooldown_AllSurvive()
        {
            // Adversarial: 3 abilities at distinct cooldown states.
            // A buggy save/load could share state across abilities
            // (e.g. the second ability inherits the first's cooldown).
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var part = new ActivatedAbilitiesPart();
            actor.AddPart(part);
            var idA = part.AddAbility("A", "CmdA", "ClassA");
            var idB = part.AddAbility("B", "CmdB", "ClassB");
            var idC = part.AddAbility("C", "CmdC", "ClassC");
            part.CooldownAbility(idA, 5);  // 5 turns left
            part.CooldownAbility(idB, 2);  // 2 turns left
            // C stays off-cooldown

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var lp = loaded.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(5, lp.GetAbility(idA).CooldownRemaining);
            Assert.AreEqual(2, lp.GetAbility(idB).CooldownRemaining);
            Assert.AreEqual(0, lp.GetAbility(idC).CooldownRemaining,
                "Counter-check: C wasn't put on cooldown — stays 0.");
        }

        // ── Mid-duration effects ──────────────────────────────────

        [Test]
        public void Effect_MidDuration_LowerThanCtorDefault_RoundTrips()
        {
            // RootedEffect's ctor default is duration=4. Apply at 4,
            // simulate 1 tick (drop to 3). Pin 3 round-trips, NOT
            // resets to ctor default of 4.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.ForceApplyEffect(new RootedEffect(duration: 4));
            actor.GetEffect<RootedEffect>().Duration = 3; // simulate 1 tick passed

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(3,
                loaded.GetEffect<RootedEffect>().Duration,
                "Mid-duration 3 (one tick from ctor default 4) round-trips. "
                + "A bug that resets Duration to the ctor-default value "
                + "on load would surface here.");
        }

        [Test]
        public void HookedEffect_MidDrag_HookerStillSet_DurationPartWay()
        {
            // HookedEffect ctor default is duration=9. Simulate 5
            // turns elapsed → Duration=4 with Hooker still set.
            // Pin both survive.
            var hooker = new Entity { ID = "puller", BlueprintName = "Puller" };
            var victim = new Entity { ID = "victim", BlueprintName = "Victim" };
            victim.ForceApplyEffect(new HookedEffect(duration: 9, hooker: hooker));
            victim.GetEffect<HookedEffect>().Duration = 4; // partway through

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(victim);
            var hookEffect = loaded.GetEffect<HookedEffect>();
            Assert.IsNotNull(hookEffect);
            Assert.AreEqual(4, hookEffect.Duration,
                "Mid-drag Duration=4 survives.");
            Assert.IsNotNull(hookEffect.Hooker,
                "Hooker entity ref still resolves after partial-duration save.");
            Assert.AreEqual("puller", hookEffect.Hooker.ID);
        }

        [Test]
        public void ShatterArmorEffect_StackCount_MidStack_RoundTrips()
        {
            // Production-real: 3 successful Shatter hits → StackCount=3.
            // Save mid-stack. Pin StackCount=3 survives.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.ForceApplyEffect(new ShatterArmorEffect());
            actor.GetEffect<ShatterArmorEffect>().StackCount = 3;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(3,
                loaded.GetEffect<ShatterArmorEffect>().StackCount,
                "Mid-stack StackCount=3 survives. NOT reset to the field "
                + "initializer (=1).");
        }

        // ── Multi-effect actor ────────────────────────────────────

        [Test]
        public void MultiEffectActor_AllAtMidDuration_AllSurviveIndependently()
        {
            // Adversarial: 3 simultaneous effects on one actor,
            // EACH at a different non-default duration. A buggy save
            // that shares duration across effects would surface here.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.ForceApplyEffect(new RootedEffect(duration: 5));
            actor.ForceApplyEffect(new StunnedEffect(duration: 3));
            actor.ForceApplyEffect(new ConfusedEffect(duration: 7));
            // Now flux: simulate per-effect ticks
            actor.GetEffect<RootedEffect>().Duration = 4;
            actor.GetEffect<StunnedEffect>().Duration = 1;
            actor.GetEffect<ConfusedEffect>().Duration = 6;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(4, loaded.GetEffect<RootedEffect>().Duration);
            Assert.AreEqual(1, loaded.GetEffect<StunnedEffect>().Duration);
            Assert.AreEqual(6, loaded.GetEffect<ConfusedEffect>().Duration);
        }

        [Test]
        public void Actor_WithCooldown_AndEffect_AndStack_AllMidFlux_AllSurvive()
        {
            // Cumulative: an actor with a mid-cooldown ability, a
            // mid-duration RootedEffect, AND a stacked ShatterArmor —
            // ALL non-default state simultaneously. Pin all survive
            // independently.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var abilities = new ActivatedAbilitiesPart();
            actor.AddPart(abilities);
            var idA = abilities.AddAbility("Slam", "CmdSlam", "Cudgel");
            abilities.CooldownAbility(idA, 3);
            actor.ForceApplyEffect(new RootedEffect(duration: 5));
            actor.GetEffect<RootedEffect>().Duration = 2;
            actor.ForceApplyEffect(new ShatterArmorEffect());
            actor.GetEffect<ShatterArmorEffect>().StackCount = 4;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(3,
                loaded.GetPart<ActivatedAbilitiesPart>().GetAbility(idA).CooldownRemaining,
                "Cooldown survives in the multi-state matrix.");
            Assert.AreEqual(2,
                loaded.GetEffect<RootedEffect>().Duration,
                "Mid-duration effect survives.");
            Assert.AreEqual(4,
                loaded.GetEffect<ShatterArmorEffect>().StackCount,
                "Stacked effect survives.");
        }

        // ── Counter-check ─────────────────────────────────────────

        [Test]
        public void DefaultStateActor_NoMidFlux_RoundTripsAsDefault()
        {
            // Counter-check: an actor with no mid-flux state must
            // round-trip with all defaults intact. Catches a bug
            // where some "save side-effect" mutates state during the
            // save call (e.g. reads Duration via a getter that ticks).
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.ForceApplyEffect(new RootedEffect(duration: 4)); // no manual flux
            int beforeDuration = actor.GetEffect<RootedEffect>().Duration;
            Assert.AreEqual(4, beforeDuration,
                "Setup precondition: ctor default is 4.");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(4, loaded.GetEffect<RootedEffect>().Duration,
                "No flux → no change. Survives intact.");
        }
    }
}
