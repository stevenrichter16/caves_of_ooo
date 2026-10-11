using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CavesOfOoo.Scenarios.Custom
{
 public sealed partial class ReferenceGladeNativePlayer
 {
  static readonly string[] AudioPolish13Checks={"audio13_original_dagger_equipped","audio13_metal_imported","audio13_actual_chest_torch","audio13_wood_dropped","audio13_wood_imported","audio13_same_torch_recovered","audio13_fixed_pool","audio13_preferences_restored"};
  const string AudioPolish13CanVerify="Ordinary original starter steel dagger equipped by its real inventory command, original glade chest torch acquired through actual container UI, wood drop and same-owner recovery, actual imported metal/wood recordings with committed exact-item receipts and fixed16 sources.";
  const string AudioPolish13CannotVerify="Soft materials, split stacks and callback negatives are native unit coverage, not claimed live here. Container transfer deliberately retains its original generic family; no inferred material is taken from an arbitrary item after bulk transfer. No generated items, material rewrites, HP or enemy suppression. Actual local travel retains danger. Source samples are not subjective listening or Deck performance; saved audio preferences remain unchanged.";
  IEnumerator RunAudioPolish13()
  {
   StartUtilityNative("audio13_preferences_restored",WorldAudioCue.HandleMetal,WorldAudioCue.HandleWood);
   var actor=_input.PlayerEntity;var zone=_input.CurrentZone;var pack=actor.GetPart<InventoryPart>();
   var dagger=pack.Objects.Single(e=>e.BlueprintName=="Dagger");Require(dagger.GetPart<MaterialPart>().MaterialID=="Steel","original authored steel dagger");
   BeginWaterNative(dagger,null,WorldAudioCue.HandleMetal);yield return ItemAction(dagger,"equip_auto");yield return FinishWaterNative("audio13_metal_imported");yield return ResidentCloseMenus();
   Check("audio13_original_dagger_equipped",InventorySystem.IsEquipped(actor,dagger)&&dagger.GetPart<PhysicsPart>().Equipped==actor);
   yield return Capture("handling-01-original-steel-dagger");
   var chest=zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Chest");
   var torch=chest.GetPart<ContainerPart>().Contents.Single(e=>e.BlueprintName=="Torch");
   yield return WalkTo(50,8);yield return WorldAction(chest,"OpenContainer");Require(State()=="PickupOpen","actual original chest picker");yield return Tap(Key.Tab);yield return ResidentCloseMenus();
   Check("audio13_actual_chest_torch",pack.Objects.Contains(torch)&&torch.GetPart<MaterialPart>().MaterialID=="Wood"&&!chest.GetPart<ContainerPart>().Contents.Contains(torch));
   BeginWaterNative(torch,null,WorldAudioCue.HandleWood);yield return ItemAction(torch,"drop");yield return FinishWaterNative("audio13_wood_imported");yield return ResidentCloseMenus();
   Check("audio13_wood_dropped",zone.GetEntityCell(torch)==Cell()&&!pack.Contains(torch));
   yield return Capture("handling-02-original-wooden-torch");
   yield return Tap(Key.G);if(State()=="PickupOpen")yield return Tap(Key.Tab);yield return ResidentCloseMenus();
   Check("audio13_same_torch_recovered",pack.Contains(torch)&&zone.GetEntityCell(torch)==null&&torch.GetPart<MaterialPart>().MaterialID=="Wood");
   Check("audio13_fixed_pool",_waterNativeRuntime.Player.AllocatedSources==16&&_waterNativeRuntime.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
  }
 }
}
