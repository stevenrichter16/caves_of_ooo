using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Exercises the authored Hurler through the real build service and throw commands.</summary>
    public sealed class ThrowingStartingBuildTests : FiftyWorldFixture
    {
        private const string Measured = "Throwing_MeasuredThrow";
        private const string Recovery = "Throwing_RecoveringStep";
        private static readonly string[] StartingSkills = { "ThrowingSkill", Measured, Recovery };

        [SetUp]
        public void LoadBuilds()
        {
            StartingBuildRegistry.ResetForTests();
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Builds/StartingBuilds.json")));
            SkillRegistry.ResetForTests();
            SkillRegistry.InitializeFromJson(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Skills/Throwing.json")));
        }

        [TearDown]
        public void ResetRegistries()
        {
            StartingBuildRegistry.ResetForTests();
            SkillRegistry.ResetForTests();
        }

        private StartingBuildDef ApplyHurler()
        {
            var build = StartingBuildRegistry.Get("hurler");
            Assert.NotNull(build, "A new player needs a selectable throwing-skill start.");
            var result = StartingBuildService.Apply(Actor, Factory, build);
            Assert.True(result.Success, string.Join("; ", result.Errors));
            return build;
        }

        private static int CarriedUnits(InventoryPart inventory, string blueprint)
            => inventory.Objects.Where(e => e.BlueprintName == blueprint)
                .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);

        [Test]
        public void HurlerHasTheAdvertisedKitWithoutSpendingPointsOrGrantingItsWholeTree()
        {
            int startingSp = Actor.GetStatValue("SP");
            var build = ApplyHurler();
            Assert.AreEqual("The Hurler", build.Name);
            Assert.AreEqual("hurler", Actor.GetProperty("StartingBuild"));
            Assert.AreEqual(20, Actor.GetStatValue("Strength"));
            Assert.AreEqual(20, Actor.GetStatValue("Agility"));
            Assert.AreEqual(16, Actor.GetStatValue("Toughness"));
            Assert.AreEqual(14, Actor.GetStatValue("Ego"));
            Assert.AreEqual(startingSp, Actor.GetStatValue("SP"));

            var backup = StartingBuildService.PrimaryHandWeapon(Actor);
            Assert.NotNull(backup);
            Assert.AreEqual("Dagger", backup.BlueprintName);
            Assert.AreEqual(1, backup.GetPart<StackerPart>().StackCount);
            Assert.AreSame(Actor, backup.GetPart<PhysicsPart>().Equipped);
            Assert.False(Pack.Objects.Contains(backup), "The melee backup must not merge into carried ammunition.");
            Assert.AreEqual(3, CarriedUnits(Pack, "Dagger"));
            Assert.AreEqual(2, CarriedUnits(Pack, "HealingTonic"));
            Assert.AreEqual(2, CarriedUnits(Pack, "DriedMeat"));
            CollectionAssert.AreEquivalent(new[] { "Dagger", "Cloak" }, Pack.GetAllEquipped().Select(e => e.BlueprintName));
            CollectionAssert.AreEquivalent(new[] { "Dagger", "HealingTonic", "DriedMeat" }, Pack.Objects.Select(e => e.BlueprintName).Distinct());

            var skills = Actor.GetPart<SkillsPart>();
            CollectionAssert.AreEqual(StartingSkills, skills.SkillList.Select(s => s.GetType().Name));
            foreach (var skill in StartingSkills) Assert.AreEqual(1, skills.GetRank(skill));
            Assert.False(skills.HasSkill("Throwing_DrivingThrow"));
            Assert.False(skills.HasSkill("Throwing_FocusedSplash"));
            var abilities = Actor.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(2, abilities.AbilityList.Count);
            Assert.AreEqual("CommandMeasuredThrow", abilities.GetAbilityBySlot(0).Command);
            Assert.AreEqual("CommandRecoveringStep", abilities.GetAbilityBySlot(1).Command);
            foreach (var ability in abilities.AbilityList) Assert.Zero(ability.CooldownRemaining);
        }

        [TestCase("Throwing_DrivingThrow", "Throwing_FocusedSplash")]
        [TestCase("Throwing_FocusedSplash", "Throwing_DrivingThrow")]
        public void EitherUnownedSpecializationCostsOneEarnedPoint(string choice, string other)
        {
            ApplyHurler();
            Actor.GetStat("SP").BaseValue = 0;
            Assert.False(BuySkillAction.Execute(Actor, choice).Succeeded);
            Assert.False(Actor.GetPart<SkillsPart>().HasSkill(choice));
            Actor.GetStat("SP").BaseValue = 1;
            Assert.True(BuySkillAction.Execute(Actor, choice).Succeeded);
            Assert.Zero(Actor.GetStatValue("SP"));
            Assert.AreEqual(1, Actor.GetPart<SkillsPart>().GetRank(choice));
            Assert.False(Actor.GetPart<SkillsPart>().HasSkill(other));
        }

        [Test]
        public void BombardierRetainsItsDifferentConsumableAndSpellStart()
        {
            var result = StartingBuildService.Apply(Actor, Factory, StartingBuildRegistry.Get("bombardier"));
            Assert.True(result.Success, string.Join("; ", result.Errors));
            CollectionAssert.AreEqual(new[] { "Hydromancy_DrenchLob", "Cryomancy_ColdSnap", "Spellcraft_Calm" },
                Actor.GetPart<SkillsPart>().SkillList.Select(s => s.GetType().Name));
            Assert.False(Actor.GetPart<SkillsPart>().HasSkill("ThrowingSkill"));
            foreach (var blueprint in new[] { "PoisonGasGrenade", "LightningTonic", "FrostTonic" })
                Assert.AreEqual(1, CarriedUnits(Pack, blueprint));
            Assert.Zero(CarriedUnits(Pack, "Dagger"));
        }

        [Test]
        public void StarterCanPrepareThrowOneCarriedUnitAndRecoverItWithoutEquipping()
        {
            ApplyHurler();
            var backup = StartingBuildService.PrimaryHandWeapon(Actor);
            var source = Pack.Objects.Single(e => e.BlueprintName == "Dagger");
            Assert.AreEqual(3, source.GetPart<StackerPart>().StackCount);
            var target = Place("MarlbackScrabbler", 14, 10);
            target.GetStat("Hitpoints").Max = target.GetStat("Hitpoints").BaseValue = 500;
            Zone.GetCell(14, 10).IsVisible = Zone.GetCell(14, 10).Explored = true;
            int tick = Clock.TickCount;
            Assert.True(ThrowingRules.TryBeginSelection(Actor, "CommandMeasuredThrow", out var selection));
            Assert.AreEqual(ThrowSelectionKind.Measured, selection.Kind);
            Assert.AreEqual(tick, Clock.TickCount, "Opening the source selector is free.");
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.True(ThrowingRules.Prepare(Actor, source, Zone, target, Zone.GetEntityCell(target), out string reason), reason);
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, Measured));
            // The headless fixture explicitly invokes the same owner-turn closing hook.
            // Native input tests separately verify scheduling and payment of the action.
            ThrowingRules.OnOwnerTurnEnded(Actor, Zone);
            Assert.True(Actor.GetPart<ThrowingStatePart>().Prepared);
            var launch = InventorySystem.ExecuteCommand(new ThrowItemCommand(source, 14, 10, new System.Random(123)), Actor, Zone);
            Assert.True(launch.ActionCommitted, launch.ErrorMessage);
            Assert.True(launch.Success, launch.ErrorMessage);
            Assert.Less(target.GetStatValue("Hitpoints"), 500);
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.AreEqual(2, source.GetPart<StackerPart>().StackCount);
            Assert.AreSame(backup, StartingBuildService.PrimaryHandWeapon(Actor));
            var landed = Zone.GetCell(14, 10).Objects.Single(e => e.GetPart<ThrownRecoveryPart>()?.ThrowerId == Actor.ID);
            Assert.AreNotSame(source, landed);
            Assert.AreEqual(1, landed.GetPart<StackerPart>().StackCount);
            Assert.False(ThrowingRules.CanRecover(Actor, landed, Zone, out _), "Recovery cannot reach a distant unit.");

            for (int i = 0; i < 3; i++) Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0));
            Assert.False(ThrowingRules.CanRecover(Actor, landed, Zone, out _), "A surviving target blocks the recovery step.");
            Assert.True(MovementSystem.TryMove(target, Zone, 0, 1));
            var unthrown = Place("Dagger", 14, 10);
            Assert.False(ThrowingRules.CanRecover(Actor, unthrown, Zone, out _), "A matching blueprint is not the exact thrown unit.");
            Assert.True(ThrowingRules.TryBeginSelection(Actor, "CommandRecoveringStep", out selection));
            Assert.AreEqual(ThrowSelectionKind.Recovery, selection.Kind);
            Assert.True(ThrowingRules.CanRecover(Actor, landed, Zone, out reason), reason);
            var recovered = ThrowingRules.Recover(Actor, landed, Zone);
            Assert.True(recovered.Committed, recovered.Reason);
            Assert.True(recovered.Collected, recovered.Reason);
            Assert.AreEqual(14, Zone.GetEntityCell(Actor).X);
            Assert.AreEqual(10, Zone.GetEntityCell(Actor).Y);
            Assert.True(Pack.Objects.Contains(landed));
            Assert.Null(landed.GetPart<PhysicsPart>().Equipped);
            Assert.AreSame(backup, StartingBuildService.PrimaryHandWeapon(Actor));
            Assert.AreEqual(3, CarriedUnits(Pack, "Dagger"));
            Assert.AreSame(Zone.GetCell(14, 10), Zone.GetEntityCell(unthrown));
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, Recovery));
            Assert.False(ThrowingRules.Recover(Actor, landed, Zone).Committed, "The collected unit cannot pay out twice.");
        }

        [Test]
        public void HurlerSurvivesTokenGraphLoadAndCannotClaimASecondStartingKit()
        {
            var build = ApplyHurler();
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Assert.AreEqual("hurler", loaded.GetProperty("StartingBuild"));
            Assert.AreEqual(20, loaded.GetStatValue("Strength"));
            var inventory = loaded.GetPart<InventoryPart>();
            Assert.AreEqual(3, CarriedUnits(inventory, "Dagger"));
            var backup = StartingBuildService.PrimaryHandWeapon(loaded);
            Assert.AreEqual("Dagger", backup.BlueprintName);
            Assert.AreSame(loaded, backup.GetPart<PhysicsPart>().Equipped);
            CollectionAssert.AreEqual(StartingSkills, loaded.GetPart<SkillsPart>().SkillList.Select(s => s.GetType().Name));
            Assert.AreEqual("CommandMeasuredThrow", loaded.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(0).Command);
            Assert.AreEqual("CommandRecoveringStep", loaded.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(1).Command);
            Assert.False(StartingBuildService.Apply(loaded, Factory, build).Success);
            Assert.AreEqual(3, CarriedUnits(inventory, "Dagger"));
            Assert.AreEqual(2, CarriedUnits(inventory, "HealingTonic"));
        }
    }
}
