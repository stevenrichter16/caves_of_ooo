using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class FinitePoolDrawDirtyTests
    {
        Zone zone; Entity actor, vessel, source; LiquidPoolPart pool;
        readonly List<(int x,int y,string reason)> dirty = new List<(int,int,string)>();
        Action<int,int,string> priorDirty; Action<string> priorFull, priorMessage; Func<int> priorTick;
        Dictionary<string,LiquidDefinition> priorLiquids; bool priorInitialized;
        readonly Dictionary<FieldInfo,object[]> priorLists = new Dictionary<FieldInfo,object[]>();
        int priorSerial;
        static FieldInfo Field(Type t,string n)=>t.GetField(n,BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp] public void SetUp()
        {
            priorDirty=ZoneRenderHooks.CellDirtyCallback; priorFull=ZoneRenderHooks.FullDirtyCallback;
            priorMessage=MessageLog.OnMessage; priorTick=MessageLog.TickProvider;
            MessageLog.OnMessage=null; MessageLog.TickProvider=null;
            foreach(var name in new[]{"Messages","Ticks","Serials"})
            {var f=Field(typeof(MessageLog),name); priorLists[f]=((IEnumerable)f.GetValue(null)).Cast<object>().ToArray();}
            priorSerial=(int)Field(typeof(MessageLog),"NextSerial").GetValue(null);
            var registry=(Dictionary<string,LiquidDefinition>)Field(typeof(LiquidRegistry),"_byId").GetValue(null);
            priorLiquids=new Dictionary<string,LiquidDefinition>(registry); priorInitialized=LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
            zone=new Zone("finite-pool-dirty"); actor=Owner("actor"); actor.AddPart(new InventoryPart());
            Assert.True(zone.AddEntity(actor,10,10));
            source=Owner("SpreadDrawPoint"); pool=new LiquidPoolPart{LiquidId="water",Volume=3};source.AddPart(pool);
            Assert.True(zone.AddEntity(source,11,10));
            ZoneRenderHooks.CellDirtyCallback=(x,y,r)=>dirty.Add((x,y,r)); ZoneRenderHooks.FullDirtyCallback=r=>Assert.Fail("Finite source requested full redraw: "+r);
            dirty.Clear();
        }
        [TearDown] public void TearDown()
        {
            ZoneRenderHooks.CellDirtyCallback=priorDirty; ZoneRenderHooks.FullDirtyCallback=priorFull;
            MessageLog.OnMessage=priorMessage; MessageLog.TickProvider=priorTick;
            foreach(var p in priorLists){var list=(IList)p.Key.GetValue(null);list.Clear();foreach(var value in p.Value)list.Add(value);}priorLists.Clear();
            Field(typeof(MessageLog),"NextSerial").SetValue(null,priorSerial);
            if(priorLiquids!=null){var d=(Dictionary<string,LiquidDefinition>)Field(typeof(LiquidRegistry),"_byId").GetValue(null);d.Clear();foreach(var p in priorLiquids)d.Add(p.Key,p.Value);Field(typeof(LiquidRegistry),"_initialized").SetValue(null,priorInitialized);}
        }
        static Entity Owner(string blueprint)
        {var e=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName=blueprint};e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());return e;}
        void Vessel(bool skin)
        {
            vessel=Owner(skin?"Waterskin":"EmptyFlask");vessel.SetTag("Item");vessel.GetPart<PhysicsPart>().Takeable=true;
            if(skin)vessel.AddPart(new WaterskinPart{Capacity=3,Charges=0});else vessel.AddPart(new LiquidVesselPart{Capacity=3,Volume=0,LiquidId=""});
            Assert.True(actor.GetPart<InventoryPart>().AddObject(vessel));Assert.AreSame(actor,vessel.GetPart<PhysicsPart>().InInventory);dirty.Clear();
        }
        bool Fill(bool skin,InventoryTransaction tx=null)=>skin?WaterVesselService.TryAct(actor,vessel,zone,"FillWaterskin",tx)
            :LiquidVesselService.TryAct(actor,vessel,zone,"FillLiquidVessel|"+Uri.EscapeDataString(source.ID)+"|water",tx);
        int Carried(bool skin)=>skin?vessel.GetPart<WaterskinPart>().Charges:vessel.GetPart<LiquidVesselPart>().Volume;
        void OneSourceDirty(){Assert.AreEqual(1,dirty.Count);Assert.AreEqual((11,10),(dirty[0].x,dirty[0].y));}

        string PrepareFlaskDraw(int initial = 2)
        {
            Vessel(false);
            actor.SetTag("Player");
            actor.Statistics["Hitpoints"] = new Stat { Owner = actor, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            pool.Volume = 7;
            var flask = vessel.GetPart<LiquidVesselPart>();
            flask.Volume = initial;
            flask.LiquidId = initial > 0 ? "water" : "";
            string prefix = "FillLiquidVessel|" + Uri.EscapeDataString(source.ID) + "|";
            return InventorySystem.GetActions(actor, vessel).Single(row => row.Command.StartsWith(prefix, StringComparison.Ordinal)).Command;
        }

        sealed class AfterFillProbe : Part
        {
            public Action Run;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction") Run();
                return true;
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void LateIndependentDrawsAndInjurySurviveARefusedFiniteFlaskFill(bool veto)
        {
            string command = PrepareFlaskDraw();
            var flask = vessel.GetPart<LiquidVesselPart>();
            int calls = 0;
            actor.AddPart(new AfterFillProbe { Run = () =>
            {
                calls++;
                Assert.AreEqual(6, pool.Volume, "The fill already took one measure.");
                Assert.AreEqual(3, flask.Volume, "The same measure already reached the flask.");
                pool.Volume -= 2;
                flask.Volume--;
                actor.GetStat("Hitpoints").Penalty += 2;
                if (veto) throw new InvalidOperationException("Independent draw vetoes the staged fill.");
            } });

            Assert.False(InventorySystem.PerformAction(actor, vessel, command, zone));
            Assert.AreEqual(1, calls);
            Assert.AreEqual(5, pool.Volume, "Seven less the independent draw of two.");
            Assert.AreEqual(1, flask.Volume, "Two less the independent draw of one.");
            Assert.AreEqual("water", flask.LiquidId);
            Assert.AreEqual(18, actor.GetStatValue("Hitpoints"));
            Assert.IsEmpty(dirty);
        }

        [Test]
        public void SpendingStagedWaterLeavesOnlyTheRecoverableMeasuresToRefund()
        {
            string command = PrepareFlaskDraw(initial: 0);
            var flask = vessel.GetPart<LiquidVesselPart>();
            int calls = 0;
            actor.AddPart(new AfterFillProbe { Run = () =>
            {
                calls++;
                Assert.AreEqual(4, pool.Volume);
                Assert.AreEqual(3, flask.Volume);
                flask.Volume--;
            } });

            Assert.False(InventorySystem.PerformAction(actor, vessel, command, zone));
            Assert.AreEqual(1, calls);
            Assert.AreEqual(6, pool.Volume, "Only two staged measures were still recoverable.");
            Assert.AreEqual(0, flask.Volume);
            Assert.AreEqual("", flask.LiquidId);
            Assert.IsEmpty(dirty);
        }

        [TestCase("moved-source")] [TestCase("new-pool-part")] [TestCase("new-flask-part")] [TestCase("dropped-flask")]
        public void AFiniteFlaskFillRejectsLateMovementOrReplacementWithoutUndoingIt(string change)
        {
            string command = PrepareFlaskDraw();
            var flask = vessel.GetPart<LiquidVesselPart>();
            var pack = actor.GetPart<InventoryPart>();
            var replacementPool = new LiquidPoolPart { LiquidId = "water", Volume = 4 };
            var replacementFlask = new LiquidVesselPart { Capacity = 3, Volume = 1, LiquidId = "oil" };
            int calls = 0;
            actor.AddPart(new AfterFillProbe { Run = () =>
            {
                calls++;
                Assert.AreEqual(6, pool.Volume);
                Assert.AreEqual(3, flask.Volume);
                switch (change)
                {
                    case "moved-source": Assert.True(zone.MoveEntity(source, 11, 11)); break;
                    case "new-pool-part": Assert.True(source.RemovePart(pool)); source.AddPart(replacementPool); break;
                    case "new-flask-part": Assert.True(vessel.RemovePart(flask)); vessel.AddPart(replacementFlask); break;
                    case "dropped-flask": Assert.True(pack.RemoveObject(vessel)); Assert.True(zone.AddEntity(vessel, 10, 11)); break;
                }
                dirty.Clear();
            } });

            Assert.False(InventorySystem.PerformAction(actor, vessel, command, zone));
            Assert.AreEqual(1, calls);
            Assert.IsEmpty(dirty, "No draw was published after the independent mutation.");
            if (change == "new-pool-part")
            {
                Assert.AreSame(replacementPool, source.GetPart<LiquidPoolPart>());
                Assert.AreEqual(4, replacementPool.Volume);
            }
            else Assert.AreEqual(change == "new-flask-part" ? 6 : 7, pool.Volume);
            if (change == "new-flask-part")
            {
                Assert.AreSame(replacementFlask, vessel.GetPart<LiquidVesselPart>());
                Assert.AreEqual(1, replacementFlask.Volume);
                Assert.AreEqual("oil", replacementFlask.LiquidId);
            }
            else Assert.AreEqual(2, flask.Volume);
            if (change == "moved-source") Assert.AreSame(zone.GetCell(11, 11), zone.GetEntityCell(source));
            if (change == "dropped-flask")
            {
                Assert.AreSame(zone.GetCell(10, 11), zone.GetEntityCell(vessel));
                Assert.False(pack.Objects.Contains(vessel));
                Assert.IsNull(vessel.GetPart<PhysicsPart>().InInventory);
            }
        }

        [TestCase(false,3)][TestCase(true,3)][TestCase(false,7)][TestCase(true,7)]
        public void CommittedFiniteDrawInvalidatesExactCellAndPreservesPersistentOwner(bool skin,int amount)
        {Vessel(skin);pool.Volume=amount;Assert.True(Fill(skin));Assert.AreEqual(3,Carried(skin));Assert.AreEqual(amount-3,pool.Volume);Assert.AreSame(source,zone.GetReadOnlyEntities().Single(e=>e.ID==source.ID));Assert.AreSame(pool,source.GetPart<LiquidPoolPart>());OneSourceDirty();}
        [TestCase(false)][TestCase(true)] public void OuterCommitIsOnlyPublicationBoundary(bool skin)
        {Vessel(skin);var tx=new InventoryTransaction();try{Assert.True(Fill(skin,tx));Assert.Zero(pool.Volume);Assert.IsEmpty(dirty);tx.Commit();OneSourceDirty();tx.Commit();OneSourceDirty();}finally{tx.Rollback();}}
        [TestCase(false)][TestCase(true)] public void OuterRollbackRestoresVolumeWithoutPublication(bool skin)
        {Vessel(skin);var tx=new InventoryTransaction();try{Assert.True(Fill(skin,tx));Assert.Zero(pool.Volume);}finally{tx.Rollback();}Assert.AreEqual(3,pool.Volume);Assert.Zero(Carried(skin));Assert.IsEmpty(dirty);}
        [TestCase(false)][TestCase(true)] public void FullVesselRefusalDoesNotInvalidateSource(bool skin)
        {Vessel(skin);if(skin)vessel.GetPart<WaterskinPart>().Charges=3;else{vessel.GetPart<LiquidVesselPart>().Volume=3;vessel.GetPart<LiquidVesselPart>().LiquidId="water";}Assert.False(Fill(skin));Assert.AreEqual(3,pool.Volume);Assert.IsEmpty(dirty);}
        [TestCase(false,"removed")][TestCase(true,"removed")][TestCase(false,"replacement")][TestCase(true,"replacement")]
        [TestCase(false,"part")][TestCase(true,"part")][TestCase(false,"moved")][TestCase(true,"moved")]
        [TestCase(false,"backlink")][TestCase(true,"backlink")]
        public void StaleOuterCommitDoesNotPublishAnotherSourceOrCell(bool skin,string mutation)
        {
            Vessel(skin);var tx=new InventoryTransaction();try
            {
                Assert.True(Fill(skin,tx));Assert.IsEmpty(dirty);
                if(mutation=="removed"||mutation=="replacement")
                {Assert.True(zone.RemoveEntity(source));if(mutation=="replacement"){var other=Owner("SpreadDrawPoint");other.ID=source.ID;other.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});Assert.True(zone.AddEntity(other,11,10));Assert.AreNotSame(source,zone.GetReadOnlyEntities().Single(e=>e.ID==source.ID));}}
                else if(mutation=="part"){source.RemovePart(pool);source.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});Assert.AreNotSame(pool,source.GetPart<LiquidPoolPart>());}
                else if(mutation=="moved"){Assert.True(zone.MoveEntity(source,12,10));Assert.AreEqual((12,10),zone.GetEntityPosition(source));}
                else{pool.ParentEntity=actor;Assert.AreNotSame(source,pool.ParentEntity);}
                dirty.Clear();
                Assert.Throws<InvalidOperationException>(()=>tx.Commit());tx.Rollback();
                if(skin || mutation=="moved")Assert.AreEqual(3,pool.Volume);
                Assert.Zero(Carried(skin));
                if(!skin)Assert.AreEqual("",vessel.GetPart<LiquidVesselPart>().LiquidId);
                Assert.IsEmpty(dirty);
            }finally{tx.Rollback();}
        }
        [TestCase(false)][TestCase(true)] public void ExistingPouredPoolStillRetiresThroughItsExistingLifecycle(bool skin)
        {Vessel(skin);source.BlueprintName=LiquidVesselService.PoolBlueprint;Assert.True(Fill(skin));Assert.Zero(pool.Volume);Assert.IsNull(zone.GetEntityCell(source));Assert.True(dirty.Any(d=>d.reason=="LiquidVessel.SourceExhausted"));}
        [TestCase(false)][TestCase(true)] public void MovedPouredOwnerCannotRetireOrDirtyItsNewCellOnOuterCommit(bool skin)
        {Vessel(skin);source.BlueprintName=LiquidVesselService.PoolBlueprint;var tx=new InventoryTransaction();try{Assert.True(Fill(skin,tx));Assert.True(zone.MoveEntity(source,12,10));Assert.AreEqual((12,10),zone.GetEntityPosition(source));dirty.Clear();Assert.Throws<InvalidOperationException>(()=>tx.Commit());tx.Rollback();Assert.AreEqual(3,pool.Volume);Assert.Zero(Carried(skin));Assert.AreSame(zone.GetCell(12,10),zone.GetEntityCell(source));Assert.AreEqual((12,10),zone.GetEntityPosition(source));Assert.IsEmpty(dirty);}finally{tx.Rollback();}}
        [Test] public void InfiniteWellWithoutFinitePoolDoesNotEmitFiniteVolumeInvalidation()
        {Vessel(true);source.RemovePart(pool);source.AddPart(new WellPart());Assert.True(Fill(true));Assert.AreEqual(3,Carried(true));Assert.IsEmpty(dirty);}

        void NaturalSteamSource(bool exhausted)
        {
            Assert.True(zone.RemoveEntity(source));
            var factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            source=factory.CreateEntity("SteamVent");
            Assert.NotNull(source);
            Assert.IsNull(source.GetPart<LiquidPoolPart>(),"The authored vent has no separate liquid reservoir.");
            Assert.IsNull(source.GetPart<WellPart>());
            Assert.AreEqual("Steam",source.GetPart<MaterialPart>().MaterialID);
            Assert.True(source.GetPart<MaterialPart>().HasMaterialTag("Vapor"));
            Assert.True(zone.AddEntity(source,11,10));
            if(exhausted)source.AddPart(new SourceStockPart{Capacity=3,Available=0,LastTick=123});
            zone.GetCell(10,10).IsVisible=true;
            zone.GetCell(11,10).IsVisible=true;
            Vessel(true);
        }
        InventoryAction[] DirectSourceRows()
        {
            var cell=zone.GetCell(11,10);
            return WorldInteractionSystem.GatherCellActions(WorldInteractionSystem.ResolveTarget(cell,actor),cell,actor)
                .Where(a=>a.Command!=null&&a.Command.StartsWith("FillWaterskin|",StringComparison.Ordinal)).ToArray();
        }
        void SteamStockUnchanged(bool exhausted)
        {
            var stock=source.GetPart<SourceStockPart>();
            if(!exhausted)Assert.IsNull(stock,"A rejected direct fill must not initialize condensation stock.");
            else
            {
                Assert.NotNull(stock);Assert.AreSame(source,stock.ParentEntity);
                Assert.AreEqual(3,stock.Capacity);Assert.Zero(stock.Available);Assert.AreEqual(123,stock.LastTick);
            }
        }
        [TestCase(false)][TestCase(true)]
        public void AuthoredSteamVentOffersNoDirectWaterskinFill(bool exhausted)
        {
            NaturalSteamSource(exhausted);
            Assert.IsEmpty(DirectSourceRows(),"A renewing vapor coating is not an unlimited liquid spring.");
            Assert.Zero(Carried(true));SteamStockUnchanged(exhausted);
        }
        [TestCase(false,false)][TestCase(false,true)][TestCase(true,false)][TestCase(true,true)]
        public void AuthoredSteamVentCannotBypassMissingOrExhaustedCondensationStock(bool exhausted,bool named)
        {
            NaturalSteamSource(exhausted);
            string command=named?"FillWaterskin|"+Uri.EscapeDataString(source.ID):"FillWaterskin";
            Assert.False(WaterVesselService.TryAct(actor,vessel,zone,command));
            Assert.Zero(Carried(true));SteamStockUnchanged(exhausted);Assert.IsEmpty(dirty);
        }
        [Test] public void GenericVaporIdentityCannotBecomeDirectWaterByChangingTheBlueprintName()
        {
            NaturalSteamSource(false);
            source.BlueprintName="OtherWaterVaporSource";
            source.GetPart<MaterialPart>().MaterialID="WaterMist";
            Assert.True(source.GetPart<MaterialPart>().HasMaterialTag("Vapor"));
            Assert.False(WaterVesselService.TryAct(actor,vessel,zone,"FillWaterskin|"+Uri.EscapeDataString(source.ID)));
            Assert.Zero(Carried(true));SteamStockUnchanged(false);
        }
        [Test] public void OrdinaryNonVaporSpringRetainsItsDirectFillRowAndUnlimitedSupply()
        {
            source.RemovePart(pool);
            source.AddPart(new TileStateSourcePart{Coating="water",CoatingTurns=8});
            zone.GetCell(10,10).IsVisible=true;zone.GetCell(11,10).IsVisible=true;
            Vessel(true);
            var rows=DirectSourceRows();Assert.AreEqual(1,rows.Length);
            Assert.AreEqual("FillWaterskin|"+Uri.EscapeDataString(source.ID),rows[0].Command);
            Assert.True(WaterVesselService.TryAct(actor,vessel,zone,rows[0].Command));
            Assert.AreEqual(3,Carried(true));Assert.IsNull(source.GetPart<SourceStockPart>());Assert.IsEmpty(dirty);
        }
    }
}
