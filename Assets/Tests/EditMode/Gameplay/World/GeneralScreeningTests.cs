using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GeneralScreeningTests : FiftyWorldFixture
    {
        const string Prefix="MaterialProcess|screen_size_grades|";
        EntityFactory oldForge;
        [SetUp] public void ReadyScreening(){oldForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}}
        [TearDown] public void RestoreScreening(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();}
        Entity Feed(int n=1){var e=Carry("DryClaySpoil");e.GetPart<StackerPart>().StackCount=n;return e;}
        Entity Sieve()=>Carry("HandSieve");
        string Screen(Entity e)=>Choice(e,Prefix);
        int Held(string bp)=>Pack.Objects.Where(e=>e.BlueprintName==bp).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        void Contents(string raw)=>Factory.Blueprints["DryClaySpoil"].Parts["SiftableStock"]["Contents"]=raw;
        static Part Stock(Entity e)=>e.Parts.Single(p=>p.Name=="SiftableStock");
        static void SetContents(Entity e,string raw){var p=Stock(e);p.GetType().GetField("Contents").SetValue(p,raw);}

        [Test]
        public void PacketExamineExplainsBothFractionsWithoutASieveAndKeepsTheScreeningQuote()
        {
            const string fractions = "2 fire clay + 1 coarse grit";
            var spoil = Feed(2);
            int tick = Clock.TickCount;
            var withoutTool = ExamineDocumentBuilder.BuildInventory(spoil, Actor);
            Assert.That(withoutTool, Is.Not.Null);
            Assert.That(withoutTool.ToPlainText(), Does.Contain(fractions).IgnoreCase);
            Assert.That(withoutTool.ToPlainText(), Does.Not.Contain("4 fire clay + 2 coarse grit").IgnoreCase);
            Assert.That(Actions(spoil).Any(row => !row.IsDisabled && row.Command.StartsWith(Prefix, StringComparison.Ordinal)), Is.False);

            var sieve = Sieve();
            var selected = Actions(spoil).Single(row => row.Command.StartsWith(Prefix, StringComparison.Ordinal));
            Assert.That(selected.Display + " " + selected.Hint, Does.Contain(fractions).IgnoreCase);
            var withTool = ExamineDocumentBuilder.BuildInventory(spoil, Actor);
            Assert.That(withTool, Is.Not.Null);
            Assert.That(withTool.ToPlainText(), Does.Contain(fractions).IgnoreCase);
            Assert.That(Clock.TickCount, Is.EqualTo(tick));
            Assert.That(Held("DryClaySpoil"), Is.EqualTo(2));
            Assert.That(Held("FireClay") + Held("CoarseGrit"), Is.Zero);

            Assert.True(Act(spoil, selected.Command));
            Assert.That(Held("DryClaySpoil"), Is.EqualTo(1));
            Assert.That(Held("FireClay"), Is.EqualTo(2));
            Assert.That(Held("CoarseGrit"), Is.EqualTo(1));
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
        }

        [TestCase("FireClay:2:0;FireClay:1:2")]
        [TestCase("FireClay:1:0;CoarseGrit:2:2")]
        public void AlteredPacketExamineDoesNotPromiseScreeningOutput(string contents)
        {
            Sieve();
            var spoil = Feed();
            SetContents(spoil, contents);
            var document = ExamineDocumentBuilder.BuildInventory(spoil, Actor);
            Assert.That(document, Is.Not.Null);
            Assert.That(document.ToPlainText(), Does.Not.Contain("2 fire clay + 1 coarse grit").IgnoreCase);
            Assert.That(document.ToPlainText(), Does.Not.Contain("1 fire clay + 2 coarse grit").IgnoreCase);
            Assert.That(document.ToPlainText(), Does.Not.Contain("two measures of fine fire clay").IgnoreCase);
            Assert.That(Actions(spoil).Any(row => !row.IsDisabled && row.Command.StartsWith(Prefix, StringComparison.Ordinal)), Is.False);
            Assert.That(Held("DryClaySpoil"), Is.EqualTo(1));
        }

        [Test]
        public void PacketReadersFollowTheCurrentAdmittedManifestRatherThanItsSavedFlavor()
        {
            Contents("FireClay:1:0;CoarseGrit:2:2");
            var spoil = Feed();
            var description = spoil.GetPart<ExaminablePart>();
            string stored = description.Text;
            Assert.That(stored, Does.Contain("two measures of fine fire clay"));
            const string current = "1 fire clay + 2 coarse grit";
            Assert.That(description.BuildExamineLine(), Does.Contain(current));
            Assert.That(ExamineDocumentBuilder.BuildInventory(spoil, Actor).ToPlainText(), Does.Contain(current));
            Assert.That(description.Text, Is.EqualTo(stored));
            Sieve();
            var action = Actions(spoil).Single(row => row.Command.StartsWith(Prefix, StringComparison.Ordinal));
            Assert.That(action.Display + " " + action.Hint, Does.Contain(current));
        }

        [Test]
        public void ReadingAnOwnedPacketDoesNotOpenItsScreeningMenuCache()
        {
            var spoil = Feed();
            Sieve();
            const string yield = "2 fire clay + 1 coarse grit";
            var provider = typeof(MaterialProcessInputPart).Assembly.GetType("CavesOfOoo.Core.MaterialProcessActions");
            var cache = provider?.GetField("Menus", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null);
            bool HasQuoteCache() => cache != null && (bool)cache.GetType().GetMethod("TryGetValue").Invoke(cache, new object[] { spoil, null });
            Assert.That(HasQuoteCache(), Is.False);
            Assert.That(spoil.GetPart<ExaminablePart>().BuildExamineLine(), Does.Contain(yield));
            Assert.That(ExamineDocumentBuilder.BuildInventory(spoil, Actor).ToPlainText(), Does.Contain(yield));
            Assert.That(HasQuoteCache(), Is.False);
        }

        private static InventoryAction LeaveGritRow(IEnumerable<InventoryAction> rows)
        {
            var matches = rows.Where(a => a.Display != null
                && a.Display.StartsWith("screen clay", StringComparison.OrdinalIgnoreCase)
                && a.Display.IndexOf("leave grit", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            Assert.That(matches, Has.Length.EqualTo(1), "The choice to leave grit has its own inventory row.");
            Assert.That(matches[0].IsDisabled, Is.False);
            return matches[0];
        }

        [Test]
        public void LeaveGritKeepsTwoClayAndPlacesExactlyOneNormallyRecoverableGroundOwner()
        {
            var sieve = Sieve();
            var spoil = Feed();
            var cell = Zone.GetEntityCell(Actor);
            int mass = Pack.GetCarriedWeight();
            Pack.MaxWeight = mass;
            var rows = Actions(spoil);
            var all = rows.Single(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal));
            var leave = LeaveGritRow(rows);
            Assert.That(leave.Command, Is.Not.EqualTo(all.Command));
            Assert.That(leave.Time, Is.EqualTo(ActionTime.OneAction));
            Assert.That(rows.ToList().IndexOf(all), Is.LessThan(rows.ToList().IndexOf(leave)));
            Assert.That(all.Display, Does.Contain("pack both").And.Contain("3 lb"));
            Assert.That(leave.Display, Does.Contain("carry 2 lb").And.Contain("leave 1 lb"));
            Assert.That(HandlingService.GetWeight(spoil), Is.EqualTo(3));

            Assert.That(Act(spoil, leave.Command), Is.True);
            var grit = cell.Objects.Single(e => e.BlueprintName == "CoarseGrit");
            Assert.That(Zone.GetEntityCell(grit), Is.SameAs(cell));
            Assert.That(grit.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(grit.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.That(Held("DryClaySpoil"), Is.Zero);
            Assert.That(Held("FireClay"), Is.EqualTo(2));
            Assert.That(Held("CoarseGrit"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.EqualTo(1));
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(mass - 1));

            Pack.MaxWeight = mass - 1;
            Assert.That(InventorySystem.Pickup(Actor, grit, Zone), Is.False);
            Assert.That(Zone.GetEntityCell(grit), Is.SameAs(cell));
            Pack.MaxWeight = mass;
            Assert.That(InventorySystem.Pickup(Actor, grit, Zone), Is.True);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "CoarseGrit"), Is.SameAs(grit));
            Assert.That(Zone.GetEntityCell(grit), Is.Null);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(mass));
        }

        [Test]
        public void NewCoatingInvalidatesGroundOnlyQuoteAndTheSameSpoilCanStillScreenIntoPack()
        {
            var sieve = Sieve();
            var spoil = Feed();
            var cell = Zone.GetEntityCell(Actor);
            int mass = Pack.GetCarriedWeight();
            var rows = Actions(spoil);
            var all = rows.Single(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal));
            var leave = LeaveGritRow(rows);
            Zone.TileState.WriteCoating(cell.X, cell.Y, "oil", 30);

            Assert.That(Act(spoil, leave.Command), Is.False);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "DryClaySpoil"), Is.SameAs(spoil));
            Assert.That(spoil.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.Zero);
            Assert.That(Zone.TileState.CoatingTurns(cell.X, cell.Y, "oil"), Is.EqualTo(30));
            Assert.That(Act(spoil, all.Command), Is.True);
            Assert.That(Held("FireClay"), Is.EqualTo(2));
            Assert.That(Held("CoarseGrit"), Is.EqualTo(1));
            Assert.That(cell.Objects.Any(e => e.BlueprintName == "CoarseGrit"), Is.False);
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(mass));
        }

        [Test]
        public void LateVetoRetractsGroundGritAndPackedClayWhileKeepingIndependentInjury()
        {
            var sieve = Sieve();
            var spoil = Feed(2);
            var cell = Zone.GetEntityCell(Actor);
            var existingGrit = Place("CoarseGrit", cell.X + 1, cell.Y);
            var leave = LeaveGritRow(Actions(spoil));
            int mass = Pack.GetCarriedWeight();
            int hp = Actor.GetStat("Hitpoints").BaseValue;
            Entity stagedGroundGrit = null;
            int packedClay = 0;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                stagedGroundGrit = cell.Objects.SingleOrDefault(e => e.BlueprintName == "CoarseGrit");
                packedClay = Held("FireClay");
                Actor.GetStat("Hitpoints").BaseValue -= 2;
            } });
            FailAfter();

            Assert.That(Act(spoil, leave.Command), Is.False);
            Assert.That(stagedGroundGrit, Is.Not.Null);
            Assert.That(packedClay, Is.EqualTo(2));
            Assert.That(Zone.GetEntityCell(stagedGroundGrit), Is.Null);
            Assert.That(Pack.Objects.Contains(stagedGroundGrit), Is.False);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "DryClaySpoil"), Is.SameAs(spoil));
            Assert.That(spoil.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Zone.GetCell(cell.X + 1, cell.Y).Objects.Single(e => e.BlueprintName == "CoarseGrit"), Is.SameAs(existingGrit));
            Assert.That(Count("CoarseGrit"), Is.EqualTo(1));
            Assert.That(Actor.GetStat("Hitpoints").BaseValue, Is.EqualTo(hp - 2));
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(mass));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NeitherDestinationCommitsWhenIndependentStockExceedsItsWeightBudget(bool leaveOnGround)
        {
            var sieve = Sieve();
            var spoil = Feed();
            var ballast = Factory.CreateEntity("SalvagedTimber");
            ballast.GetPart<StackerPart>().StackCount = 2;
            Assert.That(InventoryPart.GetItemWeight(ballast), Is.EqualTo(2));
            int mass = Pack.GetCarriedWeight();
            Pack.MaxWeight = mass;
            var rows = Actions(spoil);
            var row = leaveOnGround ? LeaveGritRow(rows)
                : rows.Single(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal));
            // A callback brings in unrelated workbench stock after output allocation.
            // The weighed extra stock is more than this operation can free in either mode.
            bool added = false;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                Pack.MaxWeight = 200;
                added = Pack.AddObject(ballast);
                Pack.MaxWeight = mass;
            } });

            Assert.That(Act(spoil, row.Command), Is.False);
            Assert.That(added, Is.True);
            Assert.That(Pack.Objects.Contains(ballast), Is.True);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "DryClaySpoil"), Is.SameAs(spoil));
            Assert.That(spoil.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.Zero);
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
        }

        [Test]
        public void LeaveGritKeepsAnExistingCarriedStackUntouched()
        {
            Sieve();
            var alreadyCarried = Carry("CoarseGrit");
            alreadyCarried.GetPart<StackerPart>().StackCount = 3;
            var spoil = Feed();
            var cell = Zone.GetEntityCell(Actor);
            var row = LeaveGritRow(Actions(spoil));

            Assert.That(Act(spoil, row.Command), Is.True);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "CoarseGrit"), Is.SameAs(alreadyCarried));
            Assert.That(Held("CoarseGrit"), Is.EqualTo(3));
            var loose = cell.Objects.Single(e => e.BlueprintName == "CoarseGrit");
            Assert.That(loose.ID, Is.Not.EqualTo(alreadyCarried.ID));
            Assert.That(loose.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
        }

        [Test]
        public void LateMovePreservesTheGroundedResultAndDoesNotAlsoRefundTheSpoil()
        {
            Sieve();
            var spoil = Feed(2);
            var cell = Zone.GetEntityCell(Actor);
            var row = LeaveGritRow(Actions(spoil));
            Entity moved = null;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                moved = cell.Objects.Single(e => e.BlueprintName == "CoarseGrit");
                Assert.That(Zone.AddEntity(moved, cell.X + 1, cell.Y), Is.True);
            } });

            Assert.That(Act(spoil, row.Command), Is.False);
            Assert.That(moved, Is.Not.Null);
            Assert.That(Zone.GetEntityCell(moved), Is.SameAs(Zone.GetCell(cell.X + 1, cell.Y)));
            Assert.That(Count("CoarseGrit"), Is.EqualTo(1));
            Assert.That(Held("FireClay"), Is.Zero);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "DryClaySpoil"), Is.SameAs(spoil));
            Assert.That(Held("DryClaySpoil"), Is.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void FatalInterruptionRestoresSpentSpoilAndOldClayToTheirDeathSpill(int count)
        {
            Sieve();
            var oldClay = Carry("FireClay");
            var spoil = Feed(count);
            var cell = Zone.GetEntityCell(Actor);
            var row = LeaveGritRow(Actions(spoil));
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                Assert.That(Held("FireClay"), Is.EqualTo(3));
                Assert.That(cell.Objects.Count(e => e.BlueprintName == "CoarseGrit"), Is.EqualTo(1));
                Actor.GetStat("Hitpoints").BaseValue = 0;
                CombatSystem.HandleDeath(Actor, null, Zone);
            } });

            Assert.That(Act(spoil, row.Command), Is.False);
            Assert.That(CombatSystem.IsDeathHandled(Actor), Is.True);
            Assert.That(Actor.GetStatValue("Hitpoints"), Is.Zero);
            Assert.That(Zone.GetEntityCell(spoil), Is.SameAs(cell));
            Assert.That(Zone.GetEntityCell(oldClay), Is.SameAs(cell));
            Assert.That(spoil.GetPart<StackerPart>().StackCount, Is.EqualTo(count));
            Assert.That(oldClay.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Count("FireClay"), Is.EqualTo(1));
            Assert.That(Count("CoarseGrit"), Is.Zero);
            Assert.That(Pack.Objects.Contains(spoil), Is.False);
            Assert.That(Pack.Objects.Contains(oldClay), Is.False);
        }

        [Test] public void ARealSievePartitionsTheActualPacketAndRetainsTheSameTool()
        {
            var sieve=Sieve();var feed=Feed();int mass=Pack.GetCarriedWeight();Assert.True(Act(feed,Screen(feed)));
            Assert.AreEqual(0,Held("DryClaySpoil"));Assert.AreEqual(2,Held("FireClay"));Assert.AreEqual(1,Held("CoarseGrit"));
            Assert.True(Pack.Objects.Contains(sieve));Assert.AreEqual(mass,Pack.GetCarriedWeight());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ScreeningCanAddPlainClayToTheExactMarkedStackWithoutChangingItsOrigin(bool interrupted)
        {
            var sieve = Sieve();
            var clay = Carry("FireClay");
            string key = System.Guid.NewGuid().ToString("N");
            Assert.True(ConnectedSpreadProgress.BindClay(clay, key, 1));
            var origin = clay.GetPart<ConnectedClayOriginPart>();
            var feed = Feed();
            int mass = Pack.GetCarriedWeight();
            string action = Screen(feed);
            bool sawOutput = false;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                sawOutput = true;
                Assert.That(Pack.Objects.Single(e => e.BlueprintName == "FireClay"), Is.SameAs(clay));
                Assert.That(Held("FireClay"), Is.EqualTo(3));
                Assert.That(origin.Units, Is.EqualTo(2));
            } });
            if (interrupted) FailAfter();

            Assert.That(Act(feed, action), Is.EqualTo(!interrupted));
            Assert.True(sawOutput);
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "FireClay"), Is.SameAs(clay));
            Assert.That(Held("FireClay"), Is.EqualTo(interrupted ? 1 : 3));
            Assert.That(Held("DryClaySpoil"), Is.EqualTo(interrupted ? 1 : 0));
            Assert.That(Held("CoarseGrit"), Is.EqualTo(interrupted ? 0 : 1));
            Assert.That(clay.GetPart<ConnectedClayOriginPart>(), Is.SameAs(origin));
            Assert.That(origin.Units, Is.EqualTo(2));
            Assert.That(origin.WorldKey, Is.EqualTo(key));
            Assert.That(origin.OwnerID, Is.EqualTo(clay.ID));
            Assert.Contains(sieve, Pack.Objects);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(mass));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ScreeningRollbackPreservesTheMovedMarkedStackWithoutDuplicatingTheFeed(bool independentlyAltered)
        {
            var sieve = Sieve();
            var clay = Carry("FireClay");
            string key = System.Guid.NewGuid().ToString("N");
            Assert.True(ConnectedSpreadProgress.BindClay(clay, key, 1));
            var origin = clay.GetPart<ConnectedClayOriginPart>();
            var stack = clay.GetPart<StackerPart>();
            var feed = Feed();
            string action = Screen(feed);
            bool sawOutput = false;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                sawOutput = true;
                Assert.That(stack.StackCount, Is.EqualTo(3));
                Assert.That(origin.Units, Is.EqualTo(2));
                Assert.That(Held("CoarseGrit"), Is.EqualTo(1));
                Assert.True(Pack.RemoveObject(clay));
                Assert.True(Zone.AddEntity(clay, 13, 10));
                if (independentlyAltered) clay.GetPart<PhysicsPart>().Weight = 2;
            } });

            Assert.False(Act(feed, action));
            Assert.True(sawOutput);
            Assert.That(Zone.GetEntityPosition(clay), Is.EqualTo((13, 10)));
            Assert.That(Zone.GetCell(13, 10).Objects.Single(e => e.BlueprintName == "FireClay"), Is.SameAs(clay));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.That(clay.GetPart<ConnectedClayOriginPart>(), Is.SameAs(origin));
            Assert.That(origin.OwnerID, Is.EqualTo(clay.ID));
            Assert.That(origin.WorldKey, Is.EqualTo(key));
            Assert.That(origin.Units, Is.EqualTo(2));
            Assert.That(stack.StackCount, Is.EqualTo(independentlyAltered ? 3 : 1));
            Assert.That(clay.GetPart<PhysicsPart>().Weight, Is.EqualTo(independentlyAltered ? 2 : 1));
            Assert.That(Held("FireClay"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.Zero);
            Assert.That(Pack.Objects.SingleOrDefault(e => e.BlueprintName == "DryClaySpoil"), Is.SameAs(independentlyAltered ? null : feed));
            Assert.Contains(sieve, Pack.Objects);
        }

        [Test] public void DerivedScreeningNeverRegistersAPerBlueprintRecipe()
        {Sieve();var feed=Feed();Assert.IsNull(MaterialProcessRegistry.Get("screen_size_grades"));Assert.True(Act(feed,Screen(feed)));Assert.IsNull(MaterialProcessRegistry.Get("screen_size_grades"));}
        [Test] public void ASecondCanonicalMaterialLotUsesTheSameSizeRule()
        {
            Sieve();Feed();var source=Factory.Blueprints["DryClaySpoil"];var bp=new Blueprint{Name="KnownSaltGradingLot",Baked=true};
            foreach(var p in source.Parts)bp.Parts[p.Key]=new Dictionary<string,string>(p.Value);foreach(var p in source.Tags)bp.Tags[p.Key]=p.Value;
            bp.Parts["SiftableStock"]["Contents"]="PaleSalt:1:0;CoarseGrit:2:2";bp.Parts["Commerce"]["Value"]="16";Factory.Blueprints[bp.Name]=bp;
            var second=Carry(bp.Name);Assert.True(Act(second,Screen(second)));Assert.AreEqual(1,Held("PaleSalt"));Assert.AreEqual(2,Held("CoarseGrit"));Assert.AreEqual(1,Held("DryClaySpoil"));
        }
        [Test] public void NoSieveLeavesTheLotAndItsOrdinaryTradeAlternativeIntact()
        {var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(Pack.Objects.Contains(feed));Assert.Greater(feed.GetPart<CommercePart>().Value,0);}
        [Test] public void ACombatKnifeDoesNotPretendToBeAMesh()
        {Carry("Dagger");var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));}
        [Test] public void RepeatedCurrentMenuReadsRetainTheExactDisplayedQuote()
        {Sieve();var feed=Feed();string command=Screen(feed);Actions(feed);Assert.True(Act(feed,command));Assert.AreEqual(2,Held("FireClay"));}
        [Test] public void AChangedLiveManifestCannotReuseTheOldQuote()
        {Sieve();var feed=Feed();string command=Screen(feed);SetContents(feed,"FireClay:1:0;CoarseGrit:2:2");Assert.False(Act(feed,command));Assert.AreEqual(1,Held("DryClaySpoil"));Assert.AreEqual(0,Held("FireClay"));}
        [Test] public void AChangedCanonicalManifestCannotReuseTheOldQuote()
        {Sieve();var feed=Feed();string command=Screen(feed);Contents("FireClay:1:0;CoarseGrit:2:2");Assert.False(Act(feed,command));Assert.AreEqual(0,Held("CoarseGrit"));}
        [Test] public void LostActualSieveRefusesWithoutSpendingThePacket()
        {var sieve=Sieve();var feed=Feed();string command=Screen(feed);Assert.True(Pack.RemoveObject(sieve));Assert.False(Act(feed,command));Assert.AreEqual(1,Held("DryClaySpoil"));}
        [Test] public void ExplicitProcessPermissionDenialWinsOverTheStockAdapter()
        {Sieve();var feed=Feed();feed.AddPart(new MaterialProcessInputPart{ProcessIds=""});Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));}
        [Test] public void UnknownPrivateStateCannotBeErasedByScreening()
        {Sieve();var feed=Feed();feed.AddPart(new GritPrivatePayloadProbe());Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(feed.HasPart<GritPrivatePayloadProbe>());}
        [Test] public void ActiveBrokenStateRefusesAndStaysAttached()
        {Sieve();var feed=Feed();Assert.True(feed.ApplyEffect(new BrokenEffect()));Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(feed.HasEffect<BrokenEffect>());}
        [TestCase("FireClay:2:1;CoarseGrit:1:1")]
        [TestCase("FireClay:2:0;CoarseGrit:1:1")]
        [TestCase("FireClay:2:2;CoarseGrit:1:2")]
        [TestCase("FireClay:1:0;FireClay:2:2")]
        public void OverlapOrAnUnchangedSingleGradeDoesNotPretendToSeparateIdentities(string contents)
        {Sieve();Assert.True(Factory.Blueprints.ContainsKey("DryClaySpoil"));Contents(contents);var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(Pack.Objects.Contains(feed));}
        [TestCase("")][TestCase("FireClay:2.5:0;CoarseGrit:1:2")]
        [TestCase("FireClay:0:0;CoarseGrit:1:2")][TestCase("FireClay:-1:0;CoarseGrit:1:2")]
        [TestCase("FireClay:64:0;CoarseGrit:1:2")][TestCase("FireClay:2:3;CoarseGrit:1:2")]
        [TestCase("DryClaySpoil:2:0;CoarseGrit:1:2")][TestCase("UnknownMineral:2:0;CoarseGrit:1:2")]
        public void MalformedUnboundedOrSelfProducingStockHasNoExecutableQuote(string contents)
        {Sieve();Assert.True(Factory.Blueprints.ContainsKey("DryClaySpoil"));Contents(contents);var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(Pack.Objects.Contains(feed));}
        [Test] public void AManifestCannotCreateMoreMassThanTheAuthoredPacket()
        {Sieve();Assert.True(Factory.Blueprints.ContainsKey("DryClaySpoil"));Contents("FireClay:3:0;CoarseGrit:1:2");var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));}
        [TestCase("FireClay")][TestCase("DryClaySpoil")]
        public void CanonicalHandlingWeightMustAlsoBalance(string blueprint)
        {
            Sieve();Factory.Blueprints[blueprint].Parts["Handling"]=new Dictionary<string,string>{{"Weight","100"}};
            var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)),"The actual handling mass must balance for source and products.");
        }
        [Test] public void TwoPacketsRemainFiniteAcrossTwoPasses()
        {
            Sieve();var feed=Feed(2);Assert.True(Act(feed,Screen(feed)));Assert.AreEqual(1,feed.GetPart<StackerPart>().StackCount);
            Assert.True(Act(feed,Screen(feed)));Assert.AreEqual(0,Held("DryClaySpoil"));Assert.AreEqual(4,Held("FireClay"));Assert.AreEqual(2,Held("CoarseGrit"));
            Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));
        }
        [Test] public void SplittingStockPreservesItsManifestAndTotalYield()
        {
            Sieve();var feed=Feed(2);var split=feed.GetPart<StackerPart>().SplitStack(1);Assert.NotNull(split);
            Assert.AreEqual(Stock(feed).GetType().GetField("Contents").GetValue(Stock(feed)),Stock(split).GetType().GetField("Contents").GetValue(Stock(split)));
            Assert.True(Pack.AddObject(split));Assert.AreEqual(2,Held("DryClaySpoil"));Assert.True(Act(feed,Screen(feed)));Assert.AreEqual(1,Held("DryClaySpoil"));
        }
        [Test] public void PrivatePayloadCannotBeDroppedWhenPermissionIsRemovedBeforeSplitting()
        {
            var feed=Feed(2);feed.RemovePart(Stock(feed));feed.AddPart(new GritPrivatePayloadProbe());
            Assert.False(feed.GetPart<StackerPart>().CanSplit());Assert.IsNull(feed.GetPart<StackerPart>().SplitStack(1));Assert.AreEqual(2,feed.GetPart<StackerPart>().StackCount);
        }
        [Test] public void VetoReturnsTheInputAndRetractsItsProducts()
        {Sieve();var feed=Feed();string command=Screen(feed);FailAfter();Assert.False(Act(feed,command));Assert.AreEqual(1,Held("DryClaySpoil"));Assert.AreEqual(0,Held("FireClay"));Assert.AreEqual(0,Held("CoarseGrit"));}
        [Test] public void VetoDoesNotHealIndependentInjury()
        {
            Sieve();var feed=Feed();string command=Screen(feed);int hp=Actor.GetStat("Hitpoints").BaseValue;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Actor.GetStat("Hitpoints").BaseValue-=3});FailAfter();
            Assert.False(Act(feed,command));Assert.AreEqual(hp-3,Actor.GetStat("Hitpoints").BaseValue);Assert.AreEqual(1,Held("DryClaySpoil"));
        }
        [Test] public void LateCapacityLossPreservesIndependentStockAndRefusesOutputs()
        {
            Sieve();var feed=Feed();int initial=Pack.GetCarriedWeight();Pack.MaxWeight=initial;string command=Screen(feed);Entity added=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{Pack.MaxWeight=200;added=Carry("SalvagedTimber");Pack.MaxWeight=initial;}});
            Assert.False(Act(feed,command));Assert.True(Pack.Objects.Contains(added));Assert.AreEqual(1,Held("DryClaySpoil"));Assert.AreEqual(0,Held("FireClay"));
        }
        [Test] public void NativeSaveRetainsTheActualManifestAndRemainingPacket()
        {Sieve();var feed=Feed(2);Assert.True(Act(feed,Screen(feed)));var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(feed);Assert.AreEqual(1,saved.GetPart<StackerPart>().StackCount);Assert.AreEqual(Stock(feed).GetType().GetField("Contents").GetValue(Stock(feed)),Stock(saved).GetType().GetField("Contents").GetValue(Stock(saved)));}
        [Test] public void LegacyQuarryScreeningKeepsItsExistingExactFractions()
        {Sieve();var feed=Carry("QuarrySweepings");Assert.True(Act(feed,Choice(feed,"MaterialProcess|screen_quarry_sweepings|")));Assert.AreEqual(1,Held("PaleSalt"));Assert.AreEqual(2,Held("SilverSand"));}
        [Test] public void SharedMaterialWorkRejectsALateRemainingSourceAlias()
        {
            Sieve();var feed=Carry("QuarrySweepings");feed.GetPart<StackerPart>().StackCount=2;string command=Choice(feed,"MaterialProcess|screen_quarry_sweepings|");Entity alias=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{Assert.True(Pack.Objects.Remove(feed));alias=Factory.CreateEntity("QuarrySweepings");alias.ID=feed.ID;alias.GetPart<PhysicsPart>().InInventory=Actor;Pack.Objects.Add(alias);}});
            Assert.False(Act(feed,command),"A replacement with the same ID cannot stand in for the selected source.");
            Assert.True(Pack.Objects.Contains(alias),"Keep the independent callback's replacement.");
            Assert.False(Pack.Objects.Contains(feed),"Do not reinsert a conflicting saved identity during limited rollback.");
            Assert.AreEqual(1,Pack.Objects.Count(e=>e.ID==feed.ID));Assert.AreEqual(0,Held("PaleSalt"));Assert.AreEqual(0,Held("SilverSand"));
        }
    }
}
