using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Cold population preflight/commit for optional original pair or warned viper.
    /// Refusal leaves the ordinary group intact. Never runs on revisit or render.</summary>
    public sealed class SpreadRareEncounterBuilder
    {
        public const string SourceKey = "SpreadRare.Source";
        private readonly OverworldZoneManager manager;
        public SpreadRareEncounterBuilder(OverworldZoneManager owner) { manager = owner; }

        public bool TryPlace(Zone zone, EntityFactory factory)
        {
            if (zone == null || factory == null || manager?.RareEncounters?.Selects(manager, zone.ZoneID) != true) return false;
            if (factory != manager.Factory) return Refuse(zone,"foreign-factory");
            if (manager.RareEncounters.SelectsViper(manager, zone.ZoneID)) return TryPlaceViper(zone, factory);
            string[] required = { SpreadRareEncounterPlan.PairLeader, SpreadRareEncounterPlan.PairMate, "ShortSword", "Cudgel", "LeatherCap" };
            if (factory.Blueprints == null || required.Any(id => !factory.Blueprints.ContainsKey(id)) || LoadoutPart.Factory != factory)
                return Refuse(zone, "missing-content-or-loadout-owner");
            if (zone.GetReadOnlyEntities().Any(e => e.Properties.ContainsKey(SourceKey))) return Refuse(zone, "already-owned-source");

            // Candidate geometry is checked before factory callbacks and again
            // immediately before each placement. Keep a visible side approach.
            var reachable = ReachableFromEdges(zone);
            int px = -1, py = -1;
            for (int y = 5; y < Zone.Height - 5 && px < 0; y++)
                for (int x = 5; x < Zone.Width - 7; x++)
                    if (reachable[x,y] && Pocket(zone, x, y)) { px = x; py = y; break; }
            if (px < 0) return Refuse(zone, "no-unreserved-hedge-pocket");
            var a = factory.CreateEntity(required[0]);
            var b = factory.CreateEntity(required[1]);
            if (!Fresh(a, required[0], "ShortSword", "LeatherCap") || !Fresh(b, required[1], "Cudgel", null)
                || ReferenceEquals(a, b) || a.ID == b.ID || !CurrentPairSource(zone, factory) || !Pocket(zone, px, py)
                || !ReachableFromEdges(zone)[px,py]) return Refuse(zone, "staging-or-callback-refused");
            if (!LegalBody(zone, a, px, py) || !LegalBody(zone, b, px + 2, py)) return Refuse(zone, "invalid-body");
            if (!zone.AddEntity(a, px, py)) return Refuse(zone, "leader-placement-refused");
            if (!CurrentPairSource(zone, factory) || !Fresh(b, required[1], "Cudgel", null)
                || !LegalBody(zone, b, px + 2, py) || !zone.AddEntity(b, px + 2, py))
            { if (a.SpatialZone == zone) zone.RemoveEntity(a); return Refuse(zone, "mate-placement-refused"); }
            if (!CurrentPairSource(zone, factory) || a.SpatialZone != zone || b.SpatialZone != zone || zone.GetEntityPosition(a) != (px,py)
                || zone.GetEntityPosition(b) != (px+2,py) || !OwnsKit(a, "ShortSword", "LeatherCap") || !OwnsKit(b, "Cudgel", null))
            { if (a.SpatialZone == zone) zone.RemoveEntity(a); if (b.SpatialZone == zone) zone.RemoveEntity(b); return Refuse(zone, "commit-owner-changed"); }
            a.Properties[SourceKey] = b.Properties[SourceKey] = zone.ZoneID;
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "SpreadRareCommitted", a, payload: new
            { zone = zone.ZoneID, family = "ditch-cutters", leader = a.ID, mate = b.ID,
                items = a.GetPart<InventoryPart>().EquippedItems.Values.Concat(b.GetPart<InventoryPart>().EquippedItems.Values).Distinct().Select(e => e.ID).ToArray() });
            return true;
        }

        // Creation may change the live map or frozen selection. Recheck this
        // pair's authority without admitting the separate optional viper source.
        private bool CurrentPairSource(Zone zone, EntityFactory factory)
            => factory == manager.Factory && manager.RareEncounters?.Initialized == true
                && manager.RareEncounters.PairZoneID == zone.ZoneID
                && SpreadRareEncounterPlan.IsEligible(manager, zone.ZoneID)
                && !zone.GetReadOnlyEntities().Any(e => e.Properties.ContainsKey(SourceKey));

        private bool TryPlaceViper(Zone zone, EntityFactory factory)
        {
            if (factory.Blueprints == null || !factory.Blueprints.ContainsKey(SpreadRareEncounterPlan.ViperBlueprint)
                || !factory.Blueprints.ContainsKey("Signpost")) return ViperRefuse(zone, "missing-latchcoil-content");
            if (zone.GetReadOnlyEntities().Any(e => e.Properties.ContainsKey(SourceKey))) return ViperRefuse(zone, "already-owned-source");
            int px = -1, py = -1;
            for (int y = 5; y < Zone.Height - 5 && px < 0; y++)
                for (int x = 8; x < Zone.Width - 7; x++)
                    if (ViperPocket(zone, x, y)) { px = x; py = y; break; }
            if (px < 0) return ViperRefuse(zone, "no-warned-dry-bypass");
            var snake = factory.CreateEntity(SpreadRareEncounterPlan.ViperBlueprint);
            var sign = factory.CreateEntity("Signpost");
            if (!FreshViper(snake) || !FreshSign(sign) || ReferenceEquals(snake, sign) || snake.ID == sign.ID
                || !CurrentViperSource(zone, factory) || !ViperPocket(zone, px, py)) return ViperRefuse(zone, "latchcoil-staging-refused");
            sign.GetPart<ExaminablePart>().Description = "Old cuts warn of chalk-ring vipers in these hedges and poisonous bites. A scratched arrow points south, around the hedge corner.";
            if (!zone.AddEntity(snake, px, py)) return ViperRefuse(zone, "latchcoil-placement-refused");
            if (!CurrentViperSource(zone, factory) || !FreshSign(sign) || !ViperPocket(zone, px, py, snake)
                || !zone.AddEntity(sign, px - 5, py))
            { RemoveOwned(zone, snake, sign); return ViperRefuse(zone, "warning-placement-refused"); }
            if (!CurrentViperSource(zone, factory) || zone.GetEntityPosition(snake) != (px, py)
                || zone.GetEntityPosition(sign) != (px - 5, py) || !ViperContract(snake) || !SignContract(sign)
                || !ViperPocket(zone, px, py, snake, sign))
            { RemoveOwned(zone, snake, sign); return ViperRefuse(zone, "latchcoil-commit-owner-changed"); }
            snake.Properties[SourceKey] = sign.Properties[SourceKey] = zone.ZoneID;
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "SpreadRareCommitted", snake,
                payload: new { zone = zone.ZoneID, family = "chalk-ring-viper", warning = sign.ID });
            return true;
        }
        private bool CurrentViperSource(Zone zone, EntityFactory factory)
            => factory == manager.Factory && manager.RareEncounters?.SelectsViper(manager, zone.ZoneID) == true
                && !zone.GetReadOnlyEntities().Any(e => e.Properties.ContainsKey(SourceKey));
        private static void RemoveOwned(Zone zone, Entity a, Entity b)
        { if (a?.SpatialZone == zone) zone.RemoveEntity(a); if (b?.SpatialZone == zone) zone.RemoveEntity(b); }
        private static bool Uncarried(Entity e)
            => e != null && !string.IsNullOrEmpty(e.ID) && e.GetPart<PhysicsPart>()?.ParentEntity == e
                && e.GetPart<PhysicsPart>().InInventory == null && e.GetPart<PhysicsPart>().Equipped == null;
        private static bool FreshViper(Entity e) => e?.SpatialZone == null && ViperContract(e);
        private static bool ViperContract(Entity e)
            => Uncarried(e) && !e.HasPart<SpatialFootprintPart>() && e.BlueprintName == SpreadRareEncounterPlan.ViperBlueprint && e.GetStatValue("Hitpoints") == 8
                && !CombatSystem.IsDeathHandled(e) && e.GetPart<BrainPart>()?.ParentEntity == e
                && e.GetPart<BrainPart>().SightRadius == 3 && e.GetPart<AIAmbushPart>()?.ParentEntity == e
                && e.GetPart<AIAmbushPart>().WakeOnDamage && e.GetPart<AIAmbushPart>().WakeOnHostileInSight
                && e.GetPart<AIAmbushPart>().DormantPushed && e.GetPart<BrainPart>().PeekGoal() is DormantGoal dormant && !dormant.Finished()
                && e.GetPart<RenderPart>()?.ParentEntity == e && e.GetPart<RenderPart>().Visible
                && e.GetPart<ExaminablePart>()?.ParentEntity == e && e.GetProperty("NaturalWeapon") == "ViperBite";
        private static bool FreshSign(Entity e) => e?.SpatialZone == null && SignContract(e);
        private static bool SignContract(Entity e)
            => Uncarried(e) && !e.HasPart<SpatialFootprintPart>() && e.BlueprintName == "Signpost" && e.GetPart<PhysicsPart>().Solid
                && !e.HasTag("Creature") && !e.HasPart<BrainPart>() && !e.GetPart<PhysicsPart>().Takeable
                && e.GetPart<RenderPart>()?.ParentEntity == e && e.GetPart<RenderPart>().Visible
                && e.GetPart<RenderPart>().RenderString == "I" && e.GetPart<ExaminablePart>()?.ParentEntity == e
                && e.GetPart<RegionalSignpostPart>()?.ParentEntity == e;
        private static bool Dry(Zone zone, Cell cell)
        {
            if (cell == null) return false;
            var state = zone.TileState.Get(cell.X, cell.Y);
            return (state == null || state.IsEmpty) && !cell.Occupants.Any(e =>
                e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<TriggerOnStepPart>());
        }
        private static bool ViperClear(Zone zone, int x, int y, Entity allowed = null)
        {
            var cell = zone.GetCell(x, y);
            return Dry(zone, cell) && !cell.BlocksMovement(allowed) && !zone.GenReservedCells.Contains((x, y))
                && !cell.Occupants.Any(e => e.HasTag("Creature") && !ReferenceEquals(e, allowed));
        }
        private static bool DryWalkable(Zone zone, int x, int y)
        { var cell = zone.GetCell(x, y); return Dry(zone, cell) && !cell.BlocksMovement(); }
        private static bool ViperPocket(Zone zone, int x, int y, Entity snake = null, Entity sign = null)
        {
            if (!ViperClear(zone, x, y, snake) || !ViperClear(zone, x - 5, y, sign)) return false;
            if (snake != null && (snake.SpatialZone != zone || zone.GetOccupiedCells(snake, x, y).Count != 1)) return false;
            if (sign != null && (sign.SpatialZone != zone || zone.GetOccupiedCells(sign, x - 5, y).Count != 1)) return false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dy != 0) && !ViperClear(zone, x + dx, y + dy)) return false;
            bool hedge = false;
            for (int dy = -3; dy <= 3; dy++) for (int dx = -3; dx <= 3; dx++)
                if (zone.GetCell(x + dx, y + dy)?.Occupants.Any(e => e.BlueprintName == "Hedge") == true) hedge = true;
            if (!hedge) return false;
            // South bypass stays outside the fresh actor's Chebyshev sight3.
            // Reservations can be walked, but neither owner is placed on one.
            for (int yy = y; yy <= y + 4; yy++)
                if (!DryWalkable(zone, x - 6, yy) || !DryWalkable(zone, x + 5, yy)) return false;
            for (int xx = x - 6; xx <= x + 5; xx++) if (!DryWalkable(zone, xx, y + 4)) return false;
            return ViperApproachReachesEdge(zone, x - 6, y, x, y);
        }
        private static bool ViperApproachReachesEdge(Zone zone, int ax, int ay, int sx, int sy)
        {
            var seen = new bool[Zone.Width, Zone.Height]; var q = new Queue<(int x, int y)>();
            if (!DryWalkable(zone, ax, ay)) return false;
            seen[ax, ay] = true; q.Enqueue((ax, ay));
            while (q.Count > 0)
            {
                var p = q.Dequeue(); if (p.x == 0 || p.y == 0 || p.x == Zone.Width - 1 || p.y == Zone.Height - 1) return true;
                for (int d = 0; d < 4; d++)
                {
                    int x = p.x + (d == 0 ? 1 : d == 1 ? -1 : 0), y = p.y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (zone.GetCell(x, y) == null || seen[x, y] || AIHelpers.ChebyshevDistance(x, y, sx, sy) <= 3 || !DryWalkable(zone, x, y)) continue;
                    seen[x, y] = true; q.Enqueue((x, y));
                }
            }
            return false;
        }

        private static bool Fresh(Entity e, string blueprint, string weapon, string armor)
            => e != null && e.BlueprintName == blueprint && !string.IsNullOrEmpty(e.ID) && e.SpatialZone == null
                && e.GetPart<PhysicsPart>()?.InInventory == null && e.GetPart<PhysicsPart>()?.Equipped == null
                && e.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(e) && OwnsKit(e, weapon, armor);

        private static bool OwnsKit(Entity actor, string weapon, string armor)
        {
            var inventory = actor?.GetPart<InventoryPart>(); var body = actor?.GetPart<Body>();
            if (inventory?.ParentEntity != actor || body?.ParentEntity != actor) return false;
            var all = inventory.Objects.Concat(inventory.EquippedItems.Values).Distinct().ToArray();
            if (all.Length != (armor == null ? 1 : 2)) return false;
            foreach (string bp in armor == null ? new[] { weapon } : new[] { weapon, armor })
            {
                var items = all.Where(e => e.BlueprintName == bp).ToArray();
                if (items.Length != 1 || items[0].SpatialZone != null || items[0].GetPart<PhysicsPart>()?.Equipped != actor
                    || !body.GetParts().Any(p => ReferenceEquals(p.Equipped, items[0]))) return false;
            }
            return true;
        }

        private static bool Clear(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return cell != null && !cell.BlocksMovement() && !zone.GenReservedCells.Contains((x, y))
                && !cell.Occupants.Any(e => e.HasTag("Creature") || e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<TriggerOnStepPart>());
        }
        // A clear local pocket can still be sealed behind a complete hedge.
        // Inspect existing walkable dry terrain; reservations may be traversed
        // but never used for actors. No terrain is removed to admit the source.
        private static bool[,] ReachableFromEdges(Zone zone)
        {
            var reached = new bool[Zone.Width,Zone.Height];
            var queue = new Queue<(int x,int y)>();
            for (int y=0;y<Zone.Height;y++) for (int x=0;x<Zone.Width;x++)
                if ((x==0 || y==0 || x==Zone.Width-1 || y==Zone.Height-1) && Walkable(zone.GetCell(x,y)))
                { reached[x,y]=true; queue.Enqueue((x,y)); }
            while(queue.Count>0)
            {
                var p=queue.Dequeue();
                for(int d=0;d<4;d++)
                {
                    int x=p.x+(d==0?1:d==1?-1:0), y=p.y+(d==2?1:d==3?-1:0);
                    var c=zone.GetCell(x,y);
                    if(c==null || reached[x,y] || !Walkable(c))continue;
                    reached[x,y]=true;queue.Enqueue((x,y));
                }
            }
            return reached;
        }
        private static bool Walkable(Cell cell) => cell!=null && !cell.BlocksMovement()
            && !cell.Occupants.Any(e=>e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<TriggerOnStepPart>());
        private static bool Pocket(Zone zone, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 3; dx++) if (!Clear(zone, x + dx, y + dy)) return false;
            bool hedge = false;
            for (int dy = -4; dy <= 4; dy++) for (int dx = -4; dx <= 6; dx++)
            {
                var c = zone.GetCell(x + dx, y + dy); if (c == null) continue;
                foreach (var e in c.Occupants)
                { if (e.HasTag("Creature")) return false; if (e.BlueprintName == "Hedge" || e.BlueprintName == "Bush") hedge = true; }
            }
            return hedge;
        }
        private static bool LegalBody(Zone z, Entity e, int x, int y)
        {
            var cells = z.GetOccupiedCells(e, x, y);
            return cells.Count > 0 && cells.All(c => c != null && Clear(z, c.X, c.Y));
        }
        private static bool ViperRefuse(Zone zone, string reason)
        { if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "SpreadRareRejected", payload: new { zone = zone.ZoneID, family = "chalk-ring-viper", reason }); return false; }
        private static bool Refuse(Zone zone, string reason)
        { if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "SpreadRareRejected", payload: new { zone = zone.ZoneID, reason }); return false; }
    }
}
