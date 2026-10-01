using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The pick-a-build screen's logic, kept in Gameplay so it is testable
    /// without Unity: menu navigation, the card the player reads, and the
    /// policy for WHEN the prompt appears (it must never stall a scripted
    /// native audit, a scenario launch or DevMode).
    /// </summary>
    public class StartingBuildMenuTests
    {
        private static ScenarioTestHarness _h;

        [OneTimeSetUp] public void Up() { _h = new ScenarioTestHarness(); }
        [OneTimeTearDown] public void Down() { _h?.Dispose(); _h = null; }

        [SetUp]
        public void Setup()
        {
            StartingBuildRegistry.ResetForTests();
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Builds/StartingBuilds.json")));
        }

        [TearDown] public void TearDown() { StartingBuildRegistry.ResetForTests(); StartingBuildSelection.Enabled = true; }

        private static StartingBuildMenuModel Menu() => new StartingBuildMenuModel(StartingBuildRegistry.All);

        // ── navigation ──────────────────────────────────────────

        [Test]
        public void Opens_OnTheFirstBuild_NotClassic()
        {
            Assert.AreEqual("duelist", Menu().Selected.Id);
            Assert.AreEqual(0, Menu().SelectedIndex);
        }

        [Test]
        public void Move_WrapsInBothDirections()
        {
            var m = Menu();
            m.Move(-1);
            Assert.AreEqual("classic", m.Selected.Id, "up from the top wraps to the bottom");
            m.Move(+1);
            Assert.AreEqual("duelist", m.Selected.Id, "down from the bottom wraps to the top");
            m.Move(+2);
            Assert.AreEqual("stormcaller", m.Selected.Id);
        }

        [TestCase(0, "duelist")] [TestCase(3, "bombardier")] [TestCase(4, "classic")]
        public void Select_JumpsToAnIndex(int index, string id)
        {
            var m = Menu();
            m.Select(index);
            Assert.AreEqual(id, m.Selected.Id);
        }

        [TestCase(-1)] [TestCase(5)] [TestCase(99)]
        public void Select_IgnoresAnOutOfRangeIndex(int index)
        {
            var m = Menu();
            m.Select(2);
            m.Select(index);
            Assert.AreEqual("stormcaller", m.Selected.Id, "the selection did not move");
        }

        [Test]
        public void EmptyMenu_IsSafe()
        {
            var m = new StartingBuildMenuModel(new StartingBuildDef[0]);
            Assert.DoesNotThrow(() => { m.Move(1); m.Select(0); });
            Assert.IsNull(m.Selected);
            Assert.IsEmpty(m.CardLines(_h.Factory));
        }

        // ── the card ────────────────────────────────────────────

        [Test]
        public void Card_ShowsTheDerivedNumbersTheDesignPromised()
        {
            // Duelist: DV 6 + Agi modifier 3 + Cloak 1 + Buckler 1 = 11.
            var m = Menu(); m.Select(0);
            string text = string.Join("\n", m.CardLines(_h.Factory));

            StringAssert.Contains("DV 11", text);
            StringAssert.Contains("HP 40", text);
            StringAssert.Contains("Str 16", text);
            StringAssert.Contains("Agi 22", text);
        }

        [TestCase("duelist", 11)] [TestCase("breaker", 7)] [TestCase("stormcaller", 9)] [TestCase("bombardier", 8)]
        public void DerivedDv_MatchesTheEngineFormula(string id, int dv)
        {
            Assert.AreEqual(dv, StartingBuildCard.DerivedDv(StartingBuildRegistry.Get(id), _h.Factory));
        }

        [Test]
        public void DerivedDv_AgreesWithTheRealPlayer_AfterApply()
        {
            // Counter-check: the card's number must be what combat will use.
            foreach (var id in new[] { "duelist", "breaker", "stormcaller", "bombardier" })
            {
                var p = _h.CreateContext(playerBlueprint: "Player").PlayerEntity;
                Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, StartingBuildRegistry.Get(id)).Success);

                Assert.AreEqual(CombatSystem.GetDV(p), StartingBuildCard.DerivedDv(StartingBuildRegistry.Get(id), _h.Factory), id);
            }
        }

        [Test]
        public void Card_NamesTheWeakness_AndEverySkillReadably()
        {
            var m = Menu(); m.Select(1);   // breaker
            // Wrapping may split a phrase across two lines; read it as the player does.
            string text = System.Text.RegularExpressions.Regex.Replace(string.Join("\n", m.CardLines(_h.Factory)), "\\s+", " ");

            StringAssert.Contains("Weakness", text);
            StringAssert.Contains("you cannot choose who gets stunned", text);
            foreach (var label in new[] { "Cudgel", "Expertise", "Conk", "Slam", "Ground Pound", "Shattering Blows" })
                StringAssert.Contains(label, text);
            StringAssert.DoesNotContain("Cudgel_Conk", text, "class names are for code, not for the player");
        }

        [Test]
        public void Card_ListsWornGearSeparatelyFromThePack()
        {
            var m = Menu(); m.Select(3);   // bombardier
            string text = string.Join("\n", m.CardLines(_h.Factory));

            StringAssert.Contains("Wearing", text);
            StringAssert.Contains("Carrying", text);
            StringAssert.Contains("poison gas grenade", text.ToLowerInvariant());
        }

        [Test]
        public void ClassicCard_IsDerivedFromTheLegacyKit_NotCopied()
        {
            var m = Menu(); m.Select(4);
            string text = string.Join("\n", m.CardLines(_h.Factory));

            foreach (var e in NewGameLoadout.Items)
                StringAssert.Contains(e.Blueprint.ToLowerInvariant().Replace("healingtonic", "healing tonic").Replace("driedmeat", "dried meat"),
                    text.ToLowerInvariant());
            StringAssert.Contains("Ember Spit", text, "the six spells appear by name");
            StringAssert.Contains("Calm", text);
        }

        [TestCase("Pyromancy_Kindle", "Kindle")]
        [TestCase("Cudgel_GroundPound", "Ground Pound")]
        [TestCase("ShortBladesSkill", "Short Blades")]
        [TestCase("SpellcraftSkill", "Spellcraft")]
        [TestCase("Axe_HookAndDrag", "Hook And Drag")]
        [TestCase("Spellcraft_Calm", "Calm")]
        public void SkillLabel_TurnsAClassNameIntoWords(string cls, string label)
        {
            Assert.AreEqual(label, StartingBuildCard.SkillLabel(cls));
        }

        [TestCase(null)] [TestCase("")]
        public void SkillLabel_OfNothing_IsEmpty(string cls)
        {
            Assert.AreEqual("", StartingBuildCard.SkillLabel(cls));
        }

        [Test]
        public void CardLines_FitThePopup()
        {
            // The popup is 80 columns wide; a card line wider than the card
            // column would be clipped, so every line is wrapped to the budget.
            foreach (var b in StartingBuildRegistry.All)
            {
                var m = new StartingBuildMenuModel(StartingBuildRegistry.All);
                m.Select(StartingBuildRegistry.All.ToList().IndexOf(b));
                foreach (var line in m.CardLines(_h.Factory))
                    Assert.LessOrEqual(line.Length, StartingBuildCard.CardWidth, b.Id + ": '" + line + "'");
            }
        }

        // ── when the prompt appears ─────────────────────────────

        [Test]
        public void Prompts_InOrdinaryNewGames()
        {
            Assert.IsTrue(StartingBuildSelection.ShouldPrompt(devMode: false, scenarioPending: false, auditSaveRoot: false, batchMode: false));
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        public void NeverPrompts_ForDevModeScenariosAuditsOrBatchRuns(bool dev, bool scenario, bool audit, bool batch)
        {
            // Each of these runs scripted input; a modal picker would stall it.
            Assert.IsFalse(StartingBuildSelection.ShouldPrompt(dev, scenario, audit, batch));
        }

        [Test]
        public void ADisabledSwitch_SuppressesThePrompt_AndOffByDefaultIsNotAThing()
        {
            Assert.IsTrue(StartingBuildSelection.Enabled, "on by default");
            StartingBuildSelection.Enabled = false;
            Assert.IsFalse(StartingBuildSelection.ShouldPrompt(false, false, false, false));
        }

        [Test]
        public void NoBuildsLoaded_MeansNoPrompt()
        {
            // Counter-check: a missing or corrupt builds file must fall back to the
            // ordinary Classic start, not open an empty modal nobody can dismiss.
            StartingBuildRegistry.ResetForTests();
            Assert.IsFalse(StartingBuildSelection.ShouldPrompt(false, false, false, false));
        }
    }
}
