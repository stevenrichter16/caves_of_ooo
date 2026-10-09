using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ItemUtilityEquipmentAdversarialTests : FiftyWorldFixture
    {
        [SetUp] public void RevealEquipmentWorld() { foreach(var cell in Zone.Cells) cell.IsVisible=cell.Explored=true; }
        Entity Wear(string blueprint) { var item = Carry(blueprint); Assert.True(InventorySystem.Equip(Actor, item)); return item; }
        string At(Entity item, string prefix, int x = 11, int y = 10) => Choice(item, prefix,
            a => a.Command.Split('|')[4] == x.ToString() && a.Command.Split('|')[5] == y.ToString());
        bool Braced => Actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Any(e => e.DisplayName == "braced" && e.Duration > 0) == true;
        Entity Gas(int density = 12)
        {
            var gas = new Entity { ID = "equipment-adversarial-gas", BlueprintName = "GasPoison" };
            gas.AddPart(new PhysicsPart()); gas.AddPart(new RenderPart()); gas.SetTag("Gas");
            gas.AddPart(new GasPoolPart { GasId = "poison-vapor", Density = density }); Assert.True(Zone.AddEntity(gas, 11, 10)); return gas;
        }
        sealed class Callback : Part
        {
            public string EventID = "AfterInventoryAction"; public Action Action; public bool Refuse;
            public override bool HandleEvent(GameEvent e) { if(e.ID!=EventID)return true; Action?.Invoke(); return !Refuse; }
        }
        [Test] public void ABraceVetoRefusesTheActionRatherThanClaimingProtection()
        {
            Bed(10,10);var boots=Wear("IronshodBoots");string command=Choice(boots,"BraceBoots|");
            Actor.AddPart(new Callback { EventID="BeforeApplyEffect",Refuse=true });
            Assert.False(Act(boots,command));Assert.False(Braced);
        }
        [Test] public void StunAfterBracingDoesNotRemoveThePhysicalPosture()
        {
            Bed(10,10);var boots=Wear("IronshodBoots");var pusher=Place("MarlbackScrabbler",9,10);
            Assert.True(Act(boots,Choice(boots,"BraceBoots|")));Actor.ApplyEffect(new StunnedEffect(2));
            Assert.False(SkillCombatHelpers.TryPush(pusher,Actor,Zone));Assert.False(Braced);
        }
        public sealed class QuartzCreationMove : Part
        {
            public static Action Callback;
            public override bool HandleEvent(GameEvent e) { if(e.ID=="ObjectCreated")Callback?.Invoke();return true; }
        }
        [Test] public void MovementDuringQuartzCreationRejectsOldDestination()
        {
            var quartz=Carry("GlowQuartz");string command=At(quartz,"CrackQuartz|");bool invoked=false;
            Factory.RegisterPartType<QuartzCreationMove>();
            Factory.Blueprints["CrackedGlowQuartz"].Parts["QuartzCreationMove"]=new System.Collections.Generic.Dictionary<string,string>();
            QuartzCreationMove.Callback=()=> { invoked=true;Zone.MoveEntity(Actor,8,10); };
            try { Assert.False(Act(quartz,command));Assert.True(invoked,"Actual ObjectCreated callback must run.");Assert.Contains(quartz,Pack.Objects);
                Assert.False(Zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="CrackedGlowQuartz")); }
            finally { QuartzCreationMove.Callback=null; }
        }
        [Test] public void GasChangedByOuterCallbackCannotBeFannedForFree()
        {
            var fan=Wear("LampveinFan");var gas=Gas();string command=At(fan,"FanGas|");
            Actor.AddPart(new Callback { Action=()=>gas.GetPart<GasPoolPart>().Density=17 });
            Assert.False(Act(fan,command));Assert.AreEqual(17,gas.GetPart<GasPoolPart>().Density);
        }
        [TestCase(false)] [TestCase(true)]
        public void DestroyedWallCannotBeUsedAsAFixedHandhold(bool gone)
        {
            var grip=Wear("GripfrondWrap");var wall=Place("StoneWall");string command=At(grip,"BraceGrip|");
            if(gone)wall.GetPart<DestructiblePart>().Gone=true;else wall.GetPart<DestructiblePart>().HP=0;
            Assert.False(Actions(grip).Any(a=>a.Command.StartsWith("BraceGrip|")));Assert.False(Act(grip,command));
        }
        [TestCase("Torch","TouchTorch|","Bush")] [TestCase("LampveinFan","FanGas|","gas")] [TestCase("GripfrondWrap","BraceGrip|","StoneWall")]
        public void UnseenTargetsCannotBeSelectedOrUsedThroughAnOldMenu(string blueprint,string prefix,string targetBlueprint)
        {
            var item=Wear(blueprint);var target=targetBlueprint=="gas"?Gas():Place(targetBlueprint);
            string command=At(item,prefix);Zone.GetCell(11,10).IsVisible=false;
            Assert.False(Actions(item).Any(a=>a.Command.StartsWith(prefix)));Assert.False(Act(item,command));
            Zone.GetCell(11,10).IsVisible=true;target.GetPart<RenderPart>().Visible=false;
            Assert.False(Actions(item).Any(a=>a.Command.StartsWith(prefix)));
        }
        [TestCase("GlowQuartz","CrackQuartz|")] [TestCase("GroundwireScreen","GroundCharge|")]
        public void UnseenGroundCannotBeSelectedOrUsedThroughAnOldMenu(string blueprint,string prefix)
        {
            var item=blueprint=="GlowQuartz"?Carry(blueprint):Wear(blueprint);
            if(blueprint=="GroundwireScreen"){Actor.ApplyEffect(new ElectrifiedEffect(1));Actor.RemoveEffect<StunnedEffect>();}
            string command=At(item,prefix);Zone.GetCell(11,10).IsVisible=false;
            Assert.False(Actions(item).Any(a=>a.Command==command));Assert.False(Act(item,command));
        }
        [Test] public void HookedTurnAbsorbedByEarlierBraceTicksTheHookOnlyOnce()
        {
            Bed(10,10);var boots=Wear("IronshodBoots");var hooker=Place("MarlbackScrabbler",14,10);
            Assert.True(Act(boots,Choice(boots,"BraceBoots|")));
            var hook=new HookedEffect(9,hooker,100,new Random(1));Actor.ApplyEffect(hook);
            var end=GameEvent.New("EndTurn");end.SetParameter("Zone",Zone);Actor.FireEventAndRelease(end);
            Assert.AreEqual(8,hook.Duration,"Removing an earlier stance during reverse effect iteration must not tick Hooked twice.");
            Assert.AreEqual((10,10),Zone.GetEntityPosition(Actor));Assert.False(Braced);
        }
        [TestCase("Torch", "TouchTorch|")] [TestCase("LampveinFan", "FanGas|")]
        [TestCase("GroundwireScreen", "GroundCharge|")] [TestCase("GripfrondWrap", "BraceGrip|")]
        [TestCase("IronshodBoots", "BraceBoots|")]
        public void MerelyCarryingEquipmentDoesNotGrantItsActiveVerb(string blueprint, string prefix)
        {
            Bed(10, 10); Place("StoneWall", 10, 9); Gas(); Zone.TileState.WriteCoating(11, 10, "oil", 8);
            Actor.ApplyEffect(new ElectrifiedEffect(1)); Actor.RemoveEffect<StunnedEffect>();
            Assert.False(Actions(Carry(blueprint)).Any(a => a.Command.StartsWith(prefix)));
        }
        [TestCase("moved")] [TestCase("unequipped")] [TestCase("unlit")]
        public void StaleTorchSelectionPreservesFuelAndTarget(string fault)
        {
            var torch = Wear("Torch"); var bush = Place("Bush"); string command = At(torch, "TouchTorch|"); float fuel = torch.GetPart<FuelPart>().FuelMass;
            if (fault == "moved") Assert.True(Zone.MoveEntity(Actor, 9, 10));
            if (fault == "unequipped") Assert.True(InventorySystem.UnequipItem(Actor, torch));
            if (fault == "unlit") torch.GetPart<LightSourcePart>().Enabled = false;
            Assert.False(Act(torch, command)); Assert.AreEqual(fuel, torch.GetPart<FuelPart>().FuelMass); Assert.IsNull(bush.GetEffect<BurningEffect>());
        }
        [Test] public void FailedTorchActionDoesNotPublishFireOrLoseFuel()
        {
            var torch = Wear("Torch"); var bush = Place("Bush"); string command = At(torch, "TouchTorch|"); float fuel = torch.GetPart<FuelPart>().FuelMass; FailAfter();
            Assert.False(Act(torch, command)); Assert.AreEqual(fuel, torch.GetPart<FuelPart>().FuelMass); Assert.IsNull(bush.GetEffect<BurningEffect>());
        }
        [TestCase(12)] [TestCase(3)]
        public void FailedFanRestoresExactGasDensityIdentityAndPlacement(int amount)
        {
            var fan = Wear("LampveinFan"); var gas = Gas(amount); var pool = gas.GetPart<GasPoolPart>(); string command = At(fan, "FanGas|"); FailAfter();
            Assert.False(Act(fan, command)); Assert.AreEqual(amount, pool.Density); Assert.AreEqual((11, 10), Zone.GetEntityPosition(gas));
            Assert.AreSame(pool, gas.GetPart<GasPoolPart>());
        }
        [TestCase("stable")] [TestCase("moved")] [TestCase("density")]
        public void AChangedGasSelectionCannotFanUnselectedState(string fault)
        {
            var fan = Wear("LampveinFan"); var gas = Gas(); var pool = gas.GetPart<GasPoolPart>(); string command = At(fan, "FanGas|");
            if (fault == "stable") pool.Stable = true;
            if (fault == "moved") Assert.True(Zone.MoveEntity(gas, 12, 10));
            if (fault == "density") pool.Density = 13;
            Assert.False(Act(fan, command)); Assert.AreEqual(fault == "density" ? 13 : 12, pool.Density);
        }
        [Test] public void FailedGroundingRestoresChargeWithoutWritingFloorEnergy()
        {
            var screen = Wear("GroundwireScreen"); var charge = new ElectrifiedEffect(1); Actor.ApplyEffect(charge); Actor.RemoveEffect<StunnedEffect>();
            string command = At(screen, "GroundCharge|"); FailAfter(); Assert.False(Act(screen, command));
            Assert.AreSame(charge, Actor.GetEffect<ElectrifiedEffect>()); Assert.IsNull(Zone.TileState.Get(11, 10));
        }
        [Test] public void FullGroundChargeAndUnchargedHolderCannotWasteTheGroundingAction()
        {
            var screen = Wear("GroundwireScreen"); Assert.False(Actions(screen).Any(a => a.Command.StartsWith("GroundCharge|")));
            Actor.ApplyEffect(new ElectrifiedEffect(2)); Actor.RemoveEffect<StunnedEffect>(); Zone.TileState.AddCharge(11, 10, 2);
            Assert.False(Actions(screen).Any(a => a.Command.StartsWith("GroundCharge|") && a.Command.Split('|')[4] == "11" && a.Command.Split('|')[5] == "10"));
        }
        [TestCase("GripfrondWrap", "BraceGrip|")] [TestCase("IronshodBoots", "BraceBoots|")]
        public void BraceWorksForAPullAndDoesNotStackRepeatedReadying(string blueprint, string prefix)
        {
            Bed(10, 10); Place("StoneWall", 10, 9); var equipment = Wear(blueprint); var puller = Place("MarlbackScrabbler", 14, 10);
            Assert.True(Act(equipment, Choice(equipment, prefix))); Assert.False(Actions(equipment).Any(a => a.Command.StartsWith(prefix)));
            Assert.False(SkillCombatHelpers.TryPull(puller, Actor, Zone, 2)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Actor)); Assert.False(Braced);
        }
        [Test] public void WallGoneAfterReadyingMakesGripUnableToResist()
        {
            var wall = Place("StoneWall", 10, 9); var grip = Wear("GripfrondWrap"); var pusher = Place("MarlbackScrabbler", 9, 10);
            Assert.True(Act(grip, Choice(grip, "BraceGrip|"))); Zone.RemoveEntity(wall);
            Assert.True(SkillCombatHelpers.TryPush(pusher, Actor, Zone)); Assert.AreEqual((11, 10), Zone.GetEntityPosition(Actor));
        }
        [Test] public void SlipperyFloorAfterReadyingMakesBootsUnableToResist()
        {
            Bed(10, 10); var boots = Wear("IronshodBoots"); var pusher = Place("MarlbackScrabbler", 9, 10);
            Assert.True(Act(boots, Choice(boots, "BraceBoots|"))); Zone.TileState.WriteCoating(10, 10, "ice", 5);
            Assert.True(SkillCombatHelpers.TryPush(pusher, Actor, Zone)); Assert.AreEqual((11, 10), Zone.GetEntityPosition(Actor));
        }
        [Test] public void APhysicallyBlockedPushDoesNotConsumeBrace()
        {
            Bed(10, 10); var boots = Wear("IronshodBoots"); var pusher = Place("MarlbackScrabbler", 9, 10); Place("StoneWall", 11, 10);
            Assert.True(Act(boots, Choice(boots, "BraceBoots|"))); Assert.False(SkillCombatHelpers.TryPush(pusher, Actor, Zone)); Assert.True(Braced);
        }
        [Test] public void VoluntaryMoveAwayAndBackCannotRecoverTheCancelledStance()
        {
            Bed(10, 10); var boots = Wear("IronshodBoots"); Assert.True(Act(boots, Choice(boots, "BraceBoots|")));
            Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0)); Assert.True(MovementSystem.TryMove(Actor, Zone, -1, 0)); Assert.False(Braced);
        }
        [Test] public void UnequippingBeforeImpactInvalidatesTheStance()
        {
            Bed(10, 10); var boots = Wear("IronshodBoots"); var pusher = Place("MarlbackScrabbler", 9, 10);
            Assert.True(Act(boots, Choice(boots, "BraceBoots|"))); Assert.True(InventorySystem.UnequipItem(Actor, boots));
            Assert.True(SkillCombatHelpers.TryPush(pusher, Actor, Zone));
        }
        [Test] public void ThreeOwnerEndsExpireBraceAndSaveLoadPreservesEquipmentBinding()
        {
            Bed(10, 10); var boots = Wear("IronshodBoots"); Assert.True(Act(boots, Choice(boots, "BraceBoots|")));
            var saved = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor); Zone.RemoveEntity(Actor); Actor = saved; Assert.True(Zone.AddEntity(Actor, 10, 10));
            Assert.True(Braced); for (int i = 0; i < 3; i++) { var end = GameEvent.New("EndTurn"); end.SetParameter("Zone", Zone); Actor.FireEventAndRelease(end); }
            Assert.False(Braced);
        }
        [Test] public void OuterFailureCannotGrantAnUnpaidBrace()
        {
            Bed(10, 10); var boots = Wear("IronshodBoots"); string command = Choice(boots, "BraceBoots|"); FailAfter();
            Assert.False(Act(boots, command)); Assert.False(Braced);
        }
    }
}
