using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FellingSceneInteractionTurnTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ClearMenuChargesOneTurnOnlyWhenItActuallyClears(bool adjacent)
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            var zone = new Zone(FellingSiteBuilder.ZoneID);
            Assert.IsTrue(FellingSceneRuntime.Install(zone, factory));
            var prop = FellingSceneDefinition.Load().layers.First(layer => layer.mutable && !layer.blocksMovement);
            Entity target = FellingSceneRuntime.FindOwner(zone, prop.id);
            Assert.IsNotNull(target);
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.AddPart(new RenderPart { RenderLayer = 10 });
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 0, Max = 200 };
            zone.AddEntity(player, adjacent ? prop.anchorX : 0, adjacent ? prop.anchorY : 24);
            var turns = new TurnManager();
            turns.AddEntity(player); turns.ProcessUntilPlayerTurn();
            int before = turns.TickCount;
            var go = new GameObject("Felling turn test");
            try
            {
                var input = go.AddComponent<InputHandler>();
                input.PlayerEntity = player; input.CurrentZone = zone; input.TurnManager = turns;
                var action = WorldInteractionSystem.GatherActions(target, player).Single(a => a.Command == FellingScenePropPart.ClearCommand);
                var execute = typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(execute);
                execute.Invoke(input, new object[] { action, target, zone.GetEntityCell(target), false });
                Assert.AreEqual(!adjacent, FellingSceneRuntime.IsPresent(zone, prop.id));
                if (adjacent) Assert.AreEqual(before + TurnManager.ActionThreshold / 100, turns.TickCount, "Successful menu clearing must spend exactly one normal-speed turn.");
                else Assert.AreEqual(before, turns.TickCount, "Rejected remote action must not spend a turn.");
                int after = turns.TickCount;
                execute.Invoke(input, new object[] { action, target, zone.GetCell(prop.anchorX, prop.anchorY), false });
                Assert.AreEqual(after, turns.TickCount, "A stale menu must not clear twice or spend another turn.");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
