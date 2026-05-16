using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Liquid Spell Test Bench (manual QA) — cast elemental spells at
    /// NPCs that are pre-coated AND standing in pools of their liquid,
    /// to see exactly how the coat re-weights the spell.
    ///
    /// Layout (player at p, casts EAST):
    ///
    ///                     [Brine NW]                         (ArcBolt here)
    ///   [Player p] →  [Dry] [Water] [Oil] [Pitch]            (Conflagration line)
    ///                     [Ichor SW] [Acid SW2]              (IceLance / passive)
    ///
    /// The **comparison row is on the player's exact row** so a single
    /// eastward Conflagration (Heat AoE) wave passes through Dry → Water
    /// → Oil → Pitch and you see the SAME spell deal four different
    /// numbers in one cast:
    ///   - Dry   : baseline
    ///   - Water : Heat DAMPENED (FireDampen 40 → ×0.6)
    ///   - Oil   : Heat AMPLIFIED (Combustibility 90 → ×1.45)
    ///   - Pitch : Heat AMPLIFIED (Combustibility 90) + the target is
    ///             also −2 Agi / −3 DV from the LQ.6 coat
    ///
    /// Directional spells for the off-row targets:
    ///   - ArcBolt (Electric) NW at Brine: Conductivity 100 ×2 AND the
    ///     coat's −15 ElectricResistance compounding — the hardest hit.
    ///     (Also try ArcBolt at Water: ×2 + WetEffect.)
    ///   - IceLance (Cold) SW at Carapace-Ichor: the coat's −20
    ///     ColdResistance makes cold land harder (+4 AV is the upside).
    ///   - Acid pool dummy (SW2): no spell needed — its coat ticks Acid
    ///     damage every turn; watch its HP fall on its own.
    ///
    /// Every dummy carries <see cref="SpellTestProbePart"/>, which logs
    /// each incoming hit's element + the live HeatRes/ElecRes/ColdRes so
    /// you can read the lever per cast. Compare a coated dummy's HP drop
    /// to the Dry control's for the same spell.
    ///
    /// Honesty bound: the coat re-weight happens pre-resistance
    /// (BeforeTakeDamage); the probe line shows raw incoming + the
    /// resistance snapshot, but the authoritative result is the actual
    /// HP delta / floating number — read THAT against the Dry control.
    /// Dummies are neutral high-HP Snapjaws so they survive many casts;
    /// they may shuffle — re-aim as needed (pools re-coat on re-entry).
    /// </summary>
    [Scenario(
        name: "Liquid Spell Test Bench",
        category: "Combat",
        description: "Pre-coated NPCs standing in water/oil/pitch/brine/ichor/acid pools + a Dry control, with ArcBolt/Conflagration/IceLance/AcidSpray granted. Cast and compare coated vs dry.")]
    public class LiquidSpellTestBench : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Beefy caster with the elemental spell kit.
            ctx.Player
                .SetStatMax("Hitpoints", 999)
                .SetHp(999)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .AddMutation("ArcBoltMutation", level: 5)        // Electric, aimed
                .AddMutation("ConflagrationMutation", level: 5)  // Heat, AoE wave
                .AddMutation("IceLanceMutation", level: 5)       // Cold, aimed
                .AddMutation("AcidSprayMutation", level: 5)      // Acid, aimed
                .GiveItem("HealingTonic", 5);

            // ── Conflagration comparison line (player's row) ──
            // One eastward Heat wave → 4 different numbers.
            Dummy(ctx, null, 0, p.x + 2, p.y);          // Dry control
            Dummy(ctx, "water", 60, p.x + 4, p.y);      // Heat ↓ / Electric ↑
            Dummy(ctx, "oil", 60, p.x + 6, p.y);        // Heat ↑↑
            Dummy(ctx, "pitch", 60, p.x + 8, p.y);      // Heat ↑ + Agi/DV ↓

            // ── Directional targets (off rows) ──
            Dummy(ctx, "brine", 60, p.x + 4, p.y - 2);  // Electric ↑ + −ElecRes
            Dummy(ctx, "carapace-ichor", 60, p.x + 4, p.y + 2); // Cold ↑ / +AV
            Dummy(ctx, "acid", 60, p.x + 6, p.y + 2);   // passive Acid tick

            ctx.Log("=== Liquid Spell Test Bench ===");
            ctx.Log("Spells: ArcBolt (Electric), Conflagration (Heat AoE),");
            ctx.Log("        IceLance (Cold), AcidSpray (Acid).");
            ctx.Log("ROW (your row, east): Dry | Water | Oil | Pitch —");
            ctx.Log("  one Conflagration wave = 4 different fire numbers.");
            ctx.Log("NW Brine: ArcBolt → ×2 conductivity + −15 ElecRes.");
            ctx.Log("SW Ichor: IceLance → amplified (−20 ColdRes), +4 AV.");
            ctx.Log("SW Acid : no spell — its coat ticks Acid every turn.");
            ctx.Log("Read [SpellTest] lines; compare each coated HP drop to Dry.");
        }

        /// <summary>
        /// Spawn a neutral high-HP dummy, pre-apply its liquid coat
        /// (<paramref name="liquidId"/> null = the Dry control), surround
        /// it with a 5-cell pool cluster of that liquid so it reads as
        /// "standing in a pool" and re-coats if it shuffles, and attach
        /// the probe.
        /// </summary>
        private static void Dummy(ScenarioContext ctx, string liquidId, int amount, int x, int y)
        {
            var npc = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 300)
                .WithHpAbsolute(300)
                .At(x, y);
            if (npc == null) return;

            npc.AddPart(new SpellTestProbePart { CoatLabel = liquidId ?? "dry" });

            if (string.IsNullOrEmpty(liquidId))
                return; // Dry control: no coat, no pool.

            // Surround with a pool cluster (center + 4 orthogonal) so the
            // dummy is visibly IN the liquid (and re-coats on re-entry).
            int[,] cells = { { 0, 0 }, { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                var pool = new Entity
                { ID = $"{liquidId}Pool_{x}_{y}_{i}", BlueprintName = liquidId + "Pool" };
                pool.AddPart(new RenderPart { DisplayName = liquidId + " pool", RenderString = "~" });
                pool.AddPart(new PhysicsPart { Solid = false });
                pool.AddPart(new LiquidPoolPart { LiquidId = liquidId, Volume = amount + 40 });
                ctx.Zone.AddEntity(pool, x + cells[i, 0], y + cells[i, 1]);
            }

            // Pre-apply the coat so spells work immediately (no need to
            // herd the dummy onto a pool first — transfer-on-contact is
            // exercised by the Liquid Hazard Showcase instead).
            npc.ApplyEffect(new LiquidCoveredEffect(liquidId, amount), source: null, zone: ctx.Zone);
        }
    }

    /// <summary>
    /// Showcase-only probe. On every incoming hit logs the element, the
    /// raw amount the dummy is about to take, the live elemental
    /// resistances (so the LQ.6 stat coats are visible), and the coat
    /// label — so the spell × liquid lever is readable per cast.
    /// Production combat does not emit these lines.
    /// </summary>
    public class SpellTestProbePart : Part
    {
        public override string Name => "SpellTestProbe";

        /// <summary>Which liquid this dummy is coated with ("dry" =
        /// the no-coat control). Set by the scenario.</summary>
        public string CoatLabel = "dry";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "BeforeTakeDamage" || !(e.GetParameter("Damage") is Damage d))
                return true;

            string who = ParentEntity?.GetDisplayName() ?? "?";
            string elem =
                d.IsHeatDamage() ? "HEAT" :
                d.IsElectricDamage() ? "ELECTRIC" :
                d.IsColdDamage() ? "COLD" :
                d.IsAcidDamage() ? "ACID" : "physical";
            int hr = ParentEntity?.GetStatValue("HeatResistance", 0) ?? 0;
            int er = ParentEntity?.GetStatValue("ElectricResistance", 0) ?? 0;
            int cr = ParentEntity?.GetStatValue("ColdResistance", 0) ?? 0;

            MessageLog.Add(
                $"[SpellTest] {who} [{CoatLabel}]: {elem} in={d.Amount} " +
                $"(HeatRes={hr} ElecRes={er} ColdRes={cr})");
            return true;
        }
    }
}
