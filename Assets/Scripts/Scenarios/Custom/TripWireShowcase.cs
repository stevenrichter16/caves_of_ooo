using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// TripWire showcase — demonstrates the multi-segment line trap.
    /// Each wire is N entity-segments sharing a WireGroupId. Stepping
    /// on ANY segment detonates the WHOLE wire, damaging actors at every
    /// segment cell + removing all segments in one event.
    ///
    /// Setup (player at left edge):
    ///
    ///                 . . . . [Snapjaw3] . . . . .
    ///                 .                           .
    ///   [Player] . [Wire1A] [Wire1B] [Wire1C] . [Wire2A] [Wire2B] .
    ///                 .                           .
    ///                 . . . . [Snapjaw1] . . . [Snapjaw2] . . . .
    ///
    /// Wire 1 (group "wire-1"): 3 horizontal segments at p.y. Stepping
    /// on any of the three detonates all three; if Snapjaw1 is at the
    /// southmost segment's cell, the snapjaw also takes damage even if
    /// the player steps on a different segment cell (proves the
    /// LINE-vs-AOE contract).
    ///
    /// Wire 2 (group "wire-2"): 2 segments further east, separate group.
    /// Stepping on wire-1 does NOT affect wire-2 — proves group-id
    /// isolation. Player can step into wire-2 after wire-1 is consumed
    /// to see a second independent detonation.
    ///
    /// Wire-3 (group "wire-3"): single-segment wire (degenerate case)
    /// to show that a content author who forgets to put N>1 segments
    /// in a group still ships a working 1-cell trap.
    ///
    /// What the player should observe in the message log:
    ///
    ///   --- Step east onto Wire1A (or B, or C) ---
    ///   "The tripwire snaps taut!"
    ///   <player takes Damage>      (player at the stepped-on cell)
    ///   <Snapjaw1 takes Damage>    (placed at one of the OTHER
    ///                               segments' cells via the scenario;
    ///                               proves multi-cell line strike)
    ///   <all 3 wire-1 segments removed from the zone>
    ///
    ///   --- Continue east into Wire2A ---
    ///   "The tripwire snaps taut!"  (independent detonation)
    ///
    /// Player loadout: HP 200, Strength 24. Generous so the player
    /// can step into both wires.
    /// </summary>
    [Scenario(
        name: "TripWire Showcase",
        category: "Combat",
        description: "Multi-segment line trap demo. Wire-1 is 3 segments + a snapjaw planted at one segment cell — stepping on ANY of the 3 detonates all 3 and damages the snapjaw too (LINE vs AOE). Wire-2 is independent. Wire-3 is a degenerate 1-segment.")]
    public class TripWireShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Player loadout — beefy HP so the player survives walking
            // through both wires (each wire deals 10 damage at the
            // stepped-on cell, plus AOE-like collateral if the wire's
            // other segments have occupants).
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .GiveItem("HealingTonic", 5);

            // Clear the corridor — entities spawning on wire/snapjaw
            // cells would block placement.
            for (int dx = 1; dx <= 11; dx++)
            {
                ctx.World.ClearCell(p.x + dx, p.y);
                ctx.World.ClearCell(p.x + dx, p.y - 1);
                ctx.World.ClearCell(p.x + dx, p.y + 1);
            }

            // ---- Wire 1: 3 horizontal segments at (p.x+2,3,4, p.y) ----
            // Group id "wire-1". Stepping on any of these detonates all 3.
            SpawnWireSegment(ctx, p.x + 2, p.y, "wire-1");
            SpawnWireSegment(ctx, p.x + 3, p.y, "wire-1");
            SpawnWireSegment(ctx, p.x + 4, p.y, "wire-1");

            // Snapjaw planted at the FAR segment of wire-1 (p.x+4).
            // This is the LINE-vs-AOE pin: even if the player steps onto
            // the NEAR segment (p.x+2), this snapjaw at the FAR segment's
            // cell still takes damage because the wire detonates at every
            // segment's cell, not just the tripped one.
            // (We give the snapjaw a unique tag for the QA observation
            // path: "diag_query category=damage" should show damage
            // applied to TWO entities — player + this snapjaw — from a
            // single step event.)
            ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 100)
                .WithHpAbsolute(100)
                .At(p.x + 4, p.y);

            // ---- Wire 2: 2 horizontal segments at (p.x+7,8, p.y) ----
            // Group id "wire-2". Different group → unaffected by wire-1's
            // detonation.
            SpawnWireSegment(ctx, p.x + 7, p.y, "wire-2");
            SpawnWireSegment(ctx, p.x + 8, p.y, "wire-2");

            // ---- Wire 3: degenerate 1-segment wire (group "wire-3") ----
            // Demonstrates the "content author forgot to add siblings"
            // graceful degradation: a single segment with a group id
            // (or even an empty group id — see TripWireTriggerPart docs)
            // still works as a 1-cell trap, just damaging only the
            // stepper.
            SpawnWireSegment(ctx, p.x + 10, p.y, "wire-3");

            MessageLog.Add("TripWire Showcase: walk east. Three wires lie in wait.");
            MessageLog.Add("Wire-1 (cells x+2..x+4) — 3-segment LINE; snapjaw at x+4 takes damage too.");
            MessageLog.Add("Wire-2 (cells x+7,x+8) — 2-segment, independent group.");
            MessageLog.Add("Wire-3 (cell x+10) — degenerate 1-segment (content-author robustness).");
        }

        /// <summary>
        /// Spawn one tripwire segment at (x, y) with the given group id.
        /// We don't have a pre-built TripWire blueprint in Objects.json
        /// (the multi-segment topology is content-driven, not blueprint-
        /// driven), so we spawn a generic placeholder entity and add the
        /// TripWireTriggerPart directly. Each segment gets a "wire" tag
        /// for diag-query filterability.
        /// </summary>
        private static Entity SpawnWireSegment(
            ScenarioContext ctx, int x, int y, string groupId)
        {
            // PhysicalObject is the inheritable base used by SpikeTrap /
            // FireTrap / BearTrap blueprints. Spawning it directly gives
            // us the same Render/Physics infrastructure they use.
            var seg = ctx.Spawn("PhysicalObject").At(x, y);
            if (seg == null) return null;

            seg.GetPart<RenderPart>().DisplayName = "tripwire";
            seg.GetPart<RenderPart>().RenderString = "-";
            seg.GetPart<RenderPart>().ColorString = "&K";

            var physics = seg.GetPart<PhysicsPart>();
            if (physics != null) physics.Solid = false;

            seg.AddPart(new TripWireTriggerPart
            {
                WireGroupId = groupId,
                Damage = 10,
                DamageAttribute = "Piercing",
            });
            seg.SetTag("Trap", "");
            return seg;
        }
    }
}
