namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marker part: the wearer is a sanctuary (shrine, altar, holy site).
    /// Wounded Passive NPCs carrying <see cref="AIFleeToShrinePart"/> scan
    /// the zone for entities with this part and treat the nearest one as
    /// their flee waypoint.
    ///
    /// Pure marker — no behavior of its own. The "HealOverTime" polish
    /// feature in the M3.3 plan is deferred; when implemented it'll add
    /// a field here plus a tick handler that regenerates HP for adjacent
    /// allied creatures. Today a shrine just provides a destination; it
    /// doesn't heal on arrival.
    ///
    /// Blueprint attachment:
    ///   { "Name": "Sanctuary", "Params": [] }
    ///
    /// Intended wearers: Shrine blueprint (M3.3), plus future altar /
    /// hearth blueprints as settlement content grows.
    /// </summary>
    public class SanctuaryPart : Part
    {
        public override string Name => "Sanctuary";
    }
}
