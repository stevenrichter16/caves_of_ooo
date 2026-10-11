using System;
using System.Collections;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Actual-keyboard acceptance through one real generated repair
    /// field. Only initial player travel is a disclosed setup shortcut.</summary>
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _repairCultivationOnly;
        private bool _repairWorkAudioOnly;
        public void ConfigureRepairWorkAudio() => _repairWorkAudioOnly=true;
        private const string RepairNativeZone = "Overworld.2.6.0";
        private static readonly string[] RepairCultivationChecks = {
            "repair_generated_field", "repair_gather_clay", "repair_gather_timber", "repair_gather_cord",
            "repair_lining", "repair_frame", "repair_line", "repair_draw_restored_water", "repair_operate_restored_gate",
            "crop_harvest_original", "crop_pickup_original", "crop_plant_returned_seed", "crop_learn_rain",
            "crop_water_planted", "crop_grow_sprout", "crop_grow_ripe", "crop_harvest_grown", "crop_pickup_grown",
            "repair_crop_native_checkpoint", "repair_crop_ordinary_finish",
            "work_audio_fixed_pool", "work_sound_repair_lining", "work_sound_repair_frame", "work_sound_repair_line",
            "work_sound_crop_original", "work_sound_crop_grown", "work_audio_checkpoint_silent", "work_audio_settings_restored" };
        private bool RepairCultivationComplete => RepairCultivationChecks.All(n => _audit.Count(a => a == "PASS " + n) == 1)
            && _audit.Count == RepairCultivationChecks.Length + 1 && _biomeShortcuts == 1 && _screenshots.Count >= 9;
        private static readonly string[] RepairWorkAudioChecks = {
            "repair_generated_field", "repair_gather_clay", "repair_gather_timber", "repair_gather_cord",
            "repair_lining", "repair_frame", "repair_line", "repair_draw_restored_water", "repair_operate_restored_gate",
            "crop_harvest_original", "crop_pickup_original", "repair_crop_native_checkpoint", "repair_crop_ordinary_finish",
            "work_audio_fixed_pool", "work_sound_repair_lining", "work_sound_repair_frame", "work_sound_repair_line",
            "work_sound_crop_original", "work_audio_checkpoint_silent", "work_audio_settings_restored" };
        private bool RepairWorkAudioComplete => RepairWorkAudioChecks.All(n => _audit.Count(a => a == "PASS " + n) == 1)
            && _audit.Count(a => a == "PASS ordinary_player_and_native_zone") == 1
            && _audit.Count == RepairWorkAudioChecks.Length + 1 && _biomeShortcuts == 1 && _screenshots.Count >= 8;
        private const string RepairWorkAudioCanVerify = "Isolated ordinary seed64 actor in the real generated west-of-Morrowfast field. Actual keyboard gathering of clay, timber and cord, three material repairs, restored well drinking and gate operation, the original ripe knotflax harvest and physical produce/seed pickup. Four committed work receipts correlate exact actor/source/contact to actual imported short recordings, accepted playback and nonzero per-source PCM windows. Native F5/F6 graph replacement preserves repaired flags, remaining crops, spent sources and inventory without replaying work. The fixed16-source pool and restored in-memory SFX preference are checked.";
        private const string RepairWorkAudioCannotVerify = "One explicitly logged initial player travel shortcut reaches the unchanged generated field; every local action and movement uses ordinary keys with live NPC scheduling. No HP, inventory, materials, crop-stage, time or RNG grants, forced repairs or removed enemies. Up to two exact original starting HealingTonic units may be consumed through ordinary inventory keys at or below two-thirds HP; use, identity and actual consumed units are recorded. No healing is injected. This distinct work-audio route deliberately ends after its first real harvest and checkpoint: it does not claim live planting, rain, growth, a repeat crop harvest, forge work or portable repair. The separate full farming attempt stopped at real enemy danger and remains a failed receipt. Sampled per-source PCM is not subjective listening, continuous recording, physical speaker output, global clipping proof or Deck performance. Saved preferences are untouched; only in-memory SFX is initialized and exactly restored. Screenshots need visual review.";
        private const string RepairCultivationCanVerify = "Isolated ordinary seed64 player in the real generated west-of-Morrowfast field. Actual keyboard gathering, three material repairs, restored well drinking and gate operation; actual ripe knotflax harvest, physical produce/seed pickup, planting its returned seed on the preserved cultivated bed, original watering-book learning, Conjure Rain and paid player waits through sprout and ripe stages, a second harvest/pickup, and native F5/F6 graph replacement preserving repairs, remaining crop states and spent sources. Exact source/owner IDs and quantities are observed. Five committed work receipts correlate the live current actor/source/contact to the expected imported short recording, accepted playback and nonzero per-source PCM windows; the fixed audio pool and silent loaded work are checked. Native screenshots require visual inspection.";
        private const string RepairCultivationCannotVerify = "One explicitly logged player travel shortcut reaches the generated field from the isolated glade launch. All local movement and actions use ordinary keys and live NPC scheduling. No inventory, HP, stat, material, moisture, crop-stage, clock or RNG grants/edits, forced harvests or direct repair calls. A dangerous field stops the route honestly; no enemies are removed or suppressed. No natural discovery, all-seed terrain success, offscreen farming, economic balance or subjective model-quality claim. Wells supply drinking water; this route does not claim they water crops. Crops grow only while their area is active. Only the in-memory SFX level is temporarily set to1 and restored; saved preferences are not modified. This route does not exercise a forge or portable gear repair; their transaction behavior has separate native unit coverage. Sampled per-source PCM is not subjective listening, physical speaker output, continuous capture, a clipping guarantee or a Steam Deck performance claim. The visual review is separate from these state checks.";

        private Entity RepairNativeOwner(string blueprint)
        {
            var rows = _input.CurrentZone.GetReadOnlyEntities().Where(e => e.BlueprintName == blueprint).ToArray();
            Require(rows.Length == 1, "one exact current repair owner " + blueprint); return rows[0];
        }
        private IEnumerator RepairNativeApproach(Entity owner)
        {
            var approach = SpecialistApproach(_input.CurrentZone, owner);
            Require(approach != null, "reachable unchanged repair field frontage " + owner.BlueprintName);
            yield return WalkTo(approach.X, approach.Y);
            Require(_input.PlayerEntity.GetStatValue("Hitpoints") > 10, "live safe field approach without healing grants");
        }
        private IEnumerator RunRepairCultivationAudit()
        {
            bool oldFurniture = Diag.IsChannelEnabled("furniture"), oldCrop = Diag.IsChannelEnabled("crop");
            Diag.SetChannel("furniture", true); Diag.SetChannel("crop", true);
            var oldSfx=WorldAudioSettings.SfxVolume;
            Coroutine monitor=null;
            _repairWorkCleanup=()=>{
                WorldAudioWork.Callback-=ObserveRepairNativeWork;
                if(monitor!=null)StopCoroutine(monitor);
                _repairWorkPending=null; WorldAudioSettings.SfxVolume=oldSfx;
                Diag.SetChannel("furniture",oldFurniture); Diag.SetChannel("crop",oldCrop);
                Check("work_audio_settings_restored",WorldAudioSettings.SfxVolume==oldSfx);
            };
            try
            {
                _repairWorkRuntime=FindFirstObjectByType<WorldAudioRuntime>();
                Require(_repairWorkRuntime?.Player!=null,"actual bootstrap world audio for handwork");
                _repairWorkLibrary=WorldAudioLibrary.LoadResources();
                _repairWorkSamples=new float[1024];
                WorldAudioSettings.SfxVolume=1;
                WorldAudioWork.Callback+=ObserveRepairNativeWork;
                monitor=StartCoroutine(RepairNativeAudioMonitor());
                Check("work_audio_fixed_pool",_repairWorkRuntime.Player.AllocatedSources==16
                    &&_repairWorkRuntime.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
                yield return RepairCultivationRoute();
            }
            finally { _repairWorkCleanup?.Invoke(); _repairWorkCleanup=null; }
        }
        private IEnumerator RepairCultivationRoute()
        {
            var actor = _input.PlayerEntity; string actorID = actor.ID;
            Require(BiomeManager != null && BiomeManager.WorldSeed == 64 && actor.GetStat("Hitpoints").Max == 40
                && !DevMode.Enabled && !actor.HasPart<BitLockerPart>(), "ordinary isolated farming actor");
            if (_repairWorkAudioOnly)
            {
                _discoveryTonics.AddRange(actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic"));
                Require(_discoveryTonics.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==2,"two original starting tonic units, no grants");
            }
            var zone = BiomeManager.GetZone(RepairNativeZone);
            var lined = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "RepairLinedWell");
            Require(lined != null, "actual cold-generated west field, no replacement setup");
            var arrival = SpecialistApproach(zone, lined); Require(arrival != null, "safe unchanged field arrival");
            yield return BiomeTravel(zone, arrival, "real west-of-Morrowfast repair and cultivated field; all faults, materials and plants left as generated");
            var ropeWell = RepairNativeOwner("RepairRopeWell"); var gate = RepairNativeOwner("RepairWoodenGate");
            var crops = zone.GetReadOnlyEntities().Where(e => new[] { "KnotflaxCrop", "HearthbulbCrop", "SeamleafCrop" }.Contains(e.BlueprintName)).ToArray();
            var sources = new[] { "RepairClayBank", "RepairTimberPile", "RepairCordBundle" }.Select(RepairNativeOwner).ToArray();
            string[] sourceIDs = sources.Select(e => e.ID).ToArray();
            Check("repair_generated_field", new[] { lined, ropeWell, gate }.All(e => e.GetPart<RepairablePart>()?.Repaired == false)
                && !lined.GetPart<WellPart>().IsUsable && !ropeWell.GetPart<WellPart>().IsUsable
                && !gate.GetPart<DoorPart>().IsClosed && !gate.GetPart<DoorPart>().CanOperate(actor, zone)
                && crops.Length == 6 && crops.Count(e => e.GetPart<CropPart>()?.GrowthStage == 2) == 3
                && crops.All(e => CultivatedSoilPart.IsCultivated(zone, zone.GetEntityCell(e))) && BiomeDrawn(lined));
            yield return Capture("repair-01-generated-field");
            yield return CurationExamine(lined, "repair-02-cracked-lining-and-material");
            yield return RepairNativeGather(sources[0], "repair_gather_clay");
            yield return RepairNativeGather(sources[1], "repair_gather_timber");
            yield return RepairNativeGather(sources[2], "repair_gather_cord");
            yield return Capture("repair-03-gathered-world-supplies");
            yield return RepairNativeFix(lined, "repair_lining");
            yield return RepairNativeFix(gate, "repair_frame");
            yield return RepairNativeFix(ropeWell, "repair_line");
            yield return RepairNativeApproach(lined);
            var seen = Diag.Snapshot(Diag.BufferCapacity).Select(r => r.TraceId).ToHashSet();
            yield return ResidentWorldAction(lined, "DrawWaterAtWell"); yield return ResidentCloseMenus();
            Check("repair_draw_restored_water", lined.GetPart<WellPart>().IsUsable
                && Diag.Snapshot(Diag.BufferCapacity).Any(r => !seen.Contains(r.TraceId) && r.Kind == "WaterDrawn" && r.ActorId == actor.ID && r.TargetId == lined.ID));
            yield return RepairNativeApproach(gate);
            yield return ResidentWorldAction(gate, DoorPart.CloseCommand); yield return ResidentCloseMenus();
            bool closed = gate.GetPart<DoorPart>().IsClosed && BiomeDrawn(gate);
            yield return Capture("repair-04-restored-gate-closed");
            yield return ResidentWorldAction(gate, DoorPart.OpenCommand); yield return ResidentCloseMenus();
            Check("repair_operate_restored_gate", closed && !gate.GetPart<DoorPart>().IsClosed && gate.GetPart<DoorPart>().CanOperate(actor, zone));
            yield return Capture("repair-05-restored-gate-open");

            var original = crops.Single(e => e.BlueprintName == "KnotflaxCrop" && e.GetPart<CropPart>().GrowthStage == 2);
            var bed = zone.GetEntityCell(original); var originalPart = original.GetPart<CropPart>();
            string yieldBlueprint = originalPart.YieldBlueprint, seedBlueprint = originalPart.SeedYieldBlueprint;
            int yieldCount = originalPart.YieldCount, seedCount = originalPart.SeedYieldCount;
            yield return RepairNativeApproach(original);
            int units = ResidentUnits(actor, yieldBlueprint), seeds = ResidentUnits(actor, seedBlueprint);
            BeginRepairNativeWork(original,WorldAudioCue.HarvestPlant,true);
            yield return ResidentWorldAction(original, "HarvestCultivatedCrop"); yield return ResidentCloseMenus();
            yield return FinishRepairNativeWork("work_sound_crop_original");
            var harvest = bed.Objects.Where(e => e.BlueprintName == yieldBlueprint || e.BlueprintName == seedBlueprint).ToArray();
            Check("crop_harvest_original", zone.GetEntityCell(original) == null && harvest.Count(e => e.BlueprintName == yieldBlueprint) == yieldCount
                && harvest.Count(e => e.BlueprintName == seedBlueprint) == seedCount && CultivatedSoilPart.IsCultivated(zone, bed));
            yield return Capture("crop-01-harvest-on-preserved-bed");
            yield return WalkTo(bed.X, bed.Y); yield return RepairNativePickup();
            Check("crop_pickup_original", ResidentUnits(actor, yieldBlueprint) == units + yieldCount && ResidentUnits(actor, seedBlueprint) == seeds + seedCount
                && harvest.All(e => zone.GetEntityCell(e) == null));
            if (_repairWorkAudioOnly)
            {
                yield return RepairNativeFinish(actorID,sourceIDs);
                yield break;
            }
            var seed = actor.GetPart<InventoryPart>().Objects.First(e => e.BlueprintName == seedBlueprint);
            seeds = ResidentUnits(actor, seedBlueprint);
            yield return ItemAction(seed, "PlantSeed"); yield return ResidentCloseMenus();
            var planted = bed.Objects.SingleOrDefault(e => e.BlueprintName == "KnotflaxCrop");
            Check("crop_plant_returned_seed", planted != null && !crops.Contains(planted) && ResidentUnits(actor, seedBlueprint) == seeds - 1
                && planted.GetPart<CropPart>().GrowthStage == 0 && planted.GetPart<CropPart>().MoistureTicks == 0);
            Require(planted != null, "new crop produced by actual carried seed action");
            yield return Capture("crop-02-actual-planted-seed");
            var book = actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "WateringGrimoire");
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            if (!abilities.AbilityList.Any(a => a.Command == "CommandConjureRain"))
            { yield return ItemAction(book, "ReadGrimoire"); yield return ResidentCloseMenus(); }
            var rain = abilities.AbilityList.SingleOrDefault(a => a.Command == "CommandConjureRain");
            Check("crop_learn_rain", rain != null && actor.GetPart<InventoryPart>().Objects.Contains(book));
            Require(rain != null, "actual starter grimoire unlocks registered rain");
            int slot = abilities.GetSlotForAbility(rain.ID); Require(slot >= 0 && slot < 10, "actual rain hotbar assignment");
            var crop = planted.GetPart<CropPart>(); int waits = 0, casts = 0;
            Require(crop.TicksPerStage > 0 && crop.TicksPerStage <= 80, "bounded real crop duration");
            yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + ((slot + 1) % 10)));
            yield return Capture("crop-03a-native-conjured-rain");
            yield return CombatWaitForFx(); yield return ResidentCloseMenus(); casts++;
            Check("crop_water_planted", crop.MoistureTicks > 0 && crop.MoistureTicks <= 40 && rain.CooldownRemaining > 0);
            yield return Capture("crop-03-rain-watered-soil");
            bool sproutSeen = false;
            while (crop.GrowthStage < 2 && waits < 200)
            {
                Require(_input.CurrentZone == zone && zone.GetEntityCell(planted) == bed && State() == "Normal"
                    && actor.GetStatValue("Hitpoints") > 10 && !CombatSystem.IsDeathHandled(actor), "safe actual crop growth wait; no skipped danger");
                if (crop.MoistureTicks == 0 && rain.CooldownRemaining == 0)
                {
                    Require(casts < 8, "bounded ordinary rewatering");
                    yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + ((slot + 1) % 10)));
                    yield return CombatWaitForFx(); yield return ResidentCloseMenus(); casts++;
                }
                else { yield return Tap(Key.Period); waits++; yield return CombatWaitForFx(); }
                if (!sproutSeen && crop.GrowthStage == 1)
                {
                    Check("crop_grow_sprout", zone.GetEntityCell(planted) == bed && crop.TicksInStage < crop.TicksPerStage && BiomeDrawn(planted));
                    yield return Capture("crop-04-grown-sprout"); sproutSeen = true;
                }
            }
            Check("crop_grow_ripe", sproutSeen && crop.GrowthStage == 2 && zone.GetEntityCell(planted) == bed
                && !bed.Objects.Any(e => e.BlueprintName == yieldBlueprint) && BiomeDrawn(planted));
            Require(crop.GrowthStage == 2, "actual scheduler reaches standing maturity without direct state writes");
            _descriptions.Add(new Description { subject = "Actual cultivated growth", source = planted.ID,
                text = "Ordinary waits: " + waits + "; real rain casts: " + casts + "; ticks per stage: " + crop.TicksPerStage + ". No offscreen progress or manual growth changes." });
            yield return Capture("crop-05-standing-ripe-plant");
            units = ResidentUnits(actor, yieldBlueprint); seeds = ResidentUnits(actor, seedBlueprint);
            BeginRepairNativeWork(planted,WorldAudioCue.HarvestPlant,true);
            yield return ResidentWorldAction(planted, "HarvestCultivatedCrop"); yield return ResidentCloseMenus();
            yield return FinishRepairNativeWork("work_sound_crop_grown");
            Check("crop_harvest_grown", zone.GetEntityCell(planted) == null && CultivatedSoilPart.IsCultivated(zone, bed)
                && bed.Objects.Count(e => e.BlueprintName == yieldBlueprint) == yieldCount && bed.Objects.Count(e => e.BlueprintName == seedBlueprint) == seedCount);
            yield return RepairNativePickup();
            Check("crop_pickup_grown", ResidentUnits(actor, yieldBlueprint) == units + yieldCount && ResidentUnits(actor, seedBlueprint) == seeds + seedCount);
            yield return Capture("crop-06-earned-repeat-crop-yield");
            yield return RepairNativeFinish(actorID,sourceIDs);
        }
        private IEnumerator RepairNativeFinish(string actorID,string[] sourceIDs)
        {
            int workBeforeLoad=_repairWorkReceiptCount;
            yield return RepairNativeCheckpoint(sourceIDs);
            Check("work_audio_checkpoint_silent",_repairWorkReceiptCount==workBeforeLoad
                &&_repairWorkRuntime.Player.AllocatedSources==16);
            Check("repair_crop_ordinary_finish", _input.PlayerEntity.ID == actorID && State() == "Normal"
                && _input.PlayerEntity.GetStatValue("Hitpoints") > 0 && _input.PlayerEntity.GetStat("Hitpoints").Max == 40
                && !DevMode.Enabled && !_input.PlayerEntity.HasPart<BitLockerPart>()
                && (_repairWorkAudioOnly ? _discoveryTonicsUsed>=0&&_discoveryTonicsUsed<=2 : _discoveryTonicsUsed==0));
            yield return Capture("repair-06-restored-and-saved-field");
        }
        private IEnumerator RepairNativeGather(Entity source, string check)
        {
            yield return RepairNativeApproach(source);
            var actor = _input.PlayerEntity; var part = source.GetPart<HarvestablePart>(); var cell = _input.CurrentZone.GetEntityCell(source);
            Require(part != null && !part.Harvested && part.YieldChance == 100 && part.YieldMin == part.YieldMax, "actual finite deterministic material source");
            int before = ResidentUnits(actor, part.YieldBlueprint);
            yield return ResidentWorldAction(source, "Harvest"); yield return ResidentCloseMenus();
            if (cell.Objects.Any(e => e.BlueprintName == part.YieldBlueprint))
            { yield return WalkTo(cell.X, cell.Y); yield return RepairNativePickup(); }
            Check(check, part.Harvested && _input.CurrentZone.GetEntityCell(source) == null
                && ResidentUnits(actor, part.YieldBlueprint) == before + part.YieldMin);
        }
        private IEnumerator RepairNativeFix(Entity target, string check)
        {
            yield return RepairNativeApproach(target);
            var fault = target.GetPart<RepairablePart>(); var recipe = RepairRecipeRegistry.Get(fault.RecipeId);
            Require(recipe != null && !fault.Repaired, "actual defined field fault");
            int before = ResidentUnits(_input.PlayerEntity, recipe.MaterialBlueprint);
            Require(before >= recipe.Quantity, "gathered real repair supplies");
            var cue=recipe.Composition=="Masonry"?WorldAudioCue.WorkRepairStone:recipe.Composition=="Wood"?WorldAudioCue.WorkRepairWood:WorldAudioCue.Use;
            BeginRepairNativeWork(target,cue,false);
            yield return ResidentWorldAction(target, RepairablePart.RepairCommand); yield return ResidentCloseMenus();
            yield return FinishRepairNativeWork("work_sound_"+check);
            Check(check, fault.Repaired && ResidentUnits(_input.PlayerEntity, recipe.MaterialBlueprint) == before - recipe.Quantity
                && !RepairablePart.BlocksFunction(target) && BiomeDrawn(target));
        }
        private Action _repairWorkCleanup;
        private WorldAudioRuntime _repairWorkRuntime;
        private WorldAudioLibrary _repairWorkLibrary;
        private RepairNativeWorkEvidence _repairWorkPending;
        private float[] _repairWorkSamples;
        private int _repairWorkReceiptCount;
        private sealed class RepairNativeWorkEvidence
        {
            public Entity Actor,Source; public Zone Zone; public Point Contact; public WorldAudioCue Cue;
            public bool Consumed,ExactReceipt,ImportedPlaying; public int BeforeAccepted,ReceiptCount;
            public AudioSource Voice; public AudioClip Clip; public float Peak; public int SampleCount;
            public double Began;
        }
        private void BeginRepairNativeWork(Entity source,WorldAudioCue cue,bool consumed)
        {
            var cell=_input.CurrentZone.GetEntityCell(source); Require(cell!=null,"current physical work target");
            _repairWorkPending=new RepairNativeWorkEvidence { Actor=_input.PlayerEntity,Source=source,Zone=_input.CurrentZone,
                Contact=new Point(cell.X,cell.Y),Cue=cue,Consumed=consumed,BeforeAccepted=_repairWorkRuntime.Player.AcceptedSoundCount };
        }
        private void ObserveRepairNativeWork(WorldAudioWorkReceipt receipt)
        {
            _repairWorkReceiptCount++;
            var e=_repairWorkPending;if(e==null)return;
            e.ReceiptCount++;
            e.ExactReceipt=receipt.Actor==e.Actor&&receipt.Source==e.Source&&receipt.Zone==e.Zone
                &&receipt.Contact.Equals(e.Contact)&&receipt.Cue==e.Cue&&receipt.SourceConsumed==e.Consumed;
            if(!e.ExactReceipt)return;
            var first=_repairWorkLibrary.Sound(e.Cue,0)?.Clip;var second=_repairWorkLibrary.Sound(e.Cue,1)?.Clip;
            e.Voice=_repairWorkRuntime.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>!s.loop
                &&s.priority==64&&s.isPlaying&&s.volume>0&&s.clip!=null&&(s.clip==first||s.clip==second));
            e.Clip=e.Voice?.clip;e.Began=Time.realtimeSinceStartupAsDouble;
            e.ImportedPlaying=e.Clip!=null&&e.Clip.loadState==AudioDataLoadState.Loaded
                &&e.Clip.loadType==AudioClipLoadType.DecompressOnLoad&&_repairWorkRuntime.Player.AcceptedSoundCount>e.BeforeAccepted;
        }
        private IEnumerator RepairNativeAudioMonitor()
        {
            while(true)
            {
                var e=_repairWorkPending;
                if(e?.Voice!=null&&e.Voice.clip==e.Clip&&e.Voice.isPlaying&&e.ImportedPlaying
                    &&Time.realtimeSinceStartupAsDouble-e.Began<1)
                {
                    e.Voice.GetOutputData(_repairWorkSamples,0);
                    foreach(float value in _repairWorkSamples){e.Peak=Mathf.Max(e.Peak,Mathf.Abs(value));e.SampleCount++;}
                }
                yield return null;
            }
        }
        private IEnumerator FinishRepairNativeWork(string check)
        {
            var e=_repairWorkPending; Require(e!=null,"pending physical work observation");
            double until=Time.realtimeSinceStartupAsDouble+.25;
            while(e.Peak<=.000001f&&Time.realtimeSinceStartupAsDouble<until)yield return null;
            Check(check,e.ReceiptCount==1&&e.ExactReceipt&&e.ImportedPlaying&&e.SampleCount>0&&e.Peak>.000001f
                &&_repairWorkRuntime.Player.AllocatedSources==16);
            _descriptions.Add(new Description { subject=check,source=e.Source.ID,text="Cue="+e.Cue+"; receiptCount="+e.ReceiptCount
                +"; exactOwnerContact="+e.ExactReceipt+"; importedPlaying="+e.ImportedPlaying+"; clip="+e.Clip?.name
                +"; sampledSourcePeak="+e.Peak+"; sampledFloats="+e.SampleCount
                +". Noncontiguous per-source playback windows, not a listening or physical-output claim." });
            _repairWorkPending=null;
        }
        private IEnumerator RepairNativePickup()
        { yield return Tap(Key.G); if (State() == "PickupOpen") yield return Tap(Key.Tab); yield return ResidentCloseMenus(); }
        private static string RepairNativeFieldState(Zone zone) => string.Join("|", zone.GetReadOnlyEntities()
            .Where(e => e.HasPart<RepairablePart>() || e.HasPart<CropPart>() || e.HasPart<CultivatedSoilPart>())
            .OrderBy(e => e.ID, StringComparer.Ordinal).Select(e => e.ID + ":" + e.BlueprintName + ":" + zone.GetEntityPosition(e)
                + ":" + e.GetPart<RepairablePart>()?.Repaired + ":" + e.GetPart<DoorPart>()?.IsOpen
                + ":" + e.GetPart<CropPart>()?.GrowthStage + ":" + e.GetPart<CropPart>()?.TicksInStage
                + ":" + e.GetPart<CropPart>()?.MoistureTicks + ":" + e.GetPart<RenderPart>()?.RenderString
                + ":" + e.HasPart<CultivatedSoilPart>()));
        private IEnumerator RepairNativeCheckpoint(string[] spentSources)
        {
            var before = _input.PlayerEntity; var at = Cell(); var zone = _input.CurrentZone;
            string state = RepairNativeFieldState(zone), items = BiomeItemCollection(before);
            int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(before), hp = before.GetStatValue("Hitpoints");
            var info = SaveGameService.GetSaveInfo("Quick"); Require(info != null, "isolated ordinary Quick checkpoint");
            string path = Path.Combine(_ownedRoot, info.GameID, "Quick.sav.gz"), previous = CheckpointHash(path);
            yield return Tap(Key.F5); string saved = CheckpointHash(path);
            Require(saved != previous && SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID == RepairNativeZone, "actual native F5 field save");
            var next = new[] { (1,0), (-1,0), (0,1), (0,-1) }.Select(d => zone.GetCell(at.X + d.Item1, at.Y + d.Item2))
                .FirstOrDefault(c => BiomeSafe(zone, c, 0)); Require(next != null, "real safe post-save movement");
            yield return Tap(Direction(next.X - at.X, next.Y - at.Y)); Require(Cell() == next && CheckpointHash(path) == saved, "real movement after saved checkpoint");
            yield return Tap(Key.F6); double began = Time.realtimeSinceStartupAsDouble;
            while (ReferenceEquals(before, _input.PlayerEntity)) { Require(Time.realtimeSinceStartupAsDouble - began < 8, "native field graph replacement"); yield return null; }
            var actor = _input.PlayerEntity; var loaded = _input.CurrentZone;
            Check("repair_crop_native_checkpoint", loaded.ZoneID == RepairNativeZone && !ReferenceEquals(zone, loaded)
                && actor.ID == before.ID && Cell().X == at.X && Cell().Y == at.Y && RepairNativeFieldState(loaded) == state
                && BiomeItemCollection(actor) == items && _input.TurnManager.TickCount == tick && _input.TurnManager.GetEnergy(actor) == energy
                && actor.GetStatValue("Hitpoints") == hp && CheckpointHash(path) == saved
                && !loaded.GetReadOnlyEntities().Any(e => spentSources.Contains(e.ID) || new[] { "RepairClayBank", "RepairTimberPile", "RepairCordBundle" }.Contains(e.BlueprintName)));
            _descriptions.Add(new Description { subject = "Native repair and cultivated checkpoint", source = info.GameID,
                text = "F5, real step, F6; unchanged saved bytes " + saved + "; exact repaired flags, remaining crop clocks, prepared terrain IDs and carried quantities restored; all three harvested sources remain absent." });
        }
    }
}
