using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
namespace CavesOfOoo.Scenarios.Custom
{
 public sealed partial class ReferenceGladeNativePlayer
 {
  static readonly string[] AudioPolish12Checks={"audio12_generated_alembic","audio12_real_reagent_harvest","audio12_original_reagent_consumed","audio12_real_tonic","audio12_imported_preparation","audio12_existing_station_clock","audio12_fixed_pool","audio12_preferences_restored"};
  const string AudioPolish12CanVerify="Existing generated FieldAlembic, ordinary harvesting of its actual StoneburrPatch, selection and BrewMix through native menus, consumed ingredient and real binding tonic, one committed command receipt with imported vessel-preparation source samples, preserved station clock and fixed16 audio sources.";
  const string AudioPolish12CannotVerify="One disclosed existing BiomeTravel shortcut reaches the unchanged generated alembic; this does not prove natural discovery or travel balance. No reagent, still, product, outcome, HP or enemy changes. Real hostiles/scheduler remain. Single/batch/partial/mishap semantics have separate unit coverage; this live run demonstrates one ordinary binding preparation. Neutral cork/vial handling does not imply success fanfare or continuous boiling. Source samples are not subjective listening, physical output or Deck performance; saved preferences are untouched.";
  IEnumerator RunAudioPolish12()
  {
   StartUtilityNative("audio12_preferences_restored",WorldAudioCue.BrewPrepare);
   var actor=_input.PlayerEntity;var inventory=actor.GetPart<InventoryPart>();
   Require(BiomeManager.WorldSeed==64&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled,"ordinary unchanged seed64 actor");
   Entity still;Cell approach;var zone=SpecialistSite("FieldAlembic","AlchemyStill",out still,out approach);
   Require(zone!=null&&still!=null,"actual generated station with safe original approach");
   yield return BiomeTravel(zone,approach,"audio12: unchanged generated alembic; stock, hostiles and result remain ordinary");
   var patch=zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="StoneburrPatch");
   Check("audio12_generated_alembic",BiomeDrawn(still)&&patch.HasPart<HarvestablePart>());
   var near=SpecialistApproach(zone,patch);Require(near!=null,"safe existing harvest approach");yield return WalkTo(near.X,near.Y);
   int before=ResidentUnits(actor,"StoneburrSeed");yield return BiomeWorldAction(patch,"Harvest");yield return ResidentCloseMenus();
   int harvested=ResidentUnits(actor,"StoneburrSeed");
   Check("audio12_real_reagent_harvest",patch.GetPart<HarvestablePart>().Harvested&&zone.GetEntityCell(patch)==null&&harvested-before>=1&&harvested-before<=2);
   near=SpecialistApproach(zone,still);Require(near!=null,"safe original station approach");yield return WalkTo(near.X,near.Y);
   foreach(var marked in inventory.Objects.Where(e=>e.HasPart<ReagentPart>()&&CraftingMarkPart.IsMarked(e)).ToArray())
    yield return BiomeWorldAction(still,CraftingMarkPart.ToggleCommandPrefix+marked.ID);
   var reagent=inventory.Objects.First(e=>e.BlueprintName=="StoneburrSeed");
   yield return BiomeWorldAction(still,CraftingMarkPart.ToggleCommandPrefix+reagent.ID);
   yield return Capture("brew-01-actual-reagent-selection");
   int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor),receipts=_waterNativeReceiptCount;
   BeginWaterNative(null,null,WorldAudioCue.BrewPrepare);
   yield return BiomeWorldAction(still,"BrewMix");yield return FinishWaterNative("audio12_imported_preparation");yield return ResidentCloseMenus();
   Check("audio12_original_reagent_consumed",ResidentUnits(actor,"StoneburrSeed")==harvested-1);
   var brew=inventory.Objects.Single(e=>e.HasPart<BrewItemPart>());
   Check("audio12_real_tonic",brew.GetPart<BrewItemPart>().Form=="Tonic"&&brew.GetPart<PhysicsPart>().InInventory==actor);
   Check("audio12_existing_station_clock",_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(actor)==energy);
   Check("audio12_fixed_pool",_waterNativeReceiptCount==receipts+1&&_waterNativeRuntime.Player.AllocatedSources==16
    &&_waterNativeRuntime.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
   yield return Capture("brew-02-earned-binding-tonic");
  }
 }
}
