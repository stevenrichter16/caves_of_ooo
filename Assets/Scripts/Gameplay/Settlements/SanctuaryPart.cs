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

        /// <summary>BIOME-OVERHAUL A4 — donation cost in drams.</summary>
        public const int DonationCost = 5;

        /// <summary>Blessing: minor stoneskin (damage −1 per hit).</summary>
        public const int BlessingReduction = 1;
        public const int BlessingDuration = 150;

        // BIOME-OVERHAUL A4: shrines accept donations — 5 drams for a
        // 150-turn minor stoneskin. The flee-waypoint marker contract
        // above is untouched (AIFleeToShrinePart only checks for the
        // part's presence).
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                actions?.AddAction("Donate", "donate", "DonateAtShrine", 'o', 20);
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                if (e.GetStringParameter("Command") != "DonateAtShrine") return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                int drams = actor.GetIntProperty(TradeSystem.CURRENCY_PROP, 0);
                if (drams < DonationCost)
                {
                    MessageLog.Add($"The offering bowl expects {DonationCost} drams you don't have.");
                }
                else
                {
                    actor.SetIntProperty(TradeSystem.CURRENCY_PROP, drams - DonationCost);
                    var effects = actor.GetPart<StatusEffectsPart>();
                    effects?.ForceApplyEffect(new StoneskinEffect(BlessingReduction, BlessingDuration));
                    MessageLog.Add("You leave a small offering. A quiet blessing settles over you.");
                    if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("furniture"))
                    {
                        CavesOfOoo.Diagnostics.Diag.Record(
                            category: "furniture", kind: "Blessed",
                            actor: actor,
                            payload: new { cost = DonationCost, duration = BlessingDuration });
                    }
                }
                e.Handled = true;
                return false;
            }
            return true;
        }
    }
}
