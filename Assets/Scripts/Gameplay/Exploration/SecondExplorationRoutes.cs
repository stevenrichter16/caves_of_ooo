using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    public sealed class RopeShortcutPart : SecondExplorationServicePart, IDescribeState
    {
        public override string Name => "RopeShortcut";
        public string ZoneID, OtherZoneID;
        public int X, Y, OtherX, OtherY;
        public bool Down, Installed;
        const string RigCommand = "RigRopeShortcut";
        const string RigLabel = "rig rope (two knotflax cord)";
        const string CordNeeded = "You need two usable knotflax cord.";
        internal const string LegacyGuidance = "\nTwo knotflax cord will rig a reciprocal rope to the corresponding lower terrace. First visit both existing levels and confirm their landings; then use the ordinary stair controls at the installed line.";
        protected override string[] Verbs => new[] { RigCommand };
        protected override bool Civilian => false;
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;

        public string DescribeState(DescribeContext context)
        {
            var zone = context?.Zone;
            if (zone == null || context.Owner != ParentEntity || ParentEntity?.GetPart<RopeShortcutPart>() != this
                || !WorldResourceActions.Ground(ParentEntity, zone) || zone.GetEntityPosition(ParentEntity) != (X, Y))
                return null;

            if (Installed)
            {
                if (!Endpoint(zone, out var manager, out _, out _, out var other)
                    || !other.Installed || !HasLink(manager, this) || !HasLink(manager, other))
                    return "Both landings need to be connected before you can use this rope.";
                return Down
                    ? "The rope is rigged. Use the ordinary stair controls to descend when the other landing is clear."
                    : "The rope is rigged. Use the ordinary stair controls to climb when the other landing is clear.";
            }

            string guidance = RigBlock(zone);
            if (guidance != null) return guidance + " Rigging needs two knotflax cord.";
            return context.Viewer == null || HasRiggingCord(context.Viewer)
                ? "Both landings are clear. Rig the rope from here with two knotflax cord."
                : "Both landings are clear. " + CordNeeded;
        }

        static bool HasLink(OverworldZoneManager manager, RopeShortcutPart end)
        {
            string direction = end.Down ? "StairsDown" : "StairsUp";
            if (!end.ParentEntity.HasTag(direction)
                || (end.Down ? !end.ParentEntity.HasPart<StairsDownPart>() : !end.ParentEntity.HasPart<StairsUpPart>()))
                return false;

            var links = manager.GetConnections(end.ZoneID).Where(c => AtAnchor(c, end)).ToArray();
            return links.Length == 1 && links[0].Type == direction && links[0].TargetZoneID == end.OtherZoneID
                && links[0].TargetX == end.OtherX && links[0].TargetY == end.OtherY;
        }

        static bool AtAnchor(ZoneConnection connection, RopeShortcutPart end)
            => connection.SourceZoneID == end.ZoneID && connection.SourceX == end.X && connection.SourceY == end.Y;

        static bool HasStairs(Entity owner) => owner.HasPart<StairsDownPart>() || owner.HasPart<StairsUpPart>();

        bool ClearLandings(Zone zone, Zone otherZone, Entity other)
            => !zone.GetCell(X, Y).BlocksMovement(ParentEntity) && !otherZone.GetCell(OtherX, OtherY).BlocksMovement(other);

        bool CanRig(Zone zone, out OverworldZoneManager manager, out Zone otherZone, out Entity other, out RopeShortcutPart counterpart)
        {
            return Endpoint(zone, out manager, out otherZone, out other, out counterpart)
                && !Installed && !counterpart.Installed && !HasStairs(ParentEntity) && !HasStairs(other)
                && ClearLandings(zone, otherZone, other)
                && !HasConnectionAt(manager, this) && !HasConnectionAt(manager, counterpart);
        }

        static bool HasConnectionAt(OverworldZoneManager manager, RopeShortcutPart end)
            => manager.GetConnections(end.ZoneID).Any(c => AtAnchor(c, end));

        string RigBlock(Zone zone)
        {
            if (CanRig(zone, out var manager, out _, out _, out _)) return null;
            return !string.IsNullOrEmpty(OtherZoneID) && manager?.CachedZones.ContainsKey(OtherZoneID) == false
                ? "Use the ordinary stairs to reach and check the other landing first."
                : "Both landings need to be clear and unrigged.";
        }

        static bool HasRiggingCord(Entity actor)
        {
            var inventory = actor?.GetPart<InventoryPart>();
            if (inventory == null) return false;
            int needed = 2;
            foreach (var item in inventory.Objects)
            {
                if (item?.BlueprintName != "KnotflaxCord" || !SecondExplorationActions.Carried(actor, item)) continue;
                if (!inventory.CanConsumeOne(item)) return false;
                needed -= Math.Min(needed, SecondExplorationActions.Units(item));
                if (needed == 0) return true;
            }
            return false;
        }

        protected override void Choices(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (Installed || actions == null) return;
            string block = RigBlock(zone);
            if (block != null) actions.AddDisabled(RigCommand, RigLabel, "rope-landing-unavailable", block, priority: 24).Hint = block;
            else if (!HasRiggingCord(actor)) actions.AddDisabled(RigCommand, RigLabel, "rope-cord-missing", CordNeeded, priority: 24).Hint = CordNeeded;
            else actions.AddActionWithHint(RigCommand, RigLabel, RigCommand, "Tie both landings in one action. Two cords are spent.", priority: 24);
        }

        bool Endpoint(Zone zone, out OverworldZoneManager manager, out Zone otherZone, out Entity other, out RopeShortcutPart counterpart)
        {
            manager = WorldLocationContext.For(zone); otherZone = null; other = null; counterpart = null;
            if (manager == null || ZoneID != zone.ZoneID || !manager.CachedZones.TryGetValue(ZoneID, out var current) || current != zone
                || string.IsNullOrEmpty(OtherZoneID) || OtherZoneID == ZoneID || !manager.CachedZones.TryGetValue(OtherZoneID, out otherZone)
                || WorldLocationContext.For(otherZone) != manager || zone.GetEntityPosition(ParentEntity) != (X, Y) || !zone.InBounds(X,Y) || !otherZone.InBounds(OtherX,OtherY)) return false;
            var found = otherZone.GetCell(OtherX, OtherY).Objects.Where(e => e.GetPart<RopeShortcutPart>() is RopeShortcutPart r
                && r.ZoneID == OtherZoneID && r.OtherZoneID == ZoneID && r.X == OtherX && r.Y == OtherY && r.OtherX == X && r.OtherY == Y && r.Down != Down).ToArray();
            if (found.Length != 1) return false; other = found[0]; counterpart = other.GetPart<RopeShortcutPart>();
            return SecondExplorationActions.Live(other) && WorldResourceActions.Ground(other, otherZone);
        }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (command != RigCommand || !CanRig(zone, out var manager, out var otherZone, out var other, out var counterpart)
                || !tx.TryClaim(other, actor, command)) return false;
            if (!SecondExplorationActions.Spend(tx, actor, "KnotflaxCord", 2, command) || !Current(actor, zone)
                || !CanRig(zone, out var managerAfter, out var zoneAfter, out var ownerAfter, out var partAfter)
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
