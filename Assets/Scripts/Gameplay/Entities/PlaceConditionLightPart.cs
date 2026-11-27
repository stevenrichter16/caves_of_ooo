using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>Binds a fixed light to a settlement condition in its own world.</summary>
    public sealed class PlaceConditionLightPart : Part, IDescribeState, IMenuStatus
    {
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
