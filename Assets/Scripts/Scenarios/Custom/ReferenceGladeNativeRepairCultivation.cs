using System;
using System.Collections;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Actual-keyboard acceptance through one real generated repair
    /// field. Only initial player travel is a disclosed setup shortcut.</summary>
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _repairCultivationOnly;
        private const string RepairNativeZone = "Overworld.2.6.0";
        private static readonly string[] RepairCultivationChecks = {
            "repair_generated_field", "repair_gather_clay", "repair_gather_timber", "repair_gather_cord",
            "repair_lining", "repair_frame", "repair_line", "repair_draw_restored_water", "repair_operate_restored_gate",
            "crop_harvest_original", "crop_pickup_original", "crop_plant_returned_seed", "crop_learn_rain",
            "crop_water_planted", "crop_grow_sprout", "crop_grow_ripe", "crop_harvest_grown", "crop_pickup_grown",
            "repair_crop_native_checkpoint", "repair_crop_ordinary_finish" };
        private bool RepairCultivationComplete => RepairCultivationChecks.All(n => _audit.Count(a => a == "PASS " + n) == 1)
            && _audit.Count == RepairCultivationChecks.Length + 1 && _biomeShortcuts == 1 && _screenshots.Count >= 9;
        private const string RepairCultivationCanVerify = "Isolated ordinary seed64 player in the real generated west-of-Morrowfast field. Actual keyboard gathering, three material repairs, restored well drinking and gate operation; actual ripe knotflax harvest, physical produce/seed pickup, planting its returned seed on the preserved cultivated bed, original watering-book learning, Conjure Rain and paid player waits through sprout and ripe stages, a second harvest/pickup, and native F5/F6 graph replacement preserving repairs, remaining crop states and spent sources. Exact source/owner IDs and quantities are observed; native screenshots require visual inspection.";
        private const string RepairCultivationCannotVerify = "One explicitly logged player travel shortcut reaches the generated field from the isolated glade launch. All local movement and actions use ordinary keys and live NPC scheduling. No inventory, HP, stat, material, moisture, crop-stage, clock or RNG grants/edits, forced harvests or direct repair calls. A dangerous field stops the route honestly; no enemies are removed or suppressed. No natural discovery, all-seed terrain success, offscreen farming, economic balance or subjective model-quality claim. Wells supply drinking water; this route does not claim they water crops. Crops grow only while their area is active. The visual review is separate from these state checks.";

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
            try { yield return RepairCultivationRoute(); }
            finally { Diag.SetChannel("furniture", oldFurniture); Diag.SetChannel("crop", oldCrop); }
        }
        private IEnumerator RepairCultivationRoute()
        {
            var actor = _input.PlayerEntity; string actorID = actor.ID;
            Require(BiomeManager != null && BiomeManager.WorldSeed == 64 && actor.GetStat("Hitpoints").Max == 40
                && !DevMode.Enabled && !actor.HasPart<BitLockerPart>(), "ordinary isolated farming actor");
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
            yield return ResidentWorldAction(original, "HarvestCultivatedCrop"); yield return ResidentCloseMenus();
            var harvest = bed.Objects.Where(e => e.BlueprintName == yieldBlueprint || e.BlueprintName == seedBlueprint).ToArray();
            Check("crop_harvest_original", zone.GetEntityCell(original) == null && harvest.Count(e => e.BlueprintName == yieldBlueprint) == yieldCount
                && harvest.Count(e => e.BlueprintName == seedBlueprint) == seedCount && CultivatedSoilPart.IsCultivated(zone, bed));
            yield return Capture("crop-01-harvest-on-preserved-bed");
            yield return WalkTo(bed.X, bed.Y); yield return RepairNativePickup();
            Check("crop_pickup_original", ResidentUnits(actor, yieldBlueprint) == units + yieldCount && ResidentUnits(actor, seedBlueprint) == seeds + seedCount
                && harvest.All(e => zone.GetEntityCell(e) == null));
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
            yield return ResidentWorldAction(planted, "HarvestCultivatedCrop"); yield return ResidentCloseMenus();
            Check("crop_harvest_grown", zone.GetEntityCell(planted) == null && CultivatedSoilPart.IsCultivated(zone, bed)
                && bed.Objects.Count(e => e.BlueprintName == yieldBlueprint) == yieldCount && bed.Objects.Count(e => e.BlueprintName == seedBlueprint) == seedCount);
            yield return RepairNativePickup();
            Check("crop_pickup_grown", ResidentUnits(actor, yieldBlueprint) == units + yieldCount && ResidentUnits(actor, seedBlueprint) == seeds + seedCount);
            yield return Capture("crop-06-earned-repeat-crop-yield");
            yield return RepairNativeCheckpoint(sourceIDs);
            Check("repair_crop_ordinary_finish", _input.PlayerEntity.ID == actorID && State() == "Normal"
                && _input.PlayerEntity.GetStatValue("Hitpoints") > 0 && _input.PlayerEntity.GetStat("Hitpoints").Max == 40
                && !DevMode.Enabled && !_input.PlayerEntity.HasPart<BitLockerPart>() && _discoveryTonicsUsed == 0);
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
            yield return ResidentWorldAction(target, RepairablePart.RepairCommand); yield return ResidentCloseMenus();
            Check(check, fault.Repaired && ResidentUnits(_input.PlayerEntity, recipe.MaterialBlueprint) == before - recipe.Quantity
                && !RepairablePart.BlocksFunction(target) && BiomeDrawn(target));
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
