using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The owner (2026-11-21): the description of a machine or tool must say exactly how to use it, what materials or surroundings it needs, and whether it can be used right now, instead of a long flavor text. Every
    /// shipped machine and tool answers through <see cref="IDescribeUse"/>: the static facts come from the live constants of the rule that enforces them, and the "right now" line comes from the thing's own menu rows.
    /// These tests read the block the way the Examine readers show it, for each machine, in the states a player meets it in.
    /// </summary>
    public sealed class MachineUseGuideTests : FiftyWorldFixture
    {
        [SetUp] public void SetupGuides() { ProofingRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); Pack.MaxWeight = 1000; Actor.IntProperties.Remove(Noise.Property); }
        [TearDown] public void TearGuides() { ProofingRegistry.ResetForTests(); }

        List<string> Guide(Entity e, bool carried = false)
        {
            var lines = new List<string>(); UseGuides.Collect(e, carried ? null : Zone, carried ? null : Zone.GetEntityCell(e), Actor, lines); return lines;
        }
        static string Line(List<string> lines, string label) => lines.FirstOrDefault(l => l.StartsWith(label)) ?? "";
        static string All(List<string> lines) => string.Join("\n", lines);
        /// <summary>A kit standing in the world as set down leaves it: installed, and marked as the player's.</summary>
        Entity Standing(string blueprint, int x = 11, int y = 10)
        {
            var kit = Place(blueprint, x, y); kit.AddPart(new InstallablePart { Mode = InstallablePart.Portable, State = InstallablePart.Installed });
            kit.AddPart(new InstallationMarkerPart { Origin = HeldFixtureActions.PlayerOrigin, InstallationId = "player:" + kit.ID, RetainZone = true }); return kit;
        }

        // ── tools you carry ──

        [Test] public void TheScraperSaysHowToUseItWhatItNeedsAndWhatItCostsFromTheLiveNumbers()
        {
            var lines = Guide(Carry("Scraper"), true);
            StringAssert.Contains("(C) menu", Line(lines, "How to use"));
            StringAssert.Contains("steam vent or ash bed", Line(lines, "Needs")); StringAssert.Contains("within one step", Line(lines, "Needs"));
            string limits = Line(lines, "Cost and limits");
            StringAssert.Contains(SourceStockPart.MinCapacity + " to " + SourceStockPart.MaxCapacity + " grains", limits); StringAssert.Contains("rings through the stone", limits); StringAssert.Contains("cannot be sold", limits);
            StringAssert.Contains("within " + ProofingSources.Reach + " cells", Line(lines, "How to use"), "the pack row's reach is the rule's own number");
        }

        [Test] public void TheScrapersRightNowFollowsTheSourcesAroundIt()
        {
            var scraper = Carry("Scraper");
            StringAssert.Contains("no steam vent, ash bed, frost vent, ice sheet or pool of brine, acid or mire", Line(Guide(scraper, true), "Right now"), "nothing near: it says what is missing");
            Place("SteamVent", 12, 10);
            StringAssert.StartsWith("Right now you can: scrape vent scale from the steam vent", Line(Guide(scraper, true), "Right now"));
            var capacity = SourceStockPart.CapacityFor(Zone, Zone.GetCell(12, 10).Objects.First(o => o.BlueprintName == "SteamVent"));
            for (int i = 0; i < capacity; i++) Assert.True(Act(scraper, Actions(scraper).First(a => a.Name == "ScrapeSource").Command));
            StringAssert.StartsWith("Right now you cannot: scrape vent scale from the steam vent (", Line(Guide(scraper, true), "Right now"));
            StringAssert.Contains("spent for now", Line(Guide(scraper, true), "Right now"));
        }

        [Test] public void AToolLyingOnTheGroundSaysToPickItUpNotThatNothingIsNear()
        {
            Place("SteamVent", 12, 10);
            foreach (var name in new[] { "Scraper", "FlintStriker", "SoundingRod", "HushStrip", "SpringTrap", "Waterskin", "LiquidFlask" })
            {
                var lying = Place(name, 11, 11);   // beside you: out of reach it would only say you are not beside it
                StringAssert.Contains(UseGuides.PickUpFirst, Line(Guide(lying), "Right now"), name); Zone.RemoveEntity(lying);
            }
            var carried = Carry("Scraper"); Assert.That(Line(Guide(carried, true), "Right now"), Does.Not.Contain(UseGuides.PickUpFirst), "counter: carried, the line is about the place");
        }

        [Test] public void AStunnedActorIsToldItCannotActNotThatNothingIsNear()
        {
            var scraper = Carry("Scraper"); Place("SteamVent", 12, 10);
            Assert.True(Actor.ApplyEffect(new StunnedEffect(duration: 5)), "fixture: the stun lands");
            StringAssert.Contains("you cannot act just now", Line(Guide(scraper, true), "Right now"));
        }

        [Test] public void TheFlintStrikerCountsItsStrikesAndSaysWhatItLights()
        {
            var striker = Carry("FlintStriker"); var part = striker.GetPart<FlintStrikerPart>();
            var lines = Guide(striker, true);
            StringAssert.Contains("a strike left (" + part.Strikes + " of " + part.MaxStrikes + ")", Line(lines, "Needs")); StringAssert.Contains("strike a spark", Line(lines, "How to use"));
            StringAssert.Contains("brings back " + FlintStrikerPart.KnapStrikes, Line(lines, "Cost and limits"));
            StringAssert.Contains("no cell beside you is coated with oil", Line(lines, "Right now"), "full edge and nothing to light: the reason");
            part.Strikes = 7; StringAssert.Contains("(7 of " + part.MaxStrikes + ")", Line(Guide(striker, true), "Needs"));
            part.Strikes = 0; Carry("IronSpikeComponent");
            StringAssert.Contains("knap an edge", Line(Guide(striker, true), "Right now you can"), "a worn flint with a spike carried: the way out is a row");
        }

        [Test] public void AWornStrikerWithNoSpikeSaysItNeedsOne()
        {
            var striker = Carry("FlintStriker"); striker.GetPart<FlintStrikerPart>().Strikes = 0;
            StringAssert.Contains("Knapping a new edge needs an iron spike", Line(Guide(striker, true), "Right now you cannot"));
        }

        [Test] public void TheSoundingRodSaysWhereItWorksAndWhyItCannotHere()
        {
            var rod = Carry("SoundingRod"); var lines = Guide(rod, true);
            StringAssert.Contains("sound the ground", Line(lines, "How to use")); StringAssert.Contains("survey log", Line(lines, "How to use"));
            StringAssert.Contains("ordinary cave floor below the surface", Line(lines, "Needs")); StringAssert.Contains("rings through the stone", Line(lines, "Cost and limits"));
            StringAssert.Contains("nothing nearer than " + SoundingRodPart.MinPaces + " paces", Line(lines, "Cost and limits"));
            string now = Line(lines, "Right now"); StringAssert.StartsWith("Right now you cannot", now); StringAssert.Contains("sound the ground (The ground up here is too loose to carry a sounding)", now, "the surface is refused, and the row says so");
        }

        [Test] public void HushStripsSayHowToLayThemAndWhatALaneDoes()
        {
            var strips = Carry("HushStrip"); var lines = Guide(strips, true);
            StringAssert.Contains("lay hush strip", Line(lines, "How to use")); StringAssert.Contains("take up", Line(lines, "How to use")); StringAssert.Contains("empty, passable cell beside you", Line(lines, "Needs"));
            string limits = Line(lines, "Cost and limits"); StringAssert.Contains("One hush strip to a cell", limits); StringAssert.Contains("Footfalls on a laid strip are not heard", limits);
            StringAssert.StartsWith("Right now you can: lay a device here (", Line(lines, "Right now"), "eight open cells are the interact menu's one grouped row");
            var laid = Place("HushStripLaid", 11, 10); var laidLines = Guide(laid);
            StringAssert.Contains("take up the hush strip", Line(laidLines, "How to use")); StringAssert.Contains("you beside it", Line(laidLines, "Needs"));
            StringAssert.Contains("take up the hush strip", Line(laidLines, "Right now you can"), "beside it: the row is there");
        }

        [Test] public void TheSpringTrapSaysWhatItTakesToLoadAndLay_AndLoadingChangesIt()
        {
            var trap = Carry("SpringTrap"); var lines = Guide(trap, true);
            StringAssert.Contains("load the spring trap", Line(lines, "How to use")); StringAssert.Contains("Oil under fire moss burns", Line(lines, "How to use"));
            StringAssert.Contains("fire moss, frost lichen or spark root", Line(lines, "Needs")); StringAssert.Contains("you spring it too", Line(lines, "Cost and limits"));
            StringAssert.Contains("You carry none", Line(lines, "Right now you cannot"));
            Carry("FireMoss"); StringAssert.Contains("load the spring trap with fire moss", Line(Guide(trap, true), "Right now you can"));
            Assert.True(Act(trap, Actions(trap).First(a => a.Name == "LoadSpringTrap").Command), string.Join("|", MessageLog.GetRecent(3)));
            var loaded = Guide(trap, true); StringAssert.Contains("it is loaded with fire moss", Line(loaded, "Needs")); StringAssert.Contains("lay a device here (", Line(loaded, "Right now you can"));
            var state = new List<string>(); StateReader.Collect(trap, false, null, null, Actor, state); Assert.AreEqual("Loaded with fire moss.", state.Last(), "the state line is state only");
        }

        [Test] public void TheWaterskinAndTheFlaskSayHowToFillAndPour()
        {
            var skin = Skin(2, 3); var lines = Guide(skin, true);
            StringAssert.Contains("fill the waterskin", Line(lines, "How to use")); StringAssert.Contains("holds 3 drinks and holds 2 now", Line(lines, "Cost and limits")); StringAssert.Contains("fresh water", Line(lines, "Needs"));
            var flask = Flask(5, "oil", 12); var f = Guide(flask, true);
            StringAssert.Contains("choose pour or pour one", Line(f, "How to use")); StringAssert.Contains("holds up to 12 units and holds 5 now", Line(f, "Cost and limits"));
        }

        // ── kits ──

        [Test] public void ACarriedKitSaysHowToSetItDownAndWhatGroundItWantsAndAStandingOneHowToMoveIt()
        {
            var box = Carry("CampBox"); var carried = Guide(box, true);
            StringAssert.Contains("set down", Line(carried, "How to use")); StringAssert.Contains("make camp here", Line(carried, "How to use"));
            string needs = Line(carried, "Needs"); StringAssert.Contains("open ground", needs); StringAssert.Contains(PlacementCheck.DoorStairReach + " cells of a door, stair or well", needs); StringAssert.Contains(PlacementCheck.EdgeCells + " cells from the edge", needs);
            StringAssert.Contains("in the Spread, the Sodden, the Beating or the Stump", needs, "the country the placement rule admits, from its own list");
            StringAssert.Contains("any of the eight cells around it", needs, "a solid kit needs all eight sides clear");
            StringAssert.Contains("the four cells beside it", Line(Guide(Carry("CampBench"), true), "Needs"), "a bench needs its four sides");
            string limits = Line(carried, "Cost and limits"); StringAssert.Contains("only where it is set down", limits); StringAssert.Contains(PlacementCheck.ZoneCap + " things standing in one area", limits); StringAssert.Contains(PlacementCheck.WorldCap + " in the world", limits);
            Assert.That(Line(carried, "Right now"), Does.StartWith("Right now you cannot: set down ("), "this world has no ground a kit can stand on, and the line says so with the placement rule's own reason");

            var standing = Guide(Standing("CampBox"));
            StringAssert.Contains("take it up", Line(standing, "How to use")); StringAssert.Contains("takes up to 12 separate things", Line(standing, "How to use")); Assert.AreEqual("", Line(standing, "Needs"), "a standing kit needs nothing more");
        }

        [Test] public void TheKitSentenceComesFirstWhileCarriedAndLastWhileStanding()
        {
            var carried = Line(Guide(Carry("CampHearth"), true), "How to use");
            Assert.That(carried, Does.StartWith("How to use: Press C on an open cell beside you and choose set down"), "the kit sentence is first while it is carried: " + carried);
            StringAssert.Contains("Once it is set down", carried); Assert.That(carried, Does.Not.Contain("Feed coals"), "a carried hearth cannot be fed: its how-to is how to set it down");
            var standing = Line(Guide(Standing("CampHearth")), "How to use");
            Assert.That(standing.IndexOf("Feed coals", System.StringComparison.Ordinal), Is.GreaterThanOrEqualTo(0), standing);
            Assert.That(standing.IndexOf("take it up", System.StringComparison.Ordinal), Is.GreaterThan(standing.IndexOf("Feed coals", System.StringComparison.Ordinal)), "what it does first, how to move it last: " + standing);
        }

        [Test] public void ACarriedStillExplainsSettingDownNotSettling_Counter()
        {
            var carried = All(Guide(Carry("CampStill"), true));
            StringAssert.Contains("set down", carried); StringAssert.Contains("Once it is set down, press C beside it to brew", carried);
            Assert.That(carried, Does.Not.Contain("choose settle"), "the settling and baking verbs belong to the standing still"); Assert.That(carried, Does.Not.Contain("brew the mix"));
        }

        [Test] public void AHandedOverOrWorldFixtureDoesNotSayItCanBeTakenUp()
        {
            var kit = Standing("CampBox"); StringAssert.Contains("take it up", Line(Guide(kit), "How to use"));
            kit.RemovePart(kit.GetPart<InstallationMarkerPart>());   // handed over: it stands, and is no longer yours to lift
            Assert.IsEmpty(Guide(kit), "a fixture that is not yours to take up has no kit guide, and a box has no verbs of its own to describe");
        }

        [Test] public void ADroppedKitBesideYouSaysToPickItUpWhateverMachineItIs()
        {
            foreach (var name in new[] { "CampSettlingTray", "CampFishTrap", "CampHearth", "CampAwning", "CampBox" })
            {
                var lying = Place(name, 11, 10);   // never set down: a loose kit on the ground
                StringAssert.Contains("it lies on the ground: pick it up first", Line(Guide(lying), "Right now"), name + ": its machine's narrower reason must not overrule the kit's"); Zone.RemoveEntity(lying);
            }
        }

        [Test] public void AStandingKitFarFromYouSaysYouAreNotBesideIt()
        {
            var kit = Standing("CampBench", 17, 10); var lines = Guide(kit);
            Assert.That(lines.Single(l => l.StartsWith("Right now")), Is.EqualTo("Right now: you are not beside it."), "from further off its menu cannot be opened: nothing else is said");
            var near = Standing("CampBench", 11, 9); Assert.That(Guide(near).Single(l => l.StartsWith("Right now")), Does.StartWith("Right now you can"));
        }

        [Test] public void TheWaterskinSaysWhetherDrinkingAndFillingWouldWork()
        {
            var skin = Skin(0, 3);
            string now = Line(Guide(skin, true), "Right now");
            StringAssert.Contains("drink water (0/3) (The waterskin is empty)", now); StringAssert.Contains("fill from nearby fresh water (There is no fresh water beside you)", now);
            Pool(8, "water", 11, 10);
            now = Line(Guide(skin, true), "Right now"); StringAssert.StartsWith("Right now you can:", now); Assert.That(now.IndexOf("fill from nearby fresh water", System.StringComparison.Ordinal), Is.LessThan(now.IndexOf("Not now:", System.StringComparison.Ordinal)), "fill is something you can do now: " + now); StringAssert.Contains("Not now: drink water (0/3) (The waterskin is empty)", now);
            skin.GetPart<WaterskinPart>().Charges = 3; now = Line(Guide(skin, true), "Right now"); StringAssert.Contains("drink water (3/3)", now); StringAssert.Contains("(The waterskin is full)", now);
        }

        [Test] public void TheForgeSaysWhatThePickedPartsWouldMake()
        {
            var forge = Place("TinkersForge", 11, 10);
            StringAssert.Contains("Craft (Pick a blade, a haft and a binding", Line(Guide(forge), "Right now you cannot"), "nothing picked: Craft is listed but does nothing");
            foreach (var name in new[] { "SteelBladeComponent", "OakHaftComponent", "LeatherBindingComponent" }) { var part = Carry(name); Assert.True(CraftingMarkPart.Toggle(part), name); }
            Assert.That(Line(Guide(forge), "Right now"), Is.EqualTo("Right now you can: Craft: forge a weapon."));
        }

        [Test] public void TheStillSaysWhetherAMixIsPicked()
        {
            var still = Standing("CampStill"); StringAssert.Contains("brew the mix (Pick the reagents to brew", Line(Guide(still), "Right now"));
            var moss = Carry("FireMoss"); Assert.True(CraftingMarkPart.Toggle(moss)); StringAssert.Contains("brew the mix, brew a full batch", Line(Guide(still), "Right now you can"));
        }

        [Test] public void TheShoreKitNamesTheShoreItNeeds()
        {
            StringAssert.Contains("right beside water it can fish", Line(Guide(Carry("CampFishTrap"), true), "Needs"));
        }

        [Test] public void EveryShippedKitExplainsItselfCarriedAndStanding()
        {
            int seen = 0;
            foreach (var def in PackableFixtureRegistry.All.Where(d => d.Class == PackableFixtureDefinition.Carried))
            {
                seen++; var carried = Guide(Carry(def.Blueprint), true);
                Assert.That(Line(carried, "How to use"), Does.Contain("set down"), def.Kind + ": carried"); Assert.That(Line(carried, "Needs"), Is.Not.Empty, def.Kind + ": needs"); Assert.That(Line(carried, "Cost and limits"), Is.Not.Empty, def.Kind + ": limits");
                var standing = Guide(Standing(def.Blueprint, 11, 9 + seen));
                Assert.That(Line(standing, "How to use"), Is.Not.Empty, def.Kind + ": standing"); Assert.That(Line(standing, "How to use"), Does.Contain("take it up"), def.Kind + ": standing");
            }
            Assert.That(seen, Is.GreaterThanOrEqualTo(8), "the eight camp kits");
        }

        [Test] public void TheHearthExplainsFeedingLightingAndCooking_AndOnlyTheCampHearthDoes()
        {
            var hearth = Standing("CampHearth"); var lines = Guide(hearth);
            StringAssert.Contains("Feed coals", Line(lines, "How to use")); StringAssert.Contains("cook this stack beside a fire", Line(lines, "How to use"));
            StringAssert.Contains("to feed it: salvaged timber", Line(lines, "Needs")); StringAssert.Contains("to light it: a flame", Line(lines, "Needs")); StringAssert.Contains("to rest by it: no hostile creature within " + RestSystem.HostileScanRadius + " cells", Line(lines, "Needs")); StringAssert.Contains("goes out when the fuel does", Line(lines, "Cost and limits"));
            StringAssert.Contains("rest, or rest until a time of day", Line(lines, "How to use"), "resting beside it is explained too");
            var village = Place("Campfire", 11, 11); Assert.False(UseGuides.Collect(village, Zone, Zone.GetEntityCell(village), Actor, new List<string>()), "an ordinary campfire has no guide");
        }

        [Test] public void TheStillExplainsBrewingSettlingAndBakingWithItsRates()
        {
            var still = Standing("CampStill"); var lines = Guide(still); string how = Line(lines, "How to use");
            StringAssert.Contains("brew the mix", how); StringAssert.Contains("settle", how); StringAssert.Contains("bake", how);
            StringAssert.Contains("to settle: a flask with at least " + LiquidSettlingPart.Ratio + " units", Line(lines, "Needs")); StringAssert.Contains("to bake clay: a flask with at least " + LiquidSettlingPart.ClayRatio + " units", Line(lines, "Needs")); StringAssert.Contains(LiquidSettlingPart.FuelPerBatch + " fuel", Line(lines, "Needs"));
            StringAssert.Contains("1 fire clay for every " + LiquidSettlingPart.ClayRatio, Line(lines, "Cost and limits"));
        }

        [Test] public void TheBenchAndTheForgeExplainCrafting_AndACarriedBenchLeavesItToTheKit()
        {
            var forge = Place("TinkersForge", 11, 10); var lines = Guide(forge);
            StringAssert.Contains("choose Craft", Line(lines, "How to use")); StringAssert.Contains("a blade, a haft and a binding", Line(lines, "Needs")); StringAssert.Contains("uses up the picked parts", Line(lines, "Cost and limits"));
            var carried = Guide(Carry("CampBench"), true); Assert.That(Line(carried, "How to use"), Does.Not.Contain("choose Craft"), "a carried bench is a kit: it explains setting down, not crafting");
            var benchNow = Guide(Standing("CampBench", 11, 9)); StringAssert.Contains("choose Craft", Line(benchNow, "How to use"));
            Assert.That(Line(benchNow, "Right now"), Does.Not.Contain("=="), "the picker's section headers do nothing and are not offered"); Assert.That(Line(benchNow, "Right now"), Does.Not.Contain("none carried"));
        }

        [Test] public void TheTroughTraySaysTheNumbersItsRulesUse()
        {
            var trough = Standing("CampTrough"); var t = Guide(trough);
            StringAssert.Contains("for good", Line(t, "How to use")); StringAssert.Contains(ProofingRules.PermanentUnits + " units", Line(t, "Needs")); StringAssert.Contains(ProofingRules.PermanentClay + " fire clay", Line(t, "Needs"));
            StringAssert.Contains("+" + ProofingRules.PermanentAmount + " resistance", Line(t, "Cost and limits")); StringAssert.Contains("Nothing you carry fits", Line(t, "Right now"), "beside it: its own take-up rows come first, and the refused proofing is named with its reason");

            var tray = Standing("CampSettlingTray", 11, 9); var g = Guide(tray);
            StringAssert.Contains("within " + ProofingSources.Reach + " cells", Line(g, "Needs")); StringAssert.Contains("choose gather", Line(g, "How to use")); StringAssert.DoesNotContain("Set it down", Line(g, "How to use"), "standing, it is already set down");
            StringAssert.Contains("Set it down within " + ProofingSources.Reach + " cells of a source", Line(Guide(Carry("CampSettlingTray"), true), "How to use")); StringAssert.Contains("up to " + SettlingTrayPart.MaxHeld + " at a time", Line(g, "Cost and limits"));
            StringAssert.Contains(SourceStockPart.MinCapacity + " to " + SourceStockPart.MaxCapacity + " grains", Line(g, "Cost and limits")); StringAssert.Contains("nothing near enough to settle from", Line(g, "Right now"));
        }

        [Test] public void TheFishTrapAndTheAwningSayWhatTheyNeedAndWhatTheyGive()
        {
            var trap = Standing("CampFishTrap"); var f = Guide(trap);
            StringAssert.Contains("take the fish", Line(f, "How to use")); StringAssert.Contains("cascade basin", Line(f, "Needs")); StringAssert.Contains("up to " + FishTrapPart.MaxHeld + " at a time", Line(f, "Cost and limits"));
            StringAssert.Contains("not beside a pond", Line(f, "Right now"));
            var awning = Standing("CampAwning"); var a = Guide(awning); int reach = System.Math.Min(awning.GetPart<ShadePart>().Radius, ShadePart.MaxRadius);
            StringAssert.Contains("within " + reach + " of it", Line(a, "How to use")); StringAssert.Contains("every " + BeatingGlareSystem.ExposureTurnsPerStack + " turns", Line(a, "Cost and limits"));
        }

        // ── machines in the world ──

        [Test] public void TheSortersFrameAndTheSettersBenchSayWhatTheyWantAndGive()
        {
            var frame = Place("SortingFrame", 11, 10); var f = Guide(frame);
            StringAssert.Contains("set in", Line(f, "How to use")); StringAssert.Contains("worth at most " + SortingFramePart.CheapLimit + " drams", Line(f, "Needs")); StringAssert.Contains("stays in the frame for good", Line(f, "Cost and limits"));
            StringAssert.Contains("nothing with a colour", Line(f, "Right now you cannot"));

            var bench = Place("SettingBench", 11, 9); var b = Guide(bench);
            StringAssert.Contains("lacquer", Line(b, "How to use")); StringAssert.Contains("regard of " + SettingBenchPart.MinRegard + " or more", Line(b, "Needs")); StringAssert.Contains("up to " + SettingBenchPart.MaxTier, Line(b, "Needs"));
        }

        [Test] public void TheProvingPostListsWhatItCanCall()
        {
            var post = Place("ElementalProvingPost", 11, 10); var lines = Guide(post);
            StringAssert.Contains("call a foe", Line(lines, "How to use")); StringAssert.Contains("Up to ", Line(lines, "Cost and limits")); StringAssert.Contains("call a", Line(lines, "Right now you can"));
        }

        [Test] public void AFarawayMachineSaysYouAreNotBesideIt()
        {
            var frame = Place("SortingFrame", 20, 10);
            StringAssert.Contains("you are not beside it", Line(Guide(frame), "Right now"));
        }

        // ── the owner's voice review: every block, as a player reads it ──

        /// <summary>Not a check: it writes the whole wording of every guide, in the states a player meets each machine in, so the owner can read it in one place (Docs/MACHINE-USE-TEXTS.md). Run it by name in a clone and copy the file back.</summary>
        [Test, Explicit("writes Docs/MACHINE-USE-TEXTS.md for the owner's voice review")]
        public void ExportUseTextsForTheVoicePass()
        {
            var md = new System.Text.StringBuilder();
            md.AppendLine("# How every machine and tool explains itself").AppendLine();
            md.AppendLine("Generated by `MachineUseGuideTests.ExportUseTextsForTheVoicePass` from the live rules (every number below is read from the constant that enforces it), so it never drifts. Each entry is the text a player sees when examining the thing in the named state: the blueprint's short description, then the block (**How to use**, **Needs**, **Cost and limits**, and the live **Right now** line built from the thing's own menu rows). The wording is placeholder pending the owner's voice review: please mark lines that are unclear, wrong or too long.").AppendLine();
            string Flavor(string name) { var parts = Factory.Blueprints[name].Parts["Examinable"]; return parts.TryGetValue("Description", out var d) ? d : parts["Text"]; }
            void Show(string heading, string blueprint, List<string> lines, bool flavor = true)
            {
                md.AppendLine("### " + heading).AppendLine();
                foreach (var line in lines) md.AppendLine("- " + line.Replace("\n", " "));
                if (flavor) md.AppendLine("- *" + Flavor(blueprint) + "*");
                md.AppendLine();
            }
            void Head(string title) { md.AppendLine("## " + title).AppendLine(); }

            Head("Scraper"); var scraper = Carry("Scraper");
            Show("In your pack, no source near", "Scraper", Guide(scraper, true));
            var vent = Place("SteamVent", 11, 10); Show("In your pack, beside a steam vent", "Scraper", Guide(scraper, true), false);
            int cap = SourceStockPart.CapacityFor(Zone, vent); for (int i = 0; i < cap; i++) Act(scraper, Actions(scraper).First(a => a.Name == "ScrapeSource").Command);
            Show("The vent is spent", "Scraper", Guide(scraper, true), false);
            Head("Flint striker"); var striker = Carry("FlintStriker");
            Show("Full edge, nothing to light", "FlintStriker", Guide(striker, true)); striker.GetPart<FlintStrikerPart>().Strikes = 0;
            Show("Worn smooth, no spike", "FlintStriker", Guide(striker, true), false);
            Head("Sounding rod"); Show("On the surface", "SoundingRod", Guide(Carry("SoundingRod"), true));
            Head("Hush strips"); Show("In your pack", "HushStrip", Guide(Carry("HushStrip"), true)); Show("A laid strip, beside it", "HushStripLaid", Guide(Place("HushStripLaid", 10, 11)));
            Head("Spring trap"); var trap = Carry("SpringTrap"); Show("Unloaded, nothing to load it with", "SpringTrap", Guide(trap, true)); Carry("FireMoss");
            Show("Unloaded, with fire moss in the pack", "SpringTrap", Guide(trap, true), false); Act(trap, Actions(trap).First(a => a.Name == "LoadSpringTrap").Command); Show("Loaded with fire moss", "SpringTrap", Guide(trap, true), false);
            Head("Waterskin and flask"); Show("Waterskin, two of three", "Waterskin", Guide(Skin(2, 3), true)); Show("Flask of oil", "LiquidFlask", Guide(Flask(5, "oil", 12), true), false);

            foreach (var def in PackableFixtureRegistry.All.Where(d => d.Class == PackableFixtureDefinition.Carried))
            {
                Head("Camp kit: " + def.Kind);
                Show("In your pack", def.Blueprint, Guide(Carry(def.Blueprint), true));
                var kit = Standing(def.Blueprint, 11, 10); Show("Set down beside you", def.Blueprint, Guide(kit), false); Zone.RemoveEntity(kit);
            }
            Head("Machines in the world");
            foreach (var (blueprint, heading) in new[] { ("TinkersForge", "Forge beside you"), ("SortingFrame", "Sorters' frame beside you, nothing coloured carried"), ("SettingBench", "Setters' bench beside you, a stranger to the Bower-Folk"), ("ElementalProvingPost", "Proving post beside you") })
            { var machine = Place(blueprint, 11, 10); Show(heading, blueprint, Guide(machine)); Zone.RemoveEntity(machine); }
            System.IO.File.WriteAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Docs/MACHINE-USE-TEXTS.md")), md.ToString());
        }

        // ── the descriptions themselves stay short ──

        static readonly string[] FlavorOnly =
        {
            "CampBox", "CampHearth", "CampBench", "CampStill", "CampTrough", "CampFishTrap", "CampSettlingTray", "CampAwning", "FlintStriker", "Scraper", "SoundingRod",
            "HushStrip", "HushStripLaid", "SpringTrap", "SortingFrame", "SettingBench", "Waterskin", "ElementalProvingPost",
        };

        [Test] public void AMachinesStaticDescriptionIsWhatItIsAndNeverHowToUseIt()
        {
            foreach (var name in FlavorOnly)
            {
                Assert.True(Factory.Blueprints.TryGetValue(name, out var blueprint), name); Assert.True(blueprint.Parts.TryGetValue("Examinable", out var examinable), name);
                string text = examinable.TryGetValue("Description", out var d) ? d : examinable["Text"];
                Assert.That(text.Length, Is.LessThanOrEqualTo(130), name + " is flavor, not a manual: " + text);
                foreach (var forbidden in new[] { "Press ", "choose ", "C menu", "menu", "Set it down", "set it down" }) Assert.That(text, Does.Not.Contain(forbidden), name + ": " + text);
                Assert.That(text.Any(char.IsDigit), Is.False, name + ": a number belongs in the guide, where it comes from the rule: " + text);
            }
        }
    }
}
