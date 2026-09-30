using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual content through the existing inventory commands. Sources are
    /// placed by this fixture; distribution and native input are separate checks.</summary>
    public sealed class SpreadSpecialistGatheringTests
    {
        DensityLootTestScope scope;
        EntityFactory oldHarvestFactory;
        EntityFactory Factory => scope.Factory;
        Zone zone;
        Entity player;
        InventoryPart Inventory => player.GetPart<InventoryPart>();

        [SetUp] public void Setup()
        {
            scope = new DensityLootTestScope();
            oldHarvestFactory = HarvestablePart.Factory;
            HarvestablePart.Factory = Factory;
            BrewRuleRegistry.InitializeFromJson(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Alchemy/BrewRules.json")));
            zone = new Zone("Overworld.11.9.0");
            player = Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(player, 10, 10));
        }
        [TearDown] public void Cleanup()
        {
            HarvestablePart.Factory = oldHarvestFactory;
            BrewRuleRegistry.ResetForTests();
            scope?.Dispose();
        }
        Entity Place(string blueprint, int x = 11, int y = 10)
        {
            var entity = Factory.CreateEntity(blueprint);
            Assert.NotNull(entity, blueprint);
            Assert.True(zone.AddEntity(entity, x, y));
            return entity;
        }
        static int Units(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        bool Harvest(Entity source) => InventorySystem.ExecuteCommand(
            new PerformInventoryActionCommand(source, "Harvest"), player, zone).Success;
        Entity Gather(string source, string product)
        {
            Assert.True(Harvest(Place(source)));
            return Inventory.Objects.First(e => e.BlueprintName == product);
        }
        Entity Brew(Entity reagent)
        {
            Assert.False(AlchemyStillPart.IsNearStill(player, zone));
            Assert.True(InventorySystem.ExecuteCommand(new BrewReagentsCommand(new[] { reagent }, Factory), player, zone).Success);
            return Inventory.Objects.Single(e => e.HasPart<BrewItemPart>());
        }

        [TestCase("StoneburrPatch", "StoneburrSeed")]
        [TestCase("FrostLichenPatch", "FrostLichen")]
        public void RealPatchRequiresReachAndYieldsFiniteStockExactlyOnce(string source, string product)
        {
            var patch = Place(source, 16);
            Assert.False(Harvest(patch));
            Assert.AreEqual(0, Units(player, product));
            Assert.False(patch.GetPart<HarvestablePart>().Harvested);
            Assert.True(zone.MoveEntity(patch, 11, 10));
            Assert.True(Harvest(patch));
            int earned = Units(player, product);
            Assert.That(earned, Is.InRange(1, 2));
            Assert.Null(zone.GetEntityCell(patch));
            Assert.True(patch.GetPart<HarvestablePart>().Harvested);
            Assert.False(Harvest(patch));
            Assert.AreEqual(earned, Units(player, product));
            Assert.False(WorldInteractionSystem.GatherActions(patch, player).Any(a => a.Command == "Harvest"));
        }

        [Test]
        public void GatheredSeedCanBeBrewedInTheFieldAndDrunkForRealDamageReduction()
        {
            var seed = Gather("StoneburrPatch", "StoneburrSeed");
            int before = Units(player, "StoneburrSeed");
            var tonic = Brew(seed);
            Assert.AreEqual(before - 1, Units(player, "StoneburrSeed"));
            Assert.AreEqual("Tonic", tonic.GetPart<BrewItemPart>().Form);
            Assert.False(player.HasEffect<StoneskinEffect>());
            Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(tonic, "ApplyTonic"), player, zone).Success);
            Assert.AreEqual(0, Units(player, "BrewedTonic"));
            var effect = player.GetEffect<StoneskinEffect>();
            Assert.NotNull(effect); Assert.AreEqual(2, effect.Reduction); Assert.AreEqual(30, effect.Duration);
            var control = Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(control, 12, 10));
            int protectedBefore = player.GetStat("Hitpoints").Value, ordinaryBefore = control.GetStat("Hitpoints").Value;
            CombatSystem.ApplyDamage(player, 7, null, zone);
            CombatSystem.ApplyDamage(control, 7, null, zone);
            Assert.AreEqual(5, protectedBefore - player.GetStat("Hitpoints").Value);
            Assert.AreEqual(7, ordinaryBefore - control.GetStat("Hitpoints").Value);
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(tonic, "ApplyTonic"), player, zone).Success);
        }

        [Test]
        public void GatheredLichenQuenchesAtAForgeAndFreezesTheDefenderOnlyOnADamagingSuccessfulRoll()
        {
            var coating = Brew(Gather("FrostLichenPatch", "FrostLichen"));
            Assert.AreEqual("Coating", coating.GetPart<BrewItemPart>().Form);
            var weapon = Factory.CreateEntity("Dagger"); Assert.True(Inventory.AddObject(weapon));
            string original = weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw;
            var command = new TemperWeaponCommand(weapon, coating);
            Assert.False(InventorySystem.ExecuteCommand(command, player, zone).Success);
            Assert.True(Inventory.CanConsumeOne(coating));
            Assert.AreEqual(original, weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
            Place("TinkersForge", 10, 11);
            Assert.True(InventorySystem.ExecuteCommand(command, player, zone).Success);
            Assert.False(Inventory.CanConsumeOne(coating));
            var melee = command.TemperedWeapon.GetPart<MeleeWeaponPart>();
            Assert.That(melee.OnHitEffectsRaw, Does.Contain("Frozen,40,,0,2"));
            var target = Factory.CreateEntity("MarlbackScrabbler"); Assert.True(zone.AddEntity(target, 12, 10));
            var damage = new Damage(1);
            int successSeed = Enumerable.Range(0, 100).First(s => new System.Random(s).Next(100) < 40);
            int missSeed = Enumerable.Range(0, 100).First(s => new System.Random(s).Next(100) >= 40);
            OnHitWeaponEffects.Apply(melee, damage, 0, target, player, zone, new System.Random(successSeed));
            Assert.False(target.HasEffect<FrozenEffect>(), "No freeze through fully blocked damage.");
            OnHitWeaponEffects.Apply(melee, damage, 1, target, player, zone, new System.Random(missSeed));
            Assert.False(target.HasEffect<FrozenEffect>(), "No freeze on a failed coating roll.");
            OnHitWeaponEffects.Apply(melee, damage, 1, target, player, zone, new System.Random(successSeed));
            Assert.True(target.HasEffect<FrozenEffect>());
            Assert.False(target.GetEffect<FrozenEffect>().AllowAction(target));
            Assert.False(player.HasEffect<FrozenEffect>(), "The forge path does not drink the coating.");
            Assert.False(InventorySystem.ExecuteCommand(new TemperWeaponCommand(command.TemperedWeapon, coating), player, zone).Success);
        }

        [TestCase("StoneburrPatch", "StoneburrSeed")]
        [TestCase("FrostLichenPatch", "FrostLichen")]
        public void BatchStillGatePreservesGatheredStockBeforeTheStationIsAvailable(string source, string product)
        {
            var reagent = Gather(source, product);
            int before = Units(player, product);
            var command = new BrewReagentsCommand(new[] { reagent }, Factory, 2);
            Assert.False(InventorySystem.ExecuteCommand(command, player, zone).Success);
            Assert.AreEqual(before, Units(player, product)); Assert.AreEqual(0, Units(player, "BrewedTonic"));
            Place("AlchemyStill", 10, 11);
            Assert.True(InventorySystem.ExecuteCommand(command, player, zone).Success);
            int made = Units(player, "BrewedTonic");
            Assert.That(made, Is.InRange(1, 2));
            Assert.AreEqual(before - made, Units(player, product));
        }

        [TestCase("StoneburrPatch", "StoneburrSeed", false)]
        [TestCase("StoneburrPatch", "StoneburrSeed", true)]
        [TestCase("FrostLichenPatch", "FrostLichen", false)]
        [TestCase("FrostLichenPatch", "FrostLichen", true)]
        public void FullSavePreservesUnharvestedSourcesOrEarnedStockWithoutRespawning(string source, string product, bool harvested)
        {
            // Retention belongs to an accepted exploration site. A disabled
            // manager around a hand-placed fixture intentionally unloads it.
            scope.Seed(64);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            Assert.True(zone.RemoveEntity(player));
            zone = manager.GetZone("Overworld.11.9.0");
            Assert.NotNull(zone);
            Assert.AreEqual(2, manager.Exploration.DispositionFor(zone.ZoneID), "Actual north field alembic committed.");
            var patch = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == source);
            Assert.True(patch.Properties.ContainsKey("SpreadWorksite.Role"));
            var at = zone.GetEntityCell(patch);
            var approach = new[] { (at.X - 1, at.Y), (at.X + 1, at.Y), (at.X, at.Y - 1), (at.X, at.Y + 1) }
                .Select(p => zone.GetCell(p.Item1, p.Item2)).FirstOrDefault(c => c != null
                    && zone.CanPlaceFootprint(player, c.X, c.Y)
                    && !c.Objects.Any(e => e.HasTag("Creature") || e.HasPart<TriggerOnStepPart>()));
            Assert.NotNull(approach, "Legal adjacent footprint for the actual generated patch.");
            Assert.True(zone.AddEntity(player, approach.X, approach.Y));
            string sourceId = patch.ID;
            if (harvested) Assert.True(Harvest(patch));
            int earned = Units(player, product);
            manager.SetActiveZone(zone);
            var state = GameSessionState.Capture("specialist-harvest", "core-only-fixture", manager, null, player);
            GameSessionState loaded;
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                loaded = GameSessionState.Load(new SaveReader(stream, Factory));
            }
            var returned = loaded.ZoneManager.GetZone(zone.ZoneID);
            Assert.AreNotSame(zone, returned);
            Assert.AreEqual(earned, Units(loaded.Player, product));
            var restored = returned.GetReadOnlyEntities().SingleOrDefault(e => e.ID == sourceId);
            if (harvested) Assert.Null(restored);
            else
            {
                Assert.NotNull(restored); Assert.False(restored.GetPart<HarvestablePart>().Harvested);
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(restored, "Harvest"), loaded.Player, returned).Success);
                int first = Units(loaded.Player, product); Assert.That(first, Is.InRange(1, 2));
                Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(restored, "Harvest"), loaded.Player, returned).Success);
                Assert.AreEqual(first, Units(loaded.Player, product));
            }
            loaded.ZoneManager.UnloadZone(zone.ZoneID);
            Assert.AreSame(returned, loaded.ZoneManager.GetZone(zone.ZoneID));
            Assert.False(returned.GetReadOnlyEntities().Any(e => e.ID == sourceId), "Loading and accessing do not refill the patch.");
        }
    }
}
