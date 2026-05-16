using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Liquid Spell Test Bench (manual QA) — cast elemental spells at
    /// stationary, permanently-coated NPCs and watch each coat re-weight
    /// the spell.
    ///
    /// <para><b>v2 rebuild (after a diag audit of the v1 run).</b> The
    /// v1 bench was correct in mechanics but unobservable: coats dried
    /// in ~1-2 turns (water Fluidity 30 + Evap 20), Snapjaw dummies
    /// wandered and brawled (156 melee exchanges, 81 re-coats), and
    /// Snapjaw lacks HeatResistance/ElectricResistance/ColdResistance/
    /// AV/DV so the LQ.6 stat liquids applied nothing. Diag showed only
    /// 2 PreDamageMutation across 215 hits — the coat was almost never
    /// present at impact. This rebuild fixes the HARNESS (the mechanics
    /// were already proven by `damage/PreDamageMutation` turn 410:
    /// Fire 3→4, 4→6 = ×1.45 Combustibility amp).</para>
    ///
    /// Three harness fixes:
    ///   1. <c>.NotRegisteredForTurns()</c> → the dummy never takes a
    ///      turn → never gets EndTurn → the coat never dries. Combined
    ///      with a huge coat Amount it is effectively permanent for any
    ///      manual session.
    ///   2. <c>.Passive().NotRegisteredForTurns()</c> → it never moves
    ///      or fights — a clean stationary subject.
    ///   3. Resistance/combat stats injected directly onto the entity
    ///      (Snapjaw's blueprint lacks them, so `WithStat` would no-op)
    ///      so brine (+HeatRes/−ElecRes), pitch (−Agi/−DV) and
    ///      carapace-ichor (+AV/−ColdRes) deltas actually land and the
    ///      Cold-on-ichor amplification works via ColdResistance.
    ///
    /// Layout (player at p, casts EAST):
    ///
    ///   p.y-2:                 [Brine]            (ArcBolt → Electric)
    ///   p.y  : [Player] → [Dry][Water][Oil][Pitch]  (Conflagration line)
    ///   p.y+2:                 [Ichor]            (IceLance → Cold)
    ///
    /// The comparison line is on the player's exact row, so ONE eastward
    /// Conflagration (Heat AoE) wave shows the SAME spell deal four
    /// different numbers:
    ///   - Dry   : baseline
    ///   - Water : Heat ×0.6  (FireDampen 40)
    ///   - Oil   : Heat ×1.45 (Combustibility 90)
    ///   - Pitch : Heat ×1.45 (Combustibility 90) + the target is also
    ///             −2 Agi / −3 DV from the LQ.6 stat coat
    /// Off-row directional targets:
    ///   - Brine (NW), ArcBolt → Electric ×2 (Conductivity 100) AND the
    ///     coat's −15 ElectricResistance compounding via resistance.
    ///   - Carapace-Ichor (SW), IceLance → Cold amplified by the coat's
    ///     −20 ColdResistance (+4 AV is the upside; try a Melee swing).
    ///
    /// <para><b>Read it two ways.</b> (1) The floating number / HP-bar
    /// drop vs the Dry control for the same spell. (2) The authoritative
    /// confirmation — query the diag the tool emits:
    /// <c>diag_query category=damage kind=PreDamageMutation</c> shows
    /// <c>amountBefore/amountAfter/delta</c> per coat-modified hit;
    /// <c>diag_query category=liquid</c> shows Coated/StatModApplied.
    /// The probe also prints a <c>[SpellTest]</c> line per incoming hit
    /// with the element + live resistances + coat label.</para>
    ///
    /// Honesty bound: the coat re-weight is pre-resistance; the probe's
    /// printed <c>in=</c> is the amount at probe time (ordering vs the
    /// coat hook is not guaranteed), so trust the HP delta / the
    /// PreDamageMutation diag for the exact figure. Script-verified via
    /// smoke; the in-editor feel is yours to judge.
    /// </summary>
    [Scenario(
        name: "Liquid Spell Test Bench",
        category: "Combat",
        description: "Stationary, permanently-coated, stat-bearing NPCs (water/oil/pitch/brine/ichor) + a Dry control, with ArcBolt/Conflagration/IceLance/AcidSpray granted. Cast and compare coated vs dry; confirm via diag_query PreDamageMutation.")]
    public class LiquidSpellTestBench : IScenario
    {
        // Huge so the coat is effectively permanent even if some passive
        // sim loop ever ticks dry-down on an unregistered entity.
        private const int PERMA_COAT = 100000;

        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

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

            // Conflagration comparison line — one Heat wave, 4 numbers.
            Dummy(ctx, null, p.x + 2, p.y);          // Dry control
            Dummy(ctx, "water", p.x + 4, p.y);       // Heat ×0.6 / Electric ×2
            Dummy(ctx, "oil", p.x + 6, p.y);         // Heat ×1.45
            Dummy(ctx, "pitch", p.x + 8, p.y);       // Heat ×1.45 + Agi/DV ↓

            // Directional off-row targets.
            Dummy(ctx, "brine", p.x + 4, p.y - 2);          // ArcBolt: ×2 + −ElecRes
            Dummy(ctx, "carapace-ichor", p.x + 4, p.y + 2); // IceLance: −ColdRes / +AV

            ctx.Log("=== Liquid Spell Test Bench (v2 — permanent coats, stationary) ===");
            ctx.Log("Spells: ArcBolt(Electric) Conflagration(Heat AoE) IceLance(Cold) AcidSpray.");
            ctx.Log("YOUR ROW east: Dry | Water | Oil | Pitch —");
            ctx.Log("  one Conflagration wave = 4 different fire numbers.");
            ctx.Log("NW Brine: ArcBolt → Electric ×2 (Conductivity) + −15 ElecRes.");
            ctx.Log("SW Ichor: IceLance → Cold amplified (−20 ColdRes); +4 AV vs melee.");
            ctx.Log("Coats are PERMANENT here (dummies never take a turn).");
            ctx.Log("Confirm exact numbers: diag_query category=damage kind=PreDamageMutation");
            ctx.Log("…and diag_query category=liquid (Coated / StatModApplied).");
        }

        /// <summary>
        /// Spawn a stationary, turn-unregistered, stat-bearing dummy,
        /// inject the combat/resistance stats Snapjaw's blueprint lacks
        /// (so every liquid's deltas are observable), pre-apply a huge
        /// permanent coat (<paramref name="liquidId"/> null = the Dry
        /// control), ring it with a cosmetic pool, and attach the probe.
        /// </summary>
        private static void Dummy(ScenarioContext ctx, string liquidId, int x, int y)
        {
            var npc = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 4000)
                .WithHpAbsolute(4000)   // survive heavy repeated casting
                .Passive()
                .NotRegisteredForTurns() // never moves, never fights, coat never dries
                .At(x, y);
            if (npc == null) return;

            // Inject the stats Snapjaw's blueprint doesn't carry, so the
            // LQ.6 stat/resistance coats land and ApplyResistances can
            // read them. BaseValue 0 → the coat delta IS the value.
            void Stat(string n, int v) => npc.Statistics[n] =
                new Stat { Owner = npc, Name = n, BaseValue = v, Min = -200, Max = 400 };
            Stat("HeatResistance", 0);
            Stat("ColdResistance", 0);
            Stat("ElectricResistance", 0);
            Stat("AV", 0);
            Stat("DV", 0);
            if (npc.GetStat("Agility") == null) Stat("Agility", 14);

            npc.AddPart(new SpellTestProbePart { CoatLabel = liquidId ?? "dry" });

            if (string.IsNullOrEmpty(liquidId))
                return; // Dry control: no coat, no pool.

            // Cosmetic pool ring (the "surrounded by liquid" the bench is
            // for). Functionally the coat below is what spells react to.
            int[,] cells = { { 0, 0 }, { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                var pool = new Entity
                { ID = $"{liquidId}Pool_{x}_{y}_{i}", BlueprintName = liquidId + "Pool" };
                pool.AddPart(new RenderPart { DisplayName = liquidId + " pool", RenderString = "~" });
                pool.AddPart(new PhysicsPart { Solid = false });
                pool.AddPart(new LiquidPoolPart { LiquidId = liquidId, Volume = PERMA_COAT });
                ctx.Zone.AddEntity(pool, x + cells[i, 0], y + cells[i, 1]);
            }

            // Permanent pre-applied coat — spells react immediately and
            // it never dries (dummy is turn-unregistered + huge Amount).
            npc.ApplyEffect(new LiquidCoveredEffect(liquidId, PERMA_COAT), source: null, zone: ctx.Zone);
        }
    }

    /// <summary>
    /// Showcase-only probe. On every incoming hit logs the element, the
    /// amount the dummy is about to take, the live elemental resistances
    /// (so the LQ.6 stat coats are visible), and the coat label — so the
    /// spell × liquid lever is readable per cast. Production combat does
    /// not emit these lines; the authoritative figures are the HP delta
    /// and the <c>damage/PreDamageMutation</c> diag record.
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
