using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        static readonly string[] AudioPolish06Checks={"animal_original_ready_rime","animal_generated_live_source","animal_awake_imported_call",
            "animal_real_rime_commits_freeze","animal_frozen_call_stays_quiet","animal_freeze_quiet_keeps_scan_budget","animal_actual_waits_thaw",
            "animal_awake_call_returns","animal_ordinary_finish","animal_audio_preferences_restored"};
        const string AudioPolish06CanVerify="An actual generated approved frog calls through an imported playing source; the ordinary starter's real Rime Grip hotbar/direction input spends its action and cooldown and freezes that exact living animal; no call while the real effect remains; ordinary paid waits thaw it and allow its call to return. Eleven checks include shared ordinary boot.";
        const string AudioPolish06CannotVerify="One disclosed exact-actor travel shortcut reaches a bounded selected safe ray toward an original frog. The frog may be hostile; ordinary AI, retaliation, spell cost and damage remain active. No animal, spell, inventory, HP, effect, cooldown, FOV or clock is granted or reset. Route selection is not natural discovery/balance. Live proof is frozen/waking frog only; gas sleep/hibernation/species variants have separate native unit tests. Noncontiguous source samples do not prove listening quality, physical speakers, global clipping or Deck performance.";
        sealed class Animal06Site{public Zone Zone;public Entity Animal;public Cell Approach;public int Dx,Dy;}
        IEnumerator RunAudioPolish06AnimalRest()
        {
            float old=WorldAudioSettings.AmbienceVolume;
            _audioPolishCleanup=()=>{WorldAudioSettings.AmbienceVolume=old;Check("animal_audio_preferences_restored",WorldAudioSettings.AmbienceVolume==old);};
            WorldAudioSettings.AmbienceVolume=.6f;Require(!AudioListener.pause,"normal isolated listener");
            var actor=_input.PlayerEntity;string actorId=actor.ID;var abilities=actor.GetPart<ActivatedAbilitiesPart>();int slot=-1;ActivatedAbility rime=null;
            for(int i=0;i<ActivatedAbilitiesPart.SlotCount;i++)if(abilities?.GetAbilityBySlot(i)?.Command=="CommandRimeGrip"){slot=i;rime=abilities.GetAbilityBySlot(i);break;}
            Check("animal_original_ready_rime",rime!=null&&slot>=0&&rime.CooldownRemaining==0&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>());
            Require(rime!=null&&slot>=0&&rime.CooldownRemaining==0,"original ready ability; no learned ability or cooldown rewrite");
            var site=Animal06FindSite();Require(site!=null,"bounded unchanged generated frog with exact clear Rime ray and no competing animal voice");
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"existing bootstrap audio");
            int tick=_input.TurnManager.TickCount,hp=actor.GetStatValue("Hitpoints"),energy=_input.TurnManager.GetEnergy(actor);string gear=CombatGear(actor);
            Require(Animal06Safe(site.Zone,site.Approach,site.Animal),"legal approach while other hostiles remain distant");
            Require(_input.CurrentZone.TryTransferEntityTo(actor,site.Zone,site.Approach.X,site.Approach.Y),"disclosed exact-actor travel");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=site.Zone,NewPlayerX=site.Approach.X,NewPlayerY=site.Approach.Y}});
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("AudioPolish06Travel");yield return null;yield return new WaitForEndOfFrame();
            Require(actor.GetStatValue("Hitpoints")==hp&&CombatGear(actor)==gear&&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(actor)==energy,"travel preserves actor state/time");
            _descriptions.Add(new Description{subject="Explicit animal-audio travel shortcut",source=site.Zone.ZoneID,text="Unchanged generated "+site.Animal.BlueprintName+" exact owner="+site.Animal.ID+"; legal ray approach="+Cell().X+","+Cell().Y});
            var at=site.Zone.GetEntityCell(site.Animal);
            Check("animal_generated_live_source",at?.IsVisible==true&&at.Explored&&!at.IsInterior&&site.Animal.GetStatValue("Hitpoints")>Cryomancy_RimeGrip.GRIP_DAMAGE
                &&site.Animal.GetEffect<FrozenEffect>()?.Cold>0!=true&&SkillLine.Collect(site.Zone,actor,Cell().X,Cell().Y,site.Dx,site.Dy,Cryomancy_RimeGrip.GRIP_RANGE).FirstOrDefault()==site.Animal);
            var library=WorldAudioLibrary.LoadResources();var clips=new[]{library.Detail("frog",0)?.Clip,library.Detail("frog",1)?.Clip};Require(clips.All(c=>c!=null),"actual imported frog palette");
            Require(Animal06OnlyFrog(site),"exact original frog is the only audible approved frog at listener");
            yield return Animal06Voice(audio,clips,"animal_awake_imported_call");yield return Capture("animal-01-original-awake-call");
            Require(SkillLine.Collect(site.Zone,actor,Cell().X,Cell().Y,site.Dx,site.Dy,Cryomancy_RimeGrip.GRIP_RANGE).FirstOrDefault()==site.Animal,"same actual selected nearest target before cast");
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);int animalHP=site.Animal.GetStatValue("Hitpoints");
            yield return Tap((Key)Enum.Parse(typeof(Key),slot==9?"Digit0":"Digit"+(slot+1)));Require(State()=="AwaitingDirection","real Rime direction mode");
            yield return Tap(Direction(site.Dx,site.Dy));yield return CombatWaitForFx();yield return ResidentCloseMenus();
            var frozen=site.Animal.GetEffect<FrozenEffect>();
            Check("animal_real_rime_commits_freeze",frozen?.Owner==site.Animal&&frozen.Cold>0&&rime.CooldownRemaining>0&&site.Animal.GetStatValue("Hitpoints")>0
                &&site.Animal.GetStatValue("Hitpoints")<animalHP&&River03OneTurn(tick,energy));
            Require(frozen?.Cold>0&&site.Zone.GetEntityCell(site.Animal)?.IsVisible==true&&Animal06OnlyFrog(site),"real current freeze and visible living target are mandatory");
            // The FX waiter resumes between Update and LateUpdate. Let the completed
            // paid turn reach WorldAudioRuntime before measuring strictly idle work.
            yield return new WaitForEndOfFrame();
            Require((int)Field(audio,"tick")==_input.TurnManager.TickCount
                &&ReferenceEquals(Field(audio,"zone"),site.Zone)&&ReferenceEquals(Field(audio,"actor"),actor)
                &&_input.ZoneRenderer?.Paused!=true&&State()=="Normal","completed cast context sampled after current FOV");
            int scans=audio.Soundscape.ScanCount,contexts=audio.ContextCheckCount,quietTick=_input.TurnManager.TickCount;
            bool heard=false;double until=Time.realtimeSinceStartupAsDouble+28;
            while(Time.realtimeSinceStartupAsDouble<until)
            {
                Require(site.Animal.GetEffect<FrozenEffect>()==frozen&&frozen.Cold>0&&site.Zone.GetEntityCell(site.Animal)?.IsVisible==true,"frozen source persists through no-input observation");
                heard|=audio.Player.Root.GetComponentsInChildren<AudioSource>().Any(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.volume>0&&clips.Contains(s.clip));yield return null;
            }
            Check("animal_frozen_call_stays_quiet",!heard);
            Check("animal_freeze_quiet_keeps_scan_budget",audio.Soundscape.ScanCount==scans&&audio.ContextCheckCount==contexts
                &&_input.TurnManager.TickCount==quietTick&&audio.Soundscape.LastVisitedOwners<=512&&audio.Player.AllocatedSources==16);
            _descriptions.Add(new Description{subject="Post-cast idle audio work",source=site.Zone.ZoneID,text="After completed-frame boundary: scans="+scans+" -> "+audio.Soundscape.ScanCount
                +"; context checks="+contexts+" -> "+audio.ContextCheckCount+"; tick="+quietTick+" -> "+_input.TurnManager.TickCount
                +"; last visited owners="+audio.Soundscape.LastVisitedOwners+"; allocated sources="+audio.Player.AllocatedSources+"; no input for28s."});
            yield return Capture("animal-02-real-frozen-quiet");int waits=0;
            while(site.Animal.GetEffect<FrozenEffect>()?.Cold>0&&waits<12)
            {Require(actor.GetStatValue("Hitpoints")>10&&site.Animal.GetStatValue("Hitpoints")>0,"bounded genuine live thaw, no forced effect removal");yield return Tap(Key.Period);yield return CombatWaitForFx();yield return ResidentCloseMenus();waits++;}
            Check("animal_actual_waits_thaw",waits>0&&waits<=12&&site.Animal.GetEffect<FrozenEffect>()?.Cold>0!=true&&site.Animal.GetStatValue("Hitpoints")>0&&site.Zone.GetEntityCell(site.Animal)?.IsVisible==true);
            Require(site.Animal.GetStatValue("Hitpoints")>0&&site.Animal.GetEffect<FrozenEffect>()?.Cold>0!=true,"same original animal truly thawed");
            Require(Animal06OnlyFrog(site),"same thawed original frog remains the sole eligible frog, no replacement audio owner");
            yield return Animal06Voice(audio,clips,"animal_awake_call_returns");yield return Capture("animal-03-original-animal-awake-again");
            Check("animal_ordinary_finish",actor.ID==actorId&&actor.GetStatValue("Hitpoints")>0&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&State()=="Normal");
        }
        IEnumerator Animal06Voice(WorldAudioRuntime audio,AudioClip[] clips,string check)
        {
            AudioSource voice=null;double until=Time.realtimeSinceStartupAsDouble+45;
            while(voice==null&&Time.realtimeSinceStartupAsDouble<until)
            {voice=audio.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.volume>0&&clips.Contains(s.clip));yield return null;}
            float peak=0;int count=0;var samples=new float[1024];until=Time.realtimeSinceStartupAsDouble+.35;
            while(voice!=null&&voice.isPlaying&&Time.realtimeSinceStartupAsDouble<until)
            {voice.GetOutputData(samples,0);foreach(float sample in samples){peak=Mathf.Max(peak,Mathf.Abs(sample));count++;}yield return null;}
            Check(check,voice!=null&&clips.Contains(voice.clip)&&peak>.000001f&&count>0);
        }
        Animal06Site Animal06FindSite()
        {
            foreach(var zone in BiomeManager.CachedZones.Values.ToArray()){var site=Animal06SiteIn(zone);if(site!=null)return site;}
            int visited=0;for(int y=0;y<WorldMap.Height&&visited<48;y++)for(int x=0;x<WorldMap.Width&&visited<48;x++)
            {
                if(BiomeManager.WorldMap.GetBiome(x,y)!=BiomeType.Sodden||BiomeManager.WorldMap.GetPOI(x,y)!=null)continue;
                visited++;var site=Animal06SiteIn(BiomeManager.GetZone(WorldMap.ToZoneID(x,y,0)));if(site!=null)return site;
            }
            return null;
        }
        Animal06Site Animal06SiteIn(Zone zone)
        {
            if(zone==null||WorldMap.FromZoneID(zone.ZoneID).z!=0)return null;var actor=_input.PlayerEntity;
            foreach(var animal in zone.GetReadOnlyEntities().Where(e=>Animal06Frog(e)&&e.GetStatValue("Hitpoints")>Cryomancy_RimeGrip.GRIP_DAMAGE
                &&e.GetEffect<FrozenEffect>()?.Cold>0!=true&&e.GetPart<RenderPart>()?.Visible==true))
            {
                var at=zone.GetEntityCell(animal);if(at==null||at.IsInterior)continue;
                foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                {
                    var near=zone.GetCell(at.X-4*d.Item1,at.Y-4*d.Item2);if(!Animal06Safe(zone,near,animal))continue;
                    if(zone.GetReadOnlyEntities().Any(e=>e!=animal&&(Animal06Frog(e)||e.BlueprintName=="PetDog"||e.BlueprintName=="Magpie")&&SpatialQuery.DistanceToCell(zone,e,near.X,near.Y)<=6))continue;
                    if(SkillLine.Collect(zone,actor,near.X,near.Y,d.Item1,d.Item2,Cryomancy_RimeGrip.GRIP_RANGE).FirstOrDefault()!=animal)continue;
                    return new Animal06Site{Zone=zone,Animal=animal,Approach=near,Dx=d.Item1,Dy=d.Item2};
                }
            }
            return null;
        }
        bool Animal06OnlyFrog(Animal06Site site)
        {
            var from=Cell();var owners=site.Zone.GetReadOnlyEntities().Where(e=>Animal06Frog(e)&&e.GetStatValue("Hitpoints")>0
                &&e.GetPart<RenderPart>()?.Visible==true&&site.Zone.GetEntityCell(e)?.IsVisible==true
                &&site.Zone.GetEntityCell(e).IsInterior==from.IsInterior&&SpatialQuery.DistanceToCell(site.Zone,e,from.X,from.Y)<=6).ToArray();
            return owners.Length==1&&owners[0]==site.Animal;
        }
        static bool Animal06Frog(Entity e)=>e.BlueprintName=="Bandfrog"||e.BlueprintName=="Reedfrog"||e.BlueprintName=="GinFrog"||e.BlueprintName=="MawToad"||e.BlueprintName=="HelmwoodFrog";
        bool Animal06Safe(Zone zone,Cell cell,Entity selected)=>cell!=null&&!cell.IsInterior&&!cell.BlocksMovement(_input.PlayerEntity)
            &&!cell.Occupants.Any(e=>e.HasTag("Creature")||e.HasTag("Trap")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>())
            &&zone.TileState.Get(cell.X,cell.Y)?.Coatings?.Any(c=>c.Turns>0)!=true&&zone.TileState.Heat(cell.X,cell.Y)<=0
            &&!zone.GetReadOnlyEntities().Any(e=>e!=selected&&e!=_input.PlayerEntity&&e.HasTag("Creature")&&FactionManager.IsHostile(e,_input.PlayerEntity)&&SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)<=12);
    }
}
