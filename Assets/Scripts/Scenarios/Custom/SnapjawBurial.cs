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
            var snapjaw = ctx.Spawn("Snapjaw").AtPlayerOffset(2, 0);
            if (snapjaw == null)
            {
                ctx.Log("[SnapjawBurial] FAILED: could not place Snapjaw at player+2,+0. Check console for a spawn-skip warning.");
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
            var undertaker = ctx.Spawn("Undertaker").NearPlayer(minRadius: 3, maxRadius: 4);
            if (undertaker == null)
            {
                ctx.Log("[SnapjawBurial] FAILED: could not place Undertaker in the player+3..+4 ring.");
                return;
            }

            // --- Graveyard (the deposit target) ---
            // ObjectPlacer only supports At(x,y) and AtPlayerOffset(dx,dy) —
            // no NearPlayer ring. player+6 east gives enough travel distance
            // to visually observe the haul phase without leaving the screen.
            var grave = ctx.World.PlaceObject("Graveyard").AtPlayerOffset(6, 0);
            if (grave == null)
            {
                ctx.Log("[SnapjawBurial] FAILED: could not place Graveyard at player+6,+0. Check for a blocking wall/obstacle.");
                return;
            }

            ctx.Log("[SnapjawBurial] Snapjaw (player+2), Undertaker (player+3..+4), Graveyard (player+6). " +
                    "Kill the snapjaw ('s'), watch the corpse ('%') drop, then advance turns ('.') and observe the undertaker ('U') " +
                    "walk to the corpse, pick it up, and deposit it at the graveyard ('+'). Press 't' for thought inspector: " +
                    "expect 'fetching corpse' → 'hauling corpse' → 'buried'.");
        }
    }
}
