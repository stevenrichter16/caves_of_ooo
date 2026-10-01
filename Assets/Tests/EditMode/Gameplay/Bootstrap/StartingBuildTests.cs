using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/STARTING-BUILDS-IMPL.md. User-visible invariant: "On a new game
    /// I pick one of four builds (or Classic); the character I get is the
    /// one the card described: its attributes, its weapon in hand and
    /// swinging, its skills on the hotbar in order, and its pack."
    ///
    /// Why the weapon-in-the-primary-hand check is its own test: the primary
    /// hand swings and a shield or torch in it turns your fist into the
    /// primary attack, with the weapon demoted to a 15% off-hand. Equipping a
    /// buckler before a cudgel cut the cudgel's damage from 13.4 to 4.8 per
    /// swing in measurement. See <see cref="BuildWithABucklerFirst_PutsTheFistInTheHandThatSwings"/>.
    /// </summary>
    public class StartingBuildTests
    {
        private static ScenarioTestHarness _h;
        private static readonly string[] Ids = { "duelist", "breaker", "stormcaller", "bombardier" };

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _h = new ScenarioTestHarness();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { _h?.Dispose(); _h = null; }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            StartingBuildRegistry.ResetForTests();
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Builds/StartingBuilds.json")));
        }

        [TearDown]
        public void TearDown() => StartingBuildRegistry.ResetForTests();

        private static Entity FreshPlayer() => _h.CreateContext(playerBlueprint: "Player").PlayerEntity;

        private static StartingBuildDef Build(string id) => StartingBuildRegistry.Get(id);

        // ════════════════════════════════════════════════════════
        // The set
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheSet_IsFourBuildsThenClassic()
        {
            CollectionAssert.AreEqual(new[] { "duelist", "breaker", "stormcaller", "bombardier", "classic" },
                StartingBuildRegistry.All.Select(b => b.Id).ToArray());
            Assert.IsTrue(Build("classic").IsClassic);
            foreach (var id in Ids) Assert.IsFalse(Build(id).IsClassic, id);
        }

        [Test]
        public void ShippedContent_HasNoValidationProblems()
        {
            var problems = StartingBuildRegistry.Validate(_h.Factory);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [TestCase("duelist")] [TestCase("breaker")] [TestCase("stormcaller")] [TestCase("bombardier")]
        public void Attributes_SumTo70_InEvenSteps(string id)
        {
            var a = Build(id).Attributes;
            Assert.AreEqual(70, a.Strength + a.Agility + a.Toughness + a.Ego,
                "same total as today's 18/18/18/16, spent differently");
            foreach (int v in new[] { a.Strength, a.Agility, a.Toughness, a.Ego })
            {
                Assert.AreEqual(0, v % 2, id + ": the modifier is floor((s-16)/2), so odd scores buy nothing");
                Assert.That(v, Is.InRange(14, 22));
            }
        }

        [TestCase("duelist", 6)] [TestCase("breaker", 6)] [TestCase("stormcaller", 6)] [TestCase("bombardier", 3)]
        public void SkillRows_FollowThePowerBudget(string id, int rows)
        {
            Assert.AreEqual(rows, Build(id).Skills.Count);
        }

        // ════════════════════════════════════════════════════════
        // Applying a build
        // ════════════════════════════════════════════════════════

        [TestCase("duelist")] [TestCase("breaker")] [TestCase("stormcaller")] [TestCase("bombardier")]
        public void Apply_SetsTheDeclaredAttributes_AndLeavesTheRestAlone(string id)
        {
            var p = FreshPlayer();
            int hp = p.GetStatValue("Hitpoints"), intel = p.GetStatValue("Intelligence"), will = p.GetStatValue("Willpower");

            var r = StartingBuildService.Apply(p, _h.Factory, Build(id));

            Assert.IsTrue(r.Success, string.Join("; ", r.Errors));
            var a = Build(id).Attributes;
            Assert.AreEqual(a.Strength, p.GetStatValue("Strength"));
            Assert.AreEqual(a.Agility, p.GetStatValue("Agility"));
            Assert.AreEqual(a.Toughness, p.GetStatValue("Toughness"));
            Assert.AreEqual(a.Ego, p.GetStatValue("Ego"));
            Assert.AreEqual(hp, p.GetStatValue("Hitpoints"), "HP model is untouched");
            Assert.AreEqual(intel, p.GetStatValue("Intelligence"));
            Assert.AreEqual(will, p.GetStatValue("Willpower"));
        }

        [TestCase("duelist", "Dagger")] [TestCase("breaker", "Cudgel")]
        [TestCase("stormcaller", "Dagger")] [TestCase("bombardier", "Dagger")]
        public void Apply_PutsTheWeaponInThePrimaryHand(string id, string weapon)
        {
            var p = FreshPlayer();

            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, Build(id)).Success);

            var primary = StartingBuildService.PrimaryHandWeapon(p);
            Assert.IsNotNull(primary, "the primary hand holds a weapon, not a fist");
            Assert.AreEqual(weapon, primary.BlueprintName);
        }

        [Test]
        public void BuildWithABucklerFirst_PutsTheFistInTheHandThatSwings()
        {
            // Pins the mechanism the rest of the file protects against: if the
            // buckler goes on first, the dagger is NOT the primary weapon.
            var p = FreshPlayer();
            var inv = p.GetPart<InventoryPart>();
            var buckler = _h.Factory.CreateEntity("Buckler"); inv.AddObject(buckler);
            var dagger = _h.Factory.CreateEntity("Dagger"); inv.AddObject(dagger);
            Assert.IsTrue(InventorySystem.Equip(p, buckler));
            Assert.IsTrue(InventorySystem.Equip(p, dagger));

            var primary = StartingBuildService.PrimaryHandWeapon(p);

            Assert.AreNotEqual("Dagger", primary?.BlueprintName,
                "the weapon was demoted to the off-hand; the natural fist swings first");
        }

        [TestCase("duelist")] [TestCase("breaker")] [TestCase("stormcaller")] [TestCase("bombardier")]
        public void Apply_GrantsSkillsInOrder_AndTheActivesLandOnTheHotbarInThatOrder(string id)
        {
            var p = FreshPlayer();
            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, Build(id)).Success);

            var skills = p.GetPart<SkillsPart>();
            var abilities = p.GetPart<ActivatedAbilitiesPart>();
            foreach (var s in Build(id).Skills)
                Assert.IsTrue(skills.HasSkill(s), s);

            // Hotbar slots fill 0,1,2... in grant order; passives take none.
            int slot = 0;
            foreach (var s in Build(id).Skills)
            {
                var skill = skills.SkillList.First(x => x.GetType().Name == s);
                if (skill.ActivatedAbilityID == System.Guid.Empty) continue;
                Assert.AreEqual(slot, abilities.GetSlotForAbility(skill.ActivatedAbilityID), id + " " + s);
                slot++;
            }
            Assert.LessOrEqual(slot, ActivatedAbilitiesPart.SlotCount);
            Assert.Greater(slot, 0, "every build has at least one button to press");
        }

        [Test]
        public void Apply_Duelist_PackAndWornGear()
        {
            var p = FreshPlayer();
            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, Build("duelist")).Success);

            var inv = p.GetPart<InventoryPart>();
            var worn = inv.GetAllEquipped().Select(e => e.BlueprintName).ToList();
            CollectionAssert.IsSupersetOf(worn, new[] { "Dagger", "Buckler", "Cloak" });
            Assert.AreEqual(2, StackOf(inv, "HealingTonic"));
            Assert.AreEqual(2, StackOf(inv, "DriedMeat"));
        }

        [Test]
        public void Apply_Bombardier_CarriesTheThreeThrowables_AndTheyAreThrowable()
        {
            var p = FreshPlayer();
            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, Build("bombardier")).Success);

            var inv = p.GetPart<InventoryPart>();
            foreach (var name in new[] { "PoisonGasGrenade", "LightningTonic", "FrostTonic" })
            {
                var item = inv.Objects.FirstOrDefault(o => o.BlueprintName == name);
                Assert.IsNotNull(item, name);
                Assert.IsTrue(HandlingService.IsThrowable(item), name + " must be throwable to be a bomb");
            }
        }

        [Test]
        public void Apply_RecordsTheBuildOnThePlayer()
        {
            var p = FreshPlayer();
            Assert.IsNull(p.GetProperty("StartingBuild"));

            StartingBuildService.Apply(p, _h.Factory, Build("breaker"));

            Assert.AreEqual("breaker", p.GetProperty("StartingBuild"));
        }

        [Test]
        public void Apply_EmitsAnAppliedRecord_WithWhatWasGranted()
        {
            var p = FreshPlayer();

            StartingBuildService.Apply(p, _h.Factory, Build("stormcaller"));

            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "build", Kind = "Applied", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"id\":\"stormcaller\"", recs[0].PayloadJson);
            StringAssert.Contains("Galvanism_ArcBolt", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════
        // Classic stays, and stops punching with fists
        // ════════════════════════════════════════════════════════

        [Test]
        public void Classic_GrantsTodaysKit_AndSixSpells()
        {
            var p = FreshPlayer();

            var r = StartingBuildService.Apply(p, _h.Factory, Build("classic"));

            Assert.IsTrue(r.Success, string.Join("; ", r.Errors));
            Assert.IsEmpty(StartingSpellKit.MissingFrom(p), "the six starting spells");
            var inv = p.GetPart<InventoryPart>();
            Assert.AreEqual(2, StackOf(inv, "HealingTonic"));
            Assert.AreEqual(2, StackOf(inv, "DriedMeat"));
            Assert.AreEqual(18, p.GetStatValue("Strength"), "attributes unchanged");
            Assert.AreEqual(16, p.GetStatValue("Ego"));
        }

        [Test]
        public void Classic_NowEquipsItsDagger()
        {
            // RED pre-fix: the dagger was only ever added to the pack, so a
            // fresh character punched (2.1 per swing vs 3.9 with the dagger).
            var p = FreshPlayer();

            StartingBuildService.Apply(p, _h.Factory, Build("classic"));

            Assert.AreEqual("Dagger", StartingBuildService.PrimaryHandWeapon(p)?.BlueprintName);
        }

        [Test]
        public void ClassicFallback_WorksEvenWhenNoBuildsAreLoaded()
        {
            // The fallback exists for the day the builds file is missing or corrupt.
            StartingBuildRegistry.ResetForTests();
            var p = FreshPlayer();

            var r = StartingBuildService.ApplyClassicFallback(p, _h.Factory);

            Assert.IsTrue(r.Success, string.Join("; ", r.Errors));
            Assert.AreEqual("Dagger", StartingBuildService.PrimaryHandWeapon(p)?.BlueprintName);
            Assert.IsEmpty(StartingSpellKit.MissingFrom(p));
            Assert.AreEqual("classic", p.GetProperty("StartingBuild"));
        }

        [Test]
        public void ClassicFallback_LeavesAnAlreadyBuiltPlayerAlone()
        {
            var p = FreshPlayer();
            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, Build("bombardier")).Success);
            int skillsBefore = p.GetPart<SkillsPart>().SkillList.Count;

            var r = StartingBuildService.ApplyClassicFallback(p, _h.Factory);

            Assert.IsFalse(r.Success, "refused: it already has a start");
            Assert.AreEqual("bombardier", p.GetProperty("StartingBuild"));
            Assert.AreEqual(skillsBefore, p.GetPart<SkillsPart>().SkillList.Count, "no six spells stacked on top");
        }

        [Test]
        public void TheLegacyLoadoutContract_IsStillThreeEntries()
        {
            // Counter-check: Classic reuses NewGameLoadout rather than copying it.
            Assert.AreEqual(3, NewGameLoadout.Items.Length);
            Assert.AreEqual("Dagger", NewGameLoadout.Items[0].Blueprint);
        }

        // ════════════════════════════════════════════════════════
        // Failure leaves nothing half-built
        // ════════════════════════════════════════════════════════

        [Test]
        public void UnknownSkill_RefusesTheWholeBuild_AndTouchesNothing()
        {
            var p = FreshPlayer();
            var bad = Clone(Build("duelist"));
            bad.Skills.Add("Pyromancy_DoesNotExist");
            int strength = p.GetStatValue("Strength");
            int packBefore = p.GetPart<InventoryPart>().Objects.Count;

            var r = StartingBuildService.Apply(p, _h.Factory, bad);

            Assert.IsFalse(r.Success);
            StringAssert.Contains("Pyromancy_DoesNotExist", string.Join(";", r.Errors));
            Assert.AreEqual(strength, p.GetStatValue("Strength"), "no attribute was changed");
            Assert.AreEqual(packBefore, p.GetPart<InventoryPart>().Objects.Count, "no item was granted");
            Assert.AreEqual(0, p.GetPart<SkillsPart>().SkillList.Count, "no skill was granted");
            Assert.IsNull(p.GetProperty("StartingBuild"));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "build", Kind = "Rejected", Limit = 5 }).Records.Count);
        }

        [Test]
        public void UnknownBlueprint_RefusesTheWholeBuild_AndTouchesNothing()
        {
            var p = FreshPlayer();
            var bad = Clone(Build("breaker"));
            bad.Items.Add(new StartingBuildItem { Blueprint = "NoSuchItem", Count = 1 });

            var r = StartingBuildService.Apply(p, _h.Factory, bad);

            Assert.IsFalse(r.Success);
            StringAssert.Contains("NoSuchItem", string.Join(";", r.Errors));
            Assert.AreEqual(0, p.GetPart<SkillsPart>().SkillList.Count);
            Assert.AreEqual(18, p.GetStatValue("Strength"));
        }

        [Test]
        public void ApplyingASecondBuild_IsRefused_NoDoubleGrants()
        {
            var p = FreshPlayer();
            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, Build("duelist")).Success);
            int tonics = StackOf(p.GetPart<InventoryPart>(), "HealingTonic");

            var r = StartingBuildService.Apply(p, _h.Factory, Build("breaker"));

            Assert.IsFalse(r.Success);
            Assert.AreEqual("duelist", p.GetProperty("StartingBuild"));
            Assert.AreEqual(tonics, StackOf(p.GetPart<InventoryPart>(), "HealingTonic"));
        }

        [Test]
        public void NullArguments_AreRefusedNotThrown()
        {
            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(StartingBuildService.Apply(null, _h.Factory, Build("duelist")).Success);
                Assert.IsFalse(StartingBuildService.Apply(FreshPlayer(), null, Build("duelist")).Success);
                Assert.IsFalse(StartingBuildService.Apply(FreshPlayer(), _h.Factory, null).Success);
            });
        }

        // ════════════════════════════════════════════════════════
        // helpers
        // ════════════════════════════════════════════════════════

        private static int StackOf(InventoryPart inv, string blueprint)
        {
            int n = 0;
            foreach (var o in inv.Objects)
                if (o.BlueprintName == blueprint) n += o.GetPart<StackerPart>()?.StackCount ?? 1;
            return n;
        }

        private static StartingBuildDef Clone(StartingBuildDef d) =>
            JsonUtility.FromJson<StartingBuildDef>(JsonUtility.ToJson(d));
    }
}
