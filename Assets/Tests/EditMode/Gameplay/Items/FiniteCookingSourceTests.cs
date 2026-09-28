using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Core state/transaction/save and actual grain-content tests only.
    // No native keyboard, presentation, site placement or offscreen cooling claim.
    public sealed partial class FiniteCookingSourceTests
    {
        const BindingFlags Static=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        HotbarSaveFixture scope;EntityFactory factory,oldFactory;Entity actor;Zone zone,oldActive;
        Action<string> oldMessage;Action<Entity> oldProbe;
        Array oldBuffer;object oldWrite,oldFilled,oldDropped;
        List<MaterialReactionBlueprint> oldReactions;MaterialReactionBlueprint[] oldReactionItems;bool oldReactionsInitialized;
        [SetUp] public void Setup()
        {
            scope=new HotbarSaveFixture(false,false);
            oldFactory=MaterialReactionResolver.Factory;oldMessage=MessageLog.OnMessage;oldProbe=CookingOutputProbe.Callback;oldActive=SettlementRuntime.ActiveZone;
            oldBuffer=(Array)((Array)typeof(Diag).GetField("_buffer",Static).GetValue(null)).Clone();
            oldWrite=typeof(Diag).GetField("_writeIndex",Static).GetValue(null);oldFilled=typeof(Diag).GetField("_filledCount",Static).GetValue(null);oldDropped=typeof(Diag).GetField("_droppedCount",Static).GetValue(null);
            oldReactions=(List<MaterialReactionBlueprint>)typeof(MaterialReactionResolver).GetField("_reactions",Static).GetValue(null);
            oldReactionItems=oldReactions.ToArray();oldReactionsInitialized=(bool)typeof(MaterialReactionResolver).GetField("_initialized",Static).GetValue(null);
            MaterialReactionResolver.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/MaterialReactions"),"*.json").OrderBy(p=>p,StringComparer.Ordinal).Select(File.ReadAllText));
            Assert.True(MaterialReactionResolver.IsInitialized);Assert.Greater(MaterialReactionResolver.ReactionCount,0,"Use the actual reaction catalog, not an accidentally empty global.");
            MessageLog.OnMessage=null;CookingOutputProbe.Callback=null;SettlementRuntime.ActiveZone=null;
            factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            MaterialReactionResolver.Factory=factory;zone=new Zone("Overworld.11.9.0");actor=new Entity{ID="finite-cooking-player",BlueprintName="Player"};actor.SetTag("Player");
            actor.AddPart(new PhysicsPart());actor.AddPart(new InventoryPart{MaxWeight=100});
            actor.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",Owner=actor,BaseValue=40,Min=0,Max=40};
            actor.Statistics["Speed"]=new Stat{Name="Speed",Owner=actor,BaseValue=100,Min=1,Max=1000};
            Assert.True(zone.AddEntity(actor,10,10));
        }
        [TearDown] public void Cleanup()
        {
            CookingOutputProbe.Callback=oldProbe;MaterialReactionResolver.Factory=oldFactory;MessageLog.OnMessage=oldMessage;SettlementRuntime.ActiveZone=oldActive;
            if(oldBuffer!=null)
            {
                oldBuffer.CopyTo((Array)typeof(Diag).GetField("_buffer",Static).GetValue(null),0);
                typeof(Diag).GetField("_writeIndex",Static).SetValue(null,oldWrite);typeof(Diag).GetField("_filledCount",Static).SetValue(null,oldFilled);typeof(Diag).GetField("_droppedCount",Static).SetValue(null,oldDropped);
            }
            if(oldReactions!=null)
            {
                oldReactions.Clear();oldReactions.AddRange(oldReactionItems);
                typeof(MaterialReactionResolver).GetField("_reactions",Static).SetValue(null,oldReactions);
                typeof(MaterialReactionResolver).GetField("_initialized",Static).SetValue(null,oldReactionsInitialized);
            }
            scope?.Dispose();
        }
        static FieldInfo Flag()
        {
            var field=typeof(CampfirePart).GetField("FiniteCooking",BindingFlags.Public|BindingFlags.Instance);
            Assert.NotNull(field,"New utility source needs an explicit saved opt-in; established stations must default to legacy.");Assert.AreEqual(typeof(bool),field.FieldType);return field;
        }
        static void SetFinite(Entity station,bool value)=>Flag().SetValue(station.GetPart<CampfirePart>(),value);
        static bool IsFinite(Entity station)=>(bool)Flag().GetValue(station.GetPart<CampfirePart>());
        Entity Station(bool finite=false,int x=11,int y=10)
        {var source=factory.CreateEntity("Campfire");Assert.NotNull(source);Assert.True(zone.AddEntity(source,x,y));if(finite)SetFinite(source,true);return source;}
        Entity Raw(int quantity=2)
        {var raw=factory.CreateEntity("RawMeat");raw.GetPart<StackerPart>().StackCount=quantity;Assert.True(actor.GetPart<InventoryPart>().AddObject(raw));Assert.AreSame(actor,raw.GetPart<PhysicsPart>().InInventory);return raw;}
        int Units(string blueprint)=>actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==blueprint).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        void CookAndCheck(Entity raw,bool expected)
        {
            Assert.AreEqual(expected,CookingService.TryCook(actor,raw,zone,factory));Assert.AreEqual(expected?0:2,Units("RawMeat"));Assert.AreEqual(expected?2:0,Units("CookedMeat"));
            if(!expected){Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(raw));Assert.AreSame(actor,raw.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(2,raw.GetPart<StackerPart>().StackCount);}
        }
        [TestCase(150f,200f,true)][TestCase(149.99f,200f,false)][TestCase(500f,0f,false)][TestCase(500f,200f,true)]
        [TestCase(float.NaN,200f,false)][TestCase(150f,float.NaN,false)][TestCase(float.PositiveInfinity,200f,false)][TestCase(150f,float.PositiveInfinity,false)]
        public void OptedInSourceRequiresFiniteUsableHeatAndFuelWithoutRequiringBurning(float temperature,float fuel,bool expected)
        {
            var source=Station(true);var heat=source.GetPart<ThermalPart>();var combustible=source.GetPart<FuelPart>();heat.Temperature=temperature;combustible.FuelMass=fuel;
            Assert.False(source.HasEffect<BurningEffect>(),"Fresh factory Campfire is initially hot but has no active Burning effect.");var raw=Raw();CookAndCheck(raw,expected);
            Assert.AreEqual(temperature,heat.Temperature);Assert.AreEqual(fuel,combustible.FuelMass,"Cooking does not invent a fuel charge.");Assert.False(source.HasEffect<BurningEffect>());
        }
        [TestCase("Fuel")][TestCase("Thermal")]
        public void MalformedFiniteSourceDoesNotFallBackToLegacy(string part)
        {var source=Station(true);source.RemovePart(source.Parts.Single(p=>p.Name==part));CookAndCheck(Raw(),false);Assert.True(IsFinite(source));}
        [TestCase(false)][TestCase(true)]
        public void EstablishedAuthoredStationsRemainUsableWhenColdSpentOrThermalless(bool thermalless)
        {
            var source=Station();if(thermalless){source.RemovePart(source.GetPart<FuelPart>());source.RemovePart(source.GetPart<ThermalPart>());}
            else{source.GetPart<FuelPart>().FuelMass=0;source.GetPart<ThermalPart>().Temperature=25;}
            CookAndCheck(Raw(),true);
            Assert.True(WorldInteractionSystem.GatherActions(source,actor).Any(a=>a.Command=="RestAtCampfire"),"Cooking policy does not remove ordinary rest.");
        }
        [Test]
        public void FactoryAndPlainCampfirePartsDoNotOptEstablishedSourcesIn()
        {Assert.False((bool)Flag().GetValue(new CampfirePart()));Assert.False(IsFinite(Station()));}
        void DuringOutput(Action action)
        {
            factory.RegisterPartType<CookingOutputProbe>();factory.Blueprints["CookedMeat"].Parts[nameof(CookingOutputProbe)]=new Dictionary<string,string>();
            CookingOutputProbe.Callback=_=>action();
        }
        [TestCase("unchanged")][TestCase("remove-part")][TestCase("replace-part")][TestCase("move-adjacent")]
        public void OutputFactoryCannotKeepPermissionFromChangedExactLegacyStation(string mutation)
        {
            var source=Station();var original=source.GetPart<CampfirePart>();var originalCell=zone.GetEntityCell(source);int calls=0;
            DuringOutput(()=>
            {
                calls++;if(mutation=="remove-part")Assert.True(source.RemovePart(original));
                if(mutation=="replace-part"){Assert.True(source.RemovePart(original));source.AddPart(new CampfirePart());Assert.AreNotSame(original,source.GetPart<CampfirePart>());}
                if(mutation=="move-adjacent"){Assert.True(zone.MoveEntity(source,10,11));Assert.AreEqual((10,11),zone.GetEntityPosition(source));}
            });
            CookAndCheck(Raw(),mutation=="unchanged");Assert.AreEqual(1,calls,"Exact actual output factory hook ran once.");
            if(mutation=="remove-part")Assert.IsNull(source.GetPart<CampfirePart>());
            if(mutation=="replace-part")Assert.AreNotSame(original,source.GetPart<CampfirePart>());
            Assert.AreSame(mutation=="move-adjacent"?zone.GetCell(10,11):originalCell,zone.GetEntityCell(source),"Refusal must not undo the callback's independent move.");
        }
        [TestCase("recipe")][TestCase("stack")]
        public void OutputFactoryCannotKeepPermissionFromReplacedInputParts(string mutation)
        {
            Station();var raw=Raw();var recipe=raw.GetPart<CookablePart>();var stack=raw.GetPart<StackerPart>();int calls=0;
            DuringOutput(()=>
            {
                calls++;
                if(mutation=="recipe"){Assert.True(raw.RemovePart(recipe));raw.AddPart(new CookablePart{Into=recipe.Into});Assert.AreNotSame(recipe,raw.GetPart<CookablePart>());}
                else{Assert.True(raw.RemovePart(stack));raw.AddPart(new StackerPart{StackCount=stack.StackCount,MaxStack=stack.MaxStack});Assert.AreNotSame(stack,raw.GetPart<StackerPart>());}
            });
            CookAndCheck(raw,false);Assert.AreEqual(1,calls);Assert.AreEqual(2,raw.GetPart<StackerPart>().StackCount);
            if(mutation=="recipe")Assert.AreNotSame(recipe,raw.GetPart<CookablePart>());else Assert.AreNotSame(stack,raw.GetPart<StackerPart>());
        }
        [TestCase("unchanged")][TestCase("replace-fuel")][TestCase("replace-thermal")][TestCase("cool")][TestCase("spent")][TestCase("clear-opt-in")]
        public void OutputFactoryRechecksExactOptedInPartsAndCurrentEligibility(string mutation)
        {
            var source=Station(true);var heat=source.GetPart<ThermalPart>();var fuel=source.GetPart<FuelPart>();int calls=0;
            DuringOutput(()=>
            {
                calls++;
                if(mutation=="replace-fuel"){Assert.True(source.RemovePart(fuel));source.AddPart(new FuelPart{FuelMass=fuel.FuelMass});Assert.AreNotSame(fuel,source.GetPart<FuelPart>());}
                if(mutation=="replace-thermal"){Assert.True(source.RemovePart(heat));source.AddPart(new ThermalPart{Temperature=heat.Temperature});Assert.AreNotSame(heat,source.GetPart<ThermalPart>());}
                if(mutation=="cool")heat.Temperature=149;
                if(mutation=="spent")fuel.FuelMass=0;
                if(mutation=="clear-opt-in")SetFinite(source,false);
            });
            CookAndCheck(Raw(),mutation=="unchanged");Assert.AreEqual(1,calls);
            if(mutation=="replace-fuel")Assert.AreNotSame(fuel,source.GetPart<FuelPart>());
            if(mutation=="replace-thermal")Assert.AreNotSame(heat,source.GetPart<ThermalPart>());
            if(mutation=="cool")Assert.AreEqual(149,heat.Temperature);if(mutation=="spent")Assert.AreEqual(0,fuel.FuelMass);
            Assert.AreEqual(mutation!="clear-opt-in",IsFinite(source));
        }
        [Test]
        public void ExhaustedFirstNeighborDoesNotHideTheNextUsableAuthoredStation()
        {
            var spent=Station(true);spent.GetPart<FuelPart>().FuelMass=0;var usable=Station(false,10,11);
            CookAndCheck(Raw(),true);Assert.AreEqual(0,spent.GetPart<FuelPart>().FuelMass);Assert.AreSame(usable.GetPart<CampfirePart>().ParentEntity,usable);
        }
        [Test]
        public void ActualLocalMaterialTicksCloseStoredHeatWindowWithoutInventingBurningOrFuelUse()
        {
            var source=Station(true);var heat=source.GetPart<ThermalPart>();var fuel=source.GetPart<FuelPart>();float originalFuel=fuel.FuelMass;
            Assert.AreEqual(500,heat.Temperature);Assert.False(source.HasEffect<BurningEffect>());
            for(int i=0;i<66;i++)MaterialSimSystem.TickMaterialEntities(zone);
            Assert.That(heat.Temperature,Is.GreaterThanOrEqualTo(150));Assert.Less(heat.Temperature,151);Assert.False(source.HasEffect<BurningEffect>());Assert.AreEqual(originalFuel,fuel.FuelMass);
            var raw=Raw();CookAndCheck(raw,true);foreach(var food in actor.GetPart<InventoryPart>().Objects.ToArray())Assert.True(actor.GetPart<InventoryPart>().RemoveObject(food));
            MaterialSimSystem.TickMaterialEntities(zone);Assert.Less(heat.Temperature,150);Assert.False(source.HasEffect<BurningEffect>());Assert.AreEqual(originalFuel,fuel.FuelMass);CookAndCheck(Raw(),false);
        }
        [TestCase(true,500f,200f,true)][TestCase(true,140f,200f,false)][TestCase(true,500f,0f,false)][TestCase(false,25f,0f,true)]
        public void FullSaveRestoresSameOptInHeatFuelAndCookingOutcome(bool finite,float temperature,float amount,bool expected)
        {
            var source=Station(finite);source.GetPart<ThermalPart>().Temperature=temperature;source.GetPart<FuelPart>().FuelMass=amount;var raw=Raw();
            var manager=OverworldZoneManager.CreateDetached(factory,1);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            var turns=(TurnManager)Activator.CreateInstance(typeof(TurnManager),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{false},null);
            turns.RestoreSavedState(17,true,actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=actor,Energy=1000}});
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("finite-cooking-save","core-audit",manager,turns,actor));
            Assert.AreNotSame(actor,loaded.Player);Assert.AreNotSame(zone,loaded.ZoneManager.ActiveZone);Assert.AreEqual(17,loaded.TurnManager.TickCount);Assert.AreEqual(1000,loaded.TurnManager.GetEnergy(loaded.Player));
            actor=loaded.Player;zone=loaded.ZoneManager.ActiveZone;var restored=zone.GetReadOnlyEntities().Single(e=>e.ID==source.ID);var restoredRaw=actor.GetPart<InventoryPart>().Objects.Single(e=>e.ID==raw.ID);
            Assert.AreNotSame(source,restored);Assert.AreNotSame(raw,restoredRaw);Assert.AreEqual((11,10),zone.GetEntityPosition(restored));Assert.AreSame(zone,typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(restored));
            Assert.AreSame(restored,restored.GetPart<CampfirePart>().ParentEntity);Assert.AreEqual(finite,IsFinite(restored));Assert.AreEqual(temperature,restored.GetPart<ThermalPart>().Temperature);Assert.AreEqual(amount,restored.GetPart<FuelPart>().FuelMass);Assert.AreSame(actor,restoredRaw.GetPart<PhysicsPart>().InInventory);
            CookAndCheck(restoredRaw,expected);Assert.AreEqual(temperature,restored.GetPart<ThermalPart>().Temperature);Assert.AreEqual(amount,restored.GetPart<FuelPart>().FuelMass);
        }
        [Test]
        public void OldZeroFieldCampfirePartWireLoadsAsLegacyWithoutRetrofit()
        {
            // This is the actual old Campfire part field layout, constructed in
            // this runtime, not a claim about cross-runtime historical save bytes.
            using(var stream=new MemoryStream())
            {
                var writer=new SaveWriter(stream);var getType=typeof(SaveGraphSerializer).GetMethod("GetTypeName",Static);
                writer.WriteString((string)getType.Invoke(null,new object[]{typeof(CampfirePart)}));writer.Write(0);stream.Position=0;
                var part=(CampfirePart)typeof(SaveGraphSerializer).GetMethod("LoadPart",Static).Invoke(null,new object[]{new SaveReader(stream,null)});
                Assert.NotNull(part);Assert.False((bool)Flag().GetValue(part));Assert.AreEqual(stream.Length,stream.Position);
            }
        }
        public sealed class CookingOutputProbe:Part
        {
            public static Action<Entity> Callback;public override string Name=>nameof(CookingOutputProbe);
            public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated"){var callback=Callback;Callback=null;callback?.Invoke(ParentEntity);}return true;}
        }
    }
}
