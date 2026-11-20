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
        const string SetDownPrefix = "SetDown|", TakeUpPrefix = "TakeUp|", HandOverPrefix = "HandOver|", MakeCampPrefix = "MakeCamp|", PackUpPrefix = "PackUp|", MovePrefix = "MoveFixture|", NameCampPrefix = "NameCamp|";
        /// <summary>How far from the actor a kit can be moved to (Chebyshev). The kit itself must still be beside the actor to be lifted.</summary>
        public const int MoveReach = 2;
        /// <summary>The single "move" row's command. It is not a Part command: the interact menu intercepts it and opens a cell picker, and the move itself runs as <see cref="MoveCommand"/> once a cell is chosen.</summary>
        public const string PickMovePrefix = "MovePick|";
        /// <summary>How far from the actor "pack up camp" reaches for the things you keep (Chebyshev). Ordinary take-up still needs you beside the one thing.</summary>
        public const int PackUpReach = 4;
        /// <summary>The order "make camp" places kits in: the fire first, then what must be beside it, then the rest; unknown kinds last.</summary>
        static readonly string[] CampOrder = { CampKinds.Hearth, CampKinds.Still, CampKinds.Bench, CampKinds.Box };
        public const string OwnedTag = "Owned", PlayerOrigin = "Player";
        static readonly (int x, int y, string label)[] Directions =
        {
            (0,-1,"north"),(1,-1,"northeast"),(1,0,"east"),(1,1,"southeast"),
            (0,1,"south"),(-1,1,"southwest"),(-1,0,"west"),(-1,-1,"northwest")
        };

        public static bool IsCommand(string command) => command != null
            && (command.StartsWith(SetDownPrefix, StringComparison.Ordinal) || command.StartsWith(TakeUpPrefix, StringComparison.Ordinal)
                || command.StartsWith(HandOverPrefix, StringComparison.Ordinal) || command.StartsWith(MakeCampPrefix, StringComparison.Ordinal)
                || command.StartsWith(PackUpPrefix, StringComparison.Ordinal) || command.StartsWith(MovePrefix, StringComparison.Ordinal)
                || command.StartsWith(NameCampPrefix, StringComparison.Ordinal));

        /// <summary>The plain sentence for a refusal code. Mechanical on purpose: it never attributes intent to the ground, and every string still needs the voice review.</summary>
        /// <summary>The refusal sentence for a kit: the generic one, except that a shore-ground kit refused on the ground class says what it needs (open ground right beside water it can fish).</summary>
        public static string Reason(string code, PackableFixtureDefinition kit)
            => code == PlacementCheck.ClassSpecific && kit?.GroundClass == PlacementCheck.ShoreGround ? "It needs open ground right beside water it can fish: a wide pool of falling water, such as a cascade basin." : Reason(code);

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
                case "out-of-reach": return "That is too far from you to move it there.";
                case "same-cell": return "It is already there.";
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
            int near = HeldWithin(zone, origin, PackUpReach).Count;
            if (near >= 2) actions.AddActionWithHint("PackUpCamp", "pack up camp (" + near + " things)", PackUpPrefix + tail, "Takes up every kit of yours within " + PackUpReach + " cells in one action. All or nothing; a burning fire goes out.", '\0', 21, false, ActionTime.OneAction);
            AddMoveRows(actor, fixture, zone, part, cell, actions);
            string next = HeldFixtureReader.NextCampName(HeldFixtureReader.CampNameOf(HeldFixtureReader.ClusterOf(zone, fixture)));
            actions.AddActionWithHint("NameCamp", next == "" ? "clear this camp's name" : "name this camp: " + next, NameCampPrefix + tail,
                "Names this camp in the notes of where you keep things. Choose again for the next name.", '\0', 1, false, ActionTime.Free);
        }

        /// <summary>Every fixture the player holds in the zone within <paramref name="reach"/> of the actor's cell, in a stable order.</summary>
        static List<Entity> HeldWithin(Zone zone, Cell origin, int reach)
        {
            var found = new List<Entity>(); if (zone == null || origin == null) return found;
            var owners = zone.GetReadOnlyEntitiesWithPart<PackableFixturePart>();
            for (int i = 0; i < owners.Count; i++)
            {
                var e = owners[i]; var at = zone.GetEntityCell(e);
                if (at == null || !IsPlayerHeld(e) || Math.Max(Math.Abs(at.X - origin.X), Math.Abs(at.Y - origin.Y)) > reach) continue;
                var def = PackableFixtureRegistry.Get(e.GetPart<PackableFixturePart>().Kind); if (def == null || def.Class != PackableFixtureDefinition.Carried) continue;
                found.Add(e);
            }
            found.Sort((a, b) => string.CompareOrdinal(a.ID, b.ID)); return found;
        }

        /// <summary>A placement that fails only on the world and zone caps is fine for a move: taking the kit up frees the slot it is about to take again.</summary>
        static bool OkForMove(PlacementResult r) => r.Ok || r.Reasons.All(reason => reason == PlacementCheck.CapZone || reason == PlacementCheck.CapWorld);

        /// <summary>The command a chosen cell turns into: take the kit up and set it down at (x, y), as one action.</summary>
        public static string MoveCommand(Zone zone, Entity fixture, int x, int y)
            => MovePrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + fixture.ID + "|" + N(x) + "|" + N(y);

        /// <summary>
        /// Whether the standing kit could be moved to (x, y) right now, and if not the refusal code (see <see cref="Reason"/>). It asks the same questions the move itself asks, so the
        /// green and red borders of the cell picker always agree with what confirming would do: the actor beside the kit, the cell within <see cref="MoveReach"/> of the actor and not
        /// the kit's own, the load liftable, and the placement rules for open ground, hazards, stairs and doors, other kits and the zone caps (a move frees the slot it takes).
        /// </summary>
        public static bool CanMoveTo(Entity actor, Entity fixture, Zone zone, int x, int y, out string code)
        {
            code = null;
            var part = fixture?.GetPart<PackableFixturePart>(); var def = part == null ? null : PackableFixtureRegistry.Get(part.Kind);
            var origin = actor == null || zone == null ? null : zone.GetEntityCell(actor); var cell = fixture == null || zone == null ? null : zone.GetEntityCell(fixture);
            if (def == null || def.Class != PackableFixtureDefinition.Carried || zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || origin == null || cell == null
                || fixture.SpatialZone != zone || !IsPlayerHeld(fixture) || Math.Abs((long)cell.X - origin.X) > 1 || Math.Abs((long)cell.Y - origin.Y) > 1) { code = "stale-selection"; return false; }
            if (!zone.InBounds(x, y) || Math.Abs((long)x - origin.X) > MoveReach || Math.Abs((long)y - origin.Y) > MoveReach) { code = "out-of-reach"; return false; }
            if (x == cell.X && y == cell.Y) { code = "same-cell"; return false; }
            if (!CanLift(actor, fixture)) { code = "overweight-load"; return false; }
            var result = new PlacementCheck.PlacementBatch(WorldLocationContext.For(zone), zone).Evaluate(x, y, def.Spec, fixture);
            if (!OkForMove(result)) { code = result.Reasons.FirstOrDefault(r => r != PlacementCheck.CapZone && r != PlacementCheck.CapWorld) ?? PlacementCheck.CellOccupied; return false; }
            return true;
        }

        /// <summary>Whether taking the standing fixture up would fit in the actor's pack (what the take-up half of a move needs): a dry run of the pack's own capacity rule.</summary>
        static bool CanLift(Entity actor, Entity fixture)
        {
            var pack = actor?.GetPart<InventoryPart>(); if (pack == null || fixture == null) return false;
            return pack.MaxWeight < 0 || (long)pack.GetCarriedWeight() + InventoryPart.GetItemWeight(fixture) <= pack.MaxWeight;
        }

        /// <summary>The single "move" row: opens the cell picker (green where it fits, red where it does not). A kit too heavy to lift says so instead.</summary>
        static void AddMoveRows(Entity actor, Entity fixture, Zone zone, PackableFixturePart part, Cell cell, InventoryActionList actions)
        {
            var def = PackableFixtureRegistry.Get(part.Kind); if (def == null || def.Class != PackableFixtureDefinition.Carried) return;
            if (!CanLift(actor, fixture)) { actions.AddDisabled("MoveFixture", "move", "overweight-load", "You cannot carry that load, so you cannot lift it to move it.", 'm', 2); return; }
            actions.AddActionWithHint("MoveFixture", "move", PickMovePrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + fixture.ID,
                "Pick a cell with the arrow keys: green fits, red does not. It keeps its fuel; a burning fire goes out.", 'm', 2);
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
                actions.AddAction("SetDownFixture", "set down " + d.label + Hint(part.Kind, zone, x, y), SetDownCommand(zone, origin, x, y, kit), '\0', 17, false, ActionTime.OneAction);
            }
            // Nothing legal here: say why in one disabled row instead of eight, or none.
            if (legal == 0) { actions.AddDisabled("SetDownFixture", "set down", firstReason ?? "unavailable", Reason(firstReason, def), '\0', 17); return; }
            int kits = CarriedKits(actor).Count;
            if (kits >= 2) actions.AddActionWithHint("MakeCamp", "make camp here (" + kits + " kits)", MakeCampPrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + kit.ID,
                "Sets down every kit you carry at once: fire first, still beside it.", '\0', 18, false, ActionTime.OneAction);
        }

        /// <summary>Where the kit would stand relative to what you already keep: a still wants a fire within reach (its settling is coupled to one), a hearth is by a still, so a cell says so.</summary>
        static string Hint(string kind, Zone zone, int x, int y)
        {
            if (kind != CampKinds.Still && kind != CampKinds.Hearth) return "";
            bool hearth = false, fire = false, still = false;
            var owners = zone.GetReadOnlyEntitiesWithPart<PackableFixturePart>();
            for (int i = 0; i < owners.Count; i++)
            {
                var e = owners[i]; var at = zone.GetEntityCell(e);
                if (at == null || !IsPlayerHeld(e) || Math.Max(Math.Abs(at.X - x), Math.Abs(at.Y - y)) > LiquidSettlingPart.FireReach) continue;
                string other = e.GetPart<PackableFixturePart>().Kind;
                if (other == CampKinds.Hearth) { hearth = true; if (e.GetPart<HearthGlowPart>()?.Burning() == true) fire = true; }
                else if (other == CampKinds.Still) still = true;
            }
            if (kind == CampKinds.Still) return fire ? " (by your fire)" : hearth ? " (by your hearth)" : "";
            return still ? " (by your still)" : "";
        }

        /// <summary>The loose, set-down-able kits the actor carries, in the order "make camp" places them.</summary>
        static List<Entity> CarriedKits(Entity actor)
        {
            var kits = new List<Entity>(); var pack = actor?.GetPart<InventoryPart>(); if (pack == null) return kits;
            foreach (var e in pack.Objects)
            {
                var p = e?.GetPart<PackableFixturePart>(); if (p == null) continue;
                var def = PackableFixtureRegistry.Get(p.Kind); if (def == null || def.Class != PackableFixtureDefinition.Carried) continue;
                var installable = e.GetPart<InstallablePart>(); if (installable != null && !installable.IsLoose) continue;
                if ((e.GetPart<StackerPart>()?.StackCount ?? 1) != 1) continue;
                kits.Add(e);
            }
            int Rank(Entity e) { int i = Array.IndexOf(CampOrder, e.GetPart<PackableFixturePart>().Kind); return i < 0 ? CampOrder.Length : i; }
            kits.Sort((a, b) => Rank(a) != Rank(b) ? Rank(a).CompareTo(Rank(b)) : string.CompareOrdinal(a.ID, b.ID));
            return kits;
        }

        static string SetDownCommand(Zone zone, Cell origin, int x, int y, Entity kit)
            => SetDownPrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + N(x) + "|" + N(y) + "|" + kit.ID;

        /// <summary>True for a camp kit the player set down and still holds (installed, origin Player), as opposed to a fixture in the world.</summary>
        public static bool IsPlayerHeld(Entity e)
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
            if (command.StartsWith(MakeCampPrefix, StringComparison.Ordinal)) return MakeCamp(actor, fixture, zone, command, tx);
            if (command.StartsWith(PackUpPrefix, StringComparison.Ordinal)) return PackUp(actor, fixture, zone, command, tx);
            if (command.StartsWith(MovePrefix, StringComparison.Ordinal)) return Move(actor, fixture, part, zone, command, tx);
            if (command.StartsWith(NameCampPrefix, StringComparison.Ordinal)) return NameCamp(actor, fixture, zone, command, tx);
            return HandOver(actor, fixture, part, zone, command, tx);
        }

        static bool SetDown(Entity actor, Entity kit, PackableFixturePart part, Zone zone, string command, InventoryTransaction tx, int reach = 1)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, kit, command, "actor-unavailable");
            if (f.Length != 7 || !Int(f[2], out int ox) || !Int(f[3], out int oy) || !Int(f[4], out int x) || !Int(f[5], out int y)) return Reject(actor, kit, command, "malformed-selection");
            var origin = zone.GetEntityCell(actor); var pack = actor.GetPart<InventoryPart>();
            if (Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[6] != kit.ID || origin == null || origin.X != ox || origin.Y != oy
                || pack == null || !pack.Objects.Contains(kit)) return Reject(actor, kit, command, "stale-selection");
            if (Math.Abs((long)x - origin.X) > reach || Math.Abs((long)y - origin.Y) > reach || (x == origin.X && y == origin.Y)) return Reject(actor, kit, command, PlacementCheck.CellOccupied);
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

        static bool TakeUp(Entity actor, Entity fixture, PackableFixturePart part, Zone zone, string command, InventoryTransaction tx, int reach = 1)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, fixture, command, "actor-unavailable");
            var cell = zone.GetEntityCell(fixture); var origin = zone.GetEntityCell(actor); var pack = actor.GetPart<InventoryPart>();
            if (f.Length != 3 || Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[2] != fixture.ID || cell == null || origin == null || pack == null
                || fixture.SpatialZone != zone || !IsPlayerHeld(fixture) || Math.Abs(cell.X - origin.X) > reach || Math.Abs(cell.Y - origin.Y) > reach) return Reject(actor, fixture, command, "stale-selection");
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
            // Taken up, the kit leaves its camp, so it leaves the camp's name behind (a move puts it back).
            string campName = part.CampName ?? ""; tx.Do(() => part.CampName = "", () => part.CampName = campName);
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

        // ── composites: every one is a single transaction, so any refusal anywhere gives everything back ──

        /// <summary>
        /// Lay out every carried kit in one action: the fire first, then the still within its reach, the bench, and the box not beside the fire. Each kit takes the first legal cell of its
        /// preference order (checked against what is already standing, including the kits just set down), kits with no legal cell stay in the pack, and any set-down that is refused
        /// after being chosen fails the whole command.
        /// </summary>
        static bool MakeCamp(Entity actor, Entity trigger, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, trigger, command, "actor-unavailable");
            var origin = zone.GetEntityCell(actor);
            if (f.Length != 5 || !Int(f[2], out int ox) || !Int(f[3], out int oy) || Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[4] != trigger.ID || origin == null || origin.X != ox || origin.Y != oy)
                return Reject(actor, trigger, command, "stale-selection");
            var kits = CarriedKits(actor); if (!kits.Contains(trigger) || kits.Count < 2) return Reject(actor, trigger, command, "stale-selection");
            int hearthX = int.MinValue, hearthY = int.MinValue; int placed = 0; var names = new List<string>(); string firstRefusal = null;
            foreach (var near in HeldWithin(zone, origin, PackUpReach))
                if (near.GetPart<PackableFixturePart>().Kind == CampKinds.Hearth) { var at = zone.GetEntityCell(near); hearthX = at.X; hearthY = at.Y; break; }
            foreach (var kit in kits)
            {
                var part = kit.GetPart<PackableFixturePart>(); var def = PackableFixtureRegistry.Get(part.Kind); var batch = new PlacementCheck.PlacementBatch(WorldLocationContext.For(zone), zone);
                int bx = int.MinValue, by = int.MinValue; bool preferred = false;
                foreach (var d in Directions)
                {
                    int x = origin.X + d.x, y = origin.Y + d.y;
                    var verdict = batch.Evaluate(x, y, def.Spec, kit);
                    if (!verdict.Ok) { firstRefusal ??= verdict.Reasons.FirstOrDefault(); continue; }
                    bool known = hearthX != int.MinValue; int dist = known ? Math.Max(Math.Abs(x - hearthX), Math.Abs(y - hearthY)) : 0;
                    bool wants = part.Kind == CampKinds.Still ? !known || dist <= LiquidSettlingPart.FireReach : part.Kind == CampKinds.Box ? !known || dist >= 2 : true;
                    if (bx == int.MinValue || (wants && !preferred)) { bx = x; by = y; preferred = wants; }
                    if (wants) break;
                }
                if (bx == int.MinValue) continue;   // no legal cell for this kit: it stays in the pack
                if (!SetDown(actor, kit, part, zone, SetDownCommand(zone, origin, bx, by, kit), tx)) return false;
                if (part.Kind == CampKinds.Hearth) { hearthX = bx; hearthY = by; }
                placed++; names.Add(kit.GetDisplayName());
            }
            if (placed == 0) return Reject(actor, trigger, command, firstRefusal ?? PlacementCheck.OpenNeighbours);
            int total = placed, carried = kits.Count;
            tx.AfterCommit(() => MessageLog.Add(total == carried ? "You make camp." : "You make camp; " + (carried - total) + " could not be set down here."));
            return true;
        }

        /// <summary>Take up everything the player holds within <see cref="PackUpReach"/> in one action. All or nothing: if the whole camp cannot be carried, nothing is taken.</summary>
        static bool PackUp(Entity actor, Entity trigger, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, trigger, command, "actor-unavailable");
            var origin = zone.GetEntityCell(actor); var cell = zone.GetEntityCell(trigger);
            if (f.Length != 3 || Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[2] != trigger.ID || origin == null || cell == null || !IsPlayerHeld(trigger)
                || Math.Abs(cell.X - origin.X) > 1 || Math.Abs(cell.Y - origin.Y) > 1) return Reject(actor, trigger, command, "stale-selection");
            var camp = HeldWithin(zone, origin, PackUpReach); if (camp.Count < 2) return Reject(actor, trigger, command, "stale-selection");
            foreach (var fixture in camp)
                if (!TakeUp(actor, fixture, fixture.GetPart<PackableFixturePart>(), zone, TakeUpPrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + fixture.ID, tx, PackUpReach)) return false;
            int n = camp.Count; tx.AfterCommit(() => MessageLog.Add("You pack up camp (" + n + " things)."));
            return true;
        }

        /// <summary>Move one standing fixture one cell: the same take-up and set-down as the ordinary verbs, in one action, so it keeps its ID, contents, fuel and escrow.</summary>
        static bool Move(Entity actor, Entity fixture, PackableFixturePart part, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, fixture, command, "actor-unavailable");
            var origin = zone.GetEntityCell(actor); var cell = zone.GetEntityCell(fixture);
            if (f.Length != 5 || !Int(f[3], out int x) || !Int(f[4], out int y) || Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[2] != fixture.ID || origin == null || cell == null || !IsPlayerHeld(fixture)
                || Math.Abs((long)x - origin.X) > MoveReach || Math.Abs((long)y - origin.Y) > MoveReach || (x == cell.X && y == cell.Y)) return Reject(actor, fixture, command, "stale-selection");
            string name = part.CampName ?? "";
            if (!TakeUp(actor, fixture, part, zone, TakeUpPrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + fixture.ID, tx)) return false;
            if (!SetDown(actor, fixture, part, zone, SetDownCommand(zone, origin, x, y, fixture), tx, MoveReach)) return false;
            // A moved kit is still part of its camp: the name that taking it up cleared comes back with it.
            tx.Do(() => part.CampName = name, () => part.CampName = "");
            return true;
        }

        /// <summary>Gives the whole camp (every held fixture within <see cref="HeldFixtureReader.ClusterRadius"/>, single-link) the next name in the list, or clears it after the last. Free, and undone with the rest of the transaction.</summary>
        static bool NameCamp(Entity actor, Entity trigger, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, trigger, command, "actor-unavailable");
            var origin = zone.GetEntityCell(actor); var cell = zone.GetEntityCell(trigger);
            if (f.Length != 3 || Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[2] != trigger.ID || origin == null || cell == null || !IsPlayerHeld(trigger)
                || Math.Abs(cell.X - origin.X) > 1 || Math.Abs(cell.Y - origin.Y) > 1) return Reject(actor, trigger, command, "stale-selection");
            var camp = HeldFixtureReader.ClusterOf(zone, trigger); string name = HeldFixtureReader.NextCampName(HeldFixtureReader.CampNameOf(camp));
            var parts = new List<PackableFixturePart>(); var before = new List<string>();
            foreach (var fixture in camp) { var p = fixture.GetPart<PackableFixturePart>(); if (p == null) continue; parts.Add(p); before.Add(p.CampName ?? ""); }
            tx.Do(() => { foreach (var p in parts) p.CampName = name; }, () => { for (int i = 0; i < parts.Count; i++) parts[i].CampName = before[i]; });
            tx.AfterCommit(() => MessageLog.Add(name == "" ? "This camp has no name now." : "You call this camp " + name + "."));
            return true;
        }

        // ── helpers ──

        static int Contents(Entity fixture) => fixture.GetPart<ContainerPart>()?.Contents.Count ?? 0;
        static bool Int(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        static string N(int value) => value.ToString(CultureInfo.InvariantCulture);

        static bool Reject(Entity actor, Entity fixture, string command, string reason)
        {
            Diag.Record("furniture", "FixtureRefused", actor, fixture, new { command = (command ?? "").Split('|')[0], reason });
            MessageLog.Add(Reason(reason, PackableFixtureRegistry.Get(fixture?.GetPart<PackableFixturePart>()?.Kind)));
            return false;
        }
    }
}
