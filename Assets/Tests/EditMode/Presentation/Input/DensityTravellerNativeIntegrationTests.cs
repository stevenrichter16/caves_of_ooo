using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DensityTravellerNativeIntegrationTests
    {
        DensityLootTestScope content; HotbarSaveFixture native;
        Entity player; Zone oldZone, destination; OverworldZoneManager manager;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); native = new HotbarSaveFixture(true, false);
            FactionManager.Initialize(); player = native.Input.PlayerEntity; oldZone = native.Input.CurrentZone;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 718);
            var probe = content.Factory.CreateEntity("Player");
            for (int x = 0; x < WorldMap.Width && destination == null; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    if (WorldMapAuthoring.TierAt(x,y) > 3 || manager.WorldMap.GetPOI(x,y) != null) continue;
                    var candidate = new Zone(WorldMap.ToZoneID(x,y,0)); candidate.AddEntity(probe,10,10);
                    manager.ReplaceLoadedState(new Dictionary<string,Zone>{{candidate.ZoneID,candidate}}, candidate.ZoneID, new Dictionary<string,List<ZoneConnection>>());
                    if (WorldTravellers.OnZoneEntered(probe,candidate)) { destination = new Zone(candidate.ZoneID); break; }
                    candidate.RemoveEntity(probe);
                }
            Assert.NotNull(destination);
            manager.ReplaceLoadedState(new Dictionary<string,Zone>{{oldZone.ZoneID,oldZone},{destination.ZoneID,destination}}, oldZone.ZoneID, new Dictionary<string,List<ZoneConnection>>());
            native.BindOld(GameSessionState.Capture("traveller-native", "test", manager, native.Input.TurnManager, player));
        }
        [TearDown] public void Cleanup() { native?.Dispose(); content?.Dispose(); FactionManager.Reset(); }
        void Transfer()
        {
            if (oldZone.GetEntityCell(player) != null) oldZone.RemoveEntity(player);
            if (destination.GetEntityCell(player) == null) Assert.True(destination.AddEntity(player,10,10));
            var result = new ZoneTransitionResult { Success=true, NewZone=destination, NewPlayerX=10, NewPlayerY=10 };
            typeof(InputHandler).GetMethod("HandleZoneTransition",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(native.Input,new object[]{result});
        }
        [Test] public void RealTransitionRegistersOnePersistentMerchantWithoutAnotherTurn()
        {
            int tick=native.Input.TurnManager.TickCount; Transfer();
            var merchant=destination.GetReadOnlyEntities().Single(e=>e.GetProperty(WorldTravellers.OriginProperty)!=null);
            Assert.True(native.Input.TurnManager.IsRegistered(merchant)); Assert.AreSame(destination,merchant.GetPart<BrainPart>().CurrentZone);
            Assert.AreEqual(tick,native.Input.TurnManager.TickCount);
            var inv=merchant.GetPart<InventoryPart>(); foreach(var item in inv.Objects.ToArray())inv.RemoveObject(item); TradeSystem.SetDrams(merchant,17);
            Transfer(); Assert.AreEqual(0,inv.Objects.Count); Assert.AreEqual(17,TradeSystem.GetDrams(merchant));
            Assert.AreEqual(1,destination.GetReadOnlyEntities().Count(e=>e.GetProperty(WorldTravellers.OriginProperty)!=null));
        }
        [Test] public void ReadingCachedDestinationDoesNotRollOrSpawn()
        {
            Assert.False(destination.GetReadOnlyEntities().Any(e=>e.GetProperty(WorldTravellers.OriginProperty)!=null));
            Assert.AreSame(destination,manager.GetZone(destination.ZoneID));
            // Existing authored sites may upgrade their own unrelated owners on
            // access. They must not roll or create a travelling encounter.
            Assert.False(destination.GetReadOnlyEntities().Any(e=>e.GetProperty(WorldTravellers.OriginProperty)!=null));
            Assert.False(player.IntProperties.Keys.Any(k=>k.StartsWith("Traveller")));
        }
        [Test] public void DeadPlayerTransitionDoesNotCreateEncounter()
        {player.SetTag("_DeathHandled");Transfer();Assert.False(destination.GetReadOnlyEntities().Any(e=>e.GetProperty(WorldTravellers.OriginProperty)!=null));Assert.False(player.IntProperties.Keys.Any(k=>k.StartsWith("Traveller")));}
    }
}
