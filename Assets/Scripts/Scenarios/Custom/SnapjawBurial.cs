using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M5 manual-playtest showcase — end-to-end verification of the Corpse
    /// system. Spawns a Snapjaw, an Undertaker, and a Graveyard within a
    /// few cells of the player. When the player kills the Snapjaw, a
    /// <c>SnapjawCorpse</c> drops; the Undertaker's
    /// <see cref="AIUndertakerPart"/> fires on the next idle tick, claims
    /// the corpse, walks to it, picks it up, walks to the Graveyard, and
    /// deposits.
    ///
    /// <para><b>Expected flow:</b></para>
    /// <list type="bullet">
    ///   <item>Turn 0: Snapjaw at player+2 east. Undertaker at player+4 south. Graveyard at player+6 east.</item>
    ///   <item>Player kills Snapjaw. <c>%</c> glyph in red appears at Snapjaw's cell — the <c>SnapjawCorpse</c>.</item>
    ///   <item>Turn 1-2: Undertaker's bored-tick fires <see cref="AIBoredEvent"/>. AIUndertakerPart finds corpse + graveyard, claims the corpse, pushes <see cref="DisposeOfCorpseGoal"/>. Thought: "fetching corpse".</item>
    ///   <item>Turns 2-N: Undertaker walks to corpse, picks it up (corpse glyph disappears from ground), walks to graveyard. Thought switches to "hauling corpse" after pickup.</item>
    ///   <item>Terminal turn: Undertaker reaches cell adjacent to graveyard. Corpse transferred into the graveyard's ContainerPart. Goal pops. Terminal thought: "buried".</item>
    /// </list>
    ///
    /// <para><b>Good for</b></para>
    /// <list type="bullet">
    ///   <item>Visually confirming M5.1 corpse spawn on "Died" event fires at the right cell</item>
    ///   <item>Observing M5.2 two-phase state machine thoughts ("fetching corpse" → "hauling corpse" → "buried") via the Phase 10 goal-stack inspector (press 't')</item>
    ///   <item>Confirming M5.3 AIUndertakerPart consumes AIBoredEvent and pushes DisposeOfCorpseGoal</item>
    ///   <item>Smoke-testing the full Snapjaw → SnapjawCorpse → Undertaker → Graveyard pipeline end-to-end</item>
    /// </list>
    ///
    /// <para><b>Does not cover</b> (out of M5 scope):</para>
    /// <list type="bullet">
    ///   <item>Burnt/Vaporized variants (Qud-parity deferred to M9)</item>
    ///   <item>World-gen Graveyard placement (scenario-placed for now)</item>
    ///   <item>Butchering / necromancy on corpses (future phases)</item>
    /// </list>
    ///
    /// <para><b>Deterministic drop.</b> The Snapjaw blueprint ships with
    /// <c>CorpseChance=70</c> for gameplay feel (not every snapjaw leaves a
    /// corpse). This scenario bumps the attached CorpsePart's
    /// <c>CorpseChance</c> to 100 at spawn time so the playtester never
    /// sees a null-drop that looks like the pipeline's broken. Production
    /// code path is unchanged.</para>
    /// </summary>
    [Scenario(
        name: "Snapjaw Burial",
        category: "AI Behavior",
        description: "Kill a snapjaw, watch the undertaker haul the corpse to the graveyard. Exercises M5.1 + M5.2 + M5.3 end-to-end.")]
    public class SnapjawBurial : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // --- Snapjaw (victim) ---
            // NearPlayer(1,3) instead of AtPlayerOffset(2,0) because the
            // starting village has solid CompassStones at player+2 east and
            // player+6 east. NearPlayer's resolver filters for passable cells
            // and picks one from the Chebyshev band. Same fix M4's
            // ScribeSeeksShelter used after the identical pitfall.
            var snapjaw = ctx.Spawn("Snapjaw").NearPlayer(minRadius: 1, maxRadius: 3);
            if (snapjaw == null)
            {
                ctx.Log("[SnapjawBurial] FAILED: no passable cell in the player+1..+3 ring for Snapjaw. Unusual village layout — rerun or report.");
                return;
            }

            // Force corpse drop deterministic for the playtest. Default
            // CorpseChance=70 means the playtester might see a null drop and
            // assume the pipeline is broken — bump to 100 here.
            var corpsePart = snapjaw.GetPart<CorpsePart>();
            if (corpsePart != null)
            {
                corpsePart.CorpseChance = 100;
            }
            else
            {
                ctx.Log("[SnapjawBurial] WARNING: Snapjaw blueprint missing CorpsePart — corpse will not drop. Check Objects.json Snapjaw definition.");
            }

            // --- Undertaker (the AI under test) ---
            // 1000 HP so accidental friendly fire / environmental damage
            // can't kill the undertaker mid-haul during playtest. The known
            // "NPC dies mid-haul" bug (corpse reservation leaks, corpse drops
            // at feet via HandleDeath) is documented in QUD-PARITY.md §M5
            // follow-ups but is a distraction for the happy-path playtest.
            var undertaker = ctx.Spawn("Undertaker")
                .WithStatMax("Hitpoints", 1000)
                .WithHpAbsolute(1000)
                .NearPlayer(minRadius: 3, maxRadius: 5);
            if (undertaker == null)
            {
                ctx.Log("[SnapjawBurial] FAILED: no passable cell in the player+3..+5 ring for Undertaker.");
                return;
            }

            // --- Graveyard (the deposit target) ---
            // ObjectPlacer has only At(x,y) / AtPlayerOffset(dx,dy) — no
            // NearPlayer ring. AtPlayerOffset(5,-3) lands on the y-3 strip
            // that the PlayMode sanity sweep confirmed is open in the
            // starting village (see Docs/QUD-PARITY.md §M5 PlayMode sweep
            // results). Fallback hunt if this specific offset is blocked.
            var grave = TryPlaceGraveyard(ctx);
            if (grave == null)
            {
                ctx.Log("[SnapjawBurial] FAILED: could not place Graveyard — tried multiple offsets, all blocked. Unusual village layout.");
                return;
            }

            var gyPos = ctx.Zone.GetEntityPosition(grave);
            ctx.Log($"[SnapjawBurial] Snapjaw spawned (player+1..+3 ring), Undertaker spawned (player+3..+5 ring), Graveyard at ({gyPos.x},{gyPos.y}). " +
                    "Kill the snapjaw ('s'), watch the corpse ('%') drop, then advance turns ('.') and observe the undertaker ('U') " +
                    "walk to the corpse, pick it up, and deposit it at the graveyard ('+'). Press 't' for thought inspector: " +
                    "expect 'fetching corpse' → 'hauling corpse' → 'buried'.");
        }

        /// <summary>
        /// Try a small fixed set of player-relative offsets for the Graveyard
        /// placement. Each candidate is pre-checked for passability AND for
        /// lack of Solid objects (PhysicsPart.Solid or "Solid" tag) so the
        /// Graveyard doesn't land on a CompassStone / Chest it'll visually
        /// overlap. First candidate that's clean wins.
        /// </summary>
        private static Entity TryPlaceGraveyard(ScenarioContext ctx)
        {
            // Offsets ordered by preference: open y-3 strip first (proven
            // clean in PlayMode sweep), then alternative rows as fallbacks.
            var candidates = new[]
            {
                (5, -3), (6, -3), (4, -3), (7, -3),
                (5,  3), (6,  3), (4,  3),
                (5, -2), (6, -2), (5,  2),
            };
            var playerPos = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            if (playerPos.x < 0) return null;

            foreach (var (dx, dy) in candidates)
            {
                int x = playerPos.x + dx;
                int y = playerPos.y + dy;
                if (!ctx.Zone.InBounds(x, y)) continue;
                var cell = ctx.Zone.GetCell(x, y);
                if (cell == null) continue;
                // Strict steppability — same predicate DisposeOfCorpseGoal uses
                // so we don't drop the Graveyard on top of an invisible-to-
                // IsPassable solid (Chest, CompassStone) the Undertaker then
                // can't navigate to a neighbor of.
                bool clean = true;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var obj = cell.Objects[i];
                    if (obj.HasTag("Solid")) { clean = false; break; }
                    var phys = obj.GetPart<PhysicsPart>();
                    if (phys != null && phys.Solid) { clean = false; break; }
                }
                if (!clean) continue;
                return ctx.World.PlaceObject("Graveyard").At(x, y);
            }
            return null;
        }
    }
}
