using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    // A cold kitchen must give a normal-speed arrival from its authored north
    // port an initial geometric approach margin. This is not NPC invulnerability.
    public sealed class SpreadKitchenInitialSafetyTests
    {
        const string Kitchen = "Overworld.12.11.0", Role = "SpreadResident.Role";
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        readonly List<(FieldInfo field, object original)> borrowed = new List<(FieldInfo, object)>();
        HaulingContentScope scope;

        [SetUp] public void Setup()
        {
            foreach (string name in new[] { "_factionFeelings", "_registeredFactions", "_factionData" }) Replace(typeof(FactionManager), name);
            Replace(typeof(PlayerReputation), "_reputation"); scope = new HaulingContentScope();
            FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Factions.json")));
        }
        void Replace(Type type, string name)
        {
            var field = type.GetField(name, All); Assert.NotNull(field); borrowed.Add((field, field.GetValue(null)));
            field.SetValue(null, Activator.CreateInstance(field.FieldType));
        }
        [TearDown] public void Cleanup()
        {
            try { scope?.Dispose(); }
            finally { for (int i = borrowed.Count - 1; i >= 0; i--) borrowed[i].field.SetValue(null, borrowed[i].original); borrowed.Clear(); }
        }
        static Entity Cook(Zone zone) => zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpreadWaysideCook");
        static int Distance((int x, int y) a, (int x, int y) b) => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        static bool Blocks(string role) => role == "resident" || role == "oven" || role == "pan" || role == "shelter";

        // Independent unbounded shortest-path oracle, using the established
        // physical walk predicate and actual court obstructions. Walkable stones,
        // cot and tray positions are not invented walls.
        static int ApproachSteps(Zone zone, SpreadCompositionPlan plan, (int x, int y) origin,
            HashSet<Entity> ignored, IEnumerable<(int x, int y)> obstructions)
        {
            var type = typeof(SpreadWildernessSituationBuilder).GetNestedType("Geometry", BindingFlags.NonPublic); Assert.NotNull(type);
            var geometry = Activator.CreateInstance(type, All, null, new object[] { zone, ignored }, null);
            var walkMethod = type.GetMethod("Walk", All); Assert.NotNull(walkMethod);
            var blocked = new HashSet<(int x, int y)>(obstructions);
            bool Walk(int x, int y) => (bool)walkMethod.Invoke(geometry, new object[] { x, y, blocked });
            var start = (x: plan.NorthX, y: 0); Assert.True(Walk(start.x, start.y), "The authored northern arrival must stay open.");
            var seen = new HashSet<(int x, int y)> { start };
            var queue = new Queue<(int x, int y, int steps)>(); queue.Enqueue((start.x, start.y, 0));
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                if (Math.Abs(at.x - origin.x) + Math.Abs(at.y - origin.y) == 1) return at.steps;
                foreach (var n in new[] { (x:at.x-1,y:at.y), (x:at.x+1,y:at.y), (x:at.x,y:at.y-1), (x:at.x,y:at.y+1) })
                    if (Walk(n.x, n.y) && seen.Add(n)) queue.Enqueue((n.x, n.y, at.steps + 1));
            }
            Assert.Fail("A legal cardinal northern approach must remain."); return -1;
        }
        static void OutsideNorthApproachMargin(Zone zone, SpreadCompositionPlan plan, Entity cook)
        {
            var packet = zone.GetReadOnlyEntities().Where(e => e.Properties.ContainsKey(Role)).ToHashSet();
            int approach = ApproachSteps(zone, plan, zone.GetEntityPosition(cook), packet,
                packet.Where(e => Blocks(e.GetProperty(Role))).Select(zone.GetEntityPosition));
            var origin = zone.GetEntityPosition(cook);
            foreach (var foe in zone.GetReadOnlyEntities().Where(e => e != cook && e.HasTag("Creature")))
            {
                var brain = foe.GetPart<BrainPart>();
                if (brain?.ParentEntity != foe || brain.Passive || foe.GetStatValue("Hitpoints") <= 0
                    || CombatSystem.IsDeathHandled(foe) || !FactionManager.IsHostile(foe, cook)) continue;
                int distance = Distance(origin, zone.GetEntityPosition(foe));
                Assert.Greater(distance, brain.SightRadius, foe.BlueprintName + " starts inside acquisition range of the cook.");
                long available = (long)(distance - brain.SightRadius - 1) * TurnManager.DefaultSpeed;
                long needed = (long)approach * Math.Max(TurnManager.DefaultSpeed, foe.GetStatValue("Speed", TurnManager.DefaultSpeed));
                Assert.GreaterOrEqual(available, needed, foe.BlueprintName + " consumes the normal-speed northern approach margin.");
            }
        }

        [TestCase(224401440)] [TestCase(200893250)] [TestCase(64)] [TestCase(65)]
        public void ColdKitchenKeepsCompleteStaffedCourtOutsideSpeedAwareNorthernApproach(int seed)
        {
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(Kitchen, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            bool oldChannel = Diag.IsChannelEnabled("worldgen");
            var oldTraces = Diag.Snapshot(Diag.BufferCapacity).Select(e => e.TraceId).ToHashSet();
            Zone zone; string evidence;
            Diag.SetChannel("worldgen", true);
            try
            {
                zone = manager.GetZone(Kitchen);
                evidence = string.Join("; ", Diag.Snapshot(Diag.BufferCapacity)
                    .Where(e => !oldTraces.Contains(e.TraceId) && e.Kind == "SpreadResidentRefused")
                    .Select(e => e.PayloadJson));
            }
            finally { Diag.SetChannel("worldgen", oldChannel); }
            Assert.NotNull(zone);
            Assert.AreEqual(2, manager.Exploration.DispositionFor(Kitchen), "Do not solve the service failure by omitting its site. " + evidence);
            var cook = Cook(zone); var packet = zone.GetReadOnlyEntities().Where(e => e.Properties.ContainsKey(Role)).ToArray();
            Assert.AreEqual(32, packet.Length); Assert.AreEqual(15, packet.Count(e => e.GetProperty(Role) == "court-floor"));
            Assert.AreEqual(20, cook.GetStatValue("Hitpoints")); Assert.True(cook.GetPart<BrainPart>().Passive);
            Assert.AreEqual("WaysideCookStock", cook.GetPart<TraderPart>().StockTable); Assert.AreEqual(35, TradeSystem.GetDrams(cook));
            Assert.AreEqual(5, cook.GetPart<InventoryPart>().Objects.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            var pan = packet.Single(e => e.GetProperty(Role) == "pan").GetPart<KitchenBatchPart>();
            Assert.True(pan.Configured); Assert.AreSame(cook, pan.Worker); Assert.AreEqual("Idle", pan.State);
            var plan = SpreadCompositionPlan.Create(Kitchen, seed, Formation.Hedgerow,
                manager.Exploration.Entries.Single(e => e.ZoneID == Kitchen).Topology);
            OutsideNorthApproachMargin(zone, plan, cook);
            var original = zone.GetReadOnlyEntities().ToDictionary(e => e, zone.GetEntityPosition);
            // Loaded graphs stay literal, including a resident's earned damage.
            cook.GetStat("Hitpoints").BaseValue = 1;
            manager.UnloadZone(Kitchen); Assert.AreSame(zone, manager.GetZone(Kitchen));
            Assert.AreSame(cook, Cook(zone)); Assert.AreEqual(1, cook.GetStatValue("Hitpoints"));
            Assert.AreEqual(original.Count, zone.EntityCount);
            foreach (var owner in original) Assert.AreEqual(owner.Value, zone.GetEntityPosition(owner.Key));
            Assert.AreSame(cook, pan.Worker); Assert.AreEqual("Idle", pan.State);
        }

        (Zone zone, SpreadCompositionBuilder terrain) Controlled()
        {
            const int seed = 224401440; scope.Seed(seed); var zone = new Zone(Kitchen);
            var terrain = new SpreadCompositionBuilder(seed)
                { FormationOverride = Formation.Hedgerow, Topology = SpreadExplorationTopology.OffsetLanes };
            Assert.True(terrain.BuildZone(zone, scope.Factory, new Random(seed)));
            // These isolated boundary counters deliberately use bare source ground;
            // the four native-world cases above retain every generated owner.
            var bare = typeof(DoorPart).GetMethod("IsBareGround", All); Assert.NotNull(bare);
            foreach (var owner in zone.GetReadOnlyEntities().Where(e => !(bool)bare.Invoke(null, new object[] { e })).ToArray()) zone.RemoveEntity(owner);
            zone.GenReservedCells.Clear(); return (zone, terrain);
        }
        bool Place(Zone zone, SpreadCompositionBuilder terrain, out Entity[] added, out Func<bool> final, bool connected = true)
            => SpreadExplorationResidents.TryPlaceConnected(zone, scope.Factory, terrain,
                SpreadExplorationFamily.WaysideKitchen, () => true, out added, out final, connected,
                "0123456789abcdef0123456789abcdef");

        ((int x, int y) origin, int steps) Baseline()
        {
            var f = Controlled(); Assert.True(Place(f.zone, f.terrain, out var packet, out var final)); Assert.True(final());
            var origin = f.zone.GetEntityPosition(Cook(f.zone)); var ignored = packet.ToHashSet();
            int steps = ApproachSteps(f.zone, f.terrain.Plan, origin, ignored,
                packet.Where(e => Blocks(e.GetProperty(Role))).Select(f.zone.GetEntityPosition));
            return (origin, steps);
        }
        Entity Threat(Zone zone, (int x, int y) at, string blueprint = "MarlbackScrabbler")
        {
            var foe = scope.Factory.CreateEntity(blueprint); Assert.NotNull(foe);
            Assert.True(zone.AddEntity(foe, at.x, at.y)); Assert.False(foe.GetPart<BrainPart>().Passive);
            return foe;
        }
        static void Preserved(Zone zone, Dictionary<Entity, (int x, int y)> original, int added)
        {
            Assert.AreEqual(original.Count + added, zone.EntityCount);
            foreach (var owner in original) Assert.AreEqual(owner.Value, zone.GetEntityPosition(owner.Key));
        }

        [TestCase("benign")] [TestCase("passive")]
        public void NonInitiatingPopulationKeepsTheExistingCourtComposition(string kind)
        {
            var expected = Baseline(); var f = Controlled();
            var foe = Threat(f.zone, (expected.origin.x + 8, expected.origin.y));
            if (kind == "benign") foe.Tags["Faction"] = "Villagers"; else foe.GetPart<BrainPart>().Passive = true;
            var original = f.zone.GetReadOnlyEntities().ToDictionary(e => e, f.zone.GetEntityPosition);
            Assert.True(Place(f.zone, f.terrain, out var packet, out var final)); Assert.True(final());
            Assert.AreEqual(expected.origin, f.zone.GetEntityPosition(Cook(f.zone)));
            Assert.AreEqual(kind != "benign", FactionManager.IsHostile(foe, Cook(f.zone)));
            Preserved(f.zone, original, packet.Length);
        }

        [TestCase(10)] [TestCase(11)]
        public void InitialSightClearanceAloneCannotReplaceTheNorthernApproach(int distance)
        {
            var expected = Baseline(); var f = Controlled();
            var foe = Threat(f.zone, (expected.origin.x + distance, expected.origin.y));
            Assert.AreEqual(10, foe.GetPart<BrainPart>().SightRadius);
            var original = f.zone.GetReadOnlyEntities().ToDictionary(e => e, f.zone.GetEntityPosition);
            Assert.True(Place(f.zone, f.terrain, out var packet, out var final)); Assert.True(final());
            Assert.AreNotEqual(expected.origin, f.zone.GetEntityPosition(Cook(f.zone)));
            OutsideNorthApproachMargin(f.zone, f.terrain.Plan, Cook(f.zone));
            Preserved(f.zone, original, packet.Length); Assert.AreEqual(15, foe.GetStatValue("Hitpoints"));
        }

        [TestCase(100)] [TestCase(130)]
        public void ActualViperSpeedChangesTheRequiredNorthernApproachMargin(int speed)
        {
            var expected = Baseline(); var f = Controlled(); var foe = Threat(f.zone, (4, expected.origin.y), "Viper");
            Assert.AreEqual(130, foe.GetStatValue("Speed")); foe.GetStat("Speed").BaseValue = speed;
            int lowerBound = Math.Abs(expected.origin.x - f.terrain.Plan.NorthX) + expected.origin.y - 1;
            Assert.GreaterOrEqual(expected.steps, lowerBound);
            Assert.Less(expected.steps * TurnManager.DefaultSpeed / 130, lowerBound,
                "The fast case must remain short even if a different court orientation shortens its approach.");
            int distance = Distance(expected.origin, f.zone.GetEntityPosition(foe)); Assert.Greater(distance, expected.steps + 1);
            foe.GetPart<BrainPart>().SightRadius = distance - expected.steps - 1;
            var original = f.zone.GetReadOnlyEntities().ToDictionary(e => e, f.zone.GetEntityPosition);
            Assert.True(Place(f.zone, f.terrain, out var packet, out var final)); Assert.True(final());
            if (speed == TurnManager.DefaultSpeed) Assert.AreEqual(expected.origin, f.zone.GetEntityPosition(Cook(f.zone)));
            else Assert.AreNotEqual(expected.origin, f.zone.GetEntityPosition(Cook(f.zone)));
            OutsideNorthApproachMargin(f.zone, f.terrain.Plan, Cook(f.zone));
            Assert.AreEqual(speed, foe.GetStatValue("Speed")); Preserved(f.zone, original, packet.Length);
        }

        [TestCase(true)] [TestCase(false)]
        public void ImpossibleThreatRefusesOnlyTheConnectedKitchenWithoutRemovingSources(bool connected)
        {
            var expected = Baseline(); var f = Controlled(); var foe = Threat(f.zone, (4, expected.origin.y));
            foe.GetPart<BrainPart>().SightRadius = 100;
            var original = f.zone.GetReadOnlyEntities().ToDictionary(e => e, f.zone.GetEntityPosition);
            bool placed = Place(f.zone, f.terrain, out var packet, out var final, connected);
            Assert.AreEqual(!connected, placed);
            if (connected)
            {
                Assert.Null(packet); Assert.Null(final); Preserved(f.zone, original, 0);
                Assert.False(f.zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "SpreadWaysideCook"));
            }
            else { Assert.True(final()); Assert.AreEqual(6, packet.Length); Preserved(f.zone, original, packet.Length); }
            Assert.AreEqual(15, foe.GetStatValue("Hitpoints")); Assert.AreEqual(100, foe.GetPart<BrainPart>().SightRadius);
        }
    }
}
