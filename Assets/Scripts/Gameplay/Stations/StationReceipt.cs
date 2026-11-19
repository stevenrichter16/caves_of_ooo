using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A deliberate workshop operation retains one physical station through callbacks (the generalised <c>ForgeWorkReceipt.Begin</c>): it cannot switch to another
    /// nearby station at commit. Access is asked before any claim; a refusal records <c>furniture/StationRefused</c>.</summary>
    public sealed class StationReceipt
    {
        public Entity Station { get; }
        StationReceipt(Entity station) { Station = station; }

        public static StationReceipt Begin(Entity actor, Zone zone, InventoryTransaction tx, string action, string kind, Entity selected = null)
        {
            var station = selected ?? StationQuery.Resolve(actor, zone, kind);
            if (station != null && !StationAccess.Allowed(actor, station, zone, kind, action, out string denyCode, out _)) return Refuse(actor, station, kind, action, denyCode ?? "access");
            if (tx == null || !StationQuery.Near(actor, station, zone, kind) || !tx.TryClaim(actor, actor, action) || !tx.TryClaim(station, actor, action))
                return Refuse(actor, station, kind, action, station == null ? "no-station" : "not-near-or-claimed");
            var actorCell = zone.GetEntityCell(actor); var stationCell = zone.GetEntityCell(station);
            string id = station.ID, blueprint = station.BlueprintName; var parts = station.Parts.ToArray();
            tx.BeforeCommit(() => station.ID == id && station.BlueprintName == blueprint && station.Parts.SequenceEqual(parts)
                && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(station) == stationCell
                && StationQuery.Near(actor, station, zone, kind) && !CombatSystem.IsDeathHandled(actor)
                && (actor.GetStat("Hitpoints") == null || actor.GetStatValue("Hitpoints") > 0)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true);
            return new StationReceipt(station);
        }

        static StationReceipt Refuse(Entity actor, Entity station, string kind, string action, string code)
        {
            if (Diag.IsChannelEnabled("furniture")) Diag.Record("furniture", "StationRefused", actor: actor, target: station, payload: new { code, kind, command = action });
            return null;
        }
    }
}
