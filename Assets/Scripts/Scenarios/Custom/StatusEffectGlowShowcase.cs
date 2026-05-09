using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Status-effect glow showcase. Spawns six Snapjaws in a vertical
    /// column east of the player; five wear an HDR-color status effect
    /// (Burning, Acidic, Electrified, Frozen, Poisoned), one is a
    /// control with no effect. Lets you visually verify whether the
    /// Pass 3 §3.B HDR colors are actually blooming on the screen.
    ///
    ///                  [Snapjaw E-2: NO EFFECT (control)]
    ///                  [Snapjaw E-1: Burning   (HDR red)   ]
    ///                  [Snapjaw E  : Acidic    (HDR green) ]
    ///   [Player] →→→→  [Snapjaw E+1: Electrified (HDR yellow)]
    ///                  [Snapjaw E+2: Frozen    (HDR cyan)  ]
    ///                  [Snapjaw E+3: Poisoned  (HDR green) ]
    ///
    /// <para><b>How to use:</b> click <c>Caves Of Ooo / Scenarios /
    /// Combat Stress / Status Effect Glow Showcase</c>; press ▶ Play;
    /// the URP Bloom volume from Pass 1 (threshold 1.05) should make
    /// each affected Snapjaw's glyph emit a halo. Compare against the
    /// no-effect Snapjaw — if all six look identical, bloom is NOT
    /// firing (gap is in the runtime render-pipeline; data + wiring
    /// proven by `Pass3WiringAdversarialTests`).</para>
    ///
    /// <para><b>Note:</b> Acidic and Poisoned both use HDR-bright-green
    /// (&amp;*G) so they look the same — that's an authoring choice
    /// (acid and venom share the green-hue language). The control
    /// (no effect) Snapjaw uses the blueprint's default color so you
    /// can confirm the effects are doing the tinting.</para>
    ///
    /// <para>Each Snapjaw has HP=999 + Passive so they don't aggro
    /// the player and the layout stays stable for inspection.</para>
    /// </summary>
    [Scenario(
        name: "Status Effect Glow Showcase",
        category: "Combat",
        description: "Visual verification of Pass 3 §3.B HDR status colors. 5 status-effect Snapjaws + 1 control; URP Bloom should halo each affected glyph. If they all look the same, the runtime render-pipeline gap is the next thing to fix.")]
    public class StatusEffectGlowShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout: parked + immortal so the showcase doesn't end ===
            ctx.Player
                .SetStatMax("Hitpoints", 999)
                .SetHp(999)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24);

            // === Six snapjaws in a vertical column 5 cells east ===
            // Placed at p.x + 5 so they're visible without the player
            // having to walk far. Y offsets keep them in a readable
            // column.
            int x = p.x + 5;

            // Control — no effect, blueprint-default color.
            ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 999).WithHpAbsolute(999)
                .Passive()
                .At(x, p.y - 2);

            // Burning — &*R HDR red.
            var burning = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 999).WithHpAbsolute(999)
                .Passive()
                .At(x, p.y - 1);
            if (burning != null)
                burning.ApplyEffect(new BurningEffect());

            // Acidic — &*G HDR green.
            var acidic = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 999).WithHpAbsolute(999)
                .Passive()
                .At(x, p.y);
            if (acidic != null)
                acidic.ApplyEffect(new AcidicEffect());

            // Electrified — &*Y HDR yellow.
            var electrified = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 999).WithHpAbsolute(999)
                .Passive()
                .At(x, p.y + 1);
            if (electrified != null)
                electrified.ApplyEffect(new ElectrifiedEffect());

            // Frozen — &*C HDR cyan.
            var frozen = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 999).WithHpAbsolute(999)
                .Passive()
                .At(x, p.y + 2);
            if (frozen != null)
                frozen.ApplyEffect(new FrozenEffect());

            // Poisoned — &*G HDR green (same hue as Acidic, by design).
            var poisoned = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 999).WithHpAbsolute(999)
                .Passive()
                .At(x, p.y + 3);
            if (poisoned != null)
                poisoned.ApplyEffect(new PoisonedEffect());

            // === Walk-through log ===
            ctx.Log("=== Status Effect Glow Showcase (Pass 3 §3.B HDR colors) ===");
            ctx.Log($"5 effects + 1 control, in a vertical column at x={x}.");
            ctx.Log("If URP Bloom (threshold 1.05) is firing, each effect's");
            ctx.Log("glyph should have a halo. The no-effect Snapjaw is the");
            ctx.Log("baseline — no halo. If all 6 look identical, the gap is");
            ctx.Log("in the runtime render-pipeline (HDR tilemap shader / ");
            ctx.Log("render-target format). Data + wiring already proven by");
            ctx.Log("Pass3WiringAdversarialTests (11 tests, all GREEN).");
            ctx.Log("");
            ctx.Log("Glow legend:");
            ctx.Log("  y-2 control   (no effect)");
            ctx.Log("  y-1 Burning   (HDR red    &*R)");
            ctx.Log("  y   Acidic    (HDR green  &*G)");
            ctx.Log("  y+1 Electrified (HDR yellow &*Y)");
            ctx.Log("  y+2 Frozen    (HDR cyan   &*C)");
            ctx.Log("  y+3 Poisoned  (HDR green  &*G — same as Acidic)");
        }
    }
}
