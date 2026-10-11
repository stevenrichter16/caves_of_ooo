using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Music region follows the actual occupied world-map tile. No test generates a destination.</summary>
    public sealed class WorldAudioWorldMapTests
    {
        GameObject root;
        InputHandler input;
        WorldAudioRuntime runtime;
        WorldMap map;
        Zone mapZone;
        Entity actor;
        TurnManager priorTurns;
        float priorMusic,priorAmbience,priorSfx;
        bool priorPause;
        readonly Dictionary<string,float?> prefs=new Dictionary<string,float?>();

        [SetUp] public void Setup()
        {
            priorMusic=WorldAudioSettings.MusicVolume;priorAmbience=WorldAudioSettings.AmbienceVolume;priorSfx=WorldAudioSettings.SfxVolume;
            priorPause=AudioListener.pause;AudioListener.pause=false;
            foreach(var key in new[]{WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey})
            {prefs[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetFloat(key):(float?)null;PlayerPrefs.SetFloat(key,1);}
            priorTurns=TurnManager.Active;
            root=new GameObject("world-map musical region tests");
            input=root.AddComponent<InputHandler>();
            map=new WorldMap(64);map.Tiles[10,10]=BiomeType.Beating;
            mapZone=new Zone(WorldMap.WorldMapZoneID);
            actor=WorldAudioAdversarialTests.Actor("map-player");
            actor.AddPart(new WorldMapPart{LastZoneIDOnSurface="Overworld.10.10.0",LastZoneX=5,LastZoneY=5});
            actor.AddPart(new WorldMapTravelCostPart());
            var at=WorldMap.WorldCellToZoneCell(10,10);Assert.True(mapZone.AddEntity(actor,at.zoneX,at.zoneY));
            foreach(var cell in mapZone.Cells){cell.Explored=true;cell.IsVisible=true;}
            input.PlayerEntity=actor;input.CurrentZone=mapZone;input.WorldMap=map;input.TurnManager=new TurnManager();
            runtime=root.AddComponent<WorldAudioRuntime>();
        }
        [TearDown] public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(root);
            typeof(TurnManager).GetProperty("Active").SetValue(null,priorTurns);
            foreach(var pair in prefs)if(pair.Value.HasValue)PlayerPrefs.SetFloat(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);
            WorldAudioSettings.MusicVolume=priorMusic;WorldAudioSettings.AmbienceVolume=priorAmbience;WorldAudioSettings.SfxVolume=priorSfx;
            AudioListener.pause=priorPause;
        }
        void Start(){runtime.Initialize(input);runtime.Tick(3);}
        void Next(){input.TurnManager.AdvanceClock(1);runtime.Tick(3);}
        void SilentMapEnvironment()
        {Assert.Null(runtime.Context.AmbienceId);Assert.False(runtime.Context.Danger);Assert.AreEqual(0,runtime.Soundscape.SourceCount);}

        [TestCase(BiomeType.Spread,"organic")]
        [TestCase(BiomeType.Sodden,"organic")]
        [TestCase(BiomeType.Grovelands,"organic")]
        [TestCase(BiomeType.Beating,"dry")]
        [TestCase(BiomeType.Overwrit,"dry")]
        [TestCase(BiomeType.Stump,"underground")]
        public void MapUsesItsOccupiedRegionsExistingScoreAndNoLocalSoundscape(BiomeType biome,string expected)
        {
            map.Tiles[10,10]=biome;Start();
            Assert.AreEqual(expected,runtime.Context.MusicId);
            Assert.AreEqual(expected,runtime.Player.CurrentMusicId);
            Assert.AreEqual(1,runtime.Player.ActiveMusicSources);
            SilentMapEnvironment();Assert.AreEqual(0,runtime.Soundscape.ScanCount);Assert.AreEqual(16,runtime.Player.AllocatedSources);
        }

        [Test] public void RealMapMovementUsesNewTileRatherThanSavedLastSurfaceAddress()
        {
            map.Tiles[11,10]=BiomeType.Stump;Start();Assert.AreEqual("dry",runtime.Context.MusicId);
            int before=input.TurnManager.TickCount;
            Assert.True(MovementSystem.TryMove(actor,mapZone,1,0));
            Assert.Greater(input.TurnManager.TickCount,before,"The real map travel part supplies the sampling boundary.");
            runtime.Tick(3);
            Assert.AreEqual("underground",runtime.Context.MusicId);
            Assert.AreEqual("Overworld.10.10.0",actor.GetPart<WorldMapPart>().LastZoneIDOnSurface,"This is historical return information, not current position.");
            Assert.True(MovementSystem.TryMove(actor,mapZone,-1,0));runtime.Tick(3);
            Assert.AreEqual("dry",runtime.Context.MusicId);SilentMapEnvironment();
        }

        [Test] public void ActualAscendAndDescendPreserveSameRegionWithoutRestartingMusic()
        {
            var ground=new Zone("Overworld.10.10.0");mapZone.RemoveEntity(actor);Assert.True(ground.AddEntity(actor,5,5));
            var marker=new Entity{BlueprintName="WorldMapTerrain"};marker.AddPart(new WorldMapCellPart{WorldX=10,WorldY=10});
            var at=WorldMap.WorldCellToZoneCell(10,10);Assert.True(mapZone.AddEntity(marker,at.zoneX,at.zoneY));
            var manager=new ZoneManager(null,64);manager.CachedZones[mapZone.ZoneID]=mapZone;manager.SetActiveZone(ground);
            input.ZoneManager=manager;input.CurrentZone=ground;Start();
            Assert.AreEqual("dry",runtime.Context.MusicId);int starts=runtime.Player.MusicStartCount;
            var up=WorldMapTraversal.Ascend(actor,ground,manager);Assert.True(up.Success,up.ErrorReason);
            input.CurrentZone=up.NewZone;manager.SetActiveZone(up.NewZone);runtime.Tick(3);
            Assert.AreEqual("dry",runtime.Context.MusicId);Assert.AreEqual(starts,runtime.Player.MusicStartCount);SilentMapEnvironment();
            int cached=manager.CachedZones.Count;
            var down=WorldMapTraversal.Descend(actor,mapZone,manager);Assert.True(down.Success,down.ErrorReason);
            input.CurrentZone=down.NewZone;manager.SetActiveZone(down.NewZone);runtime.Tick(3);
            Assert.AreSame(ground,input.CurrentZone);Assert.AreEqual("dry",runtime.Context.MusicId);
            Assert.AreEqual(starts,runtime.Player.MusicStartCount);Assert.AreEqual(cached,manager.CachedZones.Count);
            Assert.AreEqual((5,5),ground.GetEntityPosition(actor));
        }

        [Test] public void ActiveOverworldManagerIsAuthoritativeOverAStaleInputMap()
        {
            var manager=OverworldZoneManager.CreateDetached(null,64);
            manager.WorldMap.Tiles[10,10]=BiomeType.Stump;manager.SetActiveZone(mapZone);
            input.ZoneManager=manager;map.Tiles[10,10]=BiomeType.Spread;
            int cached=manager.CachedZones.Count;Start();
            Assert.AreEqual("underground",runtime.Context.MusicId);
            Assert.AreEqual(cached,manager.CachedZones.Count,"Audio must not call GetZone or generate a destination.");
        }

        [Test] public void UnrelatedManagersActiveGraphCannotOverrideTheCurrentInputMap()
        {
            var manager=OverworldZoneManager.CreateDetached(null,64);
            manager.WorldMap.Tiles[10,10]=BiomeType.Stump;manager.SetActiveZone(new Zone(mapZone.ZoneID));
            input.ZoneManager=manager;Start();
            Assert.AreEqual("dry",runtime.Context.MusicId,"Equal zone IDs cannot make an old graph authoritative.");
        }

        [Test] public void MapSymbolsAndVisibleHostilesNeverCreateLocalThreatMusic()
        {
            var enemy=WorldAudioAdversarialTests.Actor("map-marker-enemy");enemy.Tags.Remove("Player");
            enemy.AddPart(new BrainPart());enemy.GetPart<BrainPart>().PersonalEnemies.Add(actor);
            var at=mapZone.GetEntityCell(actor);Assert.True(mapZone.AddEntity(enemy,at.X+1,at.Y));
            map.POIs[10,10]=new PointOfInterest(POIType.Lair,"unvisited lair",bossBlueprint:"BogTaken");
            Start();Assert.AreEqual("dry",runtime.Context.MusicId);SilentMapEnvironment();
            Assert.False(map.IsVisited(10,10),"Music must not mark locations explored.");
        }

        [TestCase(29,13)]
        [TestCase(50,13)]
        [TestCase(40,2)]
        [TestCase(40,23)]
        public void MapBorderCellsDoNotInventSpreadOrReadAnInvalidTile(int x,int y)
        {
            Assert.True(mapZone.MoveEntity(actor,x,y));Start();
            Assert.Null(runtime.Context.MusicId);Assert.AreEqual(0,runtime.Player.ActiveMusicSources);SilentMapEnvironment();
            var valid=WorldMap.WorldCellToZoneCell(10,10);Assert.True(mapZone.MoveEntity(actor,valid.zoneX,valid.zoneY));Next();
            Assert.AreEqual("dry",runtime.Context.MusicId,"A real valid counterpart must become audible.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingMapOrTilesIsSilentAndRestoredDataHasAValidCounterpart(bool missingTiles)
        {
            if(missingTiles)map.Tiles=null;else input.WorldMap=null;
            Assert.DoesNotThrow(Start);Assert.Null(runtime.Context.MusicId);SilentMapEnvironment();
            input.WorldMap=map;map.Tiles=new BiomeType[WorldMap.Width,WorldMap.Height];map.Tiles[10,10]=BiomeType.Beating;
            Next();Assert.AreEqual("dry",runtime.Context.MusicId);
        }

        [Test] public void DetachedActorCannotLeaveAMapMusicContextAlive()
        {
            Start();Assert.AreEqual("dry",runtime.Context.MusicId);
            mapZone.RemoveEntity(actor);runtime.Tick(3);
            Assert.Null(runtime.Context.MusicId);Assert.AreEqual(0,runtime.Player.ActiveMusicSources);SilentMapEnvironment();
        }

        [Test] public void ReplacedGraphAndMapSelectTheirActualPositionWithoutDuplicatingVoices()
        {
            Start();int beforeChecks=runtime.ContextCheckCount;
            var replacement=new Zone(WorldMap.WorldMapZoneID);var next=WorldAudioAdversarialTests.Actor("map-player");
            var at=WorldMap.WorldCellToZoneCell(3,7);Assert.True(replacement.AddEntity(next,at.zoneX,at.zoneY));
            var loadedMap=new WorldMap(67);loadedMap.Tiles[3,7]=BiomeType.Stump;
            input.CurrentZone=replacement;input.PlayerEntity=next;input.WorldMap=loadedMap;runtime.Tick(3);
            Assert.AreEqual("underground",runtime.Context.MusicId);Assert.AreEqual(beforeChecks+1,runtime.ContextCheckCount);
            Assert.AreEqual(16,root.GetComponentsInChildren<AudioSource>().Length);SilentMapEnvironment();
        }

        [Test] public void MapIdleAndMenuKeepTheTrackWithoutLocalDiscoveryOrRepeatedContextWork()
        {
            Start();int checks=runtime.ContextCheckCount,starts=runtime.Player.MusicStartCount;
            for(int i=0;i<120;i++)runtime.Tick(.016f);
            Assert.AreEqual(checks,runtime.ContextCheckCount);Assert.AreEqual(starts,runtime.Player.MusicStartCount);
            input.enabled=false;runtime.Tick(3);Assert.AreEqual("dry",runtime.Context.MusicId);SilentMapEnvironment();
            input.enabled=true;runtime.Tick(0);Assert.AreEqual(checks+1,runtime.ContextCheckCount);
            Assert.AreEqual(starts,runtime.Player.MusicStartCount);Assert.AreEqual(16,runtime.Player.AllocatedSources);
        }

        [Test] public void OrdinaryGroundAndDepthSelectionKeepTheirExistingRules()
        {
            var ground=new Zone("Overworld.10.10.0");mapZone.RemoveEntity(actor);Assert.True(ground.AddEntity(actor,5,5));
            input.CurrentZone=ground;Start();Assert.AreEqual("dry",runtime.Context.MusicId);
            var cave=new Zone("Overworld.10.10.2");ground.RemoveEntity(actor);Assert.True(cave.AddEntity(actor,5,5));
            input.CurrentZone=cave;runtime.Tick(3);Assert.AreEqual("underground",runtime.Context.MusicId);
        }
    }
}
