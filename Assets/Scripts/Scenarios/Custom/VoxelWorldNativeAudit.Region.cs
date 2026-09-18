using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    // Adds four-chunk coverage to the preserved native pilot mechanics workload.
    public sealed partial class VoxelWorldNativeAudit
    {
        readonly List<RegionRow> region = new List<RegionRow>();
        readonly List<BorderRow> borders = new List<BorderRow>();
        readonly Dictionary<string, Zone> visited = new Dictionary<string, Zone>();
        int spellPeakMeshes, spellFrames;
        bool observingSpell;
        Transform[] observedBones;
        Quaternion[] startingBones;
        float spellPeakBoneDegrees;

        IEnumerator AuditTownAndWest()
        {
            yield return RecordChunk("spawn-town");
            var town = input.CurrentZone;
            var required = new[] { "rope-reserve-coil", "north-oath-arch", "guest-stool-south", "guest-bed-west", "guest-bed-east" };
            var questOwners = required.Select(id => MorrowfastSceneRuntime.FindOwner(town, id)).ToArray();
            Check("morrowfast_quest_owners_preserved", questOwners.All(e => e != null) && questOwners.Distinct().Count() == required.Length);
            var garden = new[] { (41,21), (41,22), (41,23), (42,21), (42,22), (42,23) };
            Check("morrowfast_starting_garden_preserved", garden.All(p => !town.GetCell(p.Item1,p.Item2).BlocksMovement(input.PlayerEntity)
                && town.GetCell(p.Item1,p.Item2).Objects.Count(e => e.ID=="morrowfast-terrain:"+p.Item1+":"+p.Item2
                    && e.BlueprintName=="TepuiStone" && e.HasTag("Plantable") && e.HasTag(MorrowfastSceneRuntime.TerrainTag))==1),
                "Read-only six reserved native garden cells; this audit does not invoke Ensure to repair missing content.");
            var village = input.ZoneRenderer.Village3D;
            Require(village.TryGetOwnerView("north-guard-west", out var guard, out var guardRoot) && guard != null && guardRoot != null,
                "The native named guard, rather than a generic replacement NPC, owns the view.");
            FixturePlace(town,NearestReachableEdge(town,guard));yield return WaitForVoxel();
            var equipped = guard.GetPart<InventoryPart>()?.GetAllEquipped().ToArray();
            Require(equipped != null && equipped.Length > 0, "Native guard has real equipment before testing attachments.");
            int attachments = equipped.Count(item => village.TryGetEquipmentView(guard, item, out var view) && view != null && view.transform.IsChildOf(guardRoot.transform));
            var guardAnimator = guardRoot.GetComponentInChildren<Animator>(true);
            Check("native_guard_equipment_and_animation_rig", attachments > 0 && village.IsRenderedEntity(guard) && guardAnimator != null
                && guardAnimator.runtimeAnimatorController != null && guardAnimator.HasState(0, Animator.StringToHash("Walk"))
                && guardAnimator.HasState(0, Animator.StringToHash("Attack")),
                "Native equipped item attachment under its exact actor; controller states are a capability gate, separate from the evaluated spell gesture below.");
            yield return Cross(Id(2,6), Key.A);
            yield return RecordChunk("western-compost-field");
            yield return Cross(Id(3,6), Key.D);
            Check("morrowfast_revisit_retains_quest_owner_references", ReferenceEquals(town,input.CurrentZone)
                && required.Select((id,i) => ReferenceEquals(questOwners[i],MorrowfastSceneRuntime.FindOwner(town,id))).All(v=>v));
            FixturePlace(input.CurrentZone,(40,23));
            yield return WaitForVoxel();
        }

        IEnumerator AuditEastAndReturn()
        {
            yield return Cross(Id(4,7), Key.D);
            yield return RecordChunk("eastern-grove");
            yield return Cross(Id(3,7), Key.A);
            yield return WaitForRing();
            Check("pilot_four_chunk_revisit_preserves_damage", ExpectedState(input.CurrentZone));
        }

        // Only fixture-position the existing player onto a naturally clear seam.
        // The actual cross-zone operation below is one ordinary keyboard input.
        IEnumerator Cross(string targetId, Key key)
        {
            var source=input.CurrentZone; var target=input.ZoneManager.GetZone(targetId);
            bool horizontal=key==Key.A||key==Key.D;
            bool positive=key==Key.D||key==Key.S;
            (int x,int y) at=(-1,-1), arrival=(-1,-1);
            int length=horizontal?Zone.Height:Zone.Width;
            for(int distance=0;distance<length && at.x<0;distance++)
                for(int n=1;n<length-1;n++)
                {
                    if(Math.Abs(n-length/2)!=distance)continue;
                    var a=horizontal?(positive?Zone.Width-1:0,n):(n,positive?Zone.Height-1:0);
                    var b=horizontal?(positive?0:Zone.Width-1,n):(n,positive?0:Zone.Height-1);
                    if(source.CanPlaceFootprint(input.PlayerEntity,a.Item1,a.Item2)
                        && target.CanPlaceFootprint(input.PlayerEntity,b.Item1,b.Item2)) {at=a;arrival=b;break;}
                }
            Require(at.x>=0,"Both sides of the native seam have an unmodified, exactly aligned free lane: "+source.ZoneID+">"+targetId);
            FixturePlace(source,at); yield return WaitForVoxel();
            var player=input.PlayerEntity; int tick=input.TurnManager.TickCount;
            yield return Tap(key);nativeSteps++;
            bool pass=ReferenceEquals(input.CurrentZone,target) && ReferenceEquals(player,input.PlayerEntity)
                && Position()==arrival && source.GetEntityCell(player)==null && PlayerReferences()==1
                && input.TurnManager.TickCount>tick && CurrentBindingIntact();
            borders.Add(new BorderRow{from=source.ZoneID,to=targetId,key=key.ToString(),pass=pass,
                fromX=at.x,fromY=at.y,toX=Position().x,toY=Position().y,tickBefore=tick,tickAfter=input.TurnManager.TickCount});
            Check("border_"+source.ZoneID+"_to_"+targetId,pass,"Actual key crossing; only seam positioning is an explicitly labelled fixture.");
            Require(pass,"Native seam traversal failed.");yield return WaitForVoxel();
        }

        IEnumerator WaitForVoxel()
        {
            double start=Time.realtimeSinceStartupAsDouble;
            while(true)
            {
                bool town=input.CurrentZone.ZoneID==Id(3,6);
                var v=input.ZoneRenderer.Village3D;var r=Presenter();
                bool ready=town?v!=null&&v.IsReady&&v.PresentationVisible&&v.VoxelPresentationActive
                    :r!=null&&r.IsReady&&r.PresentationVisible&&r.VoxelPresentationActive;
                if(ready && input.CurrentZone.GetCell(Position().x,Position().y).IsVisible)break;
                Require(Time.realtimeSinceStartupAsDouble-start<12,"Current-zone voxel mesh replacement failed to become ready.");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(.4f);
        }

        IEnumerator RecordChunk(string label)
        {
            yield return WaitForVoxel();var zone=input.CurrentZone;bool town=zone.ZoneID==Id(3,6);
            var v=input.ZoneRenderer.Village3D;var r=Presenter();
            int visible=0,explored=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {var cell=zone.GetCell(x,y);if(cell.IsVisible)visible++;if(cell.Explored)explored++;}
            var row=new RegionRow{zoneId=zone.ZoneID,playerId=input.PlayerEntity.ID,entities=zone.EntityCount,
                creatures=zone.GetEntitiesWithTag("Creature").Count(),visible=visible,explored=explored,
                swapped=town?v.VoxelAppliedMeshCount:r.VoxelAppliedMeshCount,missing=town?v.VoxelMissingMeshCount:r.VoxelMissingMeshCount,
                active=town?v.VoxelPresentationActive:r.VoxelPresentationActive,
                fullReveal=town?v.FullReveal:r.FullReveal,playerVisible=town?v.IsRenderedEntity(input.PlayerEntity):r.IsRenderedEntity(input.PlayerEntity)};
            region.Add(row);visited[zone.ZoneID]=zone;
            Check("voxel_chunk_"+zone.ZoneID,row.active&&row.swapped>0&&row.missing==0&&!row.fullReveal
                &&row.playerVisible&&visible>0&&visible<Zone.Width*Zone.Height&&PlayerReferences()==1&&CurrentBindingIntact(),
                JsonUtility.ToJson(row));
            Require(checks.Last().pass,"Four-chunk voxel/fog coverage must be complete.");
            yield return Capture(label);
        }

        void RecordInitialSouthBorder(Zone source,int tick)
        {borders.Add(new BorderRow{from=source.ZoneID,to=input.CurrentZone.ZoneID,key="S",pass=checks.Last().pass,
            fromX=40,fromY=24,toX=Position().x,toY=Position().y,tickBefore=tick,tickAfter=input.TurnManager.TickCount});}
        void RecordRevisitNorthBorder(Zone source,int tick)
        {borders.Add(new BorderRow{from=source.ZoneID,to=input.CurrentZone.ZoneID,key="W",pass=input.CurrentZone.ZoneID==Id(3,6)&&PlayerReferences()==1&&CurrentBindingIntact(),
            fromX=40,fromY=0,toX=Position().x,toY=Position().y,tickBefore=tick,tickAfter=input.TurnManager.TickCount});}

        IEnumerator AuditNativeSpellGesture()
        {
            var zone=input.CurrentZone;FixturePlace(zone,NearestFree(zone,40,12));yield return WaitForRing();
            Require(Presenter().TryGetEntityView(input.PlayerEntity,out var root,out _)&&root!=null,"Live player model before starter cast.");
            var animator=root.GetComponentInChildren<Animator>(true);
            Require(animator!=null&&animator.runtimeAnimatorController!=null,"Player retains the actual animation controller.");
            observedBones=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s=>s.bones).Where(b=>b!=null).Distinct().ToArray();
            Require(observedBones.Length>0,"Imported skin has real bones; static voxel replacement is insufficient.");
            startingBones=observedBones.Select(b=>b.localRotation).ToArray();
            var abilities=input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();int slot=-1;
            for(int i=0;i<ActivatedAbilitiesPart.SlotCount;i++)if(abilities.GetAbilityBySlot(i)?.Command=="CommandFlamingHands"){slot=i;break;}
            Require(slot>=0&&abilities.GetAbilityBySlot(slot).CooldownRemaining==0,"Unmodified starter Flaming Hands is bound and ready.");
            var mode=SpellFxSettings.Mode;int tick=input.TurnManager.TickCount;var origin=Position();
            try
            {
                SpellFxSettings.Mode=SpellFxMode.Full;
                yield return Tap((Key)Enum.Parse(typeof(Key),slot==9?"Digit0":"Digit"+(slot+1)));
                Require(State()=="AwaitingDirection","Actual hotbar opened direction input.");
                observingSpell=true;yield return Tap(Key.D);
                double deadline=Time.realtimeSinceStartupAsDouble+8;
                while(input.ZoneRenderer.WorldFx.HasBlockingFx||State()=="WaitingForFxResolution")
                {Require(Time.realtimeSinceStartupAsDouble<deadline,"Native starter cast did not release input.");yield return null;}
                Check("native_starter_cast_retains_voxel_rig_and_effects",spellFrames>1&&spellPeakMeshes>0&&spellPeakBoneDegrees>3
                    &&input.TurnManager.TickCount>tick&&Position()==origin&&State()=="Normal"
                    &&input.ZoneRenderer.WorldFx.NativeRenderer.LastEntry?.SpellId=="Pyromancy_FlamingHands",
                    $"Ordinary hotbar+east input; frames={spellFrames}, nativeMeshes={spellPeakMeshes}, evaluatedBoneDegrees={spellPeakBoneDegrees}. Native terrain/actors in the cone retain normal consequences; no damage or RNG override.");
            }
            finally {observingSpell=false;SpellFxSettings.Mode=mode;}
        }
        void Update()
        {
            if(!observingSpell||input?.ZoneRenderer?.WorldFx?.NativeRenderer==null)return;
            spellFrames++;spellPeakMeshes=Math.Max(spellPeakMeshes,input.ZoneRenderer.WorldFx.NativeRenderer.ActiveCount);
            for(int i=0;i<observedBones.Length;i++)if(observedBones[i]!=null)
                spellPeakBoneDegrees=Math.Max(spellPeakBoneDegrees,Quaternion.Angle(startingBones[i],observedBones[i].localRotation));
        }
        void CompleteRegion()
        {
            var expected=new HashSet<string>(new[]{Id(2,6),Id(3,6),Id(3,7),Id(4,7)});
            Check("four_connected_voxel_chunks_captured",region.Count==4&&region.All(r=>expected.Remove(r.zoneId))&&expected.Count==0);
            var edges=new HashSet<string>(new[]{Id(3,6)+">"+Id(2,6),Id(2,6)+">"+Id(3,6),Id(3,6)+">"+Id(3,7),Id(3,7)+">"+Id(3,6),Id(3,7)+">"+Id(4,7),Id(4,7)+">"+Id(3,7)});
            Check("all_six_directed_native_links",borders.Count==6&&borders.All(b=>b.pass&&edges.Remove(b.from+">"+b.to))&&edges.Count==0);
            Check("four_chunks_still_cached_after_real_save_load",visited.Keys.All(id=>input.ZoneManager.CachedZones.ContainsKey(id)));
        }
        [Serializable] public sealed class RegionRow
        {public string zoneId,playerId;public int entities,creatures,visible,explored,swapped,missing;public bool active,fullReveal,playerVisible;}
        [Serializable] public sealed class BorderRow
        {public string from,to,key;public int fromX,fromY,toX,toY,tickBefore,tickAfter;public bool pass;}
    }
}
