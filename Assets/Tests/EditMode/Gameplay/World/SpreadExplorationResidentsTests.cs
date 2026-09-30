using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadExplorationResidentsTests
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string RoleKey = "SpreadResident.Role";
        sealed class ForbiddenRandom : Random
        {
            public override int Next() => throw new InvalidOperationException("Ordinary random stream was consumed.");
            public override int Next(int max) => Next();
            public override int Next(int min, int max) => Next();
            public override double NextDouble() => Next();
        }
        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope = new HaulingContentScope();
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone = new Zone("Overworld.11.8.0");
            internal readonly SpreadCompositionBuilder Terrain = new SpreadCompositionBuilder(64) { FormationOverride = Formation.OldRoad };
            internal Entity[] Owners;
            internal Func<bool> Final;
            internal Fixture()
            {
                Scope.Seed(64);
                Assert.True(Terrain.BuildZone(Zone, Factory, new Random(64)));
                // Controlled space only. Separate native tests use wholly unedited generation.
                foreach (var e in Zone.GetReadOnlyEntities().Where(e => !(bool)typeof(DoorPart).GetMethod("IsBareGround", All).Invoke(null, new object[] { e })).ToArray())
                    Zone.RemoveEntity(e);
                Zone.GenReservedCells.Clear();
                var source = Factory.CreateEntity("Crate");
                Assert.True(Zone.AddEntity(source, 12, 10));
                Assert.True(source.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("CandyCarrotSeed")));
            }
            internal bool Place(string family, Func<bool> authority = null)
            {
                var helper = typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationResidents");
                Assert.NotNull(helper, "Ordinary wilderness must realize the two resident sites.");
                var kind = (SpreadExplorationFamily)Enum.Parse(typeof(SpreadExplorationFamily), family);
                object[] args = { Zone, Factory, Terrain, kind, authority ?? (() => true), null, null };
                bool result = (bool)helper.GetMethod("TryPlace").Invoke(null, args);
                Owners = (Entity[])args[5]; Final = (Func<bool>)args[6];
                return result;
            }
            internal Func<bool> Proof(IEnumerable<Entity> owners) => (Func<bool>)typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState", All).Invoke(null, new object[] { Zone, owners });
            public void Dispose() => Scope.Dispose();
        }

        [TestCase("SeedKeepersPlot", "SpreadSeedKeeper", "SeedKeeperStock")]
        [TestCase("WaysideKitchen", "SpreadWaysideCook", "WaysideCookStock")]
        public void ResidentAddsOneRealTraderAndPreservesEveryOrdinaryOwner(string family, string blueprint, string table)
        {
            using (var f = new Fixture())
            {
                var original = f.Zone.GetReadOnlyEntities().ToArray(); var before = f.Proof(original);
                Assert.True(f.Place(family)); Assert.True(f.Final()); Assert.True(before());
                var actor = f.Owners.Single(e => e.HasTag("Creature")); Assert.AreEqual(blueprint, actor.BlueprintName);
                Assert.AreEqual(table, actor.GetPart<TraderPart>().StockTable);
                Assert.NotNull(actor.GetPart<ConversationPart>()); Assert.NotNull(actor.GetPart<BrainPart>());
                Assert.IsNotEmpty(actor.GetPart<InventoryPart>().Objects);
                foreach (var item in actor.GetPart<InventoryPart>().Objects)
                    Assert.AreSame(actor, item.GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual(original.Length + f.Owners.Length, f.Zone.EntityCount);
                foreach (var owner in f.Owners)
                {
                    var at = f.Zone.GetEntityPosition(owner);
                    Assert.False(f.Zone.GenReservedCells.Contains(at));
                    Assert.Greater(Math.Max(Math.Abs(at.x - 40), Math.Abs(at.y - 12)), 6);
                    Assert.True(f.Zone.TileState.Get(at.x, at.y)?.IsEmpty != false);
                    Assert.NotNull(owner.GetProperty(RoleKey));
                }
                Assert.False(f.Zone.GetCell(40, 12).BlocksMovement());
            }
        }

        [Test] public void PlotStartsWithFourDrySeedsOnPlantableGroundAndNoFreeHarvest()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place("SeedKeepersPlot")); var crops = f.Owners.Where(e => e.HasPart<CropPart>()).ToArray();
                CollectionAssert.AreEquivalent(new[] { "CandyCarrotCrop", "CandyCarrotCrop", "EmberwheatCrop", "EmberwheatCrop" }, crops.Select(e => e.BlueprintName));
                foreach (var crop in crops)
                {
                    var part = crop.GetPart<CropPart>(); Assert.Zero(part.GrowthStage); Assert.Zero(part.TicksInStage); Assert.Zero(part.MoistureTicks);
                    var cell = f.Zone.GetEntityCell(crop); Assert.True(cell.Objects.Any(e => e.HasTag("Terrain") && e.HasTag("Plantable")));
                    Assert.False(BarrenGroundRules.IsBarren(cell));
                }
                var sign = f.Owners.Single(e => e.BlueprintName == "Signpost"); Assert.NotNull(sign.GetPart<ExaminablePart>());
                Assert.False(f.Owners.Any(e => e.BlueprintName == "CandyCarrot" || e.BlueprintName == "Emberwheat"));
            }
        }

        [Test] public void KitchenHasCookingOnlyOvenAndAnUnownedAvailableCot()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place("WaysideKitchen")); var oven = f.Owners.Single(e => e.BlueprintName == "Oven");
                var cooking = oven.GetPart<CampfirePart>(); Assert.NotNull(cooking); Assert.False(cooking.AllowRest); Assert.False(cooking.FiniteCooking);
                var bed = f.Owners.Single(e => e.BlueprintName == "Bed").GetPart<BedPart>(); Assert.NotNull(bed); Assert.IsEmpty(bed.Owner); Assert.False(bed.Occupied);
                Assert.NotNull(f.Owners.Single(e => e.BlueprintName == "Chair").GetPart<ChairPart>());
                Assert.False(f.Owners.Any(e => e.HasPart<ContainerPart>()), "This site adds traded ingredients, not a second free food cache.");
            }
        }

        [TestCase("SeedKeepersPlot")][TestCase("WaysideKitchen")]
        public void ResidentStockUsesPrivateRandomRestoresFactoriesAndAvoidsImmediateTopUp(string family)
        {
            using (var f = new Fixture())
            {
                var oldL = LoadoutPart.Factory; var oldT = TraderPart.Factory; var oldLR = LoadoutPart.Rng; var oldTR = TraderPart.Rng;
                var foreign = new EntityFactory(); var forbidden = new ForbiddenRandom();
                try
                {
                    LoadoutPart.Factory = TraderPart.Factory = foreign; LoadoutPart.Rng = TraderPart.Rng = forbidden;
                    Assert.True(f.Place(family)); Assert.AreSame(foreign, LoadoutPart.Factory); Assert.AreSame(foreign, TraderPart.Factory);
                    Assert.AreSame(forbidden, LoadoutPart.Rng); Assert.AreSame(forbidden, TraderPart.Rng);
                    var actor = f.Owners.Single(e => e.HasTag("Creature")); var inventory = actor.GetPart<InventoryPart>().Objects.ToArray();
                    int drams = TradeSystem.GetDrams(actor); Assert.AreEqual(WorldClock.CurrentTick, actor.GetIntProperty(TraderRestockSystem.LastRestockProp, -1));
                    Assert.Zero(TraderRestockSystem.RestockZone(f.Zone, WorldClock.CurrentTick)); Assert.AreEqual(drams, TradeSystem.GetDrams(actor));
                    CollectionAssert.AreEquivalent(inventory, actor.GetPart<InventoryPart>().Objects);
                }
                finally { LoadoutPart.Factory = oldL; TraderPart.Factory = oldT; LoadoutPart.Rng = oldLR; TraderPart.Rng = oldTR; }
            }
        }

        [TestCase("authority")][TestCase("missing")][TestCase("reserved")][TestCase("wet")][TestCase("unplantable")]
        public void RefusalAddsNothingAndPreservesAllSources(string fault)
        {
            using (var f = new Fixture())
            {
                if (fault == "missing") f.Factory.Blueprints.Remove("CandyCarrotCrop");
                if (fault == "reserved") f.Zone.ForEachCell((cell, x, y) => f.Zone.GenReservedCells.Add((x, y)));
                if (fault == "wet") f.Zone.ForEachCell((cell, x, y) => f.Zone.TileState.WriteCoating(x, y, "water", 5));
                if (fault == "unplantable") foreach (var e in f.Zone.GetReadOnlyEntities()) e.Tags.Remove("Plantable");
                var original = f.Zone.GetReadOnlyEntities().ToArray(); var before = f.Proof(original);
                Assert.False(f.Place("SeedKeepersPlot", () => fault != "authority"));
                Assert.True(before()); CollectionAssert.AreEquivalent(original, f.Zone.GetReadOnlyEntities()); Assert.Null(f.Owners); Assert.Null(f.Final);
            }
        }

        [TestCase("revoke")][TestCase("source-change")][TestCase("malformed-stock")]
        public void CreationCallbackCannotCommitAgainstChangedAuthorityOrSources(string fault)
        {
            using (var f = new Fixture())
            {
                // Check the missing helper before looking up the newly authored blueprint for a behavioral RED.
                Assert.NotNull(typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationResidents"));
                bool allowed = true; var original = f.Zone.GetReadOnlyEntities().ToArray(); var source = original.First(e => e.BlueprintName == "Crate");
                f.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                f.Factory.Blueprints["SpreadSeedKeeper"].Parts["ReceiptCreated"] = new Dictionary<string, string>();
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = actor =>
                {
                    if (fault == "revoke") allowed = false;
                    if (fault == "source-change") source.SetIntProperty("callback-change", 17);
                    if (fault == "malformed-stock") actor.GetPart<InventoryPart>().Objects.Add(actor.GetPart<InventoryPart>().Objects[0]);
                };
                try
                {
                    Assert.False(f.Place("SeedKeepersPlot", () => allowed)); CollectionAssert.AreEquivalent(original, f.Zone.GetReadOnlyEntities());
                    if (fault == "source-change") Assert.AreEqual(17, source.GetIntProperty("callback-change"), "Refusal must not undo callback-owned state.");
                }
                finally { SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = null; }
            }
        }

        [Test] public void FinalProofPinsCropStateAndSecondCallCannotMintAnotherResident()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place("SeedKeepersPlot")); var final = f.Final; var owners = f.Owners;
                Assert.False(f.Place("SeedKeepersPlot")); Assert.True(final());
                owners.First(e => e.HasPart<CropPart>()).GetPart<CropPart>().MoistureTicks = 20; Assert.False(final());
            }
        }

        [TestCase("SeedKeepersPlot")][TestCase("WaysideKitchen")]
        public void ExistingTalkableFarmerKeepsItsLandmarkWithoutASecondDomesticSite(string family)
        {
            using (var f = new Fixture())
            {
                var farmer = f.Factory.CreateEntity("Farmer"); Assert.NotNull(farmer.GetPart<ConversationPart>());
                Assert.True(f.Zone.AddEntity(farmer, 65, 18));
                var original = f.Zone.GetReadOnlyEntities().ToArray(); var before = f.Proof(original);
                Assert.False(f.Place(family), "The existing landmark resident retains this domestic opportunity and its later ordinary naming pass.");
                Assert.True(before()); CollectionAssert.AreEquivalent(original, f.Zone.GetReadOnlyEntities());
                Assert.Null(f.Owners); Assert.Null(f.Final);
            }
        }

        [TestCase(64, "SeedKeepersPlot")][TestCase(64, "WaysideKitchen")]
        [TestCase(1729, "SeedKeepersPlot")][TestCase(1729, "WaysideKitchen")]
        public void NativeGenerationActuallyCommitsAndCachedAndSavedVisitsDoNotRefill(int seed, string family)
        {
            using (var scope = new HaulingContentScope())
            {
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
                var entries = manager.Exploration.Entries.Where(e => e.PlacementEligible && e.Family.ToString() == family).ToArray();
                Assert.IsNotEmpty(entries, "A real native manifest must select the resident family."); Zone zone = null;
                foreach (var entry in entries)
                {
                    scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(entry.ZoneID, int.MaxValue)));
                    var candidate = manager.GetZone(entry.ZoneID); Assert.NotNull(candidate, "Generated graph was rejected: seed=" + seed + " zone=" + entry.ZoneID);
                    if (manager.Exploration.DispositionFor(entry.ZoneID) == 2) { zone = candidate; break; }
                }
                Assert.NotNull(zone, "At least one wholly unedited native zone must expose this resident.");
                var actor = zone.GetReadOnlyEntities().Single(e => e.GetProperty(RoleKey) == "resident");
                var inventory = actor.GetPart<InventoryPart>(); var removed = inventory.Objects[0]; Assert.True(inventory.RemoveObject(removed)); TradeSystem.SetDrams(actor, 7);
                var crop = zone.GetReadOnlyEntities().FirstOrDefault(e => e.GetProperty(RoleKey) == "crop");
                if (crop != null) { crop.GetPart<CropPart>().GrowthStage = 1; crop.GetPart<CropPart>().TicksInStage = 3; crop.GetPart<CropPart>().MoistureTicks = 8; }
                string goods = Goods(actor); var owners = zone.GetReadOnlyEntities().ToArray();
                manager.UnloadZone(zone.ZoneID); Assert.AreSame(zone, manager.GetZone(zone.ZoneID)); CollectionAssert.AreEquivalent(owners, zone.GetReadOnlyEntities()); Assert.AreEqual(goods, Goods(actor));
                manager.SetActiveZone(zone); var player = scope.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(player, 40, 12));
                var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("resident", "native-resident", manager, null, player));
                var restored = loaded.ZoneManager.ActiveZone; var savedActor = restored.GetReadOnlyEntities().Single(e => e.ID == actor.ID);
                Assert.AreNotSame(actor, savedActor); Assert.AreEqual(goods, Goods(savedActor)); Assert.False(savedActor.GetPart<InventoryPart>().Objects.Any(e => e.ID == removed.ID));
                Assert.AreEqual(2, loaded.ZoneManager.Exploration.DispositionFor(zone.ZoneID));
                if (crop != null)
                {
                    var savedCrop = restored.GetReadOnlyEntities().Single(e => e.ID == crop.ID).GetPart<CropPart>();
                    Assert.AreEqual(1, savedCrop.GrowthStage); Assert.AreEqual(3, savedCrop.TicksInStage); Assert.AreEqual(8, savedCrop.MoistureTicks);
                }
                Assert.AreSame(restored, loaded.ZoneManager.GetZone(restored.ZoneID));
                TestContext.WriteLine(family + " actual native witness seed=" + seed + " zone=" + zone.ZoneID);
            }
        }
        [TestCase(64, "Overworld.11.8.0", "SpreadSeedKeeper")][TestCase(64, "Overworld.12.11.0", "SpreadWaysideCook")]
        [TestCase(1729, "Overworld.11.8.0", "SpreadSeedKeeper")][TestCase(1729, "Overworld.12.11.0", "SpreadWaysideCook")]
        public void EachNearbyNativeExampleActuallyCommitsWithoutEditingTerrain(int seed, string id, string actor)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(id, int.MaxValue)));
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true); var zone = manager.GetZone(id);
                Assert.NotNull(zone); Assert.AreEqual(2, manager.Exploration.DispositionFor(id), "Assignment alone is not a usable nearby site: " + seed + " " + id);
                Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == actor && e.GetProperty(RoleKey) == "resident"));
            }
        }
        static string Goods(Entity actor) => TradeSystem.GetDrams(actor) + "|" + actor.GetIntProperty(TraderRestockSystem.LastRestockProp) + "|" + string.Join(";", actor.GetPart<InventoryPart>().Objects.OrderBy(e => e.ID).Select(e => e.ID + ":" + e.BlueprintName + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1)));
    }
}
