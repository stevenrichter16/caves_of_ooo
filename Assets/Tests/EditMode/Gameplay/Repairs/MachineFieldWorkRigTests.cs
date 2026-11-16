using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using System.Reflection;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineFieldWorkRigTests : FiftyWorldFixture
    {
        const string Blueprint = "AbandonedFieldWorkRig";
        EntityFactory oldForge;
        [SetUp] public void SetupRig() { oldForge = ForgePart.Factory; ForgePart.Factory = Factory; RepairRecipeRegistry.ResetForTests(); }
        [TearDown] public void RestoreRig() { ForgePart.Factory = oldForge; RepairRecipeRegistry.ResetForTests(); }

        // The pre-content RED fixture uses real native Parts, not replacement services.
        Entity Rig(bool repaired = false)
        {
            var e = Factory.Blueprints.ContainsKey(Blueprint) ? Factory.CreateEntity(Blueprint) : null;
            if (e == null)
            {
                e = Factory.CreateEntity("TinkersForge"); e.BlueprintName = Blueprint; e.SetTag("RecoverableWorkRig");
                e.GetPart<PhysicsPart>().Solid = true; e.GetPart<PhysicsPart>().Weight = 120;
                e.AddPart(new HandlingPart { Weight = 120, Carryable = false, Throwable = false, BulkClass = "Massive" });
                e.AddPart(new CompositionPart { MaterialsRaw = "Masonry,Wood" });
                e.AddPart(new RepairablePart { RecipeId = "clay-field-work-rig" });
                e.AddPart(new HarvestablePart { YieldBlueprint = "SalvagedTimber", YieldMin = 2, YieldMax = 2, YieldChance = 100,
                    ActionText = "dismantle: 2 timber; lose workshop" });
            }
            e.GetPart<RepairablePart>().Repaired = repaired; Assert.True(Zone.AddEntity(e, 11, 10)); return e;
        }
        Entity Weapon()
        {
            var command = new ForgeWeaponCommand(Carry("SteelBladeComponent"), Carry("OakHaftComponent"), Carry("LeatherBindingComponent"), Factory);
            Assert.True(InventorySystem.ExecuteCommand(command, Actor, Zone).Success, "Single pack craft stays available");
            return command.ForgedWeapons.Single();
        }
        [Test] public void FaultedRigIsNotAForgeAndHasNoCraftMenu()
        {
            var rig = Rig(); Assert.False(ForgePart.IsNearForge(Actor, Zone));
            Assert.False(Actions(rig).Any(a => a.Command == "CraftKit"));
            Assert.True(Actions(rig).Any(a => a.Command == RepairablePart.RepairCommand));
            Assert.True(Actions(rig).Any(a => a.Command == "Harvest"));
        }
        [Test] public void OrdinaryForgeStillWorksAndSinglePackCraftNeedsNoForge()
        {
            Assert.NotNull(Weapon()); Assert.False(ForgePart.IsNearForge(Actor, Zone));
            var forge = Place("TinkersForge"); Assert.True(ForgePart.IsNearForge(Actor, Zone));
            Assert.True(Actions(forge).Any(a => a.Command == "CraftKit"));
        }
        [Test] public void RepairConsumesTwoClayOnceAndReclamationNeverRefundsClay()
        {
            var rig = Rig(); Carry("FireClay"); Carry("FireClay"); Carry("FireClay");
            Assert.True(Act(rig, RepairablePart.RepairCommand)); Assert.AreEqual(1, Count("FireClay"));
            Assert.True(ForgePart.IsNearForge(Actor, Zone)); Assert.False(Act(rig, RepairablePart.RepairCommand));
            Assert.True(Act(rig, "Harvest")); Assert.AreEqual(2, Count("SalvagedTimber")); Assert.AreEqual(1, Count("FireClay"));
            Assert.False(ForgePart.IsNearForge(Actor, Zone)); Assert.False(Act(rig, "Harvest"));
            Assert.AreEqual(2, Count("SalvagedTimber"));
        }
        [Test] public void WorkRequiresReleaseAfterRealHaulingAndBrokenRigCanStillBeMoved()
        {
            Actor.GetStat("Strength").BaseValue = 18; var rig = Rig();
            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(Actor, rig, Zone));
            rig.GetPart<RepairablePart>().Repaired = true;
            Assert.False(ForgePart.IsNearForge(Actor, Zone)); Assert.False(Actions(rig).Any(a => a.Command == "CraftKit"));
            Assert.True(DragSystem.Release(Actor)); Assert.True(ForgePart.IsNearForge(Actor, Zone));
        }
        [TestCase("spent")] [TestCase("duplicate-forge")] [TestCase("duplicate-fault")] [TestCase("wrong-fault")]
        [TestCase("portable-fault")] [TestCase("wrong-weight")] [TestCase("wrong-yield")] [TestCase("takeable")]
        public void MalformedOrSpentRigCannotSupplyWorkshopAuthority(string fault)
        {
            var rig = Rig(true);
            switch (fault)
            {
                case "spent": rig.GetPart<HarvestablePart>().Harvested = true; break;
                case "duplicate-forge": rig.AddPart(new ForgePart()); break;
                case "duplicate-fault": rig.AddPart(new RepairablePart { Repaired = true }); break;
                case "wrong-fault": rig.GetPart<RepairablePart>().RecipeId = "clay-well-lining"; break;
                case "portable-fault": rig.GetPart<RepairablePart>().PortableEquipment = true; break;
                case "wrong-weight": rig.GetPart<HandlingPart>().Weight = 1; break;
                case "wrong-yield": rig.GetPart<HarvestablePart>().YieldMax = 9; break;
                case "takeable": rig.GetPart<PhysicsPart>().Takeable = true; break;
            }
            Assert.False(ForgePart.IsNearForge(Actor, Zone)); Assert.False(Actions(rig).Any(a => a.Command == "CraftKit"));
        }
        [Test] public void ReforgeRefusesFaultThenChangesActualAssemblyAndReturnsOldHead()
        {
            var weapon = Weapon(); var newHead = Carry("PeatMalletHeadComponent"); var rig = Rig();
            var command = new ReforgeWeaponCommand(weapon, newHead, Factory);
            Assert.False(InventorySystem.ExecuteCommand(command, Actor, Zone).Success);
            Assert.AreEqual("SteelBladeComponent", weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
            rig.GetPart<RepairablePart>().Repaired = true;
            Assert.True(InventorySystem.ExecuteCommand(new ReforgeWeaponCommand(weapon, newHead, Factory), Actor, Zone).Success);
            Assert.AreEqual("PeatMalletHeadComponent", weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
            Assert.AreEqual(1, Count("SteelBladeComponent")); Assert.AreEqual(0, Count("PeatMalletHeadComponent"));
        }
        [Test] public void RepairAndReclamationRollbackOnAfterActionFailure()
        {
            var rig = Rig(); Carry("FireClay"); Carry("FireClay"); FailAfter();
            Assert.False(Act(rig, RepairablePart.RepairCommand)); Assert.False(rig.GetPart<RepairablePart>().Repaired);
            Assert.AreEqual(2, Count("FireClay")); Assert.False(Act(rig, "Harvest"));
            Assert.False(rig.GetPart<HarvestablePart>().Harvested); Assert.NotNull(Zone.GetEntityCell(rig));
            Assert.AreEqual(0, Count("SalvagedTimber"));
        }
        [Test] public void ReclamationOverflowStaysAtTheSourceAndSpentStateSurvivesGraphReload()
        {
            var rig = Rig(); Pack.MaxWeight = 0; Assert.True(Act(rig, "Harvest"));
            Assert.AreEqual(2, Zone.GetCell(11, 10).Objects.Count(e => e.BlueprintName == "SalvagedTimber"));
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(rig);
            Assert.True(copy.GetPart<HarvestablePart>().Harvested); Assert.AreEqual(0, Pack.Objects.Count(e => e.BlueprintName == "SalvagedTimber"));
        }
        [TestCase("remove")] [TestCase("replace")] [TestCase("haul")]
        public void ReforgeKeepsTheExactStationThroughCreationCallbacks(string change)
        {
            var weapon = Weapon(); var head = Carry("PeatMalletHeadComponent"); var rig = Rig(true);
            Factory.RegisterPartType<RigCreationCallback>();
            Factory.Blueprints["SteelBladeComponent"].Parts["RigCreationCallback"] = new Dictionary<string, string>();
            RigCreationCallback.Action = () =>
            {
                if (change == "haul") { Actor.GetStat("Strength").BaseValue = 18; DragSystem.TryGrab(Actor, rig, Zone); }
                else { Zone.RemoveEntity(rig); if (change == "replace") Place("TinkersForge"); }
            };
            try
            {
                Assert.False(InventorySystem.ExecuteCommand(new ReforgeWeaponCommand(weapon, head, Factory), Actor, Zone).Success);
                Assert.AreEqual("SteelBladeComponent", weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
                Assert.AreEqual(1, Count("PeatMalletHeadComponent")); Assert.AreEqual(0, Count("SteelBladeComponent"));
            }
            finally { RigCreationCallback.Action = null; }
        }
        [Test] public void WorldReforgeJoinsOuterReceiptAndCannotCommitThroughAfterActionFailure()
        {
            var weapon = Weapon(); var head = Carry("PeatMalletHeadComponent"); var rig = Rig(true);
            CraftingMarkPart.Toggle(weapon); CraftingMarkPart.Toggle(head); FailAfter();
            Assert.False(Act(rig, "ReforgeWeapon"));
            Assert.AreEqual("SteelBladeComponent", weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
            Assert.AreEqual(1, Count("PeatMalletHeadComponent")); Assert.AreEqual(0, Count("SteelBladeComponent"));
        }
        [TestCase("station")] [TestCase("actor")]
        public void GridReplacementDuringReforgeCannotAuthorizeWork(string target)
        {
            var weapon=Weapon();var head=Carry("PeatMalletHeadComponent");var rig=Rig(true);
            Factory.RegisterPartType<RigCreationCallback>();Factory.Blueprints["SteelBladeComponent"].Parts["RigCreationCallback"]=new Dictionary<string,string>();
            var cell=Zone.GetEntityCell(target=="actor"?Actor:rig); Cell replacement=null;
            RigCreationCallback.Action=()=> { replacement=new Cell(cell.X,cell.Y,Zone); Zone.Cells[cell.X,cell.Y]=replacement; };
            try { Assert.False(InventorySystem.ExecuteCommand(new ReforgeWeaponCommand(weapon,head,Factory),Actor,Zone).Success);
                Assert.AreEqual("SteelBladeComponent",weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);Assert.AreEqual(1,Count("PeatMalletHeadComponent"));
                Assert.AreSame(replacement,Zone.GetCell(cell.X,cell.Y)); }
            finally { RigCreationCallback.Action=null; }
        }
        [TestCase("repaired")] [TestCase("portable")] [TestCase("container")] [TestCase("inventory")] [TestCase("weight")]
        public void InvalidOptionalContentPreservesBaseWorks(string change)
        {
            var bp=Factory.Blueprints[Blueprint];
            if(change=="repaired")bp.Parts["Repairable"]["Repaired"]="true";
            else if(change=="portable")bp.Parts["Repairable"]["PortableEquipment"]="true";
            else if(change=="weight")bp.Parts["Handling"]["Weight"]="1";
            else bp.Parts[change=="container"?"Container":"Inventory"]=new Dictionary<string,string>();
            var zone=new Zone(SoddenDistrictPlan.WorksZoneID);var builder=new SoddenDistrictBuilder(64);
            Assert.True(builder.BuildZone(zone,Factory,new Random(64)));Assert.True(builder.ValidateFinal(zone));
            Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName==Blueprint));
            Assert.AreEqual(1,zone.GetCell(56,5).Objects.Count(e=>e.BlueprintName=="StoneFloor"));
        }
        public sealed class RigStockCallback:Part
        {
            public override bool HandleEvent(GameEvent e) { if(e.ID=="ObjectCreated") ParentEntity.AddPart(new InventoryPart());return true; }
        }
        [Test] public void CreationCallbackCannotAddUnplannedRigStorage()
        {
            Factory.RegisterPartType<RigStockCallback>();Factory.Blueprints[Blueprint].Parts["RigStockCallback"]=new Dictionary<string,string>();
            var zone=new Zone(SoddenDistrictPlan.WorksZoneID);var builder=new SoddenDistrictBuilder(64);
            Assert.False(builder.BuildZone(zone,Factory,new Random(64)));Assert.AreEqual(0,zone.EntityCount);Assert.AreEqual(0,zone.GenReservedCells.Count);
        }
        public sealed class RigAfterCallback:Part
        {
            public Action Callback; public override bool HandleEvent(GameEvent e) { if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true; }
        }
        [TestCase(false)] [TestCase(true)]
        public void RepairOrDismantleRefusesActorMovementInOuterCallback(bool dismantle)
        {
            var rig=Rig();Carry("FireClay");Carry("FireClay");
            Actor.AddPart(new RigAfterCallback { Callback=()=> { Zone.RemoveEntity(Actor);Zone.AddEntity(Actor,40,20); } });
            Assert.False(Act(rig,dismantle?"Harvest":RepairablePart.RepairCommand));
            Assert.False(rig.GetPart<RepairablePart>().Repaired);Assert.False(rig.GetPart<HarvestablePart>().Harvested);
            Assert.AreEqual(2,Count("FireClay"));Assert.AreEqual(0,Count("SalvagedTimber"));Assert.AreEqual((40,20),Zone.GetEntityPosition(Actor));
        }
        [TestCase("valid")] [TestCase("fault")] [TestCase("after")]
        public void RealWorldMenuReforgeCommitsOneActionOrNothing(string condition)
        {
            using(var ui=new HotbarSaveFixture(true,false))
            {
                var manager=OverworldZoneManager.CreateDetached(Factory,64,true);manager.SetActiveZone(Zone);
                var weapon=Weapon();var head=Carry("PeatMalletHeadComponent");var rig=Rig(true);
                CraftingMarkPart.Toggle(weapon);CraftingMarkPart.Toggle(head);
                var turns=new TurnManager();turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("rig-input","normal-world-menu",manager,turns,Actor));
                ForgePart.Factory=Factory;
                var menu=ui.Root.AddComponent<WorldActionMenuUI>();ui.Input.WorldActionMenuUI=menu;
                const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
                var state=typeof(InputHandler).GetField("_worldActionMenuReturnState",hidden);state.SetValue(ui.Input,Enum.Parse(state.FieldType,"Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor",hidden).Invoke(ui.Input,new object[]{rig,Zone.GetEntityCell(rig),false});
                var action=((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",hidden).GetValue(menu)).Single(a=>a.Command=="CraftKit");
                if(condition=="fault")rig.GetPart<RepairablePart>().Repaired=false;if(condition=="after")FailAfter();
                typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",hidden).Invoke(ui.Input,new object[]{action,rig,Zone.GetEntityCell(rig),false});
                bool success=condition=="valid";Assert.AreEqual(success?"PeatMalletHeadComponent":"SteelBladeComponent",weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
                Assert.AreEqual(success?27:17,turns.TickCount);Assert.AreEqual(success?0:1,Count("PeatMalletHeadComponent"));Assert.AreEqual(success?1:0,Count("SteelBladeComponent"));
            }
        }
        [Test] public void LateHarvestPartReplacementRestoresOriginalSourceWithoutOverwritingReplacement()
        {
            var rig=Rig();var old=rig.GetPart<HarvestablePart>();var replacement=new HarvestablePart();
            Actor.AddPart(new RigAfterCallback { Callback=()=> { rig.RemovePart(old);rig.AddPart(replacement); } });
            Assert.False(Act(rig,"Harvest"));Assert.AreEqual(0,Count("SalvagedTimber"));
            Assert.AreEqual((11,10),Zone.GetEntityPosition(rig));Assert.AreSame(replacement,rig.GetPart<HarvestablePart>());
            Assert.False(old.Harvested);Assert.False(replacement.Harvested);
        }
        [TestCase("valid")] [TestCase("sharp")] [TestCase("temper")] [TestCase("live-component-stat")]
        [TestCase("missing-blueprint")] [TestCase("zero-component")] [TestCase("stale-owner")]
        public void RefitDetailsPreviewTheRealResultWithoutWorkOrFactoryCallbacks(string condition)
        {
            using (var ui = new HotbarSaveFixture(true, false))
            {
                const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var manager = OverworldZoneManager.CreateDetached(Factory,64,true); manager.SetActiveZone(Zone);
                var weapon = Weapon(); var head = Carry("PeatMalletHeadComponent"); var rig = Rig(true);
                if (condition == "sharp") weapon.SetTag("ModSharp");
                if (condition == "temper") weapon.AddPart(new WeaponTemperPart { TemperCount = 1 });
                if (condition == "live-component-stat") head.GetPart<WeaponComponentPart>().PenBonus = 999;
                if (condition == "missing-blueprint") weapon.GetPart<WeaponAssemblyPart>().HaftBlueprint = "MissingHaft";
                CraftingMarkPart.Toggle(weapon); CraftingMarkPart.Toggle(head);
                var turns = new TurnManager(); turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("rig-preview","free reader",manager,turns,Actor)); ForgePart.Factory = Factory;
                var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                var reader = ui.Root.AddComponent<AnnouncementUI>(); ui.Input.AnnouncementUI = reader;
                var grid = new UnityEngine.GameObject("Preview grid"); grid.transform.SetParent(ui.Root.transform); grid.AddComponent<UnityEngine.Grid>();
                UnityEngine.Tilemaps.Tilemap Tiles(string name) { var go = new UnityEngine.GameObject(name); go.transform.SetParent(grid.transform); return go.AddComponent<UnityEngine.Tilemaps.Tilemap>(); }
                reader.Tilemap = Tiles("Text"); reader.BgTilemap = Tiles("Background");
                menu.Tilemap = Tiles("Menu text"); menu.BgTilemap = Tiles("Menu background");
                Zone.GetEntityCell(rig).IsVisible = true; Zone.GetEntityCell(rig).Explored = true;
                var state = typeof(InputHandler).GetField("_worldActionMenuReturnState",hidden); state.SetValue(ui.Input,Enum.Parse(state.FieldType,"Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor",hidden).Invoke(ui.Input,new object[]{rig,Zone.GetEntityCell(rig),false});
                var rows = (List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",hidden).GetValue(menu);
                typeof(WorldActionMenuUI).GetField("_cursorIndex",hidden).SetValue(menu,rows.FindIndex(a=>a.Command=="CraftKit"));
                if (condition == "zero-component") head.GetPart<StackerPart>().StackCount = 0;
                if (condition == "stale-owner") Zone.RemoveEntity(rig);
                var idField = typeof(EntityFactory).GetField("_nextEntityID",hidden); var nextId = idField.GetValue(Factory);
                var items = Actor.GetPart<InventoryPart>().Objects.ToArray(); var originalDamage = weapon.GetPart<MeleeWeaponPart>().BaseDamage;
                typeof(InputHandler).GetMethod("OpenWorldActionDetailsReader",hidden).Invoke(ui.Input,null);
                if (condition == "stale-owner") Assert.False(reader.IsOpen);
                else
                {
                    Assert.True(reader.IsOpen); var lines = new List<string>();
                    for (int i=0;i<reader.PageCount;i++) { reader.GoToPage(i); lines.AddRange(reader.VisibleLines); }
                    string text = string.Join(" ",lines);
                    if (condition == "missing-blueprint" || condition == "zero-component") StringAssert.Contains("Refit preview unavailable",text);
                    else
                    {
                        StringAssert.Contains("Family: Cudgel",text); StringAssert.Contains("Damage: 1d4",text);
                        StringAssert.Contains("Consume: 1 peat-packing mallet head",text); StringAssert.Contains("Return: 1 steel blade",text);
                        StringAssert.Contains(condition=="sharp" ? "Penetration: +0" : "Penetration: -1",text);
                        if (condition=="temper") StringAssert.Contains("Current temper is removed",text);
                        StringAssert.DoesNotContain("999",text);
                    }
                    reader.Close();
                }
                Assert.AreEqual(17,turns.TickCount); Assert.AreEqual(nextId,idField.GetValue(Factory),"Readers must not create transient components");
                CollectionAssert.AreEqual(items,Actor.GetPart<InventoryPart>().Objects); Assert.AreEqual(originalDamage,weapon.GetPart<MeleeWeaponPart>().BaseDamage);
                Assert.AreEqual("SteelBladeComponent",weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
            }
        }
        [Test] public void MissingRigContextRefusesWithoutThrowing()
        {
            var rig=Rig();var method=typeof(ForgePart).Assembly.GetType("CavesOfOoo.Core.FieldWorkRigRules").GetMethod("Available",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.False((bool)method.Invoke(null,new object[]{null,rig,Zone,false,false}));
            Assert.False((bool)method.Invoke(null,new object[]{Actor,null,Zone,false,false}));
            Assert.False((bool)method.Invoke(null,new object[]{Actor,rig,null,false,false}));
        }
        [Test] public void FailedRigReforgePublishesNoSuccessMessage()
        {
            var weapon=Weapon();var head=Carry("PeatMalletHeadComponent");var rig=Rig(true);CraftingMarkPart.Toggle(weapon);CraftingMarkPart.Toggle(head);FailAfter();
            var messages=new List<string>();MessageLog.OnMessage=messages.Add;
            try { Assert.False(Act(rig,"ReforgeWeapon")); } finally { MessageLog.OnMessage=null; }
            Assert.False(messages.Any(m=>m.Contains("re-forges")));
        }
        public sealed class RigCreationCallback : Part
        {
            public static Action Action;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "ObjectCreated") Action?.Invoke(); return true; }
        }
        [TestCase(1)] [TestCase(64)] [TestCase(224401440)]
        public void FreshWorksPlacesExactlyOneRigWithoutChangingExistingFloorLockerOrRoutes(int seed)
        {
            var zone = new Zone(SoddenDistrictPlan.WorksZoneID); var builder = new SoddenDistrictBuilder(seed);
            Assert.True(builder.BuildZone(zone, Factory, new Random(seed))); Assert.True(builder.ValidateFinal(zone));
            var rigs = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == Blueprint).ToArray(); Assert.AreEqual(1, rigs.Length);
            Assert.AreEqual((56, 5), zone.GetEntityPosition(rigs[0]));
            Assert.AreEqual(1, zone.GetCell(56, 5).Objects.Count(e => e.BlueprintName == "StoneFloor"));
            Assert.True(zone.GenReservedCells.Contains((56, 5))); Assert.False(zone.GetCell(54, 10).BlocksMovement());
            Assert.AreEqual(8, zone.GetCell(61, 6).Objects.Single(e => e.BlueprintName == "SoddenWorksLocker").GetPart<ContainerPart>().Contents.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            Assert.AreEqual(4, zone.GetCell(27, 16).Objects.Single(e => e.BlueprintName == "SoddenWorksSalvage").GetPart<HarvestablePart>().YieldMax);
        }
        [TestCase(SoddenDistrictPlan.StopZoneID)] [TestCase(SoddenDistrictPlan.CrossingZoneID)] [TestCase(SoddenDistrictPlan.WorksZoneID)]
        public void MissingOptionalRigDoesNotRemoveAnyBaseDistrict(string zoneId)
        {
            Factory.Blueprints.Remove(Blueprint); var zone = new Zone(zoneId); var builder = new SoddenDistrictBuilder(64);
            Assert.True(builder.BuildZone(zone, Factory, new Random(64))); Assert.True(builder.ValidateFinal(zone));
            Assert.False(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == Blueprint));
        }
        [Test] public void CachedGraphIsNeverRetrofitted()
        {
            Factory.Blueprints.Remove(Blueprint); var zone = new Zone(SoddenDistrictPlan.WorksZoneID);
            Assert.True(new SoddenDistrictBuilder(64).BuildZone(zone, Factory, new Random(64))); int count = zone.EntityCount;
            Assert.False(new SoddenDistrictBuilder(64).BuildZone(zone, Factory, new Random(64)));
            Assert.AreEqual(count, zone.EntityCount); Assert.False(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == Blueprint));
        }
    }
}
