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
    // Saved policy + actual world-reader/glyph/proximity only. Ember registration,
    // native pixels, source placement and scheduler approach remain separate gates.
    public sealed class FiniteCookingPresentationTests
    {
        const BindingFlags Static=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
        HotbarSaveFixture scope; EntityFactory factory; Entity actor,source; Zone zone,oldZone; TurnManager turns,oldTurns;
        Action<string> oldMessage; Func<int> oldTick; List<MessageLog.Entry> oldEntries; List<string> oldAnnouncements;
        int oldFlash,oldSerial; Array oldBuffer; object oldWrite,oldFilled,oldDropped;
        [SetUp] public void Setup()
        {
            scope=new HotbarSaveFixture(false,false);oldZone=SettlementRuntime.ActiveZone;oldTurns=TurnManager.Active;
            oldMessage=MessageLog.OnMessage;oldTick=MessageLog.TickProvider;oldEntries=MessageLog.GetAllEntries();oldAnnouncements=MessageLog.GetPendingAnnouncementsSnapshot();
            oldFlash=MessageLog.FlashStamp;oldSerial=(int)typeof(MessageLog).GetField("NextSerial",Static).GetValue(null);
            oldBuffer=(Array)((Array)typeof(Diag).GetField("_buffer",Static).GetValue(null)).Clone();
            oldWrite=typeof(Diag).GetField("_writeIndex",Static).GetValue(null);oldFilled=typeof(Diag).GetField("_filledCount",Static).GetValue(null);oldDropped=typeof(Diag).GetField("_droppedCount",Static).GetValue(null);
            MessageLog.OnMessage=null;MessageLog.TickProvider=null;MessageLog.Clear();
            factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            zone=new Zone("Overworld.11.9.0");actor=new Entity{ID="finite-reader-player",BlueprintName="Player"};actor.SetTag("Player");actor.AddPart(new PhysicsPart());actor.AddPart(new InventoryPart());
            actor.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",Owner=actor,BaseValue=10,Min=0,Max=40};actor.Statistics["Speed"]=new Stat{Name="Speed",Owner=actor,BaseValue=100,Min=1,Max=1000};Assert.True(zone.AddEntity(actor,10,10));
            source=factory.CreateEntity("Campfire");Assert.True(zone.AddEntity(source,11,10));source.GetPart<ExaminablePart>().Text="A small work spot.";
            zone.GetCell(11,10).IsVisible=zone.GetCell(11,10).Explored=true;SettlementRuntime.ActiveZone=zone;
            turns=new TurnManager();turns.RestoreSavedState(17,true,actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=actor,Energy=1000}});
        }
        [TearDown] public void Cleanup()
        {
            if(oldEntries!=null)MessageLog.Restore(oldEntries,oldAnnouncements,oldFlash,oldSerial);
            MessageLog.OnMessage=oldMessage;MessageLog.TickProvider=oldTick;SettlementRuntime.ActiveZone=oldZone;
            typeof(TurnManager).GetProperty("Active",Static).SetValue(null,oldTurns);
            if(oldBuffer!=null){oldBuffer.CopyTo((Array)typeof(Diag).GetField("_buffer",Static).GetValue(null),0);typeof(Diag).GetField("_writeIndex",Static).SetValue(null,oldWrite);typeof(Diag).GetField("_filledCount",Static).SetValue(null,oldFilled);typeof(Diag).GetField("_droppedCount",Static).SetValue(null,oldDropped);}
            scope?.Dispose();
        }
        static FieldInfo RestField(){var f=typeof(CampfirePart).GetField("AllowRest");Assert.NotNull(f,"Cooking-only sources require an explicit default-true saved rest option.");Assert.AreEqual(typeof(bool),f.FieldType);return f;}
        void Allow(bool value)=>RestField().SetValue(source.GetPart<CampfirePart>(),value);
        string Read(Zone z=null,Cell cell=null)=>source.GetPart<ExaminablePart>().BuildWorldExamineLine(z??zone,cell??zone.GetEntityCell(source));
        void Unchanged(int tick=17){Assert.AreEqual(tick,turns.TickCount);Assert.AreEqual(1000,turns.GetEnergy(actor));Assert.AreEqual(10,actor.GetStatValue("Hitpoints"));}
        [Test] public void PlainAndAuthoredPartsDefaultToAllowingEstablishedRest()
        {Assert.True((bool)RestField().GetValue(new CampfirePart()));Assert.True((bool)RestField().GetValue(source.GetPart<CampfirePart>()));}
        [TestCase(false)][TestCase(true)] public void ActualOldPartWireWithoutRestFieldKeepsRest(bool priorFiniteField)
        {
            using(var stream=new MemoryStream())
            {
                var writer=new SaveWriter(stream);writer.WriteString((string)typeof(SaveGraphSerializer).GetMethod("GetTypeName",Static).Invoke(null,new object[]{typeof(CampfirePart)}));
                writer.Write(priorFiniteField?1:0);if(priorFiniteField){writer.WriteString("FiniteCooking");writer.Write(true);}stream.Position=0;
                var loaded=(CampfirePart)typeof(SaveGraphSerializer).GetMethod("LoadPart",Static).Invoke(null,new object[]{new SaveReader(stream,null)});
                Assert.True((bool)RestField().GetValue(loaded));Assert.AreEqual(priorFiniteField,loaded.FiniteCooking);Assert.AreEqual(stream.Length,stream.Position);
            }
        }
        [TestCase(false,false)][TestCase(false,true)][TestCase(true,false)][TestCase(true,true)]
        public void MenuRestOptOutIsIndependentOfFiniteCooking(bool finite,bool allow)
        {
            source.GetPart<CampfirePart>().FiniteCooking=finite;if(!allow)Allow(false);
            var actions=WorldInteractionSystem.GatherActions(source,actor);
            Assert.AreEqual(allow,actions.Any(a=>a.Command=="RestAtCampfire"));Assert.AreEqual(allow,actions.Any(a=>a.Command=="RestUntilNextBand"));
            Assert.True(actions.Any(a=>a.Command=="Examine"));Assert.False(actions.Any(a=>a.Command=="Cook"),"Cooking remains the carried ingredient's command.");Unchanged();
        }
        [TestCase("RestAtCampfire",false)][TestCase("RestUntilNextBand",false)]
        [TestCase("RestAtCampfire",true)][TestCase("RestUntilNextBand",true)]
        public void DirectRestCommandsRespectPolicyBeforeHealingOrClock(string command,bool allow)
        {
            source.GetPart<CampfirePart>().FiniteCooking=true;if(!allow)Allow(false);
            source.GetPart<ThermalPart>().Temperature=25;source.GetPart<FuelPart>().FuelMass=0;
            Assert.AreEqual(allow,InventorySystem.PerformAction(actor,source,command,zone));
            if(allow){Assert.AreEqual(40,actor.GetStatValue("Hitpoints"));Assert.AreEqual(command=="RestAtCampfire"?77:300,turns.TickCount);Assert.AreEqual(1000,turns.GetEnergy(actor));}
            else{Unchanged();StringAssert.Contains("not a resting place",MessageLog.GetLast());}
            Assert.AreEqual(25,source.GetPart<ThermalPart>().Temperature);Assert.AreEqual(0,source.GetPart<FuelPart>().FuelMass);
        }
        [TestCase(false,25f)][TestCase(false,500f)][TestCase(true,25f)][TestCase(true,500f)]
        public void FiniteGlyphKeepsAuthoredColorWhileLegacyFlickers(bool finite,float temperature)
        {
            source.GetPart<CampfirePart>().FiniteCooking=finite;source.GetPart<ThermalPart>().Temperature=temperature;source.GetPart<RenderPart>().ColorString="&c";
            var seen=new HashSet<string>();for(int i=0;i<12;i++){var e=GameEvent.New("Render");try{e.SetParameter("ColorString","&c");source.FireEvent(e);seen.Add(e.GetStringParameter("ColorString"));}finally{e.Release();}}
            if(finite)CollectionAssert.AreEquivalent(new[]{"&c"},seen);else CollectionAssert.AreEquivalent(new[]{"&R","&Y"},seen);
            Assert.AreEqual("&c",source.GetPart<RenderPart>().ColorString);Assert.AreEqual(temperature,source.GetPart<ThermalPart>().Temperature);Unchanged();
        }
        [TestCase(false,25f)][TestCase(false,500f)][TestCase(true,25f)][TestCase(true,500f)]
        public void FiniteProximityNeverClaimsLegacyCrackle(bool finite,float temperature)
        {
            var part=source.GetPart<CampfirePart>();part.FiniteCooking=finite;source.GetPart<ThermalPart>().Temperature=temperature;
            source.FireEventAndRelease(GameEvent.New("EndTurn"));source.FireEventAndRelease(GameEvent.New("EndTurn"));
            Assert.AreEqual(finite?0:1,MessageLog.GetRecent(10).Count(t=>t=="The campfire crackles warmly."));
            part.ResetProximityMessage();source.FireEventAndRelease(GameEvent.New("EndTurn"));Assert.AreEqual(finite?0:2,MessageLog.GetRecent(10).Count(t=>t=="The campfire crackles warmly."));
            Unchanged();
        }
        [TestCase(150f,200f,"enough stored heat")][TestCase(149.99f,200f,"no longer hot enough")]
        [TestCase(500f,0f,"still hot")][TestCase(float.NaN,200f,"unavailable")]
        [TestCase(500f,float.NaN,"unavailable")][TestCase(float.PositiveInfinity,200f,"unavailable")]
        public void FocusedReadoutDescribesReadinessWithoutInventingActionsOrChangingState(float heat,float fuel,string expected)
        {
            source.GetPart<CampfirePart>().FiniteCooking=true;source.GetPart<ThermalPart>().Temperature=heat;source.GetPart<FuelPart>().FuelMass=fuel;
            int version=zone.EntityVersion,logs=MessageLog.Count;string first=Read(),second=Read();StringAssert.Contains("Cooking:",first);StringAssert.Contains(expected,first);Assert.AreEqual(first,second);
            if(expected=="enough stored heat")StringAssert.Contains("carried raw food's Cook",first);else StringAssert.DoesNotContain("carried raw food's Cook",first);
            StringAssert.DoesNotContain("C: cook",first);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(logs,MessageLog.Count);Assert.AreEqual(heat,source.GetPart<ThermalPart>().Temperature);Assert.AreEqual(fuel,source.GetPart<FuelPart>().FuelMass);Unchanged();
        }
        [TestCase("hidden")][TestCase("unseen-cell")][TestCase("wrong-cell")][TestCase("foreign-zone")][TestCase("removed")][TestCase("foreign-part")]
        [TestCase("inactive-zone")][TestCase("foreign-render")][TestCase("foreign-physics")][TestCase("carried")][TestCase("equipped")][TestCase("moved")]
        public void FocusedFiniteReadoutRequiresCurrentVisibleOwnedSource(string fault)
        {
            source.GetPart<CampfirePart>().FiniteCooking=true;StringAssert.Contains("Cooking:",Read());var cell=zone.GetEntityCell(source);Zone z=zone;
            if(fault=="hidden")source.GetPart<RenderPart>().Visible=false;if(fault=="unseen-cell")cell.IsVisible=false;
            if(fault=="wrong-cell")cell=zone.GetCell(12,10);if(fault=="foreign-zone")z=new Zone(zone.ZoneID);
            if(fault=="removed")Assert.True(zone.RemoveEntity(source));if(fault=="foreign-part")source.GetPart<CampfirePart>().ParentEntity=actor;
            if(fault=="inactive-zone")SettlementRuntime.ActiveZone=new Zone(zone.ZoneID);
            if(fault=="foreign-render")source.GetPart<RenderPart>().ParentEntity=actor;if(fault=="foreign-physics")source.GetPart<PhysicsPart>().ParentEntity=actor;
            if(fault=="carried")source.GetPart<PhysicsPart>().InInventory=actor;if(fault=="equipped")source.GetPart<PhysicsPart>().Equipped=actor;
            if(fault=="moved"){Assert.True(zone.MoveEntity(source,12,10));Assert.AreNotSame(cell,zone.GetEntityCell(source));}
            StringAssert.DoesNotContain("Cooking:",source.GetPart<ExaminablePart>().BuildWorldExamineLine(z,cell));Unchanged();
        }
        [TestCase("missing-fuel")][TestCase("missing-thermal")][TestCase("foreign-fuel")][TestCase("foreign-thermal")]
        public void MalformedOwnedFiniteStateNeverAdvertisesUsableCooking(string fault)
        {
            source.GetPart<CampfirePart>().FiniteCooking=true;StringAssert.Contains("enough stored heat",Read());
            if(fault=="missing-fuel")Assert.True(source.RemovePart(source.GetPart<FuelPart>()));
            if(fault=="missing-thermal")Assert.True(source.RemovePart(source.GetPart<ThermalPart>()));
            if(fault=="foreign-fuel")source.GetPart<FuelPart>().ParentEntity=actor;if(fault=="foreign-thermal")source.GetPart<ThermalPart>().ParentEntity=actor;
            StringAssert.Contains("Cooking: unavailable",Read());StringAssert.DoesNotContain("carried raw food's Cook",Read());Unchanged();
        }
        [Test] public void SameOwnerReadoutTracksHotExhaustedThenCooledWithoutMutatingTheSource()
        {
            source.GetPart<CampfirePart>().FiniteCooking=true;var heat=source.GetPart<ThermalPart>();var fuel=source.GetPart<FuelPart>();
            StringAssert.Contains("enough stored heat",Read());fuel.FuelMass=0;StringAssert.Contains("still hot",Read());
            heat.Temperature=149;StringAssert.Contains("no longer hot enough",Read());fuel.FuelMass=17;StringAssert.Contains("no longer hot enough",Read());
            Assert.AreEqual(149,heat.Temperature);Assert.AreEqual(17,fuel.FuelMass);Assert.AreSame(source,zone.GetCell(11,10).Objects.Single(e=>e==source));Unchanged();
        }
        [TestCase(false)][TestCase(true)] public void LegacyFocusedDescriptionGetsNoFiniteClaim(bool hot)
        {source.GetPart<ThermalPart>().Temperature=hot?500:25;StringAssert.DoesNotContain("Cooking:",Read());StringAssert.Contains("A small work spot.",Read());Unchanged();}
        [TestCase(false)][TestCase(true)] public void FullSaveKeepsRestChoiceHeatFuelAndExactReplacement(bool allow)
        {
            Allow(allow);source.GetPart<CampfirePart>().FiniteCooking=true;source.GetPart<ThermalPart>().Temperature=140;source.GetPart<FuelPart>().FuelMass=17;
            var manager=OverworldZoneManager.CreateDetached(factory,1);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("finite-rest-policy","private-source-only",manager,turns,actor));
            Assert.AreNotSame(actor,loaded.Player);var restored=loaded.ZoneManager.ActiveZone.GetReadOnlyEntities().Single(e=>e.ID==source.ID);Assert.AreNotSame(source,restored);
            Assert.AreEqual(allow,(bool)RestField().GetValue(restored.GetPart<CampfirePart>()));Assert.True(restored.GetPart<CampfirePart>().FiniteCooking);Assert.AreEqual(140,restored.GetPart<ThermalPart>().Temperature);Assert.AreEqual(17,restored.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual((11,10),loaded.ZoneManager.ActiveZone.GetEntityPosition(restored));Assert.AreSame(restored,restored.GetPart<CampfirePart>().ParentEntity);
            Assert.AreEqual(allow,WorldInteractionSystem.GatherActions(restored,loaded.Player).Any(a=>a.Command=="RestAtCampfire"));Assert.AreEqual(17,loaded.TurnManager.TickCount);Assert.AreEqual(1000,loaded.TurnManager.GetEnergy(loaded.Player));
        }
    }
}
