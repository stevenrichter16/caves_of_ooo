using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _claimedSupplies;
        string _claimActorId, _claimPostId;
        int _claimActionStart;
        const string ClaimedSuppliesIntent = "Seed1 ordinary duelist bootstrap, then a disclosed original-player-only transfer to the unchanged actual generated cache at17.12. Native keys walk its outside bypass, inspect the real guard's warning, enter and withdraw, then take the original finite two-item stock through the real pickup UI with one paid action. Real F5/unsaved-step/F6 preserves the empty cache, exact original loot and current guard state.";
        const string ClaimedSuppliesLimits = "Generated-source acceptance with an explicit player-only transfer, not an all-ordinary walking journey, blind discovery or an authored new encounter. One fixed seed/address, at most64 local actions. No source, HP, inventory, AI, hostility, grace, clock, RNG or FOV grants. Current user-selected full reveal remains unchanged, so this does not prove normal-fog discovery. Native danger can honestly stop the route. Screenshots need review; no balance or universal-frequency claim.";
        static readonly string[] ClaimedSuppliesChecks = { "ordinary_start", "claimed_source_intact", "claimed_native_readout", "claimed_bypass", "claimed_warning", "claimed_withdrawal", "claimed_paid_stock", "claimed_saved_depletion", "claimed_finish" };
        static readonly (int x, int y)[] ClaimCardinals = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        Entity ClaimActor => Owner(_claimActorId);
        Entity ClaimPost => Owner(_claimPostId);
        SpreadTerritoryPart ClaimRole => ClaimActor?.GetPart<SpreadTerritoryPart>();

        public void InitializeClaimedSupplies(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Isolated launcher required before claimed-cache bootstrap.");
            _claimedSupplies = true; Initialize(context, connectedBuild: "duelist");
        }
        static bool ClaimInside(SpreadTerritoryPart role, int x, int y)
            => x >= role.Left && x <= role.Right && y >= role.Top && y <= role.Bottom;
        bool ClaimSafe(Zone zone, Cell cell, Entity guard)
            => cell != null && Safe(zone, cell, 1, Threats(zone).Where(e => e != guard).ToArray())
                && zone.CanPlaceFootprint(Player, cell.X, cell.Y);
        // Pure bounded route observation. This permits danger from this exact
        // native guard; no production collision, AI or threat state is changed.
        List<(int x, int y)> ClaimPath(Zone zone, (int x, int y) start, Func<Cell, bool> goal,
            Entity guard, Func<Cell, bool> admission)
        {
            var seen = new bool[Zone.Width, Zone.Height]; var parent = new (int x, int y)[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>(); queue.Enqueue(start); seen[start.x, start.y] = true;
            var threats = Threats(zone).Where(e => e != guard).ToArray();
            bool Admitted(Cell cell) => cell != null && admission(cell) && Safe(zone, cell, 1, threats)
                && zone.CanPlaceFootprint(Player, cell.X, cell.Y);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue(); if (goal(zone.GetCell(at.x, at.y)))
                {
                    var path = new List<(int x, int y)>();
                    while (at != start) { path.Add(at); at = parent[at.x, at.y]; }
                    path.Reverse(); return path;
                }
                foreach (var d in ClaimCardinals)
                {
                    int x = at.x + d.x, y = at.y + d.y;
                    if (!zone.InBounds(x, y) || seen[x, y] || !Admitted(zone.GetCell(x, y))) continue;
                    seen[x, y] = true; parent[x, y] = at; queue.Enqueue((x, y));
                }
            }
            return null;
        }
        IEnumerator ClaimStep(int x, int y, string label)
        {
            Require(_localInputs - _claimActionStart < 64 && Player.GetStatValue("Hitpoints") > 0
                && Math.Abs(x - At.X) + Math.Abs(y - At.Y) == 1 && ClaimSafe(Zone, Zone.GetCell(x, y), ClaimActor),
                "bounded real claimed-cache step remains physically possible; no survival grants");
            var zone = Zone;
            yield return Paid(Tap(Direction(x - At.X, y - At.Y)), "local", label);
            Require(Zone == zone && At.X == x && At.Y == y, "native claimed-cache movement reaches its actual cell");
        }
        string ClaimStock() => string.Join("|", ClaimPost.GetPart<ContainerPart>().Contents.OrderBy(e => e.ID, StringComparer.Ordinal)
            .Select(e => e.ID + ":" + e.BlueprintName + ":" + Units(e) + ":" + e.GetPart<PhysicsPart>()?.InInventory?.ID));
        string ClaimState()
        {
            var r = ClaimRole; var b = ClaimActor.GetPart<BrainPart>(); var p = Zone.GetEntityPosition(ClaimActor);
            return ClaimActor.ID + ":" + p + ":hp=" + ClaimActor.GetStatValue("Hitpoints") + ":post=" + r.Post?.ID
                + ":home=" + r.HomeX + "," + r.HomeY + ":bounds=" + r.Left + "," + r.Top + "," + r.Right + "," + r.Bottom
                + ":warning=" + r.WarningTarget?.ID + ":grace=" + r.GraceRemaining + ":returns=" + r.ReturnAttempts
                + ":target=" + b.Target?.ID + ":personal=" + b.IsPersonallyHostileTo(Player) + ":stock=" + ClaimStock();
        }
        IEnumerator ClaimTakeAll(string[] ids)
        {
            yield return WorldAction(ClaimPost, "OpenContainer");
            Require(State == "PickupOpen" && ReferenceEquals(Field(_input.PickupUI, "_sourceContainer"), ClaimPost),
                "native pickup popup belongs to the exact stationary cache");
            var items = (List<Entity>)Field(_input.PickupUI, "_items");
            Require(items.Count == 2 && items.Select(e => e.ID).OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(ids.OrderBy(id => id, StringComparer.Ordinal)), "actual two original loot rows");
            yield return Capture("claim-06-real-two-item-pickup");
            yield return Tap(Key.Tab); yield return CloseNormal();
        }
        IEnumerator ClaimedSuppliesJourney()
        {
            _claimActionStart = _localInputs;
            Require(Manager.WorldSeed == 1 && Manager.Exploration.Version == 16, "fixed fresh16 seed1 source witness");
            var destination = Manager.GetZone("Overworld.17.12.0");
            var guard = destination.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "MarlbackScrabbler"
                && e.GetPart<SpreadTerritoryPart>()?.Post?.BlueprintName == "Sack");
            Require(guard != null, "actual complete generation commits the predeclared stationary sack claim");
            var role = guard.GetPart<SpreadTerritoryPart>(); var cache = role.Post;
            var stock = cache.GetPart<ContainerPart>().Contents.ToArray();
            Require(destination.GetEntityPosition(cache) == (74, 19) && stock.Length == 2
                && stock.SingleOrDefault(e => e.BlueprintName == "BogSap") is Entity sap && Units(sap) == 1
                && stock.SingleOrDefault(e => e.BlueprintName == "GoldCoin") is Entity coins && Units(coins) == 3,
                "literal fixed-source stock matches the native source probe, without restocking");
            Require(stock.All(e => !Player.GetPart<InventoryPart>().Objects.Any(p => p.BlueprintName == e.BlueprintName)),
                "original loot can retain literal IDs without preexisting merge recipients");
            _claimActorId = guard.ID; _claimPostId = cache.ID;
            (int x, int y) entry = default, outside = default, arrival = default; bool found = false;
            var center = destination.GetEntityPosition(cache); var g = destination.GetEntityCell(guard);
            // Four finite center-line approaches, no seed/source reroll. Each
            // admission proves an actual outside bypass and cardinal loot path.
            foreach (var d in ClaimCardinals)
            {
                var a = (x: d.x > 0 ? role.Right : d.x < 0 ? role.Left : center.x,
                    y: d.y > 0 ? role.Bottom : d.y < 0 ? role.Top : center.y);
                var o = (x: a.x + d.x, y: a.y + d.y);
                var opposite = (x: d.x > 0 ? role.Left - 1 : d.x < 0 ? role.Right + 1 : center.x,
                    y: d.y > 0 ? role.Top - 1 : d.y < 0 ? role.Bottom + 1 : center.y);
                if (!ClaimSafe(destination, destination.GetCell(a.x, a.y), guard)
                    || !Safe(destination, destination.GetCell(o.x, o.y), 1)
                    || !Safe(destination, destination.GetCell(opposite.x, opposite.y), 1)
                    || !AIHelpers.HasLineOfSight(destination, g.X, g.Y, a.x, a.y)
                    || !AIHelpers.HasLineOfSight(destination, o.x, o.y, g.X, g.Y)
                    || AIHelpers.ChebyshevDistance(g.X, g.Y, a.x, a.y) > guard.GetPart<BrainPart>().SightRadius) continue;
                var bypass = ClaimPath(destination, opposite, c => c.X == o.x && c.Y == o.y, guard,
                    c => !ClaimInside(role, c.X, c.Y));
                var loot = ClaimPath(destination, a, c => Math.Abs(c.X - center.x) + Math.Abs(c.Y - center.y) == 1,
                    guard, c => ClaimInside(role, c.X, c.Y));
                if (bypass == null || bypass.Count < 4 || bypass.Count > 28 || loot == null || loot.Count > 6) continue;
                entry = a; outside = o; arrival = opposite; found = true; break;
            }
            Require(found, "one of four real approaches has a visible warning, reachable stock and short outside bypass");
            yield return Transfer(destination, destination.GetCell(arrival.x, arrival.y), "unchanged generated seed1 claimed supplies");
            int hp = Player.GetStatValue("Hitpoints"), drams = TradeSystem.GetDrams(Player);
            string originalStock = ClaimStock(); string[] ids = stock.Select(e => e.ID).ToArray();
            Check("claimed_source_intact", ClaimActor == guard && ClaimPost == cache && role.Post == cache
                && role.GraceTurns == 2 && stock.All(e => cache.GetPart<ContainerPart>().Contents.Contains(e)
                    && e.GetPart<PhysicsPart>().InInventory == cache) && ClaimActor.GetStatValue("Hitpoints") == 15
                && Manager.Exploration.TryGetPlacement(Manager, Zone.ZoneID, out var accepted)
                && accepted.Family == SpreadExplorationFamily.OccupiedBank
                && Manager.Exploration.DispositionFor(Zone.ZoneID) == 2
                && ReferenceEquals(Manager.ActiveZone, Zone)
                && Manager.CachedZones.TryGetValue(Zone.ZoneID, out var cached) && ReferenceEquals(cached, Zone));
            _observations.Add(new { phase = "claimed-generated-source", source = Zone.ZoneID, seed = Manager.WorldSeed,
                guard = guard.ID, cache = cache.ID, stock = originalStock, entry, outside, arrival,
                bounds = new[] { role.Left, role.Top, role.Right, role.Bottom }, transfer = ClaimedSuppliesLimits });
            yield return Capture("claim-01-unchanged-generated-source");

            for (int n = 0; At.X != outside.x || At.Y != outside.y; n++)
            {
                Require(n < 28, "bounded native outside bypass");
                var path = ClaimPath(Zone, (At.X, At.Y), c => c.X == outside.x && c.Y == outside.y, ClaimActor,
                    c => !ClaimInside(ClaimRole, c.X, c.Y));
                Require(path != null && path.Count > 0, "current native bypass stays open");
                yield return ClaimStep(path[0].x, path[0].y, "claimed-native-bypass");
                Require(!ClaimInside(ClaimRole, At.X, At.Y) && ClaimRole.WarningTarget != Player
                    && ClaimActor.GetPart<BrainPart>().Target != Player && Player.GetStatValue("Hitpoints") == hp,
                    "each bypass step stays outside the actual claim without player-directed enforcement");
            }
            Check("claimed_bypass", ClaimStock() == originalStock && !ClaimActor.GetPart<BrainPart>().IsPersonallyHostileTo(Player));
            yield return Capture("claim-02-real-outside-bypass");
            yield return WaterExamine(ClaimActor, "claim-03-real-guard-readout");
            Check("claimed_native_readout", NormalizeText(_readerText).Contains("guards the ground around that sack")
                && NormalizeText(_readerText).Contains("withdraw to avoid a fight") && ClaimStock() == originalStock);

            long beforeWarning = MessageLog.NextSerialValue;
            yield return ClaimStep(entry.x, entry.y, "claimed-native-warning-entry");
            Check("claimed_warning", ClaimRole.WarningTarget == Player && ClaimRole.GraceRemaining == 2
                && ClaimActor.GetPart<BrainPart>().Target != Player && Player.GetStatValue("Hitpoints") == hp
                && MessageLog.NextSerialValue > beforeWarning && MessageLog.GetRecentEntries(24).Any(m => m.Serial >= beforeWarning && m.Text.Contains("warns you to leave the marked ground")));
            yield return Capture("claim-04-native-warning");
            yield return ClaimStep(outside.x, outside.y, "claimed-native-immediate-withdrawal");
            Check("claimed_withdrawal", ClaimRole.WarningTarget != Player && ClaimActor.GetPart<BrainPart>().Target != Player
                && !ClaimActor.GetPart<BrainPart>().IsPersonallyHostileTo(Player) && Player.GetStatValue("Hitpoints") == hp
                && ClaimStock() == originalStock);
            yield return Capture("claim-05-native-withdrawal");

            yield return ClaimStep(entry.x, entry.y, "claimed-native-return-to-stock");
            for (int n = 0; Math.Abs(At.X - center.x) + Math.Abs(At.Y - center.y) != 1; n++)
            {
                Require(n < 8 && Player.GetStatValue("Hitpoints") > 0, "bounded live stock approach, without health grants");
                var path = ClaimPath(Zone, (At.X, At.Y), c => Math.Abs(c.X - center.x) + Math.Abs(c.Y - center.y) == 1,
                    ClaimActor, c => ClaimInside(ClaimRole, c.X, c.Y));
                Require(path != null && path.Count > 0, "real guard leaves an actual unoccupied loot contact");
                yield return ClaimStep(path[0].x, path[0].y, "claimed-native-stock-approach");
            }
            Require(ClaimStock() == originalStock, "stock remained untouched until actual pickup");
            string marker = Mark("claimed-before-real-take-all");
            Require(_localInputs - _claimActionStart < 64, "bounded real pickup opportunity");
            yield return Paid(ClaimTakeAll(ids), "local", "claimed-native-two-item-take-all");
            Check("claimed_paid_stock", ClaimPost.GetPart<ContainerPart>().Contents.Count == 0
                && stock.All(e => Owns(Player, e) && CountGraphId(e.ID) == 1)
                && Units(stock.Single(e => e.BlueprintName == "BogSap")) == 1
                && Units(stock.Single(e => e.BlueprintName == "GoldCoin")) == 3
                && TradeSystem.GetDrams(Player) == drams && _lastClock.CompletedTurns == 1);
            _observations.Add(new { phase = "claimed-paid-stock", originalStock, actual = Gear(Player), state = ClaimState(), rows = Window(marker) });
            yield return Capture("claim-07-actual-paid-stock");

            var oldPlayer = Player; var oldZone = Zone; var oldGuard = ClaimActor; var oldCache = ClaimPost;
            string state = ClaimState(), gear = Gear(Player), stats = Stats(Player), notes = NoteSignature();
            int x = At.X, y = At.Y, tick = Tick, energy = Energy, world = WorldClock.CurrentTick;
            yield return Tap(Key.F5); yield return Settled(); string file = SaveFile(); _checkpointHash = HashFile(file);
            Require(MessageLog.GetLast() == "Game saved." && !string.IsNullOrEmpty(_checkpointHash), "real depleted checkpoint saved");
            var next = ClaimCardinals.Select(d => Zone.GetCell(x + d.x, y + d.y)).Where(c => ClaimSafe(Zone, c, ClaimActor))
                .OrderByDescending(c => SpatialQuery.DistanceToCell(Zone, ClaimActor, c.X, c.Y)).FirstOrDefault();
            Require(next != null, "real unsaved movement exists; no forced guard displacement");
            yield return ClaimStep(next.X, next.Y, "claimed-native-unsaved-step");
            Require(Tick > tick && HashFile(file) == _checkpointHash, "paid unsaved step leaves checkpoint bytes unchanged");
            yield return Reload(oldPlayer);
            Check("claimed_saved_depletion", Player != oldPlayer && Zone != oldZone && ClaimActor != oldGuard && ClaimPost != oldCache
                && At.X == x && At.Y == y && Tick == tick && Energy == energy && WorldClock.CurrentTick == world
                && Stats(Player) == stats && Gear(Player) == gear && NoteSignature() == notes
                && TradeSystem.GetDrams(Player) == drams && ClaimState() == state
                && ClaimPost.GetPart<ContainerPart>().Contents.Count == 0
                && ids.All(id => Owns(Player, Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e => e.ID == id)) && CountGraphId(id) == 1)
                && ReferenceEquals(Manager.GetZone("Overworld.17.12.0"), Zone) && HashFile(file) == _checkpointHash);
            yield return Capture("claim-08-restored-empty-actual-cache");
            Check("claimed_finish", State == "Normal" && _localInputs - _claimActionStart <= 64
                && Player.GetStatValue("Hitpoints") > 0 && !DevMode.Enabled && !DebugInvincibility.IsEnabled(Player)
                && !Player.HasPart<BitLockerPart>());
        }
    }
}
