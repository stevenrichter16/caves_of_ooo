using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Real command fixtures; no simulated replacements for the production actions.
    public abstract class FiftyWorldFixture
    {
        protected EntityFactory Factory;
        protected Zone Zone;
        protected Entity Actor;
        protected TurnManager Clock;
        protected InventoryPart Pack => Actor.GetPart<InventoryPart>();
        EntityFactory oldCrop, oldSeed, oldHarvest, oldReaction;
        TurnManager oldClock;
        Entity oldWorld;
        Zone oldZone;
        SettlementManager oldSettlement;
        Func<int> oldTickProvider;
        Dictionary<string, LiquidDefinition> oldLiquids;
        bool oldInitialized;
        static readonly FieldInfo Active = typeof(TurnManager).GetField("<Active>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo CurrentSettlement = typeof(SettlementManager).GetField("<Current>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo Registry = typeof(LiquidRegistry).GetField("_byId", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo Initialized = typeof(LiquidRegistry).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp] public void SetUpWorld()
        {
            oldSettlement = SettlementManager.Current; oldTickProvider = MessageLog.TickProvider; CurrentSettlement.SetValue(null, null);
            oldClock = TurnManager.Active; oldWorld = TurnManager.World; oldZone = SettlementRuntime.ActiveZone;
            oldCrop = CropSystem.Factory; oldSeed = SeedPart.Factory;
            oldHarvest = HarvestablePart.Factory; oldReaction = MaterialReactionResolver.Factory;
            oldLiquids = new Dictionary<string, LiquidDefinition>((Dictionary<string, LiquidDefinition>)Registry.GetValue(null));
            oldInitialized = (bool)Initialized.GetValue(null);
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,
                "Resources/Content/Data/LiquidDefinitions"), "*.json").Select(File.ReadAllText));
            Factory = new EntityFactory();
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            CropSystem.Factory = SeedPart.Factory = HarvestablePart.Factory = MaterialReactionResolver.Factory = Factory;
            TurnManager.World = null; Clock = new TurnManager(); MessageLog.TickProvider = () => Clock.TickCount;
            Zone = new Zone("fifty-world-tests"); SettlementRuntime.ActiveZone = Zone;
            Actor = Factory.CreateEntity("Player"); Assert.NotNull(Actor);
            Assert.True(Zone.AddEntity(Actor, 10, 10)); Pack.MaxWeight = 200;
        }
        [TearDown] public void TearDownWorld()
        {
            CropSystem.Factory = oldCrop; SeedPart.Factory = oldSeed;
            HarvestablePart.Factory = oldHarvest; MaterialReactionResolver.Factory = oldReaction;
            CurrentSettlement.SetValue(null, oldSettlement); MessageLog.TickProvider = oldTickProvider;
            SettlementRuntime.ActiveZone = oldZone; TurnManager.World = oldWorld; Active.SetValue(null, oldClock);
            var registry = (Dictionary<string, LiquidDefinition>)Registry.GetValue(null);
            registry.Clear(); foreach (var pair in oldLiquids) registry[pair.Key] = pair.Value;
            Initialized.SetValue(null, oldInitialized);
        }
        protected void WithoutClock() => Active.SetValue(null, null);
        protected Entity Place(string blueprint, int x = 11, int y = 10)
        { var entity = Factory.CreateEntity(blueprint); Assert.NotNull(entity, blueprint); Assert.True(Zone.AddEntity(entity, x, y)); return entity; }
        protected Entity Carry(string blueprint)
        { var entity = Factory.CreateEntity(blueprint); Assert.NotNull(entity, blueprint); Assert.True(Pack.AddObject(entity)); return entity; }
        protected Entity Bed(int x = 11, int y = 10, bool prepared = true)
        { var bed = Factory.CreateEntity("Grass"); bed.SetTag("Plantable"); if (prepared) bed.AddPart(new CultivatedSoilPart()); Assert.True(Zone.AddEntity(bed, x, y)); return bed; }
        protected Entity Crop(int stage = 0, int moisture = 0, int x = 11, int y = 10, bool prepared = true)
        {
            Bed(x, y, prepared); var entity = Place("CandyCarrotCrop", x, y); var crop = entity.GetPart<CropPart>();
            crop.HarvestAtMaturity = true; crop.GrowthStage = stage; crop.MoistureTicks = moisture;
            crop.YieldBlueprint = "CandyCarrot"; crop.YieldCount = 2; crop.SeedYieldBlueprint = "CandyCarrotSeed"; crop.SeedYieldCount = 1;
            Assert.True(CropTime.Reconcile(crop, Zone, Clock.TickCount)); return entity;
        }
        protected Entity Seed()
        { var seed = Carry("CandyCarrotSeed"); seed.GetPart<SeedPart>().RequireCultivatedSoil = true; return seed; }
        protected Entity Flask(int volume = 3, string liquid = "water", int capacity = 12)
        { var entity = Carry("LiquidFlask"); var p = entity.GetPart<LiquidVesselPart>(); p.Volume = volume; p.LiquidId = volume == 0 ? "" : liquid; p.Capacity = capacity; return entity; }
        protected Entity Skin(int units = 0, int capacity = 3)
        { var entity = Carry("Waterskin"); var p = entity.GetPart<WaterskinPart>(); p.Charges = units; p.Capacity = capacity; return entity; }
        protected Entity Pool(int volume = 5, string liquid = "water", int x = 11, int y = 10)
        { var entity = Place("PouredLiquidPool", x, y); entity.GetPart<LiquidPoolPart>().LiquidId = liquid; entity.GetPart<LiquidPoolPart>().Volume = volume; return entity; }
        protected static int Units(Entity vessel) => vessel.GetPart<WaterskinPart>()?.Charges ?? vessel.GetPart<LiquidVesselPart>().Volume;
        protected bool Act(Entity entity, string command) => InventorySystem.PerformAction(Actor, entity, command, Zone);
        protected InventoryAction[] Actions(Entity entity) => InventorySystem.GetActions(Actor, entity).ToArray();
        protected string Choice(Entity source, string prefix, Func<InventoryAction, bool> match = null)
        {
            var choices = Actions(source).Where(a => a.Command.StartsWith(prefix, StringComparison.Ordinal) && (match == null || match(a))).ToArray();
            Assert.AreEqual(1, choices.Length, "Expected one actual menu choice for " + prefix); return choices[0].Command;
        }
        protected static string Id(Entity entity) => Uri.EscapeDataString(entity.ID);
        protected string Pour(Entity vessel, bool one, int x = 11, int y = 10)
            => Choice(vessel, one ? "PourOneLiquidVessel|" : "PourLiquidVessel|", a => a.Command.EndsWith("|" + x + "|" + y, StringComparison.Ordinal));
        protected int Count(string blueprint) => Pack.Objects.Concat(Zone.GetReadOnlyEntities())
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        protected int PoolUnits(int x = 11, int y = 10) => Zone.GetCell(x, y).Objects
            .Where(e => e.HasPart<LiquidPoolPart>()).Sum(e => e.GetPart<LiquidPoolPart>().Volume);
        protected void FailAfter() => Actor.AddPart(new FiftyWorldThrowAfterPart());
    }
    public sealed class FiftyWorldThrowAfterPart : Part
    {
        public override bool HandleEvent(GameEvent e)
        { if (e.ID == "AfterInventoryAction") throw new InvalidOperationException("world receipt rollback probe"); return true; }
    }
}
