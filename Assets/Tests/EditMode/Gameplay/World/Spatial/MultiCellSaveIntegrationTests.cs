using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class MultiCellSaveIntegrationTests
    {
        private EntityFactory Factory()
        {
            var f=new EntityFactory();f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));return f;
        }
        [Test] public void AccessingAnAlreadyCachedLegacySouthChunkInstallsPilotWithoutReplacingSavedLoot()
        {
            var f=Factory();var manager=new OverworldZoneManager(f,47);var legacy=new Zone(MultiCellPilotRuntime.ZoneID);
            var loot=f.CreateEntity("GoldCoin");Assert.NotNull(loot);legacy.AddEntity(loot,40,12);
            manager.CachedZones[legacy.ZoneID]=legacy;
            Assert.AreSame(legacy,manager.GetZone(legacy.ZoneID));
            Assert.IsTrue(MultiCellPilotRuntime.IsActive(legacy));Assert.AreEqual((40,12),legacy.GetEntityPosition(loot));
        }
        [Test] public void LoadedActiveLegacySouthChunkUpgradesBeforeRenderingWithoutAnotherGetZone()
        {
            var f=Factory();var manager=new OverworldZoneManager(f,47);var legacy=new Zone(MultiCellPilotRuntime.ZoneID);
            manager.CachedZones[legacy.ZoneID]=legacy;manager.SetActiveZone(legacy);
            var state=new GameSessionState {ZoneManager=manager,ActiveZoneID=legacy.ZoneID};
            SaveGraphSerializer.RebuildLoadedWorld(state);
            Assert.IsTrue(MultiCellPilotRuntime.IsActive(legacy));
        }
    }
}
