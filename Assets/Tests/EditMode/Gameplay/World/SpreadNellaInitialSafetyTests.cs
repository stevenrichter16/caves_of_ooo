using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>Cold placement must not publish the required keeper inside an
    /// already hostile creature's acquisition range or inside the normal-speed
    /// southern approach margin. Later roaming/combat and literal cached
    /// worlds remain ordinary gameplay.</summary>
    public sealed class SpreadNellaInitialSafetyTests
    {
        const string Nella = "Overworld.11.8.0";
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        readonly List<(FieldInfo field, object original)> borrowed = new List<(FieldInfo, object)>();
        HaulingContentScope scope;

        [SetUp] public void SetUp()
        {
            foreach (string name in new[] { "_factionFeelings", "_registeredFactions", "_factionData" })
                Replace(typeof(FactionManager), name);
            Replace(typeof(PlayerReputation), "_reputation");
            scope = new HaulingContentScope();
            FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Factions.json")));
        }

        void Replace(Type type, string name)
        {
            var field = type.GetField(name, Static); Assert.NotNull(field);
            borrowed.Add((field, field.GetValue(null)));
            field.SetValue(null, Activator.CreateInstance(field.FieldType));
        }

        [TearDown] public void TearDown()
        {
            try { scope?.Dispose(); }
            finally
            {
                for (int i = borrowed.Count - 1; i >= 0; i--)
                    borrowed[i].field.SetValue(null, borrowed[i].original);
                borrowed.Clear();
            }
        }

        static int Distance((int x, int y) a, (int x, int y) b)
            => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));

        static Entity Keeper(Zone zone)
            => zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpreadSeedKeeper");

        static void OutsideInitialAcquisition(Zone zone, Entity keeper)
        {
            var at = zone.GetEntityPosition(keeper);
            foreach (var other in zone.GetReadOnlyEntities().Where(e => e != keeper && e.HasTag("Creature")))
            {
                var brain = other.GetPart<BrainPart>();
                if (brain == null || brain.Passive || other.GetStatValue("Hitpoints") <= 0
                    || !FactionManager.IsHostile(other, keeper)) continue;
                Assert.Greater(Distance(at, zone.GetEntityPosition(other)), brain.SightRadius,
                    other.BlueprintName + " begins within acquisition range of the newly placed keeper.");
            }
        }

        static readonly (int x, int y)[] Stamp = {
            (0,0),(-2,-1),(-1,-1),(-2,1),(-1,1),(2,0),
            (-2,-3),(-1,-3),(2,-2),(1,-3)
        };

        // Independent shortest-path oracle, rather than the production Flood limit.
        // The whole stamp is conservatively avoided, including passable crop owners.
        static int ApproachSteps(Zone zone, SpreadCompositionPlan plan, (int x, int y) origin,
            HashSet<Entity> ignored, IEnumerable<(int x, int y)> footprint)
        {
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = typeof(SpreadWildernessSituationBuilder).GetNestedType("Geometry", BindingFlags.NonPublic);
            Assert.NotNull(type);
            var geometry = Activator.CreateInstance(type, instance, null, new object[] { zone, ignored }, null);
            var method = type.GetMethod("Walk", instance); Assert.NotNull(method);
            var blocked = new HashSet<(int x, int y)>(footprint);
            bool Walk(int x, int y) => (bool)method.Invoke(geometry, new object[] { x, y, blocked });
            var start = (x: plan.SouthX, y: Zone.Height - 1);
            Assert.True(Walk(start.x, start.y), "The authored southern port must be open.");
            var seen = new HashSet<(int x, int y)> { start };
            var queue = new Queue<(int x, int y, int steps)>(); queue.Enqueue((start.x, start.y, 0));
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                if (Math.Abs(p.x - origin.x) + Math.Abs(p.y - origin.y) == 1) return p.steps;
                foreach (var n in new[] { (x:p.x-1,y:p.y), (x:p.x+1,y:p.y), (x:p.x,y:p.y-1), (x:p.x,y:p.y+1) })
                    if (Walk(n.x, n.y) && seen.Add(n)) queue.Enqueue((n.x, n.y, p.steps + 1));
            }
            Assert.Fail("A legal cardinal southern approach must remain."); return -1;
        }

        static int ApproachSteps(Zone zone, SpreadCompositionPlan plan, Entity keeper)
        {
            var owners = zone.GetReadOnlyEntities().Where(e => e.Properties.ContainsKey(SpreadExplorationResidents.RoleKey)).ToHashSet();
            return ApproachSteps(zone, plan, zone.GetEntityPosition(keeper), owners, owners.Select(zone.GetEntityPosition));
        }

        static void OutsideSouthernApproachMargin(Zone zone, SpreadCompositionPlan plan, Entity keeper)
        {
            int approach = ApproachSteps(zone, plan, keeper);
            var at = zone.GetEntityPosition(keeper);
            foreach (var foe in zone.GetReadOnlyEntities().Where(e => e != keeper && e.HasTag("Creature")))
            {
                var brain = foe.GetPart<BrainPart>();
                if (brain == null || brain.Passive || foe.GetStatValue("Hitpoints") <= 0
                    || !FactionManager.IsHostile(foe, keeper)) continue;
                // Long arithmetic states the inequality without duplicating production integer division.
                long available = (long)(Distance(at, zone.GetEntityPosition(foe)) - brain.SightRadius - 1) * 100;
                long needed = (long)approach * Math.Max(100, foe.GetStatValue("Speed", 100));
                Assert.GreaterOrEqual(available, needed,
                    foe.BlueprintName + " can consume the geometric approach margin before normal-speed contact.");
            }
        }

        [TestCase(242967504)] [TestCase(224401440)] [TestCase(200893250)] [TestCase(64)] [TestCase(65)]
        public void NativeColdGenerationKeepsTheCompleteServiceOutsideInitialHostileAcquisition(int seed)
        {
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(Nella, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var zone = manager.GetZone(Nella); Assert.NotNull(zone);
            Assert.AreEqual(2, manager.Exploration.DispositionFor(Nella), "Do not solve contact failure by dropping the site.");
            var keeper = Keeper(zone);
            Assert.AreEqual(10, zone.GetReadOnlyEntities().Count(e => e.Properties.ContainsKey(SpreadExplorationResidents.RoleKey)));
            Assert.AreEqual(20, keeper.GetStatValue("Hitpoints")); Assert.True(keeper.GetPart<BrainPart>().Passive);
            Assert.AreEqual("SeedKeeperStock", keeper.GetPart<TraderPart>().StockTable);
            Assert.AreEqual(10, keeper.GetPart<InventoryPart>().Objects.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            OutsideInitialAcquisition(zone, keeper);
            var plan = SpreadCompositionPlan.Create(Nella, seed, Formation.FieldStrips,
                manager.Exploration.Entries.Single(e => e.ZoneID == Nella).Topology);
            OutsideSouthernApproachMargin(zone, plan, keeper);
            var original = zone.GetReadOnlyEntities().ToDictionary(e => e, zone.GetEntityPosition);
            manager.UnloadZone(Nella); Assert.AreSame(zone, manager.GetZone(Nella));
            Assert.AreEqual(original.Count, zone.EntityCount);
            foreach (var row in original) Assert.AreEqual(row.Value, zone.GetEntityPosition(row.Key));
        }

        (Zone zone, SpreadCompositionBuilder terrain, (int x, int y) preferred) Controlled()
        {
            scope.Seed(64); var zone = new Zone(Nella);
            var terrain = new SpreadCompositionBuilder(64)
                { FormationOverride = Formation.FieldStrips, Topology = SpreadExplorationTopology.OffsetLanes };
            Assert.True(terrain.BuildZone(zone, scope.Factory, new Random(64)));
            Assert.True(terrain.Plan.TryGetNellaFrontage(out var frontage));
            return (zone, terrain, (frontage.X, frontage.Y));
        }

        Entity Nearby(Zone zone, (int x, int y) preferred, int distance)
        {
            var actor = scope.Factory.CreateEntity("MarlbackScrabbler"); Assert.NotNull(actor);
            Assert.True(zone.AddEntity(actor, preferred.x + distance, preferred.y));
            Assert.False(actor.GetPart<BrainPart>().Passive); Assert.AreEqual(10, actor.GetPart<BrainPart>().SightRadius);
            return actor;
        }

        bool Place(Zone zone, SpreadCompositionBuilder terrain, out Entity[] added, out Func<bool> final)
            => SpreadExplorationResidents.TryPlaceConnected(zone, scope.Factory, terrain,
                SpreadExplorationFamily.SeedKeepersPlot, () => true, out added, out final, true,
                "0123456789abcdef0123456789abcdef");

        [TestCase(10)] [TestCase(11)]
        public void InitialClearanceAloneIsInsufficientForTheSouthernApproach(int distance)
        {
            var f = Controlled(); var hostile = Nearby(f.zone, f.preferred, distance);
            var original = f.zone.GetReadOnlyEntities().ToDictionary(e => e, f.zone.GetEntityPosition);
            Assert.True(Place(f.zone, f.terrain, out var added, out var final)); Assert.True(final());
            var keeper = Keeper(f.zone); Assert.True(FactionManager.IsHostile(hostile, keeper));
            Assert.AreNotEqual(f.preferred, f.zone.GetEntityPosition(keeper));
            OutsideInitialAcquisition(f.zone, keeper);
            OutsideSouthernApproachMargin(f.zone, f.terrain.Plan, keeper);
            Assert.AreEqual(original.Count + added.Length, f.zone.EntityCount);
            foreach (var row in original) Assert.AreEqual(row.Value, f.zone.GetEntityPosition(row.Key));
            Assert.AreEqual(15, hostile.GetStatValue("Hitpoints"));
            Assert.AreEqual(10, hostile.GetPart<BrainPart>().SightRadius);
        }

        [TestCase("benign")] [TestCase("passive")]
        public void UnrelatedPopulationDoesNotRejectThePreferredBay(string kind)
        {
            var f = Controlled(); var other = Nearby(f.zone, f.preferred, 8);
            if (kind == "benign") other.Tags["Faction"] = "Villagers";
            else other.GetPart<BrainPart>().Passive = true;
            Assert.True(Place(f.zone, f.terrain, out _, out var final)); Assert.True(final());
            var keeper = Keeper(f.zone);
            Assert.AreEqual(f.preferred, f.zone.GetEntityPosition(keeper), "Keep the existing preferred composition when there is no initiating threat.");
            Assert.AreEqual((f.preferred.x + 8, f.preferred.y), f.zone.GetEntityPosition(other));
            Assert.AreEqual(kind != "benign", FactionManager.IsHostile(other, keeper));
        }

        [Test] public void NoInitiatingHostileKeepsTheAcceptedPreferredBay()
        {
            var f = Controlled(); Assert.True(Place(f.zone, f.terrain, out _, out var final)); Assert.True(final());
            Assert.AreEqual(f.preferred, f.zone.GetEntityPosition(Keeper(f.zone)));
        }

        Entity DistantThreat(Zone zone, (int x, int y) origin, string blueprint)
        {
            var foe = scope.Factory.CreateEntity(blueprint); Assert.NotNull(foe);
            int x = origin.x < Zone.Width / 2 ? Zone.Width - 5 : 4;
            Assert.True(zone.AddEntity(foe, x, origin.y));
            Assert.False(foe.GetPart<BrainPart>().Passive);
            return foe;
        }

        [TestCase(-1)] [TestCase(0)]
        public void ExactNormalSpeedApproachBudgetDistinguishesOneStepShortFromEnough(int delta)
        {
            var f = Controlled(); var foe = DistantThreat(f.zone, f.preferred, "MarlbackScrabbler");
            int steps = ApproachSteps(f.zone, f.terrain.Plan, f.preferred, new HashSet<Entity>(),
                Stamp.Select(p => (f.preferred.x + p.x, f.preferred.y + p.y)));
            int distance = Distance(f.preferred, f.zone.GetEntityPosition(foe));
            Assert.Greater(distance, steps + 1); Assert.AreEqual(100, foe.GetStatValue("Speed", 100));
            foe.GetPart<BrainPart>().SightRadius = distance - steps - delta - 1;
            Assert.True(Place(f.zone, f.terrain, out _, out var final)); Assert.True(final());
            var keeper = Keeper(f.zone);
            if (delta < 0) Assert.AreNotEqual(f.preferred, f.zone.GetEntityPosition(keeper));
            else Assert.AreEqual(f.preferred, f.zone.GetEntityPosition(keeper));
            OutsideSouthernApproachMargin(f.zone, f.terrain.Plan, keeper);
        }

        [TestCase(100)] [TestCase(130)]
        public void FasterViperNeedsMoreApproachClearanceWithoutChangingItsSpeed(int speed)
        {
            var f = Controlled(); var foe = DistantThreat(f.zone, f.preferred, "Viper");
            Assert.AreEqual(130, foe.GetStatValue("Speed", 100));
            foe.GetStat("Speed").BaseValue = speed;
            int steps = ApproachSteps(f.zone, f.terrain.Plan, f.preferred, new HashSet<Entity>(),
                Stamp.Select(p => (f.preferred.x + p.x, f.preferred.y + p.y)));
            int distance = Distance(f.preferred, f.zone.GetEntityPosition(foe));
            Assert.Greater(distance, steps + 1);
            foe.GetPart<BrainPart>().SightRadius = distance - steps - 1;
            Assert.True(Place(f.zone, f.terrain, out _, out var final)); Assert.True(final());
            var keeper = Keeper(f.zone);
            if (speed > 100) Assert.AreNotEqual(f.preferred, f.zone.GetEntityPosition(keeper));
            else Assert.AreEqual(f.preferred, f.zone.GetEntityPosition(keeper));
            Assert.AreEqual(speed, foe.GetStatValue("Speed", 100));
            OutsideSouthernApproachMargin(f.zone, f.terrain.Plan, keeper);
        }

        [Test] public void ExistingWallDetourCannotPassOnlyTheManhattanLowerBound()
        {
            var f = Controlled();
            int gap = Math.Max(f.preferred.x, f.terrain.Plan.SouthX) + 5;
            Assert.Less(gap, Zone.Width - 2);
            for (int x = 0; x < Zone.Width; x++)
            {
                if (x == gap) continue;
                Assert.True(f.zone.AddEntity(scope.Factory.CreateEntity("StoneWall"), x, Zone.Height - 3));
            }
            var foe = DistantThreat(f.zone, f.preferred, "MarlbackScrabbler");
            int lowerBound = Math.Abs(f.preferred.x - f.terrain.Plan.SouthX)
                + Math.Abs(f.preferred.y - (Zone.Height - 1)) - 1;
            int detour = ApproachSteps(f.zone, f.terrain.Plan, f.preferred, new HashSet<Entity>(),
                Stamp.Select(p => (f.preferred.x + p.x, f.preferred.y + p.y)));
            Assert.Greater(detour, lowerBound, "The existing wall must force a real detour beyond the cheap filter.");
            int distance = Distance(f.preferred, f.zone.GetEntityPosition(foe));
            Assert.Greater(distance, lowerBound + 1);
            foe.GetPart<BrainPart>().SightRadius = distance - lowerBound - 1;
            var original = f.zone.GetReadOnlyEntities().ToDictionary(e => e, f.zone.GetEntityPosition);
            Assert.True(Place(f.zone, f.terrain, out var added, out var final)); Assert.True(final());
            var keeper = Keeper(f.zone);
            Assert.AreNotEqual(f.preferred, f.zone.GetEntityPosition(keeper), "Manhattan alone would incorrectly keep this bay.");
            OutsideSouthernApproachMargin(f.zone, f.terrain.Plan, keeper);
            Assert.AreEqual(original.Count + added.Length, f.zone.EntityCount);
            foreach (var row in original) Assert.AreEqual(row.Value, f.zone.GetEntityPosition(row.Key));
        }

        [Test] public void NoSafeCandidateRefusesWithoutRemovingTheOriginalEncounter()
        {
            var f = Controlled(); var hostile = Nearby(f.zone, f.preferred, 8);
            hostile.GetPart<BrainPart>().SightRadius = 100;
            var original = f.zone.GetReadOnlyEntities().ToDictionary(e => e, f.zone.GetEntityPosition);
            Assert.False(Place(f.zone, f.terrain, out var added, out var final));
            Assert.Null(added); Assert.Null(final); Assert.AreEqual(original.Count, f.zone.EntityCount);
            foreach (var row in original) Assert.AreEqual(row.Value, f.zone.GetEntityPosition(row.Key));
            Assert.False(f.zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "SpreadSeedKeeper"));
            Assert.AreEqual(15, hostile.GetStatValue("Hitpoints"));
        }
    }
}
