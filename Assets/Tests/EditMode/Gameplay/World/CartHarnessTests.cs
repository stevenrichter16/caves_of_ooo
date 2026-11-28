using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CartHarnessTests : FiftyWorldFixture
    {
        const string Site = "Overworld.10.11.0";
        const string CartBlueprint = "OverturnedCart";
        const string Offcut = "CuredLeatherOffcut";
        const string CutLabel = "Cut one leather offcut";
        const int WorldSeed = 65, CartX = 62, CartY = 2, ApproachX = 61;

        OverworldZoneManager EnterCartSite()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, WorldSeed, true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(Site);
            Assert.NotNull(Zone);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, ApproachX, CartY));
            Zone.ForEachCell((cell, _, _) => { cell.Explored = true; cell.IsVisible = true; });
            return manager;
        }

        Entity Cart => Zone.GetCell(CartX, CartY).Objects.Single(owner => owner.BlueprintName == CartBlueprint);

        InventoryAction[] CutRows(int x = CartX, int y = CartY) => CellMenu(x, y)
            .Where(row => !row.IsDisabled && row.Display?.StartsWith(CutLabel, StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        int PackedOffcuts => CountPacked(Actor, Offcut);

        static int CountPacked(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects
            .Where(item => item.BlueprintName == blueprint).Sum(item => item.GetPart<StackerPart>()?.StackCount ?? 1);

        CartHarnessPart ReloadHarness(OverworldZoneManager manager, Entity cart, out Entity loadedPlayer)
        {
            var save = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("cart-harness", "test", manager, Clock, Actor));
            loadedPlayer = save.Player;
            var restored = save.ZoneManager.GetZone(Site).GetCell(CartX, CartY).Objects.Single(owner => owner.ID == cart.ID);
            return restored.GetPart<CartHarnessPart>();
        }

        [Test]
        public void FreshGenerationInstallsOneHarnessOnThePhysicalFallenCart()
        {
            var manager = EnterCartSite();
            var cart = Cart;
            var harness = cart.GetPart<CartHarnessPart>();
            Assert.NotNull(harness);
            Assert.False(harness.Cut);
            Assert.False(harness.Charred);
            Assert.That(cart.Parts.OfType<CartHarnessPart>().Count(), Is.EqualTo(1));
            Assert.That(Zone.GetReadOnlyEntities().Where(owner => owner.HasPart<CartHarnessPart>()), Is.EqualTo(new[] { cart }));
            Assert.That(WorldInteractionSystem.ResolveTarget(Zone.GetCell(CartX, CartY), Actor), Is.SameAs(cart));

            Assert.That(manager.GetZone(Site), Is.SameAs(Zone));
            Assert.That(Cart.GetPart<CartHarnessPart>(), Is.SameAs(harness));
            Assert.False(Factory.CreateEntity(CartBlueprint).HasPart<CartHarnessPart>());
        }

        [Test]
        public void VisibleLooseHarnessTintsTheSameNativeCartArtUntilCut()
        {
            EnterCartSite();
            using var catalog = new FlatPixelGreyboxCatalog();
            Assert.True(catalog.TryResolvePresentation(Zone, CartX, CartY, out var loose));
            Assert.True(loose.NativeForeground);
            Assert.True(loose.PreserveRenderColorCue);
            Assert.True(loose.PreserveAuthoredNativeColor);

            Cart.GetPart<CartHarnessPart>().Cut = true;
            Assert.True(catalog.TryResolvePresentation(Zone, CartX, CartY, out var cut));
            Assert.True(cut.NativeForeground);
            Assert.False(cut.PreserveAuthoredNativeColor);
            Assert.That(cut.ForegroundTile, Is.SameAs(loose.ForegroundTile));
            Assert.That(cut.ForegroundColor, Is.Not.EqualTo(loose.ForegroundColor));
        }

        [Test]
        public void OlderSavedCartDoesNotGainHarnessStockOnLoad()
        {
            var manager = EnterCartSite();
            var cart = Cart;
            Assert.True(cart.RemovePart(cart.GetPart<CartHarnessPart>()));

            Assert.Null(ReloadHarness(manager, cart, out _));
        }

        [Test]
        public void OnlySoundFlexibleCuttingToolsOfferTheRow()
        {
            EnterCartSite();
            Assert.That(CutRows(), Is.Empty);
            Carry("Hatchet");
            Assert.That(CutRows(), Is.Empty, "A wood-stock tool cannot cut the leather.");
            var dagger = Carry("Dagger");
            Assert.True(dagger.ApplyEffect(new BrokenEffect()));
            Assert.That(CutRows(), Is.Empty);
            Assert.True(dagger.RemoveEffect<BrokenEffect>());
            Assert.That(CutRows(), Has.Length.EqualTo(1));
            Assert.That(CutRows(ApproachX, CartY), Is.Empty);
        }

        [Test]
        public void AnEquippedCutterCanSalvageTheHarnessInRegionalRain()
        {
            var manager = EnterCartSite();
            var dagger = Carry("Dagger");
            Assert.True(InventorySystem.Equip(Actor, dagger));
            int rainWindow = RegionalWeather.NextRainWindow(WorldSeed, manager.WorldMap.GetBiome(10, 11), 0);
            Assert.That(rainWindow, Is.GreaterThanOrEqualTo(0));
            Clock.AdvanceClock(rainWindow * RegionalWeather.WindowTicks);
            Assert.True(RegionalWeather.Current(Zone, Clock.TickCount, RegionalWeather.PolicyVersion).Raining);

            Assert.True(ChooseRow(CutRows().Single(), CartX, CartY));
            Assert.True(InventorySystem.IsEquipped(Actor, dagger));
            Assert.That(PackedOffcuts, Is.EqualTo(1));
        }

        [Test]
        public void FullPackLeavesTheHarnessAndSuccessfulCutPaysOneUnitAcrossSave()
        {
            var manager = EnterCartSite();
            var cart = Cart;
            var harness = cart.GetPart<CartHarnessPart>();
            var dagger = Carry("Dagger");
            var quoted = CutRows().Single();
            Assert.That(quoted.Time, Is.EqualTo(ActionTime.OneAction));
            int capacity = Pack.MaxWeight;
            Pack.MaxWeight = Pack.GetCarriedWeight();

            Assert.False(ChooseRow(quoted, CartX, CartY));
            Assert.False(harness.Cut);
            Assert.That(Count(Offcut), Is.Zero);

            Pack.MaxWeight = capacity;
            var row = CutRows().Single();
            Assert.True(ChooseRow(row, CartX, CartY));
            Assert.True(harness.Cut);
            Assert.False(harness.Charred);
            Assert.That(PackedOffcuts, Is.EqualTo(1));
            Assert.That(Count(Offcut), Is.EqualTo(1));
            Assert.That(dagger.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(CutRows(), Is.Empty);
            Assert.False(ChooseRow(row, CartX, CartY));
            Assert.That(PackedOffcuts, Is.EqualTo(1));

            var restored = ReloadHarness(manager, cart, out var loadedPlayer);
            Assert.True(restored.Cut);
            Assert.False(restored.Charred);
            Assert.That(CountPacked(loadedPlayer, Offcut), Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AQuotedCutFailsAfterVisibilityOrReachIsLost(bool hideCart)
        {
            EnterCartSite();
            Carry("Dagger");
            var quoted = CutRows().Single();

            if (hideCart) Zone.GetCell(CartX, CartY).IsVisible = false;
            else Assert.True(Zone.MoveEntity(Actor, ApproachX - 1, CartY));

            Assert.False(ChooseRow(quoted, CartX, CartY));
            Assert.That(CutRows(), Is.Empty);
            Assert.That(Count(Offcut), Is.Zero);
            Assert.False(Cart.GetPart<CartHarnessPart>().Cut);
        }

        [Test]
        public void UnattachedFireEventDoesNotCharTheHarness()
        {
            EnterCartSite();
            var cart = Cart;
            Carry("Dagger");
            var signal = GameEvent.New("EffectApplied");
            signal.SetParameter("Target", (object)cart);
            signal.SetParameter("Effect", (object)new BurningEffect());
            signal.SetParameter("EffectType", nameof(BurningEffect));
            cart.FireEventAndRelease(signal);

            Assert.False(cart.HasEffect<BurningEffect>());
            Assert.False(cart.GetPart<CartHarnessPart>().Charred);
            Assert.That(CutRows(), Has.Length.EqualTo(1));
        }

        [Test]
        public void RealFireCharsTheHarnessAcrossDousingAndSaveWhileCookingStillWorks()
        {
            var manager = EnterCartSite();
            var cart = Cart;
            var harness = cart.GetPart<CartHarnessPart>();
            Carry("Dagger");
            var raw = Carry("LakePerch");
            var skin = Skin(1);
            var quoted = CutRows().Single();

            var burn = new BurningEffect(1f, Actor);
            Assert.True(cart.ApplyEffect(burn, Actor, Zone));
            Assert.That(cart.GetEffect<BurningEffect>(), Is.SameAs(burn));
            Assert.True(harness.Charred);
            Assert.That(CutRows(), Is.Empty);

            var cook = CellMenu(CartX, CartY).Single(row => row.Command?.StartsWith("CookAt|", StringComparison.Ordinal) == true);
            Assert.That(cook.Via, Is.SameAs(raw));
            Assert.True(ChooseRow(cook, CartX, CartY));
            Assert.That(CountPacked(Actor, "RoastedLakePerch"), Is.EqualTo(1));
            Assert.That(cart.GetEffect<BurningEffect>(), Is.SameAs(burn));

            var douse = CellMenu(CartX, CartY).Single(row => row.Command == "DouseWorld|" + Id(cart));
            Assert.That(douse.Via, Is.SameAs(skin));
            Assert.True(ChooseRow(douse, CartX, CartY));
            Assert.That(Units(skin), Is.Zero);
            Assert.False(cart.HasEffect<BurningEffect>());
            Assert.True(harness.Charred);
            Assert.False(harness.Cut);
            Assert.False(ChooseRow(quoted, CartX, CartY));
            Assert.That(CutRows(), Is.Empty);
            Assert.That(Count(Offcut), Is.Zero);

            var restored = ReloadHarness(manager, cart, out _);
            Assert.True(restored.Charred);
            Assert.False(restored.Cut);
        }

        [Test]
        public void LateFireRetractsOnlyTheAttemptedUnitAndLeavesTheHarnessCharred()
        {
            EnterCartSite();
            var cart = Cart;
            var harness = cart.GetPart<CartHarnessPart>();
            Carry("Dagger");
            var existing = Carry(Offcut);
            existing.GetPart<StackerPart>().StackCount = 2;
            var quoted = CutRows().Single();
            var burn = new BurningEffect(1f, Actor);
            int stagedUnits = 0;
            bool applied = false;
            Actor.AddPart(new LateCutObserver
            {
                Command = quoted.Command,
                Observe = () =>
                {
                    stagedUnits = PackedOffcuts;
                    applied = cart.ApplyEffect(burn, Actor, Zone);
                }
            });

            Assert.False(ChooseRow(quoted, CartX, CartY));
            Assert.That(stagedUnits, Is.EqualTo(3));
            Assert.True(applied);
            Assert.That(cart.GetEffect<BurningEffect>(), Is.SameAs(burn));
            Assert.True(harness.Charred);
            Assert.False(harness.Cut);
            Assert.That(existing.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(PackedOffcuts, Is.EqualTo(2));
            Assert.That(Count(Offcut), Is.EqualTo(2));
            Assert.That(CutRows(), Is.Empty);
        }

        sealed class LateCutObserver : Part
        {
            internal string Command;
            internal Action Observe;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && e.GetStringParameter("Command") == Command)
                    Observe?.Invoke();
                return true;
            }
        }
    }
}
