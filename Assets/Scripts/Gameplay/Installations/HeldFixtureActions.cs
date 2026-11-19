using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Set down, take up and hand over (Docs/HOME-BASE-DESIGN.md sections 8.2 to 8.4, slice PB2a). One <b>same entity</b> moves between the pack and the ground: the same
    /// ID, contents, escrow and condition. Every step runs inside the native inventory transaction, so a refusal or a later outer failure restores the pack, the cell, the
    /// flags, the tag and the Parts exactly. Rows declare <see cref="ActionTime.OneAction"/> (spec S3): one action on commit, none on refusal.
    /// </summary>
    public static class HeldFixtureActions
    {
        const string SetDownPrefix = "SetDown|", TakeUpPrefix = "TakeUp|", HandOverPrefix = "HandOver|";
        public const string OwnedTag = "Owned", PlayerOrigin = "Player";
        static readonly (int x, int y, string label)[] Directions =
        {
            (0,-1,"north"),(1,-1,"northeast"),(1,0,"east"),(1,1,"southeast"),
            (0,1,"south"),(-1,1,"southwest"),(-1,0,"west"),(-1,-1,"northwest")
        };

        public static bool IsCommand(string command) => command != null
            && (command.StartsWith(SetDownPrefix, StringComparison.Ordinal) || command.StartsWith(TakeUpPrefix, StringComparison.Ordinal)
                || command.StartsWith(HandOverPrefix, StringComparison.Ordinal));

        /// <summary>The plain sentence for a refusal code. Mechanical on purpose: it never attributes intent to the ground, and every string still needs the voice review.</summary>
        public static string Reason(string code)
        {
            switch (code)
            {
                case PlacementCheck.PoiZone: return "Nothing can be set down in a place like this.";
                case PlacementCheck.ProtectedGround: return "Nothing can be set down on this ground.";
                case PlacementCheck.Biome: return "Nothing can be set down in this country.";
                case PlacementCheck.FamilyOwned: return "Nothing can be set down in this place yet.";
                case PlacementCheck.ColumnPending: return "Nothing can be set down underground yet.";
                case PlacementCheck.UnsupportedZone: return "Nothing can be set down here.";
                case PlacementCheck.Ground: return "The ground here will not hold it.";
                case PlacementCheck.CellOccupied: return "Something is already there.";
                case PlacementCheck.OpenNeighbours: return "It needs open ground on every side.";
                case PlacementCheck.EdgeMargin: return "It is too near the edge of this ground.";
                case PlacementCheck.StairsDoors: return "It is too near a door, a stair or a well.";
                case PlacementCheck.CapZone: return "You already keep as many things standing here as you can.";
                case PlacementCheck.CapWorld: return "You already keep as many things standing as you can.";
                case PlacementCheck.State: return "That cannot be set down as it is.";
                case PlacementCheck.ClassSpecific: return "That cannot be set on this kind of ground.";
                case "overweight-load": return "You cannot carry that load.";
                case "unknown-kind": return "You cannot tell how this was meant to be moved.";
                case "stale-selection": return "That is no longer how things stand.";
                case "vetoed": return "Something prevents that.";
                default: return "That cannot be done right now.";
            }
        }

        // ── rows ──

        /// <summary>Called from the fixture's own Part. A carried kit offers "set down" in the pack menu; a standing, player-held fixture offers "take up" and "hand over" to an adjacent actor.</summary>
        public static void AddActions(Entity actor, Entity fixture, InventoryActionList actions)
        {
            if (actions == null || actor == null || fixture == null) return;
            var zone = actor.SpatialZone; var part = fixture.GetPart<PackableFixturePart>();
            if (zone == null || part == null || !WorldResourceActions.ActorCurrent(actor, zone)) return;
            var installable = fixture.GetPart<InstallablePart>();
            bool standing = fixture.SpatialZone == zone && zone.GetEntityCell(fixture) != null;
            if (!standing)
            {
                if (installable != null && !installable.IsLoose) return;
                AddSetDownRows(actor, fixture, zone, part, actions);
                return;
            }
            if (!IsPlayerHeld(fixture)) return;
            var cell = zone.GetEntityCell(fixture); var origin = zone.GetEntityCell(actor);
            if (origin == null || Math.Abs(cell.X - origin.X) > 1 || Math.Abs(cell.Y - origin.Y) > 1) return;
            string tail = Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + fixture.ID;
            actions.AddAction("TakeUpFixture", "take up", TakeUpPrefix + tail, '\0', 22, false, ActionTime.OneAction);
            // The label states the consequence plainly, since there is no confirmation step: the hold ends, the object stays, and it can no longer be taken up.
            actions.AddAction("HandOverFixture", "hand over (you can no longer take it up)", HandOverPrefix + tail, '\0', 3, false, ActionTime.OneAction);
        }

        static void AddSetDownRows(Entity actor, Entity kit, Zone zone, PackableFixturePart part, InventoryActionList actions)
        {
            var pack = actor.GetPart<InventoryPart>();
            if (pack == null || !pack.Objects.Contains(kit)) return;
            var def = PackableFixtureRegistry.Get(part.Kind);
            if (def == null || def.Class != PackableFixtureDefinition.Carried) return;
            var origin = zone.GetEntityCell(actor); var batch = new PlacementCheck.PlacementBatch(WorldLocationContext.For(zone), zone);
            int legal = 0; string firstReason = null;
            foreach (var d in Directions)
            {
                int x = origin.X + d.x, y = origin.Y + d.y;
                var result = batch.Evaluate(x, y, def.Spec, kit);
                if (!result.Ok) { if (firstReason == null) firstReason = result.First; continue; }
                legal++;
                actions.AddAction("SetDownFixture", "set down " + d.label, SetDownCommand(zone, origin, x, y, kit), '\0', 17, false, ActionTime.OneAction);
            }
            // Nothing legal here: say why in one disabled row instead of eight, or none.
            if (legal == 0) actions.AddDisabled("SetDownFixture", "set down", firstReason ?? "unavailable", Reason(firstReason), '\0', 17);
        }

        static string SetDownCommand(Zone zone, Cell origin, int x, int y, Entity kit)
            => SetDownPrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + N(x) + "|" + N(y) + "|" + kit.ID;

        static bool IsPlayerHeld(Entity e)
        {
            var marker = e.GetPart<InstallationMarkerPart>();
            return marker != null && marker.Origin == PlayerOrigin && e.GetPart<InstallablePart>()?.State == InstallablePart.Installed;
        }

        // ── execution ──

        internal static bool TryAct(Entity actor, Entity fixture, PackableFixturePart part, Zone zone, string command, InventoryTransaction tx)
        {
            if (tx == null) return Reject(actor, fixture, command, "missing-transaction");
            if (command.StartsWith(SetDownPrefix, StringComparison.Ordinal)) return SetDown(actor, fixture, part, zone, command, tx);
            if (command.StartsWith(TakeUpPrefix, StringComparison.Ordinal)) return TakeUp(actor, fixture, part, zone, command, tx);
            return HandOver(actor, fixture, part, zone, command, tx);
        }

        static bool SetDown(Entity actor, Entity kit, PackableFixturePart part, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, kit, command, "actor-unavailable");
            if (f.Length != 7 || !Int(f[2], out int ox) || !Int(f[3], out int oy) || !Int(f[4], out int x) || !Int(f[5], out int y)) return Reject(actor, kit, command, "malformed-selection");
            var origin = zone.GetEntityCell(actor); var pack = actor.GetPart<InventoryPart>();
            if (Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[6] != kit.ID || origin == null || origin.X != ox || origin.Y != oy
                || pack == null || !pack.Objects.Contains(kit)) return Reject(actor, kit, command, "stale-selection");
            if (Math.Abs((long)x - origin.X) > 1 || Math.Abs((long)y - origin.Y) > 1 || (x == origin.X && y == origin.Y)) return Reject(actor, kit, command, PlacementCheck.CellOccupied);
            var def = PackableFixtureRegistry.Get(part.Kind);
            if (def == null || def.Class != PackableFixtureDefinition.Carried) return Reject(actor, kit, command, "unknown-kind");
            var installable = kit.GetPart<InstallablePart>();
            if (installable != null && !installable.IsLoose) return Reject(actor, kit, command, PlacementCheck.State);
            var check = PlacementCheck.Evaluate(WorldLocationContext.For(zone), zone, x, y, def.Spec, kit);
            if (!check.Ok) return Reject(actor, kit, command, check.First);
            if (!InstallEvents.Allow(InstallEvents.BeforeInstall, actor, kit, zone, x, y, PlayerOrigin)) return Reject(actor, kit, command, "vetoed");
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(kit, actor, command)) return Reject(actor, kit, command, "in-progress");

            var physics = kit.GetPart<PhysicsPart>(); if (physics == null) return Reject(actor, kit, command, PlacementCheck.State);
            var receipt = InventoryTransferSnapshot.Capture(pack);
            tx.Do(null, receipt.Restore);
            if (!receipt.Apply(() => pack.RemoveObject(kit)) || !receipt.ClaimChanges(tx, actor, command)) return Reject(actor, kit, command, "changed-during-payment");

            bool wasTakeable = physics.Takeable, wasSolid = physics.Solid, hadOwned = kit.HasTag(OwnedTag); string ownedValue = kit.GetTag(OwnedTag);
            tx.Do(null, () =>
            {
                physics.Takeable = wasTakeable; physics.Solid = wasSolid;
                if (hadOwned) kit.SetTag(OwnedTag, ownedValue); else kit.Tags.Remove(OwnedTag);
            });
            physics.Takeable = false; physics.Solid = def.Solid; kit.SetTag(OwnedTag, "true");

            if (!zone.AddEntity(kit, x, y)) return Reject(actor, kit, command, "placement-refused");
            tx.Do(null, () => { if (kit.SpatialZone == zone) zone.RemoveEntity(kit); });

            int priorTick = part.SetTick; bool hadInstallable = installable != null; string priorState = installable?.State, priorMode = installable?.Mode;
            var marker = kit.GetPart<InstallationMarkerPart>(); bool hadMarker = marker != null;
            string priorMarkerId = marker?.InstallationId, priorOrigin = marker?.Origin; bool priorRetain = marker?.RetainZone ?? false; int priorRevision = marker?.Revision ?? 0;
            tx.Do(null, () =>
            {
                part.SetTick = priorTick;
                var current = kit.GetPart<InstallablePart>();
                if (!hadInstallable) { if (current != null) kit.RemovePart(current); }
                else if (current != null) { current.State = priorState; current.Mode = priorMode; }
                var m = kit.GetPart<InstallationMarkerPart>();
                if (!hadMarker) { if (m != null) kit.RemovePart(m); }
                else if (m != null) { m.InstallationId = priorMarkerId; m.Origin = priorOrigin; m.RetainZone = priorRetain; m.Revision = priorRevision; }
            });
            if (installable == null) { installable = new InstallablePart(); kit.AddPart(installable); }
            installable.Mode = InstallablePart.Portable; installable.State = InstallablePart.Installed;
            if (marker == null) { marker = new InstallationMarkerPart(); kit.AddPart(marker); }
            marker.InstallationId = "player:" + kit.ID; marker.Origin = PlayerOrigin; marker.RetainZone = true;
            part.SetTick = TurnManager.Active?.TickCount ?? 0;

            tx.AfterCommit(() =>
            {
                if (kit.SpatialZone != zone) return;
                ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(kit), "FixtureSetDown");
                Diag.Record("furniture", "SetDown", actor, kit, new { kind = part.Kind, zone = zone.ZoneID, x, y, contents = Contents(kit) });
                InstallEvents.Notify(InstallEvents.Installed, actor, kit, zone, x, y, PlayerOrigin);
                MessageLog.Add("You set " + kit.GetDisplayName() + " down.");
            });
            return true;
        }

        static bool TakeUp(Entity actor, Entity fixture, PackableFixturePart part, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, fixture, command, "actor-unavailable");
            var cell = zone.GetEntityCell(fixture); var origin = zone.GetEntityCell(actor); var pack = actor.GetPart<InventoryPart>();
            if (f.Length != 3 || Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[2] != fixture.ID || cell == null || origin == null || pack == null
                || fixture.SpatialZone != zone || !IsPlayerHeld(fixture) || Math.Abs(cell.X - origin.X) > 1 || Math.Abs(cell.Y - origin.Y) > 1) return Reject(actor, fixture, command, "stale-selection");
            var def = PackableFixtureRegistry.Get(part.Kind);
            if (def == null || def.Class != PackableFixtureDefinition.Carried) return Reject(actor, fixture, command, "unknown-kind");
            if (!InstallEvents.Allow(InstallEvents.BeforeUninstall, actor, fixture, zone, cell.X, cell.Y, PlayerOrigin)) return Reject(actor, fixture, command, "vetoed");
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(fixture, actor, command)) return Reject(actor, fixture, command, "in-progress");

            var physics = fixture.GetPart<PhysicsPart>(); var installable = fixture.GetPart<InstallablePart>(); var marker = fixture.GetPart<InstallationMarkerPart>();
            if (physics == null || installable == null || marker == null) return Reject(actor, fixture, command, PlacementCheck.State);
            int x = cell.X, y = cell.Y;

            if (!zone.RemoveEntity(fixture)) return Reject(actor, fixture, command, "changed-during-payment");
            tx.Do(null, () => { if (fixture.SpatialZone == null) zone.AddEntity(fixture, x, y); });

            bool wasTakeable = physics.Takeable, wasSolid = physics.Solid, hadOwned = fixture.HasTag(OwnedTag); string ownedValue = fixture.GetTag(OwnedTag);
            string priorState = installable.State; int priorTick = part.SetTick;
            string markerId = marker.InstallationId, markerOrigin = marker.Origin; bool markerRetain = marker.RetainZone; int markerRevision = marker.Revision;
            tx.Do(null, () =>
            {
                physics.Takeable = wasTakeable; physics.Solid = wasSolid;
                if (hadOwned) fixture.SetTag(OwnedTag, ownedValue); else fixture.Tags.Remove(OwnedTag);
                installable.State = priorState; part.SetTick = priorTick;
                if (fixture.GetPart<InstallationMarkerPart>() == null)
                    fixture.AddPart(new InstallationMarkerPart { InstallationId = markerId, Origin = markerOrigin, RetainZone = markerRetain, Revision = markerRevision });
            });
            physics.Takeable = true; physics.Solid = false; fixture.Tags.Remove(OwnedTag);
            installable.State = InstallablePart.Loose; part.SetTick = -1; fixture.RemovePart(marker);

            var receipt = InventoryTransferSnapshot.Capture(pack, fixture);
            tx.Do(null, receipt.Restore);
            // AddObject refuses an over-capacity load (the container's contents weigh with it); nothing spills.
            if (!receipt.Apply(() => pack.AddObject(fixture)) || !receipt.ClaimChanges(tx, actor, command) || !pack.Objects.Contains(fixture))
                return Reject(actor, fixture, command, "overweight-load");

            tx.AfterCommit(() =>
            {
                ZoneRenderHooks.MarkCellDirty(zone.GetCell(x, y), "FixtureTakenUp");
                Diag.Record("furniture", "TakenUp", actor, fixture, new { kind = part.Kind, zone = zone.ZoneID, x, y, contents = Contents(fixture) });
                InstallEvents.Notify(InstallEvents.Uninstalled, actor, fixture, zone, x, y, PlayerOrigin);
                MessageLog.Add("You take up " + fixture.GetDisplayName() + ".");
            });
            return true;
        }

        static bool HandOver(Entity actor, Entity fixture, PackableFixturePart part, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone)) return Reject(actor, fixture, command, "actor-unavailable");
            var cell = zone.GetEntityCell(fixture); var origin = zone.GetEntityCell(actor);
            if (f.Length != 3 || Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[2] != fixture.ID || cell == null || origin == null || fixture.SpatialZone != zone
                || !IsPlayerHeld(fixture) || Math.Abs(cell.X - origin.X) > 1 || Math.Abs(cell.Y - origin.Y) > 1) return Reject(actor, fixture, command, "stale-selection");
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(fixture, actor, command)) return Reject(actor, fixture, command, "in-progress");
            var marker = fixture.GetPart<InstallationMarkerPart>(); bool hadOwned = fixture.HasTag(OwnedTag); string ownedValue = fixture.GetTag(OwnedTag);
            string markerId = marker.InstallationId, markerOrigin = marker.Origin; bool markerRetain = marker.RetainZone; int markerRevision = marker.Revision;
            tx.Do(null, () =>
            {
                if (hadOwned) fixture.SetTag(OwnedTag, ownedValue); else fixture.Tags.Remove(OwnedTag);
                if (fixture.GetPart<InstallationMarkerPart>() == null)
                    fixture.AddPart(new InstallationMarkerPart { InstallationId = markerId, Origin = markerOrigin, RetainZone = markerRetain, Revision = markerRevision });
            });
            // The object stays exactly where it is; only the player's hold on it ends. It is an ordinary, un-held object of the zone from here on.
            fixture.RemovePart(marker); fixture.Tags.Remove(OwnedTag);
            int x = cell.X, y = cell.Y;
            tx.AfterCommit(() =>
            {
                Diag.Record("furniture", "HandedOver", actor, fixture, new { kind = part.Kind, zone = zone.ZoneID, x, y, spoken = true, contents = Contents(fixture) });
                MessageLog.Add("You let go of " + fixture.GetDisplayName() + ". It stays where it is.");
            });
            return true;
        }

        // ── helpers ──

        static int Contents(Entity fixture) => fixture.GetPart<ContainerPart>()?.Contents.Count ?? 0;
        static bool Int(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        static string N(int value) => value.ToString(CultureInfo.InvariantCulture);

        static bool Reject(Entity actor, Entity fixture, string command, string reason)
        {
            Diag.Record("furniture", "FixtureRefused", actor, fixture, new { command = (command ?? "").Split('|')[0], reason });
            MessageLog.Add(Reason(reason));
            return false;
        }
    }
}
