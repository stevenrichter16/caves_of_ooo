using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Controlled finite resource fixtures through real commands,
    /// owner turns and replacement saving. Not an ordinary discovery journey.</summary>
    [Scenario(name: "World Resource Audit", category: "World", description: "Finite water, cultivation, harvest and cooking-fire commands with saved aftermath.")]
    public sealed class FiftyWorldBench : IScenario
    {
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            using (var scope = new DetachedScope())
            {
                CropSystem.Factory = SeedPart.Factory = HarvestablePart.Factory = MaterialReactionResolver.Factory = ctx.Factory;
                var zone = new Zone("Overworld.0.0.0"); SettlementRuntime.ActiveZone = zone;
                var actor = ctx.Factory.CreateEntity("Player"); var brain = actor.GetPart<BrainPart>(); if (brain != null) actor.RemovePart(brain);
                Require(zone.AddEntity(actor, 10, 10), "actor placement"); var pack = actor.GetPart<InventoryPart>(); pack.MaxWeight = 200;
                var turns = new TurnManager(); turns.AddEntity(actor); Require(turns.ProcessUntilPlayerTurn() == actor, "owner turn");
                MessageLog.TickProvider = () => turns.TickCount;
                Entity Place(string blueprint, int x = 11, int y = 10) { var e = ctx.Factory.CreateEntity(blueprint); Require(e != null && zone.AddEntity(e, x, y), blueprint + " placement"); return e; }
                Entity Carry(string blueprint) { var e = ctx.Factory.CreateEntity(blueprint); Require(e != null && pack.AddObject(e), blueprint + " carry"); return e; }
                bool Act(Entity item, string command)
                {
                    int tick = turns.TickCount;
                    if (!InventorySystem.PerformAction(actor, item, command, zone)) return false;
                    turns.EndTurn(actor, zone); Require(turns.ProcessUntilPlayerTurn() == actor, "owner turn retained");
                    Require(turns.TickCount == tick + 10, "successful command pays one ordinary owner action"); return true;
                }
                string Choice(Entity item, string prefix, Func<InventoryAction, bool> predicate = null)
                {
                    var choices = InventorySystem.GetActions(actor, item).Where(a => a.Command.StartsWith(prefix, StringComparison.Ordinal) && (predicate == null || predicate(a))).ToArray();
                    Require(choices.Length == 1, "one visible choice for " + prefix); return choices[0].Command;
                }
                int Units(Entity vessel) => vessel.GetPart<WaterskinPart>()?.Charges ?? vessel.GetPart<LiquidVesselPart>().Volume;
                var seep = Place("GroveSeep", 9, 10); seep.GetPart<LiquidPoolPart>().Volume = 2;
                actor.ApplyEffect(new ParchedEffect()); Check("finite_well_drink_paid", Act(seep, "DrawWaterAtWell") && seep.GetPart<LiquidPoolPart>().Volume == 1 && !actor.HasEffect<ParchedEffect>());
                var flask = Carry("LiquidFlask"); flask.GetPart<LiquidVesselPart>().LiquidId = "water"; flask.GetPart<LiquidVesselPart>().Volume = 3;
                var skin = Carry("Waterskin"); skin.GetPart<WaterskinPart>().Capacity = 2;
                Check("water_transfer_paid_conserved", Act(flask, Choice(flask, "TransferWater|")) && Units(flask) == 1 && Units(skin) == 2);
                var soil = Place("Grass"); soil.SetTag("Plantable"); soil.AddPart(new CultivatedSoilPart());
                var oldCrop = Place("KnotflaxCrop"); Check("clear_young_crop_paid_no_refund", Act(oldCrop, "ClearCrop") && zone.GetEntityCell(oldCrop) == null && !pack.Objects.Any(e => e.HasPart<SeedPart>()));
                var seed = Carry("KnotflaxSeed");
                Check("adjacent_seed_plant_paid", Act(seed, Choice(seed, "PlantSeedAt|", a => a.Command.EndsWith("|11|10", StringComparison.Ordinal)))
                    && zone.GetEntityPosition(actor) == (10, 10) && !pack.Objects.Contains(seed));
                var cropOwner = zone.GetCell(11, 10).Objects.Single(e => e.HasPart<CropPart>()); var crop = cropOwner.GetPart<CropPart>();
                Check("one_pour_irrigates_real_crop", Act(flask, Choice(flask, "PourOneLiquidVessel|", a => a.Command.EndsWith("|11|10", StringComparison.Ordinal)))
                    && Units(flask) == 0 && crop.MoistureTicks > 0 && crop.MoistureTicks <= 40);
                Check("one_unit_enters_soil_not_recoverable_pool", !zone.GetCell(11, 10).Objects.Any(e => e.HasPart<LiquidPoolPart>()) && CultivatedSoilPart.IsCultivated(zone, zone.GetCell(11, 10)));
                // Explicit elapsed-clock fixture: existing CropTime owns all growth.
                turns.AdvanceClock(crop.TicksPerStage * 2 * CropTime.WorldTicksPerUnit);
                CropTime.Reconcile(crop, zone, turns.TickCount); Check("finite_irrigation_reaches_ripe_crop", crop.GrowthStage == 2);
                string produce = crop.YieldBlueprint, returnedSeed = crop.SeedYieldBlueprint; int yield = crop.YieldCount, seedYield = crop.SeedYieldCount;
                Check("gather_ripe_crop_paid", Act(cropOwner, "GatherCrop") && zone.GetEntityCell(cropOwner) == null
                    && pack.Objects.Where(e => e.BlueprintName == produce).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1) == yield
                    && pack.Objects.Where(e => e.BlueprintName == returnedSeed).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1) == seedYield);
                var prop = Place("PhysicalObject"); prop.AddPart(new ThermalPart { Temperature = 450, FlameTemperature = 300 }); prop.AddPart(new FuelPart { FuelMass = 20, MaxFuel = 20 }); prop.AddPart(new MaterialPart { MaterialTagsRaw = "Wood,Flammable" }); Require(prop.ApplyEffect(new BurningEffect(1)), "controlled wooden prop must actually burn");
                Check("one_water_douses_real_prop_paid", Act(skin, Choice(skin, "DouseWorld|")) && Units(skin) == 1 && !prop.HasEffect<BurningEffect>());
                var fire = Place("SpreadCookingCoals", 10, 11); fire.GetPart<FuelPart>().FuelMass = 0; fire.GetPart<ThermalPart>().Temperature = 25;
                Carry("SalvagedTimber"); Check("timber_feeds_finite_cold_coals_paid", Act(fire, "FeedCookingFire") && fire.GetPart<FuelPart>().FuelMass == 10
                    && fire.GetPart<ThermalPart>().Temperature < CookingService.MinimumFiniteCookingTemperature && !pack.Objects.Any(e => e.BlueprintName == "SalvagedTimber"));
                int beforeRefusal = turns.TickCount; Check("cold_fuel_requires_real_flame_free_refusal", !Act(fire, "RelightCookingFire") && turns.TickCount == beforeRefusal);
                var torch = Carry("Torch"); Require(InventorySystem.Equip(actor, torch), "controlled held ignition source");
                Check("held_torch_relights_coals_paid", Act(fire, "RelightCookingFire") && fire.GetPart<ThermalPart>().Temperature >= CookingService.MinimumFiniteCookingTemperature && !fire.GetPart<CampfirePart>().AllowRest);
                var gear = ctx.Factory.CreateEntity("PhysicalObject"); gear.GetPart<PhysicsPart>().Takeable = true; Require(pack.AddObject(gear), "controlled wet gear"); gear.ApplyEffect(new WetEffect(.5f));
                Check("selected_gear_dries_paid", Act(fire, "DryGear|" + Uri.EscapeDataString(gear.ID)) && (gear.GetEffect<WetEffect>()?.Moisture ?? 0) <= .25f);
                beforeRefusal = turns.TickCount; Check("spent_timber_cannot_refeed_free_refusal", !Act(fire, "FeedCookingFire") && turns.TickCount == beforeRefusal && fire.GetPart<FuelPart>().FuelMass <= 10);
                var manager = OverworldZoneManager.CreateDetached(ctx.Factory, 64); manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
                float fuel = fire.GetPart<FuelPart>().FuelMass; GameSessionState loaded;
                using (var bytes = new MemoryStream()) { GameSessionState.Capture(RunId, "controlled world resource bench", manager, turns, actor).Save(new SaveWriter(bytes)); bytes.Position = 0; loaded = GameSessionState.Load(new SaveReader(bytes, null)); }
                var restoredZone = loaded.ZoneManager.ActiveZone; var restoredSkin = loaded.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == skin.ID);
                Check("replacement_save_retains_finite_aftermath", loaded.Player != actor && Units(restoredSkin) == 1
                    && restoredZone.GetReadOnlyEntities().Single(e => e.ID == seep.ID).GetPart<LiquidPoolPart>().Volume == 1
                    && restoredZone.GetReadOnlyEntities().Single(e => e.ID == fire.ID).GetPart<FuelPart>().FuelMass == fuel
                    && !restoredZone.GetReadOnlyEntities().Any(e => e.ID == cropOwner.ID || e.ID == oldCrop.ID));
            }
        }
        static void Require(bool value, string name) { if (!value) throw new InvalidOperationException("World audit fixture: " + name); }
        void Check(string name, bool value) { Cases++; if (!value) Failures++; Audit.Add((value ? "PASS " : "FAIL ") + name); }
        sealed class DetachedScope : IDisposable
        {
            readonly TurnManager active = TurnManager.Active; readonly Entity world = TurnManager.World; readonly Zone zone = SettlementRuntime.ActiveZone;
            readonly SettlementManager settlement = SettlementManager.Current;
            readonly CavesOfOoo.Data.EntityFactory crop = CropSystem.Factory, seed = SeedPart.Factory, harvest = HarvestablePart.Factory, reaction = MaterialReactionResolver.Factory;
            readonly Action<string> onMessage = MessageLog.OnMessage; readonly Func<int> tickProvider = MessageLog.TickProvider;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries(); readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly List<SpellFxSequence> spells = SpellFxBus.Drain(); readonly List<AsciiFxRequest> ascii = AsciiFxBus.Drain();
            readonly Queue<AsciiFxRequest> queue; readonly FieldInfo cosmeticField; readonly int cosmeticSerial;
            public DetachedScope()
            {
                queue = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                cosmeticField = typeof(SpellFxCapture).GetField("_cosmeticSerial", BindingFlags.NonPublic | BindingFlags.Static); cosmeticSerial = (int)cosmeticField.GetValue(null);
                TurnManager.World = null; MessageLog.OnMessage = null; typeof(SettlementManager).GetProperty("Current").SetValue(null, null);
            }
            public void Dispose()
            {
                foreach (var request in AsciiFxBus.Drain()) AsciiFxBus.Release(request); foreach (var request in ascii) queue.Enqueue(request);
                SpellFxBus.Clear(); foreach (var spell in spells) SpellFxBus.Emit(spell); cosmeticField.SetValue(null, cosmeticSerial);
                MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = onMessage; MessageLog.TickProvider = tickProvider;
                TurnManager.World = world; SettlementRuntime.ActiveZone = zone; typeof(TurnManager).GetProperty("Active").SetValue(null, active); typeof(SettlementManager).GetProperty("Current").SetValue(null, settlement);
                CropSystem.Factory = crop; SeedPart.Factory = seed; HarvestablePart.Factory = harvest; MaterialReactionResolver.Factory = reaction;
            }
        }
    }
}
