using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftyWorldAdversarialTests : FiftyWorldFixture
    {
        [TearDown] public void ClearCreationHook() => FiftyWorldCreationProbe.Created = null;
        void Hook(string blueprint, Action<Entity> callback)
        { Factory.RegisterPartType<FiftyWorldCreationProbe>(); Factory.Blueprints[blueprint].Parts["FiftyWorldCreationProbe"] = new Dictionary<string, string>(); FiftyWorldCreationProbe.Created = callback; }
        Entity FireProp()
        { var owner = Place("PhysicalObject"); owner.AddPart(new ThermalPart { Temperature = 450, FlameTemperature = 300 }); owner.AddPart(new FuelPart { FuelMass = 10, MaxFuel = 10 }); owner.AddPart(new MaterialPart { MaterialTagsRaw = "Wood,Flammable" }); Assert.True(owner.ApplyEffect(new BurningEffect(1)), "The fixture must actually be burning."); return owner; }
        Entity Gear(float moisture = .25f)
        { var item = Factory.CreateEntity("PhysicalObject"); item.GetPart<PhysicsPart>().Takeable = true; Assert.True(Pack.AddObject(item)); item.ApplyEffect(new WetEffect(moisture)); return item; }
        [Test] public void FiniteWellRollbackRestoresTheSupplyAndExactThirstEffect()
        {
            var seep = Place("GroveSeep"); var thirst = new ParchedEffect { Stacks = 3 }; Actor.ApplyEffect(thirst); FailAfter();
            Assert.False(Act(seep, "DrawWaterAtWell")); Assert.AreEqual(40, seep.GetPart<LiquidPoolPart>().Volume); Assert.AreSame(thirst, Actor.GetEffect<ParchedEffect>()); Assert.AreEqual(3, thirst.Stacks);
        }
        [Test] public void MixedSeepCannotEscapePurityThroughItsWellPart()
        {
            var seep = Place("GroveSeep"); Pool(4, "acid"); var skin = Skin(); Actor.ApplyEffect(new ParchedEffect());
            Assert.False(Act(seep, "DrawWaterAtWell")); Assert.False(Act(skin, "FillWaterskin")); Assert.AreEqual(40, seep.GetPart<LiquidPoolPart>().Volume); Assert.AreEqual(0, Units(skin)); Assert.True(Actor.HasEffect<ParchedEffect>());
        }
        [Test] public void GroundIndexCannotTurnACarriedWellAliasIntoAService()
        {
            var well = Place("Well"); well.GetPart<PhysicsPart>().InInventory = Actor; Actor.ApplyEffect(new ParchedEffect());
            Assert.False(Act(well, "DrawWaterAtWell")); Assert.True(Actor.HasEffect<ParchedEffect>()); Assert.False(Actions(well).Any(a => a.Command == "DrawWaterAtWell"));
        }
        [Test] public void WellUsesActualBodyContactAndRejectsAnUnregisteredFootprintChange()
        {
            var well = Factory.CreateEntity("Well"); var body = new SpatialFootprintPart { CellsRaw = "0,0;-1,0;-2,0" }; well.AddPart(body); Assert.True(Zone.AddEntity(well, 13, 10));
            Assert.True(Act(well, "DrawWaterAtWell")); body.CellsRaw = "0,0"; Actor.ApplyEffect(new ParchedEffect());
            Assert.False(Act(well, "DrawWaterAtWell")); Assert.True(Actor.HasEffect<ParchedEffect>());
        }
        [Test] public void RestSeesAHostileBodyInsideEightCellsEvenWhenItsAnchorIsFartherAway()
        {
            var fire = Place("Campfire"); var enemy = new Entity { ID = "large-rest-hostile" }; enemy.AddPart(new PhysicsPart()); enemy.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-1,0;-2,0;-3,0;-4,0" }); enemy.AddPart(new BrainPart());
            enemy.GetPart<BrainPart>().SetPersonallyHostile(Actor, false); Assert.True(Zone.AddEntity(enemy, 22, 10)); Actor.GetStat("Hitpoints").BaseValue = 3;
            Assert.False(Act(fire, "RestAtCampfire")); Assert.AreEqual(0, Clock.TickCount); Assert.AreEqual(3, Actor.GetStatValue("Hitpoints"));
            Zone.RemoveEntity(enemy); Assert.True(Act(fire, "RestAtCampfire"));
        }
        [Test] public void FailedOuterCampfireRestDoesNotAdvanceClockOrCureBleeding()
        {
            var fire = Place("Campfire"); var bleed = new BleedingEffect(20, "1d1"); Actor.ApplyEffect(bleed); Actor.GetStat("Hitpoints").BaseValue = 3;
            int tick = Clock.TickCount; FailAfter(); Assert.False(Act(fire, "RestAtCampfire"));
            Assert.AreEqual(tick, Clock.TickCount); Assert.AreEqual(3, Actor.GetStatValue("Hitpoints")); Assert.AreSame(bleed, Actor.GetEffect<BleedingEffect>());
        }
        [TestCase(false)] [TestCase(true)]
        public void CommittedRestCannotResurrectAnActorKilledByTheOuterAction(bool deathHandled)
        {
            var fire = Place("Campfire"); var bleed = new BleedingEffect(20, "1d1");
            Assert.True(Actor.ApplyEffect(bleed)); Actor.GetStat("Hitpoints").BaseValue = 3;
            bool callbackRan = false; int tick = Clock.TickCount;
            bool oldChannel = CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("furniture");
            CavesOfOoo.Diagnostics.Diag.SetChannel("furniture", true);
            try
            {
                Actor.AddPart(new FiftyWorldEventProbe { Callback = e =>
                {
                    if (e.ID != "AfterInventoryAction") return;
                    callbackRan = true;
                    if (deathHandled) Actor.SetTag("_DeathHandled");
                    else Actor.GetStat("Hitpoints").BaseValue = 0;
                    Actor.SetTag("TerminalRestProbe");
                } });
                var priorTraces = new HashSet<string>(CavesOfOoo.Diagnostics.Diag.Snapshot(CavesOfOoo.Diagnostics.Diag.BufferCapacity).Select(r => r.TraceId));
                Assert.True(Act(fire, "RestAtCampfire"), "The outer action commits; death does not refund accepted rest time.");
                Assert.True(callbackRan); Assert.AreEqual(deathHandled, CombatSystem.IsDeathHandled(Actor));
                Assert.AreEqual(tick + RestSystem.RestClockTurns, Clock.TickCount);
                Assert.AreEqual(deathHandled ? 3 : 0, Actor.GetStatValue("Hitpoints"), "Committed benefits cannot restore a dead actor.");
                Assert.AreSame(bleed, Actor.GetEffect<BleedingEffect>(), "Interrupted rest cannot remove another terminal-state payload.");
                Assert.True(Actor.HasTag("TerminalRestProbe"));
                var records = CavesOfOoo.Diagnostics.Diag.Snapshot(CavesOfOoo.Diagnostics.Diag.BufferCapacity)
                    .Where(r => r.Category == "furniture" && r.ActorId == Actor.ID && !priorTraces.Contains(r.TraceId)).ToArray();
                Assert.AreEqual(1, records.Count(r => r.Kind == "RestInterrupted"));
                StringAssert.Contains("actor_dead", records.Single(r => r.Kind == "RestInterrupted").PayloadJson);
                Assert.False(records.Any(r => r.Kind == "Rested"), "A dead actor must not receive a successful-rest receipt.");
            }
            finally { CavesOfOoo.Diagnostics.Diag.SetChannel("furniture", oldChannel); }
        }
        [Test] public void LaterFieldOutputFactoryCannotInvalidateAnEarlierStagedGrain()
        {
            var row = Place("RipeCropRow"); row.GetPart<FieldHarvestPart>().YieldCount = 2; Entity first = null;
            Hook("Emberwheat", e => { if (first == null) first = e; else first.GetPart<PhysicsPart>().Takeable = false; });
            Assert.False(Act(row, "Harvest")); Assert.False(row.GetPart<FieldHarvestPart>().Harvested); Assert.AreEqual(0, Count("Emberwheat"));
        }
        [Test] public void SeedFactoryMovingTheActorCannotFinishTheOldAdjacentSelection()
        {
            Bed(); var seed = Seed(); string command = Choice(seed, "PlantSeedAt|", a => a.Command.EndsWith("|11|10"));
            Hook("CandyCarrotCrop", _ => Zone.MoveEntity(Actor, 10, 11)); Assert.False(Act(seed, command));
            Assert.AreEqual((10, 11), Zone.GetEntityPosition(Actor)); Assert.AreEqual(1, Count("CandyCarrotSeed")); Assert.AreEqual(0, Count("CandyCarrotCrop"));
        }
        [Test] public void SeedFactoryRemovingTheBedDoesNotSpendTheSeed()
        {
            var bed = Bed(); var seed = Seed(); string command = Choice(seed, "PlantSeedAt|", a => a.Command.EndsWith("|11|10"));
            Hook("CandyCarrotCrop", _ => Zone.RemoveEntity(bed)); Assert.False(Act(seed, command)); Assert.AreEqual(1, Count("CandyCarrotSeed")); Assert.AreEqual(0, Count("CandyCarrotCrop")); Assert.Null(Zone.GetEntityCell(bed));
        }
        [Test] public void GatheringRespectsTheActualWeightOfAMergedResidentStack()
        {
            var resident = Carry("CandyCarrot"); resident.GetPart<PhysicsPart>().Weight = 10; Pack.MaxWeight = 11;
            var crop = Crop(2); Hook("CandyCarrot", item => item.GetPart<PhysicsPart>().Weight = 1);
            Assert.True(Act(crop, "GatherCrop")); Assert.LessOrEqual(Pack.GetCarriedWeight(), Pack.MaxWeight); Assert.AreEqual(3, Count("CandyCarrot")); Assert.AreEqual(1, resident.GetPart<StackerPart>().StackCount);
        }
        [Test] public void FieldGleaningAlsoRespectsActualMergedWeightAndLeavesOverflowReal()
        {
            var resident = Carry("Emberwheat"); resident.GetPart<PhysicsPart>().Weight = 10; Pack.MaxWeight = 11;
            var row = Place("RipeCropRow"); Hook("Emberwheat", item => item.GetPart<PhysicsPart>().Weight = 1);
            Assert.True(Act(row, "Harvest")); Assert.LessOrEqual(Pack.GetCarriedWeight(), Pack.MaxWeight); Assert.AreEqual(2, Count("Emberwheat")); Assert.AreEqual(1, resident.GetPart<StackerPart>().StackCount);
        }
        [Test] public void ClearRollbackRestoresCropOrderingWithoutErasingIndependentGroundWork()
        {
            var crop = Crop(); var cell = Zone.GetEntityCell(crop); int index = cell.Objects.IndexOf(crop); Entity independent = null;
            Actor.AddPart(new FiftyWorldEventProbe { Callback = e => { if (e.ID == "AfterInventoryAction") { independent = Place("SalvagedTimber"); throw new InvalidOperationException("independent ground work"); } } });
            Assert.False(Act(crop, "ClearCrop")); Assert.AreSame(cell, Zone.GetEntityCell(crop)); Assert.AreEqual(index, cell.Objects.IndexOf(crop)); Assert.Contains(independent, cell.Objects);
        }
        [Test] public void CarriedAndEquippedAliasCannotReceiveTransferredWater()
        {
            var source = Flask(); var target = Skin(); Pack.EquippedItems["forged"] = target;
            Assert.False(Act(source, "TransferWater|" + Id(target))); Assert.AreEqual(3, Units(source)); Assert.AreEqual(0, Units(target));
        }
        [Test] public void DousingNeverRaisesAnAlreadyColdBurningObjectsTemperature()
        {
            var prop = FireProp(); prop.GetPart<ThermalPart>().Temperature = -10; var skin = Skin(2);
            Assert.True(Act(skin, "DouseWorld|" + Id(prop))); Assert.LessOrEqual(prop.GetPart<ThermalPart>().Temperature, -10); Assert.AreEqual(1, Units(skin)); Assert.False(prop.HasEffect<BurningEffect>());
        }
        [Test] public void FailedDousePreservesIndependentCallbackWetButUndoesItsOwnDampening()
        {
            var prop = FireProp(); WetEffect independent = null;
            prop.AddPart(new FiftyWorldEventProbe { Callback = e => { if (e.ID == "EffectRemoved" && e.GetParameter<Effect>("Effect") is BurningEffect) { independent = new WetEffect(.2f); prop.ApplyEffect(independent); } } });
            var skin = Skin(2); FailAfter(); Assert.False(Act(skin, "DouseWorld|" + Id(prop)));
            Assert.AreEqual(2, Units(skin)); Assert.True(prop.HasEffect<BurningEffect>()); Assert.AreSame(independent, prop.GetEffect<WetEffect>()); Assert.AreEqual(.2f, independent.Moisture, .0001f);
        }
        [Test] public void DestroyedStructuralOwnerCannotReceiveDousingPayment()
        {
            var prop = FireProp(); prop.AddPart(new DestructiblePart { HP = 0, MaxHP = 10, Gone = true }); var skin = Skin(2);
            Assert.False(Act(skin, "DouseWorld|" + Id(prop))); Assert.AreEqual(2, Units(skin)); Assert.True(prop.HasEffect<BurningEffect>());
        }
        [Test] public void DryingRollbackMergesAnIndependentNewWetExposureWithoutDuplicateEffects()
        {
            var fire = Place("SpreadCookingCoals"); var gear = Gear(); bool exposed = false;
            gear.AddPart(new FiftyWorldEventProbe { Callback = e => { if (!exposed && e.ID == "EffectRemoved" && e.GetParameter<Effect>("Effect") is WetEffect) { exposed = true; gear.ApplyEffect(new WetEffect(.5f)); throw new InvalidOperationException("independent wet exposure"); } } });
            Assert.False(Act(fire, "DryGear|" + Id(gear))); Assert.True(exposed);
            Assert.AreEqual(1, gear.GetPart<StatusEffectsPart>().GetAllEffects().Count(e => e is WetEffect)); Assert.AreEqual(.75f, gear.GetEffect<WetEffect>().Moisture, .0001f);
        }
        [Test] public void NearFullCoalsSpendOneTimberWithoutExceedingTheirFuelCapacityOrCreatingHeat()
        {
            var fire = Place("SpreadCookingCoals"); fire.GetPart<FuelPart>().FuelMass = 24; fire.GetPart<ThermalPart>().Temperature = 25; Carry("SalvagedTimber");
            Assert.True(Act(fire, "FeedCookingFire")); Assert.AreEqual(25, fire.GetPart<FuelPart>().FuelMass); Assert.AreEqual(25, fire.GetPart<ThermalPart>().Temperature); Assert.AreEqual(0, Count("SalvagedTimber"));
            Carry("SalvagedTimber"); Assert.False(Act(fire, "FeedCookingFire")); Assert.AreEqual(1, Count("SalvagedTimber"));
        }
        [Test] public void ForgedEquipmentCacheCannotSupplyFlameWithoutAnActualBodyBinding()
        {
            var source = Carry("Torch"); Pack.RemoveObject(source); source.GetPart<PhysicsPart>().Equipped = Actor; Pack.EquippedItems["forged"] = source;
            Assert.NotNull(Actor.GetPart<Body>()); Assert.Null(Pack.FindEquippedBodyPart(source));
            var spare = Place("Torch"); Assert.True(Act(spare, "ExtinguishTorch")); Assert.False(Act(spare, "LightTorch")); Assert.False(spare.GetPart<ThermalPart>().IsAflame);
        }
        [Test] public void RelightRechecksRealFuelAfterItsMenuWasOpened()
        {
            var fire = Place("SpreadCookingCoals"); fire.GetPart<ThermalPart>().Temperature = 25; var torch = Carry("Torch"); Assert.True(InventorySystem.Equip(Actor, torch)); Assert.NotNull(Pack.FindEquippedBodyPart(torch));
            string command = Choice(fire, "RelightCookingFire"); fire.GetPart<FuelPart>().FuelMass = 0;
            Assert.False(Act(fire, command)); Assert.AreEqual(25, fire.GetPart<ThermalPart>().Temperature); Assert.AreEqual(50, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ReplacementSavePreservesClearingDousingAndPartialDryingWithoutRestocking()
        {
            Zone.RemoveEntity(Actor); Zone = new Zone("Overworld.10.10.0"); SettlementRuntime.ActiveZone = Zone; Assert.True(Zone.AddEntity(Actor, 10, 10));
            var crop = Crop(); Assert.True(Act(crop, "ClearCrop")); var prop = FireProp(); var skin = Skin(2); Assert.True(Act(skin, "DouseWorld|" + Id(prop)));
            var fire = Place("SpreadCookingCoals", 10, 11); var gear = Gear(.5f); Assert.True(Act(fire, "DryGear|" + Id(gear)));
            var manager = OverworldZoneManager.CreateDetached(Factory, 64); manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } }, Zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            Clock.RestoreSavedState(0, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = 1000 } });
            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("fifty-world-adversarial", "controlled-aftermath", manager, Clock, Actor)); var zone = loaded.ZoneManager.ActiveZone;
            Assert.False(zone.GetReadOnlyEntities().Any(e => e.ID == crop.ID)); Assert.True(CultivatedSoilPart.IsCultivated(zone, zone.GetCell(11, 10)));
            var restored = zone.GetReadOnlyEntities().Single(e => e.ID == prop.ID); Assert.AreNotSame(prop, restored); Assert.False(restored.HasEffect<BurningEffect>()); Assert.NotNull(restored.GetEffect<WetEffect>());
            Assert.AreEqual(1, loaded.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == skin.ID).GetPart<WaterskinPart>().Charges);
            Assert.AreEqual(.25f, loaded.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == gear.ID).GetEffect<WetEffect>().Moisture, .0001f);
        }
    }
    public sealed class FiftyWorldEventProbe : Part
    {
        public Action<GameEvent> Callback;
        public override bool HandleEvent(GameEvent e) { Callback?.Invoke(e); return true; }
    }
}
