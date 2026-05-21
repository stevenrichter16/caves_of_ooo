using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// G.7+ playable showcase — walk east through 5 different gas
    /// types to see each effect on a dummy + on yourself. Not a
    /// self-auditing bench (that's G.12); this is a *play* scenario:
    /// the player has 500 HP and walks through clouds to feel them.
    ///
    /// <para><b>Layout</b>:
    /// <code>
    ///   Player @ p
    ///                    POISON     STUN      CONFUSION   CRYO      SLEEP
    ///   ░░░░░░░░░░░░░░░░░░ °°°°° ░░░ °°°°° ░░░ °°°°° ░░░ °°°°° ░░░ °°°°° ░░░
    ///   [unmask][mask][imm] •      [d]       [d]       [d]       [d]
    /// </code>
    /// Each gas pool is a 3-cell strip (so dispersal makes them grow over
    /// time). Each strip has 1 dummy (passive Snapjaw with 800 HP) so
    /// the player can compare effects on a creature vs themselves.
    /// The poison strip has THREE dummies (bare, masked, immune) so
    /// G.6 defenses are visible side-by-side.</para>
    ///
    /// <para><b>How to play</b>: walk east. Each gas cloud you step into
    /// applies its corresponding effect (poison damage, stun, confusion,
    /// cold + frozen, sleep). The dummies in each cloud get the same
    /// effect — watch the [Applied] / [PoisonTick] log lines + their
    /// HP. The defenses dummy column (poison only) shows how a gas-mask
    /// or full immunity changes the outcome.</para>
    /// </summary>
    [Scenario(
        name: "Gas System Showcase",
        category: "Combat",
        description: "Walk east through 5 gas types (poison, stun, confusion, cryo, sleep). See defenses (mask, immunity) side-by-side in the poison strip.")]
    public class GasSystemShowcase : IScenario
    {
        // Density 300 = thick enough to last many turns before
        // dispersal drops below the low-threshold flicker-out gate.
        private const int CLOUD_DENSITY = 300;
        private const int CLOUD_LEVEL = 1;

        private static readonly (string id, string label, string color)[] GasStrips = new[]
        {
            ("poison-vapor",    "POISON",    "&g"),
            ("stun-vapor",      "STUN",      "&Y"),
            ("confusion-vapor", "CONFUSION", "&M"),
            ("cryo-mist",       "CRYO",      "&C"),
            ("sleep-vapor",     "SLEEP",     "&B"),
            // G.8d — fungal spores: probabilistic infection (chance
            // scales vs Toughness). The downwind snapjaw may take a
            // few turns to actually catch it, then progresses through
            // the multi-stage infection + becomes a contagion vector
            // itself (Blooming/Terminal hosts release more spore gas).
            ("fungal-spores",   "FUNGAL",    "&G"),
        };

        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Player beefed up so the showcase isn't a death-march.
            ctx.Player
                .SetStatMax("Hitpoints", 500)
                .SetHp(500)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24);

            // Clear a wide corridor east of the player so gas can be
            // seen + dummies can be placed without bumping decor.
            // 6 strips at p.x+8 + i*8 → last (fungal, i=5) at p.x+48,
            // its dummy at p.x+49. 56 leaves headroom for contagion
            // spread east of the fungal strip.
            int corridorWidth = 56;
            for (int dx = 1; dx <= corridorWidth; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                    ctx.World.ClearCell(p.x + dx, p.y + dy);
            }

            // Defenses column — at p.x+3..+5, one dummy per defense
            // configuration. All sit in the poison strip's path so
            // they share the cloud + the player can compare HP/effects.
            // (The poison strip starts at p.x+5.)
            //   p.x+3 : bare snapjaw  (no defense)
            //   p.x+4 : masked snapjaw (GasMaskPart Power=10)
            //   p.x+5 : immune snapjaw (GasImmunityPart GasType=Poison)
            //   This trio gets enveloped when poison disperses west.
            var bareSnapjaw = SpawnDummy(ctx, p.x + 3, p.y, "bare");
            var maskedSnapjaw = SpawnDummy(ctx, p.x + 4, p.y, "masked");
            maskedSnapjaw?.AddPart(new GasMaskPart { Power = 10 });
            var immuneSnapjaw = SpawnDummy(ctx, p.x + 5, p.y, "poison-immune");
            immuneSnapjaw?.AddPart(new GasImmunityPart { GasType = "Poison" });

            // Place each gas strip — 3 cells wide, 1 cell tall.
            // Strip i centered at (p.x + 8 + i*8, p.y).
            // A passive Snapjaw sits at the strip's east edge so the
            // player sees both "I'm in the cloud" and "the dummy is too."
            for (int i = 0; i < GasStrips.Length; i++)
            {
                var (id, label, color) = GasStrips[i];
                int stripX = p.x + 8 + i * 8;
                for (int dx = -1; dx <= 1; dx++)
                {
                    GasFactory.SpawnGas(ctx.Zone, stripX + dx, p.y, id,
                        density: CLOUD_DENSITY, level: CLOUD_LEVEL,
                        creator: ctx.PlayerEntity);
                }
                // Passive Snapjaw east of the strip, in the cloud's path.
                SpawnDummy(ctx, stripX + 1, p.y, label.ToLowerInvariant() + "-victim");
            }

            ctx.Log("=== Gas System Showcase ===");
            ctx.Log("Walk EAST through 6 gas types (poison → stun → confusion → cryo → sleep → fungal).");
            ctx.Log("First column (p.x+3..+5): bare / masked / poison-immune snapjaws.");
            ctx.Log("Each gas strip: 3 cells of cloud + 1 passive snapjaw downwind.");
            ctx.Log("FUNGAL is probabilistic — the snapjaw may take a few turns to catch it,");
            ctx.Log("  then progresses Incubation→Symptomatic→Blooming→Terminal + spreads spores.");
            ctx.Log("Watch [Applied] / [PoisonTick] / [Knockback] / [Contagion] log lines.");
            ctx.Log("Diag: diag_query category=gas (Created/Dispersed/Spread/Applied/Merged/Contagion/...).");
            ctx.Log("Relaunch the scenario to reset.");
        }

        private static Entity SpawnDummy(ScenarioContext ctx, int x, int y, string label)
        {
            var npc = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 800)
                .WithHpAbsolute(800)
                .Passive()
                .NotRegisteredForTurns() // immortal-feeling: doesn't actively attack the player
                .At(x, y);
            if (npc == null) return null;
            // Make sure the dummy has the elemental resistances stat
            // slots so cold-gas etc. has somewhere to compute against.
            void EnsureStat(string name)
            {
                if (npc.GetStat(name) != null) return;
                npc.Statistics[name] = new Stat
                { Owner = npc, Name = name, BaseValue = 0, Min = -200, Max = 400 };
            }
            EnsureStat("ColdResistance");
            EnsureStat("HeatResistance");
            EnsureStat("AcidResistance");
            EnsureStat("ElectricResistance");
            // Render label appended for visual disambiguation.
            var render = npc.GetPart<RenderPart>();
            if (render != null)
                render.DisplayName = "snapjaw (" + label + ")";
            return npc;
        }
    }
}
