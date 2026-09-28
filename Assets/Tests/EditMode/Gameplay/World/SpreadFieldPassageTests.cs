using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Real cold terrain producer, explicit Hedgerow grammar fixture. This is not
    // a natural-v5-availability or paid-input claim; those gates follow integration.
    public sealed class SpreadFieldPassageTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        const string ZoneID="Overworld.13.11.0";
        DensityLootTestScope scope;
        readonly List<(FieldInfo field,object value,List<DictionaryEntry> entries)> globals=new List<(FieldInfo,object,List<DictionaryEntry>)>();
        object oldSettlement; Action<Entity> oldProbe;
        Zone zone; SpreadCompositionBuilder terrain; Entity hedge; (int x,int y) anchor,a,b;

        [SetUp] public void Setup()
        {
            foreach(var f in typeof(LootTableRegistry).GetFields(All))
            {
                if(!f.IsStatic)continue;var value=f.GetValue(null);var entries=new List<DictionaryEntry>();
                if(value is IDictionary d)foreach(DictionaryEntry e in d)entries.Add(e);
                globals.Add((f,value,entries));
            }
            oldSettlement=SettlementManager.Current;oldProbe=CreationProbe.Callback;CreationProbe.Callback=null;
            scope=new DensityLootTestScope();scope.Seed(64);
            zone=new Zone(ZoneID);terrain=new SpreadCompositionBuilder(64){FormationOverride=Formation.Hedgerow,Topology=SpreadExplorationTopology.BrokenEnclosures};
            typeof(SpreadCompositionBuilder).GetField("CapturePassageSources",All)?.SetValue(terrain,true);
            Assert.True(terrain.BuildZone(zone,scope.Factory,new Random(64)));Assert.AreSame(zone,terrain.SourceZone);
            FindUsefulBoundary();
        }
        [TearDown] public void Cleanup()
        {
            CreationProbe.Callback=oldProbe;
            try{scope?.Dispose();}finally
            {
                foreach(var g in globals){if(!g.field.IsInitOnly&&!g.field.IsLiteral)g.field.SetValue(null,g.value);if(g.value is IDictionary d){d.Clear();foreach(var e in g.entries)d.Add(e.Key,e.Value);}}
                globals.Clear();typeof(SettlementManager).GetProperty("Current",All).SetValue(null,oldSettlement);
            }
        }
        // Independent physical cardinal distance: actor-aware FindPath is not a
        // closed-gate bypass oracle because real players can open doors en route.
        int Distance((int x,int y) start,(int x,int y) end,Entity ignore=null)
        {
            var reached=new bool[Zone.Width,Zone.Height];var q=new Queue<(int x,int y,int d)>();
            bool Walk(int x,int y)=>zone.GetCell(x,y) is Cell c&&!c.Occupants.Any(e=>e!=ignore&&
                (e.HasTag("Solid")||e.GetPart<PhysicsPart>()?.Solid==true||e.GetPart<DoorPart>()?.IsClosed==true));
            void Add(int x,int y,int d){if(x<0||y<0||x>=Zone.Width||y>=Zone.Height||reached[x,y]||!Walk(x,y))return;reached[x,y]=true;q.Enqueue((x,y,d));}
            Add(start.x,start.y,0);while(q.Count>0){var p=q.Dequeue();if((p.x,p.y)==end)return p.d;Add(p.x-1,p.y,p.d+1);Add(p.x+1,p.y,p.d+1);Add(p.x,p.y-1,p.d+1);Add(p.x,p.y+1,p.d+1);}return -1;
        }
        void FindUsefulBoundary()
        {
            foreach(var e in zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Hedge").OrderBy(e=>zone.GetEntityPosition(e).y).ThenBy(e=>zone.GetEntityPosition(e).x))
            {
                var p=zone.GetEntityPosition(e);if(p.x<4||p.y<4||p.x>=Zone.Width-4||p.y>=Zone.Height-4||zone.GenReservedCells.Contains(p))continue;
                foreach(bool horizontal in new[]{true,false})
                {
                    var left=(x:p.x-(horizontal?1:0),y:p.y-(horizontal?0:1));var right=(x:p.x+(horizontal?1:0),y:p.y+(horizontal?0:1));
                    var side1=zone.GetCell(p.x-(horizontal?0:1),p.y-(horizontal?1:0));var side2=zone.GetCell(p.x+(horizontal?0:1),p.y+(horizontal?1:0));
                    if(!side1.Objects.Any(v=>v.BlueprintName=="Hedge")||!side2.Objects.Any(v=>v.BlueprintName=="Hedge"))continue;
                    int closed=Distance(left,right);if(closed<8||closed>60||Distance(left,right,e)!=2)continue;
                    hedge=e;anchor=p;a=left;b=right;return;
                }
            }
            Assert.Fail("Actual cold Hedgerow must contain a physical, useful straight boundary segment before testing the new helper.");
        }
        bool Place(out Entity gate,out Func<bool> final,Func<bool> authority=null,SpreadCompositionBuilder producer=null,Entity source=null)
        {
            var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationPassage");
            Assert.NotNull(type,"F12 needs the scoped exact-producer passage helper, not a matching-name scenery rewrite.");
            var method=type.GetMethod("TryPlace",All,null,new[]{typeof(Zone),typeof(EntityFactory),typeof(SpreadCompositionBuilder),typeof(Entity),typeof(Func<bool>),typeof(Entity).MakeByRefType(),typeof(Func<bool>).MakeByRefType()},null);
            Assert.NotNull(method,"Exact source + current attempt + owned gate/final proof contract required.");
            object[] args={zone,scope.Factory,producer??terrain,source??hedge,authority??(()=>true),null,null};
            bool ok=(bool)method.Invoke(null,args);gate=(Entity)args[5];final=(Func<bool>)args[6];
            if(ok){var physical=FindPath.Search(zone,a.x,a.y,b.x,b.y);Assert.True(physical.Usable);Assert.Greater(physical.Steps.Count,3,"A committed gate must beat the actual diagonal-aware physical bypass after paying Open plus two steps.");}
            return ok;
        }
        string Facts(Entity e)=>e.ID+"|"+e.BlueprintName+"|"+string.Join(";",e.Statistics.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value.BaseValue+":"+x.Value.Bonus+":"+x.Value.Penalty))+"|"+string.Join(";",e.Properties.OrderBy(x=>x.Key).Select(x=>x.Key+"="+x.Value));
        void NoNewGate()=>Assert.False(zone.GetReadOnlyEntities().Any(e=>e.HasPart<DoorPart>()));
        Entity Player()
        {var p=scope.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(p,a.x,a.y));return p;}
        bool Open(Entity gate,Entity player,bool open)
        {var e=GameEvent.New("InventoryAction");try{e.SetParameter("Actor",player);e.SetParameter("Zone",zone);e.SetParameter("Command",open?"OpenDoor":"CloseDoor");gate.FireEvent(e);return e.Handled;}finally{e.Release();}}

        [Test] public void ExistingPhysicalHedgeAndOrdinaryDoorHaveARealBypassAndSeparateOpeningAction()
        {
            int closed=Distance(a,b);Assert.That(closed,Is.InRange(8,60));Assert.True(zone.RemoveEntity(hedge));
            var gate=scope.Factory.CreateEntity("VillageDoor");var d=gate.GetPart<DoorPart>();d.IsOpen=false;d.Initialize();Assert.True(zone.AddEntity(gate,anchor.x,anchor.y));
            Assert.AreEqual(closed,Distance(a,b));Assert.True(zone.GetCell(anchor.x,anchor.y).IsWall());var player=Player();
            var physical=FindPath.Search(zone,a.x,a.y,b.x,b.y);Assert.True(physical.Usable);Assert.Greater(physical.Steps.Count,3,"Actual diagonal-aware closed route must still cost more turns than Open plus two steps.");
            var capable=FindPath.Search(zone,a.x,a.y,b.x,b.y,actor:player);Assert.True(capable.Usable);Assert.AreEqual(2,capable.Steps.Count,"The actor-aware path already plans through an operable door; it is not closed-path evidence.");Assert.False(d.IsOpen);
            Assert.True(Open(gate,player,true));Assert.AreEqual(a,zone.GetEntityPosition(player));
            Assert.AreEqual(2,Distance(a,b,player));Assert.False(zone.GetCell(anchor.x,anchor.y).IsWall());Assert.True(zone.CanPlaceFootprint(player,anchor.x,anchor.y));
        }
        [Test] public void ExactProducerHedgeBecomesOneClosedGateWithoutChangingOtherOwnersOrStock()
        {
            var owners=zone.GetReadOnlyEntities().ToArray();var at=owners.ToDictionary(e=>e,zone.GetEntityPosition);var facts=owners.ToDictionary(e=>e,Facts);int closed=Distance(a,b);
            Assert.True(Place(out var gate,out var final));Assert.NotNull(gate);Assert.NotNull(final);Assert.True(final());
            Assert.AreEqual("SpreadFieldGate",gate.BlueprintName,"The final family uses one scoped field-gate child, not all VillageDoor art.");
            Assert.AreEqual(owners.Length,zone.EntityCount);Assert.Null(zone.GetEntityCell(hedge));Assert.AreEqual(anchor,zone.GetEntityPosition(gate));
            var d=gate.GetPart<DoorPart>();Assert.NotNull(d);Assert.AreSame(gate,d.ParentEntity);Assert.False(d.IsOpen);Assert.True(d.IsClosed);Assert.IsEmpty(d.OwnerId);
            Assert.False(gate.HasPart<LockPart>());Assert.False(gate.HasPart<InventoryPart>());Assert.False(gate.HasPart<ContainerPart>());Assert.False(gate.HasTag("Creature"));
            var structural=gate.GetPart<DestructiblePart>();Assert.NotNull(structural);Assert.AreSame(gate,structural.ParentEntity);Assert.AreEqual(10,structural.HP);Assert.AreEqual(10,structural.MaxHP);Assert.False(structural.Indestructible);Assert.IsEmpty(structural.WreckageBlueprint);
            Assert.False(gate.GetPart<PhysicsPart>().Takeable);Assert.False(gate.GetPart<PhysicsPart>().Solid);Assert.False(gate.HasTag("Solid"));Assert.AreEqual("+",gate.GetPart<RenderPart>().RenderString);
            Assert.AreEqual(closed,Distance(a,b));Assert.AreEqual(2,Distance(a,b,gate));
            foreach(var e in owners.Where(e=>e!=hedge)){Assert.AreSame(e,zone.GetCell(at[e].x,at[e].y).Objects.Single(x=>x==e));Assert.AreEqual(facts[e],Facts(e));}
            Assert.False(Place(out var again,out var repeated));Assert.Null(again);Assert.Null(repeated);Assert.AreEqual(owners.Length,zone.EntityCount);
        }
        [TestCase("capture-disabled")][TestCase("late-capture")][TestCase("rebuilt-producer")][TestCase("denied")][TestCase("reserved")][TestCase("interior")][TestCase("wet")][TestCase("owned")][TestCase("damaged")][TestCase("same-id-decoy")][TestCase("wrong-producer")]
        public void UnavailableOrUnprovenSourceCannotBeSubstituted(string fault)
        {
            if(fault=="late-capture")
            {terrain.CapturePassageSources=false;zone=new Zone(ZoneID);Assert.True(terrain.BuildZone(zone,scope.Factory,new Random(64)));terrain.CapturePassageSources=true;FindUsefulBoundary();}
            Entity source=hedge;SpreadCompositionBuilder producer=terrain;
            if(fault=="rebuilt-producer")Assert.True(terrain.BuildZone(new Zone(ZoneID),scope.Factory,new Random(64)));
            if(fault=="capture-disabled"){var field=typeof(SpreadCompositionBuilder).GetField("CapturePassageSources",All);Assert.NotNull(field,"Exact terrain provenance must be an explicit pre-build opt-in.");field.SetValue(terrain,false);}
            if(fault=="reserved")zone.GenReservedCells.Add(anchor);if(fault=="interior")zone.GetCell(anchor.x,anchor.y).IsInterior=true;
            if(fault=="wet")zone.TileState.WriteCoating(anchor.x,anchor.y,"water",2);if(fault=="owned")hedge.SetTag("Owned");
            if(fault=="damaged"){Assert.AreEqual(10,hedge.GetPart<DestructiblePart>().HP);hedge.GetPart<DestructiblePart>().HP=9;}
            if(fault=="same-id-decoy"){Assert.True(zone.RemoveEntity(hedge));source=scope.Factory.CreateEntity("Hedge");source.ID=hedge.ID;Assert.True(zone.AddEntity(source,anchor.x,anchor.y));}
            if(fault=="wrong-producer"){producer=new SpreadCompositionBuilder(64){FormationOverride=Formation.Hedgerow};Assert.True(producer.BuildZone(new Zone(ZoneID),scope.Factory,new Random(64)));}
            var owners=zone.GetReadOnlyEntities().ToArray();var positions=owners.ToDictionary(e=>e,zone.GetEntityPosition);string tiles=zone.TileState.ToSaveString();
            Assert.False(Place(out var gate,out var final,()=>fault!="denied",producer,source));Assert.Null(gate);Assert.Null(final);NoNewGate();
            CollectionAssert.AreEquivalent(owners,zone.GetReadOnlyEntities());foreach(var e in owners)Assert.AreEqual(positions[e],zone.GetEntityPosition(e));Assert.AreEqual(tiles,zone.TileState.ToSaveString());if(fault=="damaged")Assert.AreEqual(9,hedge.GetPart<DestructiblePart>().HP);
        }
        public sealed class CreationProbe:Part
        {public static Action<Entity> Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Callback?.Invoke(ParentEntity);return true;}}
        [TestCase(false)][TestCase(true)] public void FactoryCallbacksRevalidateExactSourceAndPreserveForeignReplacement(bool replace)
        {
            // Content must be adopted independently; no fixture-created source or gate blueprint.
            Assert.True(scope.Factory.Blueprints.ContainsKey("SpreadFieldGate"),"The scoped field-gate source is not implemented yet.");
            scope.Factory.RegisterPartType<CreationProbe>();scope.Factory.Blueprints["SpreadFieldGate"].Parts[nameof(CreationProbe)]=new Dictionary<string,string>();int calls=0;Entity foreign=null;
            CreationProbe.Callback=e=>{calls++;if(!replace)return;Assert.True(zone.RemoveEntity(hedge));foreign=scope.Factory.CreateEntity("Hedge");foreign.ID=hedge.ID;Assert.True(zone.AddEntity(foreign,anchor.x,anchor.y));};
            Assert.AreEqual(!replace,Place(out var gate,out var final));Assert.AreEqual(1,calls);
            if(replace){NoNewGate();Assert.AreSame(foreign,zone.GetCell(anchor.x,anchor.y).Objects.Single(e=>e==foreign));Assert.Null(zone.GetEntityCell(hedge));}
            else{Assert.NotNull(gate);Assert.True(final());}
        }
        [TestCase("unchanged",true)][TestCase("offroute",true)][TestCase("open",false)][TestCase("axis",false)][TestCase("approach",false)][TestCase("support",false)][TestCase("returned-source",false)]
        public void FinalProofPinsGateAndUsefulRouteButPermitsHarmlessOffrouteChange(string change,bool expected)
        {
            Assert.True(Place(out var gate,out var final));Assert.True(final());
            if(change=="offroute")Assert.True(zone.AddEntity(scope.Factory.CreateEntity("Cudgel"),0,0));
            if(change=="open"){gate.GetPart<DoorPart>().IsOpen=true;gate.GetPart<DoorPart>().Initialize();}
            if(change=="axis")gate.GetPart<DoorPart>().QuarterTurns=(gate.GetPart<DoorPart>().QuarterTurns+1)%4;
            if(change=="support"){var side=zone.GetCell(anchor.x+(a.x!=b.x?0:1),anchor.y+(a.x!=b.x?1:0)).Objects.Single(e=>e.BlueprintName=="Hedge");Assert.True(zone.RemoveEntity(side));}
            if(change=="returned-source")Assert.True(zone.AddEntity(hedge,0,0));
            if(change=="approach")Assert.True(zone.AddEntity(scope.Factory.CreateEntity("StoneWall"),a.x,a.y));
            Assert.AreEqual(expected,final());Assert.AreSame(gate,zone.GetCell(anchor.x,anchor.y).Objects.Single(e=>e==gate));
        }
        [TestCase("damaged")][TestCase("already-placed")][TestCase("empty-id")][TestCase("duplicate-id")]
        public void MalformedFactoryGateRefusesWithoutRepairingOrRemovingForeignWork(string fault)
        {
            scope.Factory.RegisterPartType<CreationProbe>();scope.Factory.Blueprints["SpreadFieldGate"].Parts[nameof(CreationProbe)]=new Dictionary<string,string>();
            Entity made=null;CreationProbe.Callback=e=>{made=e;if(fault=="damaged")e.GetPart<DestructiblePart>().HP=9;else if(fault=="empty-id")e.ID="";else if(fault=="duplicate-id")e.ID=zone.GetReadOnlyEntities().First(v=>v!=hedge).ID;else Assert.True(zone.AddEntity(e,0,0));};
            Assert.False(Place(out var gate,out var final));Assert.Null(gate);Assert.Null(final);Assert.NotNull(made);Assert.AreSame(zone.GetCell(anchor.x,anchor.y),zone.GetEntityCell(hedge));
            if(fault=="damaged"){Assert.AreEqual(9,made.GetPart<DestructiblePart>().HP);Assert.Null(zone.GetEntityCell(made));}
            else if(fault=="empty-id"){Assert.IsEmpty(made.ID);Assert.Null(zone.GetEntityCell(made));}
            else if(fault=="duplicate-id")Assert.Null(zone.GetEntityCell(made));
            else Assert.AreSame(zone.GetCell(0,0),zone.GetEntityCell(made));
        }

        [TestCase(false)][TestCase(true)] public void BreakingClosedOrOpenGateUsesTenStructuralHPAndSavesAnEmptyGap(bool opened)
        {
            Assert.True(Place(out var gate,out var final));Assert.True(final());var player=Player();if(opened)Assert.True(Open(gate,player,true));
            var structural=gate.GetPart<DestructiblePart>();Assert.NotNull(structural);Assert.AreSame(gate,structural.ParentEntity);Assert.AreEqual(10,structural.HP);Assert.AreEqual(10,structural.MaxHP);Assert.IsEmpty(structural.WreckageBlueprint);
            var other=zone.GetReadOnlyEntities().Where(e=>e!=gate).ToArray();var facts=other.ToDictionary(e=>e,Facts);var positions=other.ToDictionary(e=>e,zone.GetEntityPosition);
            int xp=player.GetStatValue("XP"),money=TradeSystem.GetDrams(player);string gateID=gate.ID,hedgeID=hedge.ID;
            Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(gate,9,player,zone));Assert.AreEqual(1,structural.HP);Assert.AreEqual(!opened,zone.GetCell(anchor.x,anchor.y).BlocksMovement());Assert.AreEqual(!opened,zone.GetCell(anchor.x,anchor.y).IsWall());
            Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(gate,1,player,zone));Assert.True(structural.Gone);Assert.Null(zone.GetEntityCell(gate));Assert.False(zone.GetCell(anchor.x,anchor.y).BlocksMovement());Assert.False(zone.GetCell(anchor.x,anchor.y).IsWall());Assert.AreEqual(2,Distance(a,b,player));
            CollectionAssert.AreEquivalent(other,zone.GetReadOnlyEntities());foreach(var e in other){Assert.AreEqual(facts[e],Facts(e));Assert.AreEqual(positions[e],zone.GetEntityPosition(e));}Assert.AreEqual(xp,player.GetStatValue("XP"));Assert.AreEqual(money,TradeSystem.GetDrams(player));
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64);manager.SetActiveZone(zone);var state=GameSessionState.Capture("F12-broken","controlled-source",manager,null,player);GameSessionState loaded;
            using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
            var current=loaded.ZoneManager.ActiveZone;Assert.AreNotSame(zone,current);Assert.False(current.GetReadOnlyEntities().Any(e=>e.ID==gateID||e.ID==hedgeID));Assert.AreEqual(other.Length,current.EntityCount);Assert.False(current.GetCell(anchor.x,anchor.y).BlocksMovement());Assert.False(current.GetCell(anchor.x,anchor.y).IsWall());
        }
        [TestCase(false)][TestCase(true)] public void ActualGateStateAndRemovedHedgeSurviveAFullSavedReplacementGraph(bool opened)
        {
            Assert.True(Place(out var gate,out var final));Assert.True(final());var player=Player();
            if(opened)Assert.True(Open(gate,player,true));string gateID=gate.ID,hedgeID=hedge.ID;int axis=gate.GetPart<DoorPart>().QuarterTurns;
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64);manager.SetActiveZone(zone);
            var state=GameSessionState.Capture("F12-primitive","controlled-source",manager,null,player);GameSessionState loaded;
            using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
            var current=loaded.ZoneManager.ActiveZone;Assert.AreNotSame(zone,current);var restored=current.GetReadOnlyEntities().Single(e=>e.ID==gateID);Assert.AreNotSame(gate,restored);
            Assert.AreEqual(anchor,current.GetEntityPosition(restored));Assert.AreEqual(opened,restored.GetPart<DoorPart>().IsOpen);Assert.AreEqual(axis,restored.GetPart<DoorPart>().QuarterTurns);Assert.AreEqual(!opened,current.GetEntityCell(restored).BlocksMovement());
            Assert.False(current.GetReadOnlyEntities().Any(e=>e.ID==hedgeID));Assert.AreEqual(1,current.GetReadOnlyEntities().Count(e=>e.HasPart<DoorPart>()));
        }
    }
}
