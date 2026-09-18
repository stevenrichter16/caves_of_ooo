using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>RED-first acceptance receipts must prove this exact native run,
    /// all seven live casts, and four measured phases; stale/partial files cannot pass.</summary>
    public sealed class StarterSpell3DNativeAuditContractTests
    {
        const string Run = "1234567890abcdef1234567890abcdef";
        const string Root = "/tmp/coo-native-save-audits/1234567890abcdef1234567890abcdef";
        static bool Validate(Receipt report, string run=Run)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.StarterSpell3DNativeAuditBatch")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type,"Native starter-spell acceptance launcher must exist.");
            var method=type.GetMethod("ValidateReceiptMetadata",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.NotNull(method,"Native metadata gate must reject stale or incomplete acceptance evidence.");
            return (bool)method.Invoke(null,new object[]{JsonUtility.ToJson(report),run,Root});
        }
        static Receipt Good()
        {
            return new Receipt { runId=Run,saveRoot=Root,worldSeed=729490642,screenWidth=1920,screenHeight=1080,
                workloadComplete=true,shutdownObserved=true,shutdownRootHeld=true,shutdownSavingUnregistered=true,
                displayPreferencesRestored=true,inputSettingsRestored=true,
                checks=new[]{"native_N_real_new_game","native_keyboard_starter_cast","native_town_ready","native_south_entry","all_seven_command_outcomes","rime_denied_reduced_feedback","rain_empty_no_crop_mesh","paired_80second_profile_complete","private_boot_marker_unchanged","owned_save_only"}.Select(n=>new Check{name=n,pass=true}).ToArray(),
                phases=Enumerable.Range(0,4).Select(i=>new Phase{zoneId=i<2?"Overworld.3.6.0":"Overworld.3.7.0",mode=i%2==0?"Full":"Reduced",seconds=20.1,frames=1000,casts=8,activeFrames=200,maxMeshes=15}).ToArray(),
                spells=new[]{"Pyromancy_EmberSpit","Pyromancy_FlamingHands","Hydromancy_JetBlast","Galvanism_GroundSurge","Cryomancy_RimeGrip","Spellcraft_Calm","Hydromancy_ConjureRain"},
                rawFrames="raw.csv",rawSha256=new string('a',64),profileSeconds=80.4,profileFrames=4000,activeFrames=800,nativeKeyboardCasts=1};
        }
        [Test] public void ExactCompletedNativeRun_AcceptsMetadata()=>Assert.IsTrue(Validate(Good()));
        [Test] public void AnotherRun_RejectsStaleReceipt()=>Assert.IsFalse(Validate(Good(),"2234567890abcdef1234567890abcdef"));
        [Test] public void MissingPhase_RejectsShortCapture(){var r=Good();r.phases=r.phases.Take(3).ToArray();Assert.IsFalse(Validate(r));}
        [Test] public void DuplicateMode_RejectsUnpairedComparison(){var r=Good();r.phases[1].mode="Full";Assert.IsFalse(Validate(r));}
        [Test] public void StartupTimeCannotSubstituteForMeasuredProfile(){var r=Good();r.phases[0].seconds=1;r.profileSeconds=61.3;Assert.IsFalse(Validate(r));}
        [Test] public void NoActualNativeMeshes_RejectsIdleOnlyCapture(){var r=Good();r.phases[0].activeFrames=0;r.phases[0].maxMeshes=0;Assert.IsFalse(Validate(r));}
        [Test] public void MissingStartingSpell_RejectsPartialMatrix(){var r=Good();r.spells=r.spells.Take(6).ToArray();Assert.IsFalse(Validate(r));}
        [Test] public void NoOrdinaryKeyboardCast_RejectsSyntheticOnlyProof(){var r=Good();r.nativeKeyboardCasts=0;Assert.IsFalse(Validate(r));}
        [Test] public void FailedControl_RejectsOtherwiseCompleteReceipt(){var r=Good();r.checks[5].pass=false;Assert.IsFalse(Validate(r));}
        [Test] public void DuplicatePositiveCheck_RejectsPaddedEvidence(){var r=Good();r.checks[5].name=r.checks[0].name;Assert.IsFalse(Validate(r));}
        [Test] public void UnrestoredInput_RejectsCleanupFailure(){var r=Good();r.inputSettingsRestored=false;Assert.IsFalse(Validate(r));}
        [Test] public void UnexpectedLog_RejectsGreenLookingMetrics(){var r=Good();r.unexpectedErrors=1;Assert.IsFalse(Validate(r));}
        [Serializable] sealed class Receipt
        {public string runId,saveRoot,rawFrames,rawSha256;public string[] spells;public int failures,unexpectedErrors,worldSeed,screenWidth,screenHeight,profileFrames,activeFrames,nativeKeyboardCasts;public double profileSeconds;public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored,inputSettingsRestored;public Check[] checks;public Phase[] phases;}
        [Serializable] sealed class Check{public string name;public bool pass;}
        [Serializable] sealed class Phase{public string zoneId,mode;public double seconds;public int frames,casts,activeFrames,maxMeshes;}
    }
}
