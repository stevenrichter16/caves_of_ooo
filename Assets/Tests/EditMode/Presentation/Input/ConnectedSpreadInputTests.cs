using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedSpreadInputTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        sealed class Probe:Part
        {
            public int Before,After;public bool Fail;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="BeforeInventoryAction")Before++;
                if(e.ID=="AfterInventoryAction"){After++;if(Fail)throw new InvalidOperationException("connected-input-rollback");}
                return true;
            }
        }
        [TestCase(100,1,0)][TestCase(200,0,5)]
        public void RealEndTurnSettlesElapsedGrowthAfterSchedulerRatherThanBeforeIt(int speed,int units,int fraction)
        {
            using(var ui=new HotbarSaveFixture(true,false))using(var content=new HaulingContentScope())
            {
                content.Seed(64);var manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);var z=new Zone("Overworld.11.10.0");manager.SetActiveZone(z);
                var player=content.Factory.CreateEntity("Player");player.GetStat("Speed").BaseValue=speed;z.AddEntity(player,4,4);
                var root=content.Factory.CreateEntity("SootrootCrop");z.AddEntity(root,5,4);var crop=root.GetPart<CropPart>();
                crop.GrowthTimingVersion=1;crop.LastGrowthWorldTick=17;crop.MoistureTicks=40;
                var turns=new TurnManager();turns.RestoreSavedState(17,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("connected-timing","native-end-turn",manager,turns,player));
                typeof(InputHandler).GetMethod("EndTurnAndProcess",Hidden).Invoke(ui.Input,null);
                Assert.AreEqual(units,crop.TicksInStage);Assert.AreEqual(fraction,crop.GrowthWetTickRemainder);Assert.AreEqual(turns.TickCount,crop.LastGrowthWorldTick);
                Assert.AreEqual(40-units,crop.MoistureTicks);
            }
        }
        [TestCase(false)][TestCase(true)]
        public void FullyLoadedWorldBaselinesLegacyCropsAndReconcilesOnlyTheActiveGraph(bool legacy)
        {
            using(var ui=new HotbarSaveFixture(true,false))using(var content=new HaulingContentScope())
            {
                content.Seed(64);var state=HotbarSaveFixture.MakeState(0);
                var active=state.ZoneManager.ActiveZone;var inactive=new Zone("Overworld.11.8.0");
                state.ZoneManager.CachedZones[inactive.ZoneID]=inactive;
                foreach(var zone in new[]{active,inactive})
                {
                    var plant=content.Factory.CreateEntity("SootrootCrop");zone.AddEntity(plant,5,4);
                    var crop=plant.GetPart<CropPart>();crop.MoistureTicks=40;
                    crop.GrowthTimingVersion=legacy?0:1;crop.LastGrowthWorldTick=legacy?-1:7;crop.GrowthWetTickRemainder=0;
                }
                HotbarSaveFixture.Set(ui.Bootstrap,"_factory",content.Factory);
                ui.Bootstrap.ApplyLoadedGame(state);
                foreach(var zone in new[]{active,inactive})
                {
                    var crop=zone.GetReadOnlyEntities().Single(e=>e.HasTag("Crop")).GetPart<CropPart>();
                    bool advance=!legacy&&zone==active;
                    Assert.AreEqual(1,crop.GrowthTimingVersion);Assert.AreEqual(legacy||advance?17:7,crop.LastGrowthWorldTick);
                    Assert.AreEqual(advance?1:0,crop.TicksInStage);Assert.AreEqual(advance?39:40,crop.MoistureTicks);
                }
                Assert.AreEqual(17,state.TurnManager.TickCount);
            }
        }

        [TestCase("ink",false)][TestCase("ink",true)][TestCase("kitchen",false)][TestCase("kitchen",true)]
        public void RealWorldMenuUsesAtomicServiceAndPaysExactlyOneSuccessfulAction(string kind,bool failAfter)
        {
            using(var ui=new HotbarSaveFixture(true,false))using(var content=new HaulingContentScope())
            {
                content.Seed(64);bool ink=kind=="ink";var manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);
                var z=new Zone(ink?"Overworld.12.12.0":"Overworld.12.11.0");manager.SetActiveZone(z);
                var player=content.Factory.CreateEntity("Player");z.AddEntity(player,4,4);TradeSystem.SetDrams(player,20);
                var worker=content.Factory.CreateEntity(ink?"CurationJuniorIndexer":"SpreadWaysideCook");z.AddEntity(worker,6,4);
                var target=content.Factory.CreateEntity(ink?"BotanicalInkDesk":"ConnectedBatchPan");z.AddEntity(target,5,4);
                string command=ink?"PrepareBotanicalInk":"StartKitchenBatch";
                if(ink)target.GetPart<BotanicalInkDeskPart>().Configure(z,worker);
                else
                {
                    target.GetPart<RepairablePart>().Repaired=true;
                    var escrow=content.Factory.CreateEntity("ConnectedKitchenEscrow");z.AddEntity(escrow,5,3);
                    var pickup=content.Factory.CreateEntity("ConnectedKitchenPickup");z.AddEntity(pickup,5,5);
                    Assert.True(target.GetPart<KitchenBatchPart>().Configure(z,worker,escrow,pickup));
                }
                foreach(string bp in ink?new[]{"SootrootPulp","SootrootPulp","PitchpodResin"}:new[]{"Emberwheat","Emberwheat","ClaspbeanPulp"})
                    Assert.True(player.GetPart<InventoryPart>().AddObject(content.Factory.CreateEntity(bp)));
                var probe=new Probe{Fail=failAfter};player.AddPart(probe);
                var turns=new TurnManager();turns.RestoreSavedState(17,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("connected-services","native-world-menu",manager,turns,player));SeedPart.Factory=CropSystem.Factory=content.Factory;
                var menu=ui.Root.AddComponent<WorldActionMenuUI>();ui.Input.WorldActionMenuUI=menu;
                var state=typeof(InputHandler).GetField("_worldActionMenuReturnState",Hidden);state.SetValue(ui.Input,Enum.Parse(state.FieldType,"Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor",Hidden).Invoke(ui.Input,new object[]{target,z.GetEntityCell(target),false});
                var action=((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Hidden).GetValue(menu)).Single(a=>a.Command==command);
                typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",Hidden).Invoke(ui.Input,new object[]{action,target,z.GetEntityCell(target),false});
                Assert.AreEqual(1,probe.Before);Assert.AreEqual(1,probe.After);
                Assert.AreEqual(failAfter?20:ink?17:18,TradeSystem.GetDrams(player));
                Assert.AreEqual(failAfter?17:27,turns.TickCount,"Only committed work pays one normal action.");
                if(ink)Assert.AreEqual(failAfter?0:1,player.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName=="InkVial"));
                else Assert.AreEqual(failAfter?"Idle":"Working",target.GetPart<KitchenBatchPart>().State);
            }
        }
    }
}
