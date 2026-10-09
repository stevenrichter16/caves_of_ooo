using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Native equipment actions and actual physical movement/light consumers.</summary>
    public sealed class ItemUtilityEquipmentTests : FiftyWorldFixture
    {
        System.Collections.Generic.List<TileReactionSystem.TileReaction> oldReactions;
        bool oldReactionsInitialized;
        static readonly System.Reflection.FieldInfo ReactionList = typeof(TileReactionSystem).GetField("_reactions",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        static readonly System.Reflection.FieldInfo ReactionInitialized = typeof(TileReactionSystem).GetField("<IsInitialized>k__BackingField",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        [SetUp] public void EquipmentWorld()
        {
            oldReactions=new System.Collections.Generic.List<TileReactionSystem.TileReaction>((System.Collections.Generic.List<TileReactionSystem.TileReaction>)ReactionList.GetValue(null));
            oldReactionsInitialized=TileReactionSystem.IsInitialized;
            TileReactionSystem.Initialize(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data/TileReactions/Reactions.json")));
            foreach(var cell in Zone.Cells)cell.IsVisible=cell.Explored=true;
        }
        [TearDown] public void RestoreEquipmentWorld()
        {
            TileReactionSystem.ResetForTests();((System.Collections.Generic.List<TileReactionSystem.TileReaction>)ReactionList.GetValue(null)).AddRange(oldReactions);
            ReactionInitialized.SetValue(null,oldReactionsInitialized);
        }
        Entity Wear(string blueprint)
        { var item = Carry(blueprint); Assert.True(InventorySystem.Equip(Actor, item), blueprint); return item; }
        string At(Entity item, string prefix, int x = 11, int y = 10) => Choice(item, prefix,
            a => a.Command.Split('|')[4] == x.ToString() && a.Command.Split('|')[5] == y.ToString());
        bool Braced => Actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Any(e => e.DisplayName == "braced" && e.Duration > 0) == true;
        Entity Gas(int density = 12, bool stable = false)
        {
            var gas = new Entity { ID = "equipment-test-gas", BlueprintName = "GasPoison" };
            gas.AddPart(new PhysicsPart()); gas.AddPart(new RenderPart()); gas.SetTag("Gas");
            gas.AddPart(new GasPoolPart { GasId = "poison-vapor", Density = density, Stable = stable });
            Assert.True(Zone.AddEntity(gas, 11, 10)); return gas;
        }

        [Test] public void HeldLitTorchPaysRealFuelToIgniteOnlyChosenOilFilm()
        {
            var torch = Wear("Torch"); Zone.TileState.WriteCoating(11, 10, "oil", 9);
            float before = torch.GetPart<FuelPart>().FuelMass; Assert.True(Act(torch, At(torch, "TouchTorch|")));
            Assert.AreEqual(before - 1, torch.GetPart<FuelPart>().FuelMass);
            Assert.True((Zone.TileState.Get(11, 10)?.Heat ?? 0) > 0 || Zone.TileState.HasResidue(11, 10, "embers"), "The real ignition impulse may already have resolved into embers.");
            Assert.IsNull(Zone.TileState.Get(12, 10));
        }

        [Test] public void TorchCanIgniteActualCombustibleScenery()
        {
            var torch = Wear("Torch"); var bush = Place("Bush");
            Assert.True(Act(torch, At(torch, "TouchTorch|"))); Assert.NotNull(bush.GetEffect<BurningEffect>());
            Assert.False(Actor.HasEffect<BurningEffect>(), "Ignition does not invent a self-burn; later spread remains real.");
        }

        [TestCase("stowed")] [TestCase("unlit")] [TestCase("wet")] [TestCase("empty")]
        public void TorchMustBeActuallyHeldLitDryAndFueled(string fault)
        {
            var torch = Wear("Torch"); Zone.TileState.WriteCoating(11, 10, "oil", 9);
            if (fault == "stowed") Assert.True(InventorySystem.UnequipItem(Actor, torch));
            if (fault == "unlit") torch.GetPart<LightSourcePart>().Enabled = false;
            if (fault == "wet") torch.ApplyEffect(new WetEffect(1));
            if (fault == "empty") torch.GetPart<FuelPart>().FuelMass = 0;
            Assert.False(Actions(torch).Any(a => a.Command.StartsWith("TouchTorch|")));
        }

        [Test] public void FanDispersesFiveDensityWithoutChangingTheGasPayload()
        {
            var fan = Wear("LampveinFan"); var gas = Gas(); var pool = gas.GetPart<GasPoolPart>();
            Assert.True(Act(fan, At(fan, "FanGas|"))); Assert.AreEqual(7, pool.Density);
            Assert.AreEqual("poison-vapor", pool.GasId); Assert.AreEqual(1, pool.Level); Assert.NotNull(Zone.GetEntityCell(gas));
        }

        [Test] public void FanCanClearATinyTransientCloudButCannotMoveAStationaryVeil()
        {
            var fan = Wear("LampveinFan"); var gas = Gas(3); Zone.TileState.WriteCloud(11, 10, "veil-mist", 4);
            Assert.True(Act(fan, At(fan, "FanGas|"))); Assert.IsNull(Zone.GetEntityCell(gas));
            Assert.True(Zone.TileState.ObscuresSight(11, 10));
        }

        [TestCase(true)] [TestCase(false)]
        public void StableGasOrStowedFanOffersNoDispersal(bool stable)
        {
            var fan = Wear("LampveinFan"); Gas(stable: stable);
            if (!stable) Assert.True(InventorySystem.UnequipItem(Actor, fan));
            Assert.False(Actions(fan).Any(a => a.Command.StartsWith("FanGas|")));
        }

        [Test] public void HeldScreenMovesAnExistingChargeToChosenGround()
        {
            var screen = Wear("GroundwireScreen"); Actor.ApplyEffect(new ElectrifiedEffect(1)); Actor.RemoveEffect<StunnedEffect>();
            Assert.True(Act(screen, At(screen, "GroundCharge|"))); Assert.IsNull(Actor.GetEffect<ElectrifiedEffect>());
            Assert.AreEqual(1, Zone.TileState.Get(11, 10).Charge); Assert.IsNull(Zone.TileState.Get(10, 10));
        }

        sealed class LowestSave : Random { public override int Next(int minValue,int maxValue)=>minValue; }
        [TestCase(false)] [TestCase(true)]
        public void WetChargeHasAnOrdinaryActionWindowAfterContactStunExpires(bool wet)
        {
            var screen=Wear("GroundwireScreen");if(wet)Actor.ApplyEffect(new WetEffect(1));
            Actor.ApplyEffect(new ElectrifiedEffect(1));Actor.GetEffect<StunnedEffect>().Rng=new LowestSave();
            for(int i=0;i<2;i++){var end=GameEvent.New("EndTurn");end.SetParameter("Zone",Zone);Actor.FireEventAndRelease(end);}
            Assert.IsNull(Actor.GetEffect<StunnedEffect>(),"Native duration recovery, not manual removal.");
            Assert.AreEqual(wet,Actions(screen).Any(a=>a.Command.StartsWith("GroundCharge|")));
            if(wet){Assert.AreEqual(1,Actor.GetEffect<ElectrifiedEffect>().Duration);Assert.True(Act(screen,At(screen,"GroundCharge|")));Assert.IsNull(Actor.GetEffect<ElectrifiedEffect>());}
            else Assert.IsNull(Actor.GetEffect<ElectrifiedEffect>(),"Dry charge expires with a failed-save stun; the screen invents no new action window.");
        }
        [Test] public void GroundwireCannotBypassTheStunAppliedWithElectricity()
        {
            var screen = Wear("GroundwireScreen"); Actor.ApplyEffect(new ElectrifiedEffect(1));
            Assert.NotNull(Actor.GetEffect<StunnedEffect>());
            Assert.False(Actions(screen).Any(a => a.Command.StartsWith("GroundCharge|")));
            Assert.NotNull(Actor.GetEffect<ElectrifiedEffect>());
        }

        [TestCase("GripfrondWrap", "BraceGrip|")] [TestCase("IronshodBoots", "BraceBoots|")]
        public void WornEquipmentBraceStopsOnePhysicalShoveThenAllowsTheNext(string blueprint, string prefix)
        {
            Bed(10, 10); var wall = Place("StoneWall", 10, 9); var equipment = Wear(blueprint); var pusher = Place("MarlbackScrabbler", 9, 10);
            Assert.True(Act(equipment, Choice(equipment, prefix))); Assert.True(Braced);
            Assert.False(SkillCombatHelpers.TryPush(pusher, Actor, Zone, 2)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Actor)); Assert.False(Braced);
            Assert.True(SkillCombatHelpers.TryPush(pusher, Actor, Zone, 1)); Assert.AreEqual((11, 10), Zone.GetEntityPosition(Actor));
        }

        [TestCase("GripfrondWrap", "BraceGrip|")] [TestCase("IronshodBoots", "BraceBoots|")]
        public void BraceDoesNotBlockGenericForcedRelocationAndMovementReleasesIt(string blueprint, string prefix)
        {
            Bed(10, 10); Place("StoneWall", 10, 9); var equipment = Wear(blueprint);
            Assert.True(Act(equipment, Choice(equipment, prefix))); Assert.True(MovementSystem.ForceMoveTo(Actor, Zone, 11, 10)); Assert.False(Braced);
        }

        [Test] public void GripNeedsAnIntactWallAndBootsNeedStableTerrain()
        {
            var grip = Wear("GripfrondWrap"); Assert.False(Actions(grip).Any(a => a.Command.StartsWith("BraceGrip|")));
            var boots = Wear("IronshodBoots"); Bed(10, 10); Zone.TileState.WriteCoating(10, 10, "oil", 6);
            Assert.False(Actions(boots).Any(a => a.Command.StartsWith("BraceBoots|")));
        }

        [Test] public void GlowQuartzBecomesFiniteSavedActualGroundLight()
        {
            Zone.AmbientLevel = 0; var baselineLight = new LightMap(); baselineLight.Compute(Zone); float baseline=baselineLight.GetBrightness(11,10);
            var quartz = Carry("GlowQuartz"); Assert.True(Act(quartz, At(quartz, "CrackQuartz|")));
            Assert.False(Pack.Objects.Contains(quartz)); var glow = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CrackedGlowQuartz");
            Assert.False(glow.GetPart<PhysicsPart>().Takeable); Assert.AreEqual(30, glow.GetPart<LifespanPart>().TurnsRemaining);
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(glow); Zone.RemoveEntity(glow); Assert.True(Zone.AddEntity(copy, 11, 10));
            var light = new LightMap(); light.Compute(Zone); Assert.Greater(light.GetBrightness(11, 10), 0.5f);
            for (int i = 0; i < 30; i++) MaterialSimSystem.TickMaterialEntities(Zone);
            Assert.IsNull(Zone.GetEntityCell(copy)); light.Compute(Zone); Assert.AreEqual(baseline, light.GetBrightness(11, 10), .0001f, "The player retains its own native light after the ground light expires.");
        }

        [Test] public void FailedOuterActionRefundsQuartzAndLeavesNoLight()
        {
            var quartz = Carry("GlowQuartz"); string command = At(quartz, "CrackQuartz|"); FailAfter();
            Assert.False(Act(quartz, command)); Assert.Contains(quartz, Pack.Objects);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "CrackedGlowQuartz"));
        }
    }
}
