using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Binds a fixed light to a settlement condition in its own world.</summary>
    public sealed class PlaceConditionLightPart : Part, IDescribeState, IMenuStatus, IInventoryActionReceiptOwner
    {
        const string WickPrefix = "LightTorchAtWick|";
        const string WickTag = "AccessibleTorchWick";
        public override string Name => "PlaceConditionLight";

        public string SettlementId = "";
        public string Condition = "";
        public int SiteX = -1;
        public int SiteY = -1;
        public bool LightsAuthoredGround;

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 30;

        internal bool IsInstalled(Zone zone) => InstalledLight(zone, out _) != null;

        internal static SettlementManager SettlementAt(Zone zone)
        {
            var world = WorldLocationContext.For(zone);
            return world != null && world.CachedZones.TryGetValue(zone.ZoneID, out var cached) && cached == zone
                ? world.SettlementManager : null;
        }

        public string DescribeState(DescribeContext context)
        {
            if (!TryReadState(context.Zone ?? ParentEntity?.SpatialZone, out bool lit)) return null;
            return lit ? "The lamp is lit." : "The lamp is out.";
        }

        public void AppendMenuStatus(DescribeContext context, List<string> lines)
        {
            var state = DescribeState(context);
            if (state != null) lines.Add(state);
        }

        internal bool TryReadState(Zone zone, out bool lit)
        {
            var light = InstalledLight(zone, out var settlement);
            lit = light is { Enabled: true } && settlement.HasExistingCondition(SettlementId, Condition);
            return light != null;
        }

        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<PlaceConditionLightPart>() == this
            && command?.StartsWith(WickPrefix, StringComparison.Ordinal) == true;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "GetInventoryActions" && e.ID != "InventoryAction") return true;
            var actor = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            if (e.ID == "GetInventoryActions")
            {
                AddWickActions(actor, zone, e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            var command = e.GetStringParameter("Command");
            if (!OwnsInventoryReceipt(command)
                || !TryUseWick(actor, zone, command, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true;
            return false;
        }

        bool WickAvailable(Entity actor, Zone zone)
        {
            var owner = ParentEntity;
            if (!WorldResourceActions.Nearby(actor, owner, zone) || !RouteWorkMenu.Visible(zone.GetEntityCell(owner))
                || owner.GetPart<RenderPart>()?.Visible != true
                || owner.GetPart<DestructiblePart>() is { Gone: true } or { HP: <= 0 }
                || !TryReadState(zone, out bool lit) || !lit) return false;
            // The exact installed object comes from the save. Read the added
            // capability from its current declaration so existing saves qualify.
            var factory = WorldLocationContext.For(zone)?.Factory;
            return factory?.Blueprints != null && factory.Blueprints.TryGetValue(owner.BlueprintName, out var definition)
                && definition.Tags.ContainsKey(WickTag);
        }

        static IEnumerable<Entity> NearbyTorches(Entity actor, Zone zone)
        {
            var equipped = actor.GetPart<InventoryPart>()?.EquippedItems.Values.AsEnumerable() ?? Enumerable.Empty<Entity>();
            return equipped.Concat(zone.GetReadOnlyEntitiesWithPart<TorchLightPart>()).Where(torch => torch != null).Distinct();
        }

        void AddWickActions(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !WickAvailable(actor, zone)) return;
            var origin = zone.GetEntityCell(actor);
            var cell = zone.GetEntityCell(ParentEntity);
            var pack = actor.GetPart<InventoryPart>();
            var labels = new HashSet<string>();
            foreach (var torch in NearbyTorches(actor, zone).OrderBy(item => item.ID, StringComparer.Ordinal))
            {
                if (!PortableKindling.CanLightFromWick(actor, torch, zone)) continue;
                string command = FireStarting.CellCommand(WickPrefix, zone, origin, cell.X, cell.Y, torch)
                    + "|" + Uri.EscapeDataString(ParentEntity.ID);
                var torchCell = zone.GetEntityCell(torch);
                string location = torchCell != null ? $"at ({torchCell.X}, {torchCell.Y})"
                    : $"in {pack?.FindEquippedBodyPart(torch)?.GetDisplayName() ?? "hand"}";
                string label = $"light torch {location} from the lamp ({torch.GetPart<FuelPart>().FuelMass:0.#} fuel)";
                if (!labels.Add(label)) continue;
                actions.AddActionWithHint("LightTorchAtWick", label, command,
                    "One action. Your torch burns its own fuel. The lamp keeps burning.", '\0', 25, false, ActionTime.OneAction);
            }
        }

        bool TryUseWick(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var owner = ParentEntity;
            if (tx == null || !WickAvailable(actor, zone)) return false;
            string ownerId = owner.ID;
            string condition = Condition;
            var light = owner.GetPart<LightSourcePart>();
            int split = command.LastIndexOf('|');
            if (WorldResourceActions.Decode(command.Substring(split + 1)) != ownerId
                || !FireStarting.ParseCell(command.Substring(0, split), out string zoneId, out int ox, out int oy,
                    out int x, out int y, out string torchId) || zone.ZoneID != zoneId) return false;
            var origin = zone.GetCell(ox, oy);
            var cell = zone.GetCell(x, y);
            bool SameWick() => zone.GetEntityCell(actor) == origin && zone.GetEntityCell(owner) == cell
                && WorldResourceActions.ExactGround(zone, Uri.EscapeDataString(ownerId)) == owner
                && owner.GetPart<LightSourcePart>() == light && Condition == condition
                && ParentEntity == owner && WickAvailable(actor, zone);
            if (!SameWick()) return false;
            var matched = NearbyTorches(actor, zone).Where(item => item.ID == torchId).Take(2).ToArray();
            return matched.Length == 1 && PortableKindling.LightFromWick(actor, matched[0], zone, owner, command, tx, SameWick);
        }

        private LightSourcePart InstalledLight(Zone zone, out SettlementManager settlement)
        {
            settlement = null;
            var owner = ParentEntity;
            if (zone == null || owner == null || string.IsNullOrWhiteSpace(SettlementId)
                || zone.ZoneID != SettlementId || string.IsNullOrWhiteSpace(Condition)
                || owner.GetPart<PlaceConditionLightPart>() != this
                || owner.Parts.Count(part => part is PlaceConditionLightPart) != 1
                || owner.Parts.Count(part => part is PhysicsPart) != 1
                || owner.HasTag("Creature") || owner.HasPart<DraggedPart>()
                || !DragRules.IsRooted(owner) || !WorldResourceActions.Ground(owner, zone)) return null;

            var cell = zone.GetCell(SiteX, SiteY);
            var light = owner.GetPart<LightSourcePart>();
            if (cell == null || zone.GetEntityCell(owner) != cell || cell.Objects.Count(item => item == owner) != 1
                || light?.ParentEntity != owner
                || light.Radius is <= 0 or > Zone.Width || light.Intensity <= 0 || light.Intensity > 1
                || !WorldResourceActions.Finite(light.Intensity)
                || owner.Parts.Count(part => part is LightSourcePart) != 1) return null;

            settlement = SettlementAt(zone);
            return settlement != null ? light : null;
        }
    }
}
