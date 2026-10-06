using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    public sealed class RopeShortcutPart : SecondExplorationServicePart
    {
        public override string Name => "RopeShortcut";
        public string ZoneID, OtherZoneID;
        public int X, Y, OtherX, OtherY;
        public bool Down, Installed;
        protected override string[] Verbs => new[] { "RigRopeShortcut" };
        protected override bool Civilian => false;
        bool Endpoint(Zone zone, out OverworldZoneManager manager, out Zone otherZone, out Entity other, out RopeShortcutPart counterpart)
        {
            manager = WorldLocationContext.For(zone); otherZone = null; other = null; counterpart = null;
            if (manager == null || ZoneID != zone.ZoneID || !manager.CachedZones.TryGetValue(ZoneID, out var current) || current != zone
                || string.IsNullOrEmpty(OtherZoneID) || OtherZoneID == ZoneID || !manager.CachedZones.TryGetValue(OtherZoneID, out otherZone)
                || zone.GetEntityPosition(ParentEntity) != (X, Y) || !zone.InBounds(X,Y) || !otherZone.InBounds(OtherX,OtherY)) return false;
            var found = otherZone.GetCell(OtherX, OtherY).Objects.Where(e => e.GetPart<RopeShortcutPart>() is RopeShortcutPart r
                && r.ZoneID == OtherZoneID && r.OtherZoneID == ZoneID && r.X == OtherX && r.Y == OtherY && r.OtherX == X && r.OtherY == Y && r.Down != Down).ToArray();
            if (found.Length != 1) return false; other = found[0]; counterpart = other.GetPart<RopeShortcutPart>();
            return SecondExplorationActions.Live(other) && WorldResourceActions.Ground(other, otherZone)
                && !zone.GetCell(X,Y).BlocksMovement(ParentEntity) && !otherZone.GetCell(OtherX,OtherY).BlocksMovement(other);
        }
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        { if (!Installed && Endpoint(zone, out _, out _, out _, out var other) && !other.Installed) Offer(a, "rig rope (2 cord; both ends visited)", "RigRopeShortcut"); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (command != "RigRopeShortcut" || Installed || !Endpoint(zone, out var manager, out var otherZone, out var other, out var counterpart)
                || counterpart.Installed || ParentEntity.HasPart<StairsDownPart>() || ParentEntity.HasPart<StairsUpPart>() || other.HasPart<StairsDownPart>() || other.HasPart<StairsUpPart>()
                || !tx.TryClaim(other, actor, command)) return false;
            if (manager.GetConnections(ZoneID).Any(c => c.SourceZoneID == ZoneID && c.SourceX == X && c.SourceY == Y)
                || manager.GetConnections(OtherZoneID).Any(c => c.SourceZoneID == OtherZoneID && c.SourceX == OtherX && c.SourceY == OtherY)) return false;
            if (!SecondExplorationActions.Spend(tx, actor, "KnotflaxCord", 2, command) || !Current(actor, zone)
                || !Endpoint(zone, out var managerAfter, out var zoneAfter, out var ownerAfter, out var partAfter)
                || managerAfter != manager || zoneAfter != otherZone || ownerAfter != other || partAfter != counterpart) return false;
            var forward = new ZoneConnection { SourceZoneID = ZoneID, SourceX = X, SourceY = Y, TargetZoneID = OtherZoneID, TargetX = OtherX, TargetY = OtherY, Type = Down ? "StairsDown" : "StairsUp" };
            var reverse = new ZoneConnection { SourceZoneID = OtherZoneID, SourceX = OtherX, SourceY = OtherY, TargetZoneID = ZoneID, TargetX = X, TargetY = Y, Type = Down ? "StairsUp" : "StairsDown" };
            Part first = Down ? (Part)new StairsDownPart() : new StairsUpPart(); Part second = Down ? (Part)new StairsUpPart() : new StairsDownPart();
            string firstTag = Down ? "StairsDown" : "StairsUp", secondTag = Down ? "StairsUp" : "StairsDown";
            bool hadFirstTag = ParentEntity.Tags.TryGetValue(firstTag, out string priorFirstTag);
            bool hadSecondTag = other.Tags.TryGetValue(secondTag, out string priorSecondTag);
            tx.Do(null, () =>
            {
                manager.RemoveConnection(forward); manager.RemoveConnection(reverse); ParentEntity.RemovePart(first); other.RemovePart(second);
                if (hadFirstTag) ParentEntity.Tags[firstTag] = priorFirstTag; else ParentEntity.Tags.Remove(firstTag);
                if (hadSecondTag) other.Tags[secondTag] = priorSecondTag; else other.Tags.Remove(secondTag);
                Installed = counterpart.Installed = false;
            });
            // Input/stair approach uses Parts; native destination selection uses tags.
            // Both authorities join the same two-owner rollback receipt.
            ParentEntity.AddPart(first); other.AddPart(second); ParentEntity.SetTag(firstTag); other.SetTag(secondTag);
            manager.RegisterConnection(forward); manager.RegisterConnection(reverse); Installed = counterpart.Installed = true;
            tx.AfterCommit(() => { MessageLog.Add("The rope now joins both landings. Use the ordinary stair travel controls at either anchor."); ZoneRenderHooks.MarkFullDirty("Rope.Installed"); }); return true;
        }
    }
    public sealed class LocalPassagePermitPart : SecondExplorationServicePart
    {
        public override string Name => "LocalPassagePermit";
        public Entity PermitHolder;
        protected override string[] Verbs => new[] { "PermitPassage" };
        internal bool CurrentPost(Zone zone)
        {
            var role = ParentEntity?.GetPart<SpreadTerritoryPart>();
            if (role?.Configured != true || role.ZoneID != zone?.ZoneID || !WorldResourceActions.Ground(role.Post, zone)
                || !WorldResourceActions.Ground(ParentEntity, zone) || !SecondExplorationActions.Live(ParentEntity)) return false;
            return zone.GetEntityPosition(role.Post) == (role.PostX, role.PostY) && role.Left <= role.Right && role.Top <= role.Bottom
                && zone.InBounds(role.Left, role.Top) && zone.InBounds(role.Right, role.Bottom);
        }
        internal bool Inside(Entity actor, Zone zone)
        {
            var role = ParentEntity?.GetPart<SpreadTerritoryPart>(); var cell = actor == null ? null : zone?.GetEntityCell(actor);
            return role != null && cell != null && cell.X >= role.Left && cell.X <= role.Right && cell.Y >= role.Top && cell.Y <= role.Bottom;
        }
        public bool Allows(Entity actor, Zone zone)
        {
            var token = actor?.GetPart<LocalPassageTokenPart>();
            return CurrentPost(zone) && PermitHolder == actor && token?.Guard == ParentEntity && !token.Spent
                && !ParentEntity.GetPart<BrainPart>().IsPersonallyHostileTo(actor);
        }
        internal bool ClaimsEntry(Entity actor, Zone zone) => actor?.HasTag("Player") == true && CurrentPost(zone) && Inside(actor, zone) && !Allows(actor, zone);
        protected override bool Current(Entity actor, Zone zone) => SecondExplorationActions.Near(actor, ParentEntity, zone) && CurrentPost(zone)
            && ParentEntity.GetPart<BrainPart>() is BrainPart b && !b.IsPersonallyHostileTo(actor) && (b.Target == null || b.Target == actor)
            && !FactionManager.IsHostile(ParentEntity, actor);
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        { if (!Inside(actor, zone) && !Allows(actor, zone)) Offer(a, "(4dr) buy one cutbank passage", "PermitPassage"); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (command != "PermitPassage" || Inside(actor, zone) || Allows(actor, zone) || !SecondExplorationActions.Pay(tx, actor, ParentEntity, 4)) return false;
            var oldToken = actor.GetPart<LocalPassageTokenPart>(); if (oldToken != null && !oldToken.Spent) return false;
            var prior = PermitHolder; var token = new LocalPassageTokenPart { Guard = ParentEntity };
            tx.Do(() => { if (oldToken != null) actor.RemovePart(oldToken); actor.AddPart(token); PermitHolder = actor; },
                () => { actor.RemovePart(token); if (oldToken != null) actor.AddPart(oldToken); PermitHolder = prior; }); return true;
        }
    }
    /// <summary>Exact local passage state follows actual successful actor moves,
    /// not guard scheduling speed. Public fields are ordinary replacement-save data.</summary>
    public sealed class LocalPassageTokenPart : Part
    {
        public override string Name => "LocalPassageToken";
        public Entity Guard;
        public bool Entered, Spent;
        public override bool HandleEvent(GameEvent e)
        {
            if (Spent || (e.ID != "AfterMove" && e.ID != "EndTurn")) return true;
            var permit = Guard?.GetPart<LocalPassagePermitPart>(); var zone = Guard?.SpatialZone;
            if (permit == null || zone == null || !permit.CurrentPost(zone) || permit.PermitHolder != ParentEntity) { Spent = true; return true; }
            if (ParentEntity.SpatialZone == zone && permit.Inside(ParentEntity, zone)) Entered = true;
            else if (Entered)
            {
                Spent = true; Diag.Record("event", "LocalPassageCompleted", ParentEntity, Guard, new { zoneId = zone.ZoneID });
            }
            return true;
        }
    }
}
