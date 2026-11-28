using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WayfarerShadeTests
    {
        private const int Seed = 65;
        private const string ZoneId = "Overworld.19.14.0";
        private const string MerchantId = "traveller:65:" + ZoneId;
        private const string ReceiptKey = "TravellerSpawn:65:" + ZoneId;
        private const int HeightTick = 2930;
        private const int DuskTick = 3000;

        private DensityLootTestScope _content;
        private OverworldZoneManager _manager;
        private TurnManager _turns;
        private Zone _zone;
        private Entity _player;
        private Entity _merchant;
        private (int x, int y) _home;
        private (int x, int y) _awningCell;

        [SetUp]
        public void SetUp()
        {
            _content = new DensityLootTestScope();
            FactionManager.Initialize();
            _manager = OverworldZoneManager.CreateDetached(_content.Factory, Seed);
            _zone = new Zone(ZoneId);
            _manager.SetActiveZone(_zone);
            _player = _content.Factory.CreateEntity("Player");
            _player.GetPart<InventoryPart>().MaxWeight = 400;
            Assert.IsTrue(_zone.AddEntity(_player, 40, 12));

            _turns = new TurnManager();
            SetTick(HeightTick);
            var (worldX, worldY, _) = WorldMap.FromZoneID(ZoneId);
            Assert.AreEqual(BiomeType.Beating, WorldMapAuthoring.BiomeAt(worldX, worldY));
            Assert.IsNull(_manager.WorldMap.GetPOI(worldX, worldY));
            Assert.IsTrue(WorldTravellers.OnZoneEntered(_player, _zone), "Expected the earned encounter's seed and address to win the ordinary entry roll.");
            _merchant = _zone.GetReadOnlyEntities().Single(e => e.ID == MerchantId);
            Assert.AreEqual("Merchant", _merchant.BlueprintName);
            Assert.AreEqual(MerchantId, _player.GetProperty(ReceiptKey));
            Assert.AreNotEqual("fallback", WorldTravellers.DescribeConversation(_merchant, _player, "Merchant_1", "Start", "fallback"));
            Assert.IsFalse(BeatingGlareSystem.HasHeadCover(_merchant));

            _home = _zone.GetEntityPosition(_merchant);
            _awningCell = (_home.x - 2, _home.y);
            Turn(_merchant);
            var brain = _merchant.GetPart<BrainPart>();
            Assert.IsTrue(brain.Staying);
            Assert.AreEqual(_home, (brain.StartingCellX, brain.StartingCellY));
            Assert.AreEqual(_home, _zone.GetEntityPosition(_merchant));
            Assert.IsTrue(_zone.MoveEntity(_player, _awningCell.x - 1, _awningCell.y));
            for (int x = _awningCell.x - 1; x <= _home.x + 1; x++)
                for (int y = _home.y - 1; y <= _home.y + 1; y++)
                    Assert.IsTrue(_zone.AddEntity(_content.Factory.CreateEntity("Sand"), x, y));
        }

        [TearDown]
        public void TearDown()
        {
            _content?.Dispose();
            FactionManager.Reset();
        }

        [Test]
        public void InstalledAwningChangesTheIdleMerchantsPlaceUntilItIsTakenUp()
        {
            var kit = CarryAwning();
            SetDown(kit);
            var stock = _merchant.GetPart<InventoryPart>().Objects.ToArray();
            int drams = TradeSystem.GetDrams(_merchant);
            int hp = _merchant.GetStatValue("Hitpoints");

            AssertStepsIntoShade(_merchant, _zone, _home);

            var underAwning = _zone.GetEntityPosition(_merchant);
            for (int i = 1; i <= 12; i++)
            {
                SetTick(HeightTick + i);
                Turn(_merchant);
                Assert.AreEqual(underAwning, _zone.GetEntityPosition(_merchant), "The existing Staying routine must not pull this actor out of useful shade during Height.");
            }

            var take = WorldInteractionSystem.GatherActions(kit, _player).Single(a => a.Name == "TakeUpFixture");
            Assert.IsTrue(InventorySystem.PerformAction(_player, kit, take.Command, _zone));
            Assert.IsTrue(_player.GetPart<InventoryPart>().Objects.Contains(kit));
            Assert.IsFalse(ShadePart.Covers(_zone, _zone.GetEntityCell(_merchant)));

            for (int i = 0; i < 3; i++) Turn(_merchant);
            Assert.AreEqual(_home, _zone.GetEntityPosition(_merchant));
            Assert.AreSame(_merchant, _zone.GetReadOnlyEntities().Single(e => e.ID == MerchantId));
            Assert.AreNotEqual("fallback", WorldTravellers.DescribeConversation(_merchant, _player, "Merchant_1", "Sources", "fallback"));
            CollectionAssert.AreEqual(stock, _merchant.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(drams, TradeSystem.GetDrams(_merchant));
            Assert.AreEqual(hp, _merchant.GetStatValue("Hitpoints"));
            Assert.IsFalse(_merchant.HasEffect<ParchedEffect>());
        }

        [Test]
        public void MerchantResumesStayingWhenHeightEndsAndAwningIsStillStanding()
        {
            var kit = CarryAwning();
            SetDown(kit);
            AssertStepsIntoShade(_merchant, _zone, _home);

            SetTick(DuskTick);
            Assert.AreEqual(DayBand.Dusk, WorldClock.GetBand(WorldClock.CurrentTick));
            for (int i = 0; i < 3; i++) Turn(_merchant);

            Assert.AreEqual(_home, _zone.GetEntityPosition(_merchant));
            Assert.IsTrue(kit.GetPart<ShadePart>().Shading);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CarriedOrDroppedAwningDoesNotChangeTheMerchantsPlace(bool drop)
        {
            var kit = CarryAwning();
            if (drop)
            {
                Assert.IsTrue(_zone.MoveEntity(_player, _awningCell.x, _awningCell.y));
                Assert.IsTrue(InventorySystem.Drop(_player, kit, _zone));
                Assert.AreEqual(_awningCell, _zone.GetEntityPosition(kit));
                Assert.IsTrue(_zone.MoveEntity(_player, _awningCell.x - 1, _awningCell.y));
            }

            Assert.IsFalse(kit.GetPart<ShadePart>().Shading);
            Turn(_merchant);
            Assert.AreEqual(_home, _zone.GetEntityPosition(_merchant));

            if (drop)
            {
                Assert.IsTrue(_zone.MoveEntity(_player, _awningCell.x, _awningCell.y));
                Assert.IsTrue(InventorySystem.Pickup(_player, kit, _zone));
                Assert.IsTrue(_zone.MoveEntity(_player, _awningCell.x - 1, _awningCell.y));
            }

            SetDown(kit);
            AssertStepsIntoShade(_merchant, _zone, _home);
        }

        [Test]
        public void AlreadyCoveredHeadKeepsTheMerchantsOrdinaryIdleRoutine()
        {
            var kit = CarryAwning();
            SetDown(kit);
            var cap = _content.Factory.CreateEntity("LeatherCap");
            var head = _merchant.GetPart<Body>().GetPartByType("Head");
            Assert.IsTrue(_merchant.GetPart<InventoryPart>().AddObject(cap));
            Assert.IsTrue(InventorySystem.Equip(_merchant, cap, head));
            Assert.IsTrue(BeatingGlareSystem.HasHeadCover(_merchant));

            Turn(_merchant);
            Assert.AreEqual(_home, _zone.GetEntityPosition(_merchant));

            Assert.IsTrue(InventorySystem.UnequipItem(_merchant, cap));
            AssertStepsIntoShade(_merchant, _zone, _home);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AdjacentShadeNeedsAClearSafeStep(bool hazard)
        {
            var kit = CarryAwning();
            SetDown(kit);
            var obstructions = new List<(int x, int y)>();
            var walls = new List<Entity>();
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    obstructions.Add((_home.x + dx, _home.y + dy));
                }

            foreach (var (x, y) in obstructions)
            {
                if (hazard)
                {
                    _zone.TileState.WriteCoating(x, y, "acid", 20);
                    Assert.IsTrue(PlacementCheck.Hazard(_zone, _zone.GetCell(x, y)));
                }
                else
                {
                    var wall = new Entity();
                    wall.SetTag("Solid");
                    wall.AddPart(new PhysicsPart { Solid = true });
                    Assert.IsTrue(_zone.AddEntity(wall, x, y));
                    walls.Add(wall);
                }
            }

            for (int i = 0; i < 3; i++) Turn(_merchant);
            Assert.AreEqual(_home, _zone.GetEntityPosition(_merchant));
            Assert.IsTrue(kit.GetPart<ShadePart>().Shading);

            foreach (var (x, y) in obstructions) _zone.TileState.Clear(x, y);
            foreach (var wall in walls) Assert.IsTrue(_zone.RemoveEntity(wall));
            AssertStepsIntoShade(_merchant, _zone, _home);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BeforeMoveCanInterruptTheShadeStep(bool assignDuty)
        {
            var kit = CarryAwning();
            SetDown(kit);
            var brain = _merchant.GetPart<BrainPart>();
            var duty = new WaitGoal(10);
            var interruption = new ShadeStepInterruption();
            interruption.Action = cell =>
            {
                if (assignDuty) brain.PushGoal(duty);
                else _zone.TileState.WriteCoating(cell.X, cell.Y, "acid", 20);
            };
            _merchant.AddPart(interruption);

            Turn(_merchant);

            Assert.IsNotNull(interruption.Contact);
            Assert.AreEqual(_home, _zone.GetEntityPosition(_merchant));
            if (assignDuty)
            {
                Assert.AreSame(duty, brain.PeekGoal());
                brain.RemoveGoal(duty);
            }
            else _zone.TileState.Clear(interruption.Contact.X, interruption.Contact.Y);
            Assert.IsTrue(_merchant.RemovePart(interruption));
            AssertStepsIntoShade(_merchant, _zone, _home);
        }

        private sealed class ShadeStepInterruption : Part
        {
            public override string Name => "ShadeStepInterruption";
            public Action<Cell> Action;
            public Cell Contact;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeMove")
                {
                    Contact = e.GetParameter<Cell>("TargetCell");
                    Action(Contact);
                }
                return true;
            }
        }

        [Test]
        public void MissingReciprocalReceiptPreventsExistingMerchantEnrollment()
        {
            var kit = CarryAwning();
            SetDown(kit);
            RemoveRuntimeIdleParts(_merchant);
            Assert.IsTrue(_player.Properties.Remove(ReceiptKey));
            Assert.AreEqual("fallback", WorldTravellers.DescribeConversation(_merchant, _player, "Merchant_1", "Start", "fallback"));
            Assert.IsFalse(WorldTravellers.OnZoneEntered(_player, _zone));

            Turn(_merchant);
            Assert.AreEqual(_home, _zone.GetEntityPosition(_merchant));

            _player.Properties[ReceiptKey] = MerchantId;
            Assert.IsFalse(WorldTravellers.OnZoneEntered(_player, _zone));
            AssertStepsIntoShade(_merchant, _zone, _home);
        }

        [Test]
        public void ReentryRestoresTheExistingMerchantsResponseWithoutAReplacement()
        {
            var kit = CarryAwning();
            SetDown(kit);
            RemoveRuntimeIdleParts(_merchant);

            var other = new Zone("Overworld.18.14.0");
            var playerCell = _zone.GetEntityPosition(_player);
            Assert.IsTrue(_zone.RemoveEntity(_player));
            _manager.SetActiveZone(other);
            Assert.IsTrue(other.AddEntity(_player, 40, 12));
            Assert.IsTrue(other.RemoveEntity(_player));
            _manager.SetActiveZone(_zone);
            Assert.IsTrue(_zone.AddEntity(_player, playerCell.x, playerCell.y));

            Assert.IsFalse(WorldTravellers.OnZoneEntered(_player, _zone), "A previously consumed entry does not create another merchant.");
            Assert.AreSame(_merchant, _zone.GetReadOnlyEntities().Single(e => e.ID == MerchantId));
            AssertStepsIntoShade(_merchant, _zone, _home);
        }

        [Test]
        public void LoadedLegacyTravellerRespondsWithoutAnotherZoneEntry()
        {
            var kit = CarryAwning();
            SetDown(kit);
            RemoveRuntimeIdleParts(_merchant);
            FarmingAccessGrant.MarkGranted(_player);
            SetTick(HeightTick);

            using var bootstrap = new HotbarSaveFixture(false, false);
            HotbarSaveFixture.Set(bootstrap.Bootstrap, "_factory", _content.Factory);
            var state = GameSessionState.Capture("wayfarer-shade", "test", _manager, _turns, _player);
            using var stream = new MemoryStream();
            state.Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(stream, _content.Factory));
            bootstrap.Bootstrap.ApplyLoadedGame(loaded);

            Assert.AreEqual(HeightTick, WorldClock.CurrentTick);
            var zone = loaded.ZoneManager.ActiveZone;
            var merchant = zone.GetReadOnlyEntities().Single(e => e.ID == MerchantId);
            var canopy = zone.GetReadOnlyEntities().Single(e => e.ID == kit.ID);
            Assert.AreEqual(MerchantId, loaded.Player.GetProperty(ReceiptKey));
            Assert.AreEqual("CampAwning", canopy.BlueprintName);
            Assert.IsTrue(canopy.GetPart<ShadePart>().Shading);
            Assert.AreEqual(_awningCell, zone.GetEntityPosition(canopy));
            AssertStepsIntoShade(merchant, zone, _home);
        }

        private void SetTick(int tick)
        {
            var entries = new List<TurnManager.SavedTurnEntry>
            {
                new TurnManager.SavedTurnEntry { Entity = _player, Energy = 1000 }
            };
            if (_merchant != null)
                entries.Add(new TurnManager.SavedTurnEntry { Entity = _merchant, Energy = 0 });
            _turns.RestoreSavedState(tick, true, _player, entries);
        }

        private Entity CarryAwning()
        {
            var kit = _content.Factory.CreateEntity("CampAwning");
            Assert.IsTrue(_player.GetPart<InventoryPart>().AddObject(kit));
            return kit;
        }

        private void SetDown(Entity kit)
        {
            var row = InventorySystem.GetActions(_player, kit)
                .Single(a => a.Name == "SetDownFixture" && !a.IsDisabled && a.ActsOnCell(_awningCell.x, _awningCell.y));
            Assert.IsTrue(InventorySystem.PerformAction(_player, kit, row.Command, _zone));
            Assert.AreEqual(_awningCell, _zone.GetEntityPosition(kit));
            Assert.IsTrue(HeldFixtureActions.IsPlayerHeld(kit));
            Assert.IsFalse(ShadePart.Covers(_zone, _zone.GetEntityCell(_merchant)), "The canopy is in reach but its ring stops one cell short of the merchant.");
        }

        private static void AssertStepsIntoShade(Entity merchant, Zone zone, (int x, int y) from)
        {
            Assert.AreEqual(from, zone.GetEntityPosition(merchant));
            Assert.AreEqual(DayBand.Height, WorldClock.GetBand(WorldClock.CurrentTick));
            Assert.IsFalse(ShadePart.Covers(zone, zone.GetEntityCell(merchant)));

            var brain = merchant.GetPart<BrainPart>();
            var idleGoal = brain.PeekGoal();
            Assert.IsInstanceOf<BoredGoal>(idleGoal);
            Turn(merchant);

            Assert.AreSame(idleGoal, brain.PeekGoal(), "Shade uses the current idle turn and leaves the ordinary goal in place.");
            var to = zone.GetEntityPosition(merchant);
            Assert.AreEqual(1, Math.Max(Math.Abs(to.x - from.x), Math.Abs(to.y - from.y)), "The merchant should take one idle movement step.");
            Assert.IsTrue(ShadePart.Covers(zone, zone.GetEntityCell(merchant)), "That step should take the merchant under the actual canopy.");
        }

        private static void RemoveRuntimeIdleParts(Entity merchant)
        {
            foreach (var behavior in merchant.Parts.OfType<AIBehaviorPart>().ToArray())
                Assert.IsTrue(merchant.RemovePart(behavior));
        }

        private static void Turn(Entity actor) => actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
    }
}
