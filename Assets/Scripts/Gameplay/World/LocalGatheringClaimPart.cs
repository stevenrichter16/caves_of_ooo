using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    public enum ReserveAccessState { Unknown, Granted, Suspended }

    /// <summary>One saved player's local relationship. Entity references, not
    /// display names or matching IDs in another world, own the permission.</summary>
    public sealed class ReserveAccessRecord
    {
        public Entity Player;
        public ReserveAccessState State;
        public int BreachOrdinal;
        public string LastBreach;
    }

    /// <summary>The seed keeper's two additional beds and tray. This is an
    /// opt-in local agreement, not faction standing or universal ownership.</summary>
    public sealed class LocalGatheringClaimPart : Part
    {
        public override string Name => "LocalGatheringClaim";
        public const string ReserveZoneID = "Overworld.11.8.0";
        public const string TermsCommand = "ReserveAccess:terms", PayCommand = "ReserveAccess:pay",
            IntroductionCommand = "ReserveAccess:introduction", ReconcileCommand = "ReserveAccess:reconcile";
        public const int AccessPrice = 8, RestitutionGrain = 2;
        public bool Configured;
        public int WorldSeed, FirstX, FirstY, SecondX, SecondY, TrayX, TrayY;
        public string WorldKey, ZoneID, KeeperID, FirstSoilID, SecondSoilID, TrayID;
        public Entity FirstSoil, SecondSoil, Tray;
        public List<ReserveAccessRecord> Permissions = new List<ReserveAccessRecord>();

        /// <summary>Generation supplies the frozen world's key before Configure
        /// when its staged zone has not yet been attached to the manager.</summary>
        public bool BindWorldKey(string key)
        {
            if (string.IsNullOrEmpty(key) || Configured && WorldKey != key) return false;
            WorldKey = key; return true;
        }

        public bool Configure(Zone zone, Entity firstSoil, Entity secondSoil, Entity tray, int worldSeed)
        {
            if (Configured || zone?.ZoneID != ReserveZoneID || ParentEntity?.GetPart<LocalGatheringClaimPart>() != this
                || !LocalGatheringClaims.Ground(ParentEntity, zone, "SpreadSeedKeeper")
                || !Soil(firstSoil, zone) || !Soil(secondSoil, zone)
                || !LocalGatheringClaims.Ground(tray, zone, "ConnectedReserveTray") || tray.GetPart<ContainerPart>()?.ParentEntity != tray
                || new[] { ParentEntity.ID, firstSoil.ID, secondSoil.ID, tray.ID }.Distinct().Count() != 4) return false;
            string key = WorldKey ?? LocalGatheringClaims.WorldKeyFor(zone);
            if (string.IsNullOrEmpty(key)) return false;
            WorldKey = key; WorldSeed = worldSeed; ZoneID = zone.ZoneID; KeeperID = ParentEntity.ID;
            FirstSoil = firstSoil; SecondSoil = secondSoil; Tray = tray;
            FirstSoilID = firstSoil.ID; SecondSoilID = secondSoil.ID; TrayID = tray.ID;
            var at = zone.GetEntityPosition(firstSoil); FirstX = at.x; FirstY = at.y;
            at = zone.GetEntityPosition(secondSoil); SecondX = at.x; SecondY = at.y;
            at = zone.GetEntityPosition(tray); TrayX = at.x; TrayY = at.y;
            Configured = true; return true;
        }

        static bool Soil(Entity owner, Zone zone) => LocalGatheringClaims.Ground(owner, zone)
            && owner.HasTag("Terrain") && owner.GetPart<CultivatedSoilPart>()?.ParentEntity == owner;
        internal bool Bound(Zone zone)
        {
            var manager = WorldLocationContext.For(zone);
            return Configured && ZoneID == ReserveZoneID && zone?.ZoneID == ZoneID
                && ParentEntity?.GetPart<LocalGatheringClaimPart>() == this && ParentEntity.ID == KeeperID
                && LocalGatheringClaims.Ground(ParentEntity, zone, "SpreadSeedKeeper")
                && !string.IsNullOrEmpty(WorldKey) && WorldKey == LocalGatheringClaims.WorldKeyFor(zone)
                && manager != null && manager.WorldSeed == WorldSeed
                && manager.CachedZones.TryGetValue(ZoneID, out var current) && current == zone
                && FirstSoil?.ID == FirstSoilID && SecondSoil?.ID == SecondSoilID && FirstSoil != SecondSoil
                && Soil(FirstSoil, zone) && Soil(SecondSoil, zone)
                && zone.GetEntityPosition(FirstSoil) == (FirstX, FirstY) && zone.GetEntityPosition(SecondSoil) == (SecondX, SecondY)
                && Tray?.ID == TrayID && LocalGatheringClaims.Ground(Tray, zone, "ConnectedReserveTray")
                && zone.GetEntityPosition(Tray) == (TrayX, TrayY) && Tray.GetPart<ContainerPart>()?.ParentEntity == Tray
                && Permissions != null && Permissions.Count <= 64 && Permissions.All(p => p != null && p.Player != null
                    && p.State >= ReserveAccessState.Unknown && p.State <= ReserveAccessState.Suspended && p.BreachOrdinal >= 0)
                && Permissions.Select(p => p.Player).Distinct().Count() == Permissions.Count;
        }
        internal bool OwnsBed(Zone zone, Cell cell) => Bound(zone) && cell != null && cell.ParentZone == zone
            && (zone.GetEntityCell(FirstSoil) == cell || zone.GetEntityCell(SecondSoil) == cell);
        public ReserveAccessState GetState(Entity actor) => Permissions?.FirstOrDefault(p => p?.Player == actor)?.State ?? ReserveAccessState.Unknown;

        bool Context(Entity actor, Zone zone) => Bound(zone) && ReferenceEquals(SettlementRuntime.ActiveZone, zone)
            && LocalGatheringClaims.PlayerAvailable(actor, zone) && LocalGatheringClaims.Alive(ParentEntity)
            && LocalGatheringClaims.Friendly(ParentEntity, actor) && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1;
        bool CanAct(Entity actor, Zone zone, string action)
        {
            if (!Context(actor, zone)) return false;
            var state = GetState(actor);
            if (action == "terms") return true;
            if (action == "pay") return state == ReserveAccessState.Unknown && TradeSystem.GetDrams(actor) >= AccessPrice
                && (long)TradeSystem.GetDrams(ParentEntity) + AccessPrice <= int.MaxValue;
            if (action == "introduction") return state == ReserveAccessState.Unknown && CookIntroductionKnowledgePart.ValidFor(actor, WorldKey);
            if (action == "reconcile") return state == ReserveAccessState.Suspended && Grain(actor).Sum(p => p.count) == RestitutionGrain;
            return false;
        }
        public static bool IsCommand(string command) => command == TermsCommand || command == PayCommand || command == IntroductionCommand || command == ReconcileCommand;
        public static bool IsReadOnlyCommand(string command) => command == TermsCommand;
        public bool TryAct(Entity actor, Zone zone, string action)
        {
            if (!CanAct(actor, zone, action)) return Reject(actor, "unavailable");
            return InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ParentEntity, "ReserveAccess:" + action), actor, zone).Success;
        }
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (CanAct(actor, zone, "terms")) actions?.AddAction("ReserveTerms", "ask about the tied row", TermsCommand, 'q', 21);
                if (CanAct(actor, zone, "pay")) actions?.AddAction("ReservePay", "arrange gathering rights (8 drams)", PayCommand, 'p', 21);
                if (CanAct(actor, zone, "introduction")) actions?.AddAction("ReserveIntroduction", "relay Orven's introduction", IntroductionCommand, 'i', 21);
                if (CanAct(actor, zone, "reconcile")) actions?.AddAction("ReserveReconcile", "make amends (2 emberwheat)", ReconcileCommand, 'r', 21);
                return true;
            }
            string command = e.GetStringParameter("Command");
            if (e.ID != "InventoryAction" || !IsCommand(command)) return true;
            string action = command.Substring("ReserveAccess:".Length);
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (tx == null || !CanAct(actor, zone, action)) { Reject(actor, "unavailable"); return true; }
            if (action == "terms")
                tx.AfterCommit(() => MessageLog.Add(GetState(actor) == ReserveAccessState.Suspended
                    ? "Nella remembers what she saw. Two emberwheat will make amends for this breach; an old introduction will not. The open beds remain public."
                    : "Nella: The two tied beds and this tray are our seed reserve. Eight drams, or Orven's introduction, gives you gathering rights. The four open beds are already for anyone."));
            else
            {
                if (!tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(actor, actor, command)) return true;
                if (action == "reconcile" && !TransferGrain(actor, zone, tx)) return true;
                if (!Context(actor, zone)) return true;
                if (action == "pay") tx.DeferCurrencyTransfer(actor, ParentEntity, AccessPrice);
                var prior = Permissions.FirstOrDefault(p => p.Player == actor);
                var next = new ReserveAccessRecord { Player = actor, State = ReserveAccessState.Granted,
                    BreachOrdinal = prior?.BreachOrdinal ?? 0, LastBreach = prior?.LastBreach };
                if (prior == null)
                    tx.Do(() => Permissions.Add(next), () => Permissions.Remove(next));
                else
                {
                    int index = Permissions.IndexOf(prior);
                    tx.Do(() => Permissions[index] = next, () => { int i = Permissions.IndexOf(next); if (i >= 0) Permissions[i] = prior; });
                }
                tx.AfterCommit(() =>
                {
                    MessageLog.Add(action == "reconcile" ? "Nella accepts the grain. You may gather from the tied row again." : "Nella agrees: the tied row and its tray are open to you. Leave the beds fit to plant again.");
                    Diag.Record("event", "ReserveAccessGranted", actor, ParentEntity, new { action, WorldKey, ZoneID });
                });
            }
            e.Handled = true; return false;
        }

        List<(Entity item, int count)> Grain(Entity actor)
        {
            var selected = new List<(Entity, int)>(); int left = RestitutionGrain; var pack = actor?.GetPart<InventoryPart>();
            if (pack == null) return selected;
            foreach (var item in pack.Objects)
            {
                if (item?.BlueprintName != "Emberwheat" || !LocalGatheringClaims.Carried(item, actor, pack)) continue;
                int count = Math.Min(left, item.GetPart<StackerPart>()?.StackCount ?? 1);
                selected.Add((item, count)); left -= count; if (left == 0) break;
            }
            return selected;
        }
        bool TransferGrain(Entity actor, Zone zone, InventoryTransaction tx)
        {
            var pack = actor.GetPart<InventoryPart>(); var stock = ParentEntity.GetPart<InventoryPart>(); var selected = Grain(actor);
            if (stock?.ParentEntity != ParentEntity || selected.Sum(p => p.count) != RestitutionGrain) return false;
            foreach (var item in stock.Objects) if (!tx.TryClaim(item, actor, ReconcileCommand)) return false;
            foreach (var entry in selected)
            {
                if (!tx.TryClaim(entry.item, actor, ReconcileCommand)) return false;
                Entity moved = null; var source = InventoryTransferSnapshot.Capture(pack, entry.item); tx.Do(null, source.Restore);
                if (!source.Apply(() =>
                {
                    if (!LocalGatheringClaims.Carried(entry.item, actor, pack)) return false;
                    int count = entry.item.GetPart<StackerPart>()?.StackCount ?? 1;
                    if (count < entry.count) return false;
                    if (count == entry.count) { moved = entry.item; return pack.RemoveObject(moved); }
                    moved = entry.item.GetPart<StackerPart>().SplitStack(entry.count);
                    if (moved?.GetPart<PhysicsPart>() is not PhysicsPart physics) return false;
                    physics.InInventory = null; physics.Equipped = null; return true;
                }) || moved == null || moved.BlueprintName != "Emberwheat" || moved.SpatialZone != null
                    || moved.GetPart<PhysicsPart>()?.InInventory != null || (moved.GetPart<StackerPart>()?.StackCount ?? 1) != entry.count
                    || !tx.TryClaim(moved, actor, ReconcileCommand)) return false;
                var destination = InventoryTransferSnapshot.Capture(stock, moved); tx.Do(null, destination.Restore);
                if (!destination.Apply(() => stock.AddObject(moved)) || !destination.ClaimChanges(tx, actor, ReconcileCommand)) return false;
            }
            return Context(actor, zone) && actor.GetPart<InventoryPart>() == pack && ParentEntity.GetPart<InventoryPart>() == stock;
        }

        internal bool Witness(Entity actor, Zone zone, Cell source)
        {
            if (!Bound(zone) || GetState(actor) == ReserveAccessState.Granted || !LocalGatheringClaims.PlayerAvailable(actor, zone)
                || !LocalGatheringClaims.Alive(ParentEntity) || source?.ParentZone != zone) return false;
            var eye = zone.GetEntityCell(ParentEntity); var subject = zone.GetEntityCell(actor); var brain = ParentEntity.GetPart<BrainPart>();
            return eye != null && subject != null && brain?.ParentEntity == ParentEntity && brain.SightRadius >= 0
                && AIHelpers.ChebyshevDistance(eye.X, eye.Y, subject.X, subject.Y) <= brain.SightRadius
                && AIHelpers.ChebyshevDistance(eye.X, eye.Y, source.X, source.Y) <= brain.SightRadius
                && AIHelpers.HasLineOfSight(zone, eye.X, eye.Y, subject.X, subject.Y)
                && AIHelpers.HasLineOfSight(zone, eye.X, eye.Y, source.X, source.Y);
        }
        internal bool RecordBreach(Entity actor, Zone zone, Cell source, string reason)
        {
            if (!Witness(actor, zone, source)) return false;
            var record = Permissions.FirstOrDefault(p => p.Player == actor);
            if (record?.State == ReserveAccessState.Suspended) return true;
            if (record == null) { record = new ReserveAccessRecord { Player = actor }; Permissions.Add(record); }
            if (record.BreachOrdinal == int.MaxValue) return false;
            record.BreachOrdinal++; record.State = ReserveAccessState.Suspended; record.LastBreach = reason;
            MessageLog.Add("Nella saw you take from the tied reserve without leave. Gathering rights here are suspended; the open beds remain public.");
            Diag.Record("event", "ReserveBreachWitnessed", actor, ParentEntity, new { reason, record.BreachOrdinal, WorldKey, ZoneID });
            return true;
        }
        bool Reject(Entity actor, string reason)
        {
            if (actor?.HasTag("Player") == true) MessageLog.Add("That arrangement is not available here. Ask Nella about the tied row.");
            Diag.Record("event", "ReserveAccessRejected", actor, ParentEntity, new { reason }); return false;
        }
    }
}
