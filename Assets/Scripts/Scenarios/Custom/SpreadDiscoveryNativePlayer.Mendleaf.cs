using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _mendleafYard, _mendleafUsed, _mendleafRecovered;
        string _mendleafStillId, _mendleafHerbId, _mendleafMedicId, _mendleafBottleId, _mendleafBrewId;
        int _mendleafX, _mendleafY, _mendleafAxisX, _mendleafAxisY;
        const string MendleafZone = "Overworld.11.9.0";
        const string MendleafIntent = "Actual seed64 Duelist picker and original kit, direct ordinary north travel, original drying-yard geometry and medicinal stock, shallow native Mendleaf harvest, withdrawal and one actual field brew through the inventory crafting panel. Enter combat with the original dagger and finite starting tonics. Observe either the exact original enemy bottle used against the player or that same original bottle dropped and picked up after death. Actual map departure and revisit, return to the glade, and F5/unsaved-step/F6 preserve the resulting graph. No controlled injury or gameplay grants.";
        const string MendleafLimits = "One generated seed64 north yard and one real combat outcome, not both alternatives, all-seed balance, unaided discovery or guaranteed survival. Observer paths and fixed-Speed clock proofs may honestly refuse after native terrain, enemy movement, status or dismemberment changes; raw rejected windows remain in the report. No player or source transfers, grants, target injury, AI suppression, RNG/time changes or reroll. The shallow BrewedTonic is actual1d4 medicine, not a claim of healing while at full HP. The enemy's used and recovered original bottle are distinct required-check branches; later ordinary combat can consume carried medicine. Postfight departure uses the ordinary paid < map command from the current cell; it does not prove a safe on-foot escape from live foes. Model mapping is tested separately; screenshots still require independent visual review.";
        string[] MendleafChecks => new[] { "ordinary_start", "mendleaf_actual_north_yard", "mendleaf_shallow_harvest", "mendleaf_withdrawn_with_herb",
            "mendleaf_native_single_brew", _mendleafRecovered ? "mendleaf_original_bottle_recovered" : "mendleaf_original_bottle_used",
            "mendleaf_actual_departure", "mendleaf_actual_revisit", "mendleaf_actual_homecoming", "mendleaf_checkpoint_saved", "mendleaf_saved_replacement", "mendleaf_finish" };
        public void InitializeMendleafYard(ScenarioContext context)
        {
            _mendleafYard = true;
            foreach (string channel in new[] { "ai", "alchemy", "loot" })
            { _oldChannels[channel] = Diag.IsChannelEnabled(channel); Diag.SetChannel(channel, true); }
            Initialize(context, connectedBuild: "duelist", fieldwork: true);
        }
        Cell MendleafCell(int x, int y) => Zone.GetCell(_mendleafX + _mendleafAxisX * x - _mendleafAxisY * y,
            _mendleafY + _mendleafAxisY * x + _mendleafAxisX * y);
        Entity MendleafOwner(string id) => ConnectedAt(MendleafZone, id);
        Diag.Entry[] MendleafRows() => _windows.SelectMany(window => (IEnumerable<Diag.Entry>)window.GetType().GetProperty("rows").GetValue(window)).ToArray();

        IEnumerator MendleafJourney()
        {
            Require(Packed("MendleafSprig") == 0 && Packed("BrewedTonic") == 0, "no starting medicine reward supplied");
            var originalPlayer = Player;
            yield return TravelSurface(MendleafZone);
            var yard = Zone;
            var still = DistrictOwner(yard, SpreadExplorationWorksites.RoleKey, "still");
            var herb = DistrictOwner(yard, SpreadExplorationWorksites.RoleKey, "medicine-forage");
            var medic = DistrictOwner(yard, SpreadExplorationWorksites.RoleKey, "patchbearer");
            var bottle = medic.GetPart<FieldMedicinePart>()?.FindCarriedMedicine();
            Require(bottle != null && bottle.BlueprintName == "HealingTonic" && Units(bottle) == 1, "real original one-bottle native medical loadout");
            _mendleafStillId = still.ID; _mendleafHerbId = herb.ID; _mendleafMedicId = medic.ID; _mendleafBottleId = bottle.ID;
            var origin = yard.GetEntityPosition(still); var hp = yard.GetEntityPosition(herb);
            _mendleafX = origin.x; _mendleafY = origin.y;
            var axes = new[] { (x: 1, y: 0), (x: 0, y: 1), (x: -1, y: 0), (x: 0, y: -1) }
                .Where(axis => hp == (origin.x - 7 * axis.x - 2 * axis.y, origin.y - 7 * axis.y + 2 * axis.x)).ToArray();
            Require(axes.Length == 1, "actual stable herb/still owners define exactly one approved quarter-turn layout");
            _mendleafAxisX = axes[0].x; _mendleafAxisY = axes[0].y;
            var shelves = yard.GetReadOnlyEntities().Where(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == "drying-shelf").ToArray();
            Check("mendleaf_actual_north_yard", ReferenceEquals(Player, originalPlayer) && _mapSteps == 1
                && Zone.ZoneID == MendleafZone && Manager.Exploration.DispositionFor(MendleafZone) == 2
                && still.BlueprintName == "AlchemyStill" && herb.BlueprintName == "MendleafPlant"
                && herb.GetPart<HarvestablePart>()?.Harvested == false && medic.BlueprintName == "MarlbackPatchbearer"
                && medic.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "HealingTonic" && Owns(medic, e)).Sum(Units) == 1
                && shelves.Length == 2 && shelves.All(e => e.BlueprintName == "AlchemyShelf" && e.GetPart<ContainerPart>()?.Contents.Count == 0)
                && shelves.Any(e => yard.GetEntityCell(e) == MendleafCell(4, -1)) && shelves.Any(e => yard.GetEntityCell(e) == MendleafCell(4, 1)));
            _observations.Add(new { phase = "mendleaf-original-generated-yard", still = still.ID, herb = herb.ID, medic = medic.ID, medicine = bottle.ID,
                herbCell = hp, stillCell = origin, actualMedicCell = yard.GetEntityPosition(medic), medicHp = medic.GetStatValue("Hitpoints"),
                medicineOwner = bottle.GetPart<PhysicsPart>()?.InInventory?.ID, medicineHealing = bottle.GetPart<TonicPart>()?.Healing,
                shelves = shelves.Select(e => new { id = e.ID, at = yard.GetEntityPosition(e), contents = e.GetPart<ContainerPart>().Contents.Count }).ToArray(),
                bound = "These are the cold-generated actual owners. A medic may move during ordinary map entry; no actor is reset to its authored anchor." });
            yield return Capture("mendleaf-01-actual-northern-yard");

            // Only the existing observer sight-exclusion policy changes here;
            // game AI and actor state remain live throughout the shallow route.
            _guard = medic; _guardId = medic.ID; _avoidGuard = true;
            int playerHp = Player.GetStatValue("Hitpoints"), medicHp = medic.GetStatValue("Hitpoints");
            try
            {
                yield return Approach(herb, 180);
                yield return WorldAction(herb, "Examine"); yield return ReadPages("mendleaf-02-harvest-reader"); yield return CloseNormal();
                yield return Paid(WorldAction(herb, "Harvest"), "local", "mendleaf-finite-native-harvest");
                Check("mendleaf_shallow_harvest", herb.GetPart<HarvestablePart>().Harvested && CountGraphId(herb.ID) == 0
                    && Packed("MendleafSprig") >= 1 && Packed("MendleafSprig") <= 2
                    && medic.GetPart<FieldMedicinePart>().FindCarriedMedicine() == bottle && medic.GetStatValue("Hitpoints") == medicHp
                    && Player.GetStatValue("Hitpoints") == playerHp && _connectedDefenses == 0);
                yield return MendleafWithdraw();
                Check("mendleaf_withdrawn_with_herb", !AlchemyStillPart.IsNearStill(Player, Zone)
                    && Packed("MendleafSprig") >= 1 && medic.GetPart<FieldMedicinePart>().FindCarriedMedicine() == bottle
                    && Player.GetStatValue("Hitpoints") == playerHp && _connectedDefenses == 0);
                yield return MendleafFieldBrew();
            }
            finally { _avoidGuard = false; _guard = null; _guardId = null; }

            yield return MendleafCombat(medic, bottle);
            Require(MendleafAftermath(), "actual medicine outcome and harvested plant before ordinary departure");
            _observations.Add(new { phase = "mendleaf-postfight-native-map-departure", x = At.X, y = At.Y,
                hp = Player.GetStatValue("Hitpoints"), stats = Stats(Player),
                bound = "The existing ordinary paid < command permits map departure from this actual cell. This is not evidence of a safe on-foot escape from the remaining live foes; the earlier shallow herb withdrawal was separately required." });
            yield return TravelSurface(GleanersDistrict.SurfaceID);
            Check("mendleaf_actual_departure", _mapSteps == 2 && ReferenceEquals(Manager.CachedZones[MendleafZone], yard) && MendleafAftermath());
            yield return Capture("mendleaf-06-returned-with-actual-medicine-outcome");
            yield return TravelSurface(MendleafZone);
            Check("mendleaf_actual_revisit", _mapSteps == 3 && ReferenceEquals(Zone, yard) && MendleafAftermath()
                && ReferenceEquals(MendleafOwner(_mendleafStillId), still));
            yield return Capture("mendleaf-07-actual-revisit-no-regrowth-or-bottle-refund");
            yield return TravelSurface(GleanersDistrict.SurfaceID);
            Check("mendleaf_actual_homecoming", _mapSteps == 4 && MendleafAftermath() && Zone.ZoneID == GleanersDistrict.SurfaceID);
            yield return MendleafCheckpoint();
            Check("mendleaf_finish", _mapSteps == 4 && _localInputs <= 500 && _rests == 0 && Player.GetStatValue("Hitpoints") > 0
                && !CombatSystem.IsDeathHandled(Player) && !DevMode.Enabled && !DebugInvincibility.IsEnabled(Player)
                && !Player.HasPart<BitLockerPart>() && State == "Normal" && MendleafAftermath());
            yield return Capture("mendleaf-09-restored-ordinary-homecoming");
        }

        IEnumerator MendleafWithdraw()
        {
            var source = MendleafCell(-7, 2);
            var path = PathTo(c =>
            {
                int localX = (c.X - _mendleafX) * _mendleafAxisX + (c.Y - _mendleafY) * _mendleafAxisY;
                int distance = Math.Max(Math.Abs(c.X - source.X), Math.Abs(c.Y - source.Y));
                return localX <= -7 && distance >= 2 && distance <= 5 && Safe(Zone, c, ThreatClearance);
            });
            Require(path != null, "actual current reachable outer-margin withdrawal, without assuming an unpromised clear tile");
            var target = path.Count == 0 ? At : Zone.GetCell(path[path.Count - 1].x, path[path.Count - 1].y);
            _observations.Add(new { phase = "mendleaf-observed-withdrawal-destination", x = target.X, y = target.Y,
                plannedSteps = path.Count, bound = "Read-only current route selection; untouched scenery and live threats remain authoritative." });
            yield return WalkTo(target, 180);
        }

        IEnumerator MendleafFieldBrew()
        {
            var inventory = Player.GetPart<InventoryPart>();
            var sprig = inventory.Objects.Single(e => e.BlueprintName == "MendleafSprig" && Owns(Player, e));
            int before = Packed("MendleafSprig"), tick = Tick, energy = Energy;
            Require(!AlchemyStillPart.IsNearStill(Player, Zone), "actual field brew is away from a still");
            var prior = new HashSet<string>(inventory.Objects.Select(e => e.ID));
            yield return Tap(Key.I);
            for (int n = 0; (int)Field(_input.InventoryUI, "_panel") != 4; n++)
            { Require(n < 6, "bounded actual crafting panel"); yield return Tap(Key.Tab); }
            yield return Tap(Key.B); yield return Tap(Key.C);
            var rows = (IList)Field(_input.InventoryUI, "_craftRows"); int row = -1;
            for (int i = 0; i < rows.Count; i++) if (ReferenceEquals(Field(rows[i], "Item"), sprig)) row = i;
            Require(row >= 0, "earned sprig appears in current native brew picker");
            for (int n = 0; (int)Field(_input.InventoryUI, "_craftCursorIndex") != row; n++)
            { Require(n < 80, "bounded exact sprig cursor"); yield return Tap((int)Field(_input.InventoryUI, "_craftCursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Space);
            var picks = ((IList)Field(_input.InventoryUI, "_pickedReagents")).Cast<Entity>().ToArray();
            Require(picks.Length == 1 && ReferenceEquals(picks[0], sprig) && CraftingMarkPart.IsMarked(sprig), "one exact earned native reagent pick");
            yield return Capture("mendleaf-03-native-single-field-brew-preview");
            string marker = Mark("mendleaf-native-field-brew");
            yield return Tap(Key.Enter); yield return CloseNormal();
            var brew = inventory.Objects.Single(e => !prior.Contains(e.ID) && e.BlueprintName == "BrewedTonic");
            _mendleafBrewId = brew.ID; var evidence = Window(marker);
            Check("mendleaf_native_single_brew", Packed("MendleafSprig") == before - 1 && Units(brew) == 1
                && Owns(Player, brew) && brew.GetPart<TonicPart>()?.Healing == "1d4"
                && Tick == tick && Energy == energy && !AlchemyStillPart.IsNearStill(Player, Zone)
                && evidence.Any(e => e.Category == "alchemy" && e.Kind == "BrewResolved" && e.ActorId == Player.ID && e.TargetId == brew.ID));
            _observations.Add(new { phase = "mendleaf-real-field-brew", sprig = sprig.ID, before, after = Packed("MendleafSprig"), brew = brew.ID,
                healing = brew.GetPart<TonicPart>().Healing, marker, rows = evidence, bound = "Single native pack brew, actual one-unit expense, free current inventory command. Output is retained; no full-HP healing benefit claim." });
            yield return Capture("mendleaf-04-earned-weak-mending-tonic");
        }

        bool MendleafMedicineWasUsed()
        {
            var rows = MendleafRows();
            var use = rows.FirstOrDefault(e => e.Category == "ai" && e.Kind == "FieldMedicineUsed" && e.ActorId == _mendleafMedicId && e.TargetId == _mendleafBottleId
                && JObject.Parse(e.PayloadJson)["threatId"]?.Value<string>() == Player.ID);
            if (string.IsNullOrEmpty(use.TraceId)) return false;
            var data = JObject.Parse(use.PayloadJson);
            return data["hpAfter"]?.Value<int>() > data["hpBefore"]?.Value<int>() && data["remainingUnits"]?.Value<int>() == 0
                && rows.Any(e => e.Category == "event" && e.Kind == "TonicApplied" && e.ActorId == _mendleafMedicId && e.TargetId == _mendleafMedicId
                    && JObject.Parse(e.PayloadJson)["item"]?.Value<string>() == _mendleafBottleId
                    && JObject.Parse(e.PayloadJson)["consumed"]?.Value<bool>() == true);
        }

        IEnumerator MendleafCombat(Entity medic, Entity bottle)
        {
            Require(medic.GetPart<FieldMedicinePart>().FindCarriedMedicine() == bottle, "original medicine still carried at actual combat decision");
            _guard = medic; _guardId = medic.ID; _fighting = true;
            _guardAttempt = _guardDamage = _guardLethal = false;
            bool readout = false;
            try
            {
                for (int actions = 0; actions < 45; actions++)
                {
                    if (MendleafMedicineWasUsed() || CombatSystem.IsDeathHandled(medic)) break;
                    var weapon = StartingBuildService.PrimaryHandWeapon(Player);
                    Require(weapon?.ID == _connectedOriginalDaggerId && weapon.GetPart<PhysicsPart>()?.Equipped == Player,
                        "same starting dagger, with no combat loadout grant");
                    if (Player.GetStatValue("Hitpoints") <= 24)
                    {
                        var tonic = Player.GetPart<InventoryPart>().Objects.FirstOrDefault(e => e.BlueprintName == "HealingTonic" && Owns(Player, e));
                        if (tonic != null)
                        {
                            int count = Packed("HealingTonic"), tick = Tick, energy = Energy;
                            yield return ItemAction(tonic, "ApplyTonic"); yield return CloseNormal();
                            Require(Packed("HealingTonic") == count - 1 && Tick == tick && Energy == energy, "ordinary finite original tonic used through native inventory");
                        }
                    }
                    if (SpatialQuery.Distance(Zone, Player, medic) <= 1)
                    {
                        if (!readout)
                        {
                            yield return WorldAction(medic, "Examine"); yield return ReadPages("mendleaf-05-live-medical-harness-reader"); yield return CloseNormal();
                            Require(NormalizeText(_readerText).IndexOf("field medicine", StringComparison.OrdinalIgnoreCase) >= 0, "actual medical stock/counterplay is visible in the native reader");
                            readout = true;
                        }
                        var cell = SpatialQuery.ClosestCell(Zone, medic, At.X, At.Y);
                        yield return Paid(Tap(Direction(cell.X - At.X, cell.Y - At.Y)), "local", "mendleaf-starting-dagger-attack");
                    }
                    else
                    {
                        var path = PathTo(c => SpatialQuery.DistanceToCell(Zone, medic, c.X, c.Y) == 1);
                        Require(path != null && path.Count > 0, "actual current medicine-encounter approach");
                        yield return StepTo(path[0].x, path[0].y);
                    }
                }
                Require(_guardAttempt && _guardDamage, "real selected-player attack and damage before medicinal outcome");
                _mendleafUsed = MendleafMedicineWasUsed();
                if (_mendleafUsed)
                {
                    Check("mendleaf_original_bottle_used", CountGraphId(_mendleafBottleId) == 0
                        && medic.GetPart<FieldMedicinePart>().FindCarriedMedicine() == null);
                    _observations.Add(new { phase = "mendleaf-original-bottle-used", medic = medic.ID, bottle = bottle.ID, rows = MendleafRows().Where(e =>
                        e.Kind == "FieldMedicineUsed" && e.ActorId == medic.ID || e.Kind == "TonicApplied" && e.ActorId == medic.ID).ToArray(),
                        stillAlive = !CombatSystem.IsDeathHandled(medic), bound = "Actual self-treatment replaced an enemy action. This branch does not claim recovery of that consumed bottle." });
                }
                else
                {
                    Require(CombatSystem.IsDeathHandled(medic) && Zone.GetEntityCell(bottle) != null
                        && bottle.GetPart<PhysicsPart>()?.InInventory == null && bottle.GetPart<PhysicsPart>()?.Equipped == null && Units(bottle) == 1,
                        "same original unspent bottle is actual death loot, not a same-blueprint random drop");
                    _fighting = false;
                    yield return DistrictWalk(Zone.GetEntityCell(bottle), 80);
                    int before = Packed("HealingTonic");
                    Require(Zone.GetEntityCell(bottle) == At && Units(bottle) == 1, "actual original bottle under player before ground pickup");
                    yield return Paid(MendleafPickupBottle(bottle), "local", "mendleaf-original-bottle-pickup");
                    _mendleafRecovered = true;
                    Check("mendleaf_original_bottle_recovered", Packed("HealingTonic") == before + 1
                        && (Owns(Player, bottle) ? CountGraphId(bottle.ID) == 1 : Units(bottle) == 0 && CountGraphId(bottle.ID) == 0));
                    _observations.Add(new { phase = "mendleaf-original-bottle-recovered", medic = medic.ID, bottle = bottle.ID,
                        before, after = Packed("HealingTonic"), bottleRetainedAsOwner = Owns(Player, bottle),
                        bound = "Native pickup of the observed original ground bottle; lawful stack merging may consume its source ID. This branch does not claim enemy medicine use." });
                }
            }
            finally { _fighting = false; _guard = null; _guardId = null; }
            yield return Capture(_mendleafUsed ? "mendleaf-05-enemy-bottle-spent" : "mendleaf-05-original-enemy-bottle-recovered");
        }

        IEnumerator MendleafPickupBottle(Entity bottle)
        {
            yield return Tap(Key.G);
            if (State == "PickupOpen")
            {
                var items = (List<Entity>)Field(_input.PickupUI, "_items");
                int row = items.FindIndex(e => ReferenceEquals(e, bottle));
                Require(row >= 0 && Field(_input.PickupUI, "_sourceContainer") == null, "exact original bottle offered in native ground pickup");
                for (int n = 0; (int)Field(_input.PickupUI, "_cursorIndex") != row; n++)
                { Require(n < 80, "bounded original bottle pickup cursor"); yield return Tap((int)Field(_input.PickupUI, "_cursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
                yield return Tap(Key.Enter);
            }
            yield return CloseNormal();
        }
        string MendleafSavedMedic()
        {
            var medic = MendleafOwner(_mendleafMedicId);
            return medic == null ? "absent" : medic.ID + ":" + Stats(medic) + ":" + Gear(medic)
                + ":" + Manager.CachedZones[MendleafZone].GetEntityPosition(medic);
        }
        bool MendleafAftermath()
        {
            var medic = MendleafOwner(_mendleafMedicId);
            var brew = OwnerOrCarried(_mendleafBrewId);
            return MendleafOwner(_mendleafHerbId) == null && CountGraphId(_mendleafHerbId) == 0
                && MendleafOwner(_mendleafStillId)?.BlueprintName == "AlchemyStill"
                && brew != null && Owns(Player, brew) && Units(brew) == 1 && brew.GetPart<TonicPart>()?.Healing == "1d4"
                && (_mendleafUsed ? CountGraphId(_mendleafBottleId) == 0 && (medic == null || medic.GetPart<FieldMedicinePart>()?.FindCarriedMedicine() == null)
                    : _mendleafRecovered && medic == null);
        }
        IEnumerator MendleafCheckpoint()
        {
            var player = Player; var zone = Zone; var yard = Manager.CachedZones[MendleafZone]; var still = MendleafOwner(_mendleafStillId);
            var brew = OwnerOrCarried(_mendleafBrewId); var medic = MendleafOwner(_mendleafMedicId); string medicState = MendleafSavedMedic();
            string id = player.ID, gear = Gear(player), stats = Stats(player), notes = NoteSignature(), file = SaveFile(), old = HashFile(file);
            int x = At.X, y = At.Y, tick = Tick, energy = Energy, world = WorldClock.CurrentTick;
            yield return Tap(Key.F5); yield return Settled(); _checkpointHash = HashFile(file);
            Check("mendleaf_checkpoint_saved", old != _checkpointHash && MessageLog.GetLast() == "Game saved."
                && SaveGameService.GetSaveInfo("Quick").ActiveZoneID == GleanersDistrict.SurfaceID);
            var next = Steps.Select(d => Zone.GetCell(x + d.x, y + d.y)).FirstOrDefault(c => Safe(Zone, c, ThreatClearance));
            Require(next != null, "one actual safe unsaved glade step"); yield return StepTo(next.X, next.Y);
            Require(Tick > tick && HashFile(file) == _checkpointHash, "actual unsaved step leaves saved file unchanged");
            yield return Reload(player);
            Check("mendleaf_saved_replacement", !ReferenceEquals(player, Player) && !ReferenceEquals(zone, Zone)
                && !ReferenceEquals(yard, Manager.CachedZones[MendleafZone]) && !ReferenceEquals(still, MendleafOwner(_mendleafStillId))
                && !ReferenceEquals(brew, OwnerOrCarried(_mendleafBrewId))
                && (medic == null ? MendleafOwner(_mendleafMedicId) == null : !ReferenceEquals(medic, MendleafOwner(_mendleafMedicId)))
                && Player.ID == id && At.X == x && At.Y == y && Tick == tick && Energy == energy && WorldClock.CurrentTick == world
                && Gear(Player) == gear && Stats(Player) == stats && NoteSignature() == notes && HashFile(file) == _checkpointHash
                && MendleafSavedMedic() == medicState && MendleafAftermath());
            yield return Capture("mendleaf-08-exact-saved-medicine-aftermath");
        }
    }
}
