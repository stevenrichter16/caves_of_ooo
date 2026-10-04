using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite ordinary-key journey from an earned pallet to the existing southern store.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _trapJamming;
        const string TrapJammingZone = "Overworld.11.11.0";
        const string TrapJammingIntent = "Actual seed64 Duelist picker, ordinary cellar descent and covered passage, finite native pallet dismantled into two timber, ordinary return and one-south map journey to the original trapper dispatch yard. Harvest outside lichen, use the actual Haul menu and two paid slowed steps to clear the beam entrance without spending timber, enter that opening and recover the original finite cache. If its original trap survives, Examine and jam it for one timber; if an NPC has sprung it, require the original trap's actual damage/death evidence and preserve both timber. Cross the observed gate, leave and revisit by world map, then F5/unsaved-step/F6 that same aftermath. The two-timber garden wicket remains unrepaired.";
        const string TrapJammingLimits = "One predeclared seed, build and store; not blind discovery, every world or campaign balance. Required checks distinguish a player-jammed original trap from a trap already consumed by an NPC. The latter proves native trap damage and persistent absence, not player jamming or necessity of hauling as the only access. A null owner never counts as armed. Independent route sufficiency and jamming are covered separately by EditMode and the earlier native jamming baseline. No player transfers, owner grants, AI suppression, timing retries, fake triggers or edited clocks. Bounded native Duelist defense uses the original dagger and finite starter tonics and can fail against real threats. Only a verified jammed original trigger receives an observer crossing exception; every other hazard remains excluded. Cache stock may lawfully merge, so exact source rows and conserved units are reported. Models and screenshots require independent pixel review.";
        string[] TrapJammingChecks => _trapConsumedByOther ? TrapConsumedChecks : TrapJammedChecks;
        static readonly string[] TrapJammedChecks = {
            "ordinary_start", "trap_original_timber_choice", "trap_finite_pallet_acquired",
            "trap_actual_southern_store", "trap_dispatch_original_yard", "trap_shallow_forage",
            "trap_beam_native_readout", "trap_beam_gripped", "trap_beam_two_paid_moves", "trap_beam_released",
            "trap_haul_gate_crossed", "trap_original_cache_recovered", "trap_depleted_cache_not_refilled",
            "trap_native_readout", "trap_one_timber_jam", "trap_original_owner_crossed", "trap_actual_revisit",
            "trap_checkpoint_saved", "trap_saved_replacement",
            "trap_restored_owner_crossed", "trap_finish" };
        static readonly string[] TrapConsumedChecks = {
            "ordinary_start", "trap_original_timber_choice", "trap_finite_pallet_acquired",
            "trap_actual_southern_store", "trap_dispatch_original_yard", "trap_consumed_by_native_actor", "trap_shallow_forage",
            "trap_beam_native_readout", "trap_beam_gripped", "trap_beam_two_paid_moves", "trap_beam_released",
            "trap_haul_gate_crossed", "trap_original_cache_recovered", "trap_depleted_cache_not_refilled",
            "trap_spent_gate_crossed", "trap_actual_revisit", "trap_checkpoint_saved", "trap_spent_saved_replacement",
            "trap_restored_spent_gate_crossed", "trap_finish" };
        string _trapPalletId, _trapTimberId, _trapWicketId, _trapOwnerId;

        public void InitializeTrapJamming(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Isolated launcher required before trap jamming bootstrap.");
            _trapJamming = true;
            // This flag selects existing bounded native defense helpers. It does
            // not change world generation, actors, inventory or AI authority.
            Initialize(context, connectedBuild: "duelist", fieldwork: true);
        }

        Entity TrapTimber => Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e => e.ID == _trapTimberId && Owns(Player, e));
        Entity TrapWicket => ConnectedAt(GleanersDistrict.SurfaceID, _trapWicketId);
        Entity TrapOwner => Owner(_trapOwnerId);

        IEnumerator TrapJammingJourney()
        {
            Require(Manager.WorldSeed == 64 && _connectedBuild == "duelist" && Manager.Exploration?.Enabled == true,
                "predeclared ordinary source and selected Duelist");
            var wicket = DistrictOwner(Zone, ReferenceGladeBuilder.FieldworkRoleKey, "wicket"); _trapWicketId = wicket.ID;
            Check("trap_original_timber_choice", Packed("SalvagedTimber") == 0 && wicket.GetPart<RepairablePart>()?.Repaired == false
                && wicket.GetPart<DoorPart>()?.IsClosed == true && wicket.GetPart<RepairablePart>()?.RecipeId == "timber-wicket-hinge");
            yield return DistrictWalk(Zone.GetCell(GleanersDistrict.EntranceX, GleanersDistrict.EntranceY), 140);
            yield return Paid(Tap(Key.LeftShift, Key.Period), "local", "trap-native-cellar-descent");
            Require(Zone.ZoneID == GleanersCellarBuilder.ZoneID, "actual cellar reached through original stairs");
            var pallet = DistrictOwner(Zone, GleanersCellarBuilder.RoleKey, "timber-pallet"); _trapPalletId = pallet.ID;
            Require(pallet.BlueprintName == "GleanersTimberPallet" && pallet.GetPart<HarvestablePart>()?.YieldMin == 2
                && pallet.GetPart<HarvestablePart>()?.YieldMax == 2 && !pallet.GetPart<HarvestablePart>().Harvested,
                "actual intact two-timber pallet");
            foreach (var at in new[] { (40, 5), (45, 5), (46, 4), (46, 2), (50, 2), (50, 5), (62, 5), (63, 6) })
                yield return DistrictWalk(Zone.GetCell(at.Item1, at.Item2), 80);
            yield return DistrictApproach(pallet, 60);
            yield return Paid(FieldworkAction(pallet, "Harvest", "dismantle for timber (2)", "trap-01-earned-pallet-menu"),
                "local", "trap-native-pallet-dismantled");
            var timber = Player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "SalvagedTimber" && Owns(Player, e));
            _trapTimberId = timber.ID;
            Check("trap_finite_pallet_acquired", Packed("SalvagedTimber") == 2 && Units(timber) == 2 && CountGraphId(_trapPalletId) == 0
                && pallet.GetPart<HarvestablePart>().Harvested && !TrapWicket.GetPart<RepairablePart>().Repaired);
            yield return Capture("trap-02-two-earned-timber");
            yield return ConnectedLeaveCellar();
            yield return TravelSurface(TrapJammingZone);
            var trap = DistrictOwner(Zone, SpreadExplorationWorksites.RoleKey, "trap"); _trapOwnerId = trap.ID;
            Require(trap.BlueprintName == "SpikeTrap" && trap.GetPart<SpikeTrapTriggerPart>()?.ConsumeOnTrigger == true
                && trap.GetPart<TrapJammingPart>() != null && !TrapJammingPart.IsJammed(trap), "unchanged armed native store trap");
            var originalCell = Zone.GetEntityCell(trap);
            Check("trap_actual_southern_store", Zone.ZoneID == TrapJammingZone && _mapSteps == 1
                && Manager.Exploration.DispositionFor(Zone.ZoneID) == 2 && Packed("SalvagedTimber") == 2
                && ReferenceEquals(Manager.CachedZones[Zone.ZoneID], Zone));
            yield return TrapDispatchYard();
            Require(TrapGateBeforeJam(), "original trap is either physically armed or proved consumed by native NPC damage");
            if (_trapConsumedByOther)
            {
                yield return TrapSpentCross("trap_spent_gate_crossed");
            }
            else
            {
                yield return DistrictApproach(trap, 180);
                Require(ReferenceEquals(TrapOwner, trap) && Zone.GetEntityCell(trap) == originalCell && !TrapJammingPart.IsJammed(trap),
                    "live approach retains the original untriggered trap; never replace one sprung by an NPC");
                int tick = Tick, energy = Energy;
                yield return WorldAction(trap, "Examine"); yield return ReadPages("trap-03-armed-mechanism-reader"); yield return CloseNormal();
                Check("trap_native_readout", NormalizeText(_readerText).IndexOf("salvaged timber", StringComparison.OrdinalIgnoreCase) >= 0
                    && WorldInteractionSystem.GatherActions(trap, Player).Any(a => a.Command == TrapJammingPart.JamCommand)
                    && Tick == tick && Energy == energy && Packed("SalvagedTimber") == 2);
                _observations.Add(new { phase = "trap-armed-original-model", owner = trap.ID, cell = Zone.GetEntityPosition(trap), visual = CardVisual(trap) });
                yield return Paid(FieldworkAction(trap, TrapJammingPart.JamCommand, "jam mechanism (1 salvaged timber)", "trap-04-real-jamming-menu"),
                    "local", "trap-native-timber-jam");
                Check("trap_one_timber_jam", TrapJammingPart.IsJammed(trap) && ReferenceEquals(TrapOwner, trap)
                    && Zone.GetEntityCell(trap) == originalCell && Packed("SalvagedTimber") == 1 && Units(TrapTimber) == 1
                    && CountGraphId(_trapTimberId) == 1 && !TrapWicket.GetPart<RepairablePart>().Repaired
                    && !WorldInteractionSystem.GatherActions(trap, Player).Any(a => a.Command == TrapJammingPart.JamCommand));
                _observations.Add(new { phase = "trap-jammed-original-model", owner = trap.ID, cell = Zone.GetEntityPosition(trap), visual = CardVisual(trap) });
                yield return Capture("trap-05-jammed-same-owner");
                yield return TrapCross("trap_original_owner_crossed");
            }
            yield return TrapDispatchRevisit();
            yield return DistrictWalk(TrapYardCell(4, 0), 180);
            yield return TrapCheckpoint();
            Check("trap_finish", State == "Normal" && Player.GetStatValue("Hitpoints") > 10 && Packed("SalvagedTimber") == TrapExpectedTimber
                && TrapYardAftermath() && _mapSteps == 3 && !TrapWicket.GetPart<RepairablePart>().Repaired
                && !DevMode.Enabled && !Player.HasPart<BitLockerPart>() && !DebugInvincibility.IsEnabled(Player));
            yield return Capture("trap-09-finished-persistent-crossing");
        }

        IEnumerator TrapCross(string check)
        {
            var trap = TrapOwner; var cell = Zone.GetEntityCell(trap);
            Require(trap != null && trap.SpatialZone == Zone && cell != null && cell.IsVisible && TrapJammingPart.IsJammed(trap)
                && cell != At && SpatialQuery.Distance(Zone, Player, trap) == 1 && Zone.CanPlaceFootprint(Player, cell.X, cell.Y),
                "actual adjacent current jammed owner for physical crossing");
            var allowed = _allowedMarker;
            try
            {
                // Read-only route-observer exception for this exact already-jammed
                // owner; Safe still rejects every other trigger and cell hazard.
                _allowedMarker = trap;
                Require(Safe(Zone, cell, ThreatClearance), "no second live trap, creature, liquid, gas or tile hazard on crossing");
                int hp = Player.GetStatValue("Hitpoints");
                yield return Paid(Tap(Direction(cell.X - At.X, cell.Y - At.Y)), "local", check);
                Check(check, At == cell && ReferenceEquals(TrapOwner, trap) && TrapJammingPart.IsJammed(trap)
                    && Player.GetStatValue("Hitpoints") == hp && Packed("SalvagedTimber") == 1);
            }
            finally { _allowedMarker = allowed; }
            yield return Capture(check);
        }

        IEnumerator TrapCheckpoint()
        {
            var player = Player; var zone = Zone; var trap = TrapOwner; var timber = TrapTimber; var wicket = TrapWicket;
            var beam = TrapYardBeam; var cache = TrapYardCache; string yard = TrapYardDigest();
            string id = Player.ID, stats = Stats(Player), gear = Gear(Player), file = SaveFile(), old = HashFile(file), notes = NoteSignature();
            int tick = Tick, energy = Energy, world = WorldClock.CurrentTick, x = At.X, y = At.Y;
            yield return Tap(Key.F5); yield return Settled(); _checkpointHash = HashFile(file);
            Check("trap_checkpoint_saved", old != _checkpointHash && MessageLog.GetLast() == "Game saved."
                && SaveGameService.GetSaveInfo("Quick").ActiveZoneID == TrapJammingZone);
            var next = Steps.Select(d => Zone.GetCell(x + d.x, y + d.y))
                .FirstOrDefault(c => Safe(Zone, c, ThreatClearance) && Zone.CanPlaceFootprint(Player, c.X, c.Y));
            Require(next != null, "genuine safe unsaved action beside the observed gate");
            yield return StepTo(next.X, next.Y);
            Require(Tick > tick && HashFile(file) == _checkpointHash, "actual unsaved action after native quicksave");
            yield return Reload(player);
            Check(_trapConsumedByOther ? "trap_spent_saved_replacement" : "trap_saved_replacement",
                !ReferenceEquals(Player, player) && !ReferenceEquals(Zone, zone)
                && (_trapConsumedByOther ? trap == null && TrapOwner == null : !ReferenceEquals(TrapOwner, trap))
                && !ReferenceEquals(TrapTimber, timber) && !ReferenceEquals(TrapWicket, wicket)
                && !ReferenceEquals(TrapYardBeam, beam) && !ReferenceEquals(TrapYardCache, cache)
                && Player.ID == id && At.X == x && At.Y == y && Tick == tick && Energy == energy && WorldClock.CurrentTick == world
                && Stats(Player) == stats && Gear(Player) == gear && NoteSignature() == notes && HashFile(file) == _checkpointHash
                && CountGraphId(_trapPalletId) == 0 && Packed("SalvagedTimber") == TrapExpectedTimber
                && Units(TrapTimber) == TrapExpectedTimber && !TrapWicket.GetPart<RepairablePart>().Repaired
                && TrapYardAftermath() && TrapYardDigest() == yard);
            yield return Capture(_trapConsumedByOther ? "trap-07-real-restored-consumed-gate" : "trap-07-real-restored-jam");
            if (_trapConsumedByOther) yield return TrapSpentCross("trap_restored_spent_gate_crossed");
            else yield return TrapCross("trap_restored_owner_crossed");
        }
    }
}
