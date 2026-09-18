using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Reject incomplete/stale native evidence before an Editor run can be labeled successful.</summary>
    public sealed class VoxelWorldIntegrationAdversarialTests
    {
        const string Run="1aaad6c4ac50415c9b4cb43b642c37cf",Root="/tmp/private-voxel-report-fixture";
        static readonly string[] Required={"native_N_real_new_game","native_south_entry","authored_native_owner_contract","native_presenter_binding",
            "physical_edge_selection_and_hole","native_physical_edge_pick","physical_edge_reach","one_structural_damage_owner",
            "destroy_clears_whole_body_once","duplicate_destroy_is_idempotent","destroyed_owner_view_removed","large_creature_path_respects_full_body",
            "large_creature_forced_motion_and_restoration","large_creature_blocked_far_edge_atomic",
            "pipe_force_move_whole_body","pipe_blocked_displacement_atomic","pipe_native_drag_preserves_grip","pipe_not_takeable_or_throwable",
            "revisit_preserves_damage_absence_and_pipe","native_F5_preserves_player_boundary","postcheckpoint_countermutation",
            "native_F6_exact_owner_and_tile_state","native_F6_replaces_all_owner_aliases","native_pilot_80second_profile_complete",
            "private_boot_marker_unchanged","owned_save_only",
            "morrowfast_quest_owners_preserved","morrowfast_starting_garden_preserved","native_guard_equipment_and_animation_rig",
            "morrowfast_revisit_retains_quest_owner_references","pilot_four_chunk_revisit_preserves_damage",
            "duplicate_body_contacts_resolve_one_damage_owner","four_connected_voxel_chunks_captured",
            "all_six_directed_native_links","four_chunks_still_cached_after_real_save_load","native_starter_cast_retains_voxel_rig_and_effects",
            "voxel_chunk_Overworld.2.6.0","voxel_chunk_Overworld.3.6.0","voxel_chunk_Overworld.3.7.0","voxel_chunk_Overworld.4.7.0"};
        static bool Validate(Receipt receipt,string run=Run,string root=Root)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldNativeAuditBatch")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type,"Explicit native pilot audit must exist before its receipt can be accepted.");
            var method=type.GetMethod("ValidateReceiptMetadata",BindingFlags.NonPublic|BindingFlags.Static);Assert.NotNull(method);
            return (bool)method.Invoke(null,new object[]{JsonUtility.ToJson(receipt),run,root});
        }
        static Receipt Good()
        {
            var phases=new[]{"pristine_idle","pristine_walk","restored_idle","restored_walk"}.Select(name=>new Phase
                {kind=name,zoneId="Overworld.3.7.0",seconds=20,frames=1000,successfulSteps=name.EndsWith("walk")?10:0,inputAttempts=name.EndsWith("walk")?10:0,
                 startTick=4,endTick=name.EndsWith("walk")?14:4,startX=40,endX=40,startY=12,endY=12}).ToArray();
            return new Receipt{runId=Run,saveRoot=Root,worldSeed=729490642,screenWidth=1920,screenHeight=1080,nativeSteps=22,
                workloadComplete=true,shutdownObserved=true,shutdownRootHeld=true,shutdownSavingUnregistered=true,displayPreferencesRestored=true,inputSettingsRestored=true,
                checks=Required.Select(name=>new Check{name=name,pass=true}).ToArray(),
                region=new[]{"Overworld.2.6.0","Overworld.3.6.0","Overworld.3.7.0","Overworld.4.7.0"}.Select(id=>new Region{zoneId=id,playerId="real-player",entities=2000,visible=100,swapped=10,active=true,playerVisible=true}).ToArray(),
                borders=new[]{"Overworld.2.6.0>Overworld.3.6.0","Overworld.3.6.0>Overworld.2.6.0", "Overworld.3.6.0>Overworld.3.7.0","Overworld.3.7.0>Overworld.3.6.0", "Overworld.3.7.0>Overworld.4.7.0","Overworld.4.7.0>Overworld.3.7.0"}.Select(edge=>new Border{from=edge.Split('>')[0],to=edge.Split('>')[1],pass=true,tickBefore=2,tickAfter=3}).ToArray(),
                profile=new Profile{runId=Run,saveRoot=Root,mode="full",valid=true,complete=true,frames=4000,steadySeconds=80,
                    worldSeed=729490642,screenWidth=1920,screenHeight=1080,phases=phases,rawFrames="owned-frames.csv.gz",rawSha256=new string('a',64)}};
        }
        [Test] public void CompleteNativeReceiptMetadataAcceptsTheActualJsonRoundtrip()=>Assert.IsTrue(Validate(Good()));
        [TestCase("run")][TestCase("root")][TestCase("seed")][TestCase("resolution")][TestCase("failures")][TestCase("unexpected")]
        [TestCase("workload")][TestCase("shutdown")][TestCase("shutdownRoot")][TestCase("shutdownSaving")][TestCase("displayRestore")][TestCase("inputRestore")]
        [TestCase("nativeSteps")][TestCase("missingCheck")][TestCase("failedCheck")][TestCase("duplicateCheck")][TestCase("missingProfile")]
        [TestCase("profileRun")][TestCase("profileRoot")][TestCase("profileComplete")][TestCase("profileValid")][TestCase("profileOverflow")]
        [TestCase("profileSeconds")][TestCase("profileFrames")][TestCase("profileResolution")][TestCase("profileMissingPhase")]
        [TestCase("profileDuplicatePhase")][TestCase("profileIdleMoved")][TestCase("profileNoWalk")][TestCase("profileShortPhase")][TestCase("profileRaw")]
        [TestCase("missingLargeMove")][TestCase("missingLargeBlock")]
        [TestCase("noRegion")][TestCase("missingChunk")][TestCase("duplicateChunk")][TestCase("outsideChunk")]
        [TestCase("inactiveVoxel")][TestCase("zeroReplacement")][TestCase("missingReplacement")][TestCase("fullReveal")]
        [TestCase("hiddenPlayer")][TestCase("noVisibleCells")][TestCase("allVisibleCells")]
        [TestCase("noBorders")][TestCase("missingBorder")][TestCase("duplicateBorder")][TestCase("outsideBorder")]
        [TestCase("failedBorder")][TestCase("borderNoTurn")][TestCase("borderReversedTime")]
        public void Adversarial_OneBrokenEvidenceInvariantCannotBeReportedAsComplete(string mutation)
        {
            Assert.IsTrue(Validate(Good()),"Identical complete metadata is the positive control.");var r=Good();
            switch(mutation)
            {
                case "noRegion":r.region=null;break;
                case "missingChunk":r.region=r.region.Skip(1).ToArray();break;
                case "duplicateChunk":r.region[3].zoneId=r.region[0].zoneId;break;
                case "outsideChunk":r.region[3].zoneId="Overworld.4.6.0";break;
                case "inactiveVoxel":r.region[0].active=false;break;
                case "zeroReplacement":r.region[0].swapped=0;break;
                case "missingReplacement":r.region[0].missing=1;break;
                case "fullReveal":r.region[0].fullReveal=true;break;
                case "hiddenPlayer":r.region[0].playerVisible=false;break;
                case "noVisibleCells":r.region[0].visible=0;break;
                case "allVisibleCells":r.region[0].visible=2000;break;
                case "noBorders":r.borders=null;break;
                case "missingBorder":r.borders=r.borders.Skip(1).ToArray();break;
                case "duplicateBorder":r.borders[5]=r.borders[0];break;
                case "outsideBorder":r.borders[0].to="Overworld.4.6.0";break;
                case "failedBorder":r.borders[0].pass=false;break;
                case "borderNoTurn":r.borders[0].tickAfter=r.borders[0].tickBefore;break;
                case "borderReversedTime":r.borders[0].tickAfter=1;break;
                case "run":r.runId=Guid.NewGuid().ToString("N");break;
                case "root":r.saveRoot="/tmp/some-other-root";break;
                case "seed":r.worldSeed++;break;
                case "resolution":r.screenWidth=1280;break;
                case "failures":r.failures=1;break;
                case "unexpected":r.unexpectedErrors=1;break;
                case "workload":r.workloadComplete=false;break;
                case "shutdown":r.shutdownObserved=false;break;
                case "shutdownRoot":r.shutdownRootHeld=false;break;
                case "shutdownSaving":r.shutdownSavingUnregistered=false;break;
                case "displayRestore":r.displayPreferencesRestored=false;break;
                case "inputRestore":r.inputSettingsRestored=false;break;
                case "nativeSteps":r.nativeSteps=0;break;
                case "missingCheck":r.checks=r.checks.Skip(1).ToArray();break;
                case "failedCheck":r.checks[0].pass=false;break;
                case "duplicateCheck":r.checks=r.checks.Concat(new[]{r.checks[0]}).ToArray();break;
                case "missingProfile":r.profile=null;break;
                case "profileRun":r.profile.runId=Guid.NewGuid().ToString("N");break;
                case "profileRoot":r.profile.saveRoot="wrong";break;
                case "profileComplete":r.profile.complete=false;break;
                case "profileValid":r.profile.valid=false;break;
                case "profileOverflow":r.profile.overflow=true;break;
                case "profileSeconds":r.profile.steadySeconds=79;break;
                case "profileFrames":r.profile.frames=0;break;
                case "profileResolution":r.profile.badResolutionFrames=1;break;
                case "profileMissingPhase":r.profile.phases=r.profile.phases.Skip(1).ToArray();break;
                case "profileDuplicatePhase":r.profile.phases[3].kind=r.profile.phases[0].kind;break;
                case "profileIdleMoved":r.profile.phases[0].endX++;break;
                case "profileNoWalk":r.profile.phases[1].successfulSteps=0;break;
                case "profileShortPhase":r.profile.phases[0].seconds=19;break;
                case "profileRaw":r.profile.rawSha256=null;break;
                case "missingLargeMove":r.checks=r.checks.Where(c=>c.name!="large_creature_forced_motion_and_restoration").ToArray();break;
                case "missingLargeBlock":r.checks=r.checks.Where(c=>c.name!="large_creature_blocked_far_edge_atomic").ToArray();break;
            }
            Assert.IsFalse(Validate(r),mutation);
        }
        [Test] public void WrongExpectedOwnershipCannotBorrowAnotherRunsSuccess()
        {
            Assert.IsTrue(Validate(Good()));Assert.IsFalse(Validate(Good(),Guid.NewGuid().ToString("N"),Root));
            Assert.IsFalse(Validate(Good(),Run,"/tmp/wrong-root"));Assert.IsFalse(Validate(Good(),"",Root));
        }
        [Serializable] sealed class Receipt
        {public string runId,saveRoot;public int worldSeed,screenWidth,screenHeight,nativeSteps,failures,unexpectedErrors;
         public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored,inputSettingsRestored;
         public Check[] checks;public Profile profile;public Region[] region;public Border[] borders;}
        [Serializable] sealed class Region{public string zoneId,playerId;public int entities,visible,swapped,missing;public bool active,fullReveal,playerVisible;}
        [Serializable] sealed class Border{public string from,to;public bool pass;public int tickBefore,tickAfter;}
        [Serializable] sealed class Check{public string name;public bool pass;}
        [Serializable] sealed class Profile
        {public string runId,saveRoot,mode,rawFrames,rawSha256;public bool valid,complete,overflow;public int frames,badResolutionFrames,worldSeed,screenWidth,screenHeight;public double steadySeconds;public Phase[] phases;}
        [Serializable] sealed class Phase
        {public string kind,zoneId;public double seconds;public int frames,successfulSteps,inputAttempts,startTick,endTick,startX,endX,startY,endY;}
    }
}
