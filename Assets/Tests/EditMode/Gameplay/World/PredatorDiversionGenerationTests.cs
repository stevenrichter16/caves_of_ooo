using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class PredatorDiversionGenerationTests
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        HaulingContentScope scope;
        Zone priorActive;

        [SetUp]
        public void SetUp()
        {
            priorActive = SettlementRuntime.ActiveZone;
            scope = new HaulingContentScope();
            scope.Seed(64);
        }

        [TearDown]
        public void TearDown()
        {
            scope.Dispose();
            SettlementRuntime.ActiveZone = priorActive;
        }

        static FieldInfo AdmissionField()
        {
            var field = typeof(SpreadPredatorPart).GetField("MeatDiversionEnabled", All);
            Assert.NotNull(field, "Fresh admitted hunts need a saved opt-in; legacy Parts must default to false.");
            return field;
        }

        static bool Admitted(SpreadPredatorPart role) => (bool)AdmissionField().GetValue(role);

        static string[] Rows(SpreadExplorationPlan plan) => plan.Entries.Select(e =>
            e.ZoneID + "|" + e.PlacementEligible + "|" + e.Family + "|" + e.Topology + "|" + e.ActorSeed + "|" + e.RewardSeed).ToArray();

        static string RestoreVersion(OverworldZoneManager manager, int version)
        {
            var world = SpreadExplorationPlan.BindForSave(manager, null);
            string wire = world.GetProperty(SpreadExplorationPlan.PropertyKey);
            wire = version + wire.Substring(wire.IndexOf('|'));
            world.Properties[SpreadExplorationPlan.PropertyKey] = wire;
            Restore(manager, world);
            return wire;
        }

        static void Restore(OverworldZoneManager manager, Entity world)
        {
            var plan = (SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore", All)
                .Invoke(null, new object[] { manager, world });
            typeof(OverworldZoneManager).GetProperty("Exploration", All).SetValue(manager, plan);
        }

        [TestCase(1)]
        [TestCase(64)]
        [TestCase(1729)]
        public void NewVersionThirteenKeepsLiteralTwelveAndElevenAssignmentsWithoutGenerating(int seed)
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            Assert.AreEqual(13, manager.Exploration.Version, "Only a fresh explicit manifest opts into meat diversion.");
            var rows = Rows(manager.Exploration);
            string worldKey = manager.Exploration.WorldKey;
            foreach (int version in new[] { 12, 11 })
            {
                string wire = RestoreVersion(manager, version);
                Assert.True(manager.Exploration.Enabled);
                Assert.AreEqual(version, manager.Exploration.Version);
                Assert.AreEqual(worldKey, manager.Exploration.WorldKey);
                CollectionAssert.AreEqual(rows, Rows(manager.Exploration));
                Assert.AreEqual(wire, SpreadExplorationPlan.BindForSave(manager, null).GetProperty(SpreadExplorationPlan.PropertyKey));
                Assert.Zero(manager.CachedZoneCount, "Restoring literal assignments must not search or regenerate their sites.");
            }
        }

        Zone FirstActualHunt(OverworldZoneManager manager)
        {
            var selected = manager.Exploration.Entries.Where(e => e.Family == SpreadExplorationFamily.HuntThroughCover)
                .Select(e => e.ZoneID).ToArray();
            Assert.IsNotEmpty(selected);
            foreach (string id in selected)
            {
                scope.Seed(unchecked(manager.WorldSeed ^ FormationSelector.StableIndex(id, int.MaxValue)));
                var zone = manager.GetZone(id);
                Assert.NotNull(zone, id);
                if (manager.Exploration.DispositionFor(id) == 2) return zone;
            }
            Assert.Fail("The declared seed64 assignment corpus must realize a hunt from actual sources; no injected actor or reroll.");
            return null;
        }

        [TestCase(13, true)]
        [TestCase(12, false)]
        [TestCase(11, false)]
        public void ActualGeneratedAndReloadedPairsUseTheirLiteralManifestAdmission(int version, bool enabled)
        {
            AdmissionField();
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            if (version != 13) RestoreVersion(manager, version);
            var zone = FirstActualHunt(manager);
            var hunter = zone.GetReadOnlyEntities().Single(e => e.HasPart<SpreadPredatorPart>());
            var role = hunter.GetPart<SpreadPredatorPart>();
            Assert.True(role.Configured);
            Assert.AreEqual(enabled, Admitted(role));
            Assert.AreSame(hunter, role.Prey.GetPart<SpreadGrazerPart>().Hunter);
            string hunterId = hunter.ID, preyId = role.Prey.ID;
            var owners = zone.GetReadOnlyEntities().ToDictionary(e => e.ID, e => zone.GetEntityPosition(e));
            manager.SetActiveZone(zone);
            var state = GameSessionState.Capture("diversion-generation", "test", manager, null, null);
            GameSessionState loaded;
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream));
                stream.Position = 0;
                loaded = GameSessionState.Load(new SaveReader(stream, scope.Factory));
            }
            var restored = loaded.ZoneManager;
            Assert.AreNotSame(manager, restored);
            Assert.AreEqual(version, restored.Exploration.Version);
            var returned = restored.GetZone(zone.ZoneID);
            Assert.AreNotSame(zone, returned);
            Assert.AreEqual(owners.Count, returned.EntityCount);
            foreach (var owner in returned.GetReadOnlyEntities())
                Assert.AreEqual(owners[owner.ID], returned.GetEntityPosition(owner));
            var savedHunter = returned.GetReadOnlyEntities().Single(e => e.ID == hunterId);
            var savedRole = savedHunter.GetPart<SpreadPredatorPart>();
            Assert.AreNotSame(hunter, savedHunter);
            Assert.AreEqual(enabled, Admitted(savedRole));
            Assert.AreEqual(preyId, savedRole.Prey.ID);
            Assert.AreSame(savedHunter, savedRole.Prey.GetPart<SpreadGrazerPart>().Hunter);
            restored.UnloadZone(zone.ZoneID);
            Assert.AreSame(returned, restored.GetZone(zone.ZoneID));
            Assert.AreEqual(enabled, Admitted(savedRole));
        }

        [Test]
        public void MissingManifestDoesNotAdmitAPairMerelyBecauseItsNumericDefaultIsCurrent()
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            string id = manager.Exploration.Entries.First(e => e.Family == SpreadExplorationFamily.HuntThroughCover).ZoneID;
            Restore(manager, new Entity { BlueprintName = "World" });
            Assert.False(manager.Exploration.Enabled);
            Assert.AreEqual(SpreadExplorationPlan.CurrentVersion, manager.Exploration.Version);
            scope.Seed(unchecked(64 ^ FormationSelector.StableIndex(id, int.MaxValue)));
            var zone = manager.GetZone(id);
            Assert.NotNull(zone);
            Assert.False(zone.GetReadOnlyEntities().Any(e => e.HasPart<SpreadPredatorPart>()));
            Assert.Zero(manager.Exploration.RetainedGraphCount);
        }

        [Test]
        public void AccessingAnExistingDefaultConfiguredPairCannotUpgradeItsCapability()
        {
            AdmissionField();
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            string id = manager.Exploration.Entries.First(e => e.Family == SpreadExplorationFamily.HuntThroughCover).ZoneID;
            // Controlled cached legacy graph, not a claim that ordinary generation created this pair.
            var zone = new Zone(id);
            var hunter = scope.Factory.CreateEntity("Furrowstalker");
            var prey = scope.Factory.CreateEntity("ReedbackGrazer");
            Assert.True(zone.AddEntity(hunter, 20, 10));
            Assert.True(zone.AddEntity(prey, 24, 10));
            var role = hunter.GetPart<SpreadPredatorPart>();
            Assert.True(role.Configure(zone, prey)); // Source-compatible omitted argument must retain false.
            Assert.False(Admitted(role));
            manager.CachedZones.Add(id, zone);
            var owners = zone.GetReadOnlyEntities().ToArray();
            Assert.AreSame(zone, manager.GetZone(id));
            CollectionAssert.AreEqual(owners, zone.GetReadOnlyEntities());
            Assert.AreSame(prey, role.Prey);
            Assert.False(Admitted(role));
            Assert.Zero(manager.Exploration.DispositionFor(id), "Attaching a graph never creates generation authority.");
        }

        sealed class TailProbe : IZoneBuilder
        {
            readonly Action<int> observe;
            public TailProbe(Action<int> observe) { this.observe = observe; }
            public string Name => "DiversionRngCounter";
            public int Priority => int.MaxValue;
            public bool BuildZone(Zone zone, EntityFactory factory, Random rng) { observe(rng.Next()); return true; }
        }

        sealed class ObservedManager : OverworldZoneManager
        {
            internal int Tail;
            internal ObservedManager(EntityFactory factory) : base(factory, 64) { }
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var pipeline = base.GetPipelineForZone(id);
                pipeline.AddBuilder(new TailProbe(value => Tail = value));
                return pipeline;
            }
        }

        static string[] Shapes(Zone zone) => zone.GetReadOnlyEntities().Select(e =>
            e.BlueprintName + "|" + zone.GetEntityPosition(e) + "|" + e.GetPart<RenderPart>()?.DisplayName
            + "|" + e.GetPart<PhysicsPart>()?.Weight + "|" + string.Join(";", e.Parts.Select(p => p.Name))
            + "|gear:" + string.Join(";", DensityLootTestScope.Gear(e).Select(g => g.BlueprintName + ":" + (g.GetPart<StackerPart>()?.StackCount ?? 1))))
            .OrderBy(s => s, StringComparer.Ordinal).ToArray();

        [Test]
        public void OptInChangesNeitherTheActualSourceLayoutNorTheGenerationRandomTail()
        {
            AdmissionField();
            var current = new ObservedManager(scope.Factory);
            var legacy = new ObservedManager(scope.Factory);
            RestoreVersion(legacy, 12);
            CollectionAssert.AreEqual(Rows(legacy.Exploration), Rows(current.Exploration));
            var currentZone = FirstActualHunt(current);
            string id = currentZone.ZoneID;
            scope.Seed(unchecked(64 ^ FormationSelector.StableIndex(id, int.MaxValue)));
            var oldZone = legacy.GetZone(id);
            Assert.NotNull(oldZone);
            Assert.AreEqual(2, legacy.Exploration.DispositionFor(id));
            CollectionAssert.AreEqual(Shapes(oldZone), Shapes(currentZone));
            Assert.AreEqual(legacy.Tail, current.Tail, "The capability gate must not spend a placement or population roll.");
            Assert.False(Admitted(oldZone.GetReadOnlyEntities().Single(e => e.HasPart<SpreadPredatorPart>()).GetPart<SpreadPredatorPart>()));
            Assert.True(Admitted(currentZone.GetReadOnlyEntities().Single(e => e.HasPart<SpreadPredatorPart>()).GetPart<SpreadPredatorPart>()));
        }
    }
}
