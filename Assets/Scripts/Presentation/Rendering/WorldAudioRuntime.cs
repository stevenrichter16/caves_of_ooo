using CavesOfOoo.Core;
using Unity.Profiling;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Bootstrap-owned audio lifetime. Place/threat queries run only when zone, actor or turn changes, or the world resumes;
    /// a bounded visible neighborhood is inspected. Per-frame work is fixed-pool fades, tail retirement and rechecking one known threat.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WorldAudioRuntime:MonoBehaviour
    {
        const float DangerReleaseSeconds=4f;const int DangerRadius=6;
        static readonly ProfilerMarker ContextMarker=new ProfilerMarker("COO.WorldAudio.Context");
        InputHandler input;WorldAudioEventRouter router;Zone zone;Entity actor;WorldMap map;
        int tick=int.MinValue;BiomeType biome=BiomeType.Spread;int depth;bool observedDanger;float dangerHold;
        Entity dangerWitness;bool wasWorldVisible;bool regionalContextValid=true;
        public WorldAudioPlayer Player{get;private set;}
        public WorldAudioSoundscape Soundscape{get;private set;}
        public WorldAudioContext Context{get;private set;}
        public int ContextCheckCount{get;private set;}
        public int AcceptedPlayerSteps=>router?.AcceptedPlayerSteps??0;
        public void Initialize(InputHandler source)
        {
            if(input==source&&Player?.AllocatedSources==16)return;Release();input=source;if(source==null)return;
            WorldAudioSettings.Load();Player=new WorldAudioPlayer(transform,WorldAudioLibrary.LoadResources());router=new WorldAudioEventRouter(Player);Soundscape=new WorldAudioSoundscape(Player);
            // The first context sample is after ZoneRenderer's FOV pass, too.
            tick=int.MinValue;
        }
        void OnEnable(){if(input!=null&&Player==null)Initialize(input);}
        void LateUpdate()=>Tick(Time.unscaledDeltaTime);
        public void Tick(float wallDelta)
        {
            if(input==null)return;
            if(Player==null||Player.AllocatedSources==0)Initialize(input);
            if(Player==null)return;
            float delta=WorldAudioPlayer.SafeDelta(wallDelta);
            var current=input.CurrentZone;var listener=input.PlayerEntity;int turn=input.TurnManager?.TickCount??0;
            bool changed=current!=zone||listener!=actor;
            bool onWorldMap=WorldMap.IsWorldMapZoneID(current?.ZoneID);
            var currentMap=ResolveWorldMap(input,current);
            if(changed){dangerHold=0;observedDanger=false;dangerWitness=null;Player.ClearSounds();}
            bool worldVisible=input.isActiveAndEnabled&&(input.ZoneRenderer==null||(input.ZoneRenderer.isActiveAndEnabled&&!input.ZoneRenderer.Paused));
            bool resumed=worldVisible&&!wasWorldVisible;wasWorldVisible=worldVisible;
            router.SetWorld(current,listener,worldVisible);
            if(changed||turn!=tick||map!=currentMap||resumed)
            {
                zone=current;actor=listener;tick=turn;map=currentMap;
                using(ContextMarker.Auto())
                {
                    ContextCheckCount++;
                    regionalContextValid=ResolveRegion(zone,actor,map,out biome,out depth);
                    // A modal can advance simulation while FOV remains from the previous
                    // view. Never discover threats from that stale hidden-world snapshot.
                    Soundscape.Observe(zone,actor,biome,depth,WorldClock.GetBand(turn),worldVisible);
                    dangerWitness=worldVisible&&!onWorldMap?VisibleDanger(zone,actor):null;observedDanger=dangerWitness!=null;if(observedDanger)dangerHold=DangerReleaseSeconds;
                }
            }
            // Recheck only the already-seen witness, never search for fresh hidden
            // enemies between turns. A later FOV hide/removal releases the hold.
            if(onWorldMap){observedDanger=false;dangerHold=0;dangerWitness=null;}
            if(observedDanger&&(!worldVisible||!VisibleWitness(zone,actor,dangerWitness)))observedDanger=false;
            if(!observedDanger)dangerHold=Mathf.Max(0,dangerHold-delta);
            bool alive=actor!=null&&actor.GetStatValue("Hitpoints",1)>0&&!CombatSystem.IsDeathHandled(actor)&&zone?.GetEntityCell(actor)!=null;
            string priorBed=Context.AmbienceId;
            Context=alive&&regionalContextValid?WorldAudioContext.Select(biome,depth,observedDanger||dangerHold>0,WorldAudioSettlement.IsPreparationStop(zone,map)):default;
            Soundscape.Tick(delta,worldVisible&&alive,Context.Danger);
            // Menus preserve the established bed but cannot discover new object/animal sources.
            if(alive)Context=Context.WithAmbience(worldVisible?Soundscape.AmbienceId:changed||priorBed=="water"?null:priorBed);
            Player.SetContext(Context);Player.Update(delta);
        }
        // The bootstrap/load coordinator mirrors the manager map onto InputHandler.
        // Prefer the manager only when it owns this exact active graph; detached
        // render fixtures and a stale manager use the input's current map instead.
        static WorldMap ResolveWorldMap(InputHandler source,Zone current)
        {
            var manager=source.ZoneManager as OverworldZoneManager;
            return current!=null&&manager?.ActiveZone==current?manager.WorldMap:source.WorldMap;
        }
        static bool ResolveRegion(Zone where,Entity listener,WorldMap world,out BiomeType region,out int level)
        {
            region=BiomeType.Spread;level=0;
            if(WorldMap.IsWorldMapZoneID(where?.ZoneID))
            {
                // This is the inverse used by world-map travel; a saved last
                // surface address is historical after the next map step.
                var cell=listener==null?null:where.GetEntityCell(listener);
                if(cell==null||cell.ParentZone!=where||listener.SpatialZone!=where
                    ||!cell.Objects.Contains(listener)||world?.Tiles==null)return false;
                var position=WorldMap.ZoneCellToWorldCell(cell.X,cell.Y);
                if(!world.InBounds(position.worldX,position.worldY))return false;
                region=world.GetBiome(position.worldX,position.worldY);return true;
            }
            var coordinates=WorldMap.FromZoneID(where?.ZoneID);
            level=coordinates.z>0?coordinates.z:0;
            if(world?.Tiles!=null&&world.InBounds(coordinates.x,coordinates.y))region=world.GetBiome(coordinates.x,coordinates.y);
            return true;
        }
        static Entity VisibleDanger(Zone where,Entity listener)
        {
            if(where==null||listener==null)return null;
            var origin=where.GetEntityCell(listener);if(origin==null)return null;
            for(int y=Mathf.Max(0,origin.Y-DangerRadius);y<=Mathf.Min(Zone.Height-1,origin.Y+DangerRadius);y++)
                for(int x=Mathf.Max(0,origin.X-DangerRadius);x<=Mathf.Min(Zone.Width-1,origin.X+DangerRadius);x++)
                {
                    var cell=where.GetCell(x,y);if(!cell.Explored||!cell.IsVisible)continue;
                    for(int i=0;i<cell.Occupants.Count;i++)
                    {
                        var owner=cell.Occupants[i];if(owner==null||owner==listener||owner.GetPart<BrainPart>()==null||owner.GetPart<RenderPart>()?.Visible!=true
                            ||owner.GetStatValue("Hitpoints",0)<=0||CombatSystem.IsDeathHandled(owner)||BrainPart.ArePartyAligned(listener,owner))continue;
                        if(FactionManager.IsHostile(owner,listener))return owner;
                    }
                }
            return null;
        }
        static bool VisibleWitness(Zone where,Entity listener,Entity witness)
        {
            if(where==null||listener==null||witness==null)return false;
            var origin=where.GetEntityCell(listener);var cell=where.GetEntityCell(witness);
            return origin!=null&&cell!=null&&cell.Explored&&cell.IsVisible&&cell.Occupants.Contains(witness)
                &&Mathf.Abs(origin.X-cell.X)<=DangerRadius&&Mathf.Abs(origin.Y-cell.Y)<=DangerRadius
                &&witness.GetPart<RenderPart>()?.Visible==true&&witness.GetStatValue("Hitpoints",0)>0
                &&!CombatSystem.IsDeathHandled(witness)&&!BrainPart.ArePartyAligned(listener,witness)&&FactionManager.IsHostile(witness,listener);
        }
        void OnDisable()=>Release();
        void OnDestroy()=>Release();
        void Release(){Soundscape=null;router?.Dispose();router=null;Player?.Dispose();Player=null;zone=null;actor=null;map=null;tick=int.MinValue;dangerHold=0;observedDanger=false;dangerWitness=null;wasWorldVisible=false;regionalContextValid=true;}
    }
}
