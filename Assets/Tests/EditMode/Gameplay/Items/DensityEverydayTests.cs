using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Uses public command events, real factory content and production save graphs.
    // Reflection keeps the missing-part RED executable before implementation.
    public class DensityEverydayTests
    {
        EntityFactory _factory, _oldFactory;
        Entity _actor;
        Zone _zone;
        TurnManager _clock;
        [SetUp] public void SetUp()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            _oldFactory = MaterialReactionResolver.Factory;
            MaterialReactionResolver.Factory = _factory;
            _clock = new TurnManager();
            _zone = new Zone("Everyday");
            _actor = new Entity { ID = "everyday-player", BlueprintName = "Player" };
            _actor.SetTag("Player");
            _actor.AddPart(new InventoryPart());
            _actor.AddPart(new StatusEffectsPart());
            _actor.AddPart(new PhysicsPart());
            foreach (string name in new[] { "Hitpoints", "Strength", "Agility" })
                _actor.Statistics[name] = new Stat { Owner = _actor, Name = name, BaseValue = 10, Min = 0, Max = 30 };
            _zone.AddEntity(_actor, 10, 10);
            FactionManager.Initialize(); MessageLog.Clear(); Diag.ResetAll();
        }
        [TearDown] public void TearDown()
        { MaterialReactionResolver.Factory = _oldFactory; FactionManager.Reset(); WorldClock.ResetForTests(); }
        Part Part(Entity entity, string name) => entity.Parts.FirstOrDefault(p => p.Name == name);
        Part RequirePart(Entity entity, string name)
        { var p = Part(entity, name); Assert.NotNull(p, name + " is required"); return p; }
        int Field(Entity entity, string name, string field) => (int)RequirePart(entity, name).GetType().GetField(field).GetValue(Part(entity, name));
        void SetField(Entity entity, string name, string field, object value) => RequirePart(entity, name).GetType().GetField(field).SetValue(Part(entity, name), value);
        Entity Skin(int charges = 0)
        {
            var skin = _factory.CreateEntity("Waterskin");
            Assert.NotNull(skin, "Authored reusable Waterskin blueprint is required");
            RequirePart(skin, "Waterskin");
            SetField(skin, "Waterskin", "Charges", charges);
            Assert.True(_actor.GetPart<InventoryPart>().AddObject(skin));
            return skin;
        }
        Entity Source(string kind, int x = 11, int volume = 3)
        {
            var source = new Entity { ID = "source", BlueprintName = kind };
            if (kind == "well") source.AddPart(new WellPart());
            else if (kind == "spring") source.AddPart(new TileStateSourcePart { Coating = "water", CoatingTurns = 3 });
            else source.AddPart(new LiquidPoolPart { LiquidId = kind, Volume = volume });
            _zone.AddEntity(source, x, 10); return source;
        }
        Entity Food(string name, int count = 1)
        {
            var food = _factory.CreateEntity(name); Assert.NotNull(food);
            food.GetPart<StackerPart>().StackCount = count;
            Assert.True(_actor.GetPart<InventoryPart>().AddObject(food)); return food;
        }
        Entity Fire(int x = 11)
        { var fire = _factory.CreateEntity("Campfire"); _zone.AddEntity(fire, x, 10); return fire; }
        bool Act(Entity item, string command) => InventorySystem.PerformAction(_actor, item, command, _zone);
        int Count(string blueprint) => _actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        int Records(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Kind = kind, Limit = 100 }).Records.Count;

        [TestCase("well")][TestCase("spring")]
        public void FillRenewableSource_ThreeDrinksWithoutConsumingSource(string source)
        { var skin = Skin(); var water = Source(source); Assert.True(Act(skin, "FillWaterskin")); Assert.AreEqual(3, Field(skin,"Waterskin","Charges")); Assert.NotNull(_zone.GetEntityCell(water)); Assert.AreEqual(1, Records("WaterskinFilled")); }
        [TestCase(1, 1, 0)][TestCase(2, 2, 0)][TestCase(8, 3, 5)]
        public void FillFinitePool_ConservesVolume(int volume, int charges, int remainder)
        { var skin = Skin(); var water = Source("water",volume: volume); Assert.True(Act(skin,"FillWaterskin")); Assert.AreEqual(charges,Field(skin,"Waterskin","Charges")); Assert.AreEqual(remainder,water.GetPart<LiquidPoolPart>().Volume); }
        [TestCase("oil")][TestCase("brine")][TestCase("acid")][TestCase("memory_bath")]
        public void FillUnsafeLiquid_RefusesWithoutChangingEitherSide(string liquid)
        { var skin = Skin(1); var pool = Source(liquid); Assert.False(Act(skin,"FillWaterskin")); Assert.AreEqual(1,Field(skin,"Waterskin","Charges")); Assert.AreEqual(3,pool.GetPart<LiquidPoolPart>().Volume); Assert.AreEqual(0,Records("WaterskinFilled")); }
        [Test] public void FillEmptyPoolOrDistantWellOrTemporaryCoating_Refuses()
        { var skin=Skin(); Source("water", volume:0); Source("well",14); _zone.TileState.WriteCoating(10,10,"water",3); Assert.False(Act(skin,"FillWaterskin")); Assert.AreEqual(0,Field(skin,"Waterskin","Charges")); }
        [Test] public void FillFullVessel_DoesNotConsumeFiniteWater()
        { var skin=Skin(3); var pool=Source("water"); Assert.False(Act(skin,"FillWaterskin")); Assert.AreEqual(3,pool.GetPart<LiquidPoolPart>().Volume); }
        [TestCase(1)][TestCase(3)] public void Drink_ConsumesExactlyOneChargeAndParchedStack(int stacks)
        {
            var skin=Skin(3); _actor.GetPart<StatusEffectsPart>().ForceApplyEffect(new ParchedEffect { Stacks=stacks });
            Assert.True(Act(skin,"DrinkWaterskin")); Assert.AreEqual(2,Field(skin,"Waterskin","Charges"));
            Assert.AreEqual(stacks-1,_actor.GetPart<StatusEffectsPart>().GetEffect<ParchedEffect>()?.Stacks??0);
            Assert.AreEqual(stacks-1,_actor.GetStat("Strength").Penalty);
            Assert.True(_actor.GetPart<InventoryPart>().Objects.Contains(skin)); Assert.AreEqual(1,Records("WaterskinDrunk"));
        }
        [Test] public void EmptyDrink_RefusesAndRetainsVessel()
        { var skin=Skin(); Assert.False(Act(skin,"DrinkWaterskin")); Assert.AreEqual(1,Count("Waterskin")); }
        [Test] public void NonParchedDrink_StillConsumesWaterWithoutHealing()
        { var skin=Skin(1); Assert.True(Act(skin,"DrinkWaterskin")); Assert.AreEqual(0,Field(skin,"Waterskin","Charges")); Assert.AreEqual(10,_actor.GetStatValue("Hitpoints")); }
        [TestCase("FillWaterskin")][TestCase("DrinkWaterskin")]
        public void StaleSkin_ForgedBackReferenceCannotAuthorize(string command)
        { var skin=Skin(1); Source("well"); _actor.GetPart<InventoryPart>().RemoveObject(skin); skin.GetPart<PhysicsPart>().InInventory=_actor; Assert.False(Act(skin,command)); Assert.AreEqual(1,Field(skin,"Waterskin","Charges")); }
        [Test] public void TwoVessels_NeverMergeOrShareCharges_AndSavePreservesState()
        {
            var first=Skin(1); var second=Skin(2); Assert.AreEqual(2,Count("Waterskin"));
            Assert.False(first.GetPart<StackerPart>()?.CanStackWith(second)??false);
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(first);
            Assert.AreEqual(1,Field(loaded,"Waterskin","Charges")); Assert.AreEqual(3,Field(loaded,"Waterskin","Capacity"));
            Assert.True(Act(first,"DrinkWaterskin")); Assert.AreEqual(2,Field(second,"Waterskin","Charges"));
        }
        [TestCase("FillWaterskin")][TestCase("DrinkWaterskin")]
        public void WaterAfterListenerThrows_RollsBackChargePoolAndParched(string command)
        {
            var skin=Skin(1); var pool=Source("water");
            _actor.GetPart<StatusEffectsPart>().ForceApplyEffect(new ParchedEffect());
            var before=_actor.GetPart<StatusEffectsPart>().GetEffect<ParchedEffect>();
            _actor.AddPart(new ThrowAfterPart()); Assert.False(Act(skin,command));
            Assert.AreEqual(1,Field(skin,"Waterskin","Charges")); Assert.AreEqual(3,pool.GetPart<LiquidPoolPart>().Volume);
            Assert.AreSame(before,_actor.GetPart<StatusEffectsPart>().GetEffect<ParchedEffect>());
            Assert.AreEqual(1,_actor.GetStat("Strength").Penalty); Assert.AreEqual(0,Records("WaterskinFilled")+Records("WaterskinDrunk"));
        }
        [TestCase("RawMeat","CookedMeat")][TestCase("Starapple","RoastedStarapple")][TestCase("Mushroom","RoastedMushroom")]
        public void Cooking_TransformsWholeStack_ConservesQuantity_AndStaleRetryRefuses(string raw,string cooked)
        {
            var food=Food(raw,4); Fire(); RequirePart(food,"Cookable");
            Assert.True(Act(food,"Cook")); Assert.AreEqual(0,Count(raw)); Assert.AreEqual(4,Count(cooked));
            Assert.False(Act(food,"Cook")); Assert.AreEqual(4,Count(cooked)); Assert.AreEqual(1,Records("FoodCooked"));
        }
        [TestCase(false)][TestCase(true)] public void Cooking_MissingOrDistantFire_Refuses(bool distant)
        { var food=Food("RawMeat",3); if(distant)Fire(13); RequirePart(food,"Cookable"); Assert.False(Act(food,"Cook")); Assert.AreEqual(3,Count("RawMeat")); Assert.AreEqual(0,Count("CookedMeat")); }
        [Test] public void Cooking_AfterListenerThrows_RestoresMergedOutputAndOriginalStack()
        { var cooked=Food("CookedMeat",2); var raw=Food("RawMeat",3); Fire(); RequirePart(raw,"Cookable"); _actor.AddPart(new ThrowAfterPart()); Assert.False(Act(raw,"Cook")); Assert.AreEqual(3,Count("RawMeat")); Assert.AreEqual(2,Count("CookedMeat")); Assert.AreSame(cooked,_actor.GetPart<InventoryPart>().Objects.First(e=>e.BlueprintName=="CookedMeat")); Assert.AreEqual(0,Records("FoodCooked")); }
        [Test] public void Cooking_HeavierExistingMergedOutputCannotOverflowCapacity()
        { var cooked=Food("CookedMeat",1); cooked.GetPart<PhysicsPart>().Weight=10; var raw=Food("RawMeat",3); Fire(); RequirePart(raw,"Cookable"); _actor.GetPart<InventoryPart>().MaxWeight=16; Assert.False(Act(raw,"Cook")); Assert.AreEqual(3,Count("RawMeat")); Assert.AreEqual(1,Count("CookedMeat")); Assert.AreEqual(16,_actor.GetPart<InventoryPart>().GetCarriedWeight()); }
        [Test] public void Cooking_AtCapacity_ReplacementOfEqualWeightSucceeds()
        { var raw=Food("RawMeat",3); Fire(); RequirePart(raw,"Cookable"); _actor.GetPart<InventoryPart>().MaxWeight=6; Assert.True(Act(raw,"Cook")); Assert.AreEqual(3,Count("CookedMeat")); Assert.AreEqual(6,_actor.GetPart<InventoryPart>().GetCarriedWeight()); }
        [Test] public void Cooking_MissingOutputFactory_RefusesWithoutLoss()
        { var raw=Food("RawMeat",3); Fire(); RequirePart(raw,"Cookable"); MaterialReactionResolver.Factory=null; Assert.False(Act(raw,"Cook")); Assert.AreEqual(3,Count("RawMeat")); }
        [Test] public void Cooking_PreparedRecipesRemainAuthored_AndNewMushroomHasThreeD4()
        { Assert.AreEqual("3d4",_factory.CreateEntity("CookedMeat").GetPart<FoodPart>().Healing); Assert.AreEqual("3d4",_factory.CreateEntity("RoastedStarapple").GetPart<FoodPart>().Healing); var mushroom=_factory.CreateEntity("RoastedMushroom"); Assert.NotNull(mushroom); Assert.AreEqual("3d4",mushroom.GetPart<FoodPart>().Healing); }
        [TestCase(0,300)][TestCase(299,300)][TestCase(300,600)][TestCase(1199,1200)]
        public void NextBandRest_LandsExactlyOnNextBoundary(int before,int after)
        {
            _clock.AdvanceClock(before); var method=typeof(RestSystem).GetMethod("TryRestUntilNextBand",BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(method,"Next-band rest API"); object[] args={_actor,_zone,"campfire",null};
            Assert.True((bool)method.Invoke(null,args)); Assert.AreEqual(after,_clock.TickCount); Assert.AreEqual(30,_actor.GetStatValue("Hitpoints"));
        }
        [Test] public void Campfire_OffersBothExistingRestAndNextBand()
        { var fire=Fire(); var actions=InventorySystem.GetActions(_actor,fire); Assert.True(actions.Any(a=>a.Command=="RestAtCampfire")); Assert.True(actions.Any(a=>a.Command=="RestUntilNextBand")); }
        [Test] public void NextBandCampfire_RestIsReachChecked_AndHostileBlocksTime()
        {
            var fire=Fire(14); Assert.False(Act(fire,"RestUntilNextBand")); Assert.AreEqual(0,_clock.TickCount);
            _zone.MoveEntity(fire,11,10); var hostile=_factory.CreateEntity("MarlbackScrabbler"); _zone.AddEntity(hostile,12,10);
            Assert.False(Act(fire,"RestUntilNextBand")); Assert.AreEqual(0,_clock.TickCount); Assert.AreEqual(10,_actor.GetStatValue("Hitpoints"));
            _zone.RemoveEntity(hostile); Assert.True(Act(fire,"RestUntilNextBand")); Assert.AreEqual(300,_clock.TickCount);
        }
        [Test] public void NextBandPaidRest_ChargesExistingPriceAndAddsWellRested()
        {
            var prior=SettlementRuntime.ActiveZone; SettlementRuntime.ActiveZone=_zone;
            try { _actor.SetIntProperty(TradeSystem.CURRENCY_PROP,25); _clock.AdvanceClock(299);
                ConversationActions.Execute("RestAtInnUntilNextBand",null,_actor,"10");
                Assert.AreEqual(300,_clock.TickCount); Assert.AreEqual(15,_actor.GetIntProperty(TradeSystem.CURRENCY_PROP));
                Assert.NotNull(_actor.GetPart<StatusEffectsPart>().GetEffect<WellRestedEffect>()); }
            finally { SettlementRuntime.ActiveZone=prior; }
        }
        [TestCase(false)][TestCase(true)] public void NextBandPaidRest_RefusalNeverCharges(bool hostileNearby)
        {
            var prior=SettlementRuntime.ActiveZone; SettlementRuntime.ActiveZone=_zone;
            try { int drams=hostileNearby?25:3; _actor.SetIntProperty(TradeSystem.CURRENCY_PROP,drams);
                if(hostileNearby)_zone.AddEntity(_factory.CreateEntity("MarlbackScrabbler"),12,10);
                ConversationActions.Execute("RestAtInnUntilNextBand",null,_actor,"10");
                Assert.AreEqual(0,_clock.TickCount); Assert.AreEqual(drams,_actor.GetIntProperty(TradeSystem.CURRENCY_PROP));
                Assert.AreEqual(10,_actor.GetStatValue("Hitpoints")); Assert.Null(_actor.GetPart<StatusEffectsPart>().GetEffect<WellRestedEffect>()); }
            finally { SettlementRuntime.ActiveZone=prior; }
        }
        [Test] public void NextBandRest_MissingZone_Refuses()
        {
            var method=typeof(RestSystem).GetMethod("TryRestUntilNextBand",BindingFlags.Public|BindingFlags.Static); Assert.NotNull(method);
            object[] args={_actor,null,"campfire",null}; Assert.False((bool)method.Invoke(null,args)); Assert.AreEqual(0,_clock.TickCount);
        }
        [Test] public void NextBandRest_OverflowBoundaryRefusesWithoutHeal()
        {
            _clock.AdvanceClock(int.MaxValue-5); var method=typeof(RestSystem).GetMethod("TryRestUntilNextBand",BindingFlags.Public|BindingFlags.Static); Assert.NotNull(method);
            object[] args={_actor,_zone,"campfire",null}; Assert.False((bool)method.Invoke(null,args)); Assert.AreEqual(int.MaxValue-5,_clock.TickCount); Assert.AreEqual(10,_actor.GetStatValue("Hitpoints"));
        }
        [Test] public void OldRest_StillAdvancesSixtyTicks()
        { Assert.True(RestSystem.TryRest(_actor,_zone,"campfire",out _)); Assert.AreEqual(60,_clock.TickCount); }
        [Test] public void GuestBed_AdvertisesNextBandOnlyThroughExistingOwnerPart()
        {
            var guest=new Entity(); guest.AddPart(new MorrowfastQuestPropPart { ComponentId="guest-bed-west" });
            Assert.True(InventorySystem.GetActions(_actor,guest).Any(a=>a.Command=="MorrowfastRestGuestBedUntilNextBand"));
            var ordinary=new Entity(); ordinary.AddPart(new BedPart());
            Assert.False(InventorySystem.GetActions(_actor,ordinary).Any(a=>a.Command=="MorrowfastRestGuestBedUntilNextBand"));
        }
        [TestCase("MorrowfastProvisionerStock")][TestCase("ProvisionerStock")][TestCase("WellKeeperStock")]
        public void Stock_AlwaysOffersOneEmptyReusableWaterskin(string tableName)
        {
            var json=File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Loot/LootTables.json"));
            var collection=JsonUtility.FromJson<LootTableCollection>(json); var table=collection.Tables.Single(t=>t.Name==tableName);
            Assert.False(table.PickOne); var entry=table.Entries.SingleOrDefault(e=>e.Blueprint=="Waterskin");
            Assert.NotNull(entry,"Guaranteed waterskin stock"); Assert.AreEqual(100,entry.Chance); Assert.AreEqual(1,entry.MinCount); Assert.AreEqual(1,entry.MaxCount);
            var skin=_factory.CreateEntity(entry.Blueprint); Assert.NotNull(skin.GetPart<WaterskinPart>()); Assert.AreEqual(0,skin.GetPart<WaterskinPart>().Charges); Assert.Null(skin.GetPart<StackerPart>());
        }
        public class ThrowAfterPart : Part
        { public override string Name=>"EverydayThrowAfter"; public override bool HandleEvent(GameEvent e) { if(e.ID=="AfterInventoryAction")throw new InvalidOperationException("injected after-listener failure"); return true; } }
    }
}
