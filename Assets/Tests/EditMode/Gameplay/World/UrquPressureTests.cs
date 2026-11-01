using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P5 B2 (Docs/UPPER-TIER-BESTIARY.md §3.1): where Urqu is active. The Felling-Site's
    /// gradient, local manifestations born of mass death, the story flag, the regional
    /// population gate, and the fauna waiting on arrival.
    /// </summary>
    public sealed class UrquPressureTests
    {
        static EntityFactory factory;
        TurnManager previousTurns;
        Entity player;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            Diag.ResetAll(); MessageLog.Clear(); FactionManager.Initialize();
            NarrativeStatePart.Current = new NarrativeStatePart();
            ConversationActions.Factory = factory;
            previousTurns = TurnManager.Active;
            SetTick(1000);
            player = factory.CreateEntity("Player");
        }
        [TearDown] public void Cleanup()
        {
            typeof(TurnManager).GetProperty("Active").SetValue(null, previousTurns);
            NarrativeStatePart.Current = null; ConversationActions.Factory = null;
        }

        static void SetTick(int tick)
        {
            var turns = TurnManager.Active ?? new TurnManager();
            typeof(TurnManager).GetProperty("TickCount").SetValue(turns, tick);
            typeof(TurnManager).GetProperty("Active").SetValue(null, turns);
        }

        static IEnumerable<(int x, int y)> Cells(System.Func<int, int, bool> where)
        {
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
                if (WorldMapAuthoring.InBounds(x, y) && where(x, y)) yield return (x, y);
        }
        static string Z(int x, int y, int z = 0) => WorldMap.ToZoneID(x, y, z);
        static int Chebyshev(int x, int y) => System.Math.Max(System.Math.Abs(x - FellingSiteBuilder.WorldX), System.Math.Abs(y - FellingSiteBuilder.WorldY));
        static bool Authored(int x, int y)
            => (x == FellingSiteBuilder.WorldX && y == FellingSiteBuilder.WorldY) || (x == RootSiteBuilder.WorldX && y == RootSiteBuilder.WorldY)
               || WorldMapAuthoring.Places.Any(p => p.X == x && p.Y == y) || SinkholeSites.All.Any(s => s.X == x && s.Y == y);

        Entity Person(Zone zone, int x, int y)
        {
            var e = factory.CreateEntity("Villager"); zone.AddEntity(e, x, y); return e;
        }

        // ════════════════ the gradient ════════════════

        [Test] public void TheFellingSitesGradientIsAlwaysActiveAndOnlyOnTheStumpNearIt()
        {
            var near = Cells((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump && Chebyshev(x, y) <= UrquPressure.GradientRadius && !Authored(x, y)).ToList();
            var far = Cells((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump && Chebyshev(x, y) > UrquPressure.GradientRadius).ToList();
            var sites = Cells((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump && Chebyshev(x, y) <= UrquPressure.GradientRadius && Authored(x, y)).ToList();
            Assert.IsNotEmpty(near); Assert.IsNotEmpty(far); Assert.IsNotEmpty(sites, "precondition: authored sites lie in the gradient");
            foreach (var (x, y) in sites) Assert.False(UrquPressure.IsActiveAt(Z(x, y)), "authored sites keep their own flags: " + Z(x, y));
            foreach (var (x, y) in near) { Assert.True(UrquPressure.IsActiveAt(Z(x, y)), Z(x, y)); Assert.True(UrquPressure.IsActiveAt(Z(x, y, 3)), "and beneath it"); }
            foreach (var (x, y) in far) Assert.False(UrquPressure.IsActiveAt(Z(x, y)), "counter-check: " + Z(x, y));
            var offStump = Cells((x, y) => WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Stump && Chebyshev(x, y) <= UrquPressure.GradientRadius);
            foreach (var (x, y) in offStump) Assert.False(UrquPressure.IsActiveAt(Z(x, y)), "the gradient is the Stump's");
            NarrativeStatePart.Current = null;
            Assert.True(UrquPressure.IsActiveAt(Z(near[0].x, near[0].y)), "geography needs no state");
        }

        [Test] public void TheStoryFlagWakesItEverywhere()
        {
            Assert.False(UrquPressure.IsActiveAt("Overworld.10.10.0"));
            NarrativeStatePart.Current.SetFact(UrquPressure.StoryFact, 1);
            Assert.True(UrquPressure.IsActiveAt("Overworld.10.10.0"));
        }

        [Test] public void MalformedZonesAreNeverActive()
        {
            foreach (var id in new[] { null, "", "nowhere", "Overworld.99.99.0", "density-combat" })
                Assert.False(UrquPressure.IsActiveAt(id), "'" + id + "'");
        }

        // ════════════════ mass death ════════════════

        [Test] public void FourDeadInASettlementManifestTheirAndTheNeighbouringCellsForThreeDays()
        {
            var sill = new Zone("Overworld.10.10.0");
            for (int i = 0; i < UrquPressure.SettlementDeaths - 1; i++) CombatSystem.HandleDeath(Person(sill, 5 + i, 5), player, sill);
            Assert.False(UrquPressure.IsActiveAt("Overworld.10.10.0"), "counter-check: three is a tragedy, not a manifestation");
            CombatSystem.HandleDeath(Person(sill, 20, 5), player, sill);
            Assert.True(UrquPressure.IsActiveAt("Overworld.10.10.0"));
            Assert.True(UrquPressure.IsActiveAt("Overworld.11.11.0"), "and the eight around it");
            Assert.False(UrquPressure.IsActiveAt("Overworld.12.10.0"), "but not two cells out");
            Assert.True(DiagQuery.Apply(new DiagQuery.Filter { Category = "urqu", Kind = "Manifested", Limit = 3 }).Records.Any());
            SetTick(1000 + UrquPressure.ManifestDays * WorldClock.DayLengthTicks + 1);
            Assert.False(UrquPressure.IsActiveAt("Overworld.10.10.0"), "three days, then quiet");
        }

        [Test] public void BeastsDoNotCountAndTheWildsNeedMore()
        {
            var sill = new Zone("Overworld.10.10.0");
            for (int i = 0; i < 6; i++) { var b = factory.CreateEntity("BrittleHound"); sill.AddEntity(b, 5 + i, 6); CombatSystem.HandleDeath(b, player, sill); }
            Assert.False(UrquPressure.IsActiveAt("Overworld.10.10.0"), "beasts are not a mass death");
            var wild = new Zone("Overworld.11.6.0");
            Assume.That(PlaceSiteCatalog.PlaceAt("Overworld.11.6.0"), Is.Null, "precondition: open country");
            for (int i = 0; i < UrquPressure.AnyZoneDeaths - 1; i++) CombatSystem.HandleDeath(Person(wild, 3 + i, 4), player, wild);
            Assert.False(UrquPressure.IsActiveAt("Overworld.11.6.0"));
            CombatSystem.HandleDeath(Person(wild, 3, 9), player, wild);
            Assert.True(UrquPressure.IsActiveAt("Overworld.11.6.0"));
        }

        [Test] public void DeathsOnDifferentDaysDoNotAddUp()
        {
            var sill = new Zone("Overworld.10.10.0");
            for (int i = 0; i < UrquPressure.SettlementDeaths - 1; i++) CombatSystem.HandleDeath(Person(sill, 5 + i, 5), player, sill);
            SetTick(1000 + WorldClock.DayLengthTicks);
            CombatSystem.HandleDeath(Person(sill, 20, 5), player, sill);
            Assert.False(UrquPressure.IsActiveAt("Overworld.10.10.0"), "a day apart is two griefs, not one catastrophe");
        }

        [Test] public void ALaterManifestationExtendsButNeverShortens()
        {
            UrquPressure.Manifest(10, 10, "test");
            int first = NarrativeStatePart.Current.GetFact("urqu.manifest.Overworld.10.10.0");
            SetTick(500);
            UrquPressure.Manifest(10, 10, "test");
            Assert.AreEqual(first, NarrativeStatePart.Current.GetFact("urqu.manifest.Overworld.10.10.0"));
            SetTick(2000);
            UrquPressure.Manifest(10, 10, "test");
            Assert.Greater(NarrativeStatePart.Current.GetFact("urqu.manifest.Overworld.10.10.0"), first);
        }

        // ════════════════ population ════════════════

        [Test] public void TheStumpsUrquFaunaRollsOnlyWhereUrquIsActive()
        {
            var near = Cells((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump && Chebyshev(x, y) <= UrquPressure.GradientRadius && !Authored(x, y)).First();
            var far = Cells((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump && Chebyshev(x, y) > UrquPressure.GradientRadius).First();
            var entry = new PopulationEntry { BlueprintName = "SariSnake", Weight = 1, MinCount = 1, MaxCount = 1, RequiresWorldFlag = UrquPressure.StoryFact };
            Assert.True(entry.AllowedByWorldState(Z(near.x, near.y)));
            Assert.False(entry.AllowedByWorldState(Z(far.x, far.y)), "counter-check: quiet slopes");
            var other = new PopulationEntry { BlueprintName = "X", Weight = 1, RequiresWorldFlag = "SomethingElse" };
            Assert.False(other.AllowedByWorldState(Z(near.x, near.y)), "other flags keep their own fact");
        }

        [Test] public void ARealRollAdmitsTheGatedEntryOnlyInAnActiveZone()
        {
            var near = Cells((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump && Chebyshev(x, y) <= UrquPressure.GradientRadius && !Authored(x, y)).First();
            var table = new PopulationTable { Name = "urqu roll" };
            table.Entries.Add(new PopulationEntry { BlueprintName = "SariSnake", Weight = 1, MinCount = 2, MaxCount = 2, RequiresWorldFlag = UrquPressure.StoryFact });
            Assert.AreEqual(2, table.Roll(new System.Random(1), Z(near.x, near.y)).Count(n => n == "SariSnake"), "the roll asks about the zone it fills");
            Assert.AreEqual(0, table.Roll(new System.Random(1), "Overworld.11.6.0").Count, "counter-check: quiet country");
        }

        // ════════════════ arrival fauna ════════════════

        [Test] public void ArrivingWhereUrquIsActiveFindsItsSignsOnceAndAgainAtTheNextManifestation()
        {
            var zone = new Zone("Overworld.11.6.0"); zone.AddEntity(player, 40, 12);
            Assert.AreEqual(0, UrquPressure.OnZoneEntered(player, zone), "counter-check: quiet country is quiet");
            UrquPressure.Manifest(11, 6, "test");
            int placed = UrquPressure.OnZoneEntered(player, zone);
            Assert.GreaterOrEqual(placed, 2);
            Assert.True(zone.GetAllEntities().Count(e => e.BlueprintName == "SariSnake") >= 2);
            Assert.AreEqual(0, UrquPressure.OnZoneEntered(player, zone), "once per manifestation");
            SetTick(1000 + 4 * WorldClock.DayLengthTicks);
            UrquPressure.Manifest(11, 6, "test");
            Assert.Greater(UrquPressure.OnZoneEntered(player, zone), 0, "a new manifestation brings new signs");
        }

        [Test] public void TheSkySariCirclesOnlyOverTheStumpAndTheBeating()
        {
            var spread = new Zone("Overworld.11.6.0"); spread.AddEntity(player, 40, 12);
            Assume.That(WorldMapAuthoring.BiomeAt(11, 6), Is.Not.EqualTo(BiomeType.Beating).And.Not.EqualTo(BiomeType.Stump));
            UrquPressure.Manifest(11, 6, "test");
            UrquPressure.OnZoneEntered(player, spread);
            Assert.False(spread.GetAllEntities().Any(e => e.BlueprintName == "SkySari"));
            var (bx, by) = Cells((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Beating).First();
            var beating = new Zone(Z(bx, by)); spread.RemoveEntity(player); beating.AddEntity(player, 40, 12);
            UrquPressure.Manifest(bx, by, "test");
            UrquPressure.OnZoneEntered(player, beating);
            Assert.AreEqual(1, beating.GetAllEntities().Count(e => e.BlueprintName == "SkySari"));
        }
    }
}
