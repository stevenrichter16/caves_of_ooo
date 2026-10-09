using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Source admission/transaction only. Whole-goal action, save and actual
    // fixed-seed source census live in the separate finite-supply suites.
    public sealed class FiniteWaterWorksiteTests
    {
        const string Shelter = "Overworld.15.10.0", Caster = "MarlbackCindercaller", Shell = "SunbladderShell";
        const BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope = new HaulingContentScope();
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone;
            internal readonly SpreadCompositionBuilder Terrain = new SpreadCompositionBuilder(64) { FormationOverride = Formation.OldRoad };
            internal readonly PopulationBuilder Population;
            internal Entity[] Owners; internal Func<bool> Final;
            readonly Action<Entity> priorCallback = SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback;
            internal Fixture(string id = Shelter, int count = 2)
            {
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = null;
                Scope.Seed(64); Zone = new Zone(id);
                Assert.True(Terrain.BuildZone(Zone, Factory, new Random(64)));
                // Controlled clear geometry; ordinary production census elsewhere
                // must prove real unedited source availability independently.
                foreach (var e in Zone.GetReadOnlyEntities().Where(e => !(bool)typeof(DoorPart).GetMethod("IsBareGround", All)
                    .Invoke(null, new object[] { e })).ToArray()) Zone.RemoveEntity(e);
                Zone.GenReservedCells.Clear();
                Population = new PopulationBuilder(new PopulationTable { Name = "SpreadTier1", Entries = new List<PopulationEntry> {
                    new PopulationEntry { BlueprintName = "Viper", EncounterGroup = "SpreadTier1Encounter", MinCount = count, MaxCount = count } } })
                { CaptureSourceReceipts = true };
                Assert.True(Population.BuildZone(Zone, Factory, new Random(17)));
                Assert.True(Population.SourceReceipt.IsCurrent);
            }
            internal bool Place(Func<bool> authority = null) => SpreadExplorationWorksites.TryPlace(Zone, Factory, Terrain,
                Population, null, SpreadExplorationFamily.TemperingShelter, authority ?? (() => true), out Owners, out Final);
            internal Func<bool> Proof(IEnumerable<Entity> owners) => (Func<bool>)typeof(SpreadGenerationReceipt)
                .GetMethod("CaptureFinalState", All).Invoke(null, new object[] { Zone, owners });
            internal void Observe(params string[] blueprints)
            {
                Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                foreach (string bp in blueprints) Factory.Blueprints[bp].Parts["ReceiptCreated"] = new Dictionary<string, string>();
            }
            public void Dispose() { SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = priorCallback; Scope.Dispose(); }
        }
        static Entity Water(Entity actor) => actor.GetPart<InventoryPart>().Objects.SingleOrDefault(e => e.BlueprintName == Shell);
        static bool Enabled(Entity actor)
        {
            var p = actor.GetPart("TacticalSupply"); Assert.NotNull(p, "Only the fresh admitted owner gains the policy.");
            var f = p.GetType().GetField("SelfDousing"); Assert.NotNull(f); return (bool)f.GetValue(p);
        }
        static void Disable(Entity actor)
        {
            var p = actor.GetPart("TacticalSupply"); Assert.NotNull(p);
            p.GetType().GetField("SelfDousing").SetValue(p, false);
        }
        static void ReplacedWaterPart(Entity shell)
        {
            var prior = shell.GetPart<WaterskinPart>(); shell.RemovePart(prior);
            shell.AddPart(new WaterskinPart { Capacity = 2, Charges = 1 });
        }
        static void AssertOldSite(Entity[] owners)
        {
            Assert.True(owners.Any(e => e.BlueprintName == "TinkersForge"));
            Assert.False(owners.Any(e => e.GetPart("TacticalSupply") != null));
            foreach (var e in owners.Where(e => e.HasPart<InventoryPart>())) Assert.Null(Water(e));
        }
        static void AssertUnclaimed(Fixture f, Entity[] before, Func<bool> proof)
        {
            Assert.True(proof()); CollectionAssert.AreEquivalent(before, f.Zone.GetReadOnlyEntities());
            Assert.True(f.Population.SourceReceipt.IsCurrent, "Refuse before consuming an ordinary source.");
            Assert.Null(f.Owners); Assert.Null(f.Final);
        }

        [Test] public void ExactFreshPairGainsOneRealWaterOnlyOnTheCasterAndRetainsOriginalLoadoutAndPopulation()
        {
            using (var f = new Fixture())
            {
                var source = f.Population.SourceReceipt.Owners.ToArray();
                var others = f.Zone.GetReadOnlyEntities().Except(source).ToArray(); var unchanged = f.Proof(others);
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.True(unchanged());
                Assert.AreEqual(2, f.Zone.GetReadOnlyEntities().Count(e => e.HasTag("Creature")));
                Assert.True(source.All(e => f.Zone.GetEntityCell(e) == null));
                var actor = f.Owners.Single(e => e.BlueprintName == Caster); Assert.True(Enabled(actor));
                var shell = Water(actor); Assert.NotNull(shell); Assert.AreSame(actor, shell.GetPart<PhysicsPart>().InInventory);
                Assert.Null(shell.GetPart<PhysicsPart>().Equipped); Assert.Null(f.Zone.GetEntityCell(shell));
                Assert.AreEqual(1, shell.GetPart<WaterskinPart>().Charges); Assert.AreEqual(2, shell.GetPart<WaterskinPart>().Capacity);
                Assert.AreEqual(1, shell.GetPart<StackerPart>()?.StackCount ?? 1);
                Assert.True(actor.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "FireMoss"));
                Assert.True(actor.GetPart<InventoryPart>().GetAllEquipped().Any(e => e.BlueprintName == "Cudgel"));
                Assert.AreEqual(1, f.Owners.Count(e => e.GetPart("TacticalSupply") != null));
                var partner = f.Owners.Single(e => e.BlueprintName == "MarlbackScrabbler"); Assert.Null(Water(partner));
            }
        }

        [TestCase("Overworld.10.9.0")] [TestCase("Overworld.12.9.0")] [TestCase("Overworld.12.10.0")]
        public void AnotherFreshTemperingShelterKeepsItsOrdinaryPairWithoutNewStock(string id)
        { using (var f = new Fixture(id)) { Assert.True(f.Place()); Assert.True(f.Final()); AssertOldSite(f.Owners); } }

        [Test] public void AOneActorReceiptCannotInventACasterOrWaterToDemonstrateTheNewPolicy()
        {
            using (var f = new Fixture(count: 1))
            {
                var source = f.Population.SourceReceipt.Owners.Single(); var proof = f.Proof(new[] { source });
                Assert.True(f.Place()); Assert.True(f.Final()); Assert.True(proof()); AssertOldSite(f.Owners);
                Assert.AreEqual(1, f.Zone.GetReadOnlyEntities().Count(e => e.HasTag("Creature")));
            }
        }

        [Test] public void MissingOptionalShellPreservesTheEarlierWorkingSite()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Factory.Blueprints.Remove(Shell)); Assert.True(f.Place()); Assert.True(f.Final());
                AssertOldSite(f.Owners); Assert.AreEqual(2, f.Zone.GetReadOnlyEntities().Count(e => e.HasTag("Creature")));
            }
        }

        [TestCase("empty")] [TestCase("extra-water")] [TestCase("foreign-owner")] [TestCase("wrong-blueprint")]
        public void MalformedOptionalFactorySupplyRefusesBeforeAnySourceIsClaimed(string fault)
        {
            using (var f = new Fixture())
            {
                f.Observe(Shell); var before = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(before); bool called = false;
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e =>
                {
                    if (e.BlueprintName != Shell) return; called = true;
                    if (fault == "empty") e.GetPart<WaterskinPart>().Charges = 0;
                    if (fault == "extra-water") e.GetPart<WaterskinPart>().Charges = 2;
                    if (fault == "foreign-owner") e.GetPart<PhysicsPart>().InInventory = new Entity();
                    if (fault == "wrong-blueprint") e.BlueprintName = "Waterskin";
                };
                Assert.False(f.Place()); Assert.True(called); AssertUnclaimed(f, before, proof);
            }
        }

        [Test] public void PureFactoryObserversSeeTheActualShellAndDoNotPreventOtherwiseValidPlacement()
        {
            using (var f = new Fixture())
            {
                f.Observe(Shell, Caster, "MarlbackScrabbler"); var seen = new List<Entity>();
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e => seen.Add(e);
                Assert.True(f.Place()); Assert.True(f.Final());
                var actor = f.Owners.Single(e => e.BlueprintName == Caster);
                Assert.Contains(Water(actor), seen); Assert.True(Enabled(actor)); Assert.AreEqual(1, seen.Count(e => e.BlueprintName == Shell));
            }
        }

        [TestCase("water")] [TestCase("water-part")] [TestCase("policy")]
        public void LaterPartnerFactoryCannotRewriteThePreviouslyPreparedSupplyAndStillCommit(string fault)
        {
            using (var f = new Fixture())
            {
                f.Observe(Shell, Caster, "MarlbackScrabbler"); Entity actor = null, shell = null; bool changed = false;
                var before = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(before);
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e =>
                {
                    if (e.BlueprintName == Shell) shell = e;
                    if (e.BlueprintName == Caster) actor = e;
                    if (e.BlueprintName != "MarlbackScrabbler" || actor == null || shell == null) return;
                    if (fault == "water") shell.GetPart<WaterskinPart>().Charges = 0;
                    if (fault == "water-part") ReplacedWaterPart(shell);
                    if (fault == "policy") Disable(actor);
                    changed = true;
                };
                Assert.False(f.Place()); Assert.True(changed, "The later callback must actually reach the prepared supply.");
                AssertUnclaimed(f, before, proof);
            }
        }

        [Test] public void ShellCreationCanRevokeAuthorityWithoutLeavingAnEnrichedPartialSite()
        {
            using (var f = new Fixture())
            {
                f.Observe(Shell); bool allowed = true, called = false;
                var before = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(before);
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e => { if (e.BlueprintName == Shell) { called = true; allowed = false; } };
                Assert.False(f.Place(() => allowed)); Assert.True(called); AssertUnclaimed(f, before, proof);
            }
        }

        [Test] public void ANewCasterUnableToCarryTheRealShellCannotReceiveFreePolicyStock()
        {
            using (var f = new Fixture())
            {
                f.Observe(Caster); bool called = false;
                var before = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(before);
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = e =>
                { if (e.BlueprintName == Caster) { e.GetPart<InventoryPart>().MaxWeight = 0; called = true; } };
                Assert.False(f.Place()); Assert.True(called); AssertUnclaimed(f, before, proof);
            }
        }

        [TestCase("water")] [TestCase("policy")]
        public void FinalGenerationProofRejectsSpentOrDisabledStockAfterPlacement(string fault)
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); Assert.True(f.Final()); var actor = f.Owners.Single(e => e.BlueprintName == Caster);
                Assert.True(Enabled(actor)); var shell = Water(actor); Assert.NotNull(shell);
                if (fault == "water") shell.GetPart<WaterskinPart>().Charges = 0; else Disable(actor);
                Assert.False(f.Final(), "A commitment must describe the unchanged new packet, not a spent or replaced one.");
            }
        }

        [Test] public void ReinvokingTheHelperCannotRetrofitAnEarlierLiteralSite()
        {
            using (var f = new Fixture())
            {
                var blueprint = f.Factory.Blueprints[Shell]; f.Factory.Blueprints.Remove(Shell);
                Assert.True(f.Place()); var oldOwners = f.Zone.GetReadOnlyEntities().ToArray(); var proof = f.Proof(oldOwners);
                f.Factory.Blueprints[Shell] = blueprint; Assert.False(f.Place()); Assert.True(proof());
                CollectionAssert.AreEquivalent(oldOwners, f.Zone.GetReadOnlyEntities()); AssertOldSite(oldOwners);
            }
        }
    }
}
