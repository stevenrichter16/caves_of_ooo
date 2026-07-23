using System;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Dedicated adversarial sweep for Docs/THROWN-MUTATION-COMBAT-PLAN.md
    /// (CLAUDE.md's mandatory adversarial-sweep gate — this feature touches
    /// RNG-gated behavior, cross-actor flows, and diag emission contracts,
    /// per the plan doc's own §4 applicability check). Complements, does
    /// NOT duplicate, the per-sub-milestone tests already shipped in
    /// InventorySystemTests.cs, ProjectileMutationTests.cs,
    /// CalmMutationTests.cs, and RetaliationHookTests.cs — this file probes
    /// bug classes those per-feature tests don't: RNG/probability
    /// boundaries, the newly-wired Body-aware AV path (zero prior coverage
    /// touched it), a previously-untested half of the SM6 fix's own claim
    /// (skill-bonus reflection, not just resistance), and — the most
    /// valuable category — CROSS-SYSTEM integration between the real
    /// production pipelines (ThrowItemCommand / DirectionalProjectileMutationBase.Cast)
    /// and the retaliation hook, which RetaliationHookTests.cs only ever
    /// exercised via synthetic CombatSystem.ApplyDamage(int, ...) calls.
    /// </summary>
    public class ThrownMutationCombatAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP A — Thrown accuracy roll (SM5/D3 Layer 1) boundaries
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Agility3_ExactThirdBoundary_HitRateApproximatelyOneThird()
        {
            // D3's formula: P(hit) = (Agility-2)/Agility. At Agility=3 this
            // is 1/3 -- "1d3" only succeeds on an exact roll of 3. Boundary
            // case explicitly called out in the plan's SM5 test list but
            // never actually written as its own test (SM5's shipped tests
            // covered Agility 1, 2, 4, 20 -- not 3).
            var zone = new Zone("Adversarial.Agility3");
            int hits = 0;
            const int trials = 60;
            for (int seed = 1; seed <= trials; seed++)
            {
                var actor = CreateThrowActor();
                actor.SetStatValue("Agility", 3);
                zone.AddEntity(actor, 5, 5);
                var weapon = CreateThrowableWeapon("1d1", 0);
                actor.GetPart<InventoryPart>().AddObject(weapon);
                var target = CreateTargetDummy(10);
                zone.AddEntity(target, 7, 5);

                InventorySystem.ExecuteCommand(
                    new ThrowItemCommand(weapon, 7, 5, new Random(seed)), actor, zone);
                if (target.GetStatValue("Hitpoints", 10) < 10) hits++;

                zone.RemoveEntity(actor);
                zone.RemoveEntity(target);
            }

            // Wide tolerance band around the true 1/3 (20/60) -- this is a
            // probability-boundary sanity check, not a precision estimate.
            Assert.Greater(hits, 10, $"Agility 3 should hit noticeably more than never ({hits}/{trials})");
            Assert.Less(hits, 35, $"Agility 3 should miss noticeably more than never ({hits}/{trials})");
        }

        [Test]
        public void Agility0_NeverCrashes_AlwaysMisses()
        {
            // Boundary: DiceRoller.Roll("1d" + 0, rng) -- "1d0" DOES match the
            // NdS regex (sides=0), so it's not the "invalid pattern" fallback;
            // instead Random.Next(1, 0+1) = Random.Next(1,1), which .NET
            // special-cases to always return 1 when min==max. So Agility=0
            // must be a deterministic, crash-free, guaranteed miss (1 is
            // never >= 3), distinct from CalmMutation's "0" DamageDice
            // (which DOES hit the invalid-pattern fallback, since "0" has no
            // "d" in it at all).
            var zone = new Zone("Adversarial.Agility0");
            var actor = CreateThrowActor();
            actor.SetStatValue("Agility", 0);
            zone.AddEntity(actor, 5, 5);
            var weapon = CreateThrowableWeapon("1d1", 0);
            actor.GetPart<InventoryPart>().AddObject(weapon);
            var target = CreateTargetDummy(10);
            zone.AddEntity(target, 7, 5);

            InventoryCommandResult result = default;
            Assert.DoesNotThrow(() =>
            {
                result = InventorySystem.ExecuteCommand(
                    new ThrowItemCommand(weapon, 7, 5, new Random(1)), actor, zone);
            });

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(10, target.GetStatValue("Hitpoints", 10), "Agility 0 must always miss.");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP B — Thrown penetration/AV wiring (SM5c/D3 Layer 3)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void MaxStrengthBonus_CapsStrengthContribution_ButNotWeaponPenBonus()
        {
            // SM5c wired MaxStrengthBonus in for the first time -- the old
            // code never read it at all. A capped weapon (MaxStrengthBonus=0)
            // must deal less-or-equal damage than an identical but uncapped
            // weapon, at the same high Strength and same seed -- proving the
            // cap genuinely suppresses the Strength contribution specifically
            // (weaponPenBonus itself is never capped by this formula, since
            // it appears on both sides of Math.Min(bonus, maxBonus)).
            var zoneCapped = new Zone("Adversarial.Capped");
            var cappedActor = CreateThrowActor();
            cappedActor.SetStatValue("Strength", 30); // large positive modifier
            zoneCapped.AddEntity(cappedActor, 5, 5);
            var cappedWeapon = CreateThrowableWeapon("3d6", 0);
            cappedWeapon.GetPart<MeleeWeaponPart>().MaxStrengthBonus = 0; // fully caps Strength's contribution
            cappedActor.GetPart<InventoryPart>().AddObject(cappedWeapon);
            var cappedTarget = CreateTargetDummy(1000);
            cappedTarget.AddPart(new ArmorPart { AV = 5 });
            zoneCapped.AddEntity(cappedTarget, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(cappedWeapon, 7, 5, new Random(1)), cappedActor, zoneCapped);
            int cappedDamage = 1000 - cappedTarget.GetStatValue("Hitpoints", 1000);

            var zoneUncapped = new Zone("Adversarial.Uncapped");
            var uncappedActor = CreateThrowActor();
            uncappedActor.SetStatValue("Strength", 30);
            zoneUncapped.AddEntity(uncappedActor, 5, 5);
            var uncappedWeapon = CreateThrowableWeapon("3d6", 0); // MaxStrengthBonus stays -1 (uncapped sentinel)
            uncappedActor.GetPart<InventoryPart>().AddObject(uncappedWeapon);
            var uncappedTarget = CreateTargetDummy(1000);
            uncappedTarget.AddPart(new ArmorPart { AV = 5 });
            zoneUncapped.AddEntity(uncappedTarget, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(uncappedWeapon, 7, 5, new Random(1)), uncappedActor, zoneUncapped);
            int uncappedDamage = 1000 - uncappedTarget.GetStatValue("Hitpoints", 1000);

            Assert.LessOrEqual(cappedDamage, uncappedDamage,
                "A MaxStrengthBonus=0 weapon must not out-damage an identical uncapped weapon at the same high Strength.");
            Assert.Greater(uncappedDamage, cappedDamage,
                "At Strength 30 vs AV 5, the cap must make a real, observable difference.");
        }

        [Test]
        public void ImprovisedItem_StrengthContribution_NeverArtificiallyCapped()
        {
            // Counter-check for the test above: an improvised (no
            // MeleeWeaponPart) throw has no MaxStrengthBonus field at all --
            // GetThrownDamage must default it to the uncapped sentinel (-1),
            // never accidentally capping at 0 (which would make a strong
            // actor's improvised throw deal the same as a weak one).
            var zoneWeak = new Zone("Adversarial.ImprovisedWeak");
            var weakActor = CreateThrowActor();
            weakActor.SetStatValue("Strength", 16); // modifier 0
            zoneWeak.AddEntity(weakActor, 5, 5);
            var weakItem = CreateHandledItem(physicsWeight: 6);
            weakActor.GetPart<InventoryPart>().AddObject(weakItem);
            var weakTarget = CreateTargetDummy(1000);
            weakTarget.AddPart(new ArmorPart { AV = 3 });
            zoneWeak.AddEntity(weakTarget, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(weakItem, 7, 5, new Random(1)), weakActor, zoneWeak);
            int weakDamage = 1000 - weakTarget.GetStatValue("Hitpoints", 1000);

            var zoneStrong = new Zone("Adversarial.ImprovisedStrong");
            var strongActor = CreateThrowActor();
            strongActor.SetStatValue("Strength", 30); // large positive modifier
            zoneStrong.AddEntity(strongActor, 5, 5);
            var strongItem = CreateHandledItem(physicsWeight: 6);
            strongActor.GetPart<InventoryPart>().AddObject(strongItem);
            var strongTarget = CreateTargetDummy(1000);
            strongTarget.AddPart(new ArmorPart { AV = 3 });
            zoneStrong.AddEntity(strongTarget, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(strongItem, 7, 5, new Random(1)), strongActor, zoneStrong);
            int strongDamage = 1000 - strongTarget.GetStatValue("Hitpoints", 1000);

            Assert.Greater(strongDamage, weakDamage,
                "A much stronger actor's improvised throw must penetrate more than a weak actor's, at the same AV -- proving no accidental cap.");
        }

        [Test]
        public void TargetWithBody_UsesSelectHitLocationAndGetPartAV_NoCrash()
        {
            // Zero prior test coverage (across SM1-SM5c) ever threw at a
            // target with a Body -- every fixture used CreateTargetDummy,
            // which has no Body, so GetThrownDamage's `targetBody != null`
            // branch (SelectHitLocation + GetPartAV) never actually ran.
            // Natural armor (not equipped-item armor) applies uniformly
            // across every body part per GetPartAV's own logic, so setting
            // it very high guarantees 0 penetrations regardless of which
            // random body part SelectHitLocation picks -- proving the whole
            // Body-aware branch executes cleanly without needing to predict
            // the RNG's specific hit-location choice.
            var zone = new Zone("Adversarial.BodyTarget");
            var actor = CreateThrowActor();
            zone.AddEntity(actor, 5, 5);
            var weapon = CreateThrowableWeapon("3d6", 3);
            actor.GetPart<InventoryPart>().AddObject(weapon);

            var target = CreateCreatureWithBody();
            target.Statistics["Hitpoints"] = new Stat { Owner = target, Name = "Hitpoints", BaseValue = 1000, Min = 0, Max = 1000 };
            target.GetPart<ArmorPart>().AV = 1000; // natural armor -- applies to every body part
            zone.AddEntity(target, 7, 5);

            InventoryCommandResult result = default;
            Assert.DoesNotThrow(() =>
            {
                result = InventorySystem.ExecuteCommand(
                    new ThrowItemCommand(weapon, 7, 5, new Random(1)), actor, zone);
            });

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(1000, target.GetStatValue("Hitpoints", 1000),
                "AV=1000 natural armor must fail every penetration, even via the Body-aware per-part path.");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ThrowPenetration", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"av\":1000", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP C — Mutation damage edge cases (SM6/D9, SM7/D10)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void FireBolt_FullResistance_LogsZeroDamage_DiagShowsZeroActualDamage()
        {
            // Boundary the SM6 tests never exercised: 100% HeatResistance
            // zeroes actualDamage entirely (not just reduces it). The fix
            // must show "for 0 damage" (not silently skip the message, and
            // not show the stale pre-fix raw roll), and the MutationDamage
            // diag's actualDamage field must be 0 while rawRoll stays > 0.
            var zone = new Zone("Adversarial.FullResist");
            var caster = CreateMutationCaster();
            var target = CreateMutationTarget("snapjaw", 20);
            target.Statistics["HeatResistance"] = new Stat
            { Name = "HeatResistance", BaseValue = 100, Min = -100, Max = 100 };

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new FireBoltMutation(), 1);
            var fireBolt = mutations.GetMutation<FireBoltMutation>();

            int rawRoll = DiceRoller.Roll("2d4", new Random(42));
            int hpBefore = target.GetStatValue("Hitpoints", 20);

            bool cast = fireBolt.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast);
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints", 20), "100% resistance must fully absorb the hit.");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("for 0 damage")),
                $"Messages: {string.Join(" | ", MessageLog.GetMessages())}");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "MutationDamage", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"actualDamage\":0", recs[0].PayloadJson);
            StringAssert.Contains($"\"rawRoll\":{rawRoll}", recs[0].PayloadJson);
            Assert.Greater(rawRoll, 0, "sanity: the raw roll itself must be positive for this to be a meaningful test.");
        }

        [Test]
        public void FireBolt_WithSpellcraftEmpower_MessageAndDiagReflectSkillBonus_NotJustResistance()
        {
            // SM6's commit message claimed the fix "closes both
            // discrepancies (resistance AND skill bonus) -- one fix closes
            // both discrepancies," but every SM6/SM7 test only ever
            // exercised the resistance half. This proves the other half:
            // ApplySpellDamage's skill-modifier folding
            // (SkillEventDispatcher.GetSpellDamageModifier) is genuinely
            // reflected in the logged/diagged actualDamage, not just
            // resistance reduction.
            var zone = new Zone("Adversarial.SkillBonus");
            var caster = CreateMutationCaster();
            caster.AddPart(new SkillsPart());
            caster.GetPart<SkillsPart>().AddSkill(new Spellcraft_Empower(), source: "test");
            var target = CreateMutationTarget("snapjaw", 20); // no resistance -- isolates the skill-bonus effect

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new FireBoltMutation(), 1);
            var fireBolt = mutations.GetMutation<FireBoltMutation>();

            int rawRoll = DiceRoller.Roll("2d4", new Random(42));
            int expectedDamage = rawRoll + Spellcraft_Empower.EMPOWER_BONUS;

            bool cast = fireBolt.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast);
            int hpAfter = target.GetStatValue("Hitpoints", 20);
            Assert.AreEqual(20 - expectedDamage, hpAfter);
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains($"for {expectedDamage} damage")),
                $"Messages: {string.Join(" | ", MessageLog.GetMessages())}");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "MutationDamage", Limit = 5 }).Records;
            StringAssert.Contains($"\"actualDamage\":{expectedDamage}", recs[0].PayloadJson);
        }

        [Test]
        public void MutationDamageDiag_MutationClassField_ReflectsConcreteSubclass_NotHardcoded()
        {
            // Q2 cross-feature-consistency check: mutationClass must be
            // GetType().Name, genuinely varying per concrete subclass, not
            // a copy-paste-hardcoded "FireBoltMutation" string that would
            // silently mislabel every other of the 9 subclasses.
            var zone = new Zone("Adversarial.MutationClassField");
            var caster = CreateMutationCaster();
            var target = CreateMutationTarget("snapjaw", 20);
            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new IceShardMutation(), 1);
            var iceShard = mutations.GetMutation<IceShardMutation>();

            iceShard.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "MutationDamage", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"mutationClass\":\"IceShardMutation\"", recs[0].PayloadJson);
            StringAssert.DoesNotContain("FireBoltMutation", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP D — Retaliation cross-system integration (SM5b/D13)
        //   RetaliationHookTests.cs only ever called
        //   CombatSystem.ApplyDamage(int,...) directly (synthetic). These
        //   tests go through the REAL production pipelines
        //   (ThrowItemCommand / DirectionalProjectileMutationBase.Cast) to
        //   prove the hook actually connects end-to-end, not just in
        //   isolation.
        // ════════════════════════════════════════════════════════════

        [Test]
        public void RealThrowItemCommand_AgainstNeutralNpc_ProvokesRetaliation_EndToEnd()
        {
            var zone = new Zone("Adversarial.ThrowRetaliation");
            var actor = CreateThrowActor();
            actor.Tags["Player"] = "";
            zone.AddEntity(actor, 5, 5);
            var weapon = CreateThrowableWeapon("2d4", 0);
            actor.GetPart<InventoryPart>().AddObject(weapon);
            var npc = CreateMutationTarget("neutral-npc", 100);
            npc.Tags["Faction"] = "Monsters";
            npc.AddPart(new BrainPart());
            zone.AddEntity(npc, 7, 5);

            var brain = npc.GetPart<BrainPart>();
            Assert.IsFalse(brain.IsPersonallyHostileTo(actor));

            var result = InventorySystem.ExecuteCommand(
                new ThrowItemCommand(weapon, 7, 5, new Random(1)), actor, zone);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.Less(npc.GetStatValue("Hitpoints", 100), 100, "sanity: real damage must have landed.");
            Assert.IsTrue(brain.IsPersonallyHostileTo(actor),
                "A real end-to-end thrown-weapon hit through the production pipeline must provoke retaliation.");
        }

        [Test]
        public void RealMutationCast_AgainstNeutralNpc_ProvokesRetaliation_EndToEnd()
        {
            var zone = new Zone("Adversarial.MutationRetaliation");
            var caster = CreateMutationCaster();
            caster.Tags["Player"] = "";
            var npc = CreateMutationTarget("neutral-npc", 100);
            npc.Tags["Faction"] = "Monsters";
            npc.AddPart(new BrainPart());

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(npc, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new FireBoltMutation(), 1);
            var fireBolt = mutations.GetMutation<FireBoltMutation>();

            var brain = npc.GetPart<BrainPart>();
            Assert.IsFalse(brain.IsPersonallyHostileTo(caster));

            bool cast = fireBolt.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast);
            Assert.Less(npc.GetStatValue("Hitpoints", 100), 100, "sanity: real damage must have landed.");
            Assert.IsTrue(brain.IsPersonallyHostileTo(caster),
                "A real end-to-end mutation hit through the production pipeline must provoke retaliation.");
        }

        [Test]
        public void ThrownFailsToPenetrate_DoesNotProvokeRetaliation()
        {
            // D13's design is explicit: "placed at damage-landed time...
            // this hook only fires when damage actually connects." A throw
            // that fails to penetrate armor never calls ApplyDamage at all
            // (GetThrownDamage returns 0 before any Damage object is even
            // built) -- this is CONSISTENT with the documented design, not
            // a bug: pinning it so a future refactor doesn't "fix" it into
            // retaliating on a bounced-off hit.
            var zone = new Zone("Adversarial.ThrowFailPenetrateRetaliation");
            var actor = CreateThrowActor();
            actor.Tags["Player"] = "";
            zone.AddEntity(actor, 5, 5);
            var weapon = CreateThrowableWeapon("3d6", 3);
            actor.GetPart<InventoryPart>().AddObject(weapon);
            var npc = CreateMutationTarget("neutral-npc", 1000);
            npc.Tags["Faction"] = "Monsters";
            npc.AddPart(new BrainPart());
            npc.GetPart<ArmorPart>().AV = 1000; // CreateMutationTarget already adds an ArmorPart -- set its AV, don't add a second one
            zone.AddEntity(npc, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(weapon, 7, 5, new Random(1)), actor, zone);

            Assert.AreEqual(1000, npc.GetStatValue("Hitpoints", 1000), "sanity: the hit must have failed to penetrate.");
            Assert.IsFalse(npc.GetPart<BrainPart>().IsPersonallyHostileTo(actor),
                "A hit that deals zero damage (bounced off armor) must not provoke retaliation.");
        }

        [Test]
        public void MutationFullyResisted_DoesNotProvokeRetaliation()
        {
            // Same design principle as the test above, via the mutation
            // path: 100% HeatResistance means CombatSystem.ApplyDamage's own
            // "Resistance fully absorbed" branch returns BEFORE the
            // retaliation hook ever runs (the hook lives further down,
            // alongside DamageDealt) -- zero landed damage, zero retaliation.
            var zone = new Zone("Adversarial.MutationFullResistRetaliation");
            var caster = CreateMutationCaster();
            caster.Tags["Player"] = "";
            var npc = CreateMutationTarget("neutral-npc", 20);
            npc.Tags["Faction"] = "Monsters";
            npc.AddPart(new BrainPart());
            npc.Statistics["HeatResistance"] = new Stat
            { Name = "HeatResistance", BaseValue = 100, Min = -100, Max = 100 };

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(npc, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new FireBoltMutation(), 1);
            var fireBolt = mutations.GetMutation<FireBoltMutation>();

            fireBolt.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.AreEqual(20, npc.GetStatValue("Hitpoints", 20), "sanity: 100% resistance must fully absorb the hit.");
            Assert.IsFalse(npc.GetPart<BrainPart>().IsPersonallyHostileTo(caster),
                "A fully-resisted hit (zero landed damage) must not provoke retaliation.");
        }

        [Test]
        public void ThrownAccuracyMiss_DoesNotProvokeRetaliation()
        {
            // Trivially true (ApplyDamage is never even called on a miss),
            // but pinned end-to-end so the invariant has a regression
            // target through the real production pipeline, not just the
            // synthetic ApplyDamage-level tests.
            var zone = new Zone("Adversarial.ThrowMissRetaliation");
            var actor = CreateThrowActor();
            actor.Tags["Player"] = "";
            actor.SetStatValue("Agility", 1); // guaranteed miss
            zone.AddEntity(actor, 5, 5);
            var weapon = CreateThrowableWeapon("1d1", 0);
            actor.GetPart<InventoryPart>().AddObject(weapon);
            var npc = CreateMutationTarget("neutral-npc", 10);
            npc.Tags["Faction"] = "Monsters";
            npc.AddPart(new BrainPart());
            zone.AddEntity(npc, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(weapon, 7, 5, new Random(1)), actor, zone);

            Assert.AreEqual(10, npc.GetStatValue("Hitpoints", 10));
            Assert.IsFalse(npc.GetPart<BrainPart>().IsPersonallyHostileTo(actor));
        }

        [Test]
        public void ThrownLethalHit_StillProvokesRetaliation_ThroughRealPipeline()
        {
            // End-to-end complement to RetaliationHookTests's synthetic
            // ApplyDamage_PlayerLandsFatalHit test -- confirms the same
            // "hostile before death" ordering holds through the real
            // ThrowItemCommand pipeline, not just a direct ApplyDamage call.
            var zone = new Zone("Adversarial.ThrowLethalRetaliation");
            var actor = CreateThrowActor();
            actor.Tags["Player"] = "";
            actor.SetStatValue("Strength", 30);
            zone.AddEntity(actor, 5, 5);
            var weapon = CreateThrowableWeapon("10d10", 10);
            actor.GetPart<InventoryPart>().AddObject(weapon);
            var npc = CreateMutationTarget("neutral-npc", 5); // low HP, 0 AV -- guaranteed overkill
            npc.Tags["Faction"] = "Monsters";
            npc.AddPart(new BrainPart());
            zone.AddEntity(npc, 7, 5);

            var brain = npc.GetPart<BrainPart>();

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(weapon, 7, 5, new Random(1)), actor, zone);

            Assert.LessOrEqual(npc.GetStatValue("Hitpoints", 5), 0, "sanity: this hit must be lethal.");
            Assert.IsTrue(brain.IsPersonallyHostileTo(actor),
                "Retaliation must register on a killing thrown-weapon blow, through the real pipeline.");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP E — Diag emission contract counter-checks (D7)
        //   "fires exactly once per landed hit, never on a miss/veto"
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TonicThrow_NeverEmitsThrowHitRollOrThrowPenetration()
        {
            // Tonics/grenades are checked BEFORE the accuracy branch in
            // ThrowItemCommand's if/else-if chain -- they must never emit
            // either new diag kind, proving the kinds are scoped strictly
            // to the plain-damage path, not leaking into the tonic AOE path.
            var zone = new Zone("Adversarial.TonicNoDiag");
            var actor = CreateThrowActor();
            zone.AddEntity(actor, 5, 5);
            var tonic = CreateThrowableTonic();
            actor.GetPart<InventoryPart>().AddObject(tonic);
            var target = CreateTargetDummy(20);
            zone.AddEntity(target, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(tonic, 7, 5, new Random(1)), actor, zone);

            var hitRollRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ThrowHitRoll", Limit = 5 }).Records;
            var penRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ThrowPenetration", Limit = 5 }).Records;
            Assert.AreEqual(0, hitRollRecs.Count, "Tonic throws must never roll/emit thrown-weapon accuracy.");
            Assert.AreEqual(0, penRecs.Count, "Tonic throws must never roll/emit thrown-weapon penetration.");
        }

        [Test]
        public void ThrowMiss_EmitsThrowHitRoll_ButNotThrowPenetration()
        {
            // A missed throw never reaches GetThrownDamage at all -- the two
            // new diag kinds must be independently gated to their own roll,
            // not accidentally paired/duplicated.
            var zone = new Zone("Adversarial.MissDiagSeparation");
            var actor = CreateThrowActor();
            actor.SetStatValue("Agility", 1); // guaranteed miss
            zone.AddEntity(actor, 5, 5);
            var weapon = CreateThrowableWeapon("1d1", 0);
            actor.GetPart<InventoryPart>().AddObject(weapon);
            var target = CreateTargetDummy(10);
            zone.AddEntity(target, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(weapon, 7, 5, new Random(1)), actor, zone);

            var hitRollRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ThrowHitRoll", Limit = 5 }).Records;
            var penRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ThrowPenetration", Limit = 5 }).Records;
            Assert.AreEqual(1, hitRollRecs.Count, "ThrowHitRoll must fire once, even on a miss.");
            StringAssert.Contains("\"landed\":false", hitRollRecs[0].PayloadJson);
            Assert.AreEqual(0, penRecs.Count, "ThrowPenetration must never fire when the accuracy roll itself missed.");
        }

        [Test]
        public void ThrowHit_EmitsThrowHitRollAndThrowPenetration_ExactlyOnceEach()
        {
            // Complements the miss-case coverage above: a successful hit
            // must fire BOTH kinds, and exactly once each -- not duplicated
            // by, e.g., a stray extra call inside a loop.
            var zone = new Zone("Adversarial.HitDiagCount");
            var actor = CreateThrowActor();
            zone.AddEntity(actor, 5, 5);
            var weapon = CreateThrowableWeapon("3d6", 3);
            actor.GetPart<InventoryPart>().AddObject(weapon);
            var target = CreateTargetDummy(1000);
            zone.AddEntity(target, 7, 5);

            InventorySystem.ExecuteCommand(
                new ThrowItemCommand(weapon, 7, 5, new Random(1)), actor, zone);

            var hitRollRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ThrowHitRoll", Limit = 5 }).Records;
            var penRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ThrowPenetration", Limit = 5 }).Records;
            Assert.AreEqual(1, hitRollRecs.Count);
            StringAssert.Contains("\"landed\":true", hitRollRecs[0].PayloadJson);
            Assert.AreEqual(1, penRecs.Count);
        }

        // ===== Helpers =====

        private static Entity CreateThrowActor()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestCreature";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart());
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            return entity;
        }

        private static Entity CreateThrowableWeapon(string damage, int penBonus, int weight = 5)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestWeapon";
            entity.Tags["Item"] = "";
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = damage, PenBonus = penBonus });
            entity.AddPart(new EquippablePart { Slot = "Hand" });
            entity.AddPart(new HandlingPart { GripType = GripType.OneHand, Throwable = true, Carryable = true });
            return entity;
        }

        private static Entity CreateThrowableTonic()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestTonic";
            entity.Tags["Item"] = "";
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            entity.AddPart(new HandlingPart { GripType = GripType.OneHand, Throwable = true, Carryable = true });
            entity.AddPart(new TonicPart { Healing = "2d4" });
            return entity;
        }

        private static Entity CreateHandledItem(int physicsWeight)
        {
            var entity = new Entity();
            entity.BlueprintName = "HandledItem";
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = "handled item" });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = physicsWeight });
            entity.AddPart(new HandlingPart { GripType = GripType.OneHand, Carryable = true, Throwable = true });
            return entity;
        }

        private static Entity CreateTargetDummy(int hitpoints)
        {
            var entity = new Entity();
            entity.BlueprintName = "TargetDummy";
            entity.Tags["Creature"] = "";
            entity.AddPart(new RenderPart { DisplayName = "target dummy" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hitpoints, Min = 0, Max = hitpoints };
            return entity;
        }

        /// <summary>Mirrors CombatSystemSpecTests.CreateCreatureWithBody() exactly.</summary>
        private static Entity CreateCreatureWithBody()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestCreature";
            entity.Tags["Creature"] = "";

            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Owner = entity, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            entity.AddPart(new RenderPart { DisplayName = "test creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });

            var body = new Body();
            entity.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());

            return entity;
        }

        private static Entity CreateMutationCaster()
        {
            var entity = new Entity();
            entity.BlueprintName = "caster";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "caster" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d2" });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            entity.AddPart(new ActivatedAbilitiesPart());
            entity.AddPart(new MutationsPart());
            return entity;
        }

        private static Entity CreateMutationTarget(string name, int hp)
        {
            var entity = new Entity { BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d2" });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            return entity;
        }
    }
}
