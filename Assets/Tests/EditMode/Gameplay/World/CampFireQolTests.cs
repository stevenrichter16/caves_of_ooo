using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Quality-of-life verbs and labels for the campfire family (Docs/MACHINE-INVENTED-USES.md section 3e): feed to full and stoke in one press, hotkeys, the time of day a rest runs to,
    /// what a rest heals, strikes shown wherever the striker is, a hint when the flint is worn, and a warning before lighting a slick beside your camp.
    /// </summary>
    public sealed class CampFireQolTests : FiftyWorldFixture
    {
        const string Oil = "oil";

        [SetUp] public void SetupQol()
        {
            Diag.ResetAll(); MessageLog.Clear(); TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }
        [TearDown] public void TearDownQol() { TileReactionSystem.ResetForTests(); }

        Entity Hearth(float fuel = 5f, float temperature = 25f) { var h = Place("CampHearth", 11, 10); h.GetPart<PhysicsPart>().Takeable = false; h.GetPart<FuelPart>().FuelMass = fuel; h.GetPart<ThermalPart>().Temperature = temperature; return h; }
        Entity Striker(int strikes = 20) { var s = Carry("FlintStriker"); s.GetPart<FlintStrikerPart>().Strikes = strikes; return s; }
        void Timber(int n) { for (int i = 0; i < n; i++) Carry("SalvagedTimber"); }
        float Fuel(Entity h) => h.GetPart<FuelPart>().FuelMass;
        InventoryAction Row(Entity h, string name) => Actions(h).SingleOrDefault(a => a.Name == name);

        // ── hotkeys ──

        [Test] public void TheHearthRowsCarryHotkeys()
        {
            var h = Hearth(5); Timber(3); Striker();
            Assert.AreEqual('f', Row(h, "FeedCookingFire").Key); Assert.AreEqual('l', Row(h, "RelightCookingFire").Key); Assert.AreEqual('u', Row(h, "FeedCookingFireFull").Key); Assert.AreEqual('t', Row(h, "StokeCookingFire").Key);
        }

        // ── feed to full ──

        [Test] public void FeedToFullFeedsAsManyTimberAsFitInOneAction_AndTheRowSaysHowMany()
        {
            var h = Hearth(5); Timber(6); var row = Row(h, "FeedCookingFireFull"); StringAssert.Contains("3 timber", row.Display); StringAssert.Contains("+25", row.Display);
            Assert.True(Act(h, "FeedCookingFireFull"), string.Join("|", MessageLog.GetRecent(2))); Assert.AreEqual(30f, Fuel(h), 0.001f); Assert.AreEqual(3, Count("SalvagedTimber"));
        }

        [Test] public void FeedToFullTakesExactlyWhatFitsWhenItFitsWhole()
        {
            var h = Hearth(10); Timber(6); StringAssert.Contains("2 timber", Row(h, "FeedCookingFireFull").Display); Assert.True(Act(h, "FeedCookingFireFull")); Assert.AreEqual(30f, Fuel(h), 0.001f); Assert.AreEqual(4, Count("SalvagedTimber"));
        }

        [Test] public void FeedToFullIsNotOfferedForASingleTimberOrAFullFire_Counters()
        {
            var h = Hearth(25); Timber(6); Assert.IsNull(Row(h, "FeedCookingFireFull"), "room for one timber: the ordinary feed row is enough"); Assert.NotNull(Row(h, "FeedCookingFire"));
            h.GetPart<FuelPart>().FuelMass = 30; Assert.IsNull(Row(h, "FeedCookingFireFull")); Assert.IsNull(Row(h, "FeedCookingFire"));
            var low = Hearth(5); Zone.RemoveEntity(h); Zone.RemoveEntity(low); var again = Hearth(5); while (Count("SalvagedTimber") > 0) Pack.RemoveObject(Pack.Objects.First(o => o.BlueprintName == "SalvagedTimber")); Timber(1); Assert.IsNull(Row(again, "FeedCookingFireFull"), "one timber carried");
        }

        [Test] public void FeedToFullGivesTheTimberBackWhenAnOuterStepFails()
        {
            var h = Hearth(5); Timber(6); FailAfter(); Assert.False(Act(h, "FeedCookingFireFull")); Assert.AreEqual(5f, Fuel(h), 0.001f); Assert.AreEqual(6, Count("SalvagedTimber"));
        }

        [Test] public void FeedToFullWithoutTimberOrWithAFullFireIsRefused_Forged()
        {
            var h = Hearth(5); Assert.False(Act(h, "FeedCookingFireFull")); Assert.AreEqual(5f, Fuel(h), 0.001f); Timber(3); h.GetPart<FuelPart>().FuelMass = 30; Assert.False(Act(h, "FeedCookingFireFull")); Assert.AreEqual(3, Count("SalvagedTimber"));
        }

        // ── stoke: feed and light in one ──

        [Test] public void StokeFeedsAndLightsAColdEmptyHearthInOneActionAndLightsItAtOnce()
        {
            var h = Hearth(0); Timber(2); var s = Striker(); var row = Row(h, "StokeCookingFire"); StringAssert.Contains("2 timber", row.Display); StringAssert.Contains("1 strike", row.Display);
            Assert.True(Act(h, "StokeCookingFire"), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(20f, Fuel(h), 0.001f); Assert.AreEqual(450f, h.GetPart<ThermalPart>().Temperature, 0.001f);
            Assert.AreEqual(19, s.GetPart<FlintStrikerPart>().Strikes); Assert.AreEqual(0, Count("SalvagedTimber")); Assert.True(h.GetPart<LightSourcePart>().Enabled);
        }

        [Test] public void StokeIsOnlyOfferedWhenThereIsBothTimberToFeedAndAFlameToLightWith_Counters()
        {
            var h = Hearth(0); Striker(); Assert.IsNull(Row(h, "StokeCookingFire"), "no timber"); Timber(2); Assert.NotNull(Row(h, "StokeCookingFire"));
            var noFlame = Hearth(0); Zone.RemoveEntity(h); Zone.RemoveEntity(noFlame); var cold = Hearth(0); Pack.RemoveObject(Pack.Objects.First(o => o.BlueprintName == "FlintStriker")); Assert.IsNull(Row(cold, "StokeCookingFire"), "no flame");
        }

        [Test] public void StokeGivesEverythingBackWhenAnOuterStepFails()
        {
            var h = Hearth(0); Timber(2); var s = Striker(); FailAfter(); Assert.False(Act(h, "StokeCookingFire"));
            Assert.AreEqual(0f, Fuel(h), 0.001f); Assert.AreEqual(25f, h.GetPart<ThermalPart>().Temperature, 0.001f); Assert.AreEqual(20, s.GetPart<FlintStrikerPart>().Strikes); Assert.AreEqual(2, Count("SalvagedTimber"));
        }

        // ── name the time, preview the rest ──

        [Test] public void RestRowsNameTheTimeOfDayYouWakeIn()
        {
            var h = Hearth(30, 450); Assert.AreEqual(0, Clock.TickCount); StringAssert.Contains("until Height", Row(h, "HearthRestBand").Display);
            Clock.AdvanceClock(600); h.GetPart<FuelPart>().FuelMass = 30; h.GetPart<ThermalPart>().Temperature = 450; StringAssert.Contains("until Dark", Row(h, "HearthRestBand").Display, "tick 600 is Dusk; the next band is Dark");
            Clock.AdvanceClock(300); h.GetPart<ThermalPart>().Temperature = 450; StringAssert.Contains("until Dawn", Row(h, "HearthRestBand").Display);
        }

        [Test] public void RestRowsSayWhatTheRestWillHeal_AndSayNothingAtFullHealth()
        {
            var h = Hearth(30, 450); Actor.GetStat("Hitpoints").BaseValue = 3; int max = Actor.GetStat("Hitpoints").Max; StringAssert.Contains("heals " + (max - 3), Row(h, "HearthRest").Display);
            StringAssert.Contains("heals " + (max - 3), Row(h, "HearthRestBand").Display); Actor.GetStat("Hitpoints").BaseValue = max; StringAssert.DoesNotContain("heals", Row(h, "HearthRest").Display);
        }

        // ── strikes wherever the striker shows ──

        [Test] public void TheStrikerNameShowsItsStrikesAndNothingElseGainsASuffix()
        {
            var s = Striker(17); Assert.AreEqual("flint striker [17]", s.GetDisplayName()); s.GetPart<FlintStrikerPart>().Strikes = 0; Assert.AreEqual("flint striker [0]", s.GetDisplayName());
            Assert.AreEqual("torch", Factory.CreateEntity("Torch").GetDisplayName()); Assert.AreEqual("camp hearth", Factory.CreateEntity("CampHearth").GetDisplayName());
        }

        [Test] public void TheRelightAndCellRowsSayHowManyStrikesAreLeft()
        {
            var h = Hearth(10); Striker(17); StringAssert.Contains("1 strike, 17 left", Row(h, "RelightCookingFire").Display);
            Zone.RemoveEntity(h); var coals = Place("SpreadCookingCoals", 9, 10); coals.GetPart<FuelPart>().FuelMass = 10; coals.GetPart<ThermalPart>().Temperature = 500;
            Place("Grass", 11, 10); Zone.TileState.WriteCoating(11, 10, Oil, 5); var rows = new System.Collections.Generic.List<InventoryAction>(); WorldInteractionSystem.AppendIgnitionActions(rows, Zone.GetCell(11, 10), Actor);
            StringAssert.Contains("1 strike, 17 left", rows.Single().Display);
        }

        [Test] public void AWornFlintSaysToKnapItWhereAFlameWasWanted()
        {
            var h = Hearth(10); Striker(0); Carry("IronSpikeComponent"); StringAssert.Contains("knap", Row(h, "RelightCookingFire").DisabledReason.ToLowerInvariant());
            var torch = Place("Torch", 11, 11); Assert.True(Act(torch, "ExtinguishTorch")); Assert.False(Act(torch, "LightTorch")); StringAssert.Contains("knap", string.Join("|", MessageLog.GetRecent(4)).ToLowerInvariant());
        }

        [Test] public void NoKnapHintWithoutAWornStriker_Counter()
        {
            var h = Hearth(10); Striker(5); var row = Row(h, "RelightCookingFire"); Assert.False(row.IsDisabled, "a usable striker relights"); var plain = Hearth(10); Zone.RemoveEntity(plain);
            Pack.RemoveObject(Pack.Objects.First(o => o.BlueprintName == "FlintStriker")); StringAssert.DoesNotContain("knap", Row(h, "RelightCookingFire").DisabledReason.ToLowerInvariant());
        }

        // ── small text saying what the highlighted row does ──

        [Test] public void TheStokeRowSaysWhatItDoes_AndTheStrikeItCosts()
        {
            var h = Hearth(0); Timber(2); var s = Striker(); var stoke = Row(h, "StokeCookingFire");
            StringAssert.Contains("adds all the timber", stoke.Hint.ToLowerInvariant()); StringAssert.Contains("lights it", stoke.Hint); StringAssert.Contains("one strike", stoke.Hint);
            Pack.RemoveObject(s); Zone.RemoveEntity(h); var torchHearth = Hearth(0); var torch = Carry("Torch"); Assert.True(InventorySystem.Equip(Actor, torch));
            StringAssert.DoesNotContain("strike", Row(torchHearth, "StokeCookingFire").Hint, "a torch costs no strike, so the hint does not mention one");
        }

        [Test] public void TheOtherFireRowsAndTheRestRowsDescribeThemselvesToo()
        {
            var h = Hearth(5); Timber(3); Striker(); foreach (var name in new[] { "FeedCookingFire", "FeedCookingFireFull", "RelightCookingFire" }) Assert.IsNotEmpty(Row(h, name).Hint, name);
            var lit = Hearth(30, 450); Zone.RemoveEntity(h); foreach (var name in new[] { "HearthRest", "HearthRestBand" }) Assert.IsNotEmpty(Row(lit, name).Hint, name);
            StringAssert.Contains("next time of day", Row(lit, "HearthRestBand").Hint);
        }

        [Test] public void ARowWithNothingToAddHasNoHint_Counter()
        {
            var gear = Carry("Dagger"); Assert.True(gear.ApplyEffect(new WetEffect(1f))); var lit = Hearth(10, 450);
            var dry = Actions(lit).SingleOrDefault(a => a.Name == "DryGear"); Assert.NotNull(dry); Assert.IsEmpty(dry.Hint, "a row the label already explains stays bare");
        }

        [Test] public void HintsWrapAtWordBoundariesWithinTheWidthAndEndWithATildeWhenCut()
        {
            var lines = WorldActionMenuUI.WrapHint("Adds all the timber the fire will take, then lights it from a flame you carry, in one action.", 30, 3);
            CollectionAssert.AreEqual(new[] { "Adds all the timber the fire", "will take, then lights it from", "a flame you carry, in one~" }, lines); Assert.True(lines.All(l => l.Length <= 30), string.Join(" / ", lines));
            var fits = WorldActionMenuUI.WrapHint("Adds one timber.", 30, 3); Assert.AreEqual(1, fits.Count); Assert.AreEqual("Adds one timber.", fits[0], "short text is not cut or marked");
        }

        [Test] public void WrappingToleratesEmptyBlankAndLongUnbrokenText_Adversarial()
        {
            Assert.IsEmpty(WorldActionMenuUI.WrapHint(null, 30, 3)); Assert.IsEmpty(WorldActionMenuUI.WrapHint("   ", 30, 3)); Assert.IsEmpty(WorldActionMenuUI.WrapHint("text", 2, 3)); Assert.IsEmpty(WorldActionMenuUI.WrapHint("text", 30, 0));
            var longWord = WorldActionMenuUI.WrapHint(new string('x', 200), 20, 3); Assert.AreEqual(3, longWord.Count); Assert.True(longWord.All(l => l.Length <= 20)); Assert.True(longWord[2].EndsWith("~"));
            var exact = WorldActionMenuUI.WrapHint("aaaa bbbb", 9, 1); Assert.AreEqual("aaaa bbbb", exact.Single());
        }

        [Test] public void TheMenuReservesHintRowsOnlyWhenARowHasAHint_AndShowsTheHighlightedRowsHint()
        {
            var bare = new System.Collections.Generic.List<InventoryAction> { new InventoryAction("A", "alpha", "A"), new InventoryAction("B", "beta", "B") };
            Assert.AreEqual(0, WorldActionMenuUI.HintRowsFor(bare, 48), "no hints: the menu keeps its old shape");
            var h = Hearth(0); Timber(2); Striker(); var rows = Actions(h); var stoke = rows.Single(a => a.Name == "StokeCookingFire");
            Assert.AreEqual(3, WorldActionMenuUI.HintRowsFor(rows, 48), "the stoke hint needs the full three lines at the narrowest menu");
            var go = new GameObject("menu"); try
            {
                var menu = go.AddComponent<WorldActionMenuUI>(); menu.Open(Actor, h, Zone.GetEntityCell(h), rows.ToList(), Zone);
                Assert.AreEqual(3, menu.HintRows); menu.RestoreHighlight(stoke); Assert.AreEqual(stoke.Command, menu.HighlightedAction.Command); StringAssert.Contains("lights it", menu.HighlightedHint);
                var bareRow = rows.First(a => string.IsNullOrEmpty(a.Hint)); menu.RestoreHighlight(bareRow); Assert.AreEqual("", menu.HighlightedHint);
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ── warn before lighting a slick beside your camp ──

        [Test] public void TheLightRowWarnsWhenACampKitStandsBesideTheSlick()
        {
            Striker(); Place("Grass", 11, 10); Zone.TileState.WriteCoating(11, 10, Oil, 5); var rows = new System.Collections.Generic.List<InventoryAction>();
            WorldInteractionSystem.AppendIgnitionActions(rows, Zone.GetCell(11, 10), Actor); StringAssert.DoesNotContain("next to", rows.Single().Display);
            Place("CampBox", 12, 10); rows.Clear(); WorldInteractionSystem.AppendIgnitionActions(rows, Zone.GetCell(11, 10), Actor); StringAssert.Contains("camp box", rows.Single().Display); StringAssert.Contains("next to", rows.Single().Display);
        }
    }
}
