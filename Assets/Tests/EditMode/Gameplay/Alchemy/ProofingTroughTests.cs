using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The trough as the permanent tier (Docs/MACHINE-PROOFING.md). It no longer treats a garment for a while: that is the bottle's work now. It gives a garment one lasting proofing against one element,
    /// for four units of that element's proofing and two fire clay (the still's product, so the chain mire, fire, clay, trough stays alive): +25, below the coat's +35 and below a purchased +50 garment, and
    /// stacking with both toward immunity. Any armor, worn or not; one lasting proofing a garment; one of the garment's two treatment slots.
    /// </summary>
    public sealed class ProofingTroughTests : FiftyWorldFixture
    {
        [SetUp] public void SetupTrough() { EnhancementFactory.ForceReinitialize(); ProofingRegistry.ResetForTests(); PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); Pack.MaxWeight = 800; }
        [TearDown] public void TearTrough() { PackableFixtureRegistry.ResetForTests(); }

        /// <summary>A trough standing the way a real set-down leaves it, next to the actor.</summary>
        Entity StandingTrough()
        {
            var trough = Factory.CreateEntity("CampTrough");
            trough.AddPart(new InstallationMarkerPart { InstallationId = "player:" + trough.ID, Origin = HeldFixtureActions.PlayerOrigin, RetainZone = true });
            var inst = trough.GetPart<InstallablePart>() ?? new InstallablePart(); inst.Mode = InstallablePart.Portable; inst.State = InstallablePart.Installed; if (trough.GetPart<InstallablePart>() == null) trough.AddPart(inst);
            var me = Zone.GetEntityPosition(Actor); Assert.True(Zone.AddEntity(trough, me.x + 1, me.y)); Zone.GetCell(me.x + 1, me.y).IsVisible = true; Zone.GetCell(me.x + 1, me.y).Explored = true; return trough;
        }
        InventoryAction[] Rows(Entity trough) => WorldInteractionSystem.GatherActions(trough, Actor).Where(a => a.Name == "ProofGarment").ToArray();
        EnhancementProofed Proofed(Entity garment) => garment.Parts.OfType<EnhancementProofed>().FirstOrDefault();
        int Stat(string name) => Actor.GetStatValue(name, 0);
        void Clay(int n) { for (int i = 0; i < n; i++) Carry("FireClay"); }
        int ClayLeft() => Pack.Objects.Where(o => o.BlueprintName == "FireClay").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        string RowFor(Entity trough, Entity garment, string element) => Rows(trough).Single(r => !r.IsDisabled && r.Command.EndsWith("|" + element, StringComparison.Ordinal) && r.Command.Contains(Uri.EscapeDataString(garment.ID))).Command;

        [TestCase(false)] [TestCase(true)]
        public void LateGarmentAndElementRemainReachableAndPayTheirExactBill(bool shortClay)
        {
            var trough = StandingTrough(); var cold = Flask(4, "cold-proofing");
            var heat = Flask(4, "heat-proofing"); Clay(shortClay ? 1 : 2);
            var garments = new System.Collections.Generic.List<Entity>();
            for (int i = 0; i < 16; i++)
            {
                var g = Factory.CreateEntity("LeatherArmor"); g.RemovePart(g.GetPart<StackerPart>());
                g.GetPart<RenderPart>().DisplayName = "garment " + i.ToString("D2");
                Assert.True(Pack.AddObject(g)); garments.Add(g);
            }
            var rows = Rows(trough); Assert.AreEqual(32, rows.Length);
            var last = garments.Last();
            var selected = rows.Single(r => r.Display.Contains("garment 15") && r.Display.Contains("against heat"));
            Assert.AreEqual(shortClay, selected.IsDisabled);
            Assert.AreEqual(!shortClay, Act(trough, "ProofGarment|" + Uri.EscapeDataString(last.ID) + "|heat"));
            Assert.AreEqual(shortClay ? 4 : 0, heat.GetPart<LiquidVesselPart>().Volume);
            Assert.AreEqual(4, cold.GetPart<LiquidVesselPart>().Volume);
            Assert.AreEqual(shortClay ? 1 : 0, ClayLeft());
            Assert.AreEqual(shortClay ? null : "heat", Proofed(last)?.Kind);
            Assert.True(garments.Take(15).All(g => Proofed(g) == null));
        }

        [Test] public void ACarriedTroughIsAKitAndOffersNothing_Counter()
        {
            var kit = Carry("CampTrough"); Carry("LeatherArmor"); Flask(4, "cold-proofing"); Clay(2); Assert.IsEmpty(Actions(kit).Where(a => a.Name == "ProofGarment"));
        }

        [Test] public void WithNoProofingInAFlaskOneDisabledRowSaysWhatToDo()
        {
            var trough = StandingTrough(); Carry("LeatherArmor"); Clay(2); var rows = Rows(trough); Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); Assert.AreEqual("no-proofing", rows[0].DisabledCode);
            StringAssert.Contains("ress a reagent into a flask", rows[0].DisabledReason); StringAssert.Contains("4", rows[0].DisabledReason); StringAssert.Contains("2 fire clay", rows[0].DisabledReason);
        }

        [Test] public void FourUnitsAndTwoClayGiveTheGarmentALastingProofingThatWorksWornAtOnce()
        {
            var trough = StandingTrough(); var garment = Carry("LeatherArmor"); var flask = Flask(4, "cold-proofing"); Clay(2); Assert.True(InventorySystem.Equip(Actor, garment));
            var row = Rows(trough).Single(); Assert.False(row.IsDisabled); Assert.AreEqual(ActionTime.OneAction, row.Time); StringAssert.Contains("cold", row.Display); StringAssert.Contains("for good", row.Display); StringAssert.Contains("+25", row.Hint); StringAssert.Contains("freezing", row.Hint);
            Assert.True(Act(trough, row.Command), string.Join("|", MessageLog.GetRecent(3)));
            var p = Proofed(garment); Assert.NotNull(p); Assert.AreEqual("cold", p.Kind); Assert.AreEqual(25, p.Amount); Assert.AreEqual(25, Stat("ColdResistance"), "worn: at once");
            Assert.AreEqual(0, flask.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual("", flask.GetPart<LiquidVesselPart>().LiquidId); Assert.AreEqual(0, ClayLeft());
            Assert.True(InventorySystem.UnequipItem(Actor, garment)); Assert.AreEqual(0, Stat("ColdResistance"), "off, exactly reverted"); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.AreEqual(25, Stat("ColdResistance"), "and it never wears out");
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "GarmentProofed", Limit = 5 }).Records; Assert.AreEqual(1, records.Count);
        }

        [Test] public void UnitsMayComeFromSeveralFlasksButOnlyOfTheRightElement()
        {
            var trough = StandingTrough(); var garment = Carry("LeatherArmor"); var wrong = Flask(6, "cold-proofing"); var a = Flask(2, "heat-proofing"); var b = Flask(2, "heat-proofing"); Clay(2);   // the wrong flask comes first in the pack: a payer that ignored the liquid would draw from it
            var rows = Rows(trough); Assert.True(rows.Any(r => !r.IsDisabled && r.Command.EndsWith("|heat")), "two flasks of two make four"); Assert.True(rows.Any(r => !r.IsDisabled && r.Command.EndsWith("|cold")), "and the six cold units pay for their own row");
            Assert.True(Act(trough, RowFor(trough, garment, "heat"))); Assert.AreEqual(0, a.GetPart<LiquidVesselPart>().Volume + b.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual(6, wrong.GetPart<LiquidVesselPart>().Volume, "the cold flask was not touched");
            Assert.AreEqual("heat", Proofed(garment).Kind);
        }

        [Test] public void ShortOfUnitsOrClayIsRefusedWithTheNumbers_AndSpendsNothing()
        {
            var trough = StandingTrough(); var garment = Carry("LeatherArmor"); var flask = Flask(3, "cold-proofing"); Clay(2);
            var row = Rows(trough).Single(); Assert.True(row.IsDisabled); Assert.AreEqual("short-units", row.DisabledCode); StringAssert.Contains("3", row.DisabledReason); StringAssert.Contains("4", row.DisabledReason);
            flask.GetPart<LiquidVesselPart>().Volume = 4; foreach (var c in Pack.Objects.Where(o => o.BlueprintName == "FireClay").ToList()) Pack.RemoveObject(c); Clay(1);
            row = Rows(trough).Single(); Assert.True(row.IsDisabled); Assert.AreEqual("short-clay", row.DisabledCode); StringAssert.Contains("fire clay", row.DisabledReason);
            Assert.False(Act(trough, "ProofGarment|" + Uri.EscapeDataString(garment.ID) + "|cold")); Assert.AreEqual(4, flask.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual(1, ClayLeft()); Assert.Null(Proofed(garment));
        }

        [Test] public void AGarmentHoldsOneLastingProofing_AndAFullOneRefusesAnother()
        {
            var trough = StandingTrough(); var done = Carry("LeatherArmor"); var flask = Flask(8, "cold-proofing"); Clay(4); Assert.True(Act(trough, RowFor(trough, done, "cold")));
            var row = Rows(trough).Single(r => r.DisabledCode == "already-proofed"); StringAssert.Contains("cold", row.DisabledReason);
            Assert.False(Act(trough, "ProofGarment|" + Uri.EscapeDataString(done.ID) + "|cold")); Assert.AreEqual(4, flask.GetPart<LiquidVesselPart>().Volume, "refused: nothing spent");
            var full = Carry("FilterHood"); full.AddPart(new EnhancementLacquered()); full.AddPart(new EnhancementGlowQuartz()); var capped = Rows(trough).Single(r => r.DisabledCode == "slot-cap"); StringAssert.Contains("two treatments", capped.DisabledReason);
        }

        [Test] public void AnyArmorQualifiesAndNothingElseDoes_Counter()
        {
            var trough = StandingTrough(); var mail = Carry("ChainMail"); var dagger = Carry("Dagger"); Flask(4, "acid-proofing"); Clay(2);
            Assert.True(Rows(trough).Any(r => r.Command.Contains(Uri.EscapeDataString(mail.ID)) && !r.IsDisabled), "metal armor takes it"); Assert.False(Rows(trough).Any(r => r.Command.Contains(Uri.EscapeDataString(dagger.ID))), "a weapon does not");
            Assert.False(ItemEnhancing.Apply(Factory.CreateEntity("CaveBear"), "EnhancementProofed"), "a creature, whatever its armor part"); Assert.False(ItemEnhancing.Apply(dagger, "EnhancementProofed"));
        }

        [Test] public void ACoatAndALastingProofingTogetherUseBothSlots_AndAThirdTreatmentIsRefused()
        {
            var trough = StandingTrough(); var garment = Carry("LeatherArmor"); var bottle = Flask(1, "cold-proofing"); var tincture = Flask(4, "cold-proofing"); Clay(2);
            Assert.True(Act(bottle, Actions(bottle).Single(a => a.Name == "CoatGarment" && !a.IsDisabled).Command)); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(garment));
            Assert.True(Act(trough, RowFor(trough, garment, "cold"))); Assert.AreEqual(2, ItemEnhancing.CountEnhancements(garment)); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.AreEqual(60, Stat("ColdResistance"), "35 + 25");
        }

        [Test] public void CoatAndGearAndALastingProofingStackToImmunity_ThatIsWhatItIsFor()
        {
            var trough = StandingTrough(); var body = Carry("LeatherArmor"); var cloak = Carry("ColdwardCloak"); var flask = Flask(4, "cold-proofing"); var bottle = Flask(1, "cold-proofing"); Clay(2);
            Assert.True(Act(trough, RowFor(trough, body, "cold"))); Assert.True(InventorySystem.Equip(Actor, body)); Assert.True(InventorySystem.Equip(Actor, cloak)); Assert.AreEqual(75, Stat("ColdResistance"), "25 + 50");
            Assert.True(Actor.ApplyEffect(new FrozenEffect(0.6f))); Assert.AreEqual(0.15f, Actor.GetEffect<FrozenEffect>().Cold, 1e-3f, "a quarter of the freeze gets through at 75"); Actor.RemoveEffect<FrozenEffect>();
            Assert.True(Act(bottle, Actions(bottle).Single(a => a.Name == "CoatGarment" && !a.IsDisabled && a.Command.Contains(Uri.EscapeDataString(cloak.ID))).Command));
            Assert.AreEqual(110, Stat("ColdResistance"), "25 + 50 + 35 = 110, past the 100 that is immunity"); Assert.False(Actor.ApplyEffect(new FrozenEffect(0.6f)), "frozen cannot take hold"); Assert.Null(Actor.GetEffect<FrozenEffect>());
        }

        [Test] public void AnInterruptedProofingGivesEverythingBack()
        {
            var trough = StandingTrough(); var garment = Carry("LeatherArmor"); var flask = Flask(4, "cold-proofing"); Clay(2); Assert.True(InventorySystem.Equip(Actor, garment)); string command = Rows(trough).Single().Command; FailAfter();
            Assert.False(Act(trough, command)); Assert.AreEqual(4, flask.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual("cold-proofing", flask.GetPart<LiquidVesselPart>().LiquidId); Assert.AreEqual(2, ClayLeft()); Assert.Null(Proofed(garment)); Assert.AreEqual(0, Stat("ColdResistance"));
        }

        [Test] public void TheOldTemporaryRowsAreGone_AndResinAndClayNoLongerTreatAnything_Counter()
        {
            var trough = StandingTrough(); Carry("LeatherArmor"); Carry("PitchpodResin"); Clay(3);
            Assert.IsEmpty(WorldInteractionSystem.GatherActions(trough, Actor).Where(a => a.Name == "Steep" || a.Name == "ClayWash"), "the bottle does the temporary work now");
        }

        [Test] public void ALasting_ProofingSurvivesSaveAndLoad_AndStillWorksWhenWorn()
        {
            var trough = StandingTrough(); var garment = Carry("LeatherArmor"); Flask(4, "electric".Length > 0 ? "shock-proofing" : ""); Clay(2); Assert.True(Act(trough, RowFor(trough, garment, "electric")));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(garment); var p = loaded.GetPart<EnhancementProofed>(); Assert.NotNull(p); Assert.AreEqual("electric", p.Kind); Assert.AreEqual(25, p.Amount); Assert.False(p.AppliedBonus);
            Pack.RemoveObject(garment); Pack.AddObject(loaded); Assert.True(InventorySystem.Equip(Actor, loaded)); Assert.AreEqual(25, Stat("ElectricResistance"));
        }

        [Test] public void TheMachinesScenariosOwnLoadoutCanDoTheWholeColdDemoItsTextPromises()
        {
            var ctx = new CavesOfOoo.Scenarios.ScenarioContext(Zone, Factory, Actor, Clock); new CavesOfOoo.Scenarios.Custom.CampMachinesShowcase().Apply(ctx);
            var trough = StandingTrough(); var empties = Pack.Objects.Where(o => o.BlueprintName == "LiquidFlask" && o.GetPart<LiquidVesselPart>().Volume == 0).ToList(); Assert.AreEqual(3, empties.Count, "three empty flasks");
            var body = Pack.Objects.Single(o => o.BlueprintName == "LeatherArmor"); var cloak = Pack.Objects.Single(o => o.BlueprintName == "ColdwardCloak"); var flask = empties[0];
            // press the lichens (cold:2 each) into one flask, as the text says
            for (int i = 0; i < 3; i++)
            {
                var lichen = Pack.Objects.First(o => o.BlueprintName == "FrostLichen"); var row = Actions(lichen).First(a => a.Name == "PressProofing" && !a.IsDisabled && a.Command.Contains(Uri.EscapeDataString(flask.ID)));
                Assert.True(Act(lichen, row.Command), "press " + i + ": " + string.Join("|", MessageLog.GetRecent(2)));
            }
            Assert.AreEqual(6, flask.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual("cold-proofing", flask.GetPart<LiquidVesselPart>().LiquidId);
            Assert.True(Act(trough, RowFor(trough, body, "cold")), "the lasting proofing: " + string.Join("|", MessageLog.GetRecent(2))); Assert.AreEqual(2, flask.GetPart<LiquidVesselPart>().Volume);
            Assert.True(InventorySystem.Equip(Actor, body)); Assert.True(InventorySystem.Equip(Actor, cloak)); Assert.AreEqual(75, Stat("ColdResistance"), "25 + 50");
            var coat = Actions(flask).Single(a => a.Name == "CoatGarment" && !a.IsDisabled && a.Command.Contains(Uri.EscapeDataString(cloak.ID))); Assert.True(Act(flask, coat.Command));
            Assert.AreEqual(110, Stat("ColdResistance"), "plus a coat: past 100"); Assert.False(Actor.ApplyEffect(new FrozenEffect(0.6f)), "immune to freezing");
        }

        [Test] public void TheProofRowSaysWhatYourResistanceWouldBe()
        {
            var trough = StandingTrough(); var cloak = Carry("ColdwardCloak"); Assert.True(InventorySystem.Equip(Actor, cloak)); var body = Carry("LeatherArmor"); Flask(4, "cold-proofing"); Clay(2);
            StringAssert.Contains("Your cold resistance: 50 → 75 once worn", Rows(trough).Single(r => r.Display.Contains("leather")).Hint);
        }

        [Test] public void TheReadersPointAtWhatYouHaveSeenNearby_AndSayNothingOfTheKindWhenYouHaveSeenNothing_Counter()
        {
            var trough = StandingTrough(); string Read() { var l = new System.Collections.Generic.List<string>(); StateReader.Collect(trough, true, Zone, Zone.GetEntityCell(trough), Actor, l); return string.Join(" ", l); }
            StringAssert.DoesNotContain("You have seen", Read());
            var vent = Place("SteamVent", 20, 10); Zone.GetCell(20, 10).IsVisible = true; Zone.GetCell(20, 10).Explored = true;
            StringAssert.Contains("You have seen heat (steam vent) in this place.", Read());
            var moss = Carry("FireMoss"); Flask(0); StringAssert.Contains("steam vent", Actions(moss).First(a => a.Name == "PressProofing").Hint);
            var coatFlask = Flask(2, "heat-proofing"); Carry("LeatherArmor"); StringAssert.Contains("steam vent", Actions(coatFlask).First(a => a.Name == "CoatGarment" && !a.IsDisabled).Hint);
            var cold = Flask(2, "cold-proofing"); StringAssert.DoesNotContain("You have seen", Actions(cold).First(a => a.Name == "CoatGarment" && !a.IsDisabled).Hint, "no cold has been seen");
        }

        [Test] public void EveryProofableRowIsShownBeforeEveryActualRefusal_WithoutTruncation()
        {
            var trough = StandingTrough(); Flask(8, "cold-proofing"); Flask(8, "heat-proofing"); Clay(8);
            var names = new[] { "AcidworkerApron", "ChainMail", "ColdwardCloak", "FilterHood", "GroundwireScreen", "IronshodBoots", "KilnfeltApron", "LeatherArmor" };
            foreach (var n in names) Carry(n); foreach (var done in Pack.Objects.Where(o => o.BlueprintName == "AcidworkerApron" || o.BlueprintName == "ChainMail").ToList()) { Assert.True(ItemEnhancing.Apply(done, "EnhancementProofed")); }
            var rows = Rows(trough); Assert.AreEqual(16, rows.Length, "all eight garments x two elements");
            int firstRefused = Array.FindIndex(rows, r => r.IsDisabled); Assert.Greater(firstRefused, 0); Assert.True(rows.Skip(firstRefused).All(r => r.IsDisabled), "every row you could use comes before any you could not");
            Assert.AreEqual(12, rows.Count(r => !r.IsDisabled)); Assert.AreEqual(4, rows.Count(r => r.DisabledCode == "already-proofed")); Assert.False(rows.Any(r => r.DisabledCode == "more"));
        }

        [Test] public void AProofedGarmentWornAtSaveTimeLoadsWithItsBonusOnceAndRevertsExactly()
        {
            var trough = StandingTrough(); var garment = Carry("LeatherArmor"); Flask(4, "cold-proofing"); Clay(2); Assert.True(Act(trough, RowFor(trough, garment, "cold"))); Assert.True(InventorySystem.Equip(Actor, garment));
            int resist = Stat("ColdResistance"); Assert.AreEqual(25, resist); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Assert.AreEqual(resist, copy.GetStatValue("ColdResistance"), "the saved boost is not applied twice on load");
            var worn = copy.GetPart<InventoryPart>().EquippedItems.Values.Single(o => o.BlueprintName == "LeatherArmor"); var p = worn.GetPart<EnhancementProofed>(); Assert.NotNull(p); Assert.True(p.AppliedBonus);
            Assert.True(InventorySystem.UnequipItem(copy, worn)); Assert.AreEqual(0, copy.GetStatValue("ColdResistance"), "taken off after a load, exactly the proofing goes");
        }

        [Test] public void TheScenariosFrostTonicIsTheEasyWayToFeelTheStatusRule_ABareDrinkerFreezesLongerThanAProofedOne()
        {
            var ctx = new CavesOfOoo.Scenarios.ScenarioContext(Zone, Factory, Actor, Clock); new CavesOfOoo.Scenarios.Custom.CampMachinesShowcase().Apply(ctx);
            var tonic = Pack.Objects.First(o => o.BlueprintName == "FrostTonic"); Assert.True(Act(tonic, "ApplyTonic"), string.Join("|", MessageLog.GetRecent(3))); float bare = Actor.GetEffect<FrozenEffect>().Cold; Actor.RemoveEffect<FrozenEffect>();
            Actor.Statistics["ColdResistance"] = new Stat { Name = "ColdResistance", Owner = Actor, BaseValue = 60, Min = -100, Max = 200 };
            var second = Pack.Objects.First(o => o.BlueprintName == "FrostTonic"); Assert.True(Act(second, "ApplyTonic")); float proofed = Actor.GetEffect<FrozenEffect>().Cold;
            Assert.AreEqual(1f, bare, 1e-3f); Assert.AreEqual(0.4f, proofed, 1e-3f, "a full-strength frost tonic at +60 cold leaves 40%");
            Assert.NotNull(Pack.Objects.FirstOrDefault(o => o.BlueprintName == "LightningTonic"), "and a lightning tonic for the shock");
        }

        [Test] public void AStackOfGarmentsIsProofedOneUnitAtATime_AndAnInterruptionPutsTheUnitBack()
        {
            var trough = StandingTrough(); var stack = Carry("LeatherArmor"); Carry("LeatherArmor"); Carry("LeatherArmor"); var flask = Flask(8, "cold-proofing"); Clay(4);
            var row = Rows(trough).Single(); StringAssert.Contains("1 of 3", row.Display); string command = row.Command; FailAfter();
            Assert.False(Act(trough, command)); Assert.AreEqual(3, stack.GetPart<StackerPart>().StackCount); Assert.AreEqual(8, flask.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual(4, ClayLeft());
            foreach (var junk in Actor.Parts.OfType<FiftyWorldThrowAfterPart>().ToList()) Actor.RemovePart(junk);
            Assert.True(Act(trough, command), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount); var proofed = Pack.Objects.Single(o => Proofed(o) != null);
            Assert.AreNotSame(stack, proofed); Assert.Null(Proofed(stack)); Assert.AreEqual("cold", Proofed(proofed).Kind); Assert.AreEqual(4, flask.GetPart<LiquidVesselPart>().Volume); Assert.AreEqual(2, ClayLeft());
            Assert.True(InventorySystem.Equip(Actor, proofed)); Assert.AreEqual(25, Stat("ColdResistance"));
        }

        [Test] public void TheReadersNameWhatTheTroughDoesAndWhatYouCarry()
        {
            var trough = StandingTrough(); Flask(2, "cold-proofing"); Clay(1); var l = new System.Collections.Generic.List<string>(); StateReader.Collect(trough, true, Zone, Zone.GetEntityCell(trough), Actor, l); string text = string.Join(" ", l);
            StringAssert.Contains("fire clay", text); StringAssert.Contains("2 cold", text.Replace("cold-proofing", "cold")); StringAssert.Contains("1 fire clay", text);
            var guide = new System.Collections.Generic.List<string>(); UseGuides.Collect(trough, Zone, Zone.GetEntityCell(trough), Actor, guide); string how = string.Join(" ", guide);
            StringAssert.Contains("for good", how); StringAssert.Contains(ProofingRules.PermanentUnits + " units", how); StringAssert.Contains(ProofingRules.PermanentClay + " fire clay", how, "what it costs is in the how-to block, from the rule's own numbers");
            var garment = Carry("LeatherArmor"); var f2 = Flask(4, "heat-proofing"); Clay(1); Assert.True(Act(trough, RowFor(trough, garment, "heat"))); StringAssert.Contains("Proofed against heat for good", Proofed(garment).GetEffectDescription()); StringAssert.Contains("+25", Proofed(garment).GetEffectDescription());
        }

        // ── unpicking a lasting proofing (the reassessment: a wrong choice must not fill a slot for good) ──

        InventoryAction[] UnpickRows(Entity trough) => WorldInteractionSystem.GatherActions(trough, Actor).Where(a => a.Name == "UnpickGarment").ToArray();
        Entity Proofing(Entity trough, string blueprint, string liquid = "cold-proofing", bool wear = false)
        {
            var garment = Carry(blueprint); var flask = Flask(4, liquid); Clay(2); if (wear) Assert.True(InventorySystem.Equip(Actor, garment));
            Assert.True(Act(trough, RowFor(trough, garment, liquid.Replace("-proofing", "").Replace("shock", "electric"))), string.Join("|", MessageLog.GetRecent(3))); Assert.NotNull(Proofed(garment)); return garment;
        }

        [Test] public void AWornProofedGarmentCanBeUnpickedWhichRemovesItsResistanceFreesTheSlotAndReturnsNothing()
        {
            var trough = StandingTrough(); var garment = Proofing(trough, "LeatherArmor", wear: true); Assert.AreEqual(25, Stat("ColdResistance")); int slots = ItemEnhancing.CountEnhancements(garment); Assert.AreEqual(1, slots);
            var rows = UnpickRows(trough); Assert.AreEqual(1, rows.Length); StringAssert.Contains("unpick the cold proofing from", rows[0].Display); StringAssert.Contains("(worn)", rows[0].Display); StringAssert.Contains("nothing is returned", rows[0].Hint); StringAssert.Contains("+25 cold resistance goes with it", rows[0].Hint);
            int clay = ClayLeft(), units = Pack.Objects.Select(o => o.GetPart<LiquidVesselPart>()).Where(v => v != null).Sum(v => v.Volume);
            MessageLog.Clear(); Assert.True(Act(trough, rows[0].Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.Null(Proofed(garment)); Assert.AreEqual(0, Stat("ColdResistance"), "the resistance goes at once"); Assert.AreEqual(0, ItemEnhancing.CountEnhancements(garment), "the slot is free");
            Assert.AreEqual(clay, ClayLeft(), "no clay comes back"); Assert.AreEqual(units, Pack.Objects.Select(o => o.GetPart<LiquidVesselPart>()).Where(v => v != null).Sum(v => v.Volume), "and no proofing comes back");
            StringAssert.Contains("Its slot is free again", string.Join(" ", MessageLog.GetRecent(3)));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "GarmentUnpicked", Limit = 5 }).Records.Count);
            Assert.AreEqual(0, UnpickRows(trough).Length, "nothing left to unpick");
            var f = Flask(4, "heat-proofing"); Clay(2); Assert.True(Act(trough, RowFor(trough, garment, "heat")), "the freed garment can be proofed against something else"); Assert.AreEqual(25, Stat("HeatResistance"));
        }

        [Test] public void ACarriedGarmentCanBeUnpickedToo_AndOnlyProofedGarmentsAreOffered_Counter()
        {
            var trough = StandingTrough(); var plain = Carry("ChainMail"); var proofed = Proofing(trough, "LeatherArmor");
            var rows = UnpickRows(trough); Assert.AreEqual(1, rows.Length, "the plain mail has nothing to unpick"); Assert.AreEqual(UnpickCommand(proofed), rows[0].Command); StringAssert.DoesNotContain("(worn)", rows[0].Display);
            Assert.True(Act(trough, rows[0].Command)); Assert.Null(Proofed(proofed)); Assert.Null(Proofed(plain));
        }

        [Test] public void OnlyTheLastingProofingComesOut_ACoatOnTheSameGarmentStays_Counter()
        {
            var trough = StandingTrough(); var garment = Proofing(trough, "LeatherArmor"); Assert.True(ItemEnhancing.Apply(garment, "EnhancementSteeped")); var coat = garment.Parts.OfType<EnhancementSteeped>().Single(); coat.Kind = "heat"; coat.TurnsLeft = 40; Assert.AreEqual(2, ItemEnhancing.CountEnhancements(garment));
            Assert.True(Act(trough, UnpickRows(trough).Single().Command)); Assert.Null(Proofed(garment)); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(garment)); Assert.AreSame(coat, garment.Parts.OfType<EnhancementSteeped>().Single()); Assert.AreEqual(40, coat.TurnsLeft);
        }

        [Test] public void AnInterruptedUnpickLeavesTheGarmentProofedAndItsResistanceIntact()
        {
            var trough = StandingTrough(); var garment = Proofing(trough, "LeatherArmor", wear: true); string command = UnpickRows(trough).Single().Command; FailAfter();
            Assert.False(Act(trough, command)); Assert.NotNull(Proofed(garment)); Assert.AreEqual("cold", Proofed(garment).Kind); Assert.AreEqual(25, Proofed(garment).Amount); Assert.AreEqual(25, Stat("ColdResistance"), "the worn bonus is exactly as it was, not doubled or lost"); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(garment));
        }

        [Test] public void AStaleOrForeignUnpickChangesNothing_Counter()
        {
            var trough = StandingTrough(); var garment = Proofing(trough, "LeatherArmor"); var plain = Carry("ChainMail");
            Assert.False(Act(trough, UnpickCommand(plain)), "a garment with nothing to unpick"); Assert.False(Act(trough, "UnpickGarment|nobody")); Assert.False(Act(trough, "UnpickGarment|")); Assert.NotNull(Proofed(garment));
            var kit = Carry("CampTrough"); Assert.IsEmpty(Actions(kit).Where(a => a.Name == "UnpickGarment"), "a carried trough is a kit and offers nothing");
            Pack.RemoveObject(garment); Assert.False(Act(trough, UnpickCommand(garment)), "no longer held"); Assert.NotNull(Proofed(garment));
        }

        [Test] public void TheReaderMentionsUnpickingOnlyWhenYouHoldAProofedGarment_Counter()
        {
            var trough = StandingTrough(); var l = new System.Collections.Generic.List<string>(); StateReader.Collect(trough, true, Zone, Zone.GetEntityCell(trough), Actor, l); StringAssert.DoesNotContain("unpick", string.Join(" ", l));
            var garment = Proofing(trough, "LeatherArmor"); l.Clear(); StateReader.Collect(trough, true, Zone, Zone.GetEntityCell(trough), Actor, l); StringAssert.Contains("a lasting proofing: unpicking one frees its slot and returns nothing", string.Join(" ", l));
        }

        [Test] public void EveryProofedGarmentCanBeUnpickedWithoutStashingOtherGarments()
        {
            var trough = StandingTrough(); var names = new[] { "LeatherArmor", "ChainMail", "PlateArmor", "Cloak", "WardedCloak", "IronHelmet", "LeatherBoots", "LeatherGloves" };
            foreach (var n in names) { var g = Carry(n); Assert.True(ItemEnhancing.Apply(g, "EnhancementProofed")); Proofed(g).Kind = "heat"; }
            var rows = UnpickRows(trough); Assert.AreEqual(8, rows.Length); Assert.True(rows.All(r => !r.IsDisabled));
            var last = Pack.Objects.Single(g => g.BlueprintName == "WardedCloak");
            var selected = rows.Single(r => r.Command == UnpickCommand(last));
            int before = Pack.Objects.Count; Assert.True(Act(trough, selected.Command));
            Assert.Null(Proofed(last)); Assert.AreEqual(7, Pack.Objects.Count(g => Proofed(g) != null));
            Assert.AreEqual(before, Pack.Objects.Count); Assert.AreEqual(0, ClayLeft());
            Assert.False(Act(trough, selected.Command), "a repeated stale unpick refunds nothing");
        }

        string UnpickCommand(Entity garment) => "UnpickGarment|" + Uri.EscapeDataString(garment.ID);
    }
}
