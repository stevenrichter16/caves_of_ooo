using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public abstract class ConnectedReserveTestBase
    {
        protected EntityFactory Factory;
        protected OverworldZoneManager Manager;
        protected Zone Reserve, Kitchen;
        protected Entity Player, Keeper, Cook, SoilA, SoilB, Tray, Pan;
        protected Part Claim, Introduction;
        EntityFactory oldCropFactory, oldSeedFactory;
        Zone oldActive;

        [SetUp] public void Setup()
        {
            oldCropFactory = CropSystem.Factory; oldSeedFactory = SeedPart.Factory; oldActive = SettlementRuntime.ActiveZone;
            Factory = new EntityFactory();
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            RepairRecipeRegistry.InitializeFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Repairs/Tier1Repairs.json")));
            CropSystem.Factory = SeedPart.Factory = Factory;
            Manager = OverworldZoneManager.CreateDetached(Factory, 1729, true);
            Reserve = new Zone("Overworld.11.8.0"); Kitchen = new Zone("Overworld.12.11.0");
            Manager.SetActiveZone(Kitchen); Manager.SetActiveZone(Reserve); SettlementRuntime.ActiveZone = Reserve;
            Player = Person("Player", Reserve, 5, 6, true); Keeper = Person("SpreadSeedKeeper", Reserve, 5, 5);
            Cook = Person("SpreadWaysideCook", Kitchen, 5, 5);
            SoilA = Soil(Reserve, 4, 5); SoilB = Soil(Reserve, 4, 4);
            Tray = Factory.CreateEntity("ConnectedReserveTray"); Assert.True(Reserve.AddEntity(Tray, 6, 5));
            Pan = Factory.CreateEntity("ConnectedBatchPan"); Assert.True(Kitchen.AddEntity(Pan, 4, 5));
            Claim = NewPart("LocalGatheringClaimPart"); Keeper.AddPart(Claim);
            Assert.True((bool)Invoke(Claim, "Configure", Reserve, SoilA, SoilB, Tray, 1729));
            Introduction = NewPart("CookIntroductionPart"); Cook.AddPart(Introduction);
            Assert.True((bool)Invoke(Introduction, "Configure", Kitchen, Pan, 1729));
            TradeSystem.SetDrams(Player, 40); TradeSystem.SetDrams(Keeper, 10);
        }

        [TearDown] public void Cleanup()
        {
            CropSystem.Factory = oldCropFactory; SeedPart.Factory = oldSeedFactory; SettlementRuntime.ActiveZone = oldActive;
            RepairRecipeRegistry.ResetForTests();
        }

        protected static Part NewPart(string name)
        {
            var type = typeof(Part).Assembly.GetType("CavesOfOoo.Core." + name);
            Assert.NotNull(type, name + " must exist before local access can be learned or witnessed.");
            return (Part)Activator.CreateInstance(type);
        }
        protected static object Invoke(object instance, string method, params object[] args)
        {
            var found = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).SingleOrDefault(m => m.Name == method && m.GetParameters().Length == args.Length);
            Assert.NotNull(found, instance.GetType().Name + "." + method + " is missing");
            return found.Invoke(instance, args);
        }
        protected Entity Person(string name, Zone zone, int x, int y, bool player = false)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = name };
            e.SetTag("Creature"); if (player) e.SetTag("Player");
            e.AddPart(new PhysicsPart()); e.AddPart(new RenderPart { DisplayName = name, Visible = true });
            e.AddPart(new InventoryPart { MaxWeight = 500 }); e.AddPart(new BrainPart { Passive = true, SightRadius = 10 });
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 30, Max = 30 };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 20, Max = 100 };
            Assert.True(zone.AddEntity(e, x, y)); return e;
        }
        protected Entity Soil(Zone zone, int x, int y)
        {
            var e = Factory.CreateEntity("Grass"); e.AddPart(new CultivatedSoilPart());
            Assert.True(zone.AddEntity(e, x, y)); return e;
        }
        protected Entity Crop(int x = 4, int y = 5)
        {
            var e = Factory.CreateEntity("MarlrootCrop"); e.GetPart<CropPart>().GrowthStage = 2;
            Assert.True(Reserve.AddEntity(e, x, y)); return e;
        }
        protected Entity Give(string blueprint, int count = 1)
        {
            Entity first = null;
            for (int i = 0; i < count; i++) { var e = Factory.CreateEntity(blueprint); Assert.True(Player.GetPart<InventoryPart>().AddObject(e)); first ??= e; }
            return first;
        }
        protected int CarriedCount(string blueprint) => Player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        protected string State(Entity actor = null) => Invoke(Claim, "GetState", actor ?? Player).ToString();
        protected bool Act(string action) => (bool)Invoke(Claim, "TryAct", Player, Reserve, action);
        protected bool Introduce() => (bool)Invoke(Introduction, "TryAct", Player, Kitchen, "introduction");
        protected void MovePlayer(Zone destination)
        {
            Reserve.RemoveEntity(Player); Kitchen.RemoveEntity(Player);
            Assert.True(destination.AddEntity(Player, 5, 6)); Manager.SetActiveZone(destination); SettlementRuntime.ActiveZone = destination;
        }
        protected InventoryCommandResult Harvest(Entity crop) => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(crop, "HarvestCultivatedCrop"), Player, Reserve);
        protected void RepairPan()
        {
            MovePlayer(Kitchen); Give("FireClay", 2); Assert.True(Pan.GetPart<RepairablePart>().TryRepair(Player, Kitchen));
        }
    }

    public sealed class ConnectedReserveTests : ConnectedReserveTestBase
    {
        [Test] public void PaidPermissionCostsExactlyEightDramsOnce_AndLeavesPublicGroundPublic()
        {
            Assert.AreEqual("Unknown", State()); Assert.True(Act("pay"));
            Assert.AreEqual("Granted", State()); Assert.AreEqual(32, TradeSystem.GetDrams(Player)); Assert.AreEqual(18, TradeSystem.GetDrams(Keeper));
            Assert.False(Act("pay")); Assert.AreEqual(32, TradeSystem.GetDrams(Player));
            Assert.True(Harvest(Crop(5, 7)).Success); Assert.AreEqual("Granted", State());
        }
        [Test] public void InsufficientMoneyDoesNotGrantPermissionOrCreditKeeper()
        { TradeSystem.SetDrams(Player, 7); Assert.False(Act("pay")); Assert.AreEqual("Unknown", State()); Assert.AreEqual(7, TradeSystem.GetDrams(Player)); Assert.AreEqual(10, TradeSystem.GetDrams(Keeper)); }
        [Test] public void WitnessedUnauthorizedHarvestSucceedsButSuspendsOnlyThisReserve()
        { Assert.True(Harvest(Crop()).Success); Assert.AreEqual("Suspended", State()); Assert.False(Keeper.GetPart<BrainPart>().IsPersonallyHostileTo(Player)); Assert.AreEqual(0, CarriedCount("MarlrootClod")); }
        [Test] public void GrantedHarvestDoesNotCreateABreach()
        { Assert.True(Act("pay")); Assert.True(Harvest(Crop()).Success); Assert.AreEqual("Granted", State()); }
        [Test] public void HarvestBeyondKeeperSightLeavesUnknownCulprit()
        { Reserve.MoveEntity(Keeper, 25, 5); Assert.True(Harvest(Crop()).Success); Assert.AreEqual("Unknown", State()); }
        [Test] public void ActualOpaqueCoverPreventsWitnessEvenWhenPlayerSeesKeeper()
        {
            Reserve.MoveEntity(Keeper, 8, 5); var wall = Factory.CreateEntity("StoneWall"); Assert.True(Reserve.AddEntity(wall, 6, 5));
            Reserve.GetCell(8, 5).IsVisible = true;
            Assert.True(Harvest(Crop()).Success); Assert.AreEqual("Unknown", State());
        }
        [Test] public void CalmDoesNotBlindWitnessOrGrantPermission()
        { Keeper.GetPart<BrainPart>().PushGoal(new NoFightGoal(10, wander: false)); Assert.True(Harvest(Crop()).Success); Assert.AreEqual("Suspended", State()); }
        [Test] public void TwoGrainRestitutionRestoresCurrentBreachOnce()
        {
            Assert.True(Harvest(Crop()).Success); Give("Emberwheat", 3);
            Assert.True(Act("reconcile")); Assert.AreEqual("Granted", State()); Assert.AreEqual(1, CarriedCount("Emberwheat"));
            Assert.False(Act("reconcile")); Assert.AreEqual(1, CarriedCount("Emberwheat"));
        }
        [Test] public void StolenReserveTrayGoodsSuspendAccessAfterActualTransfer()
        {
            var item = Factory.CreateEntity("FireClay"); Assert.True(Tray.GetPart<ContainerPart>().AddItem(item));
            Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(Tray, item), Player, Reserve).Success);
            Assert.AreEqual("Suspended", State()); Assert.AreEqual(1, CarriedCount("FireClay"));
        }
        [Test] public void FutureCropOnSameBoundBedRemainsClaimed()
        {
            var first = Crop(); Reserve.RemoveEntity(first); var future = Crop();
            Assert.True(Harvest(future).Success); Assert.AreEqual("Suspended", State());
        }
        [Test] public void RepairAloneDoesNotTellKeeper_CookAcknowledgesThenPlayerRelays()
        {
            RepairPan(); Assert.AreEqual("Unknown", State()); Assert.True(Introduce()); Assert.AreEqual("Unknown", State());
            MovePlayer(Reserve); Assert.True(Act("introduction")); Assert.AreEqual("Granted", State()); Assert.AreEqual(40, TradeSystem.GetDrams(Player));
        }
        [Test] public void MereRepairedFlagWithoutCommittedCauseDoesNotEarnIntroduction()
        { MovePlayer(Kitchen); Pan.GetPart<RepairablePart>().Repaired = true; Assert.False(Introduce()); }
    }

    public sealed class ConnectedReserveThrowAfterAction : Part
    {
        public override bool HandleEvent(GameEvent e)
        { if (e.ID == "AfterInventoryAction") throw new InvalidOperationException("reserve rollback counter"); return true; }
    }
}
