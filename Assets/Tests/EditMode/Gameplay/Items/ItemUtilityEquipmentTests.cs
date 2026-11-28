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

        [TestCase(5, 0, "clear this cloud", "You fan this cloud away.")]
        [TestCase(6, 1, "thin this cloud by 5 (1 remains)", "You thin this cloud by 5. Density remaining: 1.")]
        [TestCase(12, 7, "thin this cloud by 5 (7 remains)", "You thin this cloud by 5. Density remaining: 7.")]
        public void FanPackAndWorldMenusPreviewWhatTheSameActionActuallyDoes(int density, int remaining, string label, string receipt)
        {
            var fan = Wear("LampveinFan");
            var gas = Gas(density);
            var pool = gas.GetPart<GasPoolPart>();
            var cell = Zone.GetEntityCell(gas);
            var packRow = Actions(fan).Single(row => row.Name == "FanGas");
            var worldRow = CellMenu(11, 10).Single(row => row.Name == "FanGas");

            StringAssert.StartsWith(label, packRow.Display);
            StringAssert.Contains(" - ", packRow.Display);
            StringAssert.StartsWith(packRow.Display, worldRow.Display);
            Assert.AreEqual("Gas can change again as the turn advances.", packRow.Hint);
            Assert.AreEqual(packRow.Hint, worldRow.Hint);
            Assert.AreEqual(packRow.Command, worldRow.Command);
            Assert.AreSame(fan, worldRow.Via);
            Assert.AreEqual(ActionTime.OneAction, worldRow.Time);
            Assert.AreEqual(density, pool.Density);
            Assert.AreSame(cell, Zone.GetEntityCell(gas));

            Assert.True(ChooseRow(worldRow, 11, 10));
            Assert.AreEqual(remaining, pool.Density);
            Assert.AreSame(remaining == 0 ? null : cell, Zone.GetEntityCell(gas));
            Assert.AreSame(Actor, fan.GetPart<PhysicsPart>().Equipped);
            StringAssert.Contains(receipt, string.Join("\n", MessageLog.GetRecent(4)));
        }

        [Test]
        public void FanThinsTheSameCloudEnoughForTheBellowsToMoveIt()
        {
            var fan = Wear("LampveinFan");
            var bellows = Carry("HandBellows");
            var gas = Gas(45);
            var pool = gas.GetPart<GasPoolPart>();
            var before = CellMenu(11, 10);
            Assert.True(before.Any(row => row.Via == bellows && row.IsDisabled && row.DisabledCode == "cloud-too-thick"));
            var thin = before.Single(row => row.Name == "FanGas" && row.Via == fan);
            StringAssert.StartsWith("thin this cloud by 5 (40 remains)", thin.Display);

            Assert.True(ChooseRow(thin, 11, 10));
            Assert.AreEqual(DirectedAirPart.MaximumDensity, pool.Density);
            Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(gas));
            var push = CellMenu(11, 10).Single(row => row.Via == bellows && !row.IsDisabled && row.Command.StartsWith("PushGas|"));
            Assert.True(ChooseRow(push, 11, 10));
            Assert.AreSame(Zone.GetCell(12, 10), Zone.GetEntityCell(gas));
            Assert.AreSame(pool, gas.GetPart<GasPoolPart>());
            Assert.AreEqual(DirectedAirPart.MaximumDensity, pool.Density);
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

        [TestCase(1, 0, 1)]
        [TestCase(2, 1, 2)]
        public void GroundwirePreviewMatchesBothMenusAndTheCappedWrite(int heldCharge, int priorTileCharge, int expectedTileCharge)
        {
            var screen = Wear("GroundwireScreen");
            var held = new ElectrifiedEffect(heldCharge);
            Actor.ApplyEffect(held);
            Actor.RemoveEffect<StunnedEffect>();
            Zone.TileState.AddCharge(11, 10, priorTileCharge);
            int before = Clock.TickCount;
            int? firstWrite = null;
            System.Action<int, int> observeWrite = (x, y) =>
            {
                if (x == 11 && y == 10 && firstWrite == null)
                    firstWrite = Zone.TileState.Charge(x, y);
            };

            Zone.TileState.OnCellChanged += observeWrite;
            try
            {
                var worldRow = CellMenu(11, 10).Single(row => row.Name == "GroundCharge");
                var packRow = Actions(screen).Single(row => row.Command == worldRow.Command);
                string hint = $"Clears held charge in 1 turn. Sends up to {heldCharge} here. "
                    + "Wet or metal can carry it.";

                Assert.AreSame(screen, worldRow.Via);
                Assert.AreEqual(ActionTime.OneAction, worldRow.Time);
                Assert.AreEqual(hint, worldRow.Hint);
                Assert.AreEqual(hint, packRow.Hint);
                Assert.AreEqual(hint, string.Join(" ", CavesOfOoo.Rendering.WorldActionMenuUI.WrapHint(hint, 44, 3)));
                Assert.AreEqual(before, Clock.TickCount);
                Assert.AreSame(held, Actor.GetEffect<ElectrifiedEffect>());
                Assert.AreEqual(priorTileCharge, Zone.TileState.Charge(11, 10));
                Assert.IsNull(firstWrite);

                Assert.IsTrue(ChooseRow(worldRow, 11, 10));
                Assert.AreEqual(expectedTileCharge, firstWrite);
                Assert.IsNull(Actor.GetEffect<ElectrifiedEffect>());
                Assert.AreSame(Actor, screen.GetPart<PhysicsPart>().Equipped);
            }
            finally
            {
                Zone.TileState.OnCellChanged -= observeWrite;
            }
        }

        [Test]
        public void UnchargedScreenOffersNoGroundingClueInEitherMenu()
        {
            var screen = Wear("GroundwireScreen");
            Zone.TileState.WriteCoating(11, 10, "water", 4);
            int before = Clock.TickCount;

            Assert.IsFalse(CellMenu(11, 10).Any(row => row.Name == "GroundCharge"
                || row.Hint?.Contains("Clears held charge") == true));
            Assert.IsFalse(Actions(screen).Any(row => row.Name == "GroundCharge"
                || row.Hint?.Contains("Clears held charge") == true));
            Assert.AreEqual(before, Clock.TickCount);
            Assert.IsNull(Actor.GetEffect<ElectrifiedEffect>());
            Assert.AreEqual(0, Zone.TileState.Charge(11, 10));
        }

        sealed class LowestSave : Random { public override int Next(int minValue,int maxValue)=>minValue; }
        [TestCase(false)] [TestCase(true)]
        public void WetChargeHasAnOrdinaryActionWindowAfterContactStunExpires(bool wet)
        {
            var screen=Wear("GroundwireScreen");if(wet)Actor.ApplyEffect(new WetEffect(1));
            Actor.ForceApplyEffect(new ElectrifiedEffect(1));Actor.GetEffect<StunnedEffect>().Rng=new LowestSave();   // forced: this pins the screen's action window, not the status rule, and the screen's own electric resistance would shorten the stun (Docs/MACHINE-PROOFING.md 2.1)
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

        [Test] public void TheThingsOwnMenuOffersTheHeldTorchToIt_RunByTheTorchInHand()
        {
            Assert.IsEmpty(CellMenu(11, 10).Where(a => a.Name == "TouchTorch"), "no torch held");
            var torch = Wear("Torch"); var bush = Place("Bush"); var rows = CellMenu(11, 10).Where(a => a.Name == "TouchTorch").ToArray(); Assert.AreEqual(1, rows.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display)));
            Assert.AreSame(torch, rows[0].Via, "the torch in hand does it"); Assert.AreEqual(At(torch, "TouchTorch|"), rows[0].Command, "the very command the pack row runs");
            Assert.True(ChooseRow(rows[0], 11, 10)); Assert.NotNull(bush.GetEffect<BurningEffect>());
        }

        [Test] public void ABareOilFilmIsOfferedNoTorchRowHere_TheLightItRowOfTheCellAlreadyCoversIt_Counter()
        {
            Wear("Torch"); Zone.TileState.WriteCoating(11, 10, "oil", 9); Assert.IsEmpty(CellMenu(11, 10).Where(a => a.Name == "TouchTorch"));
        }

        [Test] public void TheGasCellOffersTheFan_TheWallOffersTheGripWrap_AndTheQuartzOffersItselfOnTheCellItWouldLight()
        {
            var fan = Wear("LampveinFan"); Gas(); var gasRows = CellMenu(11, 10).Where(a => a.Name == "FanGas").ToArray(); Assert.AreEqual(1, gasRows.Length); Assert.AreSame(fan, gasRows[0].Via); Assert.AreEqual(At(fan, "FanGas|"), gasRows[0].Command);
            Bed(10, 10); Place("StoneWall", 10, 9); var wrap = Wear("GripfrondWrap"); var wallRows = CellMenu(10, 9).Where(a => a.Name == "BraceGrip").ToArray(); Assert.AreEqual(1, wallRows.Length, string.Join(" / ", CellMenu(10, 9).Select(a => a.Display)));
            Assert.AreSame(wrap, wallRows[0].Via, "the wrap on your hands does it"); Assert.AreEqual(Choice(wrap, "BraceGrip|"), wallRows[0].Command);
            var quartz = Carry("GlowQuartz"); var lit = CellMenu(10, 11).Where(a => a.Name == "CrackQuartz").ToArray(); Assert.AreEqual(1, lit.Length, string.Join(" / ", CellMenu(10, 11).Select(a => a.Display))); Assert.AreSame(quartz, lit[0].Via); Assert.AreEqual(At(quartz, "CrackQuartz|", 10, 11), lit[0].Command);
        }

        [Test] public void PlantingYourFeetIsAboutYouNotAPlace_SoNoCellOffersIt_Counter()
        {
            Bed(10, 10); var boots = Wear("IronshodBoots"); Assert.True(Actions(boots).Any(a => a.Name == "BraceBoots"), "fixture: the pack offers it");
            foreach (var (x, y) in new[] { (10, 10), (11, 10), (10, 11) }) Assert.IsEmpty(CellMenu(x, y).Where(a => a.Name == "BraceBoots"), "cell " + x + "," + y);
        }
    }
}
