using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The shade awning (Docs/MACHINE-INVENTED-USES.md 3o): shade as a capability. A standing thing that carries ShadePart shades its own cell and the ring around it, and the Beating's glare system
    /// counts that as relief by the same rule as a roof or a hat; carried, dropped or taken up it shades nothing.
    /// </summary>
    public sealed class ShadeAwningTests : FiftyWorldFixture
    {
        const string BeatingZoneId = "Overworld.16.16.0";   // deep Beating on the authored map
        const int HeightTick = 350;                          // Height band = 300-599
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 30, AY = 12;

        [SetUp] public void SetupShade()
        {
            PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); BeatingGlareSystem.ResetForTests(); Pack.MaxWeight = 400;
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild;
        }
        [TearDown] public void TearDownShade() { PackableFixtureRegistry.ResetForTests(); BeatingGlareSystem.ResetForTests(); }

        // ── helpers ──

        /// <summary>An awning that is standing the way a real set-down leaves it: a Player-origin marker and an installed, portable fixture state.</summary>
        Entity Stand(Zone zone, int x, int y, int radius = 1)
        {
            var awning = Factory.CreateEntity("CampAwning"); awning.GetPart<ShadePart>().Radius = radius;
            awning.AddPart(new InstallationMarkerPart { InstallationId = "player:" + awning.ID, Origin = HeldFixtureActions.PlayerOrigin, RetainZone = true });
            var inst = awning.GetPart<InstallablePart>() ?? new InstallablePart(); inst.Mode = InstallablePart.Portable; inst.State = InstallablePart.Installed; if (awning.GetPart<InstallablePart>() == null) awning.AddPart(inst);
            Assert.True(zone.AddEntity(awning, x, y)); return awning;
        }
        static Entity Traveller(Zone zone, int x = 10, int y = 10)
        {
            var e = new Entity { ID = "traveller", BlueprintName = "Player" }; e.Tags["Creature"] = ""; e.AddPart(new RenderPart { DisplayName = "you" }); e.AddPart(new PhysicsPart());
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 16 }; e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 16 };
            zone.AddEntity(e, x, y); return e;
        }
        static void Expose(Entity player, Zone zone, int turns) { for (int i = 0; i < turns; i++) BeatingGlareSystem.OnPlayerTurnEnd(player, zone, HeightTick); }
        static int Stacks(Entity e) => e.GetPart<StatusEffectsPart>()?.GetEffect<ParchedEffect>()?.Stacks ?? 0;
        bool Covers(Zone zone, int x, int y) => ShadePart.Covers(zone, zone.GetCell(x, y));
        InventoryAction[] SetDownRows(Entity kit) => Actions(kit).Where(a => a.Name == "SetDownFixture" && !a.IsDisabled).ToArray();

        // ── the capability ──

        [Test] public void AnAwningShadesItsOwnCellAndTheRingAroundItAndNoFurther()
        {
            var zone = new Zone(BeatingZoneId); Stand(zone, 10, 10);
            Assert.True(Covers(zone, 10, 10)); Assert.True(Covers(zone, 11, 11)); Assert.True(Covers(zone, 9, 9)); Assert.False(Covers(zone, 12, 10), "two cells away is out of the shade"); Assert.False(Covers(zone, 10, 13));
        }

        [Test] public void TheRadiusIsTheDataAndIsCappedAtTwo()
        {
            var zone = new Zone(BeatingZoneId); Stand(zone, 10, 10, radius: 2);
            Assert.True(Covers(zone, 12, 12)); Assert.False(Covers(zone, 13, 10)); Stand(new Zone("Overworld.16.17.0"), 20, 12, radius: 9);
            var far = new Zone("Overworld.16.18.0"); Stand(far, 20, 12, radius: 9); Assert.True(Covers(far, 22, 12)); Assert.False(Covers(far, 23, 12), "a radius of nine is still two");
        }

        [Test] public void ACarriedDroppedOrTakenUpAwningShadesNothing_Counter()
        {
            var zone = new Zone(BeatingZoneId);
            var carried = Factory.CreateEntity("CampAwning"); Assert.False(carried.GetPart<ShadePart>().Shading, "in a pack, or anywhere with no zone"); Assert.True(Pack.AddObject(carried)); Assert.False(carried.GetPart<ShadePart>().Shading);
            var dropped = Factory.CreateEntity("CampAwning"); Assert.True(zone.AddEntity(dropped, 10, 10)); Assert.False(Covers(zone, 10, 10), "a kit lying on the ground is inert: it is not set down");
            var loose = Stand(zone, 20, 12); loose.GetPart<InstallablePart>().State = InstallablePart.Loose; Assert.False(Covers(zone, 20, 12), "a taken-up (loose) kit shades nothing");
            Assert.False(Covers(zone, 30, 12), "and a cell with nothing near it is not shaded"); Assert.False(ShadePart.Covers(null, null));
        }

        // ── the glare ──

        [Test] public void StandingInTheShadeOfAnAwningKeepsTheGlareOff_AndStandingInTheSunDoesNot()
        {
            var zone = new Zone(BeatingZoneId); var sun = Traveller(zone, 30, 5); Expose(sun, zone, 30); Assert.AreEqual(3, Stacks(sun), "fixture: thirty bareheaded turns in the open is badly parched");
            BeatingGlareSystem.ResetForTests(); var shaded = Traveller(zone, 10, 10); zone.RemoveEntity(sun); Stand(zone, 11, 10);
            Expose(shaded, zone, 60); Assert.AreEqual(0, Stacks(shaded), "sixty turns of the Height hours in the awning's shade and not a mark");
        }

        [Test] public void LeavingTheShadeStartsTheCountAgainAndShadeBetweenExposureResetsIt()
        {
            var zone = new Zone(BeatingZoneId); var t = Traveller(zone, 10, 10); Stand(zone, 11, 10);
            Expose(t, zone, 20); Assert.AreEqual(0, Stacks(t));
            zone.RemoveEntity(t); zone.AddEntity(t, 30, 5); Expose(t, zone, 9); Assert.AreEqual(0, Stacks(t), "nine exposed turns is not yet a stack");
            zone.RemoveEntity(t); zone.AddEntity(t, 10, 10); Expose(t, zone, 1);   // one turn in the shade resets the streak
            zone.RemoveEntity(t); zone.AddEntity(t, 30, 5); Expose(t, zone, 9); Assert.AreEqual(0, Stacks(t), "shade is relief, not a pause button");
            Expose(t, zone, 1); Assert.AreEqual(1, Stacks(t), "ten in a row out in the sun parches one stack");
        }

        [Test] public void AHatAndARoofStillAnswerTheGlareAndAnAwningTakenUpAnswersNothing_Counters()
        {
            var zone = new Zone(BeatingZoneId); var t = Traveller(zone, 30, 5); var awning = Stand(zone, 31, 5); Expose(t, zone, 40); Assert.AreEqual(0, Stacks(t), "fixture: shaded");
            awning.GetPart<InstallablePart>().State = InstallablePart.Loose; BeatingGlareSystem.ResetForTests(); Expose(t, zone, 40); Assert.AreEqual(3, Stacks(t), "the same awning taken up shades no one");
            var roofed = Traveller(zone, 40, 5); roofed.GetPart<PhysicsPart>(); zone.GetCell(40, 5).IsInterior = true; BeatingGlareSystem.ResetForTests(); Expose(roofed, zone, 40); Assert.AreEqual(0, Stacks(roofed), "a roof is still a roof");
        }

        // ── set down, take up, content ──

        [Test] public void TheSetDownVerbStandsTheAwningAndTakingItUpEndsTheShade()
        {
            var kit = Carry("CampAwning"); var row = SetDownRows(kit).First(r => r.Display.StartsWith("set down east")); Assert.True(Act(kit, row.Command), string.Join("|", MessageLog.GetRecent(3)));
            var cell = wild.GetEntityCell(kit); Assert.True(HeldFixtureActions.IsPlayerHeld(kit)); Assert.True(ShadePart.Covers(wild, wild.GetCell(cell.X + 1, cell.Y)), "the ring around it is shaded once it stands");
            StringAssert.Contains("shades the ground under it", Read(kit));
            var take = WorldInteractionSystem.GatherActions(kit, Actor).Single(a => a.Name == "TakeUpFixture"); Assert.True(Act(kit, take.Command)); Assert.False(ShadePart.Covers(wild, wild.GetCell(cell.X, cell.Y)), "taken up it shades nothing");
            StringAssert.Contains("Carried, it shades nothing", Read(kit));
        }

        string Read(Entity e) { var l = new List<string>(); StateReader.Collect(e, true, wild, wild.GetEntityCell(e), Actor, l); return string.Join(" ", l); }

        [Test] public void TheFullReaderTellsAViewerStandingInItsShadeSoAndOnlyThen_Counter()
        {
            var awning = Stand(wild, AX + 1, AY);                    // the actor stands beside it: inside the ring
            StringAssert.Contains("You are standing in it", Read(awning));
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, AX - 3, AY)); StringAssert.DoesNotContain("You are standing in it", Read(awning), "three cells off is outside the ring");
            var wide = Stand(wild, 10, 5, radius: 2); wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, 12, 7));
            StringAssert.Contains("You are standing in it", Read(wide), "a wider awning reaches two cells");
            wide.GetPart<InstallablePart>().State = InstallablePart.Loose; StringAssert.DoesNotContain("You are standing in it", Read(wide), "taken up it shades no one, so it says nothing of the sort");
        }

        [Test] public void TheKitIsRegisteredStockedAndShadeIsASavedCapability()
        {
            var def = PackableFixtureRegistry.Get("camp-awning"); Assert.NotNull(def); Assert.AreEqual(PlacementCheck.OpenGround, def.GroundClass); Assert.False(def.Solid);
            var made = Factory.CreateEntity("CampAwning"); Assert.NotNull(made.GetPart<ShadePart>()); Assert.True(made.GetPart<PhysicsPart>().Takeable); Assert.AreEqual("camp-awning", made.GetPart<PackableFixturePart>().Kind);
            Assert.True(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")).Contains("\"CampAwning\""), "the Tinker stocks it");
            made.GetPart<ShadePart>().Radius = 2; var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(made).GetPart<ShadePart>(); Assert.AreEqual(2, copy.Radius);
        }
    }
}
