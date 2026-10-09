using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using Newtonsoft.Json.Linq;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Ordinary-key fieldwork journey. Every owner/resource is acquired
    /// from the normal fresh world; queries below only observe current state.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _fieldwork;
        string _fieldworkSoilId, _fieldworkOriginalCropId, _fieldworkGrowingId, _fieldworkFinalCropId;
        string _fieldworkWicketId, _fieldworkWellId, _fieldworkPalletId, _fieldworkCrateId, _fieldworkShellId, _fieldworkForageId;
        int _fieldworkWaters, _fieldworkBrokenDetour;
        static readonly string[] FieldworkChecks = {
            "ordinary_start", "fieldwork_original_yard", "fieldwork_real_bypass", "fieldwork_shell_and_seed",
            "fieldwork_replanted", "fieldwork_cellar_supplies", "fieldwork_pallet_hauled", "fieldwork_pallet_dismantled",
            "fieldwork_well_and_vessel", "fieldwork_first_water", "fieldwork_repaired_passage", "fieldwork_actual_expedition",
            "fieldwork_return_growth", "fieldwork_retended", "fieldwork_finite_basin", "fieldwork_second_harvest",
            "fieldwork_next_cycle", "fieldwork_save_restore", "fieldwork_finish" };
        const string FieldworkIntent = "Actual seed64 Duelist picker and ordinary native-key route: discover the buckled wicket and walk its existing bypass, harvest the real ripe Drawgourd for shell/seed, replant, enter the cellar for finite clay and a pallet, haul/release/dismantle the exact pallet, repair the actual well and fill the acquired shell, spend its water on the planted crop, repair/open/cross/close the wicket, visit the northern cold bank to gather its finite frost lichen and read the actual alembic source, return to elapsed growth, retend and inspect the finite reed basin, harvest the grown owner, replant/rewater the next cycle, and F5/unsaved-step/F6 the retained physical consequences.";
        const string FieldworkLimits = "One seed and one selected build; not unaided discovery, campaign balance or all-build proof. The observer uses bounded native-key routing and existing ordinary Duelist defense, including deliberate approach to a hostile obstructing the next worksite; real threats can stop the run. No source/player transfers, grants, replenishment, weather mutation, artificial waits or clock changes. The native pallet is hauled and released before dismantling; held-dismantle and overflow rollback have separate EditMode coverage. This route chooses repair, not the alternative destructive wicket route. No rain is cast. Screenshots need independent visual review. Timing includes paced keys and audit IO.";
        Entity FieldworkGlade(string id) => ConnectedAt(GleanersDistrict.SurfaceID, id);
        Entity FieldworkShell => Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e => e.ID == _fieldworkShellId && Owns(Player, e));
        Entity FieldworkGrowing => FieldworkGlade(_fieldworkGrowingId);
        static int FieldworkGrowth(Entity owner)
        { var crop = owner?.GetPart<CropPart>(); return crop == null ? -1 : crop.GrowthStage * crop.TicksPerStage + crop.TicksInStage; }

        IEnumerator FieldworkJourney()
        {
            Require(_connectedBuild == "duelist" && Manager.Exploration?.Enabled == true && Manager.Exploration.Version >= 12,
                "fresh enabled fieldwork world and actual selected Duelist");
            var wicket = DistrictOwner(Zone, ReferenceGladeBuilder.FieldworkRoleKey, "wicket"); _fieldworkWicketId = wicket.ID;
            var garden = DistrictOwner(Zone, ReferenceGladeBuilder.FieldworkRoleKey, "garden"); _fieldworkOriginalCropId = garden.ID;
            var soil = DistrictOwner(Zone, ReferenceGladeBuilder.FieldworkRoleKey, "soil"); _fieldworkSoilId = soil.ID;
            var well = DistrictOwner(Zone, GleanersDistrict.RoleKey, "well"); _fieldworkWellId = well.ID;
            Check("fieldwork_original_yard", Zone.GetEntityPosition(wicket) == (46, 6) && Zone.GetEntityPosition(garden) == (46, 4)
                && Zone.GetEntityCell(garden) == Zone.GetEntityCell(soil) && CultivatedSoilPart.IsCultivated(Zone, Zone.GetEntityCell(soil))
                && garden.BlueprintName == "DrawgourdCrop" && garden.GetPart<CropPart>().GrowthStage == 2
                && !wicket.GetPart<RepairablePart>().Repaired && wicket.GetPart<DoorPart>().IsClosed
                && !well.GetPart<WellPart>().IsUsable && Packed("DrawgourdShell") == 0 && Packed("DrawgourdSeed") == 0
                && Packed("SalvagedTimber") == 0 && DistrictClay() == 0);
            yield return DistrictWalk(Zone.GetCell(46, 7), 140);
            var bypass = PathTo(c => c == Zone.GetEntityCell(soil));
            Require(bypass != null && bypass.Count > 3 && bypass.All(p => p != (46, 6)), "real longer path around the buckled wicket");
            _fieldworkBrokenDetour = bypass.Count;
            yield return WorldAction(wicket, "Examine"); yield return ReadPages("fieldwork-01-buckled-wicket-reader"); yield return CloseNormal();
            yield return Capture("fieldwork-02-closed-wicket-and-bypass");
            yield return DistrictWalk(Zone.GetEntityCell(soil), 80);
            Check("fieldwork_real_bypass", At == Zone.GetEntityCell(soil) && !wicket.GetPart<RepairablePart>().Repaired
                && wicket.GetPart<DoorPart>().IsClosed && _fieldworkBrokenDetour > 3);
            yield return ConnectedHarvest(garden, "DrawgourdShell");
            var shell = Player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "DrawgourdShell" && Owns(Player, e));
            _fieldworkShellId = shell.ID;
            Check("fieldwork_shell_and_seed", Packed("DrawgourdShell") == 1 && Packed("DrawgourdSeed") == 1
                && shell.GetPart<WaterskinPart>()?.Charges == 0 && shell.GetPart<WaterskinPart>()?.Capacity == 3
                && CountGraphId(_fieldworkOriginalCropId) == 0 && CountGraphId(_fieldworkShellId) == 1);
            yield return ConnectedReplantCurrentBed("DrawgourdSeed", "DrawgourdCrop"); _fieldworkGrowingId = _connectedLastPlantId;
            Check("fieldwork_replanted", Packed("DrawgourdSeed") == 0 && FieldworkGrowing != null
                && FieldworkGrowing.ID != _fieldworkOriginalCropId && FieldworkGrowth(FieldworkGrowing) == 0
                && FieldworkGrowing.GetPart<CropPart>().MoistureTicks == 0 && At.Objects.Contains(soil));
            yield return Capture("fieldwork-03-returned-seed-replanted");

            yield return DistrictWalk(Zone.GetCell(GleanersDistrict.EntranceX, GleanersDistrict.EntranceY), 140);
            yield return Paid(Tap(Key.LeftShift, Key.Period), "local", "fieldwork-cellar-down");
            Require(Zone.ZoneID == GleanersCellarBuilder.ZoneID, "actual native cellar descent");
            var crate = DistrictOwner(Zone, GleanersCellarBuilder.RoleKey, "supplies"); _fieldworkCrateId = crate.ID;
            var supplies = crate.GetPart<ContainerPart>().Contents.ToArray();
            Require(supplies.Where(e => e.BlueprintName == "FireClay").Sum(Units) == 2, "original finite clay budget");
            foreach (var at in new[] { (40, 5), (45, 5), (46, 4), (46, 2), (50, 2), (50, 5), (62, 5), (63, 6) })
                yield return DistrictWalk(Zone.GetCell(at.Item1, at.Item2), 80);
            yield return Paid(DistrictTakeCache(crate, supplies), "local", "fieldwork-original-cellar-supplies");
            Check("fieldwork_cellar_supplies", DistrictClay() == 2 && crate.GetPart<ContainerPart>().Contents.Count == 0
                && supplies.All(e => Owns(Player, e)) && supplies.All(e => CountGraphId(e.ID) == 1));
            var pallet = DistrictOwner(Zone, GleanersCellarBuilder.RoleKey, "timber-pallet"); _fieldworkPalletId = pallet.ID;
            var original = Zone.GetEntityPosition(pallet);
            Require(pallet.BlueprintName == "GleanersTimberPallet" && DragRules.WeightOf(pallet) == 90
                && pallet.GetPart<HarvestablePart>()?.YieldMin == 2 && pallet.GetPart<HarvestablePart>()?.YieldMax == 2,
                "actual finite 90-weight two-timber source");
            yield return DistrictWalk(Zone.GetCell(original.x + 1, original.y), 50);
            int speed = Player.GetStatValue("Speed"), tick = Tick, energy = Energy;
            yield return WorldAction(pallet, HandlingPart.HaulCommand); yield return CloseNormal();
            Require(DragSystem.GetDragged(Player) == pallet && Player.GetStatValue("Speed") < speed && Tick == tick && Energy == energy,
                "native free grip on exact original pallet");
            var shoulder = At;
            yield return StepTo(At.X, At.Y - 1);
            Require(Zone.GetEntityCell(pallet) == shoulder, "same pallet follows native paid movement");
            tick = Tick; energy = Energy;
            yield return WorldAction(pallet, HandlingPart.ReleaseCommand); yield return CloseNormal();
            Check("fieldwork_pallet_hauled", Zone.GetEntityCell(pallet) == shoulder && !DragSystem.IsDragging(Player)
                && pallet.GetPart<DraggedPart>() == null && Player.GetStatValue("Speed") == speed && Tick == tick && Energy == energy
                && !Zone.GetCell(original.x, original.y).BlocksMovement(Player));
            yield return Capture("fieldwork-04-pallet-moved-and-released");
            yield return Paid(FieldworkAction(pallet, "Harvest", "dismantle for timber (2)", "fieldwork-05-real-dismantle-menu"),
                "local", "fieldwork-finite-pallet-dismantled");
            Check("fieldwork_pallet_dismantled", Packed("SalvagedTimber") == 2 && CountGraphId(_fieldworkPalletId) == 0
                && pallet.GetPart<HarvestablePart>().Harvested && !DragSystem.IsDragging(Player)
                && !WorldInteractionSystem.GatherActions(pallet, Player).Any(a => a.Command == "Harvest"));
            yield return Capture("fieldwork-06-pallet-consumed");
            yield return ConnectedLeaveCellar();

            well = FieldworkGlade(_fieldworkWellId);
            yield return DistrictApproach(well, 140);
            yield return Paid(WorldAction(well, RepairablePart.RepairCommand), "local", "fieldwork-clay-to-working-well");
            Require(well.GetPart<WellPart>().IsUsable && DistrictClay() == 0, "two real clay spent on the working well");
            yield return FieldworkFill();
            Check("fieldwork_well_and_vessel", well.GetPart<RepairablePart>().Repaired && DistrictClay() == 0
                && FieldworkShell.GetPart<WaterskinPart>().Charges == 3 && CountGraphId(_fieldworkShellId) == 1
                && Packed("SalvagedTimber") == 2);
            yield return Capture("fieldwork-07-working-well-earned-shell");
            yield return FieldworkWater(FieldworkGrowing, "fieldwork-first-water");
            Check("fieldwork_first_water", _fieldworkWaters == 1 && FieldworkShell.GetPart<WaterskinPart>().Charges == 2
                && FieldworkGrowing.GetPart<CropPart>().MoistureTicks > 0 && FieldworkGrowth(FieldworkGrowing) < 56);
            yield return Capture("fieldwork-08-first-physical-watering");

            wicket = FieldworkGlade(_fieldworkWicketId);
            yield return DistrictWalk(Zone.GetCell(46, 7), 80);
            yield return Paid(WorldAction(wicket, RepairablePart.RepairCommand), "local", "fieldwork-two-timber-wicket-repair");
            Require(Packed("SalvagedTimber") == 0 && wicket.GetPart<RepairablePart>().Repaired && wicket.GetPart<DoorPart>().IsClosed,
                "repair spends both finite timber and restores a still-closed operable wicket");
            yield return Paid(WorldAction(wicket, DoorPart.OpenCommand), "local", "fieldwork-restored-wicket-open");
            yield return Capture("fieldwork-09a-repaired-wicket-open-before-crossing");
            var shortcut = PathTo(c => c == Zone.GetCell(46, 4));
            Require(shortcut != null && shortcut.Count < _fieldworkBrokenDetour && shortcut.Any(p => p == (46, 6)),
                "repaired opening measurably shortens the actual garden approach");
            yield return StepTo(46, 6); yield return StepTo(46, 5);
            yield return Paid(WorldAction(wicket, DoorPart.CloseCommand), "local", "fieldwork-close-after-real-crossing");
            Check("fieldwork_repaired_passage", At == Zone.GetCell(46, 5) && wicket.GetPart<DoorPart>().IsClosed
                && wicket.GetPart<RepairablePart>().Repaired && Packed("SalvagedTimber") == 0
                && Zone.GetCell(46, 6).BlocksMovement(Player));
            _observations.Add(new { phase = "fieldwork-measured-working-yard-route", brokenSteps = _fieldworkBrokenDetour,
                repairedOpenSteps = shortcut.Count, crossed = wicket.ID, closedAfterCrossing = true, localShortcutOnly = true });
            yield return Capture("fieldwork-09-repaired-wicket-crossed-and-closed");

            int growthBeforeTrip = FieldworkGrowth(FieldworkGrowing), worldBeforeTrip = WorldClock.CurrentTick;
            string notesBefore = NoteSignature();
            yield return TravelSurface("Overworld.11.9.0");
            var bank = Zone;
            var forage = DistrictOwner(bank, SpreadExplorationWorksites.RoleKey, "cold-forage");
            var still = DistrictOwner(bank, SpreadExplorationWorksites.RoleKey, "still");
            Require(forage.BlueprintName == "FrostLichenPatch" && forage.GetPart<HarvestablePart>()?.Harvested == false
                && still.BlueprintName == "AlchemyStill" && still.HasPart<AlchemyStillPart>() && Packed("FrostLichen") == 0,
                "actual untouched northern forage and field alembic, without supplied reagents");
            _fieldworkForageId = forage.ID;
            yield return DistrictApproach(forage, 100); tick = Tick; energy = Energy;
            yield return WorldAction(forage, "Examine"); yield return ReadPages("fieldwork-10a-real-cold-forage");
            Require(NormalizeText(_readerText).Contains("freezing coating") && Tick == tick && Energy == energy,
                "real free reader explains the acquired reagent's later use");
            yield return CloseNormal();
            yield return Paid(WorldAction(forage, "Harvest"), "local", "fieldwork-native-finite-frost-harvest");
            int frost = Packed("FrostLichen");
            Require(frost >= 1 && frost <= 2 && forage.GetPart<HarvestablePart>().Harvested && bank.GetEntityCell(forage) == null,
                "actual finite expedition forage acquired once");
            yield return DistrictApproach(still, 100); tick = Tick; energy = Energy;
            yield return WorldAction(still, "Examine"); yield return ReadPages("fieldwork-10b-real-alembic-source");
            Require(NormalizeText(_readerText).Contains("still also prepares batches") && Tick == tick && Energy == energy,
                "real free reader exposes a future useful expedition service");
            yield return CloseNormal();
            Check("fieldwork_actual_expedition", Zone.ZoneID == "Overworld.11.9.0" && _mapSteps > 0
                && Packed("FrostLichen") == frost && CountGraphId(_fieldworkForageId) == 0
                && ReferenceEquals(Manager.CachedZones[bank.ZoneID], bank) && NoteSignature() == notesBefore && Owns(Player, FieldworkShell));
            _observations.Add(new { phase = "fieldwork-useful-cold-bank-expedition", zone = bank.ZoneID,
                forage = _fieldworkForageId, harvestedUnits = frost, still = still.ID,
                scope = "Native finite forage and two source readers replace the unreachable Sill informant visit. No report note is claimed, no clearance waits are used, and no coating is brewed on this journey." });
            yield return TravelSurface(GleanersDistrict.SurfaceID);
            yield return DistrictApproach(FieldworkGrowing, 140);
            Check("fieldwork_return_growth", FieldworkGrowing != null && FieldworkGrowth(FieldworkGrowing) > growthBeforeTrip
                && FieldworkGrowth(FieldworkGrowing) <= 40 && FieldworkGrowing.GetPart<CropPart>().GrowthStage < 2
                && WorldClock.CurrentTick > worldBeforeTrip && FieldworkShell.GetPart<WaterskinPart>().Charges == 2);
            _observations.Add(new { phase = "fieldwork-elapsed-return", crop = _fieldworkGrowingId,
                beforeGrowth = growthBeforeTrip, afterGrowth = FieldworkGrowth(FieldworkGrowing), worldBeforeTrip,
                worldAfterTrip = WorldClock.CurrentTick, moisture = FieldworkGrowing.GetPart<CropPart>().MoistureTicks });
            yield return Capture("fieldwork-11-returned-same-growing-owner");
            yield return FieldworkWater(FieldworkGrowing, "fieldwork-retend-after-expedition");
            Check("fieldwork_retended", _fieldworkWaters == 2 && FieldworkShell.GetPart<WaterskinPart>().Charges == 1
                && FieldworkGrowing.GetPart<CropPart>().MoistureTicks > 0);

            // This is a useful refill/source inspection circuit, not resting or
            // repeated movement solely to manufacture growth time.
            yield return DistrictApproach(FieldworkGlade(_fieldworkWellId), 80); yield return FieldworkFill();
            var basin = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpreadDrawPoint");
            int basinVolume = basin.GetPart<LiquidPoolPart>().Volume;
            yield return DistrictApproach(basin, 80); tick = Tick; energy = Energy;
            yield return WorldAction(basin, "Examine"); yield return ReadPages("fieldwork-12-finite-basin-reader"); yield return CloseNormal();
            Check("fieldwork_finite_basin", basinVolume == 3 && basin.GetPart<LiquidPoolPart>().Volume == basinVolume
                && FieldworkShell.GetPart<WaterskinPart>().Charges == 3 && Tick == tick && Energy == energy);
            yield return DistrictApproach(FieldworkGrowing, 100);
            Require(FieldworkGrowing.GetPart<CropPart>().GrowthStage == 2,
                "actual useful refill/basin return completed the remaining wet growth; never pad the clock");
            yield return Capture("fieldwork-13-real-second-ripe-crop");
            var grown = FieldworkGrowing;
            yield return ConnectedHarvest(grown, "DrawgourdShell");
            Check("fieldwork_second_harvest", CountGraphId(_fieldworkGrowingId) == 0 && Packed("DrawgourdShell") == 2
                && Packed("DrawgourdSeed") == 1 && CountGraphId(_fieldworkShellId) == 1
                && FieldworkShell.GetPart<WaterskinPart>().Charges == 3 && CountGraphId(_fieldworkPalletId) == 0);
            yield return ConnectedReplantCurrentBed("DrawgourdSeed", "DrawgourdCrop"); _fieldworkFinalCropId = _connectedLastPlantId;
            yield return FieldworkWater(FieldworkGlade(_fieldworkFinalCropId), "fieldwork-next-cycle-water");
            Check("fieldwork_next_cycle", Packed("DrawgourdSeed") == 0 && Packed("DrawgourdShell") == 2
                && _fieldworkFinalCropId != _fieldworkGrowingId && FieldworkGrowth(FieldworkGlade(_fieldworkFinalCropId)) < 56
                && FieldworkGlade(_fieldworkFinalCropId).GetPart<CropPart>().MoistureTicks > 0
                && FieldworkShell.GetPart<WaterskinPart>().Charges == 2 && _fieldworkWaters == 3);
            yield return FieldworkCheckpoint();
            Check("fieldwork_finish", Player.GetProperty(StartingBuildService.PropertyName) == "duelist" && Player.GetStatValue("Hitpoints") > 0
                && !CombatSystem.IsDeathHandled(Player) && !DevMode.Enabled && !DebugInvincibility.IsEnabled(Player)
                && !Player.HasPart<BitLockerPart>() && _rests == 0 && _localInputs <= 650 && _mapSteps == 2 && State == "Normal");
            yield return Capture("fieldwork-15-restored-working-yard");
        }

        bool FieldworkNeedsAdjacentDefense(Entity target) => target != null && Zone.GetEntityCell(target) != null
            && !CombatSystem.IsDeathHandled(target) && target.GetStatValue("Hitpoints") > 0
            && SpatialQuery.Distance(Zone, Player, target) <= 1 && Threats(Zone).Contains(target)
            && !ReferenceGladeRouteControl.HasLiveCalm(Zone, target);

        bool FieldworkAuditedWeapon(Entity weapon)
        {
            if (weapon?.GetPart<PhysicsPart>()?.Equipped != Player || !InventorySystem.IsEquipped(Player, weapon)) return false;
            if (weapon.ID == _connectedOriginalDaggerId && weapon.BlueprintName == "Dagger") return true;
            return _soddenDistrict && !string.IsNullOrEmpty(_soddenForgedWeaponId) && weapon.ID == _soddenForgedWeaponId
                && weapon.GetPart<WeaponAssemblyPart>()?.BladeBlueprint == "PeatMalletHeadComponent"
                && weapon.GetPart<MeleeWeaponPart>()?.Attributes == "Bludgeoning Cudgel";
        }

        IEnumerator FieldworkDuelistDefense()
        {
            var oldGuard = _guard; string oldId = _guardId; bool oldFighting = _fighting;
            bool oldAttempt = _guardAttempt, oldDamage = _guardDamage, oldLethal = _guardLethal;
            try
            {
                while (true)
                {
                    var target = Threats(Zone).Where(FieldworkNeedsAdjacentDefense)
                        .OrderBy(e => e.ID, StringComparer.Ordinal).FirstOrDefault();
                    if (target == null) yield break;
                    var weapon = StartingBuildService.PrimaryHandWeapon(Player);
                    Require(_connectedDefenses < 6 && FieldworkAuditedWeapon(weapon),
                        "bounded ordinary close defense with the same starting dagger or exact earned Sodden mallet");
                    int hp = Player.GetStatValue("Hitpoints"), tonics = Packed("HealingTonic"), attacks = 0;
                    _connectedDefenses++; _guard = target; _guardId = target.ID; _fighting = true;
                    _guardAttempt = _guardDamage = _guardLethal = false;
                    while (FieldworkNeedsAdjacentDefense(target))
                    {
                        Require(attacks++ < 45, "bounded fieldwork adjacent defense; no pursuit or reroll");
                        if (Player.GetStatValue("Hitpoints") <= 24)
                        {
                            var tonic = Player.GetPart<InventoryPart>().Objects.FirstOrDefault(e => e.BlueprintName == "HealingTonic");
                            if (tonic != null)
                            {
                                int before = Packed("HealingTonic"), tick = Tick, energy = Energy;
                                yield return ItemAction(tonic, "ApplyTonic"); yield return CloseNormal();
                                Require(Packed("HealingTonic") == before - 1 && Tick == tick && Energy == energy,
                                    "actual finite original fieldwork tonic");
                            }
                        }
                        var cell = SpatialQuery.ClosestCell(Zone, target, At.X, At.Y);
                        yield return Paid(Tap(Direction(cell.X - At.X, cell.Y - At.Y)), "local", "fieldwork-adjacent-earned-weapon-defense");
                    }
                    Require(_guardAttempt && !FieldworkNeedsAdjacentDefense(target),
                        "actual acquired-weapon defense ended when the selected close threat died or separated");
                    _observations.Add(new { phase = "fieldwork-native-adjacent-defense", target = target.ID,
                        targetBlueprint = target.BlueprintName, defense = _connectedDefenses, attacks, weapon = weapon.ID, weaponBlueprint = weapon.BlueprintName,
                        hpBefore = hp, hpAfter = Player.GetStatValue("Hitpoints"), tonicsBefore = tonics,
                        tonicsAfter = Packed("HealingTonic"), endedWithDeath = CombatSystem.IsDeathHandled(target),
                        actualDamageObserved = _guardDamage, directLethalObserved = _guardLethal,
                        targetPosition = Zone.GetEntityPosition(target), playerPosition = Zone.GetEntityPosition(Player),
                        bound = "Ordinary adjacent attacks and original finite tonics only. Separation ends pursuit; every remaining adjacent hostile and the worksite route are checked again. Death is reported, not required." });
                    _fighting = false;
                    yield return Capture("fieldwork-adjacent-defense-" + _connectedDefenses);
                }
            }
            finally
            {
                _guard = oldGuard; _guardId = oldId; _fighting = oldFighting;
                _guardAttempt = oldAttempt; _guardDamage = oldDamage; _guardLethal = oldLethal;
            }
        }

        IEnumerator FieldworkConfrontObstruction(Func<Cell, bool> goal)
        {
            if (!_fieldwork || _connectedBuild != "duelist" || _fighting) yield break;
            var goalCells = Enumerable.Range(0, Zone.Width)
                .SelectMany(x => Enumerable.Range(0, Zone.Height).Select(y => Zone.GetCell(x, y))).Where(goal).ToArray();
            var target = Threats(Zone).Where(e => !ReferenceGladeRouteControl.HasLiveCalm(Zone, e)
                && goalCells.Any(c => SpatialQuery.DistanceToCell(Zone, e, c.X, c.Y) <= ThreatClearance))
                .OrderBy(e => SpatialQuery.Distance(Zone, Player, e)).ThenBy(e => e.ID, StringComparer.Ordinal).FirstOrDefault();
            if (target == null) yield break;
            var weapon = StartingBuildService.PrimaryHandWeapon(Player);
            Require(_connectedDefenses < 6 && FieldworkAuditedWeapon(weapon),
                "bounded ordinary Duelist confrontation with the same starting dagger or exact earned Sodden mallet");
            var oldGuard = _guard; string oldId = _guardId; bool oldFighting = _fighting;
            bool oldAttempt = _guardAttempt, oldDamage = _guardDamage, oldLethal = _guardLethal;
            int hp = Player.GetStatValue("Hitpoints"), tonics = Packed("HealingTonic");
            var targetPosition = Zone.GetEntityPosition(target);
            _connectedDefenses++; _guard = target; _guardId = target.ID;
            _guardAttempt = _guardDamage = _guardLethal = false;
            try
            {
                _observations.Add(new { phase = "fieldwork-ordinary-obstruction-approach", target = target.ID,
                    targetBlueprint = target.BlueprintName, targetPosition, playerPosition = Zone.GetEntityPosition(Player),
                    originalWeapon = weapon.ID, defense = _connectedDefenses,
                    goalCells = goalCells.Select(c => new { x = c.X, y = c.Y }).ToArray(),
                    bound = "The selected actual hostile obstructs the worksite. Only its observer clearance is relaxed during ordinary-key combat approach; all other hostile and terrain checks remain." });
                // The shared combat helper walks through normal input, checks
                // every paid clock window and uses only real carried tonics.
                yield return FightGuard(requireDirectLethal: false);
                Require(CombatSystem.IsDeathHandled(target) && _guardAttempt && _guardDamage,
                    "actual obstructing hostile resolved after observed audited-weapon attacks");
                _observations.Add(new { phase = "fieldwork-ordinary-obstruction-resolved", target = target.ID,
                    hpBefore = hp, hpAfter = Player.GetStatValue("Hitpoints"), tonicsBefore = tonics,
                    tonicsAfter = Packed("HealingTonic"), directLethalObserved = _guardLethal,
                    bound = "Death after observed damage may be bleeding or a counterattack; direct lethal attribution is separate." });
                yield return Capture("fieldwork-obstruction-defense-" + _connectedDefenses);
            }
            finally
            {
                _guard = oldGuard; _guardId = oldId; _fighting = oldFighting;
                _guardAttempt = oldAttempt; _guardDamage = oldDamage; _guardLethal = oldLethal;
            }
        }

        IEnumerator FieldworkFill()
        {
            var shell = FieldworkShell; var skin = shell.GetPart<WaterskinPart>();
            Require(Owns(Player, shell) && skin.Charges < skin.Capacity
                && SpatialQuery.Distance(Zone, Player, FieldworkGlade(_fieldworkWellId)) <= 1,
                "same earned non-full shell physically beside the repaired well");
            var well = FieldworkGlade(_fieldworkWellId);
            int before = skin.Charges; string marker = Mark("fieldwork-real-shell-fill");
            yield return Paid(ItemAction(shell, "FillWaterskin"), "local", "fieldwork-paid-shell-fill");
            Require(skin.Charges == skin.Capacity && skin.Charges > before && well.GetPart<WellPart>().IsUsable
                && Window(marker).Any(e => e.Category == "event" && e.Kind == "WaterskinFilled"
                    && e.ActorId == Player.ID && e.TargetId == shell.ID
                    && JObject.Parse(e.PayloadJson)["source"]?.Value<string>() == well.BlueprintName
                    && JObject.Parse(e.PayloadJson)["amount"]?.Value<int>() == skin.Capacity - before
                    && JObject.Parse(e.PayloadJson)["remaining"]?.Value<int>() == skin.Capacity),
                "native paid fill uses the exact acquired vessel and actual nearby well");
        }

        IEnumerator FieldworkWater(Entity crop, string label)
        {
            yield return DistrictApproach(crop, 140);
            var shell = FieldworkShell; int before = shell.GetPart<WaterskinPart>().Charges;
            Require(before > 0 && crop.GetPart<CropPart>().GrowthStage < 2, "actual filled shell and unripe planted owner");
            string command = WorldInteractionSystem.GatherActions(crop, Player).Single(a => CropWateringService.IsCommand(a.Command)
                && a.Command.EndsWith(Uri.EscapeDataString(shell.ID), StringComparison.Ordinal)).Command;
            string marker = Mark(label + "-supply");
            yield return Paid(FieldworkAction(crop, command, "water with " + shell.GetDisplayName() + " (1 of " + before + ")",
                "fieldwork-water-menu-" + (_fieldworkWaters + 1)), "local", label);
            Require(shell.GetPart<WaterskinPart>().Charges == before - 1 && CountGraphId(shell.ID) == 1
                && crop.GetPart<CropPart>().MoistureTicks > 0
                && Window(marker).Any(e => e.Kind == "HandWatered" && e.ActorId == Player.ID && e.TargetId == crop.ID),
                "selected real vessel spends exactly one unit on this actual crop");
            _fieldworkWaters++;
        }

        IEnumerator FieldworkAction(Entity target, string command, string display, string screenshot)
        {
            Require(Zone.GetEntityCell(target) != null && SpatialQuery.Distance(Zone, Player, target) <= 1,
                "actual adjacent fieldwork target");
            var to = SpatialQuery.ClosestCell(Zone, target, At.X, At.Y);
            yield return Tap(Key.C); Require(State == "AwaitingTalkDirection", "native fieldwork direction");
            yield return Tap(to == At ? Key.Period : Direction(to.X - At.X, to.Y - At.Y));
            Require(State == "WorldActionMenuOpen", "actual fieldwork world menu");
            string choose = WorldInteractionSystem.PickTargetCommandPrefix + target.ID;
            var offered = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            if ((_input.WorldActionMenuUI.SelectedCellIsPile || !ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target))
                && !offered.Any(a => a.Command == choose) && offered.Any(a => a.Command == WorldInteractionSystem.PickCellCommand))
                yield return MenuAction(WorldInteractionSystem.PickCellCommand);
            if (!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target)
                || ((List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions")).Any(a => a.Command == choose))
                yield return MenuAction(choose);
            offered = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target)
                && offered.Single(a => a.Command == command).Display == display, "exact target, command and factual fieldwork display");
            yield return Capture(screenshot); yield return MenuAction(command);
        }

        string FieldworkDigest()
        {
            var well = FieldworkGlade(_fieldworkWellId); var wicket = FieldworkGlade(_fieldworkWicketId);
            var soil = FieldworkGlade(_fieldworkSoilId); var crop = FieldworkGlade(_fieldworkFinalCropId); var growth = crop.GetPart<CropPart>();
            var crate = ConnectedAt(GleanersCellarBuilder.ZoneID, _fieldworkCrateId);
            return string.Join("|", Player.ID, _fieldworkShellId, FieldworkShell.GetPart<WaterskinPart>().Charges,
                string.Join(",", Player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "DrawgourdShell")
                    .OrderBy(e => e.ID, StringComparer.Ordinal).Select(e => e.ID + ":" + e.GetPart<WaterskinPart>().Charges)),
                well.ID, well.GetPart<RepairablePart>().Repaired, wicket.ID, wicket.GetPart<RepairablePart>().Repaired,
                wicket.GetPart<DoorPart>().IsOpen, soil.ID, Manager.CachedZones[GleanersDistrict.SurfaceID].GetEntityPosition(soil),
                crop.ID, growth.GrowthStage, growth.TicksInStage, growth.MoistureTicks, growth.GrowthTimingVersion,
                growth.LastGrowthWorldTick, growth.GrowthWetTickRemainder, crate.ID, crate.GetPart<ContainerPart>().Contents.Count,
                CountGraphId(_fieldworkPalletId), CountGraphId(_fieldworkOriginalCropId), CountGraphId(_fieldworkGrowingId),
                Packed("SalvagedTimber"), DistrictClay(), Packed("DrawgourdSeed"),
                CountGraphId(_fieldworkForageId), Packed("FrostLichen"), NoteSignature());
        }

        IEnumerator FieldworkCheckpoint()
        {
            var player = Player; var zone = Zone; var well = FieldworkGlade(_fieldworkWellId);
            var crop = FieldworkGlade(_fieldworkFinalCropId);
            string digest = FieldworkDigest(), stats = Stats(Player), gear = Gear(Player), file = SaveFile(), old = HashFile(file);
            int tick = Tick, energy = Energy, world = WorldClock.CurrentTick, x = At.X, y = At.Y;
            yield return Tap(Key.F5); yield return Settled(); _checkpointHash = HashFile(file);
            Require(old != _checkpointHash && MessageLog.GetLast() == "Game saved.", "actual successful fieldwork quicksave");
            var step = Steps.Select(d => Zone.GetCell(x + d.x, y + d.y))
                .FirstOrDefault(c => Safe(Zone, c, ThreatClearance) && Zone.CanPlaceFootprint(Player, c.X, c.Y));
            Require(step != null, "one genuine unsaved ordinary fieldwork step");
            yield return StepTo(step.X, step.Y);
            Require(Tick > tick && HashFile(file) == _checkpointHash, "real unsaved time and position change leaves checkpoint intact");
            yield return Reload(player);
            Check("fieldwork_save_restore", !ReferenceEquals(Player, player) && !ReferenceEquals(Zone, zone)
                && !ReferenceEquals(FieldworkGlade(_fieldworkWellId), well) && !ReferenceEquals(FieldworkGlade(_fieldworkFinalCropId), crop)
                && At.X == x && At.Y == y && Tick == tick && Energy == energy && WorldClock.CurrentTick == world
                && Stats(Player) == stats && Gear(Player) == gear && FieldworkDigest() == digest && HashFile(file) == _checkpointHash);
            yield return Capture("fieldwork-14-real-save-restored");
        }
    }
}
