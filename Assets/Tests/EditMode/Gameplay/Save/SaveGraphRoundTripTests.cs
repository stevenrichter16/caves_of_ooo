using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.Save
{
    public class SaveGraphRoundTripTests
    {
        [Test]
        public void WholeWorldRoundTripPreservesZoneInventoryEquipmentAndTurns()
        {
            Entity player = CreateCreature("player-1", "Player", "@", isPlayer: true);
            Entity item = CreateItem("blade-1", "IronLongsword", "/");
            Entity npc = CreateCreature("npc-1", "Snapjaw", "s", isPlayer: false);

            var root = new BodyPart { Type = "Body", Name = "body", ID = 100 };
            var hand = new BodyPart { Type = "Hand", Name = "hand", ID = 101 };
            root.AddPart(hand);
            var body = new Body();
            player.AddPart(body);
            body.SetBody(root);

            var inventory = new InventoryPart();
            player.AddPart(inventory);
            Assert.IsTrue(inventory.AddObject(item));
            Assert.IsTrue(inventory.EquipToBodyPart(item, hand));

            var brain = new BrainPart { Target = player };
            npc.AddPart(brain);

            var zone = new Zone("Overworld.10.10.0");
            zone.GetCell(1, 2).Explored = true;
            zone.GetCell(1, 2).IsVisible = true;
            zone.GetCell(1, 2).IsInterior = true;
            zone.AddEntity(player, 1, 2);
            zone.AddEntity(npc, 4, 5);
            brain.CurrentZone = zone;

            var manager = new OverworldZoneManager(null, 12345);
            manager.ReplaceLoadedState(
                new System.Collections.Generic.Dictionary<string, Zone> { { zone.ZoneID, zone } },
                zone.ZoneID,
                new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ZoneConnection>>());

            var turns = new TurnManager();
            turns.RestoreSavedState(
                2,
                waitingForInput: true,
                currentActor: player,
                new System.Collections.Generic.List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 200 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 150 }
                });

            var state = GameSessionState.Capture(
                "test-game",
                "test-version",
                manager,
                turns,
                player);

            GameSessionState loaded;
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream);
                state.Save(writer);
                stream.Position = 0;
                var reader = new SaveReader(stream, null);
                loaded = GameSessionState.Load(reader);
            }

            Zone loadedZone = loaded.ZoneManager.ActiveZone;
            Entity loadedPlayer = loaded.Player;
            Entity playerFromCell = loadedZone.GetCell(1, 2).Objects[0];
            Assert.AreSame(loadedPlayer, playerFromCell);
            Assert.IsTrue(loadedZone.GetCell(1, 2).Explored);
            Assert.IsTrue(loadedZone.GetCell(1, 2).IsVisible);
            Assert.IsTrue(loadedZone.GetCell(1, 2).IsInterior);

            var loadedInventory = loadedPlayer.GetPart<InventoryPart>();
            var loadedBody = loadedPlayer.GetPart<Body>();
            BodyPart loadedHand = loadedBody.GetPartByType("Hand");
            Entity equipped = loadedHand.Equipped;

            Assert.IsNotNull(equipped);
            Assert.AreSame(equipped, loadedInventory.EquippedItems[loadedHand.ID.ToString()]);
            Assert.AreEqual("IronLongsword", equipped.BlueprintName);

            Assert.AreEqual(turns.TickCount, loaded.TurnManager.TickCount);
            Assert.AreEqual(turns.GetEnergy(player), loaded.TurnManager.GetEnergy(loadedPlayer));
        }

        private static Entity CreateCreature(string id, string blueprint, string glyph, bool isPlayer)
        {
            var entity = new Entity
            {
                ID = id,
                BlueprintName = blueprint
            };
            entity.AddPart(new RenderPart { DisplayName = blueprint, RenderString = glyph, ColorString = "&W" });
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 0, Max = 1000, Owner = entity };
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10, Owner = entity };
            if (isPlayer)
                entity.SetTag("Player");
            return entity;
        }

        private static Entity CreateItem(string id, string blueprint, string glyph)
        {
            var entity = new Entity
            {
                ID = id,
                BlueprintName = blueprint
            };
            entity.AddPart(new RenderPart { DisplayName = blueprint, RenderString = glyph, ColorString = "&y" });
            entity.AddPart(new PhysicsPart());
            return entity;
        }
    }
}
