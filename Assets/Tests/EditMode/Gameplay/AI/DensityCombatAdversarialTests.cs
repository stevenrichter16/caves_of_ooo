using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent parser, actor-boundary, geometry, save and assist probes.</summary>
    public sealed class DensityCombatAdversarialTests
    {
        DensityCombatFixture f;
        bool previousAiChannel;
        [SetUp] public void Setup()
        {
            f = new DensityCombatFixture(); previousAiChannel = Diag.IsChannelEnabled("ai");
            Diag.SetChannel("ai", true);
        }
        [TearDown] public void TearDown() { Diag.SetChannel("ai", previousAiChannel); f.Dispose(); }
        static ActivatedAbility Ability(Entity actor) => DensityCombatFixture.Ability(actor);
        static bool Hostile(Entity e, Entity target) => e.GetPart<BrainPart>().IsPersonallyHostileTo(target);

        [TestCase(null)] [TestCase("")] [TestCase(" ; ; ")] [TestCase("NotASkill")]
        [TestCase("cryomancy_icelance")]
        public void Adversarial_EmptyOrUnsupportedList_DoesNotInventAbilities(string raw)
        {
            var actor = f.Actor(skills: raw);
            Assert.IsNull(actor.GetPart<SkillsPart>()); Assert.IsNull(actor.GetPart<ActivatedAbilitiesPart>());
            Assert.IsFalse(f.Cast(actor, f.Target()));
        }
        [Test] public void Adversarial_MalformedAndRepeatedNames_KeepOneValidRegistration()
        {
            var actor = f.Actor(skills: "NoSuchSkill; Cryomancy_IceLance ;;Cryomancy_IceLance");
            Assert.AreEqual(1, actor.GetPart<SkillsPart>().SkillList.Count);
            Assert.AreEqual("CommandIceLance", Ability(actor).Command);
            Assert.IsTrue(f.Cast(actor, f.Target()));
        }
        [Test] public void Adversarial_ReplayedCreation_DoesNotResetCooldownOrReplaceAbility()
        {
            var actor = f.Actor(); var target = f.Target(); Assert.IsTrue(f.Cast(actor, target));
            var before = Ability(actor); actor.FireEventAndRelease(GameEvent.New("ObjectCreated"));
            Assert.AreSame(before, Ability(actor)); Assert.AreEqual(8, before.CooldownRemaining);
        }
        [Test] public void Adversarial_PlayerCannotReceiveAnNpcKitThroughCreation()
        {
            var player = f.Target(); var count = player.GetPart<ActivatedAbilitiesPart>().AbilityList.Count;
            player.AddPart(new CombatTacticsPart { SkillClasses = "Cryomancy_IceLance" });
            player.FireEventAndRelease(GameEvent.New("ObjectCreated"));
            Assert.AreEqual(count, player.GetPart<ActivatedAbilitiesPart>().AbilityList.Count);
        }

        [TestCase("dead-actor")] [TestCase("dead-target")] [TestCase("removed-actor")]
        [TestCase("removed-target")] [TestCase("not-creature")] [TestCase("same-actor")]
        [TestCase("neutral")]
        public void Adversarial_InvalidTargetNeverReceivesDamageOrCooldown(string condition)
        {
            var actor = f.Actor(); var target = f.Target();
            switch (condition)
            {
                case "dead-actor": actor.Statistics["Hitpoints"].BaseValue = 0; break;
                case "dead-target": target.Statistics["Hitpoints"].BaseValue = 0; break;
                case "removed-actor": f.Zone.RemoveEntity(actor); break;
                case "removed-target": f.Zone.RemoveEntity(target); break;
                case "not-creature": target.Tags.Remove("Creature"); break;
                case "same-actor": target = actor; break;
                case "neutral": actor.Tags["Faction"] = "Villagers"; break;
            }
            int before = target.GetStatValue("Hitpoints");
            Assert.IsFalse(f.Cast(actor, target)); Assert.AreEqual(before, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, Ability(actor).CooldownRemaining);
        }
        [TestCase("null-zone")] [TestCase("null-rng")] [TestCase("null-target")] [TestCase("foreign-zone")]
        public void Adversarial_MissingContextDoesNotUseAmbientRandomOrForeignMap(string condition)
        {
            var actor = f.Actor(); var target = f.Target(); var zone = f.Zone; System.Random rng = new System.Random(1);
            if (condition == "null-zone") zone = null;
            if (condition == "null-rng") rng = null;
            if (condition == "null-target") target = null;
            if (condition == "foreign-zone") zone = new Zone("other");
            Assert.IsFalse(actor.GetPart<CombatTacticsPart>().TryUseAbility(target, zone, rng));
            Assert.AreEqual(0, Ability(actor).CooldownRemaining);
        }
        [TestCase(-50, false)] [TestCase(150, true)]
        public void Adversarial_OutOfBoundsChanceClamps(int chance, bool expected)
        {
            var actor = f.Actor(); actor.GetPart<CombatTacticsPart>().AbilityChance = chance;
            Assert.AreEqual(expected, f.Cast(actor, f.Target(), new DensityCombatFixture.HighRandom()));
        }
        [Test] public void Adversarial_ZeroChanceAndNoCandidatesDoNotAdvanceRng()
        {
            var actor = f.Actor(); var target = f.Target(); var rng = new CountingRandom();
            actor.GetPart<CombatTacticsPart>().AbilityChance = 0;
            Assert.IsFalse(f.Cast(actor, target, rng)); Assert.AreEqual(0, rng.Calls);
            actor.GetPart<CombatTacticsPart>().AbilityChance = 50; Ability(actor).CooldownRemaining = 2;
            Assert.IsFalse(f.Cast(actor, target, rng)); Assert.AreEqual(0, rng.Calls);
        }
        sealed class CountingRandom : System.Random
        {
            public int Calls;
            public override int Next(int maxValue) { Calls++; return 0; }
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_MultipleEligiblePowersUseRngAndOnlyOneCooldown(bool high)
        {
            var actor = f.Actor(skills: "ShortBlades_Shank;LongBlades_Lunge");
            f.Equip(actor, "Dagger").GetPart<MeleeWeaponPart>().Attributes = "Piercing LongBlades";
            var target = f.Target(11, 10);
            Assert.IsTrue(f.Cast(actor, target, high ? (System.Random)new DensityCombatFixture.HighRandom() : new DensityCombatFixture.LowRandom()));
            var abilities = actor.GetPart<ActivatedAbilitiesPart>().AbilityList;
            Assert.AreEqual(1, abilities.Count(a => a.CooldownRemaining > 0));
            Assert.AreEqual(high ? "CommandLunge" : "CommandShank", abilities.Single(a => a.CooldownRemaining > 0).Command);
        }
        [Test] public void Adversarial_CoolingFirstPowerDoesNotShadowReadySecond()
        {
            var actor = f.Actor(skills: "Corrosion_AcidSpray;Cryomancy_IceLance");
            var abilities = actor.GetPart<ActivatedAbilitiesPart>().AbilityList; abilities[0].CooldownRemaining = 7;
            Assert.IsTrue(f.Cast(actor, f.Target())); Assert.AreEqual(7, abilities[0].CooldownRemaining);
            Assert.AreEqual(8, abilities[1].CooldownRemaining);
        }
        [Test] public void Adversarial_NaturalAxeDoesNotSatisfyEquippedWeaponGate()
        {
            var actor = f.Actor(skills: "Axe_Berserk");
            actor.AddPart(new MeleeWeaponPart { Attributes = "Axe" });
            Assert.IsFalse(f.Cast(actor, f.Target(11, 10))); Assert.IsFalse(actor.HasEffect<BerserkEffect>());
        }
        [Test] public void Adversarial_CorruptCommandCannotRouteAnUnrelatedPower()
        {
            var actor = f.Actor(); Ability(actor).Command = "CommandAxeBerserk";
            Assert.IsFalse(f.Cast(actor, f.Target())); Assert.AreEqual(0, Ability(actor).CooldownRemaining);
        }
        [Test] public void Adversarial_UnregisteredSkillCannotSpendAnotherAbility()
        {
            var actor = f.Actor(); actor.GetPart<SkillsPart>().SkillList[0].ActivatedAbilityID = Guid.NewGuid();
            Assert.IsFalse(f.Cast(actor, f.Target())); Assert.AreEqual(0, Ability(actor).CooldownRemaining);
        }
        [Test] public void Adversarial_AlreadyBerserkDoesNotRefreshTheBuffForFree()
        {
            var actor = f.Actor(skills: "Axe_Berserk"); f.Equip(actor, "BreacherCleaver"); var target = f.Target(11, 10);
            Assert.IsTrue(f.Cast(actor, target)); Ability(actor).CooldownRemaining = 0;
            Assert.IsFalse(f.Cast(actor, target)); Assert.AreEqual(0, Ability(actor).CooldownRemaining);
        }
        [TestCase(0,-1)] [TestCase(1,-1)] [TestCase(1,0)] [TestCase(1,1)]
        [TestCase(0,1)] [TestCase(-1,1)] [TestCase(-1,0)] [TestCase(-1,-1)]
        public void Adversarial_AllEightActualRaysCanReachTheirEnemy(int dx, int dy)
        {
            var actor = f.Actor(); var target = f.Target(10 + dx * 3, 10 + dy * 3);
            Assert.IsTrue(f.Cast(actor, target)); Assert.Less(target.GetStatValue("Hitpoints"), 500);
        }
        [Test] public void Adversarial_FootprintContactCanBeOnRayWithoutItsAnchor()
        {
            var actor = f.Actor(); var target = f.Target(13, 11); f.Zone.RemoveEntity(target);
            target.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;0,-1" }); f.Zone.AddEntity(target, 13, 11);
            Assert.IsTrue(f.Cast(actor, target)); Assert.Less(target.GetStatValue("Hitpoints"), 500);
        }
        [Test] public void Adversarial_TargetableSceneryStopsTheRealProjectilePreview()
        {
            var actor = f.Actor(); var target = f.Target(); var tree = f.Factory.CreateEntity("Tree"); f.Zone.AddEntity(tree, 11, 10);
            Assert.IsFalse(f.Cast(actor, target)); Assert.AreEqual(0, Ability(actor).CooldownRemaining);
            f.Zone.RemoveEntity(tree); Assert.IsTrue(f.Cast(actor, target));
        }
        [Test] public void Adversarial_AdjacencyDoesNotAddAnExtraMeleeSwingAfterCasting()
        {
            var actor = f.Actor(skills: "Pyromancy_EmberSpit"); var target = f.Target(11, 10);
            actor.GetPart<BrainPart>().PushGoal(new KillGoal(target)); actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual(497, target.GetStatValue("Hitpoints"), "Only Ember Spit's fixed three damage lands this action.");
            Assert.AreEqual(8, Ability(actor).CooldownRemaining);
        }
        [Test] public void Adversarial_CommandVetoCostsNoCooldownAndFallsBackToMovement()
        {
            var actor = f.Actor(); var target = f.Target(); var veto = new VetoCommand(); actor.AddPart(veto);
            actor.Parts.Remove(veto); actor.Parts.Insert(0, veto);
            actor.GetPart<BrainPart>().PushGoal(new KillGoal(target)); actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(actor)); Assert.AreEqual(500, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, Ability(actor).CooldownRemaining);
        }
        sealed class VetoCommand : Part
        {
            public override bool HandleEvent(GameEvent e) => !e.ID.StartsWith("Command", StringComparison.Ordinal);
        }

        [TestCase("caller-disabled")] [TestCase("different-faction")] [TestCase("passive")]
        [TestCase("dead")] [TestCase("engaged")] [TestCase("party")]
        [TestCase("zero-radius")] [TestCase("negative-radius")] [TestCase("beyond-radius")]
        public void Adversarial_AssistDoesNotOverrideOtherActorBoundaries(string condition)
        {
            var caller = f.Actor(skills: "", assist: true); var ally = f.Actor(10, 12, "", assist: true); var target = f.Target();
            switch (condition)
            {
                case "caller-disabled": caller.GetPart<CombatTacticsPart>().AssistAllies = false; break;
                case "different-faction": ally.Tags["Faction"] = "Villagers"; break;
                case "passive": ally.GetPart<BrainPart>().Passive = true; break;
                case "dead": ally.Statistics["Hitpoints"].BaseValue = 0; break;
                case "engaged": ally.GetPart<BrainPart>().Target = caller; break;
                case "party": ally.GetPart<BrainPart>().SetPartyLeader(target); break;
                case "zero-radius": ally.GetPart<CombatTacticsPart>().AssistRadius = 0; break;
                case "negative-radius": caller.GetPart<CombatTacticsPart>().AssistRadius = -2; break;
                case "beyond-radius": f.Zone.MoveEntity(ally, 10, 17); break;
            }
            caller.GetPart<BrainPart>().SetPersonallyHostile(target); Assert.IsFalse(Hostile(ally, target));
        }
        [Test] public void Adversarial_AssistDoesNotRelayThroughAChainOfAllies()
        {
            var caller = f.Actor(10, 10, "", assist: true); var near = f.Actor(10, 15, "", assist: true);
            var far = f.Actor(10, 20, "", assist: true); var target = f.Target(13, 14);
            caller.GetPart<BrainPart>().SetPersonallyHostile(target);
            Assert.IsTrue(Hostile(near, target)); Assert.IsFalse(Hostile(far, target));
        }
        [Test] public void Adversarial_ReceiverMustSeeThreatAsWellAsCaller()
        {
            var caller = f.Actor(skills: "", assist: true); var ally = f.Actor(10, 12, "", assist: true);
            var target = f.Target(13, 12); f.Wall(11, 12);
            caller.GetPart<BrainPart>().SetPersonallyHostile(target); Assert.IsFalse(Hostile(ally, target));
        }
        [Test] public void Adversarial_AssistBoundaryAtSixCellsStillWorks()
        {
            var caller = f.Actor(skills: "", assist: true); var ally = f.Actor(10, 16, "", assist: true); var target = f.Target(13, 14);
            caller.GetPart<BrainPart>().SetPersonallyHostile(target); Assert.IsTrue(Hostile(ally, target));
        }
        [Test] public void Adversarial_FirstProactiveAggroAlertsWithoutRequiringDamage()
        {
            var caller = f.Actor(skills: "", assist: true); var ally = f.Actor(10, 12, "", assist: true); var target = f.Target();
            caller.FireEventAndRelease(GameEvent.New("TakeTurn")); Assert.IsTrue(Hostile(ally, target));
        }
        [Test] public void Adversarial_DiagnosticsDistinguishCastAndCooldownRefusal()
        {
            var actor = f.Actor(); var target = f.Target(); Assert.IsTrue(f.Cast(actor, target)); Assert.IsFalse(f.Cast(actor, target));
            var used = DiagQuery.Apply(new DiagQuery.Filter { Category = "ai", Kind = "TacticUsed", Actor = actor.ID }).Records;
            Assert.AreEqual(1, used.Count); StringAssert.Contains("CommandIceLance", used[0].PayloadJson); StringAssert.Contains("\"cooldown\":8", used[0].PayloadJson);
            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "ai", Kind = "TacticRejected", Actor = actor.ID }).Records;
            Assert.IsTrue(rejected.Any(r => r.PayloadJson.Contains("\"reason\":\"cooldown\"")));
        }
        [Test] public void Adversarial_GrantAndUseDiagnosticsNameTheSameCommand()
        {
            var actor = f.Actor(); var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "ai", Kind = "TacticGranted", Actor = actor.ID }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"command\":\"CommandIceLance\"", records[0].PayloadJson);
        }
        [Test] public void Adversarial_DisabledDiagnosticsDoNotChangeGameplay()
        {
            var actor = f.Actor(); var target = f.Target(); Diag.SetChannel("ai", false);
            Assert.IsTrue(f.Cast(actor, target));
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter { Category = "ai", Kind = "TacticUsed", Actor = actor.ID }).Records.Count);
        }
        [Test] public void Adversarial_SaveRoundTripPreservesKitAndOneSkillOwnerWithCooldown()
        {
            var actor = f.Actor(assist: true); var target = f.Target(); Assert.IsTrue(f.Cast(actor, target));
            actor.GetPart<CombatTacticsPart>().AbilityChance = 37;
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var tactics = loaded.GetPart<CombatTacticsPart>(); Assert.IsTrue(tactics.AssistAllies); Assert.AreEqual(37, tactics.AbilityChance);
            Assert.AreEqual("Cryomancy_IceLance", tactics.SkillClasses); Assert.AreEqual(8, Ability(loaded).CooldownRemaining);
            var skill = loaded.GetPart<SkillsPart>().SkillList.Single();
            Assert.AreSame(loaded, skill.ParentEntity); Assert.AreSame(skill, loaded.GetPart<Cryomancy_IceLance>());
            Assert.AreEqual(skill.ActivatedAbilityID, Ability(loaded).ID);
            f.Zone.RemoveEntity(actor); f.Zone.AddEntity(loaded, 10, 10);
            Assert.IsFalse(f.Cast(loaded, target));
            for (int i = 0; i < 8; i++) loaded.FireEventAndRelease(GameEvent.New("EndTurn"));
            tactics.AbilityChance = 100; Assert.IsTrue(f.Cast(loaded, target));
        }
        [Test] public void Adversarial_DetachedBrainStillAcceptsPersonalHostility()
        {
            var brain = new BrainPart(); var target = new Entity();
            Assert.DoesNotThrow(() => brain.SetPersonallyHostile(target));
            Assert.IsTrue(brain.IsPersonallyHostileTo(target));
        }
        [Test] public void Adversarial_ExistingCommandCollisionDoesNotLeaveAnUnregisteredSkill()
        {
            var actor = f.Actor(skills: "");
            var abilities = new ActivatedAbilitiesPart(); actor.AddPart(abilities);
            var existing = abilities.AddAbility("Existing", "CommandIceLance", "Other");
            actor.GetPart<CombatTacticsPart>().SkillClasses = "Cryomancy_IceLance";
            actor.FireEventAndRelease(GameEvent.New("ObjectCreated"));
            Assert.IsFalse(actor.GetPart<SkillsPart>()?.HasSkill("Cryomancy_IceLance") ?? false);
            Assert.AreEqual(1, abilities.AbilityList.Count); Assert.AreEqual(existing, abilities.AbilityList[0].ID);
            Assert.IsFalse(f.Cast(actor, f.Target()));
        }
        [Test] public void Adversarial_NonCreatureCannotAcquireNpcSkills()
        {
            var item = f.Factory.CreateEntity("Dagger");
            item.AddPart(new CombatTacticsPart { SkillClasses = "Cryomancy_IceLance" });
            item.FireEventAndRelease(GameEvent.New("ObjectCreated"));
            Assert.IsNull(item.GetPart<SkillsPart>()); Assert.IsNull(item.GetPart<ActivatedAbilitiesPart>());
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_IntermediateChanceActuallyBranches(bool high)
        {
            var actor = f.Actor(); actor.GetPart<CombatTacticsPart>().AbilityChance = 45;
            Assert.AreEqual(!high, f.Cast(actor, f.Target(), high
                ? (System.Random)new DensityCombatFixture.HighRandom() : new DensityCombatFixture.LowRandom()));
        }
        [Test] public void Adversarial_OldCreatureSaveDoesNotAcquireANewKitOnLoad()
        {
            var legacy = f.Factory.CreateEntity("Creature");
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(legacy);
            Assert.IsNull(loaded.GetPart<CombatTacticsPart>()); Assert.IsNull(loaded.GetPart<SkillsPart>());
        }
    }
}
