using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Observes the ordinary generated dispatch yard; every change is
    /// made through the existing native input helpers, without setup grants.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        string _trapBeamId, _trapStockId, _trapForageId, _trapRecoveredStock;
        bool _trapConsumedByOther;
        int _trapCenterX, _trapCenterY, _trapAxisX, _trapAxisY;
        string[] _trapStockBlueprints = Array.Empty<string>();
        Entity TrapYardBeam => Owner(_trapBeamId);
        Entity TrapYardCache => Owner(_trapStockId);
        int TrapExpectedTimber => _trapConsumedByOther ? 2 : 1;

        // Derive orientation from the two physical gates. The unchanged source
        // cache deliberately has no worksite role or newly granted property.
        Cell TrapYardCell(int x, int y) => Zone.GetCell(_trapCenterX + _trapAxisX * x - _trapAxisY * y,
            _trapCenterY + _trapAxisY * x + _trapAxisX * y);

        IEnumerator TrapDispatchYard()
        {
            var beam = DistrictOwner(Zone, SpreadExplorationWorksites.RoleKey, "haul-bypass");
            var forage = DistrictOwner(Zone, SpreadExplorationWorksites.RoleKey, "cold-forage");
            var gate = Zone.GetEntityPosition(TrapOwner); var service = Zone.GetEntityPosition(beam);
            Require(Math.Abs(gate.x - service.x) + Math.Abs(gate.y - service.y) == 6
                && (gate.x == service.x || gate.y == service.y), "two opposite gates define the actual yard axis");
            _trapCenterX = (gate.x + service.x) / 2; _trapCenterY = (gate.y + service.y) / 2;
            _trapAxisX = (gate.x - service.x) / 6; _trapAxisY = (gate.y - service.y) / 6;
            var cache = TrapYardCell(0, 0).Objects.Single(e => (e.BlueprintName == "Crate" || e.BlueprintName == "Sack")
                && e.GetPart<ContainerPart>() != null);
            _trapBeamId = beam.ID; _trapStockId = cache.ID; _trapForageId = forage.ID;
            var stock = cache.GetPart<ContainerPart>().Contents.ToArray();
            string originalStock = TrapCacheRows(cache);
            bool ring = true;
            for (int y = -2; y <= 2; y++)
                for (int x = -3; x <= 3; x++)
                    if ((Math.Abs(x) == 3 || Math.Abs(y) == 2) && !(Math.Abs(x) == 3 && y == 0))
                        ring &= TrapYardCell(x, y).Objects.Any(e => e.BlueprintName == "StoneWall"
                            && e.GetProperty(SpreadExplorationWorksites.RoleKey) == "broken-wall");
            Check("trap_dispatch_original_yard", ring && stock.Length > 0
                && stock.All(e => e.GetPart<PhysicsPart>()?.InInventory == cache && Units(e) > 0)
                && beam.BlueprintName == "FallenBeam" && DragRules.WeightOf(beam) == 60
                && beam.GetPart<HandlingPart>()?.Carryable == false && !beam.HasPart<HarvestablePart>()
                && Zone.GetEntityCell(beam) == TrapYardCell(-3, 0) && TrapYardCell(-3, 0).BlocksMovement(Player)
                && Zone.GetEntityCell(forage) == TrapYardCell(-4, 2) && forage.GetPart<HarvestablePart>()?.Harvested == false
                && TrapOwner != null && !TrapJammingPart.IsJammed(TrapOwner) && Packed("SalvagedTimber") == 2);
            _observations.Add(new { phase = "dispatch-original-generated-stock", cache = cache.ID,
                cacheBlueprint = cache.BlueprintName, beam = beam.ID, trap = TrapOwner.ID, forage = forage.ID,
                center = new { x = _trapCenterX, y = _trapCenterY }, axis = new { x = _trapAxisX, y = _trapAxisY },
                stock = stock.Select(e => new { id = e.ID, blueprint = e.BlueprintName, units = Units(e),
                    owner = e.GetPart<PhysicsPart>()?.InInventory?.ID }).ToArray(),
                scope = "Exact original nonempty cache, located between physical gates; no source tag, item or owner was added by the audit." });

            yield return DistrictApproach(forage, 180);
            int frostBefore = Packed("FrostLichen");
            yield return Paid(WorldAction(forage, "Harvest"), "local", "dispatch-outside-finite-lichen");
            Require(TrapGateBeforeJam(), "original gate's armed or consumed state has current physical evidence");
            Check("trap_shallow_forage", Packed("FrostLichen") > frostBefore && Packed("FrostLichen") <= frostBefore + 2
                && forage.GetPart<HarvestablePart>().Harvested && CountGraphId(_trapForageId) == 0
                && Zone.GetEntityCell(beam) == TrapYardCell(-3, 0) && TrapGateBeforeJam()
                && Packed("SalvagedTimber") == 2 && TrapCacheRows(cache) == originalStock);
            yield return Capture("dispatch-01-outside-forage-observed-gate-state");

            yield return DistrictWalk(TrapYardCell(-4, 0), 180);
            Require(Zone.GetEntityCell(beam) == TrapYardCell(-3, 0) && TrapGateBeforeJam(),
                "actual unopened service gate and separately observed direct gate before hauling");
            int tick = Tick, energy = Energy, speed = Player.GetStatValue("Speed");
            yield return WorldAction(beam, "Examine"); yield return ReadPages("dispatch-02-beam-reader"); yield return CloseNormal();
            Check("trap_beam_native_readout", NormalizeText(_readerText).Contains("haul")
                && WorldInteractionSystem.GatherActions(beam, Player).Any(a => a.Command == HandlingPart.HaulCommand)
                && Tick == tick && Energy == energy && !DragSystem.IsDragging(Player));
            yield return FieldworkAction(beam, HandlingPart.HaulCommand, "haul", "dispatch-03-native-haul-menu");
            yield return CloseNormal();
            int expectedSpeed = speed - Math.Min(24, Math.Max(0, speed - DragSystem.MinimumHaulingSpeed));
            Check("trap_beam_gripped", DragSystem.GetDragged(Player) == beam && beam.HasPart<DraggedPart>()
                && Player.GetStatValue("Speed") == expectedSpeed && expectedSpeed < speed
                && Tick == tick && Energy == energy && Packed("SalvagedTimber") == 2);
            // Two ordinary paid moves park the beam farther into the validated
            // apron. The resulting route is cardinal, matching the shared audit
            // pathfinder without changing the game's corner-movement policy.
            int inputs = _localInputs;
            yield return StepTo(TrapYardCell(-5, 0).X, TrapYardCell(-5, 0).Y);
            Require(Zone.GetEntityCell(beam) == TrapYardCell(-4, 0), "first paid pull moves this beam into the vacated grip cell");
            yield return StepTo(TrapYardCell(-5, -1).X, TrapYardCell(-5, -1).Y);
            Check("trap_beam_two_paid_moves", At == TrapYardCell(-5, -1) && Zone.GetEntityCell(beam) == TrapYardCell(-5, 0)
                && DragSystem.GetDragged(Player) == beam && Player.GetStatValue("Speed") == expectedSpeed
                && _localInputs == inputs + 2 && Packed("SalvagedTimber") == 2 && Units(TrapTimber) == 2
                && TrapGateBeforeJam() && TrapCacheRows(cache) == originalStock);
            tick = Tick; energy = Energy;
            yield return FieldworkAction(beam, HandlingPart.ReleaseCommand, "let go", "dispatch-04-native-release-menu");
            yield return CloseNormal();
            Check("trap_beam_released", Zone.GetEntityCell(beam) == TrapYardCell(-5, 0) && !DragSystem.IsDragging(Player)
                && !beam.HasPart<DraggedPart>() && Player.GetStatValue("Speed") == speed
                && Tick == tick && Energy == energy && !TrapYardCell(-3, 0).BlocksMovement(Player)
                && Packed("SalvagedTimber") == 2 && TrapGateBeforeJam());
            foreach (var point in new[] { (-4, -1), (-4, 0), (-3, 0) })
                yield return DistrictWalk(TrapYardCell(point.Item1, point.Item2), 40);
            Check("trap_haul_gate_crossed", At == TrapYardCell(-3, 0) && Zone.GetEntityCell(beam) == TrapYardCell(-5, 0)
                && Packed("SalvagedTimber") == 2 && TrapGateBeforeJam()
                && TrapCacheRows(cache) == originalStock && !TrapWicket.GetPart<RepairablePart>().Repaired);
            yield return Capture("dispatch-05-service-gate-entered-with-both-timber");

            yield return DistrictApproach(cache, 100);
            Require(TrapCacheRows(cache) == originalStock && TrapGateBeforeJam(),
                "ordinary service approach retains original finite cache and an evidenced other gate state");
            var quantities = stock.GroupBy(e => e.BlueprintName).ToDictionary(g => g.Key, g => g.Sum(Units));
            var before = quantities.Keys.ToDictionary(blueprint => blueprint, Packed);
            _trapStockBlueprints = quantities.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            yield return Paid(DistrictTakeCache(cache, stock), "local", "dispatch-original-stock-native-take");
            Check("trap_original_cache_recovered", cache.GetPart<ContainerPart>().Contents.Count == 0
                && quantities.All(row => Packed(row.Key) == before[row.Key] + row.Value)
                && stock.All(e => Owns(Player, e) ? CountGraphId(e.ID) == 1 : Units(e) == 0 && CountGraphId(e.ID) == 0)
                && Packed("SalvagedTimber") == 2 && TrapGateBeforeJam());
            _trapRecoveredStock = TrapRecoveredStockRows();
            _observations.Add(new { phase = "dispatch-finite-stock-recovered", originalCache = cache.ID,
                sourceIds = stock.Select(e => e.ID).ToArray(), originalUnits = quantities, carriedBefore = before,
                carriedAfter = quantities.Keys.ToDictionary(blueprint => blueprint, Packed), recipients = _trapRecoveredStock,
                bound = "The actual native cache popup contained exactly the original rows. Lawful stack merges conserve units and may consume an original source ID; no guaranteed reward blueprint is assumed." });
            tick = Tick; energy = Energy; string gear = Gear(Player);
            yield return WorldAction(cache, "OpenContainer"); yield return CloseNormal();
            Check("trap_depleted_cache_not_refilled", cache.GetPart<ContainerPart>().Contents.Count == 0
                && Tick == tick && Energy == energy && Gear(Player) == gear && TrapRecoveredStockRows() == _trapRecoveredStock);
            yield return Capture("dispatch-06-original-stock-recovered-once");
            // Leave through the cleared service route, then navigate to the
            // distinct direct gate's outside approach in its observed state.
            yield return DistrictWalk(TrapYardCell(-3, 0), 80);
            yield return DistrictWalk(TrapYardCell(-4, 0), 40);
            yield return DistrictWalk(TrapYardCell(4, 0), 180);
        }

        static string TrapCacheRows(Entity cache) => string.Join("|", cache.GetPart<ContainerPart>().Contents
            .OrderBy(e => e.ID, StringComparer.Ordinal).Select(e => e.ID + ":" + e.BlueprintName + ":" + Units(e)
                + ":" + e.GetPart<PhysicsPart>()?.InInventory?.ID));

        string TrapRecoveredStockRows() => string.Join("|", Player.GetPart<InventoryPart>().Objects
            .Where(e => _trapStockBlueprints.Contains(e.BlueprintName) && Owns(Player, e))
            .OrderBy(e => e.ID, StringComparer.Ordinal).Select(e => e.ID + ":" + e.BlueprintName + ":" + Units(e)));

        // The original consumed trap is never replaced. Read retained native
        // action windows, including its actual non-player damage and death,
        // before accepting disappearance as this separately named outcome.
        bool TrapGateBeforeJam()
        {
            var owner = TrapOwner;
            if (owner != null)
                return !_trapConsumedByOther && owner.BlueprintName == "SpikeTrap"
                    && owner.SpatialZone == Zone && Zone.GetEntityCell(owner) == TrapYardCell(3, 0)
                    && owner.GetPart<SpikeTrapTriggerPart>()?.ConsumeOnTrigger == true
                    && owner.GetPart<TrapJammingPart>() != null && !TrapJammingPart.IsJammed(owner);
            if (CountGraphId(_trapOwnerId) != 0) return false;
            if (_trapConsumedByOther) return true;
            var rows = _windows.SelectMany(window => (IEnumerable<Diag.Entry>)window.GetType()
                .GetProperty("rows").GetValue(window)).ToArray();
            var damage = rows.FirstOrDefault(row => row.Category == "damage" && row.Kind == "DamageDealt"
                && row.ActorId == _trapOwnerId && !string.IsNullOrEmpty(row.TargetId) && row.TargetId != Player.ID
                && JObject.Parse(row.PayloadJson)["amount"]?.Value<int>() > 0
                && JObject.Parse(row.PayloadJson)["lethal"]?.Value<bool>() == true);
            if (string.IsNullOrEmpty(damage.TraceId)) return false;
            var death = rows.FirstOrDefault(row => row.Category == "damage" && row.Kind == "DeathHandled"
                && row.ActorId == _trapOwnerId && row.TargetId == damage.TargetId
                && JObject.Parse(row.PayloadJson)["killerIsPlayer"]?.Value<bool>() == false
                && JObject.Parse(row.PayloadJson)["deathX"]?.Value<int>() == TrapYardCell(3, 0).X
                && JObject.Parse(row.PayloadJson)["deathY"]?.Value<int>() == TrapYardCell(3, 0).Y);
            if (string.IsNullOrEmpty(death.TraceId)) return false;
            _trapConsumedByOther = true;
            _observations.Add(new { phase = "dispatch-original-trap-consumed-by-npc", originalTrap = _trapOwnerId,
                victim = damage.TargetId, damage, death, currentCell = new { x = TrapYardCell(3, 0).X, y = TrapYardCell(3, 0).Y },
                remainingTimber = Packed("SalvagedTimber"),
                bound = "Actual native NPC movement sprang this original one-shot trap. This branch claims neither player jamming nor that the trap stayed armed during hauling. No owner was restored or replaced." });
            Check("trap_consumed_by_native_actor", TrapOwner == null && CountGraphId(_trapOwnerId) == 0
                && Packed("SalvagedTimber") == 2 && Units(TrapTimber) == 2);
            return true;
        }

        IEnumerator TrapSpentCross(string check)
        {
            var cell = TrapYardCell(3, 0);
            Require(_trapConsumedByOther && TrapOwner == null && CountGraphId(_trapOwnerId) == 0
                && cell != At && Math.Max(Math.Abs(cell.X - At.X), Math.Abs(cell.Y - At.Y)) == 1
                && cell.IsVisible && Safe(Zone, cell, ThreatClearance) && Zone.CanPlaceFootprint(Player, cell.X, cell.Y),
                "actual adjacent gate emptied by the evidenced original NPC trigger, without a hazard exemption");
            int hp = Player.GetStatValue("Hitpoints");
            yield return StepTo(cell.X, cell.Y);
            Check(check, At == cell && TrapOwner == null && CountGraphId(_trapOwnerId) == 0
                && Player.GetStatValue("Hitpoints") == hp && Packed("SalvagedTimber") == 2 && Units(TrapTimber) == 2);
            yield return Capture(check);
        }

        bool TrapYardAftermath() => TrapYardBeam != null && TrapYardCache != null
            && Zone.GetEntityCell(TrapYardBeam) == TrapYardCell(-5, 0) && !TrapYardBeam.HasPart<DraggedPart>()
            && !DragSystem.IsDragging(Player) && !TrapYardCell(-3, 0).BlocksMovement(Player)
            && TrapYardCache.GetPart<ContainerPart>()?.Contents.Count == 0
            && (_trapConsumedByOther ? TrapOwner == null && CountGraphId(_trapOwnerId) == 0
                : TrapOwner != null && TrapJammingPart.IsJammed(TrapOwner) && CountGraphId(_trapOwnerId) == 1)
            && CountGraphId(_trapBeamId) == 1 && CountGraphId(_trapStockId) == 1
            && CountGraphId(_trapForageId) == 0 && CountGraphId(_trapPalletId) == 0
            && Packed("SalvagedTimber") == TrapExpectedTimber && Units(TrapTimber) == TrapExpectedTimber
            && TrapRecoveredStockRows() == _trapRecoveredStock;

        string TrapYardDigest() => string.Join("|", _trapBeamId, Zone.GetEntityPosition(TrapYardBeam),
            _trapStockId, Zone.GetEntityPosition(TrapYardCache), TrapCacheRows(TrapYardCache),
            _trapOwnerId, TrapOwner == null ? "absent" : Zone.GetEntityPosition(TrapOwner).ToString(),
            _trapConsumedByOther ? "npc-consumed" : "player-jammed",
            CountGraphId(_trapForageId), CountGraphId(_trapPalletId), TrapRecoveredStockRows(), Packed("SalvagedTimber"));

        IEnumerator TrapDispatchRevisit()
        {
            var yard = Zone; var beam = TrapYardBeam; var cache = TrapYardCache; var trap = TrapOwner;
            string digest = TrapYardDigest(); int steps = _mapSteps;
            Require(TrapYardAftermath(), "same acquired stock and both physical entrance outcomes before actual departure");
            yield return DistrictWalk(TrapYardCell(4, 0), 60);
            yield return TravelSurface(GleanersDistrict.SurfaceID);
            Require(Zone.ZoneID == GleanersDistrict.SurfaceID && ReferenceEquals(Manager.CachedZones[TrapJammingZone], yard),
                "actual ordinary map departure retains the generated yard graph");
            yield return TravelSurface(TrapJammingZone);
            Check("trap_actual_revisit", _mapSteps == steps + 2 && ReferenceEquals(Zone, yard)
                && ReferenceEquals(TrapYardBeam, beam) && ReferenceEquals(TrapYardCache, cache) && ReferenceEquals(TrapOwner, trap)
                && TrapYardAftermath() && TrapYardDigest() == digest && !TrapWicket.GetPart<RepairablePart>().Repaired);
            yield return Capture("dispatch-07-actual-map-return-preserves-aftermath");
        }
    }
}
