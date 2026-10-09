using System;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>Only an unclaimed absence may fall back to ordinary territory.
    /// A failed begun transaction rejects its cold graph.</summary>
    public enum ClaimedSuppliesOutcome { Unavailable, Committed, Rejected }

    public static partial class SpreadExplorationActorPlacement
    {
        /// <summary>Use a current ordinary cache as the post without moving or
        /// restocking it. The caller retains exact manager/producer authority.
        /// Outer acceptance belongs inside this one actor's rollback boundary.</summary>
        public static ClaimedSuppliesOutcome TryClaimedSupplies(Zone zone, Entity actor,
            SpreadGenerationReceipt population, SpreadGenerationReceipt stock,
            Func<bool> authority, Func<Entity[], Func<bool>, bool> commit)
        {
            if (zone == null || authority == null || commit == null
                || population?.Zone != zone || stock?.Zone != zone
                || population.Factory != stock.Factory || population.Factory == null
                || population.Owners.Count != 1 || population.Owners[0] != actor
                || !population.IsCurrent || !stock.IsCurrent) return ClaimedSuppliesOutcome.Unavailable;
            var owner = ActorSnapshot.Capture(zone, actor, "MarlbackScrabbler");
            if (owner == null || actor.HasPart<SpreadTerritoryPart>() || actor.HasPart<SpreadGrazerPart>())
                return ClaimedSuppliesOutcome.Unavailable;
            // Do not adopt an observer's changes as our new starting state.
            if (!authority() || !population.IsCurrent || !stock.IsCurrent || !owner.Current(owner.Origin))
                return ClaimedSuppliesOutcome.Rejected;
            var stockUnchanged = SpreadGenerationReceipt.CaptureFinalState(zone, stock.Owners);
            var geometry = new Geometry(zone, actor);
            int sources = 0, trials = 0;
            foreach (var cache in stock.Owners.OrderBy(e => zone.GetEntityPosition(e).y)
                .ThenBy(e => zone.GetEntityPosition(e).x))
            {
                if (!ClaimedCache(zone, cache)) continue;
                var center = zone.GetEntityPosition(cache);
                if (Distance(owner.Origin, center) > 12 || !ClaimAvoidsArrivals(zone, center)) continue;
                if (++sources > MaxSources) break;
                int left = center.x - 3, right = center.x + 3, top = center.y - 2, bottom = center.y + 2;
                bool Avoid(int x, int y) => x >= left && x <= right && y >= top && y <= bottom;
                for (int y = center.y - 1; y <= center.y + 1; y++)
                    for (int x = center.x - 1; x <= center.x + 1; x++)
                    {
                        if (++trials > MaxTrials) return ClaimedSuppliesOutcome.Unavailable;
                        var destination = (x, y);
                        if (!geometry.Place(x, y) || !geometry.PreservesRoutes(destination)
                            || !geometry.HasBypass(destination, Avoid)
                            || !ObservedCacheContact(zone, geometry, center, destination)) continue;
                        if (!authority() || !population.IsCurrent || !stock.IsCurrent
                            || !owner.Current(owner.Origin) || !stockUnchanged()
                            || actor.HasPart<SpreadTerritoryPart>() || actor.HasPart<SpreadGrazerPart>())
                            return ClaimedSuppliesOutcome.Rejected;
                        var routes = geometry.CaptureRoutes(destination);
                        SpreadTerritoryPart role = null; FieldState roleState = null; bool committed = false;
                        bool CurrentPacket()
                        {
                            if (!authority() || !owner.Current(destination, role)
                                || !stock.MatchesOwnedState() || !stockUnchanged()
                                || !ClaimedCache(zone, cache) || zone.GetEntityPosition(cache) != center
                                || !ClaimAvoidsArrivals(zone, center)
                                || role == null || actor.GetPart<SpreadTerritoryPart>() != role
                                || role.ParentEntity != actor || roleState?.Matches() != true) return false;
                            var current = new Geometry(zone, actor);
                            return current.Place(destination.x, destination.y) && routes(current)
                                && current.HasBypass(destination, Avoid)
                                && ObservedCacheContact(zone, current, center, destination);
                        }
                        try
                        {
                            if (!population.TryConsume() || !stock.TryConsume()) return ClaimedSuppliesOutcome.Rejected;
                            // Authority may include callbacks; prove geometry again after it.
                            if (!authority() || !owner.Current(owner.Origin) || !stock.MatchesOwnedState()
                                || !stockUnchanged()) return ClaimedSuppliesOutcome.Rejected;
                            var current = new Geometry(zone, actor);
                            if (!ClaimAvoidsArrivals(zone, center) || !current.Place(x, y) || !routes(current)
                                || !current.HasBypass(destination, Avoid)
                                || !ObservedCacheContact(zone, current, center, destination)) return ClaimedSuppliesOutcome.Rejected;
                            if (owner.Origin != destination && !zone.MoveEntity(actor, x, y)) return ClaimedSuppliesOutcome.Rejected;
                            if (!authority() || !owner.Current(destination) || !stock.MatchesOwnedState()
                                || !stockUnchanged()) return ClaimedSuppliesOutcome.Rejected;
                            role = new SpreadTerritoryPart(); actor.AddPart(role);
                            if (!owner.Current(destination, role) || actor.GetPart<SpreadTerritoryPart>() != role
                                || !role.Configure(zone, cache, left, top, right, bottom, 2)) return ClaimedSuppliesOutcome.Rejected;
                            roleState = new FieldState(role);
                            if (!CurrentPacket()) return ClaimedSuppliesOutcome.Rejected;
                            committed = commit(new[] { actor, cache }, CurrentPacket) && CurrentPacket();
                            return committed ? ClaimedSuppliesOutcome.Committed : ClaimedSuppliesOutcome.Rejected;
                        }
                        finally
                        {
                            if (!committed)
                            {
                                if (role != null && role.ParentEntity == actor && actor.Parts.Contains(role)) actor.RemovePart(role);
                                owner.Rollback(destination);
                            }
                        }
                    }
            }
            return ClaimedSuppliesOutcome.Unavailable;
        }

        static bool ClaimedCache(Zone zone, Entity cache)
        {
            if (!SpreadActorContext.Ground(cache, zone) || (cache.BlueprintName != "Crate" && cache.BlueprintName != "Sack")
                || cache.GetPart<PhysicsPart>().Takeable || cache.HasTag("Creature") || cache.HasPart<SpatialFootprintPart>()
                || cache.HasPart<DoorPart>() || cache.HasTag("Owned") || cache.HasTag("QuestItem") || cache.HasTag("Unique")
                || cache.GetPart<RenderPart>()?.ParentEntity != cache) return false;
            var box = cache.GetPart<ContainerPart>(); var cell = zone.GetEntityCell(cache);
            return box?.ParentEntity == cache && !box.IsLocked && box.Contents.Count > 0
                && box.Contents.All(e => e != null && (e.GetPart<StackerPart>()?.StackCount ?? 1) > 0)
                && !cell.IsInterior && !zone.GenReservedCells.Contains((cell.X, cell.Y));
        }
        static bool ClaimAvoidsArrivals(Zone zone, (int x, int y) center)
        {
            int left = center.x - 3, right = center.x + 3, top = center.y - 2, bottom = center.y + 2;
            if (left < 2 || top < 2 || right >= Zone.Width - 2 || bottom >= Zone.Height - 2) return false;
            bool Inside(int x, int y) => x >= left && x <= right && y >= top && y <= bottom;
            if (Inside(Zone.Width / 2, Zone.Height / 2) || zone.GenReservedCells.Any(p => Inside(p.x, p.y))) return false;
            return !zone.GetReadOnlyEntities().Where(e => e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>())
                .Any(e => { var p = zone.GetEntityPosition(e); return p.x >= left - 1 && p.x <= right + 1 && p.y >= top - 1 && p.y <= bottom + 1; });
        }
        static bool ObservedCacheContact(Zone zone, Geometry geometry, (int x, int y) center, (int x, int y) guard)
        {
            for (int d = 0; d < 4; d++)
            {
                var contact = (x: center.x + (d == 0 ? 1 : d == 1 ? -1 : 0),
                    y: center.y + (d == 2 ? 1 : d == 3 ? -1 : 0));
                if (contact != guard && geometry.Place(contact.x, contact.y)
                    && geometry.EntryReaches(contact, guard)
                    && AIHelpers.HasLineOfSight(zone, guard.x, guard.y, contact.x, contact.y)) return true;
            }
            return false;
        }
    }
}
